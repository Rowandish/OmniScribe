using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OmniScribe.Models;

namespace OmniScribe.Services;

public interface ITranscriptionService
{
    Task<(string Transcript, long TokensUsed)> TranscribeAsync(
        List<string> audioPaths,
        AppSettings settings,
        Action<string>? onStatusUpdate = null,
        Action<double>? onProgressUpdate = null,
        CancellationToken ct = default);
}
