using System;

namespace OmniScribe.Models;

public class SessionRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string SourceFileName { get; set; } = string.Empty;
    public string TranscriptionText { get; set; } = string.Empty;
    public string AnalysisResult { get; set; } = string.Empty;
    public long TokensUsed { get; set; }
    public decimal Cost { get; set; }

    public string DisplayName => $"{Timestamp:HH:mm} — {(string.IsNullOrEmpty(SourceFileName) ? "Registrazione" : SourceFileName)}";
}
