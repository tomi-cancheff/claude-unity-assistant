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

        [MenuItem("Tools/Claude Game Assistant %#c")]   // Ctrl+Shift+C
        public static void Open() => GetWindow<ClaudeAssistantWindow>("🤖 Claude Assistant");

        // ── Persistence across domain reloads ─────────────────
        // Unity serializes [SerializeField] on EditorWindows and restores
        // them after recompilation. We mirror the history here as plain
        // serializable DTOs and rebuild the controller from them on OnEnable.

        [SerializeField] private List<PersistedMessage> _persistedMessages = new();
        [SerializeField] private string _persistedCodePreview = "";

        // ── Dependencies ──────────────────────────────────────

        private ClaudeConfig _config;
        private ConversationController _controller;

        // ── UI state ──────────────────────────────────────────

        private string _prompt = "";
        private string _scriptName = "MyGeneratedScript";
        private GenerationMode _modeOverride = GenerationMode.Unknown;
        private Vector2 _chatScroll;
        private Vector2 _codeScroll;

        // ── Styles (lazily initialized) ───────────────────────

        private GUIStyle _userBubble;
        private GUIStyle _assistantBubble;
        private GUIStyle _labelSmall;
        private bool _stylesInitialized;

        // ── Unity lifecycle ───────────────────────────────────

        private void OnEnable()
        {
            _config = LoadOrCreateConfig();

            // Rebuild history from persisted messages (survives recompile)
            var history = new ConversationHistory();
            foreach (var pm in _persistedMessages)
                history.Add(pm.ToChatMessage());

            _controller = new ConversationController(_config, history);
            _controller.OnConversationUpdated += OnConversationUpdated;

            // Restore last code preview
            if (!string.IsNullOrEmpty(_persistedCodePreview))
                _controller.RestoreCodePreview(_persistedCodePreview);

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
            // Sync serializable mirror so history survives next recompile
            _persistedMessages.Clear();
            foreach (var msg in _controller.Messages)
                _persistedMessages.Add(PersistedMessage.From(msg));

            _persistedCodePreview = _controller.LastCodePreview ?? "";

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

            // API Key — stored in EditorPrefs, never serialized in the .asset
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
            // Reserve space for code preview panel when it's visible
            bool hasPreview = !string.IsNullOrEmpty(_controller.LastCodePreview);
            float reservedBottom = hasPreview ? 390 : 230;
            float historyHeight = position.height - reservedBottom;

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
                    GUILayout.Label("⏳ Claude está pensando...", _labelSmall);
            }
        }

        // ── Code preview panel ────────────────────────────────

        private Vector2 _previewScroll;
        private bool _previewFolded = false;

        private void DrawCodePreview()
        {
            if (string.IsNullOrEmpty(_controller.LastCodePreview)) return;

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            using (new EditorGUILayout.HorizontalScope())
            {
                _previewFolded = !EditorGUILayout.Foldout(!_previewFolded, "  💾 Último código generado", true);

                if (GUILayout.Button("📋 Copiar", EditorStyles.miniButton, GUILayout.Width(65)))
                {
                    EditorGUIUtility.systemCopyBuffer = _controller.LastCodePreview;
                    Debug.Log("[ClaudeAssistant] Código copiado al portapapeles.");
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
            bool isUser = msg.Role == MessageRole.User;
            GUIStyle style = isUser ? _userBubble : _assistantBubble;

            string modeTag = msg.Mode != GenerationMode.Unknown ? $" [{msg.Mode}]" : "";
            string header = isUser
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
                string snapshot = _prompt.Trim();
                _prompt = "";
                _ = _controller.SendAsync(snapshot, _scriptName, _modeOverride);
            }
            GUI.enabled = true;
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
                _persistedCodePreview = "";
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
                padding = new RectOffset(8, 8, 6, 6),
                normal = { background = MakeTex(2, 2, new Color(0.22f, 0.35f, 0.55f, 0.3f)) }
            };

            _assistantBubble = new GUIStyle(EditorStyles.textArea)
            {
                wordWrap = true,
                fontSize = 12,
                padding = new RectOffset(8, 8, 6, 6),
                normal = { background = MakeTex(2, 2, new Color(0.15f, 0.15f, 0.15f, 0.3f)) }
            };

            _labelSmall = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 10,
                normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
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