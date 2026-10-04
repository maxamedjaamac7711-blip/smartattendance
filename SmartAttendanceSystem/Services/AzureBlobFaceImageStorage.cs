using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace SmartAttendanceSystem.Services;

public sealed class AzureBlobFaceImageStorage : IFaceImageStorage
{
    private readonly BlobContainerClient _container;

    public AzureBlobFaceImageStorage(BlobContainerClient container)
    {
        _container = container;
    }

    public async Task SaveAsync(string fileName, byte[] image, CancellationToken cancellationToken = default)
    {
        FaceImageFileName.EnsureValid(fileName);
        await using var stream = new MemoryStream(image, writable: false);
        await _container.GetBlobClient(GetBlobName(fileName)).UploadAsync(
            stream,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = "image/jpeg" }
            },
            cancellationToken);
    }

    public async Task<Stream?> OpenReadAsync(string fileName, CancellationToken cancellationToken = default)
    {
        FaceImageFileName.EnsureValid(fileName);
        var blob = _container.GetBlobClient(GetBlobName(fileName));
        if (!await blob.ExistsAsync(cancellationToken))
        {
            return null;
        }

        return await blob.OpenReadAsync(cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(string fileName, CancellationToken cancellationToken = default)
    {
        FaceImageFileName.EnsureValid(fileName);
        await _container.GetBlobClient(GetBlobName(fileName))
            .DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    private static string GetBlobName(string fileName) => $"uploads/faces/{fileName}";
}
