namespace AiKamu.Commands;

public interface IResponse
{
    bool IsSuccess { get; }
    bool IsPrivateResponse { get; }
}

public interface ITextResponse : IResponse
{
    string? Message { get; }
}

public interface IImageResponse : IResponse
{
    string ImageUrl { get; }
    string? Caption { get; }
}

public interface IFileResponse : IResponse
{
    string SourceUrl { get; }
    string? Caption { get; }
}

public class TextResponse(bool isSuccess, string message, bool isPrivateResponse = false) : ITextResponse
{
    public string? Message { get; } = message;
    public bool IsSuccess { get; } = isSuccess;
    public bool IsPrivateResponse { get; } = isPrivateResponse;
}

public class ImageResponse(bool isSuccess, string imageUrl, string caption, bool isPrivateResponse = false) : IImageResponse
{
    public string ImageUrl { get; } = imageUrl;
    public string? Caption { get; } = caption;
    public bool IsSuccess { get; } = isSuccess;
    public bool IsPrivateResponse { get; } = isPrivateResponse;
}

public class FileResponse(bool isSuccess, string sourceUrl, string caption, bool isPrivateResponse = false) : IFileResponse
{
    public string SourceUrl { get; } = sourceUrl;
    public string? Caption { get; } = caption;
    public bool IsSuccess { get; } = isSuccess;
    public bool IsPrivateResponse { get; } = isPrivateResponse;
}