using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Application.Services.IServices;

namespace Application.Services;

public class UkrainianLemmatizer : ILemmatizer
{
    private readonly HttpClient _httpClient;

    public UkrainianLemmatizer(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public string Lemmatize(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
            return string.Empty;

        try
        {
            var response = _httpClient.PostAsJsonAsync("lemmatize", new { word }).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
            var result = response.Content.ReadFromJsonAsync<SingleResponse>().GetAwaiter().GetResult();
            return result?.Lemma ?? word;
        }
        catch (Exception)
        {
            // Fallback to clean lowercase word if the NLP service is unavailable
            return word.Trim().ToLowerInvariant();
        }
    }

    public async Task<List<string>> LemmatizeBatchAsync(List<string> words)
    {
        if (words == null || words.Count == 0)
            return new List<string>();

        try
        {
            var response = await _httpClient.PostAsJsonAsync("lemmatize-batch", new { words });
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<BatchResponse>();
            return result?.Lemmas ?? words;
        }
        catch (Exception)
        {
            // Fallback to lowercase words if the NLP service is unavailable
            var fallback = new List<string>();
            foreach (var w in words)
            {
                fallback.Add(w.Trim().ToLowerInvariant());
            }
            return fallback;
        }
    }

    private class SingleResponse
    {
        public string Lemma { get; set; } = string.Empty;
    }

    private class BatchResponse
    {
        public List<string> Lemmas { get; set; } = new();
    }
}
