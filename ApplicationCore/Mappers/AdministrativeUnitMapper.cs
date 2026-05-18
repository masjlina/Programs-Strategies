using Infrastructure.Entities;

namespace ApplicationCore.Dtos.Mappers;

public class AdministrativeUnitMapper : IMapper<AdministrativeUnit, AdministrativeUnitDto>
{
    private readonly IMapper<Strategy, StrategyDto> _strategyMapper;

    public AdministrativeUnitMapper(IMapper<Strategy, StrategyDto> strategyMapper)
    {
        _strategyMapper = strategyMapper;
    }

    public AdministrativeUnitDto ToDto(AdministrativeUnit entity)
    {
        return new AdministrativeUnitDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Type = entity.Type,
            Strategies = entity.Strategies.Select(_strategyMapper.ToDto).ToList()
        };
    }

    public AdministrativeUnit ToEntity(AdministrativeUnitDto dto)
    {
        return new AdministrativeUnit
        {
            Id = dto.Id ?? Guid.Empty,
            Name = dto.Name,
            Type = dto.Type
        };
    }
}
