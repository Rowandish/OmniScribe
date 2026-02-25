using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;

namespace OmniScribe.Services;

public class AudioService : IAudioService
{
    private WaveInEvent? _waveIn;
    private WaveFileWriter? _writer;
    private string? _currentRecordingPath;
    private bool _isRecording;

    public event Action<float>? AudioLevelChanged;
    public event Action<TimeSpan>? RecordingDurationChanged;

    private DateTime _recordingStartTime;
    private System.Timers.Timer? _durationTimer;

    public bool IsRecording => _isRecording;

    public string StartRecording()
    {
        if (_isRecording) throw new InvalidOperationException("Already recording.");

        var tempFolder = Path.Combine(Path.GetTempPath(), "OmniScribe");
        Directory.CreateDirectory(tempFolder);
        _currentRecordingPath = Path.Combine(tempFolder, $"rec_{DateTime.Now:yyyyMMdd_HHmmss}.wav");

        _waveIn = new WaveInEvent
        {
            WaveFormat = new WaveFormat(16000, 16, 1) // 16kHz, 16-bit, mono — ideal for Whisper
        };

        _writer = new WaveFileWriter(_currentRecordingPath, _waveIn.WaveFormat);

        _waveIn.DataAvailable += (s, e) =>
        {
            _writer?.Write(e.Buffer, 0, e.BytesRecorded);

            // Calculate RMS level for the level meter
            float max = 0;
            for (int i = 0; i < e.BytesRecorded; i += 2)
            {
                short sample = (short)(e.Buffer[i] | (e.Buffer[i + 1] << 8));
                float sampleF = Math.Abs(sample / 32768f);
                if (sampleF > max) max = sampleF;
            }
            AudioLevelChanged?.Invoke(max);
        };

        _waveIn.RecordingStopped += (s, e) =>
        {
            _writer?.Dispose();
            _writer = null;
            _waveIn?.Dispose();
            _waveIn = null;
        };

        _waveIn.StartRecording();
        _isRecording = true;
        _recordingStartTime = DateTime.Now;

        _durationTimer = new System.Timers.Timer(500);
        _durationTimer.Elapsed += (s, e) =>
        {
            RecordingDurationChanged?.Invoke(DateTime.Now - _recordingStartTime);
        };
        _durationTimer.Start();

        return _currentRecordingPath;
    }

    public string StopRecording()
    {
        if (!_isRecording) throw new InvalidOperationException("Not recording.");

        _durationTimer?.Stop();
        _durationTimer?.Dispose();
        _durationTimer = null;

        _waveIn?.StopRecording();
        _isRecording = false;
        AudioLevelChanged?.Invoke(0);

        return _currentRecordingPath ?? string.Empty;
    }

    /// <summary>
    /// Trims silence from the beginning and end of a WAV file.
    /// </summary>
    public async Task<string> TrimSilenceAsync(string inputPath, float silenceThreshold = 0.01f, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            using var reader = new AudioFileReader(inputPath);
            var sampleRate = reader.WaveFormat.SampleRate;
            var channels = reader.WaveFormat.Channels;
            var samples = new float[sampleRate * channels]; // 1 second buffer
            var allSamples = new List<float>();

            int read;
            while ((read = reader.Read(samples, 0, samples.Length)) > 0)
            {
                ct.ThrowIfCancellationRequested();
                for (int i = 0; i < read; i++)
                    allSamples.Add(samples[i]);
            }

            // Find first non-silent sample
            int start = 0;
            for (int i = 0; i < allSamples.Count; i++)
            {
                if (Math.Abs(allSamples[i]) > silenceThreshold)
                {
                    start = Math.Max(0, i - sampleRate * channels / 4); // Keep 250ms before
                    break;
                }
            }

            // Find last non-silent sample
            int end = allSamples.Count - 1;
            for (int i = allSamples.Count - 1; i >= 0; i--)
            {
                if (Math.Abs(allSamples[i]) > silenceThreshold)
                {
                    end = Math.Min(allSamples.Count - 1, i + sampleRate * channels / 4); // Keep 250ms after
                    break;
                }
            }

            var trimmedPath = Path.Combine(
                Path.GetDirectoryName(inputPath)!,
                Path.GetFileNameWithoutExtension(inputPath) + "_trimmed.wav");

            using var writer = new WaveFileWriter(trimmedPath, reader.WaveFormat);
            var trimmedSamples = allSamples.Skip(start).Take(end - start + 1).ToArray();
            writer.WriteSamples(trimmedSamples, 0, trimmedSamples.Length);

            return trimmedPath;
        }, ct);
    }

    /// <summary>
    /// Splits an audio file into chunks of max ~25MB for API upload.
    /// </summary>
    public async Task<List<string>> AutoChunkAsync(string inputPath, long maxSizeBytes = 25 * 1024 * 1024, CancellationToken ct = default)
    {
        var fileInfo = new FileInfo(inputPath);
        if (fileInfo.Length <= maxSizeBytes)
            return new List<string> { inputPath };

        return await Task.Run(() =>
        {
            var chunks = new List<string>();
            using var reader = new AudioFileReader(inputPath);

            var bytesPerSecond = reader.WaveFormat.AverageBytesPerSecond;
            // Estimate seconds per chunk (leave margin for WAV header)
            var secondsPerChunk = (maxSizeBytes - 1000) / bytesPerSecond;
            var samplesPerChunk = (int)(secondsPerChunk * reader.WaveFormat.SampleRate * reader.WaveFormat.Channels);

            var buffer = new float[reader.WaveFormat.SampleRate * reader.WaveFormat.Channels]; // 1s buffer
            int chunkIndex = 0;
            int samplesWritten = 0;
            WaveFileWriter? chunkWriter = null;
            string? currentChunkPath = null;

            try
            {
                int read;
                while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ct.ThrowIfCancellationRequested();

                    if (chunkWriter == null)
                    {
                        currentChunkPath = Path.Combine(
                            Path.GetDirectoryName(inputPath)!,
                            $"{Path.GetFileNameWithoutExtension(inputPath)}_chunk{chunkIndex:D3}.wav");
                        chunkWriter = new WaveFileWriter(currentChunkPath, reader.WaveFormat);
                        chunks.Add(currentChunkPath);
                        samplesWritten = 0;
                    }

                    chunkWriter.WriteSamples(buffer, 0, read);
                    samplesWritten += read;

                    if (samplesWritten >= samplesPerChunk)
                    {
                        chunkWriter.Dispose();
                        chunkWriter = null;
                        chunkIndex++;
                    }
                }
            }
            finally
            {
                chunkWriter?.Dispose();
            }

            return chunks;
        }, ct);
    }

    /// <summary>
    /// Validates that the file extension is supported.
    /// </summary>
    public static bool IsSupportedFormat(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext is ".wav" or ".mp3" or ".m4a" or ".ogg" or ".webm" or ".flac";
    }
}
