using GymManagement.BLL.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GymManagement.BLL.Services.Classes
{
    public class AttachmentService : IAttachmentService
    {
        private readonly AttachmentOptions _options;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<AttachmentService> _logger;

        public AttachmentService(
            IWebHostEnvironment env,
            ILogger<AttachmentService> logger,
            IOptions<AttachmentOptions>? options = null)
        {
            _env = env;
            _logger = logger;
            _options = options?.Value ?? new AttachmentOptions();
        }

        public bool Delete(string fileName, string folderName)
        {
            if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(folderName))
                return false;

            try
            {
                var fullPath = Path.Combine(_env.ContentRootPath, folderName, fileName);

                if (!File.Exists(fullPath))
                    return false;

                File.Delete(fullPath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete attachment {File}.", fileName);
                return false;
            }
        }

        public async Task<string?> UploadAsync(
            Stream fileStream,
            string fileName,
            string folderName,
            CancellationToken ct = default)
        {
            if (fileStream is null || !fileStream.CanRead) return null;

            if (fileStream.CanSeek && fileStream.Length == 0) return null;
            if (fileStream.CanSeek && fileStream.Length > _options.MaxFileSizeBytes)
            {
                _logger.LogWarning("Rejected upload: file too large ({Size} bytes).", fileStream.Length);
                return null;
            }

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            var allowed = _options.AllowedExtensions
                .Select(value => value.ToLowerInvariant())
                .ToArray();
            if (string.IsNullOrEmpty(extension) || !allowed.Contains(extension))
            {
                _logger.LogWarning("Rejected upload: extension {Ext} not allowed.", extension);
                return null;
            }

            try
            {
                // Buffer after the size check so non-seekable streams can still be
                // signature-checked without consuming them twice.
                using var buffer = new MemoryStream();
                await fileStream.CopyToAsync(buffer, ct);
                if (buffer.Length == 0 || buffer.Length > _options.MaxFileSizeBytes)
                {
                    _logger.LogWarning("Rejected upload: file size is outside the allowed range.");
                    return null;
                }

                var bytes = buffer.GetBuffer();
                if (!HasMatchingSignature(bytes, (int)buffer.Length, extension))
                {
                    _logger.LogWarning("Rejected upload: file content does not match extension {Ext}.", extension);
                    return null;
                }

                var uploadsFolder = Path.Combine(_env.ContentRootPath, folderName);
                Directory.CreateDirectory(uploadsFolder);

                var storedFileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, storedFileName);
                buffer.Position = 0;

                await using var fs = new FileStream(
                    filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                await buffer.CopyToAsync(fs, ct);
                return storedFileName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload file {FileName}.", fileName);
                return null;
            }
        }

        public (Stream Stream, string ContentType)? GetFile(string fileName, string folderName)
        {
            if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(folderName)) return null;

            var fullPath = Path.Combine(_env.ContentRootPath, folderName, fileName);

            if (!File.Exists(fullPath)) return null;

            var contentType = Path.GetExtension(fullPath).ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                _ => "application/octet-stream"
            };

            var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return (stream, contentType);
        }

        private static bool HasMatchingSignature(byte[] bytes, int length, string extension)
        {
            ReadOnlySpan<byte> data = bytes.AsSpan(0, Math.Min(length, 8));

            return extension switch
            {
                ".jpg" or ".jpeg" => data.StartsWith(new byte[] { 0xFF, 0xD8, 0xFF }),
                ".png" => data.StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
                ".gif" => data.StartsWith("GIF87a"u8) || data.StartsWith("GIF89a"u8),
                _ => false,
            };
        }
    }
}
