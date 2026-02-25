using NSubstitute;
using OmniScribe.Models;
using OmniScribe.Services;
using OmniScribe.ViewModels;
using Xunit;

namespace OmniScribe.Tests.ViewModels;

public class SettingsViewModelTests
{
    private readonly ISettingsService _settingsService = Substitute.For<ISettingsService>();
    private readonly INotificationService _notificationService = Substitute.For<INotificationService>();

    private SettingsViewModel CreateVm(AppSettings? settings = null)
    {
        _settingsService.LoadSettings().Returns(settings ?? new AppSettings());
        return new SettingsViewModel(_settingsService, _notificationService);
    }

    [Fact]
    public void Constructor_LoadsSettingsFromService()
    {
        var settings = new AppSettings
        {
            Provider = "Groq",
            ApiKey = "sk-test",
            TranscriptionModel = "whisper-large-v3",
            AnalysisModel = "gpt-4o",
            Glossary = "ACME",
            TotalTokensUsed = 5000,
            EstimatedCost = 1.50m
        };

        var vm = CreateVm(settings);

        Assert.Equal("Groq", vm.Provider);
        Assert.Equal("sk-test", vm.ApiKey);
        Assert.Equal("whisper-large-v3", vm.TranscriptionModel);
        Assert.Equal("gpt-4o", vm.AnalysisModel);
        Assert.Equal("ACME", vm.Glossary);
        Assert.Equal(5000, vm.TotalTokensUsed);
        Assert.Equal(1.50m, vm.EstimatedCost);
    }

    [Fact]
    public void Save_PersistsToSettingsService()
    {
        var vm = CreateVm();
        vm.Provider = "Groq";
        vm.ApiKey = "sk-new";

        vm.SaveCommand.Execute(null);

        _settingsService.Received(1).SaveSettings(Arg.Is<AppSettings>(s =>
            s.Provider == "Groq" && s.ApiKey == "sk-new"));
    }

    [Fact]
    public void Save_ShowsSuccessNotification()
    {
        var vm = CreateVm();
        vm.SaveCommand.Execute(null);

        _notificationService.Received(1).Success(Arg.Any<string>());
    }

    [Fact]
    public void ToAppSettings_MapsAllProperties()
    {
        var vm = CreateVm();
        vm.Provider = "Azure";
        vm.ApiKey = "key";
        vm.CustomEndpoint = "https://custom.com";
        vm.TranscriptionModel = "whisper-large-v3";
        vm.AnalysisModel = "gpt-4o";
        vm.SystemPrompt = "Custom prompt";
        vm.Glossary = "Terms";
        vm.TotalTokensUsed = 100;
        vm.EstimatedCost = 0.5m;

        var result = vm.ToAppSettings();

        Assert.Equal("Azure", result.Provider);
        Assert.Equal("key", result.ApiKey);
        Assert.Equal("https://custom.com", result.CustomEndpoint);
        Assert.Equal("whisper-large-v3", result.TranscriptionModel);
        Assert.Equal("gpt-4o", result.AnalysisModel);
        Assert.Equal("Custom prompt", result.SystemPrompt);
        Assert.Equal("Terms", result.Glossary);
        Assert.Equal(100, result.TotalTokensUsed);
        Assert.Equal(0.5m, result.EstimatedCost);
    }

    [Fact]
    public void UpdateTokenUsage_AccumulatesTokensAndCost()
    {
        var vm = CreateVm();

        vm.UpdateTokenUsage(100, 0.01m);
        Assert.Equal(100, vm.TotalTokensUsed);
        Assert.Equal(0.01m, vm.EstimatedCost);

        vm.UpdateTokenUsage(200, 0.02m);
        Assert.Equal(300, vm.TotalTokensUsed);
        Assert.Equal(0.03m, vm.EstimatedCost);
    }

    [Fact]
    public void UpdateTokenUsage_PersistsToService()
    {
        var vm = CreateVm();
        vm.UpdateTokenUsage(100, 0.01m);

        // SaveSettings called once during UpdateTokenUsage
        _settingsService.Received().SaveSettings(Arg.Is<AppSettings>(s =>
            s.TotalTokensUsed == 100 && s.EstimatedCost == 0.01m));
    }

    [Fact]
    public void AvailableProviders_ContainsExpectedValues()
    {
        var vm = CreateVm();
        Assert.Contains("OpenAI", vm.AvailableProviders);
        Assert.Contains("Azure", vm.AvailableProviders);
        Assert.Contains("Groq", vm.AvailableProviders);
    }
}
