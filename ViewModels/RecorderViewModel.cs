using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OmniScribe.Services;

namespace OmniScribe.ViewModels;

public partial class RecorderViewModel : ViewModelBase
{
    private readonly AudioService _audioService = new();

    [ObservableProperty]
    private bool _isRecording;

    [ObservableProperty]
    private float _audioLevel;

    [ObservableProperty]
    private string _recordingDuration = "00:00";

    [ObservableProperty]
    private string? _importedFilePath;

    [ObservableProperty]
    private string _importedFileName = string.Empty;

    [ObservableProperty]
    private bool _hasImportedFile;

    [ObservableProperty]
    private bool _isDragOver;

    private string? _currentRecordingPath;

    /// <summary>
    /// Raised when audio is ready (recorded or imported). Passes the file path.
    /// </summary>
    public event Action<string>? AudioReady;

    public RecorderViewModel()
    {
        _audioService.AudioLevelChanged += level =>
        {
            Dispatcher.UIThread.Post(() => AudioLevel = level);
        };

        _audioService.RecordingDurationChanged += duration =>
        {
            Dispatcher.UIThread.Post(() => RecordingDuration = duration.ToString(@"mm\:ss"));
        };
    }

    [RelayCommand]
    private void ToggleRecording()
    {
        if (IsRecording)
            StopRecording();
        else
            StartRecording();
    }

    private void StartRecording()
    {
        try
        {
            _currentRecordingPath = _audioService.StartRecording();
            IsRecording = true;
            RecordingDuration = "00:00";
            HasImportedFile = false;
            ImportedFileName = string.Empty;
            ImportedFilePath = null;
            NotificationService.Instance.Info("Registrazione in corso...");
        }
        catch (Exception ex)
        {
            NotificationService.Instance.Error($"Errore avvio registrazione: {ex.Message}");
        }
    }

    private void StopRecording()
    {
        try
        {
            var path = _audioService.StopRecording();
            IsRecording = false;
            AudioLevel = 0;
            NotificationService.Instance.Success("Registrazione completata.");
            AudioReady?.Invoke(path);
        }
        catch (Exception ex)
        {
            NotificationService.Instance.Error($"Errore stop registrazione: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ImportFileAsync()
    {
        // This is triggered from the view via file dialog or drag-drop
        // The actual file path is set via HandleFileDrop
    }

    public void HandleFileDrop(string filePath)
    {
        if (!AudioService.IsSupportedFormat(filePath))
        {
            NotificationService.Instance.Warning("Formato non supportato. Usa: .wav, .mp3, .m4a, .ogg, .flac");
            return;
        }

        ImportedFilePath = filePath;
        ImportedFileName = System.IO.Path.GetFileName(filePath);
        HasImportedFile = true;
        NotificationService.Instance.Info($"File importato: {ImportedFileName}");
        AudioReady?.Invoke(filePath);
    }
}
