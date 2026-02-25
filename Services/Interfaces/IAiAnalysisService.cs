using System;
using System.Threading;
using System.Threading.Tasks;
using OmniScribe.Models;

namespace OmniScribe.Services;

public interface IAiAnalysisService
{
    Task<(string Analysis, long TokensUsed, decimal EstimatedCost)> AnalyzeAsync(
        string transcriptionText,
        AppSettings settings,
        Action<string>? onStatusUpdate = null,
        CancellationToken ct = default);
}
