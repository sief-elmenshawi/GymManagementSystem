using GymManagement.BLL.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GymManagement.BLL.Services.Classes
{
    public class AttachementService : IAttachementService
    {
        private readonly long maxFileSize = 5 * 1024 * 1024;
        private readonly string[] allowedExtentions = { ".jpg", ".png", ".jpeg" };
        private readonly ILogger<AttachementService> logger;
        private readonly IWebHostEnvironment environment;

        public AttachementService(ILogger<AttachementService> logger, IWebHostEnvironment environment)
        {
            this.logger = logger;
            this.environment = environment;
        }

        public bool Delet(string fileName, string folderName)
        {
            if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(folderName)) return false;

            try
            {
                var fullPath = Path.Combine(environment.ContentRootPath, folderName,fileName );

                if(!File.Exists(fullPath)) return false;

                File.Delete(fullPath);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to Delete The Attachment");
                return false;
            }
        }

        public (Stream stream, string contentType)? GetFile(string fileName, string FolderName)
        {
            throw new NotImplementedException();
        }

        public async Task<string?> UploadAsync(Stream fileStream, string fileName, string folderName, CancellationToken ct = default)
        {
            if (fileStream is null || !fileStream.CanRead) return null;

            if (fileStream.Length == 0) return null;
            
            if(fileStream.Length > maxFileSize)
            {
                logger.LogWarning("Rejected File Too Large");
                return null;
            }

            var extention = Path.GetExtension(fileName);
            if(string.IsNullOrEmpty(extention) || !allowedExtentions.Contains(extention))
            {
                logger.LogWarning("Rejected wrong Extenstion File ");
                return null;
            }

            var uploadedFolder =  Path.Combine(environment.ContentRootPath,folderName);  

            Directory.CreateDirectory(uploadedFolder);

            var storedFileName = $"{Guid.NewGuid()}{extention}";

            var filePath = Path.Combine(uploadedFolder, storedFileName);

            try
            {
                await using var fs = new FileStream(filePath ,FileMode.CreateNew , FileAccess.Write , FileShare.None);
                await fileStream.CopyToAsync(fs);
                return storedFileName;

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Faile to Upload file");
                return null;
               
            }

        }
    }
}
