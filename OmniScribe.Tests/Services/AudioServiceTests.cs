using OmniScribe.Services;
using Xunit;

namespace OmniScribe.Tests.Services;

public class AudioServiceTests
{
    [Theory]
    [InlineData("test.wav", true)]
    [InlineData("test.mp3", true)]
    [InlineData("test.m4a", true)]
    [InlineData("test.ogg", true)]
    [InlineData("test.webm", true)]
    [InlineData("test.flac", true)]
    [InlineData("test.WAV", true)]
    [InlineData("test.Mp3", true)]
    [InlineData("test.FLAC", true)]
    [InlineData("test.txt", false)]
    [InlineData("test.pdf", false)]
    [InlineData("test.exe", false)]
    [InlineData("test.aac", false)]
    [InlineData("test", false)]
    [InlineData("", false)]
    public void IsSupportedFormat_ReturnsExpected(string filePath, bool expected)
    {
        Assert.Equal(expected, AudioService.IsSupportedFormat(filePath));
    }

    [Fact]
    public void NewAudioService_IsNotRecording()
    {
        var service = new AudioService();
        Assert.False(service.IsRecording);
    }
}
