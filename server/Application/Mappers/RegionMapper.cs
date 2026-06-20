using Domain.Entities;

namespace Application.Dtos.Mappers;

public class RegionMapper : IMapper<Region, RegionDto>
{
    private readonly IMapper<District, DistrictDto> _districtMapper;
    private readonly IMapper<Community, CommunityDto> _communityMapper;
    private readonly IMapper<Settlement, SettlementDto> _settlementMapper;
    private readonly IMapper<Strategy, StrategyDto> _strategyMapper;

    public RegionMapper(
        IMapper<District, DistrictDto> districtMapper,
        IMapper<Community, CommunityDto> communityMapper,
        IMapper<Settlement, SettlementDto> settlementMapper,
        IMapper<Strategy, StrategyDto> strategyMapper)
    {
        _districtMapper = districtMapper;
        _communityMapper = communityMapper;
        _settlementMapper = settlementMapper;
        _strategyMapper = strategyMapper;
    }

    public RegionDto ToDto(Region entity)
    {
        return new RegionDto
        {
            Id = entity.Id,
            KattotgId = entity.KattotgId,
            Name = entity.Name,
            NameFull = entity.NameFull,
            Category = entity.Category,
            ImgUrl = entity.ImgUrl,
            WebsiteUrl = entity.WebsiteUrl,
            StrategiesUrl = entity.StrategiesUrl,
            Districts = entity.Districts.Select(_districtMapper.ToDto).ToList(),
            Communities = entity.Communities.Select(_communityMapper.ToDto).ToList(),
            Settlements = entity.Settlements.Select(_settlementMapper.ToDto).ToList(),
            Strategies = entity.Strategies.Select(_strategyMapper.ToDto).ToList()
        };
    }

    public Region ToEntity(RegionDto dto)
    {
        return new Region
        {
            Id = dto.Id ?? Guid.Empty,
            KattotgId = dto.KattotgId,
            Name = dto.Name,
            NameFull = dto.NameFull,
            Category = dto.Category,
            ImgUrl = dto.ImgUrl,
            WebsiteUrl = dto.WebsiteUrl,
            StrategiesUrl = dto.StrategiesUrl
        };
    }
}
