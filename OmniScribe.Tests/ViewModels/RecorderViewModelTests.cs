using NSubstitute;
using OmniScribe.Services;
using OmniScribe.ViewModels;
using Xunit;

namespace OmniScribe.Tests.ViewModels;

public class RecorderViewModelTests
{
    private readonly IAudioService _audioService = Substitute.For<IAudioService>();
    private readonly INotificationService _notificationService = Substitute.For<INotificationService>();

    private RecorderViewModel CreateVm()
    {
        return new RecorderViewModel(_audioService, _notificationService);
    }

    [Fact]
    public void InitialState_IsNotRecording()
    {
        var vm = CreateVm();
        Assert.False(vm.IsRecording);
    }

    [Fact]
    public void InitialState_HasNoImportedFile()
    {
        var vm = CreateVm();
        Assert.False(vm.HasImportedFile);
        Assert.Equal(string.Empty, vm.ImportedFileName);
        Assert.Null(vm.ImportedFilePath);
    }

    [Fact]
    public void InitialState_DurationIsZero()
    {
        var vm = CreateVm();
        Assert.Equal("00:00", vm.RecordingDuration);
    }

    [Fact]
    public void ToggleRecording_WhenNotRecording_StartsRecording()
    {
        _audioService.StartRecording().Returns("/tmp/test.wav");
        var vm = CreateVm();

        vm.ToggleRecordingCommand.Execute(null);

        Assert.True(vm.IsRecording);
        _audioService.Received(1).StartRecording();
        _notificationService.Received(1).Info(Arg.Any<string>());
    }

    [Fact]
    public void ToggleRecording_WhenRecording_StopsRecording()
    {
        _audioService.StartRecording().Returns("/tmp/test.wav");
        _audioService.StopRecording().Returns("/tmp/test.wav");
        var vm = CreateVm();

        // Start recording
        vm.ToggleRecordingCommand.Execute(null);
        Assert.True(vm.IsRecording);

        // Stop recording
        vm.ToggleRecordingCommand.Execute(null);
        Assert.False(vm.IsRecording);
        _audioService.Received(1).StopRecording();
    }

    [Fact]
    public void ToggleRecording_WhenStops_FiresAudioReadyEvent()
    {
        _audioService.StartRecording().Returns("/tmp/test.wav");
        _audioService.StopRecording().Returns("/tmp/test.wav");
        var vm = CreateVm();
        string? receivedPath = null;
        vm.AudioReady += path => receivedPath = path;

        vm.ToggleRecordingCommand.Execute(null); // start
        vm.ToggleRecordingCommand.Execute(null); // stop

        Assert.Equal("/tmp/test.wav", receivedPath);
    }

    [Fact]
    public void ToggleRecording_StartFails_ShowsError()
    {
        _audioService.StartRecording().Returns(_ => throw new System.Exception("No mic"));
        var vm = CreateVm();

        vm.ToggleRecordingCommand.Execute(null);

        Assert.False(vm.IsRecording);
        _notificationService.Received(1).Error(Arg.Is<string>(s => s.Contains("No mic")));
    }

    [Fact]
    public void HandleFileDrop_ValidFile_SetsImportedFileInfo()
    {
        var vm = CreateVm();

        vm.HandleFileDrop("C:\\audio\\meeting.mp3");

        Assert.True(vm.HasImportedFile);
        Assert.Equal("meeting.mp3", vm.ImportedFileName);
        Assert.Equal("C:\\audio\\meeting.mp3", vm.ImportedFilePath);
    }

    [Fact]
    public void HandleFileDrop_ValidFile_FiresAudioReadyEvent()
    {
        var vm = CreateVm();
        string? receivedPath = null;
        vm.AudioReady += path => receivedPath = path;

        vm.HandleFileDrop("C:\\audio\\meeting.wav");

        Assert.Equal("C:\\audio\\meeting.wav", receivedPath);
    }

    [Fact]
    public void HandleFileDrop_ValidFile_ShowsInfoNotification()
    {
        var vm = CreateVm();

        vm.HandleFileDrop("C:\\audio\\meeting.mp3");

        _notificationService.Received(1).Info(Arg.Is<string>(s => s.Contains("meeting.mp3")));
    }

    [Fact]
    public void HandleFileDrop_InvalidFormat_ShowsWarning()
    {
        var vm = CreateVm();

        vm.HandleFileDrop("C:\\docs\\report.pdf");

        Assert.False(vm.HasImportedFile);
        _notificationService.Received(1).Warning(Arg.Any<string>());
    }

    [Fact]
    public void HandleFileDrop_InvalidFormat_DoesNotFireAudioReady()
    {
        var vm = CreateVm();
        bool eventFired = false;
        vm.AudioReady += _ => eventFired = true;

        vm.HandleFileDrop("C:\\docs\\report.txt");

        Assert.False(eventFired);
    }

    [Fact]
    public void StartRecording_ClearsImportedFile()
    {
        _audioService.StartRecording().Returns("/tmp/test.wav");
        var vm = CreateVm();

        // First import a file
        vm.HandleFileDrop("C:\\audio\\meeting.mp3");
        Assert.True(vm.HasImportedFile);

        // Then start recording
        vm.ToggleRecordingCommand.Execute(null);

        Assert.False(vm.HasImportedFile);
        Assert.Equal(string.Empty, vm.ImportedFileName);
        Assert.Null(vm.ImportedFilePath);
    }
}
