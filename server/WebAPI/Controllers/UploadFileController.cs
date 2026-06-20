using Application.Dtos;
using Application.Services.IServices;
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
    private readonly IOfficialDataImportService _officialDataImportService;

    public UploadFileController(
        IParseService parseService,
        IOfficialDataImportService officialDataImportService)
    {
        _parseService = parseService;
        _officialDataImportService = officialDataImportService;
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

    /// <summary>
    /// Parses a document (PDF, DOCX, DOC) and returns the extracted StrategyDto without persisting it.
    /// </summary>
    /// <param name="request">Multipart request containing the file.</param>
    /// <response code="200">Document parsed successfully and Strategy DTO returned.</response>
    /// <response code="400">Uploaded file is invalid or cannot be parsed.</response>
    [HttpPost("parse-document")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ParseDocument([FromForm] UploadFileRequest request)
    {
        try
        {
            var parsed = await _parseService.ParseDocument(request.File);
            return Ok(parsed);
        }
        catch (InvalidDataException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Imports official Ukrainian administrative data from official-data.csv.
    /// </summary>
    /// <param name="request">Multipart request containing official-data.csv.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <response code="200">CSV imported successfully.</response>
    /// <response code="400">Uploaded CSV is invalid or cannot be imported.</response>
    [HttpPost("official-data")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ImportOfficialData(
        [FromForm] UploadFileRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _officialDataImportService.ImportAsync(request.File, cancellationToken);
            return Ok(result);
        }
        catch (InvalidDataException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
