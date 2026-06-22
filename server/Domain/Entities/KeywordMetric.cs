using System;

namespace Domain.Entities;

public class KeywordMetric : BaseEntity
{
    public Guid StrategyId { get; set; }
    public Strategy? Strategy { get; set; }
    public required string Keyword { get; set; }
    public int Count { get; set; }
}
