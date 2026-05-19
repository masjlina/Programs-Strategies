using ApplicationCore.Dtos;

namespace ApplicationCore.Services.IServices;

public interface IProgramTaskService
{
    Task<List<ProgramTaskDto>> GetAllAsync();
    Task<ProgramTaskDto> CreateAsync(ProgramTaskDto dto);
    Task<ProgramTaskDto> UpdateAsync(ProgramTaskDto dto);
    Task RemoveAsync(Guid id);
}
