# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**OmniScribe** is a modern Windows desktop application built with .NET 10 and Avalonia UI. It serves as a professional tool for recording ambient audio or importing pre-existing audio files, transcribing them via Whisper-compatible APIs, and analyzing the resulting text through LLMs to produce structured meeting minutes (verbali), action items (task), and summaries (sintesi). The entire UI and default prompts are in **Italian**.

### Vision

OmniScribe automates the manual work of taking meeting notes: the user presses Record (or drops an audio file), and the system handles audio optimization, cloud-based speech-to-text, and AI-powered document generation — all in a single pipeline with real-time status feedback.

### Target Users

Professionals who attend meetings, interviews, or workshops and need structured, actionable documentation generated automatically — secretaries, project managers, consultants, researchers.

## Build & Run

```bash
dotnet build                    # Debug build
dotnet build -c Release         # Release build
dotnet run                      # Run the app
dotnet publish -c Release       # Publish standalone executable
```

No test project exists. There is no README or CI/CD pipeline.

## Tech Stack

| Component | Technology | Version |
|---|---|---|
| Runtime | .NET | 10.0 |
| UI Framework | Avalonia UI | 11.3.12 |
| Theme | Avalonia.Themes.Fluent | 11.3.12 |
| Font | Avalonia.Fonts.Inter | 11.3.12 |
| MVVM Toolkit | CommunityToolkit.Mvvm | 8.4.0 |
| Markdown Rendering | Markdown.Avalonia | 11.0.2 |
| Audio Capture/Processing | NAudio | 2.2.1 |
| HTTP Client | RestSharp | 113.1.0 |

## Architecture

**Pattern:** MVVM with Avalonia UI 11 (compiled XAML bindings, Fluent theme, Dark Mode).
**Toolkit:** CommunityToolkit.Mvvm — use `[ObservableProperty]` and `[RelayCommand]` attributes; avoid manual INotifyPropertyChanged boilerplate.

### Layers

| Layer | Location | Role |
|---|---|---|
| Models | `Models/` | Plain data: `AppSettings`, `SessionRecord`, `NotificationItem` |
| Services | `Services/` | All business logic and external I/O |
| ViewModels | `ViewModels/` | UI state; orchestrate services |
| Views | `Views/` | XAML + minimal code-behind |
| Converters | `Converters/ValueConverters.cs` | XAML binding converters |

### File Map

```
OmniScribe/
├── Models/
│   ├── AppSettings.cs          # User preferences, API config, model choices, token counters
│   ├── SessionRecord.cs        # One transcription+analysis session (id, timestamp, texts, cost)
│   └── NotificationItem.cs     # Toast notification data (message, type, visibility)
├── Services/
│   ├── AudioService.cs         # NAudio recording, silence trimming, auto-chunking
│   ├── TranscriptionService.cs # Whisper API multipart upload, chunked file support
│   ├── AiAnalysisService.cs    # LLM chat completion, cost estimation
│   ├── SettingsService.cs      # Singleton; JSON persistence (settings + history)
│   └── NotificationService.cs  # Singleton; auto-dismissing toast queue
├── ViewModels/
│   ├── ViewModelBase.cs        # Base class (ObservableObject)
│   ├── MainWindowViewModel.cs  # Orchestrator: wires pipeline, manages status bar & results
│   ├── RecorderViewModel.cs    # Recording state, audio level, drag-drop import
│   ├── SettingsViewModel.cs    # Settings panel bindings, save/load, token tracking
│   └── HistoryViewModel.cs     # Session list, selection, persistence
├── Views/
│   ├── MainWindow.axaml(.cs)   # Root layout: sidebar + main content + status bar + notifications
│   ├── RecorderView.axaml(.cs) # Record button, level meter, drag-drop zone
│   ├── SettingsView.axaml(.cs) # API config form
│   ├── HistoryView.axaml(.cs)  # Session list in sidebar
│   └── NotificationView.axaml(.cs) # Toast overlay (top-right)
├── Converters/
│   └── ValueConverters.cs      # XAML binding converters
├── App.axaml(.cs)              # Application entry, theme setup
├── Program.cs                  # Main entry point
└── ViewLocator.cs              # ViewModel→View resolution
```

### Services (detailed)

- **AudioService** — Records from the default input device via NAudio at 16 kHz / 16-bit / mono (optimal for Whisper). Provides real-time audio level (peak amplitude) and recording duration via events. Includes:
  - `TrimSilenceAsync()` — Removes prolonged silence from start/end, keeping 250ms margin.
  - `AutoChunkAsync()` — Splits files exceeding 25 MB into smaller WAV segments for API upload.
  - `IsSupportedFormat()` — Validates file extension (.wav, .mp3, .m4a, .ogg, .webm, .flac).

- **TranscriptionService** — Sends audio files to a Whisper-compatible endpoint via HTTP multipart/form-data (RestSharp). Supports chunked uploads — transcribes each segment sequentially and concatenates results. Uses `verbose_json` response format. Injects the glossary as a `prompt` parameter to guide Whisper's vocabulary. Endpoints: OpenAI (`/v1/audio/transcriptions`), Groq, or custom URL.

- **AiAnalysisService** — Sends the transcript to an LLM via OpenAI-compatible chat completion API. The system prompt instructs the model to produce a structured Markdown document with: (1) verbale (minutes), (2) task list, (3) summary. The glossary is appended to the system prompt for domain-specific terminology. Includes per-model cost estimation (GPT-4o-mini, GPT-4o, LLaMA variants). Temperature: 0.3, max_tokens: 4096.

- **SettingsService** — Thread-safe singleton. Persists `settings.json` (API config, prompts, token counters) and `history.json` (past sessions) under `%APPDATA%/OmniScribe/`. Uses `System.Text.Json` with indented formatting.

- **NotificationService** — Thread-safe singleton. Manages an `ObservableCollection<NotificationItem>` bound to the UI overlay. Auto-dismisses notifications after 5s (Info/Success/Warning) or 8s (Error). All mutations dispatched to UI thread.

### Models (detailed)

- **AppSettings** — Provider (`OpenAI`/`Azure`/`Groq`), ApiKey, CustomEndpoint, TranscriptionModel, AnalysisModel, SystemPrompt, Glossary, TotalTokensUsed, EstimatedCost. Also holds lists of available providers/models for UI dropdowns.

- **SessionRecord** — Id (8-char GUID prefix), Timestamp, SourceFileName, TranscriptionText, AnalysisResult, TokensUsed, Cost. `DisplayName` computed property for sidebar display.

- **NotificationItem** — Message, Type (Info/Success/Warning/Error), Timestamp, IsVisible.

### Processing Pipeline (happy path)

```
User presses Record or drops an audio file
  → RecorderViewModel raises AudioReady event with file path
  → MainWindowViewModel.ProcessAudioPipelineAsync() orchestrates:
    1. AudioService.TrimSilenceAsync()     → removes silence (fallback: use original)
    2. AudioService.AutoChunkAsync()       → splits if >25 MB
    3. TranscriptionService.TranscribeAsync() → Whisper API → concatenated transcript
    4. AiAnalysisService.AnalyzeAsync()    → LLM API → Markdown analysis
    5. SettingsService (persist SessionRecord to history.json)
    6. HistoryViewModel (insert at top of sidebar list)
    7. NotificationService (success toast)
  → Status bar shows real-time progress: "Ottimizzazione audio..." → "Trascrizione chunk 1/N..." → "L'IA sta elaborando il verbale..." → "Completato — X token (~$Y)"
  → Results displayed: raw transcript (TextBox) + formatted analysis (MarkdownScrollViewer)
  → User can Copy, Export to .md, or Cancel at any point
```

### UI Layout

```
┌─────────────────────────────────────────────────────────────┐
│ ┌──────────┐ ┌────────────────────────────────────────────┐ │
│ │ SIDEBAR  │ │          MAIN CONTENT AREA                 │ │
│ │          │ │                                            │ │
│ │ ✦ Omni-  │ │  ┌──────────────────────────────────────┐  │ │
│ │   Scribe │ │  │  RECORDER (Rec button, level meter,  │  │ │
│ │          │ │  │  drag-drop zone, duration timer)      │  │ │
│ │ ─────────│ │  └──────────────────────────────────────┘  │ │
│ │          │ │                                            │ │
│ │ History  │ │  ┌──────────────────────────────────────┐  │ │
│ │ ┌──────┐ │ │  │  📝 Trascrizione (read-only TextBox) │  │ │
│ │ │ ses1 │ │ │  └──────────────────────────────────────┘  │ │
│ │ │ ses2 │ │ │                                            │ │
│ │ │ ses3 │ │ │  ┌──────────────────────────────────────┐  │ │
│ │ └──────┘ │ │  │  🤖 Analisi AI (Markdown rendered)   │  │ │
│ │          │ │  │  [📋 Copia]                          │  │ │
│ │          │ │  └──────────────────────────────────────┘  │ │
│ │ ──────── │ │                                            │ │
│ │ ⚙ Impost │ │  OR: ⚙ Settings Panel (overlay)          │ │
│ │ 📤 Export│ │                                            │ │
│ └──────────┘ └────────────────────────────────────────────┘ │
│ ┌──────────────────────────────────────────────────────────┐│
│ │ ● Status text                          [████░░] Cancel  ││
│ └──────────────────────────────────────────────────────────┘│
│                              ┌─────────────────┐           │
│                              │ 🔔 Notifications│ (overlay) │
│                              │    (top-right)   │           │
│                              └─────────────────┘           │
└─────────────────────────────────────────────────────────────┘
```

- **Dark Mode** by default (background `#0B0E14`, sidebar `#0F1219`).
- **Glassmorphism** style panels with `glass-panel` CSS class.
- Accent color: `#7A5CFF` (purple).
- Window: 1200x780, min 900x600, extends client area for custom chrome.

### Key Conventions

- Services are **instantiated directly** in ViewModels (no DI container).
- All UI updates must use `Dispatcher.UIThread.InvokeAsync(...)` or `Dispatcher.UIThread.Post(...)`.
- Long-running operations accept a `CancellationToken` and check it throughout.
- HTTP calls use **RestSharp**; all API base URLs and model names are configurable via `AppSettings`.
- Supported audio imports: `.wav`, `.mp3`, `.m4a`, `.ogg`, `.webm`, `.flac`.
- Inter-component communication uses C# events (e.g., `AudioReady`, `SessionSelected`, `AudioLevelChanged`).
- Recording is stored as temp files under `%TEMP%/OmniScribe/`.
- Export saves `.md` files to the user's Desktop.

## Configuration

Runtime settings live in `%APPDATA%/OmniScribe/settings.json` (loaded by `SettingsService`). The model exposes:

| Setting | Default | Description |
|---|---|---|
| Provider | `"OpenAI"` | API provider (OpenAI, Azure, Groq) |
| ApiKey | `""` | API key (masked in UI) |
| CustomEndpoint | `""` | Override base URL for self-hosted or Azure endpoints |
| TranscriptionModel | `"whisper-1"` | Speech-to-text model |
| AnalysisModel | `"gpt-4o-mini"` | LLM for analysis |
| SystemPrompt | Italian secretary prompt | Instructions for the AI analyst |
| Glossary | `""` | Domain terms to guide both Whisper and the LLM |
| TotalTokensUsed | `0` | Cumulative token counter |
| EstimatedCost | `0` | Cumulative cost estimate (USD) |

No compile-time secrets or config files. Session history is persisted separately in `%APPDATA%/OmniScribe/history.json`.

## Functional Features

### Dual Input Mode
- **Live Recording**: Record button with visual feedback (audio level meter, duration timer). NAudio captures at 16 kHz/16-bit/mono.
- **File Import**: Drag-and-drop zone accepting .wav, .mp3, .m4a, .ogg, .webm, .flac.

### Intelligent Audio Processing
- **Silence Trimming**: Automatically removes prolonged silence from start/end of recordings (250ms margin preserved).
- **Auto-Chunking**: Files exceeding 25 MB are automatically split into WAV segments before API upload, with user notification.

### AI Analysis Pipeline
1. **Transcription** via Whisper-compatible API (supports glossary/prompt injection for domain accuracy).
2. **Analysis** via LLM chat completion — produces Markdown with: structured minutes, extracted action items, concise summary.
3. **Real-time status bar** with descriptive Italian messages and progress bar (0-100%).

### Settings Panel (Power User)
- API provider selection (OpenAI, Azure, Groq) with custom endpoint support.
- Model selectors for transcription and analysis.
- Editable system prompt for customizing AI behavior.
- Glossary/context field for domain-specific terminology.
- Cumulative token counter and cost estimate display.

### Session History
- Sidebar list of past sessions, ordered by recency.
- Click to reload transcript + analysis in the main view.
- Clear history option.

### Output & Export
- Copy analysis to clipboard.
- Export analysis as `.md` file to Desktop.
- In-app Markdown rendering with `Markdown.Avalonia`.

### Notifications
- Auto-dismissing toast notifications (top-right overlay).
- Types: Info (5s), Success (5s), Warning (5s), Error (8s).

### Cancellation
- Any pipeline step can be cancelled via the status bar cancel button.
- Uses `CancellationToken` propagated through the entire pipeline.
