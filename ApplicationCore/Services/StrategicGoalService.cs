using ApplicationCore.Dtos;
using ApplicationCore.Services.IServices;
using Infrastructure.Data;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApplicationCore.Services;

public class StrategicGoalService : IStrategicGoalService
{
    private readonly ApplicationDbContext _dbContext;

    public StrategicGoalService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<StrategicGoalDto>> GetAllAsync()
    {
        var strategicGoals = await _dbContext.StrategicGoals.ToListAsync();

        return strategicGoals.Select(MapToDto).ToList();
    }

    public async Task<StrategicGoalDto> CreateAsync(StrategicGoalDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var strategicGoalToAdd = new StrategicGoal
        {
            StrategyId = dto.StrategyId,
            Label = dto.Label,
            Number = dto.Number,
            Title = dto.Title
        };

        _dbContext.StrategicGoals.Add(strategicGoalToAdd);
        await _dbContext.SaveChangesAsync();

        return MapToDto(strategicGoalToAdd);
    }

    public async Task<StrategicGoalDto> UpdateAsync(StrategicGoalDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var strategicGoalId = dto.Id ?? throw new ArgumentException("Strategic goal id is required", nameof(dto));
        var strategicGoalToUpdate = await _dbContext.StrategicGoals.SingleOrDefaultAsync(x => x.Id == strategicGoalId);

        if (strategicGoalToUpdate is null)
            throw new Exception("The given object does not exist");

        if (strategicGoalToUpdate.StrategyId != dto.StrategyId)
            strategicGoalToUpdate.StrategyId = dto.StrategyId;
        if (strategicGoalToUpdate.Label != dto.Label)
            strategicGoalToUpdate.Label = dto.Label;
        if (strategicGoalToUpdate.Number != dto.Number)
            strategicGoalToUpdate.Number = dto.Number;
        if (strategicGoalToUpdate.Title != dto.Title)
            strategicGoalToUpdate.Title = dto.Title;

        await _dbContext.SaveChangesAsync();
        return MapToDto(strategicGoalToUpdate);
    }

    public async Task RemoveAsync(Guid id)
    {
        var strategicGoalToDelete = await _dbContext.StrategicGoals.SingleOrDefaultAsync(x => x.Id == id);

        if (strategicGoalToDelete is null)
            throw new Exception("The given object does not exist");

        _dbContext.StrategicGoals.Remove(strategicGoalToDelete);
        await _dbContext.SaveChangesAsync();
    }

    private static StrategicGoalDto MapToDto(StrategicGoal strategicGoal)
    {
        return new StrategicGoalDto
        {
            Id = strategicGoal.Id,
            StrategyId = strategicGoal.StrategyId,
            Label = strategicGoal.Label,
            Number = strategicGoal.Number,
            Title = strategicGoal.Title
        };
    }
}
