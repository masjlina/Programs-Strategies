using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Services.IServices;

public interface IStrategyImportService
{
    Task SaveStrategyMetricsAsync(Guid strategyId, Dictionary<string, int> wordCounts);
}
