using System.Text.Json.Serialization;
using Shared;

namespace Infrastructure.Entities;

public class AdministrativeUnit : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AdministrativeUnitType Type { get; set; }

    public List<Strategy> Strategies { get; set; } = new List<Strategy>();
}