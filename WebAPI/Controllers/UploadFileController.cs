using ApplicationCore.Dtos;
using ApplicationCore.Services.IServices;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

/// <summary>
/// Handles file upload, parsing, and persistence.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class UploadFileController : ControllerBase
{
    private readonly IParseService _parseService;

    public UploadFileController(IParseService parseService)
    {
        _parseService = parseService;
    }

    /// <summary>
    /// Uploads a file, parses its contents, and saves the parsed data (currently supports only .json file).
    /// </summary>
    /// <param name="request">Multipart request containing the file.</param>
    /// <response code="200">File processed successfully and parsed data returned.</response>
    /// <response code="400">Uploaded file is invalid or cannot be parsed.</response>
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadFile([FromForm] UploadFileRequest request)
    {
        try
        {
            var parsed = await _parseService.Parse(request.File);
            await _parseService.Save(parsed);
            return Ok(parsed);
        }
        catch (InvalidDataException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
