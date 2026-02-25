using System;

namespace OmniScribe.Models;

public enum NotificationType
{
    Info,
    Success,
    Warning,
    Error
}

public class NotificationItem
{
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; } = NotificationType.Info;
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public bool IsVisible { get; set; } = true;
}
