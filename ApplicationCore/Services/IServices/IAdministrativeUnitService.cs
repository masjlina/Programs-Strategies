using ApplicationCore.Dtos;

namespace ApplicationCore.Services.IServices;

public interface IAdministrativeUnitService
{
    Task<List<AdministrativeUnitDto>> GetAllAsync();
    Task<AdministrativeUnitDto> CreateAsync(AdministrativeUnitDto dto);
    Task<AdministrativeUnitDto> UpdateAsync(AdministrativeUnitDto dto);
    Task RemoveAsync(Guid id);
}