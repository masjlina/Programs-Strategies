using ApplicationCore.Dtos;
using Microsoft.AspNetCore.Http;

namespace ApplicationCore.Services.IServices;

public interface IOfficialDataImportService
{
    Task<OfficialDataImportResultDto> ImportAsync(IFormFile file, CancellationToken cancellationToken = default);
}
