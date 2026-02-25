using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OmniScribe.Services;

public interface IAudioService
{
    bool IsRecording { get; }
    event Action<float>? AudioLevelChanged;
    event Action<TimeSpan>? RecordingDurationChanged;

    string StartRecording();
    string StopRecording();
    Task<string> TrimSilenceAsync(string inputPath, float silenceThreshold = 0.01f, CancellationToken ct = default);
    Task<List<string>> AutoChunkAsync(string inputPath, long maxSizeBytes = 25 * 1024 * 1024, CancellationToken ct = default);
}
