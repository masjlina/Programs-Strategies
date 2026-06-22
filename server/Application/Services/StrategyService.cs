using Application.Dtos;
using Application.Dtos.Mappers;
using Application.Services.IServices;
using Domain.Data;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public class StrategyService : IStrategyService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper<Strategy, StrategyDto> _mapper;
    private readonly IMapper<StrategicGoal, StrategicGoalDto> _strategicGoalMapper;
    private readonly TextProcessingPipeline _textPipeline;
    private readonly IStrategyImportService _importService;

    public StrategyService(
        ApplicationDbContext dbContext,
        IMapper<Strategy, StrategyDto> mapper,
        IMapper<StrategicGoal, StrategicGoalDto> strategicGoalMapper,
        TextProcessingPipeline textPipeline,
        IStrategyImportService importService)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _strategicGoalMapper = strategicGoalMapper;
        _textPipeline = textPipeline;
        _importService = importService;
    }

    public async Task<List<StrategyDto>> GetAllAsync()
    {
        var strategies = await _dbContext.Strategies
            .Include(x => x.KeywordMetrics)
            .ToListAsync();
        return strategies.Select(_mapper.ToDto).ToList();
    }

    public async Task<StrategyDto> GetWithNestedById(Guid id)
    {
        var strategy = await _dbContext.Strategies.Where(x => x.Id == id)
            .Include(x => x.StrategicGoals)
            .ThenInclude(x => x.OperationalGoals)
            .ThenInclude(x => x.ProgramTasks)
            .Include(x => x.KeywordMetrics)
            .SingleOrDefaultAsync();

        if (strategy is null)
            throw new Exception("The given object does not exist");

        return _mapper.ToDto(strategy);
    }

    public async Task<StrategyDto> CreateAsync(StrategyDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var strategyToAdd = _mapper.ToEntity(dto);

        _dbContext.Strategies.Add(strategyToAdd);
        await _dbContext.SaveChangesAsync();

        // Run lemmatization pipeline and save keyword metrics
        var wordCounts = await _textPipeline.ProcessStrategyTextAsync(strategyToAdd.Id);
        await _importService.SaveStrategyMetricsAsync(strategyToAdd.Id, wordCounts);

        return _mapper.ToDto(strategyToAdd);
    }

    public async Task<StrategyDto> UpdateAsync(StrategyDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var strategyId = dto.Id ?? throw new ArgumentException("Strategy id is required", nameof(dto));
        var strategyToUpdate = await _dbContext.Strategies
            .Include(x => x.StrategicGoals)
            .ThenInclude(x => x.OperationalGoals)
            .ThenInclude(x => x.ProgramTasks)
            .SingleOrDefaultAsync(x => x.Id == strategyId);

        if (strategyToUpdate is null)
            throw new Exception("The given object does not exist");

        bool titleChanged = strategyToUpdate.Title != dto.Title;
        bool urlChanged = strategyToUpdate.StrategyUrl != dto.StrategyUrl;
        bool regionChanged = strategyToUpdate.RegionId != dto.RegionId;
        bool districtChanged = strategyToUpdate.DistrictId != dto.DistrictId;
        bool communityChanged = strategyToUpdate.CommunityId != dto.CommunityId;
        bool goalsChanged = AreGoalsChanged(strategyToUpdate.StrategicGoals, dto.StrategicGoals);

        bool hasChanges = titleChanged || urlChanged || regionChanged || districtChanged || communityChanged || goalsChanged;

        if (titleChanged)
            strategyToUpdate.Title = dto.Title;
        if (regionChanged)
            strategyToUpdate.RegionId = dto.RegionId;
        if (districtChanged)
            strategyToUpdate.DistrictId = dto.DistrictId;
        if (communityChanged)
            strategyToUpdate.CommunityId = dto.CommunityId;
        if (urlChanged)
            strategyToUpdate.StrategyUrl = dto.StrategyUrl;

        if (dto.StrategicGoals != null)
        {
            _dbContext.StrategicGoals.RemoveRange(strategyToUpdate.StrategicGoals);
            strategyToUpdate.StrategicGoals = dto.StrategicGoals.Select(_strategicGoalMapper.ToEntity).ToList();
        }

        await _dbContext.SaveChangesAsync();

        if (hasChanges)
        {
            var wordCounts = await _textPipeline.ProcessStrategyTextAsync(strategyToUpdate.Id);
            await _importService.SaveStrategyMetricsAsync(strategyToUpdate.Id, wordCounts);
        }

        var updatedStrategy = await _dbContext.Strategies
            .Include(x => x.StrategicGoals)
            .ThenInclude(x => x.OperationalGoals)
            .ThenInclude(x => x.ProgramTasks)
            .Include(x => x.KeywordMetrics)
            .SingleOrDefaultAsync(x => x.Id == strategyId);

        return _mapper.ToDto(updatedStrategy!);
    }

    private bool AreGoalsChanged(List<StrategicGoal> oldGoals, List<StrategicGoalDto>? newGoals)
    {
        if (newGoals is null) return false;
        if (oldGoals.Count != newGoals.Count) return true;

        for (int i = 0; i < oldGoals.Count; i++)
        {
            var og = oldGoals[i];
            var ng = newGoals[i];

            if (og.Label != ng.Label || og.Title != ng.Title || og.Number != ng.Number)
                return true;

            var oldOps = og.OperationalGoals ?? new();
            var newOps = ng.OperationalGoals ?? new();

            if (oldOps.Count != newOps.Count) return true;

            for (int j = 0; j < oldOps.Count; j++)
            {
                var oOp = oldOps[j];
                var nOp = newOps[j];

                if (oOp.Label != nOp.Label || oOp.Title != nOp.Title || oOp.Number != nOp.Number)
                    return true;

                var oldTasks = oOp.ProgramTasks ?? new();
                var newTasks = nOp.ProgramTasks ?? new();

                if (oldTasks.Count != newTasks.Count) return true;

                for (int k = 0; k < oldTasks.Count; k++)
                {
                    var oTask = oldTasks[k];
                    var nTask = newTasks[k];

                    if (oTask.Label != nTask.Label || oTask.Description != nTask.Description || oTask.Number != nTask.Number)
                        return true;
                }
            }
        }

        return false;
    }

    public async Task RemoveAsync(Guid id)
    {
        var strategyToDelete = await _dbContext.Strategies.SingleOrDefaultAsync(x => x.Id == id);

        if (strategyToDelete is null)
            throw new Exception("The given object does not exist");

        _dbContext.Strategies.Remove(strategyToDelete);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<StrategyDto> AnalyzeAsync(Guid id)
    {
        var strategy = await _dbContext.Strategies
            .Include(x => x.KeywordMetrics)
            .SingleOrDefaultAsync(x => x.Id == id);
        
        if (strategy is null)
            throw new Exception("The given object does not exist");

        var wordCounts = await _textPipeline.ProcessStrategyTextAsync(id);
        await _importService.SaveStrategyMetricsAsync(id, wordCounts);

        var updatedStrategy = await _dbContext.Strategies
            .Include(x => x.StrategicGoals)
            .ThenInclude(x => x.OperationalGoals)
            .ThenInclude(x => x.ProgramTasks)
            .Include(x => x.KeywordMetrics)
            .SingleOrDefaultAsync(x => x.Id == id);

        return _mapper.ToDto(updatedStrategy!);
    }
}
