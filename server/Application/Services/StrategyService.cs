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

    public StrategyService(
        ApplicationDbContext dbContext,
        IMapper<Strategy, StrategyDto> mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<List<StrategyDto>> GetAllAsync()
    {
        var strategies = await _dbContext.Strategies.ToListAsync();
        return strategies.Select(_mapper.ToDto).ToList();
    }

    public async Task<StrategyDto> GetWithNestedById(Guid id)
    {
        var strategy = await _dbContext.Strategies.Where(x => x.Id == id)
            .Include(x => x.StrategicGoals)
            .ThenInclude(x => x.OperationalGoals)
            .ThenInclude(x => x.ProgramTasks)
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

        return _mapper.ToDto(strategyToAdd);
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
        if (strategyToUpdate.RegionId != dto.RegionId)
            strategyToUpdate.RegionId = dto.RegionId;
        if (strategyToUpdate.DistrictId != dto.DistrictId)
            strategyToUpdate.DistrictId = dto.DistrictId;
        if (strategyToUpdate.CommunityId != dto.CommunityId)
            strategyToUpdate.CommunityId = dto.CommunityId;
        if (strategyToUpdate.StrategyUrl != dto.StrategyUrl)
            strategyToUpdate.StrategyUrl = dto.StrategyUrl;

        await _dbContext.SaveChangesAsync();
        return _mapper.ToDto(strategyToUpdate);
    }

    public async Task RemoveAsync(Guid id)
    {
        var strategyToDelete = await _dbContext.Strategies.SingleOrDefaultAsync(x => x.Id == id);

        if (strategyToDelete is null)
            throw new Exception("The given object does not exist");

        _dbContext.Strategies.Remove(strategyToDelete);
        await _dbContext.SaveChangesAsync();
    }
}
