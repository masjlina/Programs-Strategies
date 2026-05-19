using ApplicationCore.Dtos;
using Infrastructure.Entities;

namespace ApplicationCore.Services.IServices;

public interface IParseService
{
    Task<AdministrativeUnitDto> Parse<T>(T file);
    Task Save(AdministrativeUnitDto unitDto);
}