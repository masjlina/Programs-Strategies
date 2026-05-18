using ApplicationCore.Dtos;
using ApplicationCore.Dtos.Mappers;
using ApplicationCore.Services.IServices;
using Infrastructure.Data;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApplicationCore.Services;

public class StrategicGoalService : IStrategicGoalService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper<StrategicGoal, StrategicGoalDto> _mapper;

    public StrategicGoalService(
        ApplicationDbContext dbContext,
        IMapper<StrategicGoal, StrategicGoalDto> mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<List<StrategicGoalDto>> GetAllAsync()
    {
        var strategicGoals = await _dbContext.StrategicGoals.ToListAsync();
        return strategicGoals.Select(_mapper.ToDto).ToList();
    }

    public async Task<StrategicGoalDto> CreateAsync(StrategicGoalDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var strategicGoalToAdd = _mapper.ToEntity(dto);

        _dbContext.StrategicGoals.Add(strategicGoalToAdd);
        await _dbContext.SaveChangesAsync();

        return _mapper.ToDto(strategicGoalToAdd);
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
        return _mapper.ToDto(strategicGoalToUpdate);
    }

    public async Task RemoveAsync(Guid id)
    {
        var strategicGoalToDelete = await _dbContext.StrategicGoals.SingleOrDefaultAsync(x => x.Id == id);

        if (strategicGoalToDelete is null)
            throw new Exception("The given object does not exist");

        _dbContext.StrategicGoals.Remove(strategicGoalToDelete);
        await _dbContext.SaveChangesAsync();
    }
}
