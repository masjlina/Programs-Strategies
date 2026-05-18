using ApplicationCore.Dtos;
using ApplicationCore.Dtos.Mappers;
using ApplicationCore.Services.IServices;
using Infrastructure.Data;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApplicationCore.Services;

public class OperationalGoalService : IOperationalGoalService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper<OperationalGoal, OperationalGoalDto> _mapper;

    public OperationalGoalService(
        ApplicationDbContext dbContext,
        IMapper<OperationalGoal, OperationalGoalDto> mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<List<OperationalGoalDto>> GetAllAsync()
    {
        var operationalGoals = await _dbContext.OperationalGoals.ToListAsync();
        return operationalGoals.Select(_mapper.ToDto).ToList();
    }

    public async Task<OperationalGoalDto> CreateAsync(OperationalGoalDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var operationalGoalToAdd = _mapper.ToEntity(dto);

        _dbContext.OperationalGoals.Add(operationalGoalToAdd);
        await _dbContext.SaveChangesAsync();

        return _mapper.ToDto(operationalGoalToAdd);
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
        return _mapper.ToDto(operationalGoalToUpdate);
    }

    public async Task RemoveAsync(Guid id)
    {
        var operationalGoalToDelete = await _dbContext.OperationalGoals.SingleOrDefaultAsync(x => x.Id == id);

        if (operationalGoalToDelete is null)
            throw new Exception("The given object does not exist");

        _dbContext.OperationalGoals.Remove(operationalGoalToDelete);
        await _dbContext.SaveChangesAsync();
    }
}
