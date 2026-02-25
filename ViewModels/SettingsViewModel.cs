using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OmniScribe.Models;
using OmniScribe.Services;

namespace OmniScribe.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _provider = "OpenAI";

    [ObservableProperty]
    private string _apiKey = string.Empty;

    [ObservableProperty]
    private string _customEndpoint = string.Empty;

    [ObservableProperty]
    private string _transcriptionModel = "whisper-1";

    [ObservableProperty]
    private string _analysisModel = "gpt-4o-mini";

    [ObservableProperty]
    private string _systemPrompt = string.Empty;

    [ObservableProperty]
    private string _glossary = string.Empty;

    [ObservableProperty]
    private long _totalTokensUsed;

    [ObservableProperty]
    private decimal _estimatedCost;

    public List<string> AvailableProviders { get; } = new() { "OpenAI", "Azure", "Groq" };
    public List<string> TranscriptionModels { get; } = new() { "whisper-1", "whisper-large-v3", "whisper-large-v3-turbo" };
    public List<string> AnalysisModels { get; } = new() { "gpt-4o-mini", "gpt-4o", "llama-3.3-70b-versatile", "llama-3.1-8b-instant" };

    public SettingsViewModel()
    {
        LoadFromSettings();
    }

    private void LoadFromSettings()
    {
        var s = SettingsService.Instance.LoadSettings();
        Provider = s.Provider;
        ApiKey = s.ApiKey;
        CustomEndpoint = s.CustomEndpoint;
        TranscriptionModel = s.TranscriptionModel;
        AnalysisModel = s.AnalysisModel;
        SystemPrompt = s.SystemPrompt;
        Glossary = s.Glossary;
        TotalTokensUsed = s.TotalTokensUsed;
        EstimatedCost = s.EstimatedCost;
    }

    [RelayCommand]
    private void Save()
    {
        var s = new AppSettings
        {
            Provider = Provider,
            ApiKey = ApiKey,
            CustomEndpoint = CustomEndpoint,
            TranscriptionModel = TranscriptionModel,
            AnalysisModel = AnalysisModel,
            SystemPrompt = SystemPrompt,
            Glossary = Glossary,
            TotalTokensUsed = TotalTokensUsed,
            EstimatedCost = EstimatedCost
        };
        SettingsService.Instance.SaveSettings(s);
        NotificationService.Instance.Success("Impostazioni salvate.");
    }

    public AppSettings ToAppSettings()
    {
        return new AppSettings
        {
            Provider = Provider,
            ApiKey = ApiKey,
            CustomEndpoint = CustomEndpoint,
            TranscriptionModel = TranscriptionModel,
            AnalysisModel = AnalysisModel,
            SystemPrompt = SystemPrompt,
            Glossary = Glossary,
            TotalTokensUsed = TotalTokensUsed,
            EstimatedCost = EstimatedCost
        };
    }

    public void UpdateTokenUsage(long tokens, decimal cost)
    {
        TotalTokensUsed += tokens;
        EstimatedCost += cost;

        // Persist updated counters
        var s = ToAppSettings();
        SettingsService.Instance.SaveSettings(s);
    }
}
