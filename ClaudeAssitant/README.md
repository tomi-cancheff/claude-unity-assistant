# 🤖 Claude Game Assistant for Unity

> A Unity Editor tool that integrates the **Claude API (Anthropic)** to prototype game mechanics, generate C# scripts and build scenes from natural language prompts — without leaving the Editor.

![Unity](https://img.shields.io/badge/Unity-6.0%2B-black?logo=unity)
![C#](https://img.shields.io/badge/C%23-239120?logo=csharp&logoColor=white)
![License](https://img.shields.io/badge/license-MIT-green)
![Status](https://img.shields.io/badge/status-active-brightgreen)

---

## 📸 Overview

<p align="center">
  <img src="docs/images/ClaudeAssitant%20Unity.png" alt="Claude Game Assistant Window" width="340"/>
</p>

Open the assistant from the Unity toolbar and start prototyping immediately.

<p align="center">
  <img src="docs/images/Captura%20de%20pantalla%202026-03-09%20154502.png" alt="Tools Menu" width="480"/>
</p>

---

## ✨ Features

| Feature | Description |
|---|---|
| 📄 **Script Mode** | Generates one or more coherent MonoBehaviour scripts per prompt |
| 🏗️ **Scene Mode** | Builds GameObjects and layouts directly in the active scene |
| 🔁 **Multi-script generation** | Request full systems (FSM, Spawners, etc.) — up to 5 scripts per prompt |
| 💬 **Persistent chat history** | Survives Unity recompilations via `[SerializeField]` on the EditorWindow |
| 🔒 **Secure API Key storage** | Stored in `EditorPrefs`, never serialized into project or `.asset` files |
| ↩️ **Full Undo support** | Every generated scene object is registered with `Undo.RegisterCreatedObjectUndo` |
| 💾 **Code preview panel** | Collapsible panel with the last generated code and a one-click copy button |
| 🎯 **Intent classification** | Automatically detects whether the prompt targets a script or a scene |
| 💬 **Consult Mode** | Ask Unity/C# questions in natural language without generating files |
| 🌐 **Bilingual support** | Toggle between Spanish and English — UI and AI responses update instantly |

---

## 📦 Installation

### Option A — Unity Package *(recommended)*

1. Download `ClaudeAssistant_v1.0.unitypackage` from the [latest release](../../releases/latest)
2. In Unity: **Assets → Import Package → Custom Package**
3. Select the downloaded file and click **Import**

### Option B — Manual

1. Clone or download this repository
2. Copy the `ClaudeAssistant` folder into `Assets/Editor/` of your Unity project

### Requirements

- **Unity 6.0 or later *(developed and tested on Unity 6.3)*
- **Anthropic API Key** — get one at [console.anthropic.com](https://console.anthropic.com)
- A minimum $5 API credit *(one-time, not a subscription)*

---

## 🚀 Getting Started

1. Open the assistant: **Tools → Claude Game Assistant** or `Ctrl+Shift+C`
2. Enter your API key in the **API Key** field — stored securely in `EditorPrefs`, never written to project files
3. Type your prompt and click **✨ Enviar** / **✨ Send**

The tool auto-detects whether you want a **script**, a **scene**, or a **consultation**. You can override this with the **Mode** dropdown.

---

## 💡 Example Prompts

### Script Mode

```
Script de controlador de jugador con movimiento, salto y coyote time
```
```
Sistema de vida con daño, invencibilidad temporal y evento onDeath
```
```
Singleton GameManager con estados: Menu, Playing, Paused, GameOver
```

### Multi-script Systems

```
Genera un sistema de máquina de estados con 2 estados diferentes,
"Patrol" y "Pursue" para un enemigo. Hazlo en 3 scripts separados.
```

<p align="center">
  <img src="docs/images/MultiScripting.png" alt="Multi-script generation example" width="380"/>
</p>

<p align="center">
  <img src="docs/images/ConsultMode.png" alt="Consult Mode - Unity Q&A" width="380"/>
</p>

*The assistant generates all files at once, names them correctly, and tells you exactly where they were saved.*

## 💬 Consult Mode

Ask Unity and C# questions directly in the chat without generating any files.
Auto-detected from conversational input — no need to change the mode manually.

Supported topics: light baking, Occlusion Culling, NavMesh, Profiler, ScriptableObjects,
optimization, physics, animations, design patterns and general C# best practices.

Examples:
- "How do I bake lights in Unity 6?"
- "What's the difference between Update and FixedUpdate?"
- "How can I assign a ScriptableObject via script?"
- "When should I use object pooling?"

## 🌐 Language Support

The assistant supports Spanish and English. Use the 🌐 toggle button in the toolbar
to switch languages. The selection persists across Unity sessions via EditorPrefs.

- **UI language** — all labels, buttons and messages switch instantly
- **Claude's response language** — the system prompt updates so Claude responds
  in the selected language
- **Default** — Spanish

<p align="center">
  <img src="docs/images/LanguageToggle.png" alt="Language Toggle EN/ES" width="380"/>
</p>

### Scene Mode

```
Armá una habitación 3D de 12x8 con paredes, piso y techo
```
```
Laberinto top-down de 10x10 con paredes de cubos y un punto de entrada
```

---

## ⚙️ Configuration

Select `Assets/Editor/ClaudeAssistant/ClaudeConfig` to adjust settings:

<p align="center">
  <img src="docs/images/ClaudeConfig.png" alt="Claude Config Inspector" width="480"/>
</p>

| Setting | Default | Description |
|---|---|---|
| Api Key | *(empty)* | Enter once — stored in `EditorPrefs`, never in the project |
| Model | `claude-sonnet` | Claude model to use |
| Max Tokens | `4096` | Max response length |
| Scripts Output Path | `Assets/Scripts/Generated` | Where generated `.cs` files are saved |
| Editor Scripts Path | `Assets/Editor/Generated` | Where scene builder scripts are saved |
| Auto Execute Scene Scripts | ✅ | Runs scene scripts automatically after compilation |
| Verbose Logging | ✅ | Logs API requests to the Console |

---

## 🏗️ Architecture

<p align="center">
  <img src="docs/images/ClaudeStructure.png" alt="Project Structure" width="320"/>
</p>

The tool follows a layered architecture with clear separation of concerns:

```
Assets/Editor/ClaudeAssistant/
├── Core/
│   ├── ClaudeApiClient.cs          — Singleton HTTP client (async/await + CancellationToken)
│   ├── ClaudeConfig.cs             — ScriptableObject config (API key via EditorPrefs)
│   ├── ConversationController.cs   — Orchestrates the full generation pipeline
│   ├── ConversationHistory.cs      — Chat state + API context window trimming
│   └── PersistedMessage.cs         — Serializable DTO for domain-reload persistence
├── Handlers/
│   ├── IGenerationHandler.cs       — Strategy interface
│   ├── SceneGenerationConstants.cs — Shared path/class name constants
│   ├── SceneGenerationHandler.cs   — Builds GameObjects in the active scene
│   └── ScriptGenerationHandler.cs  — Generates and saves .cs files
├── Models/
│   ├── ChatMessage.cs              — Immutable conversation turn model
│   ├── ClaudeApiModels.cs          — API request/response serialization
│   └── GenerationResult.cs         — Factory result pattern (Ok / Fail)
├── Tests/
│   └── IntentClassifierTests.cs    — NUnit EditMode tests
├── UI/
│   └── ClaudeAssistantWindow.cs    — Pure UI layer (no business logic)
└── Utils/
    ├── CodeExtractor.cs            — Parses single and multi-script responses
    └── IntentClassifier.cs         — Keyword-based intent detection
```

### Design Patterns Applied

| Pattern | Where | Why |
|---|---|---|
| **Strategy** | `IGenerationHandler` | Swap Script/Scene generation without changing the pipeline |
| **Singleton** | `ClaudeApiClient` | Single shared HTTP client across all handlers |
| **Repository** | `ConversationHistory` | Encapsulates history state and API window trimming |
| **Controller** | `ConversationController` | Separates business logic from the UI layer |
| **Factory** | `GenerationResult.Ok() / .Fail()` | Expressive, safe result construction |
| **ScriptableObject** | `ClaudeConfig` | Editor-friendly config that survives across sessions |
| **DTO** | `PersistedMessage` | Mirrors `ChatMessage` as a Unity-serializable type for reload persistence |

---

## 🔒 Security

- The API key is stored in **`EditorPrefs`** (OS-level key-value store), never in the `.asset` file
- `ClaudeConfig.asset` is excluded from version control via `.gitignore`
- Clear the stored key at any time with the **✕** button in the window toolbar

---

## 🧠 Multi-Script Generation

When requesting a system with multiple components, Claude generates each script in its own fenced block with a filename hint:

```csharp
// FileName: StateMachine.cs
public class StateMachine : MonoBehaviour { ... }
```

The tool parses all blocks, saves each as a separate `.cs` file, and displays a full summary in the chat. **Maximum 5 scripts per prompt** — if the system requires more, the assistant will tell you to split the request.

---

## 🧪 Running Tests

1. **Window → General → Test Runner**
2. Select the **EditMode** tab
3. Click **Run All**

Tests cover intent classification for Script / Scene / Unknown prompts including edge cases and tie-breaking behavior.

---

## 🗺️Possible Roadmap

- [ ] NavMesh integration in generated enemy AI scripts
- [ ] Per-handler unit test coverage
- [ ] Network error retry with exponential backoff
- [ ] Prefab generation mode
- [ ] Custom prompt templates saved per project

---

## 📄 License

MIT — free to use, modify and distribute. Attribution appreciated but not required.

---

## 🤝 Contributing

Instructions: If you add a new `IGenerationHandler`, follow the existing pattern:

1. Implement `IGenerationHandler`
2. Return `GenerationResult.Ok()` or `GenerationResult.Fail()` — never throw from a handler
3. Register the handler in `ConversationController`
4. Add keyword coverage in `IntentClassifier` if it needs auto-detection
