using Domain.Entities;

namespace Application.Dtos.Mappers;

public class SettlementMapper : IMapper<Settlement, SettlementDto>
{
    public SettlementDto ToDto(Settlement entity)
    {
        return new SettlementDto
        {
            Id = entity.Id,
            KattotgId = entity.KattotgId,
            RegionId = entity.RegionId,
            CommunityId = entity.CommunityId,
            DistrictId = entity.DistrictId,
            ParentSettlementId = entity.ParentSettlementId,
            Name = entity.Name,
            NameFull = entity.NameFull,
            ParentType = entity.ParentType,
            ChildSettlements = entity.ChildSettlements.Select(ToDto).ToList()
        };
    }

    public Settlement ToEntity(SettlementDto dto)
    {
        return new Settlement
        {
            Id = dto.Id ?? Guid.Empty,
            KattotgId = dto.KattotgId,
            RegionId = dto.RegionId,
            CommunityId = dto.CommunityId,
            DistrictId = dto.DistrictId,
            ParentSettlementId = dto.ParentSettlementId,
            Name = dto.Name,
            NameFull = dto.NameFull,
            ParentType = dto.ParentType
        };
    }
}
