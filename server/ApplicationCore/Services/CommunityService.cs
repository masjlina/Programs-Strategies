using ApplicationCore.Dtos;
using ApplicationCore.Dtos.Mappers;
using Infrastructure.Data;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApplicationCore.Services;

public class CommunityService : CrudService<Community, CommunityDto>
{
    public CommunityService(
        ApplicationDbContext dbContext,
        IMapper<Community, CommunityDto> mapper)
        : base(dbContext, mapper)
    {
    }

    public new async Task<List<CommunityDto>> GetAllAsync()
    {
        var entities = await _dbContext.Communities
            .Include(x => x.Strategies)
            .Include(x => x.Settlements)
            .ToListAsync();
        return entities.Select(_mapper.ToDto).ToList();
    }
}
