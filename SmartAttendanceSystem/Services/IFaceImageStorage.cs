namespace SmartAttendanceSystem.Services;

public interface IFaceImageStorage
{
    Task SaveAsync(string fileName, byte[] image, CancellationToken cancellationToken = default);

    Task<Stream?> OpenReadAsync(string fileName, CancellationToken cancellationToken = default);

    Task DeleteAsync(string fileName, CancellationToken cancellationToken = default);
}
