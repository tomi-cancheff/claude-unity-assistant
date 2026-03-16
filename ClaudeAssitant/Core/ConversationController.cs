// ============================================================
//  ConversationController.cs
//  Orchestrates the conversation pipeline: classifies intent,
//  delegates to the right IGenerationHandler, and updates the
//  history. The EditorWindow only calls this class — it has
//  zero knowledge of handlers or the API.
//
//  Pattern: Controller (separates UI from business logic)
// ============================================================

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ClaudeAssistant.Handlers;
using ClaudeAssistant.Models;
using ClaudeAssistant.Utils;
using UnityEngine;

namespace ClaudeAssistant.Core
{
    public class ConversationController
    {
        // ── Registered strategies ─────────────────────────────
        // Handlers are registered here — the Window never touches them directly.

        private static readonly Dictionary<GenerationMode, IGenerationHandler> _handlers = new()
        {
            { GenerationMode.Script,  new ScriptGenerationHandler() },
            { GenerationMode.Scene,   new SceneGenerationHandler()  },
            { GenerationMode.Consult, new ConsultHandler()          }
        };

        // ── Dependencies ──────────────────────────────────────

        private readonly ClaudeConfig _config;
        private readonly ConversationHistory _history;

        // ── State ─────────────────────────────────────────────

        public bool IsLoading { get; private set; }

        /// <summary>
        /// The code of the last successful generation.
        /// The Window uses this to populate the preview panel.
        /// </summary>
        public string LastCodePreview { get; private set; }

        private CancellationTokenSource _cts;

        // ── Events ────────────────────────────────────────────

        /// <summary>Fired after each completed exchange (success or failure).</summary>
        public event Action OnConversationUpdated;

        // ── Constructor ───────────────────────────────────────

        public ConversationController(ClaudeConfig config, ConversationHistory history)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _history = history ?? throw new ArgumentNullException(nameof(history));
        }

        // ── Public API ────────────────────────────────────────

        /// <summary>Read-only view of the conversation for the UI to render.</summary>
        public IReadOnlyList<ChatMessage> Messages => _history.Messages;

        /// <summary>
        /// Sends a user prompt through the full pipeline:
        /// classify → select handler → call API → update history.
        /// </summary>
        /// <param name="prompt">Raw user input.</param>
        /// <param name="artifactName">Desired script/asset name.</param>
        /// <param name="modeOverride">Force a mode; Unknown = auto-detect.</param>
        public async Task SendAsync(
            string prompt,
            string artifactName,
            GenerationMode modeOverride = GenerationMode.Unknown)
        {
            if (IsLoading || string.IsNullOrWhiteSpace(prompt)) return;

            // Cancel any previous in-flight request
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            IsLoading = true;

            // Classify
            GenerationMode mode = modeOverride != GenerationMode.Unknown
                ? modeOverride
                : IntentClassifier.Classify(prompt);

            if (mode == GenerationMode.Unknown)
                mode = GenerationMode.Consult;

            // Add user message to history before awaiting
            _history.Add(new ChatMessage(MessageRole.User, prompt, mode));
            OnConversationUpdated?.Invoke();

            try
            {
                if (!_handlers.TryGetValue(mode, out var handler))
                {
                    AddAssistantMessage($"Modo '{mode}' no tiene handler registrado.", mode);
                    return;
                }

                token.ThrowIfCancellationRequested();

                var result = await handler.HandleAsync(
                    _config,
                    _history.GetApiWindow(),
                    prompt,
                    artifactName);

                token.ThrowIfCancellationRequested();

                // Chat bubble always shows the friendly DisplayText — never raw code
                AddAssistantMessage(result.Success ? result.DisplayText : result.DisplayText, mode);

                if (result.Success)
                {
                    // Expose code preview for the Window panel
                    if (!string.IsNullOrEmpty(result.CodePreview))
                        LastCodePreview = result.CodePreview;

                    if (!string.IsNullOrEmpty(result.ArtifactPath))
                        Debug.Log($"[ClaudeAssistant] ✅ Artifact: {result.ArtifactPath}");
                }
            }
            catch (OperationCanceledException)
            {
                // Silently discard — user cancelled or window was closed
                Debug.Log("[ClaudeAssistant] Request cancelled.");
            }
            catch (Exception ex)
            {
                AddAssistantMessage($"❌ Error: {ex.Message}", mode);
                Debug.LogError($"[ClaudeAssistant] {ex}");
            }
            finally
            {
                IsLoading = false;
                OnConversationUpdated?.Invoke();
            }
        }

        /// <summary>Cancels any in-flight API request.</summary>
        public void Cancel()
        {
            _cts?.Cancel();
            IsLoading = false;
        }

        /// <summary>Clears the full conversation history.</summary>
        public void ClearHistory() => _history.Clear();

        /// <summary>
        /// Restores the last code preview after a domain reload.
        /// Called by the Window during OnEnable.
        /// </summary>
        public void RestoreCodePreview(string code) => LastCodePreview = code;

        // ── Private helpers ───────────────────────────────────

        private void AddAssistantMessage(string content, GenerationMode mode)
        {
            _history.Add(new ChatMessage(MessageRole.Assistant, content, mode));
            OnConversationUpdated?.Invoke();
        }
    }
}