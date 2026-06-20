using Application.Dtos;
using Microsoft.AspNetCore.Http;

namespace Application.Services.IServices;

public interface IParseService
{
    Task<CommunityDto> Parse<T>(T file);
    Task<StrategyDto> ParseDocument(IFormFile file);
    Task Save(CommunityDto communityDto);
}

