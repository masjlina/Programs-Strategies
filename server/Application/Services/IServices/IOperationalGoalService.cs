using Application.Dtos;

namespace Application.Services.IServices;

public interface IOperationalGoalService
{
    Task<List<OperationalGoalDto>> GetAllAsync();
    Task<OperationalGoalDto> CreateAsync(OperationalGoalDto dto);
    Task<OperationalGoalDto> UpdateAsync(OperationalGoalDto dto);
    Task RemoveAsync(Guid id);
}
