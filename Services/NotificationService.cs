using System;
using System.Collections.ObjectModel;
using System.Timers;
using OmniScribe.Models;

namespace OmniScribe.Services;

public class NotificationService : INotificationService
{
    private static readonly Lazy<NotificationService> _instance = new(() => new NotificationService());
    public static NotificationService Instance => _instance.Value;

    public ObservableCollection<NotificationItem> Notifications { get; } = new();

    private NotificationService() { }

    public void Show(string message, NotificationType type = NotificationType.Info, int autoHideMs = 5000)
    {
        var item = new NotificationItem
        {
            Message = message,
            Type = type
        };

        // Use Avalonia dispatcher to ensure UI thread
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            Notifications.Add(item);

            if (autoHideMs > 0)
            {
                var timer = new Timer(autoHideMs);
                timer.Elapsed += (s, e) =>
                {
                    timer.Stop();
                    timer.Dispose();
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        Notifications.Remove(item);
                    });
                };
                timer.Start();
            }
        });
    }

    public void Info(string message) => Show(message, NotificationType.Info);
    public void Success(string message) => Show(message, NotificationType.Success);
    public void Warning(string message) => Show(message, NotificationType.Warning);
    public void Error(string message) => Show(message, NotificationType.Error, 8000);

    public void Clear()
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() => Notifications.Clear());
    }
}
