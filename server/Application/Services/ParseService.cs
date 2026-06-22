using System.Text.Json;
using Application.Dtos;
using Application.Services.IServices;
using Domain.Data;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shared;

namespace Application.Services;

public class ParseService : IParseService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly TextProcessingPipeline _textPipeline;
    private readonly IStrategyImportService _importService;

    public ParseService(
        ApplicationDbContext dbContext,
        TextProcessingPipeline textPipeline,
        IStrategyImportService importService)
    {
        _dbContext = dbContext;
        _textPipeline = textPipeline;
        _importService = importService;
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<CommunityDto> Parse<T>(T file)
    {
        if (file is CommunityDto dto)
            return dto;

        if (file is IFormFile fileFile)
        {
            var extension = Path.GetExtension(fileFile.FileName);

            if (!string.Equals(AllowedExtension.Json, extension, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Only .json files are supported");

            try
            {
                using var stream = fileFile.OpenReadStream();

                var parsedJson = await JsonSerializer.DeserializeAsync<CommunityDto>(stream, JsonOptions);

                if (parsedJson is null)
                    throw new InvalidDataException("Uploaded JSON is empty or invalid");

                return parsedJson;
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException(
                    "Invalid JSON payload for Community.",
                    ex);
            }
        }

        throw new InvalidDataException("File type not supported");
    }

    public async Task<StrategyDto> ParseDocument(IFormFile file)
    {
        var parser = new DocumentParser();
        return await parser.ParseFormFileAsync(file);
    }

    public async Task Save(CommunityDto communityDto)
    {
        var isExist = await _dbContext.Communities
            .AnyAsync(x => x.KattotgId == communityDto.KattotgId);

        if (isExist)
            throw new Exception("The community already exists");

        var entity = new Community
        {
            Id = communityDto.Id ?? Guid.Empty,
            KattotgId = communityDto.KattotgId,
            RegionId = communityDto.RegionId,
            DistrictId = communityDto.DistrictId,
            Name = communityDto.Name,
            NameFull = communityDto.NameFull,
            Category = communityDto.Category,
            ImgUrl = communityDto.ImgUrl,
            WebsiteUrl = communityDto.WebsiteUrl,
            StrategiesUrl = communityDto.StrategiesUrl,

            Strategies = communityDto.Strategies
                .Select(strategyDto => new Strategy
                {
                    Id = strategyDto.Id ?? Guid.Empty,
                    RegionId = strategyDto.RegionId,
                    DistrictId = strategyDto.DistrictId,
                    CommunityId = strategyDto.CommunityId,
                    Title = strategyDto.Title,
                    StrategyUrl = strategyDto.StrategyUrl,

                    StrategicGoals = strategyDto.StrategicGoals
                        .Select(goalDto => new StrategicGoal
                        {
                            Label = goalDto.Label,
                            Number = goalDto.Number,
                            Title = goalDto.Title,

                            OperationalGoals = goalDto.OperationalGoals
                                .Select(opDto => new OperationalGoal
                                {
                                    Label = opDto.Label,
                                    Number = opDto.Number,
                                    Title = opDto.Title,

                                    ProgramTasks = opDto.ProgramTasks
                                        .Select(taskDto => new ProgramTask
                                        {
                                            Label = taskDto.Label,
                                            Number = taskDto.Number,
                                            Description = taskDto.Description
                                        })
                                        .ToList()
                                })
                                .ToList()
                        })
                        .ToList()
                })
                .ToList()
        };

        await _dbContext.Communities.AddAsync(entity);

        await _dbContext.SaveChangesAsync();

        foreach (var strategy in entity.Strategies)
        {
            var wordCounts = await _textPipeline.ProcessStrategyTextAsync(strategy.Id);
            await _importService.SaveStrategyMetricsAsync(strategy.Id, wordCounts);
        }
    }
}
