using Application.Dtos;

namespace Application.Services.IServices;

public interface IStrategyService
{
    Task<List<StrategyDto>> GetAllAsync();
    Task<StrategyDto> GetWithNestedById(Guid id);
    Task<StrategyDto> CreateAsync(StrategyDto dto);
    Task<StrategyDto> UpdateAsync(StrategyDto dto);
    Task RemoveAsync(Guid id);
}
