using OmniScribe.Models;
using OmniScribe.Services;
using Xunit;

namespace OmniScribe.Tests.Services;

public class TranscriptionServiceTests
{
    private readonly TranscriptionService _service = new();

    [Fact]
    public void GetTranscriptionEndpoint_OpenAI_ReturnsCorrectUrl()
    {
        var settings = new AppSettings { Provider = "OpenAI", CustomEndpoint = "" };
        var endpoint = _service.GetTranscriptionEndpoint(settings);
        Assert.Equal("https://api.openai.com/v1/audio/transcriptions", endpoint);
    }

    [Fact]
    public void GetTranscriptionEndpoint_Groq_ReturnsCorrectUrl()
    {
        var settings = new AppSettings { Provider = "Groq", CustomEndpoint = "" };
        var endpoint = _service.GetTranscriptionEndpoint(settings);
        Assert.Equal("https://api.groq.com/openai/v1/audio/transcriptions", endpoint);
    }

    [Fact]
    public void GetTranscriptionEndpoint_CustomEndpoint_AppendsPath()
    {
        var settings = new AppSettings { CustomEndpoint = "https://my-whisper.com/v1" };
        var endpoint = _service.GetTranscriptionEndpoint(settings);
        Assert.Equal("https://my-whisper.com/v1/audio/transcriptions", endpoint);
    }

    [Fact]
    public void GetTranscriptionEndpoint_CustomEndpointWithPath_UsesAsIs()
    {
        var settings = new AppSettings { CustomEndpoint = "https://my-whisper.com/v1/audio/transcriptions" };
        var endpoint = _service.GetTranscriptionEndpoint(settings);
        Assert.Equal("https://my-whisper.com/v1/audio/transcriptions", endpoint);
    }

    [Fact]
    public void GetTranscriptionEndpoint_CustomEndpointWithTrailingSlash_HandlesCorrectly()
    {
        var settings = new AppSettings { CustomEndpoint = "https://my-whisper.com/v1/" };
        var endpoint = _service.GetTranscriptionEndpoint(settings);
        Assert.Equal("https://my-whisper.com/v1/audio/transcriptions", endpoint);
    }

    [Fact]
    public void GetTranscriptionEndpoint_Azure_ThrowsNotSupported()
    {
        var settings = new AppSettings { Provider = "Azure", CustomEndpoint = "" };
        Assert.Throws<System.NotSupportedException>(() => _service.GetTranscriptionEndpoint(settings));
    }
}
