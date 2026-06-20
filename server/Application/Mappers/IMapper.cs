namespace Application.Dtos.Mappers;

public interface IMapper<TEntity, TDto>
{
    TDto ToDto(TEntity entity);
    TEntity ToEntity(TDto dto);
}
