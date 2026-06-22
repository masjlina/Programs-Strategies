using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Application.Services.IServices;
using Domain.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;

namespace Application.Services;

public class StrategyImportService : IStrategyImportService
{
    private readonly ApplicationDbContext _dbContext;

    public StrategyImportService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SaveStrategyMetricsAsync(Guid strategyId, Dictionary<string, int> wordCounts)
    {
        if (wordCounts == null || wordCounts.Count == 0)
        {
            return;
        }

        // 1. Begin isolated transaction
        using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            // 2. Clear old keyword metrics for the target strategy
            await _dbContext.Database.ExecuteSqlRawAsync(
                "DELETE FROM \"KeywordMetrics\" WHERE strategy_id = {0}", strategyId);

            // 3. Obtain the raw connection
            var dbConnection = _dbContext.Database.GetDbConnection();
            NpgsqlConnection? npgsqlConnection = dbConnection as NpgsqlConnection;
            
            if (npgsqlConnection == null)
            {
                // Fallback in case of wrapped connections (e.g. interception or profiling libraries)
                var underlyingProp = dbConnection.GetType().GetProperty("UnderlyingConnection");
                if (underlyingProp != null)
                {
                    npgsqlConnection = underlyingProp.GetValue(dbConnection) as NpgsqlConnection;
                }
            }

            if (npgsqlConnection == null)
            {
                throw new InvalidOperationException("Could not retrieve NpgsqlConnection from the DbContext.");
            }

            if (npgsqlConnection.State != ConnectionState.Open)
            {
                await npgsqlConnection.OpenAsync();
            }

            // 4. Perform optimized bulk binary COPY import
            using (var writer = await npgsqlConnection.BeginBinaryImportAsync(
                "COPY \"KeywordMetrics\" (\"Id\", strategy_id, keyword, count) FROM STDIN (FORMAT BINARY)"))
            {
                foreach (var kvp in wordCounts)
                {
                    await writer.StartRowAsync();
                    await writer.WriteAsync(Guid.NewGuid(), NpgsqlDbType.Uuid);
                    await writer.WriteAsync(strategyId, NpgsqlDbType.Uuid);
                    await writer.WriteAsync(kvp.Key, NpgsqlDbType.Text);
                    await writer.WriteAsync(kvp.Value, NpgsqlDbType.Integer);
                }
                await writer.CompleteAsync();
            }

            // 5. Commit transaction
            await transaction.CommitAsync();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
