using OmniScribe.Models;
using OmniScribe.Services;
using Xunit;

namespace OmniScribe.Tests.Services;

public class AiAnalysisServiceTests
{
    private readonly AiAnalysisService _service = new();

    [Fact]
    public void EstimateCost_Gpt4oMini_CalculatesCorrectly()
    {
        // gpt-4o-mini: input 0.15/1M, output 0.60/1M
        var cost = _service.EstimateCost("gpt-4o-mini", 1_000_000, 1_000_000);
        Assert.Equal(0.15m + 0.60m, cost);
    }

    [Fact]
    public void EstimateCost_Gpt4o_CalculatesCorrectly()
    {
        // gpt-4o: input 2.50/1M, output 10.00/1M
        var cost = _service.EstimateCost("gpt-4o", 1_000_000, 1_000_000);
        Assert.Equal(2.50m + 10.00m, cost);
    }

    [Fact]
    public void EstimateCost_Llama70b_CalculatesCorrectly()
    {
        // llama-3.3-70b: input 0.59/1M, output 0.79/1M
        var cost = _service.EstimateCost("llama-3.3-70b-versatile", 1_000_000, 1_000_000);
        Assert.Equal(0.59m + 0.79m, cost);
    }

    [Fact]
    public void EstimateCost_Llama8b_CalculatesCorrectly()
    {
        // llama-3.1-8b: input 0.05/1M, output 0.08/1M
        var cost = _service.EstimateCost("llama-3.1-8b-instant", 1_000_000, 1_000_000);
        Assert.Equal(0.05m + 0.08m, cost);
    }

    [Fact]
    public void EstimateCost_UnknownModel_UsesDefaultRates()
    {
        var cost = _service.EstimateCost("unknown-model", 1_000_000, 1_000_000);
        Assert.Equal(0.15m + 0.60m, cost); // defaults to gpt-4o-mini rates
    }

    [Fact]
    public void EstimateCost_ZeroTokens_ReturnsZero()
    {
        var cost = _service.EstimateCost("gpt-4o", 0, 0);
        Assert.Equal(0m, cost);
    }

    [Fact]
    public void EstimateCost_SmallTokenCount_CalculatesPrecisely()
    {
        // 1000 tokens of gpt-4o-mini input: 1000 * 0.15 / 1M = 0.00015
        var cost = _service.EstimateCost("gpt-4o-mini", 1000, 0);
        Assert.Equal(0.000150m, cost);
    }

    [Fact]
    public void GetChatEndpoint_OpenAI_ReturnsCorrectUrl()
    {
        var settings = new AppSettings { Provider = "OpenAI", CustomEndpoint = "" };
        var endpoint = _service.GetChatEndpoint(settings);
        Assert.Equal("https://api.openai.com/v1/chat/completions", endpoint);
    }

    [Fact]
    public void GetChatEndpoint_Groq_ReturnsCorrectUrl()
    {
        var settings = new AppSettings { Provider = "Groq", CustomEndpoint = "" };
        var endpoint = _service.GetChatEndpoint(settings);
        Assert.Equal("https://api.groq.com/openai/v1/chat/completions", endpoint);
    }

    [Fact]
    public void GetChatEndpoint_CustomEndpoint_AppendsPath()
    {
        var settings = new AppSettings { CustomEndpoint = "https://my-api.com/v1" };
        var endpoint = _service.GetChatEndpoint(settings);
        Assert.Equal("https://my-api.com/v1/chat/completions", endpoint);
    }

    [Fact]
    public void GetChatEndpoint_CustomEndpointWithPath_UsesAsIs()
    {
        var settings = new AppSettings { CustomEndpoint = "https://my-api.com/v1/chat/completions" };
        var endpoint = _service.GetChatEndpoint(settings);
        Assert.Equal("https://my-api.com/v1/chat/completions", endpoint);
    }

    [Fact]
    public void GetChatEndpoint_CustomEndpointWithTrailingSlash_HandlesCorrectly()
    {
        var settings = new AppSettings { CustomEndpoint = "https://my-api.com/v1/" };
        var endpoint = _service.GetChatEndpoint(settings);
        Assert.Equal("https://my-api.com/v1/chat/completions", endpoint);
    }

    [Fact]
    public void GetChatEndpoint_Azure_ThrowsNotSupported()
    {
        var settings = new AppSettings { Provider = "Azure", CustomEndpoint = "" };
        Assert.Throws<System.NotSupportedException>(() => _service.GetChatEndpoint(settings));
    }
}
