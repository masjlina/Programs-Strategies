using ApplicationCore.Dtos;
using ApplicationCore.Dtos.Mappers;
using ApplicationCore.Services.IServices;
using Infrastructure.Data;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApplicationCore.Services;

public class ProgramTaskService : IProgramTaskService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper<ProgramTask, ProgramTaskDto> _mapper;

    public ProgramTaskService(
        ApplicationDbContext dbContext,
        IMapper<ProgramTask, ProgramTaskDto> mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<List<ProgramTaskDto>> GetAllAsync()
    {
        var programTasks = await _dbContext.ProgramTasks.ToListAsync();
        return programTasks.Select(_mapper.ToDto).ToList();
    }

    public async Task<ProgramTaskDto> CreateAsync(ProgramTaskDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var programTaskToAdd = _mapper.ToEntity(dto);

        _dbContext.ProgramTasks.Add(programTaskToAdd);
        await _dbContext.SaveChangesAsync();

        return _mapper.ToDto(programTaskToAdd);
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
        return _mapper.ToDto(programTaskToUpdate);
    }

    public async Task RemoveAsync(Guid id)
    {
        var programTaskToDelete = await _dbContext.ProgramTasks.SingleOrDefaultAsync(x => x.Id == id);

        if (programTaskToDelete is null)
            throw new Exception("The given object does not exist");

        _dbContext.ProgramTasks.Remove(programTaskToDelete);
        await _dbContext.SaveChangesAsync();
    }
}
