using Domain.Entities;

namespace Application.Dtos.Mappers;

public class StrategicGoalMapper : IMapper<StrategicGoal, StrategicGoalDto>
{
    private readonly IMapper<OperationalGoal, OperationalGoalDto> _operationalGoalMapper;

    public StrategicGoalMapper(IMapper<OperationalGoal, OperationalGoalDto> operationalGoalMapper)
    {
        _operationalGoalMapper = operationalGoalMapper;
    }

    public StrategicGoalDto ToDto(StrategicGoal entity)
    {
        return new StrategicGoalDto
        {
            Id = entity.Id,
            StrategyId = entity.StrategyId,
            Label = entity.Label,
            Number = entity.Number,
            Title = entity.Title,
            OperationalGoals = entity.OperationalGoals.Select(_operationalGoalMapper.ToDto).ToList()
        };
    }

    public StrategicGoal ToEntity(StrategicGoalDto dto)
    {
        return new StrategicGoal
        {
            Id = dto.Id ?? Guid.Empty,
            StrategyId = dto.StrategyId,
            Label = dto.Label,
            Number = dto.Number,
            Title = dto.Title,
            OperationalGoals = dto.OperationalGoals.Select(_operationalGoalMapper.ToEntity).ToList()
        };
    }
}
