using Infrastructure.Entities;

namespace ApplicationCore.Dtos.Mappers;

public class OperationalGoalMapper : IMapper<OperationalGoal, OperationalGoalDto>
{
    private readonly IMapper<ProgramTask, ProgramTaskDto> _programTaskMapper;

    public OperationalGoalMapper(IMapper<ProgramTask, ProgramTaskDto> programTaskMapper)
    {
        _programTaskMapper = programTaskMapper;
    }

    public OperationalGoalDto ToDto(OperationalGoal entity)
    {
        return new OperationalGoalDto
        {
            Id = entity.Id,
            StrategicGoalId = entity.StrategicGoalId,
            Label = entity.Label,
            Number = entity.Number,
            Title = entity.Title,
            ProgramTasks = entity.ProgramTasks.Select(_programTaskMapper.ToDto).ToList()
        };
    }

    public OperationalGoal ToEntity(OperationalGoalDto dto)
    {
        return new OperationalGoal
        {
            Id = dto.Id ?? Guid.Empty,
            StrategicGoalId = dto.StrategicGoalId,
            Label = dto.Label,
            Number = dto.Number,
            Title = dto.Title,
            ProgramTasks = dto.ProgramTasks.Select(_programTaskMapper.ToEntity).ToList()
        };
    }
}
