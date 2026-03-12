// ============================================================
//  ClaudeConfig.cs
//  ScriptableObject that stores all user-configurable settings.
//
//  SECURITY: The API key is intentionally NOT serialized in this
//  asset. It is stored in EditorPrefs (OS-level key/value store)
//  so it never ends up in source control.
//
//  Usage: Tools ▶ Claude Game Assistant  (auto-created on first open)
// ============================================================

using UnityEngine;
using UnityEditor;

namespace ClaudeAssistant.Core
{
    [CreateAssetMenu(
        fileName = "ClaudeConfig",
        menuName = "Claude Assistant/Configuration",
        order = 0)]
    public class ClaudeConfig : ScriptableObject
    {
        // ── EditorPrefs key ───────────────────────────────────
        // Internal — only used by this class.
        private const string ApiKeyPref = "ClaudeAssistant_ApiKey";

        // ── API Key (EditorPrefs — never serialized) ──────────

        /// <summary>
        /// Reads/writes the API key from the OS registry via EditorPrefs.
        /// The value is never stored inside the .asset file.
        /// </summary>
        public string ApiKey
        {
            get => EditorPrefs.GetString(ApiKeyPref, "");
            set => EditorPrefs.SetString(ApiKeyPref, value ?? "");
        }

        /// <summary>Clears the stored API key from EditorPrefs.</summary>
        public void ClearApiKey() => EditorPrefs.DeleteKey(ApiKeyPref);

        // ── Model settings ────────────────────────────────────

        [Header("Model")]
        [Tooltip("Model to use. Sonnet balances quality and cost; Haiku is cheaper and faster.")]
        public ModelOption model = ModelOption.Sonnet;

        [Tooltip("Maximum tokens Claude can output per response.")]
        [Range(512, 8192)]
        public int maxTokens = 4096;

        // ── Output paths ──────────────────────────────────────

        [Header("Output Paths")]
        [Tooltip("Where generated MonoBehaviour scripts are saved.")]
        public string scriptsOutputPath = "Assets/Scripts/Generated";

        [Tooltip("Where generated Editor/scene scripts are saved (auto-deleted after execution).")]
        public string editorScriptsPath = "Assets/Editor/Generated";

        // ── Behaviour ─────────────────────────────────────────

        [Header("Behaviour")]
        [Tooltip("Automatically execute generated scene scripts after saving.")]
        public bool autoExecuteSceneScripts = true;

        [Tooltip("Log every API exchange to the Unity Console.")]
        public bool verboseLogging = false;

        // ── Computed helpers ──────────────────────────────────

        public string ModelId => model switch
        {
            ModelOption.Haiku => "claude-haiku-4-5-20251001",
            ModelOption.Sonnet => "claude-sonnet-4-20250514",
            _ => "claude-sonnet-4-20250514"
        };

        /// <summary>True only when a non-empty API key is present in EditorPrefs.</summary>
        public bool IsValid => !string.IsNullOrWhiteSpace(ApiKey);
    }

    public enum ModelOption
    {
        [InspectorName("claude-haiku (fast & cheap)")]
        Haiku,
        [InspectorName("claude-sonnet (smart & balanced)")]
        Sonnet
    }
}