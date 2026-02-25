using System.Collections.ObjectModel;
using OmniScribe.Models;

namespace OmniScribe.Services;

public interface INotificationService
{
    ObservableCollection<NotificationItem> Notifications { get; }
    void Show(string message, NotificationType type = NotificationType.Info, int autoHideMs = 5000);
    void Info(string message);
    void Success(string message);
    void Warning(string message);
    void Error(string message);
    void Clear();
}
