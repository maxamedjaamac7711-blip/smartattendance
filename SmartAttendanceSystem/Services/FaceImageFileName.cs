namespace SmartAttendanceSystem.Services;

internal static class FaceImageFileName
{
    public static string? FromStoredPath(string? storedPath)
    {
        if (string.IsNullOrWhiteSpace(storedPath))
        {
            return null;
        }

        var normalizedPath = storedPath.Replace('\\', '/');
        var fileName = Path.GetFileName(normalizedPath);
        return IsValid(fileName) ? fileName : null;
    }

    public static void EnsureValid(string fileName)
    {
        if (!IsValid(fileName))
        {
            throw new ArgumentException("Invalid face image file name.", nameof(fileName));
        }
    }

    private static bool IsValid(string? fileName) =>
        !string.IsNullOrWhiteSpace(fileName) &&
        string.Equals(fileName, Path.GetFileName(fileName), StringComparison.Ordinal) &&
        !fileName.Contains('/') &&
        !fileName.Contains('\\') &&
        fileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase);
}
