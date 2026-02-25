# ✦ OmniScribe

> **Audio → Intelligence** — Record, transcribe, and generate AI-powered meeting documents in one click.

![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)
![Avalonia UI](https://img.shields.io/badge/Avalonia_UI-11.3-7B2BFC?logo=data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iMjQiIGhlaWdodD0iMjQiPjwvc3ZnPg==&logoColor=white)
![License](https://img.shields.io/badge/License-MIT-green)
![Platform](https://img.shields.io/badge/Platform-Windows-0078D6?logo=windows&logoColor=white)

<p align="center">
  <img src="https://raw.githubusercontent.com/nicosanta/OmniScribe/main/.github/screenshot.png" alt="OmniScribe Screenshot" width="800" />
</p>

---

## 🎯 What is OmniScribe?

OmniScribe is a modern Windows desktop application that turns audio into structured, actionable documents. Whether you're recording a live meeting or importing an existing audio file, OmniScribe handles the entire pipeline:

1. 🎙️ **Capture** — Record from your microphone or drag-and-drop an audio file
2. ✂️ **Optimize** — Automatically trim silence and split large files
3. 📝 **Transcribe** — Send audio to a Whisper-compatible API for speech-to-text
4. 🤖 **Analyze** — An LLM generates structured meeting minutes, action items, and a summary
5. 📄 **Export** — Copy or save the result as a Markdown file

All of this happens with real-time progress feedback and a sleek dark UI.

---

## ✨ Features

| Feature | Description |
|---|---|
| 🎙️ **Live Recording** | One-click recording with real-time audio level meter and duration timer |
| 📂 **File Import** | Drag-and-drop support for `.wav`, `.mp3`, `.m4a`, `.ogg`, `.webm`, `.flac` |
| ✂️ **Silence Trimming** | Automatically removes dead air from the start and end of recordings |
| 📦 **Auto-Chunking** | Files over 25 MB are split into segments before upload — no manual work needed |
| 🌐 **Multi-Provider** | Works with OpenAI, Groq, Azure, or any Whisper/OpenAI-compatible endpoint |
| 🧠 **AI Analysis** | Generates structured Markdown: minutes, tasks, and summary |
| 📖 **Glossary Support** | Inject domain-specific terms to improve transcription and analysis accuracy |
| ✏️ **Custom Prompts** | Fully editable system prompt — tailor the AI's output to your needs |
| 📊 **Token & Cost Tracking** | Cumulative counter for tokens used and estimated API cost |
| 🕘 **Session History** | Browse and reload past transcriptions from the sidebar |
| 📤 **Export** | Save analysis as `.md` to your Desktop or copy to clipboard |
| 🔔 **Notifications** | Non-intrusive toast messages for every pipeline step |
| ❌ **Cancellation** | Cancel any long-running operation at any time |

---

## 🖥️ UI Preview

```
┌─────────────────────────────────────────────────────────────┐
│ ┌──────────┐ ┌────────────────────────────────────────────┐ │
│ │ SIDEBAR  │ │           MAIN CONTENT AREA                │ │
│ │          │ │                                            │ │
│ │ ✦ Omni-  │ │  ┌──────────────────────────────────────┐  │ │
│ │   Scribe │ │  │  🎙️ Recorder / Drop Zone             │  │ │
│ │          │ │  └──────────────────────────────────────┘  │ │
│ │ ──────── │ │                                            │ │
│ │ History  │ │  ┌──────────────────────────────────────┐  │ │
│ │ ┌──────┐ │ │  │  📝 Transcription                    │  │ │
│ │ │ ses1 │ │ │  └──────────────────────────────────────┘  │ │
│ │ │ ses2 │ │ │  ┌──────────────────────────────────────┐  │ │
│ │ └──────┘ │ │  │  🤖 AI Analysis (Markdown)           │  │ │
│ │          │ │  └──────────────────────────────────────┘  │ │
│ │ ⚙ Sett.  │ │                                            │ │
│ │ 📤 Export│ │                                            │ │
│ └──────────┘ └────────────────────────────────────────────┘ │
│ ● Status: Ready                        [████░░░░] Cancel   │
└─────────────────────────────────────────────────────────────┘
```

- 🌑 **Dark mode** by default with glassmorphism-inspired panels
- 💜 Purple accent (`#7A5CFF`)
- 🔤 Inter font family

---

## 🚀 Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- A valid API key for one of the supported providers (OpenAI, Groq, Azure)
- Windows 10/11 (NAudio requires Windows audio APIs)

### Build & Run

```bash
# Clone the repository
git clone https://github.com/nicosanta/OmniScribe.git
cd OmniScribe

# Build
dotnet build

# Run
dotnet run

# Publish standalone executable
dotnet publish -c Release
```

### First Launch

1. Click **⚙️ Impostazioni** (Settings) in the sidebar
2. Select your API **Provider** (OpenAI, Groq, or Azure)
3. Enter your **API Key**
4. Optionally set a **Custom Endpoint** for self-hosted APIs
5. Choose your preferred **Transcription Model** and **Analysis Model**
6. Click **💾 Salva** (Save)
7. You're ready! Hit **🔴 Rec** or drag-drop an audio file

---

## 🏗️ Architecture

OmniScribe follows the **MVVM** pattern with [Avalonia UI](https://avaloniaui.net/) and [CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/).

```
📁 OmniScribe/
├── 📁 Models/          → Data classes (AppSettings, SessionRecord, NotificationItem)
├── 📁 Services/        → Business logic & external I/O
│   ├── AudioService         → NAudio recording, silence trim, auto-chunk
│   ├── TranscriptionService → Whisper API (multipart upload, chunked files)
│   ├── AiAnalysisService    → LLM chat completion, cost estimation
│   ├── SettingsService      → JSON persistence (singleton)
│   └── NotificationService  → Toast queue (singleton)
├── 📁 ViewModels/      → UI state & orchestration
│   ├── MainWindowViewModel  → Pipeline orchestrator
│   ├── RecorderViewModel    → Recording & import state
│   ├── SettingsViewModel    → Settings bindings
│   └── HistoryViewModel     → Session list management
├── 📁 Views/           → XAML layouts + minimal code-behind
├── 📁 Converters/      → XAML value converters
└── 📁 Assets/          → Icons, resources
```

### 🔄 Processing Pipeline

```
🎙️ Record / 📂 Import
  ↓
✂️ Silence Trimming (AudioService)
  ↓
📦 Auto-Chunking if >25 MB (AudioService)
  ↓
🌐 Whisper API → Transcription (TranscriptionService)
  ↓
🤖 LLM API → Markdown Analysis (AiAnalysisService)
  ↓
💾 Save to History (SettingsService)
  ↓
📄 Display results + 🔔 Notify
```

---

## ⚙️ Configuration

All settings are stored in `%APPDATA%/OmniScribe/settings.json` — no compile-time secrets needed.

| Setting | Default | Description |
|---|---|---|
| `Provider` | `OpenAI` | API provider |
| `ApiKey` | — | Your API key (masked in UI) |
| `CustomEndpoint` | — | Override for self-hosted or Azure endpoints |
| `TranscriptionModel` | `whisper-1` | Speech-to-text model |
| `AnalysisModel` | `gpt-4o-mini` | LLM for document generation |
| `SystemPrompt` | Built-in Italian prompt | Customizable AI instructions |
| `Glossary` | — | Domain terms for improved accuracy |

### 🌐 Supported Providers

| Provider | Transcription Endpoint | Chat Endpoint |
|---|---|---|
| **OpenAI** | `api.openai.com/v1/audio/transcriptions` | `api.openai.com/v1/chat/completions` |
| **Groq** | `api.groq.com/openai/v1/audio/transcriptions` | `api.groq.com/openai/v1/chat/completions` |
| **Azure** | Custom endpoint required | Custom endpoint required |
| **Custom** | Any Whisper-compatible URL | Any OpenAI-compatible URL |

---

## 🛠️ Tech Stack

| Component | Technology |
|---|---|
| Runtime | .NET 10 |
| UI Framework | [Avalonia UI 11](https://avaloniaui.net/) |
| MVVM Toolkit | [CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/) |
| Audio | [NAudio](https://github.com/naudio/NAudio) |
| HTTP Client | [RestSharp](https://restsharp.dev/) |
| Markdown | [Markdown.Avalonia](https://github.com/whistyun/Markdown.Avalonia) |

---

## 🤝 Contributing

Contributions are welcome! Feel free to:

1. 🍴 Fork the repository
2. 🌿 Create a feature branch (`git checkout -b feature/amazing-feature`)
3. 💾 Commit your changes (`git commit -m 'Add amazing feature'`)
4. 📤 Push to the branch (`git push origin feature/amazing-feature`)
5. 🔀 Open a Pull Request

### Ideas for Contributions

- 🐧 Linux/macOS audio support (replace NAudio with cross-platform alternative)
- 🌍 Multi-language UI (i18n)
- 🎨 Light theme option
- 📊 Analytics dashboard for usage stats
- 🔌 Plugin system for custom analysis pipelines
- 🧪 Unit and integration tests

---

## 📄 License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.

---

## 💜 Acknowledgments

- [Avalonia UI](https://avaloniaui.net/) — Cross-platform .NET UI framework
- [NAudio](https://github.com/naudio/NAudio) — .NET audio library
- [OpenAI Whisper](https://openai.com/research/whisper) — Speech recognition model
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/) — MVVM made simple

---

<p align="center">
  <b>✦ OmniScribe</b> — Stop taking notes. Start listening.<br/>
  Made with 💜 and .NET
</p>
