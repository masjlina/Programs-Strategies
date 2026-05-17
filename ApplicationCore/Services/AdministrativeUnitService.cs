using ApplicationCore.Dtos;
using ApplicationCore.Services.IServices;
using Infrastructure.Data;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApplicationCore.Services;

public class AdministrativeUnitService : IAdministrativeUnitService
{
    private readonly ApplicationDbContext _dbContext;

    public AdministrativeUnitService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<AdministrativeUnitDto>> GetAllAsync()
    {
        var administrativeUnits = await _dbContext.AdministrativeUnits.ToListAsync();

        var dtos = new List<AdministrativeUnitDto>();

        foreach (var administrativeUnit in administrativeUnits)
        {
            dtos.Add(new AdministrativeUnitDto()
            {
                Id = administrativeUnit.Id,
                Name = administrativeUnit.Name,
                Type = administrativeUnit.Type,
            });
        }

        return dtos;
    }

    public async Task<AdministrativeUnitDto> CreateAsync(AdministrativeUnitDto dto)
    {
        if (dto is null)
            throw new Exception("The object is null");

        var unitToAdd = new AdministrativeUnit()
        {
            Name = dto.Name,
            Type = dto.Type
        };

        _dbContext.AdministrativeUnits.Add(unitToAdd);
        await _dbContext.SaveChangesAsync();

        return dto;
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
        return dto;
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