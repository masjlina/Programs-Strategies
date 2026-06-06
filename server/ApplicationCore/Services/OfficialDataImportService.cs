using ApplicationCore.Dtos;
using ApplicationCore.Services.IServices;
using Infrastructure.Data;
using Infrastructure.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ApplicationCore.Services;

public class OfficialDataImportService : IOfficialDataImportService
{
    private const string RegionCategory = "O";
    private const string CityRegionCategory = "K";
    private const string DistrictCategory = "P";
    private const string CommunityCategory = "H";

    private static readonly string[] ExpectedHeader =
    [
        "level_1",
        "level_2",
        "level_3",
        "level_4",
        "level_0",
        "category",
        "name",
        "website_url"
    ];

    private readonly ApplicationDbContext _dbContext;

    public OfficialDataImportService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<OfficialDataImportResultDto> ImportAsync(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (file.Length == 0)
            throw new InvalidDataException("Uploaded CSV is empty");

        var extension = Path.GetExtension(file.FileName);
        if (!string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Only .csv files are supported");

        await using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream);

        var rows = ReadRows(reader);
        if (rows.Count == 0)
            throw new InvalidDataException("Uploaded CSV has no data rows");

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var regionsByCode = await _dbContext.Regions
            .ToDictionaryAsync(x => x.KattotgId, cancellationToken);
        var districtsByCode = await _dbContext.Districts
            .ToDictionaryAsync(x => x.KattotgId, cancellationToken);
        var communitiesByCode = await _dbContext.Communities
            .ToDictionaryAsync(x => x.KattotgId, cancellationToken);
        var settlementsByCode = await _dbContext.Settlements
            .ToDictionaryAsync(x => x.KattotgId, cancellationToken);

        var result = new OfficialDataImportResultDto
        {
            RowsProcessed = rows.Count
        };

        ImportRegions(rows, regionsByCode, result);
        ImportDistricts(rows, regionsByCode, districtsByCode, result);
        ImportCommunities(rows, regionsByCode, districtsByCode, communitiesByCode, result);
        ImportSettlements(rows, regionsByCode, districtsByCode, communitiesByCode, settlementsByCode, result);

        result.RowsSkipped = rows.Count
            - result.RegionsCreated
            - result.DistrictsCreated
            - result.CommunitiesCreated
            - result.SettlementsCreated;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return result;
    }

    private static List<OfficialDataRow> ReadRows(TextReader reader)
    {
        var headerLine = reader.ReadLine();
        if (headerLine is null)
            return [];

        var header = ParseCsvLine(headerLine);
        if (!header.SequenceEqual(ExpectedHeader, StringComparer.OrdinalIgnoreCase))
            throw new InvalidDataException("Invalid official-data.csv header");

        var rows = new List<OfficialDataRow>();
        var lineNumber = 1;

        while (reader.ReadLine() is { } line)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var columns = ParseCsvLine(line);
            if (columns.Count != ExpectedHeader.Length)
                throw new InvalidDataException($"Invalid CSV column count at line {lineNumber}");

            rows.Add(new OfficialDataRow(
                LineNumber: lineNumber,
                Level1: Normalize(columns[0]),
                Level2: Normalize(columns[1]),
                Level3: Normalize(columns[2]),
                Level4: Normalize(columns[3]),
                Level0: Normalize(columns[4]),
                Category: Normalize(columns[5]),
                Name: Normalize(columns[6]),
                WebsiteUrl: Normalize(columns[7])));
        }

        return rows;
    }

    private void ImportRegions(
        List<OfficialDataRow> rows,
        Dictionary<string, Region> regionsByCode,
        OfficialDataImportResultDto result)
    {
        foreach (var row in rows.Where(IsRegionRow))
        {
            if (regionsByCode.ContainsKey(row.Level1))
                continue;

            var region = new Region
            {
                Id = Guid.NewGuid(),
                KattotgId = row.Level1,
                Name = row.Name,
                NameFull = BuildNameFull(row.Name, row.Category),
                Category = row.Category,
                WebsiteUrl = EmptyToNull(row.WebsiteUrl)
            };

            regionsByCode.Add(region.KattotgId, region);
            _dbContext.Regions.Add(region);
            result.RegionsCreated++;
        }
    }

    private void ImportDistricts(
        List<OfficialDataRow> rows,
        Dictionary<string, Region> regionsByCode,
        Dictionary<string, District> districtsByCode,
        OfficialDataImportResultDto result)
    {
        foreach (var row in rows.Where(x => x.Category == DistrictCategory))
        {
            if (districtsByCode.ContainsKey(row.Level2))
                continue;

            if (!regionsByCode.TryGetValue(row.Level1, out var region))
                throw MissingParent(row, "region", row.Level1);

            var district = new District
            {
                Id = Guid.NewGuid(),
                KattotgId = row.Level2,
                RegionId = region.Id,
                Name = row.Name,
                NameFull = BuildNameFull(row.Name, row.Category),
                Category = row.Category
            };

            districtsByCode.Add(district.KattotgId, district);
            _dbContext.Districts.Add(district);
            result.DistrictsCreated++;
        }
    }

    private void ImportCommunities(
        List<OfficialDataRow> rows,
        Dictionary<string, Region> regionsByCode,
        Dictionary<string, District> districtsByCode,
        Dictionary<string, Community> communitiesByCode,
        OfficialDataImportResultDto result)
    {
        foreach (var row in rows.Where(x => x.Category == CommunityCategory))
        {
            if (communitiesByCode.ContainsKey(row.Level3))
                continue;

            if (!regionsByCode.TryGetValue(row.Level1, out var region))
                throw MissingParent(row, "region", row.Level1);

            if (!districtsByCode.TryGetValue(row.Level2, out var district))
                throw MissingParent(row, "district", row.Level2);

            var community = new Community
            {
                Id = Guid.NewGuid(),
                KattotgId = row.Level3,
                RegionId = region.Id,
                DistrictId = district.Id,
                Name = row.Name,
                NameFull = BuildNameFull(row.Name, row.Category),
                Category = row.Category,
                WebsiteUrl = EmptyToNull(row.WebsiteUrl)
            };

            communitiesByCode.Add(community.KattotgId, community);
            _dbContext.Communities.Add(community);
            result.CommunitiesCreated++;
        }
    }

    private void ImportSettlements(
        List<OfficialDataRow> rows,
        Dictionary<string, Region> regionsByCode,
        Dictionary<string, District> districtsByCode,
        Dictionary<string, Community> communitiesByCode,
        Dictionary<string, Settlement> settlementsByCode,
        OfficialDataImportResultDto result)
    {
        foreach (var row in rows.Where(IsSettlementRow))
        {
            var kattotgId = row.Level0.Length > 0 ? row.Level0 : row.Level4;
            if (settlementsByCode.ContainsKey(kattotgId))
                continue;

            if (!regionsByCode.TryGetValue(row.Level1, out var region))
                throw MissingParent(row, "region", row.Level1);

            districtsByCode.TryGetValue(row.Level2, out var district);
            communitiesByCode.TryGetValue(row.Level3, out var community);

            var parentSettlementId = (Guid?)null;
            if (row.Level0.Length > 0 && settlementsByCode.TryGetValue(row.Level4, out var parentSettlement))
                parentSettlementId = parentSettlement.Id;

            var settlement = new Settlement
            {
                Id = Guid.NewGuid(),
                KattotgId = kattotgId,
                RegionId = region.Id,
                DistrictId = district?.Id,
                CommunityId = community?.Id,
                ParentSettlementId = parentSettlementId,
                Name = row.Name,
                NameFull = BuildNameFull(row.Name, row.Category),
                ParentType = row.Category
            };

            settlementsByCode.Add(settlement.KattotgId, settlement);
            _dbContext.Settlements.Add(settlement);
            result.SettlementsCreated++;
        }
    }

    private static bool IsRegionRow(OfficialDataRow row)
    {
        return (row.Category == RegionCategory || row.Category == CityRegionCategory)
            && row.Level1.Length > 0
            && row.Level2.Length == 0
            && row.Level3.Length == 0
            && row.Level4.Length == 0
            && row.Level0.Length == 0;
    }

    private static bool IsSettlementRow(OfficialDataRow row)
    {
        return row.Category is not RegionCategory
            and not CityRegionCategory
            and not DistrictCategory
            and not CommunityCategory;
    }

    private static InvalidDataException MissingParent(OfficialDataRow row, string parentType, string parentCode)
    {
        return new InvalidDataException(
            $"Missing {parentType} '{parentCode}' for row {row.LineNumber} ({row.Category}, {row.Name})");
    }

    private static string Normalize(string value)
    {
        return value.Trim();
    }

    private static string BuildNameFull(string name, string category)
    {
        name = name.Trim();
        return category switch
        {
            "O" => name == "Автономна Республіка Крим" ? name : $"{name} область",
            "P" => $"{name} район",
            "H" => $"{name} територіальна громада",
            "C" => $"село {name}",
            "X" => $"селище {name}",
            "M" => $"місто {name}",
            "B" => $"{name} район міста",
            "K" => $"{name} місто зі спецстатусом",
            _ => name
        };
    }

    private static string? EmptyToNull(string value)
    {
        return value.Length == 0 ? null : value;
    }

    private static List<string> ParseCsvLine(string line)
    {
        var values = new List<string>();
        var current = new StringWriter();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var currentChar = line[i];

            if (currentChar == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Write('"');
                    i++;
                    continue;
                }

                inQuotes = !inQuotes;
                continue;
            }

            if (currentChar == ',' && !inQuotes)
            {
                values.Add(current.ToString());
                current.GetStringBuilder().Clear();
                continue;
            }

            current.Write(currentChar);
        }

        if (inQuotes)
            throw new InvalidDataException("Invalid CSV line: missing closing quote");

        values.Add(current.ToString());

        return values;
    }

    private sealed record OfficialDataRow(
        int LineNumber,
        string Level1,
        string Level2,
        string Level3,
        string Level4,
        string Level0,
        string Category,
        string Name,
        string WebsiteUrl);
}
