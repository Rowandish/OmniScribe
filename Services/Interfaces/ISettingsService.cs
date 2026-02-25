using System.Collections.Generic;
using OmniScribe.Models;

namespace OmniScribe.Services;

public interface ISettingsService
{
    AppSettings LoadSettings();
    void SaveSettings(AppSettings settings);
    List<SessionRecord> LoadHistory();
    void SaveHistory(List<SessionRecord> history);
}
