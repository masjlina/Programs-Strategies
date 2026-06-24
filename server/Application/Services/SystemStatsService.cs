using Application.Dtos;
using Application.Services.IServices;
using Domain.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public class SystemStatsService : ISystemStatsService
{
    private readonly ApplicationDbContext _dbContext;

    public SystemStatsService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SystemStatsDto> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        var regionsCount = await _dbContext.Regions.CountAsync(cancellationToken);
        var communitiesCount = await _dbContext.Communities.CountAsync(cancellationToken);
        var totalStrategiesCount = await _dbContext.Strategies.CountAsync(cancellationToken);

        var communitiesWithStrategiesCount = await _dbContext.Communities
            .CountAsync(c => c.Strategies.Any(), cancellationToken);

        var communitiesWithStrategiesPercent = communitiesCount == 0
            ? 0
            : Math.Round((double)communitiesWithStrategiesCount / communitiesCount * 100, 1);

        var averageStrategiesPerCommunity = communitiesCount == 0
            ? 0
            : Math.Round((double)totalStrategiesCount / communitiesCount, 2);

        var communitiesWithWebsiteCount = await _dbContext.Communities
            .CountAsync(
                c => c.WebsiteUrl != null && c.WebsiteUrl.Trim() != "",
                cancellationToken);

        var communitiesWithWebsitePercent = communitiesCount == 0
            ? 0
            : Math.Round((double)communitiesWithWebsiteCount / communitiesCount * 100, 1);

        var monthStart = DateTime.UtcNow.Date.AddDays(-29);
        var strategiesLastMonthByDay = await _dbContext.Strategies
            .Where(s => s.CreatedAt >= monthStart)
            .GroupBy(s => s.CreatedAt.Date)
            .Select(g => new DailyStrategyCountDto
            {
                Date = g.Key.ToString("yyyy-MM-dd"),
                Count = g.Count()
            })
            .ToListAsync(cancellationToken);

        var dailyCounts = new List<DailyStrategyCountDto>();
        for (var day = monthStart; day <= DateTime.UtcNow.Date; day = day.AddDays(1))
        {
            var dateKey = day.ToString("yyyy-MM-dd");
            var existing = strategiesLastMonthByDay.FirstOrDefault(x => x.Date == dateKey);
            dailyCounts.Add(new DailyStrategyCountDto
            {
                Date = dateKey,
                Count = existing?.Count ?? 0
            });
        }

        return new SystemStatsDto
        {
            RegionsCount = regionsCount,
            CommunitiesCount = communitiesCount,
            TotalStrategiesCount = totalStrategiesCount,
            CommunitiesWithStrategiesCount = communitiesWithStrategiesCount,
            CommunitiesWithStrategiesPercent = communitiesWithStrategiesPercent,
            AverageStrategiesPerCommunity = averageStrategiesPerCommunity,
            CommunitiesWithWebsiteCount = communitiesWithWebsiteCount,
            CommunitiesWithWebsitePercent = communitiesWithWebsitePercent,
            StrategiesLastMonthByDay = dailyCounts
        };
    }
}
