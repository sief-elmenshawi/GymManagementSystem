using GymManagement.BLL.Services.Classes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace GymManagement.Tests;

public sealed class AttachmentServiceTests : IDisposable
{
    private readonly string _root =
        Path.Combine(Path.GetTempPath(), "gym-attachments", Guid.NewGuid().ToString("N"));

    public AttachmentServiceTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public async Task UploadAsync_AcceptsUpperCasePngExtension()
    {
        var service = CreateService();
        await using var stream = new MemoryStream(PngBytes());

        var stored = await service.UploadAsync(stream, "PHOTO.PNG", "uploads", TestContext.Current.CancellationToken);

        Assert.NotNull(stored);
        Assert.EndsWith(".png", stored!, StringComparison.OrdinalIgnoreCase);
        Assert.True(File.Exists(Path.Combine(_root, "uploads", stored)));
    }

    [Fact]
    public async Task UploadAsync_AcceptsGifAllowedByTheUiText()
    {
        var service = CreateService();
        await using var stream = new MemoryStream(GifBytes());

        var stored = await service.UploadAsync(stream, "photo.GIF", "uploads", TestContext.Current.CancellationToken);

        Assert.NotNull(stored);
        Assert.EndsWith(".gif", stored!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UploadAsync_RejectsContentThatDoesNotMatchExtension()
    {
        var service = CreateService();
        await using var stream = new MemoryStream("not an image"u8.ToArray());

        var stored = await service.UploadAsync(stream, "photo.png", "uploads", TestContext.Current.CancellationToken);

        Assert.Null(stored);
        Assert.False(Directory.Exists(Path.Combine(_root, "uploads")));
    }

    [Fact]
    public async Task UploadAsync_RejectsFileAboveConfiguredLimit()
    {
        var options = new AttachmentOptions { MaxFileSizeBytes = 8 };
        var service = CreateService(options);
        await using var stream = new MemoryStream(PngBytes());

        var stored = await service.UploadAsync(stream, "photo.png", "uploads", TestContext.Current.CancellationToken);

        Assert.Null(stored);
    }

    private AttachmentService CreateService(AttachmentOptions? options = null)
    {
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(value => value.ContentRootPath).Returns(_root);

        return new AttachmentService(
            environment.Object,
            NullLogger<AttachmentService>.Instance,
            Options.Create(options ?? new AttachmentOptions()));
    }

    private static byte[] PngBytes()
    {
        var bytes = new List<byte>(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
        bytes.AddRange("payload"u8.ToArray());
        return bytes.ToArray();
    }

    private static byte[] GifBytes()
    {
        var bytes = new List<byte>("GIF89a"u8.ToArray());
        bytes.AddRange(new byte[] { 0x01, 0x00, 0x01, 0x00 });
        return bytes.ToArray();
    }
}
