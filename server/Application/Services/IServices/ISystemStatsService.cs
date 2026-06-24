using Application.Dtos;

namespace Application.Services.IServices;

public interface ISystemStatsService
{
    Task<SystemStatsDto> GetStatsAsync(CancellationToken cancellationToken = default);
}
