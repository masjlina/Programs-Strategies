using ApplicationCore.Dtos;
using ApplicationCore.Dtos.Mappers;
using ApplicationCore.Services.IServices;
using Infrastructure.Data;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApplicationCore.Services;

public class AdministrativeUnitService : IAdministrativeUnitService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper<AdministrativeUnit, AdministrativeUnitDto> _mapper;

    public AdministrativeUnitService(
        ApplicationDbContext dbContext,
        IMapper<AdministrativeUnit, AdministrativeUnitDto> mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<List<AdministrativeUnitDto>> GetAllAsync()
    {
        var administrativeUnits = await _dbContext.AdministrativeUnits.ToListAsync();
        return administrativeUnits.Select(_mapper.ToDto).ToList();
    }

    public async Task<AdministrativeUnitDto> CreateAsync(AdministrativeUnitDto dto)
    {
        if (dto is null)
            throw new Exception("The object is null");

        var unitToAdd = _mapper.ToEntity(dto);

        _dbContext.AdministrativeUnits.Add(unitToAdd);
        await _dbContext.SaveChangesAsync();

        return _mapper.ToDto(unitToAdd);
    }

    public async Task<AdministrativeUnitDto> UpdateAsync(AdministrativeUnitDto dto)
    {
        var unitToUpdate = _dbContext.AdministrativeUnits.SingleOrDefault(x => x.Id == dto.Id);

        if (unitToUpdate is null)
            throw new Exception("The given object does not exist");

        if (unitToUpdate.Name != dto.Name)
            unitToUpdate.Name = dto.Name;
        if (unitToUpdate.Type != dto.Type)
            unitToUpdate.Type = dto.Type;

        await _dbContext.SaveChangesAsync();
        return _mapper.ToDto(unitToUpdate);
    }

    public async Task RemoveAsync(Guid id)
    {
        var unitToDelete = await _dbContext.AdministrativeUnits.SingleOrDefaultAsync(x => x.Id == id);

        if (unitToDelete is null)
            throw new Exception("The given object does not exist");

        _dbContext.AdministrativeUnits.Remove(unitToDelete);
        await _dbContext.SaveChangesAsync();
    }
}
