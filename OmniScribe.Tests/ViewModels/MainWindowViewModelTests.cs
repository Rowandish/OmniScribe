using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using OmniScribe.Models;
using OmniScribe.Services;
using OmniScribe.ViewModels;
using Xunit;

namespace OmniScribe.Tests.ViewModels;

public class MainWindowViewModelTests
{
    private readonly IAudioService _audioService = Substitute.For<IAudioService>();
    private readonly ITranscriptionService _transcriptionService = Substitute.For<ITranscriptionService>();
    private readonly IAiAnalysisService _aiAnalysisService = Substitute.For<IAiAnalysisService>();
    private readonly INotificationService _notificationService = Substitute.For<INotificationService>();
    private readonly ISettingsService _settingsService = Substitute.For<ISettingsService>();

    private MainWindowViewModel CreateVm()
    {
        _settingsService.LoadSettings().Returns(new AppSettings());
        _settingsService.LoadHistory().Returns(new List<SessionRecord>());
        _notificationService.Notifications.Returns(new ObservableCollection<NotificationItem>());

        return new MainWindowViewModel(
            _audioService, _transcriptionService, _aiAnalysisService,
            _notificationService, _settingsService);
    }

    [Fact]
    public void InitialState_StatusTextIsPronto()
    {
        var vm = CreateVm();
        Assert.Equal("Pronto", vm.StatusText);
    }

    [Fact]
    public void InitialState_IsNotProcessing()
    {
        var vm = CreateVm();
        Assert.False(vm.IsProcessing);
    }

    [Fact]
    public void InitialState_SettingsNotOpen()
    {
        var vm = CreateVm();
        Assert.False(vm.IsSettingsOpen);
    }

    [Fact]
    public void InitialState_ResultsNotShown()
    {
        var vm = CreateVm();
        Assert.False(vm.ShowTranscription);
        Assert.False(vm.ShowResult);
        Assert.Equal(string.Empty, vm.ResultMarkdown);
        Assert.Equal(string.Empty, vm.TranscriptionText);
    }

    [Fact]
    public void ToggleSettings_TogglesIsSettingsOpen()
    {
        var vm = CreateVm();

        vm.ToggleSettingsCommand.Execute(null);
        Assert.True(vm.IsSettingsOpen);

        vm.ToggleSettingsCommand.Execute(null);
        Assert.False(vm.IsSettingsOpen);
    }

    [Fact]
    public async Task ProcessAudioPipeline_WithoutApiKey_ShowsWarningAndOpensSettings()
    {
        var vm = CreateVm();
        // ApiKey is empty by default

        await vm.ProcessAudioPipelineCommand.ExecuteAsync("test.wav");

        Assert.True(vm.IsSettingsOpen);
        _notificationService.Received(1).Warning(Arg.Is<string>(s => s.Contains("API Key")));
    }

    [Fact]
    public async Task ProcessAudioPipeline_WithoutApiKey_DoesNotCallServices()
    {
        var vm = CreateVm();

        await vm.ProcessAudioPipelineCommand.ExecuteAsync("test.wav");

        await _audioService.DidNotReceive().TrimSilenceAsync(Arg.Any<string>(), Arg.Any<float>(), Arg.Any<CancellationToken>());
        await _transcriptionService.DidNotReceive().TranscribeAsync(
            Arg.Any<List<string>>(), Arg.Any<AppSettings>(), Arg.Any<System.Action<string>>(),
            Arg.Any<System.Action<double>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAudioPipeline_Success_CallsAllServicesInOrder()
    {
        var vm = CreateVm();
        vm.Settings.ApiKey = "sk-test";

        _audioService.TrimSilenceAsync(Arg.Any<string>(), Arg.Any<float>(), Arg.Any<CancellationToken>())
            .Returns("trimmed.wav");
        _audioService.AutoChunkAsync(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(new List<string> { "trimmed.wav" });
        _transcriptionService.TranscribeAsync(
            Arg.Any<List<string>>(), Arg.Any<AppSettings>(), Arg.Any<System.Action<string>>(),
            Arg.Any<System.Action<double>>(), Arg.Any<CancellationToken>())
            .Returns(("Transcribed text", 100L));
        _aiAnalysisService.AnalyzeAsync(
            Arg.Any<string>(), Arg.Any<AppSettings>(), Arg.Any<System.Action<string>>(),
            Arg.Any<CancellationToken>())
            .Returns(("# Analysis", 200L, 0.05m));

        await vm.ProcessAudioPipelineCommand.ExecuteAsync("audio.wav");

        // Verify all steps were called
        await _audioService.Received(1).TrimSilenceAsync("audio.wav", Arg.Any<float>(), Arg.Any<CancellationToken>());
        await _audioService.Received(1).AutoChunkAsync("trimmed.wav", Arg.Any<long>(), Arg.Any<CancellationToken>());
        await _transcriptionService.Received(1).TranscribeAsync(
            Arg.Any<List<string>>(), Arg.Any<AppSettings>(), Arg.Any<System.Action<string>>(),
            Arg.Any<System.Action<double>>(), Arg.Any<CancellationToken>());
        await _aiAnalysisService.Received(1).AnalyzeAsync(
            "Transcribed text", Arg.Any<AppSettings>(), Arg.Any<System.Action<string>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAudioPipeline_Success_SetsResults()
    {
        var vm = CreateVm();
        vm.Settings.ApiKey = "sk-test";

        _audioService.TrimSilenceAsync(Arg.Any<string>(), Arg.Any<float>(), Arg.Any<CancellationToken>())
            .Returns("trimmed.wav");
        _audioService.AutoChunkAsync(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(new List<string> { "trimmed.wav" });
        _transcriptionService.TranscribeAsync(
            Arg.Any<List<string>>(), Arg.Any<AppSettings>(), Arg.Any<System.Action<string>>(),
            Arg.Any<System.Action<double>>(), Arg.Any<CancellationToken>())
            .Returns(("Transcribed text", 100L));
        _aiAnalysisService.AnalyzeAsync(
            Arg.Any<string>(), Arg.Any<AppSettings>(), Arg.Any<System.Action<string>>(),
            Arg.Any<CancellationToken>())
            .Returns(("# Analysis Result", 200L, 0.05m));

        await vm.ProcessAudioPipelineCommand.ExecuteAsync("audio.wav");

        Assert.Equal("Transcribed text", vm.TranscriptionText);
        Assert.Equal("# Analysis Result", vm.ResultMarkdown);
        Assert.True(vm.ShowTranscription);
        Assert.True(vm.ShowResult);
        Assert.Equal(100, vm.ProgressValue);
        Assert.False(vm.IsProcessing);
    }

    [Fact]
    public async Task ProcessAudioPipeline_Success_SavesSessionToHistory()
    {
        var vm = CreateVm();
        vm.Settings.ApiKey = "sk-test";

        _audioService.TrimSilenceAsync(Arg.Any<string>(), Arg.Any<float>(), Arg.Any<CancellationToken>())
            .Returns("trimmed.wav");
        _audioService.AutoChunkAsync(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(new List<string> { "trimmed.wav" });
        _transcriptionService.TranscribeAsync(
            Arg.Any<List<string>>(), Arg.Any<AppSettings>(), Arg.Any<System.Action<string>>(),
            Arg.Any<System.Action<double>>(), Arg.Any<CancellationToken>())
            .Returns(("Transcription", 100L));
        _aiAnalysisService.AnalyzeAsync(
            Arg.Any<string>(), Arg.Any<AppSettings>(), Arg.Any<System.Action<string>>(),
            Arg.Any<CancellationToken>())
            .Returns(("Analysis", 200L, 0.05m));

        await vm.ProcessAudioPipelineCommand.ExecuteAsync("audio.wav");

        Assert.Single(vm.History.Sessions);
        Assert.Equal("audio.wav", vm.History.Sessions[0].SourceFileName);
    }

    [Fact]
    public async Task ProcessAudioPipeline_Success_ShowsSuccessNotification()
    {
        var vm = CreateVm();
        vm.Settings.ApiKey = "sk-test";

        _audioService.TrimSilenceAsync(Arg.Any<string>(), Arg.Any<float>(), Arg.Any<CancellationToken>())
            .Returns("trimmed.wav");
        _audioService.AutoChunkAsync(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(new List<string> { "trimmed.wav" });
        _transcriptionService.TranscribeAsync(
            Arg.Any<List<string>>(), Arg.Any<AppSettings>(), Arg.Any<System.Action<string>>(),
            Arg.Any<System.Action<double>>(), Arg.Any<CancellationToken>())
            .Returns(("Text", 0L));
        _aiAnalysisService.AnalyzeAsync(
            Arg.Any<string>(), Arg.Any<AppSettings>(), Arg.Any<System.Action<string>>(),
            Arg.Any<CancellationToken>())
            .Returns(("Result", 0L, 0m));

        await vm.ProcessAudioPipelineCommand.ExecuteAsync("audio.wav");

        _notificationService.Received().Success(Arg.Is<string>(s => s.Contains("completata")));
    }

    [Fact]
    public async Task ProcessAudioPipeline_TrimFails_FallsBackToOriginalFile()
    {
        var vm = CreateVm();
        vm.Settings.ApiKey = "sk-test";

        _audioService.TrimSilenceAsync(Arg.Any<string>(), Arg.Any<float>(), Arg.Any<CancellationToken>())
            .Returns<string>(_ => throw new System.Exception("Trim failed"));
        _audioService.AutoChunkAsync("original.wav", Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(new List<string> { "original.wav" });
        _transcriptionService.TranscribeAsync(
            Arg.Any<List<string>>(), Arg.Any<AppSettings>(), Arg.Any<System.Action<string>>(),
            Arg.Any<System.Action<double>>(), Arg.Any<CancellationToken>())
            .Returns(("Text", 0L));
        _aiAnalysisService.AnalyzeAsync(
            Arg.Any<string>(), Arg.Any<AppSettings>(), Arg.Any<System.Action<string>>(),
            Arg.Any<CancellationToken>())
            .Returns(("Result", 0L, 0m));

        await vm.ProcessAudioPipelineCommand.ExecuteAsync("original.wav");

        // Should still complete successfully using original file
        await _audioService.Received(1).AutoChunkAsync("original.wav", Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAudioPipeline_MultipleChunks_ShowsNotification()
    {
        var vm = CreateVm();
        vm.Settings.ApiKey = "sk-test";

        _audioService.TrimSilenceAsync(Arg.Any<string>(), Arg.Any<float>(), Arg.Any<CancellationToken>())
            .Returns("trimmed.wav");
        _audioService.AutoChunkAsync(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(new List<string> { "chunk1.wav", "chunk2.wav", "chunk3.wav" });
        _transcriptionService.TranscribeAsync(
            Arg.Any<List<string>>(), Arg.Any<AppSettings>(), Arg.Any<System.Action<string>>(),
            Arg.Any<System.Action<double>>(), Arg.Any<CancellationToken>())
            .Returns(("Text", 0L));
        _aiAnalysisService.AnalyzeAsync(
            Arg.Any<string>(), Arg.Any<AppSettings>(), Arg.Any<System.Action<string>>(),
            Arg.Any<CancellationToken>())
            .Returns(("Result", 0L, 0m));

        await vm.ProcessAudioPipelineCommand.ExecuteAsync("large.wav");

        _notificationService.Received().Info(Arg.Is<string>(s => s.Contains("3")));
    }

    [Fact]
    public async Task ProcessAudioPipeline_TranscriptionFails_ShowsError()
    {
        var vm = CreateVm();
        vm.Settings.ApiKey = "sk-test";

        _audioService.TrimSilenceAsync(Arg.Any<string>(), Arg.Any<float>(), Arg.Any<CancellationToken>())
            .Returns("trimmed.wav");
        _audioService.AutoChunkAsync(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(new List<string> { "trimmed.wav" });
        _transcriptionService.TranscribeAsync(
            Arg.Any<List<string>>(), Arg.Any<AppSettings>(), Arg.Any<System.Action<string>>(),
            Arg.Any<System.Action<double>>(), Arg.Any<CancellationToken>())
            .Returns<(string, long)>(_ => throw new System.Exception("API error"));

        await vm.ProcessAudioPipelineCommand.ExecuteAsync("audio.wav");

        Assert.Contains("Errore", vm.StatusText);
        _notificationService.Received().Error(Arg.Any<string>());
        Assert.False(vm.IsProcessing);
    }

    [Fact]
    public async Task ProcessAudioPipeline_Timeout_ShowsTimeoutMessage()
    {
        var vm = CreateVm();
        vm.Settings.ApiKey = "sk-test";

        _audioService.TrimSilenceAsync(Arg.Any<string>(), Arg.Any<float>(), Arg.Any<CancellationToken>())
            .Returns("trimmed.wav");
        _audioService.AutoChunkAsync(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(new List<string> { "trimmed.wav" });
        _transcriptionService.TranscribeAsync(
            Arg.Any<List<string>>(), Arg.Any<AppSettings>(), Arg.Any<System.Action<string>>(),
            Arg.Any<System.Action<double>>(), Arg.Any<CancellationToken>())
            .Returns<(string, long)>(_ => throw new System.TimeoutException("Timed out"));

        await vm.ProcessAudioPipelineCommand.ExecuteAsync("audio.wav");

        Assert.Contains("Timeout", vm.StatusText);
    }

    [Fact]
    public void CancelProcessing_SetsCorrectState()
    {
        var vm = CreateVm();

        vm.CancelProcessingCommand.Execute(null);

        Assert.Contains("annullata", vm.StatusText.ToLowerInvariant());
        Assert.False(vm.IsProcessing);
        Assert.Equal(0, vm.ProgressValue);
        _notificationService.Received(1).Warning(Arg.Any<string>());
    }

    [Fact]
    public void CopyResult_WithResult_ShowsNotification()
    {
        var vm = CreateVm();
        vm.ResultMarkdown = "# Some result";

        vm.CopyResultCommand.Execute(null);

        _notificationService.Received(1).Info(Arg.Is<string>(s => s.Contains("copiato")));
    }

    [Fact]
    public void CopyResult_WithEmptyResult_DoesNotNotify()
    {
        var vm = CreateVm();
        vm.ResultMarkdown = string.Empty;

        vm.CopyResultCommand.Execute(null);

        _notificationService.DidNotReceive().Info(Arg.Any<string>());
    }

    [Fact]
    public void ChildViewModels_AreInitialized()
    {
        var vm = CreateVm();
        Assert.NotNull(vm.Recorder);
        Assert.NotNull(vm.Settings);
        Assert.NotNull(vm.History);
    }
}
