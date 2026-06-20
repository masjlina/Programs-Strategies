using Domain.Entities;

namespace Application.Dtos.Mappers;

public class ProgramTaskMapper : IMapper<ProgramTask, ProgramTaskDto>
{
    public ProgramTaskDto ToDto(ProgramTask entity)
    {
        return new ProgramTaskDto
        {
            Id = entity.Id,
            OperationalGoalId = entity.OperationalGoalId,
            Label = entity.Label,
            Number = entity.Number,
            Description = entity.Description
        };
    }

    public ProgramTask ToEntity(ProgramTaskDto dto)
    {
        return new ProgramTask
        {
            Id = dto.Id ?? Guid.Empty,
            OperationalGoalId = dto.OperationalGoalId,
            Label = dto.Label,
            Number = dto.Number,
            Description = dto.Description
        };
    }
}
