using ApplicationCore.Dtos;
using ApplicationCore.Services.IServices;
using Infrastructure.Data;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApplicationCore.Services;

public class ProgramTaskService : IProgramTaskService
{
    private readonly ApplicationDbContext _dbContext;

    public ProgramTaskService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<ProgramTaskDto>> GetAllAsync()
    {
        var programTasks = await _dbContext.ProgramTasks.ToListAsync();

        return programTasks.Select(MapToDto).ToList();
    }

    public async Task<ProgramTaskDto> CreateAsync(ProgramTaskDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var programTaskToAdd = new ProgramTask
        {
            OperationalGoalId = dto.OperationalGoalId,
            Label = dto.Label,
            Number = dto.Number,
            Description = dto.Description
        };

        _dbContext.ProgramTasks.Add(programTaskToAdd);
        await _dbContext.SaveChangesAsync();

        return MapToDto(programTaskToAdd);
    }

    public async Task<ProgramTaskDto> UpdateAsync(ProgramTaskDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var programTaskId = dto.Id ?? throw new ArgumentException("Program task id is required", nameof(dto));
        var programTaskToUpdate = await _dbContext.ProgramTasks.SingleOrDefaultAsync(x => x.Id == programTaskId);

        if (programTaskToUpdate is null)
            throw new Exception("The given object does not exist");

        if (programTaskToUpdate.OperationalGoalId != dto.OperationalGoalId)
            programTaskToUpdate.OperationalGoalId = dto.OperationalGoalId;
        if (programTaskToUpdate.Label != dto.Label)
            programTaskToUpdate.Label = dto.Label;
        if (programTaskToUpdate.Number != dto.Number)
            programTaskToUpdate.Number = dto.Number;
        if (programTaskToUpdate.Description != dto.Description)
            programTaskToUpdate.Description = dto.Description;

        await _dbContext.SaveChangesAsync();
        return MapToDto(programTaskToUpdate);
    }

    public async Task RemoveAsync(Guid id)
    {
        var programTaskToDelete = await _dbContext.ProgramTasks.SingleOrDefaultAsync(x => x.Id == id);

        if (programTaskToDelete is null)
            throw new Exception("The given object does not exist");

        _dbContext.ProgramTasks.Remove(programTaskToDelete);
        await _dbContext.SaveChangesAsync();
    }

    private static ProgramTaskDto MapToDto(ProgramTask programTask)
    {
        return new ProgramTaskDto
        {
            Id = programTask.Id,
            OperationalGoalId = programTask.OperationalGoalId,
            Label = programTask.Label,
            Number = programTask.Number,
            Description = programTask.Description
        };
    }
}
