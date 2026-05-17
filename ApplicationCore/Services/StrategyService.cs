using ApplicationCore.Dtos;
using ApplicationCore.Services.IServices;
using Infrastructure.Data;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApplicationCore.Services;

public class StrategyService : IStrategyService
{
    private readonly ApplicationDbContext _dbContext;

    public StrategyService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<StrategyDto>> GetAllAsync()
    {
        var strategies = await _dbContext.Strategies.ToListAsync();

        return strategies.Select(MapToDto).ToList();
    }

    public async Task<StrategyDto> CreateAsync(StrategyDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var strategyToAdd = new Strategy
        {
            Title = dto.Title,
            AdministrativeUnitId = dto.AdministrativeUnitId
        };

        _dbContext.Strategies.Add(strategyToAdd);
        await _dbContext.SaveChangesAsync();

        return MapToDto(strategyToAdd);
    }

    public async Task<StrategyDto> UpdateAsync(StrategyDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var strategyId = dto.Id ?? throw new ArgumentException("Strategy id is required", nameof(dto));
        var strategyToUpdate = await _dbContext.Strategies.SingleOrDefaultAsync(x => x.Id == strategyId);

        if (strategyToUpdate is null)
            throw new Exception("The given object does not exist");

        if (strategyToUpdate.Title != dto.Title)
            strategyToUpdate.Title = dto.Title;
        if (strategyToUpdate.AdministrativeUnitId != dto.AdministrativeUnitId)
            strategyToUpdate.AdministrativeUnitId = dto.AdministrativeUnitId;

        await _dbContext.SaveChangesAsync();
        return MapToDto(strategyToUpdate);
    }

    public async Task RemoveAsync(Guid id)
    {
        var strategyToDelete = await _dbContext.Strategies.SingleOrDefaultAsync(x => x.Id == id);

        if (strategyToDelete is null)
            throw new Exception("The given object does not exist");

        _dbContext.Strategies.Remove(strategyToDelete);
        await _dbContext.SaveChangesAsync();
    }

    private static StrategyDto MapToDto(Strategy strategy)
    {
        return new StrategyDto
        {
            Id = strategy.Id,
            Title = strategy.Title,
            AdministrativeUnitId = strategy.AdministrativeUnitId
        };
    }
}
