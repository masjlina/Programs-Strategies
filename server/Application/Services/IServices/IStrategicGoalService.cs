using Application.Dtos;

namespace Application.Services.IServices;

public interface IStrategicGoalService
{
    Task<List<StrategicGoalDto>> GetAllAsync();
    Task<StrategicGoalDto> CreateAsync(StrategicGoalDto dto);
    Task<StrategicGoalDto> UpdateAsync(StrategicGoalDto dto);
    Task RemoveAsync(Guid id);
}
