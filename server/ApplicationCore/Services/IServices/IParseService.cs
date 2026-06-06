using ApplicationCore.Dtos;

namespace ApplicationCore.Services.IServices;

public interface IParseService
{
    Task<CommunityDto> Parse<T>(T file);
    Task Save(CommunityDto communityDto);
}
