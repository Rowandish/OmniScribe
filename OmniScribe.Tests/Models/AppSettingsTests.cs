using OmniScribe.Models;
using Xunit;

namespace OmniScribe.Tests.Models;

public class AppSettingsTests
{
    [Fact]
    public void DefaultProvider_IsOpenAI()
    {
        var settings = new AppSettings();
        Assert.Equal("OpenAI", settings.Provider);
    }

    [Fact]
    public void DefaultTranscriptionModel_IsWhisper1()
    {
        var settings = new AppSettings();
        Assert.Equal("whisper-1", settings.TranscriptionModel);
    }

    [Fact]
    public void DefaultAnalysisModel_IsGpt4oMini()
    {
        var settings = new AppSettings();
        Assert.Equal("gpt-4o-mini", settings.AnalysisModel);
    }

    [Fact]
    public void DefaultApiKey_IsEmpty()
    {
        var settings = new AppSettings();
        Assert.Equal(string.Empty, settings.ApiKey);
    }

    [Fact]
    public void DefaultSystemPrompt_ContainsVerbale()
    {
        var settings = new AppSettings();
        Assert.Contains("verbale", settings.SystemPrompt.ToLowerInvariant());
    }

    [Fact]
    public void DefaultTokensUsed_IsZero()
    {
        var settings = new AppSettings();
        Assert.Equal(0, settings.TotalTokensUsed);
        Assert.Equal(0m, settings.EstimatedCost);
    }

    [Fact]
    public void AvailableProviders_ContainsExpectedValues()
    {
        var settings = new AppSettings();
        Assert.Contains("OpenAI", settings.AvailableProviders);
        Assert.Contains("Azure", settings.AvailableProviders);
        Assert.Contains("Groq", settings.AvailableProviders);
    }

    [Fact]
    public void Properties_CanBeSetAndRead()
    {
        var settings = new AppSettings
        {
            Provider = "Groq",
            ApiKey = "test-key",
            CustomEndpoint = "https://custom.api.com",
            TranscriptionModel = "whisper-large-v3",
            AnalysisModel = "gpt-4o",
            Glossary = "ACME, XYZ",
            TotalTokensUsed = 1000,
            EstimatedCost = 0.05m
        };

        Assert.Equal("Groq", settings.Provider);
        Assert.Equal("test-key", settings.ApiKey);
        Assert.Equal("https://custom.api.com", settings.CustomEndpoint);
        Assert.Equal("whisper-large-v3", settings.TranscriptionModel);
        Assert.Equal("gpt-4o", settings.AnalysisModel);
        Assert.Equal("ACME, XYZ", settings.Glossary);
        Assert.Equal(1000, settings.TotalTokensUsed);
        Assert.Equal(0.05m, settings.EstimatedCost);
    }
}
