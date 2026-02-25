using System;
using System.Collections.Generic;
using NSubstitute;
using OmniScribe.Models;
using OmniScribe.Services;
using OmniScribe.ViewModels;
using Xunit;

namespace OmniScribe.Tests.ViewModels;

public class HistoryViewModelTests
{
    private readonly ISettingsService _settingsService = Substitute.For<ISettingsService>();
    private readonly INotificationService _notificationService = Substitute.For<INotificationService>();

    private HistoryViewModel CreateVm(List<SessionRecord>? history = null)
    {
        _settingsService.LoadHistory().Returns(history ?? new List<SessionRecord>());
        return new HistoryViewModel(_settingsService, _notificationService);
    }

    [Fact]
    public void Constructor_LoadsHistoryFromService()
    {
        var records = new List<SessionRecord>
        {
            new() { Timestamp = DateTime.Now.AddMinutes(-10), SourceFileName = "a.mp3" },
            new() { Timestamp = DateTime.Now, SourceFileName = "b.mp3" }
        };

        var vm = CreateVm(records);

        Assert.Equal(2, vm.Sessions.Count);
        // Should be ordered by most recent first
        Assert.Equal("b.mp3", vm.Sessions[0].SourceFileName);
        Assert.Equal("a.mp3", vm.Sessions[1].SourceFileName);
    }

    [Fact]
    public void Constructor_EmptyHistory_HasNoSessions()
    {
        var vm = CreateVm();
        Assert.Empty(vm.Sessions);
    }

    [Fact]
    public void AddSession_InsertsAtTop()
    {
        var vm = CreateVm();
        var session1 = new SessionRecord { SourceFileName = "first.mp3" };
        var session2 = new SessionRecord { SourceFileName = "second.mp3" };

        vm.AddSession(session1);
        vm.AddSession(session2);

        Assert.Equal(2, vm.Sessions.Count);
        Assert.Equal("second.mp3", vm.Sessions[0].SourceFileName);
        Assert.Equal("first.mp3", vm.Sessions[1].SourceFileName);
    }

    [Fact]
    public void AddSession_PersistsToService()
    {
        var vm = CreateVm();
        vm.AddSession(new SessionRecord { SourceFileName = "test.mp3" });

        _settingsService.Received(1).SaveHistory(Arg.Is<List<SessionRecord>>(h => h.Count == 1));
    }

    [Fact]
    public void SelectSession_FiresSessionSelectedEvent()
    {
        var vm = CreateVm();
        SessionRecord? selectedSession = null;
        vm.SessionSelected += s => selectedSession = s;

        var session = new SessionRecord { SourceFileName = "test.mp3" };
        vm.SelectSessionCommand.Execute(session);

        Assert.NotNull(selectedSession);
        Assert.Equal("test.mp3", selectedSession!.SourceFileName);
    }

    [Fact]
    public void SelectSession_SetsSelectedSession()
    {
        var vm = CreateVm();
        var session = new SessionRecord { SourceFileName = "test.mp3" };

        vm.SelectSessionCommand.Execute(session);

        Assert.Equal(session, vm.SelectedSession);
    }

    [Fact]
    public void SelectSession_WithNull_DoesNotFireEvent()
    {
        var vm = CreateVm();
        bool eventFired = false;
        vm.SessionSelected += _ => eventFired = true;

        vm.SelectSessionCommand.Execute(null);

        Assert.False(eventFired);
    }

    [Fact]
    public void ClearHistory_RemovesAllSessions()
    {
        var records = new List<SessionRecord>
        {
            new() { SourceFileName = "a.mp3" },
            new() { SourceFileName = "b.mp3" }
        };
        var vm = CreateVm(records);

        vm.ClearHistoryCommand.Execute(null);

        Assert.Empty(vm.Sessions);
    }

    [Fact]
    public void ClearHistory_PersistsEmptyList()
    {
        var vm = CreateVm(new List<SessionRecord> { new() });

        vm.ClearHistoryCommand.Execute(null);

        _settingsService.Received().SaveHistory(Arg.Is<List<SessionRecord>>(h => h.Count == 0));
    }

    [Fact]
    public void ClearHistory_ShowsNotification()
    {
        var vm = CreateVm();

        vm.ClearHistoryCommand.Execute(null);

        _notificationService.Received(1).Info(Arg.Any<string>());
    }
}
