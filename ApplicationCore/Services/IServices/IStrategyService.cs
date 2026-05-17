using ApplicationCore.Dtos;

namespace ApplicationCore.Services.IServices;

public interface IStrategyService
{
    Task<List<StrategyDto>> GetAllAsync();
    Task<StrategyDto> CreateAsync(StrategyDto dto);
    Task<StrategyDto> UpdateAsync(StrategyDto dto);
    Task RemoveAsync(Guid id);
}
