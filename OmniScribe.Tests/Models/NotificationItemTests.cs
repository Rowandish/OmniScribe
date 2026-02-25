using OmniScribe.Models;
using Xunit;

namespace OmniScribe.Tests.Models;

public class NotificationItemTests
{
    [Fact]
    public void DefaultType_IsInfo()
    {
        var item = new NotificationItem();
        Assert.Equal(NotificationType.Info, item.Type);
    }

    [Fact]
    public void DefaultIsVisible_IsTrue()
    {
        var item = new NotificationItem();
        Assert.True(item.IsVisible);
    }

    [Fact]
    public void DefaultMessage_IsEmpty()
    {
        var item = new NotificationItem();
        Assert.Equal(string.Empty, item.Message);
    }

    [Fact]
    public void Properties_CanBeSetAndRead()
    {
        var item = new NotificationItem
        {
            Message = "Test error",
            Type = NotificationType.Error,
            IsVisible = false
        };

        Assert.Equal("Test error", item.Message);
        Assert.Equal(NotificationType.Error, item.Type);
        Assert.False(item.IsVisible);
    }

    [Theory]
    [InlineData(NotificationType.Info)]
    [InlineData(NotificationType.Success)]
    [InlineData(NotificationType.Warning)]
    [InlineData(NotificationType.Error)]
    public void AllNotificationTypes_AreValid(NotificationType type)
    {
        var item = new NotificationItem { Type = type };
        Assert.Equal(type, item.Type);
    }
}
