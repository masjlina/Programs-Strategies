using ApplicationCore.Dtos;
using Microsoft.AspNetCore.Http;

namespace ApplicationCore.Services.IServices;

public interface IParseService
{
    Task<CommunityDto> Parse<T>(T file);
    Task<StrategyDto> ParseDocument(IFormFile file);
    Task Save(CommunityDto communityDto);
}

