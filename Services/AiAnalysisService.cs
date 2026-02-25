using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OmniScribe.Models;
using RestSharp;

namespace OmniScribe.Services;

public class AiAnalysisService
{
    /// <summary>
    /// Sends transcription text to an LLM for analysis (verbale, tasks, summary).
    /// Returns the analysis markdown and token usage.
    /// </summary>
    public async Task<(string Analysis, long TokensUsed, decimal EstimatedCost)> AnalyzeAsync(
        string transcriptionText,
        AppSettings settings,
        Action<string>? onStatusUpdate = null,
        CancellationToken ct = default)
    {
        onStatusUpdate?.Invoke("L'IA sta elaborando il verbale...");

        var endpoint = GetChatEndpoint(settings);
        var options = new RestClientOptions(endpoint)
        {
            Timeout = TimeSpan.FromMinutes(3) // 3 minutes for long analyses
        };

        using var client = new RestClient(options);
        var request = new RestRequest("", Method.Post);
        request.AddHeader("Authorization", $"Bearer {settings.ApiKey}");
        request.AddHeader("Content-Type", "application/json");

        var systemMessage = settings.SystemPrompt;
        if (!string.IsNullOrWhiteSpace(settings.Glossary))
        {
            systemMessage += $"\n\nGlossario/Contesto aziendale:\n{settings.Glossary}";
        }

        var body = new
        {
            model = settings.AnalysisModel,
            messages = new[]
            {
                new { role = "system", content = systemMessage },
                new { role = "user", content = $"Ecco la trascrizione da analizzare:\n\n{transcriptionText}" }
            },
            temperature = 0.3,
            max_tokens = 4096
        };

        request.AddJsonBody(body);

        try
        {
            var response = await client.ExecuteAsync(request, ct);

            if (!response.IsSuccessful)
            {
                throw new HttpRequestException(
                    $"Analysis API error ({response.StatusCode}): {response.Content?[..Math.Min(response.Content?.Length ?? 0, 500)]}");
            }

            var json = JsonDocument.Parse(response.Content!);
            var analysisText = json.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "";

            long totalTokens = 0;
            decimal cost = 0;

            if (json.RootElement.TryGetProperty("usage", out var usage))
            {
                var promptTokens = usage.TryGetProperty("prompt_tokens", out var pt) ? pt.GetInt64() : 0;
                var completionTokens = usage.TryGetProperty("completion_tokens", out var ct2) ? ct2.GetInt64() : 0;
                totalTokens = promptTokens + completionTokens;

                // Estimate cost based on model
                cost = EstimateCost(settings.AnalysisModel, promptTokens, completionTokens);
            }

            return (analysisText, totalTokens, cost);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new TimeoutException("Analysis API request timed out. Check your connection and try again.");
        }
    }

    private decimal EstimateCost(string model, long promptTokens, long completionTokens)
    {
        // Approximate pricing per 1M tokens (USD)
        var (inputRate, outputRate) = model switch
        {
            "gpt-4o-mini" => (0.15m, 0.60m),
            "gpt-4o" => (2.50m, 10.00m),
            "llama-3.3-70b-versatile" => (0.59m, 0.79m),
            "llama-3.1-8b-instant" => (0.05m, 0.08m),
            _ => (0.15m, 0.60m)
        };

        return (promptTokens * inputRate / 1_000_000m) + (completionTokens * outputRate / 1_000_000m);
    }

    private string GetChatEndpoint(AppSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.CustomEndpoint))
        {
            var baseUrl = settings.CustomEndpoint.TrimEnd('/');
            // If custom endpoint already includes /chat/completions, use as-is
            return baseUrl.EndsWith("/chat/completions") ? baseUrl : $"{baseUrl}/chat/completions";
        }

        return settings.Provider switch
        {
            "Groq" => "https://api.groq.com/openai/v1/chat/completions",
            "Azure" => throw new NotSupportedException("Azure endpoint must be set in Custom Endpoint."),
            _ => "https://api.openai.com/v1/chat/completions"
        };
    }
}
