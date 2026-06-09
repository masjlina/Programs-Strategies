using ApplicationCore.Dtos;
using ApplicationCore.Dtos.Mappers;
using ApplicationCore.Services.IServices;
using Infrastructure.Data;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApplicationCore.Services;

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
            .ToListAsync();
        return entities.Select(_mapper.ToDto).ToList();
    }
}
