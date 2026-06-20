using Microsoft.AspNetCore.Http;

namespace Application.Dtos;

/// <summary>
/// Multipart request for uploading a file to parse.
/// </summary>
public class UploadFileRequest
{
    /// <summary>
    /// Uploaded file.
    /// </summary>
    public IFormFile File { get; set; } = null!;
}
