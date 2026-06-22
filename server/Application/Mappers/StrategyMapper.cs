using System.Linq;
using Domain.Entities;

namespace Application.Dtos.Mappers;

public class StrategyMapper : IMapper<Strategy, StrategyDto>
{
    private readonly IMapper<StrategicGoal, StrategicGoalDto> _strategicGoalMapper;

    public StrategyMapper(IMapper<StrategicGoal, StrategicGoalDto> strategicGoalMapper)
    {
        _strategicGoalMapper = strategicGoalMapper;
    }

    public StrategyDto ToDto(Strategy entity)
    {
        return new StrategyDto
        {
            Id = entity.Id,
            RegionId = entity.RegionId,
            DistrictId = entity.DistrictId,
            CommunityId = entity.CommunityId,
            Title = entity.Title,
            StrategyUrl = entity.StrategyUrl,
            StrategicGoals = entity.StrategicGoals.OrderBy(x => x.Number).Select(_strategicGoalMapper.ToDto).ToList(),
            HasLinguisticAnalysis = entity.KeywordMetrics != null && entity.KeywordMetrics.Count > 0
        };
    }

    public Strategy ToEntity(StrategyDto dto)
    {
        return new Strategy
        {
            Id = dto.Id ?? Guid.Empty,
            RegionId = dto.RegionId,
            DistrictId = dto.DistrictId,
            CommunityId = dto.CommunityId,
            Title = dto.Title,
            StrategyUrl = dto.StrategyUrl,
            StrategicGoals = dto.StrategicGoals.Select(_strategicGoalMapper.ToEntity).ToList()
        };
    }
}
