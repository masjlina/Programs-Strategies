using ApplicationCore.Dtos;
using ApplicationCore.Services.IServices;
using Infrastructure.Data;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApplicationCore.Services;

public class OperationalGoalService : IOperationalGoalService
{
    private readonly ApplicationDbContext _dbContext;

    public OperationalGoalService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<OperationalGoalDto>> GetAllAsync()
    {
        var operationalGoals = await _dbContext.OperationalGoals.ToListAsync();

        return operationalGoals.Select(MapToDto).ToList();
    }

    public async Task<OperationalGoalDto> CreateAsync(OperationalGoalDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var operationalGoalToAdd = new OperationalGoal
        {
            StrategicGoalId = dto.StrategicGoalId,
            Label = dto.Label,
            Number = dto.Number,
            Title = dto.Title
        };

        _dbContext.OperationalGoals.Add(operationalGoalToAdd);
        await _dbContext.SaveChangesAsync();

        return MapToDto(operationalGoalToAdd);
    }

    public async Task<OperationalGoalDto> UpdateAsync(OperationalGoalDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var operationalGoalId = dto.Id ?? throw new ArgumentException("Operational goal id is required", nameof(dto));
        var operationalGoalToUpdate = await _dbContext.OperationalGoals.SingleOrDefaultAsync(x => x.Id == operationalGoalId);

        if (operationalGoalToUpdate is null)
            throw new Exception("The given object does not exist");

        if (operationalGoalToUpdate.StrategicGoalId != dto.StrategicGoalId)
            operationalGoalToUpdate.StrategicGoalId = dto.StrategicGoalId;
        if (operationalGoalToUpdate.Label != dto.Label)
            operationalGoalToUpdate.Label = dto.Label;
        if (operationalGoalToUpdate.Number != dto.Number)
            operationalGoalToUpdate.Number = dto.Number;
        if (operationalGoalToUpdate.Title != dto.Title)
            operationalGoalToUpdate.Title = dto.Title;

        await _dbContext.SaveChangesAsync();
        return MapToDto(operationalGoalToUpdate);
    }

    public async Task RemoveAsync(Guid id)
    {
        var operationalGoalToDelete = await _dbContext.OperationalGoals.SingleOrDefaultAsync(x => x.Id == id);

        if (operationalGoalToDelete is null)
            throw new Exception("The given object does not exist");

        _dbContext.OperationalGoals.Remove(operationalGoalToDelete);
        await _dbContext.SaveChangesAsync();
    }

    private static OperationalGoalDto MapToDto(OperationalGoal operationalGoal)
    {
        return new OperationalGoalDto
        {
            Id = operationalGoal.Id,
            StrategicGoalId = operationalGoal.StrategicGoalId,
            Label = operationalGoal.Label,
            Number = operationalGoal.Number,
            Title = operationalGoal.Title
        };
    }
}
