// ============================================================
//  ClaudeAssistantWindow.cs
//  Pure UI layer. Draws the chat interface and delegates ALL
//  business logic to ConversationController.
//
//  Responsibilities of this class:
//    ✅ Render the chat history
//    ✅ Capture user input
//    ✅ Call _controller.SendAsync()
//    ❌ No API calls
//    ❌ No handler selection
//    ❌ No history management
//
//  Pattern: thin View delegating to a Controller
// ============================================================

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ClaudeAssistant.Core;
using ClaudeAssistant.Models;

namespace ClaudeAssistant.UI
{
    public class ClaudeAssistantWindow : EditorWindow
    {
        // ── Menu entry ────────────────────────────────────────

        [MenuItem("Tools/Claude Game Assistant %#c")]
        public static void Open() => GetWindow<ClaudeAssistantWindow>("🤖 Claude Assistant");

        // ── Persistence across domain reloads ─────────────────

        [SerializeField] private List<PersistedMessage> _persistedMessages    = new();
        [SerializeField] private string                 _persistedCodePreview = "";
        [SerializeField] private string                 _persistedArtifactPath = "";

        // ── Dependencies ──────────────────────────────────────

        private ClaudeConfig           _config;
        private ConversationController _controller;

        // ── UI state ──────────────────────────────────────────

        private string         _prompt       = "";
        private string         _scriptName   = "MyGeneratedScript";
        private GenerationMode _modeOverride = GenerationMode.Unknown;
        private GenerationMode _lastSentMode = GenerationMode.Unknown;
        private Vector2        _chatScroll;
        private Vector2        _codeScroll;

        // ── Prompt history (↑↓ navigation) ───────────────────

        private readonly List<string> _promptHistory = new();
        private int                   _historyIndex  = -1;
        private const int             MAX_HISTORY    = 20;

        // ── Styles (lazily initialized) ───────────────────────

        private GUIStyle _userBubble;
        private GUIStyle _assistantBubble;
        private GUIStyle _labelSmall;
        private bool     _stylesInitialized;

        // ── Unity lifecycle ───────────────────────────────────

        private void OnEnable()
        {
            _config = LoadOrCreateConfig();

            var history = new ConversationHistory();
            foreach (var pm in _persistedMessages)
                history.Add(pm.ToChatMessage());

            _controller = new ConversationController(_config, history);
            _controller.OnConversationUpdated += OnConversationUpdated;

            if (!string.IsNullOrEmpty(_persistedCodePreview))
                _controller.RestoreCodePreview(_persistedCodePreview);

            if (!string.IsNullOrEmpty(_persistedArtifactPath))
                _controller.RestoreArtifactPath(_persistedArtifactPath);

            minSize = new Vector2(420, 580);
        }

        private void OnDisable()
        {
            _controller?.Cancel();
            if (_controller != null)
                _controller.OnConversationUpdated -= OnConversationUpdated;
        }

        private void OnConversationUpdated()
        {
            _persistedMessages.Clear();
            foreach (var msg in _controller.Messages)
                _persistedMessages.Add(PersistedMessage.From(msg));

            _persistedCodePreview  = _controller.LastCodePreview  ?? "";
            _persistedArtifactPath = _controller.LastArtifactPath ?? "";

            _chatScroll.y = float.MaxValue;
            Repaint();
        }

        private void OnGUI()
        {
            InitStylesOnce();
            DrawToolbar();
            EditorGUILayout.Space(4);
            DrawChatHistory();
            EditorGUILayout.Space(4);
            DrawCodePreview();
            EditorGUILayout.Space(4);
            DrawInputArea();
        }

        // ── Toolbar ───────────────────────────────────────────

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Claude Game Assistant", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("⚙ Config", EditorStyles.toolbarButton, GUILayout.Width(70)))
                    Selection.activeObject = _config;

                GUI.enabled = !_controller.IsLoading;
                if (GUILayout.Button("🗑 Clear", EditorStyles.toolbarButton, GUILayout.Width(60)))
                    TryClearHistory();
                GUI.enabled = true;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("API Key:", GUILayout.Width(56));

                string current = _config.ApiKey;
                string entered = EditorGUILayout.PasswordField(current);
                if (entered != current)
                    _config.ApiKey = entered;

                if (!string.IsNullOrWhiteSpace(_config.ApiKey))
                {
                    if (GUILayout.Button("✕", GUILayout.Width(22)))
                    {
                        if (EditorUtility.DisplayDialog(
                            "Clear API Key",
                            "Remove the stored API key from this machine?",
                            "Yes", "No"))
                            _config.ClearApiKey();
                    }
                }
            }
        }

        // ── Chat history ──────────────────────────────────────

        private void DrawChatHistory()
        {
            bool  hasPreview     = !string.IsNullOrEmpty(_controller.LastCodePreview);
            float reservedBottom = hasPreview ? 390 : 230;
            float historyHeight  = position.height - reservedBottom;

            using (var scroll = new EditorGUILayout.ScrollViewScope(
                _chatScroll, GUILayout.Height(Mathf.Max(historyHeight, 120))))
            {
                _chatScroll = scroll.scrollPosition;

                if (_controller.Messages.Count == 0)
                {
                    EditorGUILayout.HelpBox(
                        "👋 Hola! Describí lo que querés crear.\n\n" +
                        "Ejemplos:\n" +
                        "• \"Creá un nivel 2D con 6 plataformas en zigzag\"\n" +
                        "• \"Script de controlador de jugador con salto y coyote time\"\n" +
                        "• \"Armá una habitación 3D 10x10 con paredes y techo\"\n" +
                        "• \"Sistema de vida con daño e invencibilidad temporal\"",
                        MessageType.Info);
                    return;
                }

                foreach (var msg in _controller.Messages)
                    DrawMessageBubble(msg);

                if (_controller.IsLoading)
                {
                    string loadingMsg = _lastSentMode switch
                    {
                        GenerationMode.Script  => "⚙️ Generando script...",
                        GenerationMode.Scene   => "🏗️ Construyendo escena...",
                        GenerationMode.Consult => "💬 Claude está respondiendo...",
                        _                      => "⏳ Procesando..."
                    };
                    GUILayout.Label(loadingMsg, _labelSmall);
                }
            }
        }

        // ── Code preview panel ────────────────────────────────

        private Vector2 _previewScroll;
        private bool    _previewFolded = false;

        private void DrawCodePreview()
        {
            if (string.IsNullOrEmpty(_controller.LastCodePreview)) return;

            if (_controller.Messages.Count > 0)
            {
                var last = _controller.Messages[_controller.Messages.Count - 1];
                if (last.Mode == GenerationMode.Consult) return;
            }

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            using (new EditorGUILayout.HorizontalScope())
            {
                _previewFolded = !EditorGUILayout.Foldout(!_previewFolded, "  💾 Último código generado", true);

                // Copy button
                if (GUILayout.Button("📋 Copiar", EditorStyles.miniButton, GUILayout.Width(70)))
                {
                    EditorGUIUtility.systemCopyBuffer = _controller.LastCodePreview;
                    Debug.Log("[ClaudeAssistant] Código copiado al portapapeles.");
                }

                // Open in VS Code button — only shown when we have a saved file path
                if (!string.IsNullOrEmpty(_controller.LastArtifactPath))
                {
                    if (GUILayout.Button("📂 VS Code", EditorStyles.miniButton, GUILayout.Width(75)))
                    {
                        string fullPath = System.IO.Path.GetFullPath(_controller.LastArtifactPath);
                        System.Diagnostics.Process.Start("code", $"\"{fullPath}\"");
                    }
                }
            }

            if (!_previewFolded)
            {
                using (var scroll = new EditorGUILayout.ScrollViewScope(
                    _previewScroll, GUILayout.Height(150)))
                {
                    _previewScroll = scroll.scrollPosition;
                    EditorGUILayout.TextArea(
                        _controller.LastCodePreview,
                        _assistantBubble,
                        GUILayout.ExpandHeight(true));
                }
            }
        }

        private void DrawMessageBubble(ChatMessage msg)
        {
            bool     isUser = msg.Role == MessageRole.User;
            GUIStyle style  = isUser ? _userBubble : _assistantBubble;

            string modeTag = msg.Mode != GenerationMode.Unknown ? $" [{msg.Mode}]" : "";
            string header  = isUser
                ? $"🧑 Vos  {msg.Timestamp:HH:mm}"
                : $"🤖 Claude{modeTag}  {msg.Timestamp:HH:mm}";

            GUILayout.Label(header, _labelSmall);

            if (!isUser && msg.Content.Length > 300)
            {
                using (var scroll = new EditorGUILayout.ScrollViewScope(
                    _codeScroll, GUILayout.Height(140)))
                {
                    _codeScroll = scroll.scrollPosition;
                    GUILayout.TextArea(msg.Content, style);
                }
            }
            else
            {
                GUILayout.TextArea(msg.Content, style);
            }

            EditorGUILayout.Space(6);
        }

        // ── Input area ────────────────────────────────────────

        private void DrawInputArea()
        {
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Nombre del script:", GUILayout.Width(130));
                _scriptName = EditorGUILayout.TextField(_scriptName);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Modo:", GUILayout.Width(130));
                _modeOverride = (GenerationMode)EditorGUILayout.EnumPopup(_modeOverride);
                EditorGUILayout.LabelField("(Unknown = auto)", _labelSmall, GUILayout.Width(120));
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Describí lo que querés:");

            // Capture keyboard events for history navigation BEFORE drawing TextArea
            HandlePromptHistoryKeys();

            _prompt = EditorGUILayout.TextArea(_prompt, GUILayout.Height(60));
            EditorGUILayout.Space(4);

            if (!_config.IsValid)
                EditorGUILayout.HelpBox(
                    "⚠ Ingresá tu API key arriba. Se guarda en tu máquina, nunca en el proyecto.",
                    MessageType.Warning);

            bool canSend = !_controller.IsLoading
                && !string.IsNullOrWhiteSpace(_prompt)
                && _config.IsValid;

            GUI.enabled = canSend;
            if (GUILayout.Button(
                _controller.IsLoading ? "⏳ Generando..." : "✨ Enviar",
                GUILayout.Height(36)))
            {
                SendCurrentPrompt();
            }
            GUI.enabled = true;
        }

        // ── Prompt history ────────────────────────────────────

        /// <summary>
        /// Handles ↑↓ arrow keys to navigate through previously sent prompts.
        /// ↑ goes back in history, ↓ goes forward (toward empty / most recent).
        /// </summary>
        private void HandlePromptHistoryKeys()
        {
            var e = Event.current;
            if (e.type != EventType.KeyDown) return;

            if (e.keyCode == KeyCode.UpArrow && _promptHistory.Count > 0)
            {
                _historyIndex = Mathf.Min(_historyIndex + 1, _promptHistory.Count - 1);
                _prompt       = _promptHistory[_promptHistory.Count - 1 - _historyIndex];
                e.Use();
                Repaint();
            }
            else if (e.keyCode == KeyCode.DownArrow)
            {
                _historyIndex = Mathf.Max(_historyIndex - 1, -1);
                _prompt       = _historyIndex == -1
                    ? ""
                    : _promptHistory[_promptHistory.Count - 1 - _historyIndex];
                e.Use();
                Repaint();
            }
        }

        private void SendCurrentPrompt()
        {
            string snapshot = _prompt.Trim();
            if (string.IsNullOrEmpty(snapshot)) return;

            // Add to history if different from last entry
            if (_promptHistory.Count == 0 || _promptHistory[_promptHistory.Count - 1] != snapshot)
            {
                _promptHistory.Add(snapshot);
                if (_promptHistory.Count > MAX_HISTORY)
                    _promptHistory.RemoveAt(0);
            }

            _historyIndex = -1;
            _prompt       = "";
            _lastSentMode = _modeOverride != GenerationMode.Unknown
                ? _modeOverride
                : GenerationMode.Unknown;

            _ = _controller.SendAsync(snapshot, _scriptName, _modeOverride);
        }

        // ── Helpers ───────────────────────────────────────────

        private void TryClearHistory()
        {
            if (EditorUtility.DisplayDialog(
                "Borrar conversación",
                "¿Seguro que querés borrar todo el historial?",
                "Sí", "No"))
            {
                _controller.ClearHistory();
                _persistedMessages.Clear();
                _persistedCodePreview  = "";
                _persistedArtifactPath = "";
                Repaint();
            }
        }

        private ClaudeConfig LoadOrCreateConfig()
        {
            const string configPath = "Assets/Editor/ClaudeAssistant/ClaudeConfig.asset";

            var cfg = AssetDatabase.LoadAssetAtPath<ClaudeConfig>(configPath);
            if (cfg != null) return cfg;

            string dir = System.IO.Path.GetDirectoryName(configPath);
            if (!System.IO.Directory.Exists(dir))
                System.IO.Directory.CreateDirectory(dir);

            cfg = CreateInstance<ClaudeConfig>();
            AssetDatabase.CreateAsset(cfg, configPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[ClaudeAssistant] Config creado en {configPath}. Ingresá tu API key en la ventana.");
            return cfg;
        }

        // ── Styles ────────────────────────────────────────────

        private void InitStylesOnce()
        {
            if (_stylesInitialized) return;

            _userBubble = new GUIStyle(EditorStyles.textArea)
            {
                wordWrap = true,
                fontSize = 12,
                padding  = new RectOffset(8, 8, 6, 6),
                normal   = { background = MakeTex(2, 2, new Color(0.22f, 0.35f, 0.55f, 0.3f)) }
            };

            _assistantBubble = new GUIStyle(EditorStyles.textArea)
            {
                wordWrap = true,
                fontSize = 12,
                padding  = new RectOffset(8, 8, 6, 6),
                normal   = { background = MakeTex(2, 2, new Color(0.15f, 0.15f, 0.15f, 0.3f)) }
            };

            _labelSmall = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 10,
                normal   = { textColor = new Color(0.6f, 0.6f, 0.6f) }
            };

            _stylesInitialized = true;
        }

        private static Texture2D MakeTex(int w, int h, Color col)
        {
            var pix = new Color[w * h];
            for (int i = 0; i < pix.Length; i++) pix[i] = col;
            var tex = new Texture2D(w, h);
            tex.SetPixels(pix);
            tex.Apply();
            return tex;
        }
    }
}