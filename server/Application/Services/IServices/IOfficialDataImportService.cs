using Application.Dtos;
using Microsoft.AspNetCore.Http;

namespace Application.Services.IServices;

public interface IOfficialDataImportService
{
    Task<OfficialDataImportResultDto> ImportAsync(IFormFile file, CancellationToken cancellationToken = default);
}
