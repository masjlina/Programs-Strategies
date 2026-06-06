using Infrastructure.Entities;

namespace ApplicationCore.Dtos.Mappers;

public class DistrictMapper : IMapper<District, DistrictDto>
{
    private readonly IMapper<Community, CommunityDto> _communityMapper;
    private readonly IMapper<Settlement, SettlementDto> _settlementMapper;
    private readonly IMapper<Strategy, StrategyDto> _strategyMapper;

    public DistrictMapper(
        IMapper<Community, CommunityDto> communityMapper,
        IMapper<Settlement, SettlementDto> settlementMapper,
        IMapper<Strategy, StrategyDto> strategyMapper)
    {
        _communityMapper = communityMapper;
        _settlementMapper = settlementMapper;
        _strategyMapper = strategyMapper;
    }

    public DistrictDto ToDto(District entity)
    {
        return new DistrictDto
        {
            Id = entity.Id,
            KattotgId = entity.KattotgId,
            RegionId = entity.RegionId,
            Name = entity.Name,
            NameFull = entity.NameFull,
            Category = entity.Category,
            ImgUrl = entity.ImgUrl,
            Communities = entity.Communities.Select(_communityMapper.ToDto).ToList(),
            Settlements = entity.Settlements.Select(_settlementMapper.ToDto).ToList(),
            Strategies = entity.Strategies.Select(_strategyMapper.ToDto).ToList()
        };
    }

    public District ToEntity(DistrictDto dto)
    {
        return new District
        {
            Id = dto.Id ?? Guid.Empty,
            KattotgId = dto.KattotgId,
            RegionId = dto.RegionId,
            Name = dto.Name,
            NameFull = dto.NameFull,
            Category = dto.Category,
            ImgUrl = dto.ImgUrl
        };
    }
}
