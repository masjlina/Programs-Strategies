using ApplicationCore.Dtos;
using ApplicationCore.Dtos.Mappers;
using ApplicationCore.Services.IServices;
using Infrastructure.Data;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApplicationCore.Services;

public class CommunityService : CrudService<Community, CommunityDto>, ICrudService<CommunityDto>
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
            .ToListAsync();
        return entities.Select(_mapper.ToDto).ToList();
    }
}
