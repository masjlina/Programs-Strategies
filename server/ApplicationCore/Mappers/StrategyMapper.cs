using Infrastructure.Entities;

namespace ApplicationCore.Dtos.Mappers;

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
            AdministrativeUnitId = entity.AdministrativeUnitId,
            Title = entity.Title,
            StrategicGoals = entity.StrategicGoals.Select(_strategicGoalMapper.ToDto).ToList()
        };
    }

    public Strategy ToEntity(StrategyDto dto)
    {
        return new Strategy
        {
            Id = dto.Id ?? Guid.Empty,
            AdministrativeUnitId = dto.AdministrativeUnitId,
            Title = dto.Title
        };
    }
}
