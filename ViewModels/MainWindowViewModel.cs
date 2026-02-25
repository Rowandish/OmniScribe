using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OmniScribe.Models;
using OmniScribe.Services;

namespace OmniScribe.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly AudioService _audioService = new();
    private readonly TranscriptionService _transcriptionService = new();
    private readonly AiAnalysisService _aiAnalysisService = new();
    private CancellationTokenSource? _cts;

    // Child ViewModels
    public RecorderViewModel Recorder { get; } = new();
    public SettingsViewModel Settings { get; } = new();
    public HistoryViewModel History { get; } = new();
    public ObservableCollection<NotificationItem> Notifications => NotificationService.Instance.Notifications;

    // Status bar
    [ObservableProperty]
    private string _statusText = "Pronto";

    [ObservableProperty]
    private double _progressValue;

    [ObservableProperty]
    private bool _isProcessing;

    [ObservableProperty]
    private bool _isProgressIndeterminate;

    // Result display
    [ObservableProperty]
    private string _resultMarkdown = string.Empty;

    [ObservableProperty]
    private string _transcriptionText = string.Empty;

    // Panel visibility
    [ObservableProperty]
    private bool _isSettingsOpen;

    [ObservableProperty]
    private bool _showTranscription;

    [ObservableProperty]
    private bool _showResult;

    public MainWindowViewModel()
    {
        Recorder.AudioReady += OnAudioReady;
        History.SessionSelected += OnSessionSelected;
    }

    private async void OnAudioReady(string audioPath)
    {
        await ProcessAudioPipelineAsync(audioPath);
    }

    private void OnSessionSelected(SessionRecord session)
    {
        TranscriptionText = session.TranscriptionText;
        ResultMarkdown = session.AnalysisResult;
        ShowTranscription = !string.IsNullOrEmpty(session.TranscriptionText);
        ShowResult = !string.IsNullOrEmpty(session.AnalysisResult);
    }

    [RelayCommand]
    private void ToggleSettings()
    {
        IsSettingsOpen = !IsSettingsOpen;
    }

    [RelayCommand]
    private void CancelProcessing()
    {
        _cts?.Cancel();
        StatusText = "Operazione annullata.";
        IsProcessing = false;
        ProgressValue = 0;
        NotificationService.Instance.Warning("Elaborazione annullata dall'utente.");
    }

    [RelayCommand]
    private async Task ProcessAudioPipelineAsync(string audioPath)
    {
        if (string.IsNullOrEmpty(Settings.ApiKey))
        {
            NotificationService.Instance.Warning("Configura la tua API Key nelle impostazioni.");
            IsSettingsOpen = true;
            return;
        }

        _cts = new CancellationTokenSource();
        var ct = _cts.Token;
        IsProcessing = true;
        ProgressValue = 0;
        ShowTranscription = false;
        ShowResult = false;
        ResultMarkdown = string.Empty;
        TranscriptionText = string.Empty;

        try
        {
            // Step 1: Silence Trimming
            StatusText = "Ottimizzazione audio...";
            IsProgressIndeterminate = true;
            ProgressValue = 10;

            string processedPath;
            try
            {
                processedPath = await _audioService.TrimSilenceAsync(audioPath, ct: ct);
            }
            catch
            {
                processedPath = audioPath; // Fall back to original if trimming fails
            }

            // Step 2: Auto-Chunking
            StatusText = "Analisi dimensione file...";
            ProgressValue = 20;
            var chunks = await _audioService.AutoChunkAsync(processedPath, ct: ct);

            if (chunks.Count > 1)
                NotificationService.Instance.Info($"Audio diviso in {chunks.Count} segmenti per il caricamento.");

            // Step 3: Transcription
            IsProgressIndeterminate = false;
            StatusText = "Trascrizione in corso...";

            var appSettings = Settings.ToAppSettings();
            var (transcript, transcriptionTokens) = await _transcriptionService.TranscribeAsync(
                chunks,
                appSettings,
                status => Dispatcher.UIThread.Post(() => StatusText = status),
                progress => Dispatcher.UIThread.Post(() => ProgressValue = 20 + progress * 0.4),
                ct);

            TranscriptionText = transcript;
            ShowTranscription = true;
            ProgressValue = 60;

            // Step 4: AI Analysis
            StatusText = "L'IA sta elaborando il verbale...";
            ProgressValue = 65;

            var (analysis, analysisTokens, cost) = await _aiAnalysisService.AnalyzeAsync(
                transcript,
                appSettings,
                status => Dispatcher.UIThread.Post(() => StatusText = status),
                ct);

            ResultMarkdown = analysis;
            ShowResult = true;
            ProgressValue = 100;

            // Update token counters
            var totalTokens = transcriptionTokens + analysisTokens;
            Settings.UpdateTokenUsage(totalTokens, cost);

            // Save to history
            var session = new SessionRecord
            {
                SourceFileName = Path.GetFileName(audioPath),
                TranscriptionText = transcript,
                AnalysisResult = analysis,
                TokensUsed = totalTokens,
                Cost = cost
            };
            History.AddSession(session);

            StatusText = $"Completato — {totalTokens:N0} token (~${cost:F4})";
            NotificationService.Instance.Success("Analisi completata con successo!");
        }
        catch (OperationCanceledException)
        {
            StatusText = "Operazione annullata.";
        }
        catch (TimeoutException ex)
        {
            StatusText = "Timeout della richiesta.";
            NotificationService.Instance.Error(ex.Message);
        }
        catch (Exception ex)
        {
            StatusText = "Errore durante l'elaborazione.";
            NotificationService.Instance.Error($"Errore: {ex.Message}");
        }
        finally
        {
            IsProcessing = false;
            IsProgressIndeterminate = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    [RelayCommand]
    private void CopyResult()
    {
        if (!string.IsNullOrEmpty(ResultMarkdown))
        {
            // Copy to clipboard will be handled in the view via TopLevel.Clipboard
            NotificationService.Instance.Info("Risultato copiato negli appunti.");
        }
    }

    [RelayCommand]
    private async Task ExportResultAsync()
    {
        if (string.IsNullOrEmpty(ResultMarkdown))
            return;

        try
        {
            var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var fileName = $"OmniScribe_{DateTime.Now:yyyyMMdd_HHmmss}.md";
            var fullPath = Path.Combine(desktopPath, fileName);
            await File.WriteAllTextAsync(fullPath, ResultMarkdown);
            NotificationService.Instance.Success($"Esportato in: {fileName}");
        }
        catch (Exception ex)
        {
            NotificationService.Instance.Error($"Errore esportazione: {ex.Message}");
        }
    }
}
