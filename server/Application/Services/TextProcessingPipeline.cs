using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Linq;
using Application.Services.IServices;
using Domain.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public class TextProcessingPipeline
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILemmatizer _lemmatizer;

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        // Required stop words
        "і", "на", "та", "під", "для", "що", "це", "у", "в", "до", "з",
        // Extended common Ukrainian stop words
        "а", "але", "бо", "як", "про", "за", "від", "по", "через", "при",
        "проти", "перед", "над", "із", "зі", "чи", "або", "не", "ні", "так",
        "це", "цей", "ця", "це", "ці", "ми", "ви", "вони", "він", "вона", "воно"
    };

    // Compiled regexes for efficiency and thread safety
    private static readonly Regex HierarchicalNumberRegex = new(@"\b\d+(\.\d+)+\b", RegexOptions.Compiled);
    private static readonly Regex DigitRegex = new(@"\d+", RegexOptions.Compiled);
    private static readonly Regex LetterWordRegex = new(@"\p{L}+", RegexOptions.Compiled);

    public TextProcessingPipeline(ApplicationDbContext dbContext, ILemmatizer lemmatizer)
    {
        _dbContext = dbContext;
        _lemmatizer = lemmatizer;
    }

    /// <summary>
    /// Processes all program task descriptions of a strategy, sanitizing, stemming, and calculating word frequencies.
    /// </summary>
    public async Task<Dictionary<string, int>> ProcessStrategyTextAsync(Guid strategyId)
    {
        // 1. Extraction & Aggregation
        var tasks = await _dbContext.ProgramTasks
            .Where(t => t.OperationalGoal!.StrategicGoal!.StrategyId == strategyId)
            .ToListAsync();

        if (tasks.Count == 0)
        {
            return new Dictionary<string, int>();
        }

        var sb = new StringBuilder();
        foreach (var task in tasks)
        {
            sb.AppendLine(task.Description);
        }
        string combinedText = sb.ToString();

        // 2. Tokenization & Sanitization
        string sanitizedText = combinedText.ToLowerInvariant();

        // Strip hierarchical numbering (e.g. "1.1.1", "2.4")
        sanitizedText = HierarchicalNumberRegex.Replace(sanitizedText, " ");

        // Strip remaining numbers/digits
        sanitizedText = DigitRegex.Replace(sanitizedText, " ");

        // Strip apostrophes before token matching
        sanitizedText = sanitizedText.Replace("'", "").Replace("’", "");

        // Match all letters-only words
        var matches = LetterWordRegex.Matches(sanitizedText);

        // 3. Filtering & Collecting Tokens
        var filteredTokens = new List<string>();
        foreach (Match match in matches)
        {
            string token = match.Value;
            if (StopWords.Contains(token) || token.Length <= 1)
            {
                continue;
            }
            filteredTokens.Add(token);
        }

        if (filteredTokens.Count == 0)
        {
            return new Dictionary<string, int>();
        }

        // 4. Unique Tokens Batch Lemmatization
        var uniqueTokens = filteredTokens.Distinct().ToList();
        var lemmas = await _lemmatizer.LemmatizeBatchAsync(uniqueTokens);

        // Build token-to-lemma mapping
        var tokenToLemmaMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < uniqueTokens.Count; i++)
        {
            if (i < lemmas.Count)
            {
                tokenToLemmaMap[uniqueTokens[i]] = lemmas[i];
            }
            else
            {
                tokenToLemmaMap[uniqueTokens[i]] = uniqueTokens[i];
            }
        }

        // 5. Frequency Aggregation
        var frequencies = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var token in filteredTokens)
        {
            if (tokenToLemmaMap.TryGetValue(token, out var lemma) && !string.IsNullOrWhiteSpace(lemma) && lemma.Length > 1)
            {
                if (frequencies.TryGetValue(lemma, out int count))
                {
                    frequencies[lemma] = count + 1;
                }
                else
                {
                    frequencies[lemma] = 1;
                }
            }
        }

        return frequencies;
    }
}
