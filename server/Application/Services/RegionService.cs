using Application.Dtos;
using Application.Dtos.Mappers;
using Application.Services.IServices;
using Domain.Data;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public class RegionService : CrudService<Region, RegionDto>, ICrudService<RegionDto>
{
    public RegionService(
        ApplicationDbContext dbContext,
        IMapper<Region, RegionDto> mapper)
        : base(dbContext, mapper)
    {
    }

    public new async Task<List<RegionDto>> GetAllAsync()
    {
        var entities = await _dbContext.Regions
            .Include(x => x.Strategies)
                .ThenInclude(x => x.KeywordMetrics)
            .ToListAsync();
        return entities.Select(_mapper.ToDto).ToList();
    }
}
