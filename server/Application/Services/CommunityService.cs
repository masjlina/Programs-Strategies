using Application.Dtos;
using Application.Dtos.Mappers;
using Application.Services.IServices;
using Domain.Data;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

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
                .ThenInclude(x => x.KeywordMetrics)
            .ToListAsync();
        return entities.Select(_mapper.ToDto).ToList();
    }
}
