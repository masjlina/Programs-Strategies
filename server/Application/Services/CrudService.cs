using Application.Dtos;
using Application.Dtos.Mappers;
using Application.Services.IServices;
using Domain.Data;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public class CrudService<TEntity, TDto> : ICrudService<TDto>
    where TEntity : BaseEntity
    where TDto : BaseDto
{
    protected readonly ApplicationDbContext _dbContext;
    protected readonly IMapper<TEntity, TDto> _mapper;

    public CrudService(
        ApplicationDbContext dbContext,
        IMapper<TEntity, TDto> mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<List<TDto>> GetAllAsync()
    {
        var entities = await _dbContext.Set<TEntity>().ToListAsync();
        return entities.Select(_mapper.ToDto).ToList();
    }

    public async Task<TDto> CreateAsync(TDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var entity = _mapper.ToEntity(dto);
        _dbContext.Set<TEntity>().Add(entity);
        await _dbContext.SaveChangesAsync();

        return _mapper.ToDto(entity);
    }

    public async Task<TDto> UpdateAsync(TDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var id = dto.Id ?? throw new ArgumentException("Entity id is required", nameof(dto));
        var entity = await _dbContext.Set<TEntity>().SingleOrDefaultAsync(x => x.Id == id);

        if (entity is null)
            throw new Exception("The given object does not exist");

        var updatedEntity = _mapper.ToEntity(dto);
        _dbContext.Entry(entity).CurrentValues.SetValues(updatedEntity);
        await _dbContext.SaveChangesAsync();

        return _mapper.ToDto(entity);
    }

    public async Task RemoveAsync(Guid id)
    {
        var entity = await _dbContext.Set<TEntity>().SingleOrDefaultAsync(x => x.Id == id);

        if (entity is null)
            throw new Exception("The given object does not exist");

        _dbContext.Set<TEntity>().Remove(entity);
        await _dbContext.SaveChangesAsync();
    }
}
