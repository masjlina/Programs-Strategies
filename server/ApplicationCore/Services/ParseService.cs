using System.Text.Json;
using ApplicationCore.Dtos;
using ApplicationCore.Services.IServices;
using Infrastructure.Data;
using Infrastructure.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shared;

namespace ApplicationCore.Services;

public class ParseService : IParseService
{
    private readonly ApplicationDbContext _dbContext;

    public ParseService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<AdministrativeUnitDto> Parse<T>(T file)
    {
        if (file is AdministrativeUnitDto dto)
            return dto;

        if (file is IFormFile fileFile)
        {
            var extension = Path.GetExtension(fileFile.FileName);

            if (!string.Equals(AllowedExtension.Json, extension, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Only .json files are supported");

            try
            {
                using var stream = fileFile.OpenReadStream();

                var parsedJson = await JsonSerializer.DeserializeAsync<AdministrativeUnitDto>(stream, JsonOptions);

                if (parsedJson is null)
                    throw new InvalidDataException("Uploaded JSON is empty or invalid");

                return parsedJson;
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException(
                    "Invalid JSON payload for AdministrativeUnit.",
                    ex);
            }
        }

        throw new InvalidDataException("File type not supported");
    }

    public async Task Save(AdministrativeUnitDto unitDto)
    {
        var isExist = await _dbContext.AdministrativeUnits
            .AnyAsync(x => x.Name == unitDto.Name);

        if (isExist)
            throw new Exception("The administrative unit already exists");

        var entity = new AdministrativeUnit
        {
            Name = unitDto.Name,
            Type = unitDto.Type,

            Strategies = unitDto.Strategies
                .Select(strategyDto => new Strategy
                {
                    Title = strategyDto.Title,

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

        await _dbContext.AdministrativeUnits.AddAsync(entity);

        await _dbContext.SaveChangesAsync();
    }
}