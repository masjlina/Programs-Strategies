using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Application.Dtos;
using Application.Services.IServices;
using Domain.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebAPI.Controllers;

/// <summary>
/// Handles analytics endpoints for program strategies.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AnalyticsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILemmatizer _lemmatizer;

    public AnalyticsController(ApplicationDbContext dbContext, ILemmatizer lemmatizer)
    {
        _dbContext = dbContext;
        _lemmatizer = lemmatizer;
    }

    /// <summary>
    /// Calculates the intensity metrics for a specific keyword across all strategies.
    /// </summary>
    /// <param name="keyword">The target keyword to analyze.</param>
    /// <response code="200">Returns statistics and individual strategy deviations.</response>
    [HttpGet("intensity")]
    public async Task<ActionResult<KeywordIntensityDto>> GetIntensity([FromQuery] string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return BadRequest("Keyword parameter is required.");
        }

        // Clean, lowercase, and lemmatize keyword to match the database metrics schema
        keyword = keyword.Trim().ToLowerInvariant();
        string lemmatizedKeyword = _lemmatizer.Lemmatize(keyword);

        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        using var command = connection.CreateCommand();
        command.CommandText = @"
            WITH strategy_counts AS (
                SELECT 
                    s.""Id"" AS strategy_id,
                    s.""Title"" AS strategy_title,
                    COALESCE(r.""NameFull"", d.""NameFull"", c.""NameFull"", '') AS target_name,
                    CASE 
                        WHEN s.""RegionId"" IS NOT NULL THEN 'Region'
                        WHEN s.""DistrictId"" IS NOT NULL THEN 'District'
                        WHEN s.""CommunityId"" IS NOT NULL THEN 'Community'
                        ELSE 'Unknown'
                    END AS level,
                    COALESCE(km.count, 0) AS count
                FROM ""Strategies"" s
                LEFT JOIN ""Regions"" r ON s.""RegionId"" = r.""Id""
                LEFT JOIN ""Districts"" d ON s.""DistrictId"" = d.""Id""
                LEFT JOIN ""Communities"" c ON s.""CommunityId"" = c.""Id""
                LEFT JOIN ""KeywordMetrics"" km 
                    ON s.""Id"" = km.strategy_id AND km.keyword = @Keyword
            ),
            stats AS (
                SELECT 
                    AVG(count) AS global_mean,
                    VAR_POP(count) AS variance
                FROM strategy_counts
            )
            SELECT 
                sc.strategy_id,
                sc.strategy_title,
                sc.target_name,
                sc.level,
                sc.count,
                (sc.count - COALESCE(st.global_mean, 0))::double precision AS deviation,
                COALESCE(st.global_mean, 0)::double precision AS global_mean,
                COALESCE(st.variance, 0)::double precision AS variance
            FROM strategy_counts sc
            CROSS JOIN stats st
            ORDER BY sc.count DESC;";

        var param = command.CreateParameter();
        param.ParameterName = "@Keyword";
        param.Value = lemmatizedKeyword;
        command.Parameters.Add(param);

        var results = new List<KeywordQueryResult>();
        using (var reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                results.Add(new KeywordQueryResult
                {
                    StrategyId = reader.GetGuid(0),
                    StrategyTitle = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                    TargetName = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    Level = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                    Count = reader.GetInt32(4),
                    Deviation = reader.GetDouble(5),
                    GlobalMean = reader.GetDouble(6),
                    Variance = reader.GetDouble(7)
                });
            }
        }

        var items = new List<KeywordIntensityItemDto>();
        double globalMean = 0;
        double variance = 0;

        if (results.Count > 0)
        {
            globalMean = results[0].GlobalMean;
            variance = results[0].Variance;

            foreach (var res in results)
            {
                string targetName = res.TargetName;
                if (res.Level == "Community" && targetName.EndsWith(" територіальна громада"))
                {
                    targetName = targetName.Replace(" територіальна громада", " ТГ");
                }
                else if (res.Level == "Community" && targetName.EndsWith(" міська територіальна громада"))
                {
                    targetName = targetName.Replace(" міська територіальна громада", " ТГ");
                }

                items.Add(new KeywordIntensityItemDto
                {
                    StrategyId = res.StrategyId,
                    StrategyTitle = res.StrategyTitle,
                    TargetName = targetName,
                    Level = res.Level,
                    Count = res.Count,
                    Deviation = Math.Round(res.Deviation, 2)
                });
            }
        }

        var response = new KeywordIntensityDto
        {
            Keyword = lemmatizedKeyword,
            Statistics = new KeywordStatisticsDto
            {
                GlobalMean = Math.Round(globalMean, 2),
                Variance = Math.Round(variance, 2)
            },
            Items = items
        };

        return Ok(response);
    }

    private class KeywordQueryResult
    {
        public Guid StrategyId { get; set; }
        public required string StrategyTitle { get; set; }
        public required string TargetName { get; set; }
        public required string Level { get; set; }
        public int Count { get; set; }
        public double Deviation { get; set; }
        public double GlobalMean { get; set; }
        public double Variance { get; set; }
    }
}
