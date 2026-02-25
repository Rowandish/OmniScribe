using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OmniScribe.Models;
using RestSharp;

namespace OmniScribe.Services;

public class TranscriptionService : ITranscriptionService
{
    /// <summary>
    /// Transcribes audio file(s) via Whisper-compatible API.
    /// Supports chunked uploads — concatenates results.
    /// </summary>
    public async Task<(string Transcript, long TokensUsed)> TranscribeAsync(
        List<string> audioPaths,
        AppSettings settings,
        Action<string>? onStatusUpdate = null,
        Action<double>? onProgressUpdate = null,
        CancellationToken ct = default)
    {
        var endpoint = GetTranscriptionEndpoint(settings);
        var results = new List<string>();
        long totalTokens = 0;

        for (int i = 0; i < audioPaths.Count; i++)
        {
            ct.ThrowIfCancellationRequested();

            var progress = (double)(i) / audioPaths.Count * 100;
            onProgressUpdate?.Invoke(progress);
            onStatusUpdate?.Invoke($"Trascrizione chunk {i + 1}/{audioPaths.Count}...");

            var (text, tokens) = await TranscribeSingleAsync(audioPaths[i], endpoint, settings, ct);
            results.Add(text);
            totalTokens += tokens;
        }

        onProgressUpdate?.Invoke(100);
        return (string.Join("\n\n", results), totalTokens);
    }

    private async Task<(string Text, long Tokens)> TranscribeSingleAsync(
        string audioPath,
        string endpoint,
        AppSettings settings,
        CancellationToken ct)
    {
        var options = new RestClientOptions(endpoint)
        {
            Timeout = TimeSpan.FromMinutes(2) // 2 minutes
        };

        using var client = new RestClient(options);
        var request = new RestRequest("", Method.Post);
        request.AddHeader("Authorization", $"Bearer {settings.ApiKey}");

        request.AddFile("file", audioPath);
        request.AddParameter("model", settings.TranscriptionModel);
        request.AddParameter("response_format", "verbose_json");

        if (!string.IsNullOrWhiteSpace(settings.Glossary))
        {
            request.AddParameter("prompt", settings.Glossary);
        }

        try
        {
            var response = await client.ExecuteAsync(request, ct);

            if (!response.IsSuccessful)
            {
                throw new HttpRequestException(
                    $"Transcription API error ({response.StatusCode}): {response.Content?[..Math.Min(response.Content?.Length ?? 0, 500)]}");
            }

            var json = JsonDocument.Parse(response.Content!);
            var text = json.RootElement.GetProperty("text").GetString() ?? "";

            // Attempt to read token usage if present (some APIs provide it)
            long tokens = 0;
            if (json.RootElement.TryGetProperty("usage", out var usage) &&
                usage.TryGetProperty("total_tokens", out var totalTokens))
            {
                tokens = totalTokens.GetInt64();
            }

            // Estimate tokens from text length (rough: 1 token ≈ 4 chars)
            if (tokens == 0)
                tokens = text.Length / 4;

            return (text, tokens);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new TimeoutException("Transcription API request timed out. Check your connection and try again.");
        }
    }

    internal string GetTranscriptionEndpoint(AppSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.CustomEndpoint))
        {
            var baseUrl = settings.CustomEndpoint.TrimEnd('/');
            return baseUrl.EndsWith("/audio/transcriptions") ? baseUrl : $"{baseUrl}/audio/transcriptions";
        }

        return settings.Provider switch
        {
            "Groq" => "https://api.groq.com/openai/v1/audio/transcriptions",
            "Azure" => throw new NotSupportedException("Azure endpoint must be set in Custom Endpoint."),
            _ => "https://api.openai.com/v1/audio/transcriptions" // OpenAI default
        };
    }
}
