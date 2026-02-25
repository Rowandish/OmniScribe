using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OmniScribe.Models;
using OmniScribe.Services;

namespace OmniScribe.ViewModels;

public partial class HistoryViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly INotificationService _notificationService;

    [ObservableProperty]
    private SessionRecord? _selectedSession;

    public ObservableCollection<SessionRecord> Sessions { get; } = new();

    /// <summary>
    /// Raised when a session is selected from history.
    /// </summary>
    public event Action<SessionRecord>? SessionSelected;

    public HistoryViewModel()
        : this(SettingsService.Instance, NotificationService.Instance)
    {
    }

    public HistoryViewModel(ISettingsService settingsService, INotificationService notificationService)
    {
        _settingsService = settingsService;
        _notificationService = notificationService;
        LoadHistory();
    }

    private void LoadHistory()
    {
        var records = _settingsService.LoadHistory();
        Sessions.Clear();
        foreach (var r in records.OrderByDescending(r => r.Timestamp))
            Sessions.Add(r);
    }

    public void AddSession(SessionRecord session)
    {
        Sessions.Insert(0, session);
        SaveHistory();
    }

    private void SaveHistory()
    {
        _settingsService.SaveHistory(Sessions.ToList());
    }

    [RelayCommand]
    private void SelectSession(SessionRecord? session)
    {
        if (session != null)
        {
            SelectedSession = session;
            SessionSelected?.Invoke(session);
        }
    }

    [RelayCommand]
    private void ClearHistory()
    {
        Sessions.Clear();
        SaveHistory();
        _notificationService.Info("Cronologia cancellata.");
    }
}
