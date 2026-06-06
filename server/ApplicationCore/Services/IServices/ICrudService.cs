namespace ApplicationCore.Services.IServices;

public interface ICrudService<TDto>
{
    Task<List<TDto>> GetAllAsync();
    Task<TDto> CreateAsync(TDto dto);
    Task<TDto> UpdateAsync(TDto dto);
    Task RemoveAsync(Guid id);
}
