namespace AiKamu.Common;

public static class FileHelper
{
    public static string GetFileExtension(string contentType)
    {
        return contentType switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "video/mp4" => ".mp4",
            // Add support for other file types as needed
            _ => string.Empty,
        };
    }
}
