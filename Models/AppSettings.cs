using System.Collections.Generic;

namespace OmniScribe.Models;

public class AppSettings
{
    public string Provider { get; set; } = "OpenAI";
    public string ApiKey { get; set; } = string.Empty;
    public string CustomEndpoint { get; set; } = string.Empty;
    public string TranscriptionModel { get; set; } = "whisper-1";
    public string AnalysisModel { get; set; } = "gpt-4o-mini";
    public string SystemPrompt { get; set; } = "Agisci come un segretario tecnico esperto. Analizza la trascrizione seguente e genera:\n1. Un verbale strutturato\n2. Una lista di task/azioni emerse\n3. Una sintesi concisa\n\nFormatta tutto in Markdown.";
    public string Glossary { get; set; } = string.Empty;
    public long TotalTokensUsed { get; set; }
    public decimal EstimatedCost { get; set; }
    public List<string> AvailableProviders { get; set; } = new() { "OpenAI", "Azure", "Groq" };
    public List<string> TranscriptionModels { get; set; } = new() { "whisper-1", "whisper-large-v3", "whisper-large-v3-turbo" };
    public List<string> AnalysisModels { get; set; } = new() { "gpt-4o-mini", "gpt-4o", "llama-3.3-70b-versatile", "llama-3.1-8b-instant" };
}
