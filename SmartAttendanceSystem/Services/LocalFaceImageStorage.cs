namespace SmartAttendanceSystem.Services;

public sealed class LocalFaceImageStorage : IFaceImageStorage
{
    private readonly string _webRootPath;

    public LocalFaceImageStorage(IWebHostEnvironment environment)
    {
        _webRootPath = environment.WebRootPath
            ?? Path.Combine(environment.ContentRootPath, "wwwroot");
    }

    public async Task SaveAsync(string fileName, byte[] image, CancellationToken cancellationToken = default)
    {
        var path = GetPath(fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllBytesAsync(path, image, cancellationToken);
    }

    public Task<Stream?> OpenReadAsync(string fileName, CancellationToken cancellationToken = default)
    {
        var path = GetPath(fileName);
        Stream? stream = File.Exists(path)
            ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)
            : null;
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string fileName, CancellationToken cancellationToken = default)
    {
        var path = GetPath(fileName);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    private string GetPath(string fileName)
    {
        FaceImageFileName.EnsureValid(fileName);
        return Path.Combine(_webRootPath, "uploads", "faces", fileName);
    }
}
