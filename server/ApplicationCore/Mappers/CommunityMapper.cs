using Infrastructure.Entities;

namespace ApplicationCore.Dtos.Mappers;

public class CommunityMapper : IMapper<Community, CommunityDto>
{
    private readonly IMapper<Settlement, SettlementDto> _settlementMapper;
    private readonly IMapper<Strategy, StrategyDto> _strategyMapper;

    public CommunityMapper(
        IMapper<Settlement, SettlementDto> settlementMapper,
        IMapper<Strategy, StrategyDto> strategyMapper)
    {
        _settlementMapper = settlementMapper;
        _strategyMapper = strategyMapper;
    }

    public CommunityDto ToDto(Community entity)
    {
        return new CommunityDto
        {
            Id = entity.Id,
            KattotgId = entity.KattotgId,
            RegionId = entity.RegionId,
            DistrictId = entity.DistrictId,
            Name = entity.Name,
            NameFull = entity.NameFull,
            Category = entity.Category,
            ImgUrl = entity.ImgUrl,
            WebsiteUrl = entity.WebsiteUrl,
            StrategiesUrl = entity.StrategiesUrl,
            Settlements = entity.Settlements.Select(_settlementMapper.ToDto).ToList(),
            Strategies = entity.Strategies.Select(_strategyMapper.ToDto).ToList()
        };
    }

    public Community ToEntity(CommunityDto dto)
    {
        return new Community
        {
            Id = dto.Id ?? Guid.Empty,
            KattotgId = dto.KattotgId,
            RegionId = dto.RegionId,
            DistrictId = dto.DistrictId,
            Name = dto.Name,
            NameFull = dto.NameFull,
            Category = dto.Category,
            ImgUrl = dto.ImgUrl,
            WebsiteUrl = dto.WebsiteUrl,
            StrategiesUrl = dto.StrategiesUrl
        };
    }
}
