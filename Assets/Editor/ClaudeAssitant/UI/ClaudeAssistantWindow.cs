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
        [MenuItem("Tools/Claude Game Assistant %#c")]
        public static void Open() => GetWindow<ClaudeAssistantWindow>(AssistantL10n.Title);

        [SerializeField] private List<PersistedMessage> _persistedMessages = new();
        [SerializeField] private string _persistedCodePreview = "";

        private ClaudeConfig _config;
        private ConversationController _controller;

        private string _prompt = "";
        private string _scriptName = "MyGeneratedScript";
        private GenerationMode _modeOverride = GenerationMode.Unknown;
        private GenerationMode _lastSentMode = GenerationMode.Unknown;
        private Vector2 _chatScroll;
        private Vector2 _codeScroll;

        private GUIStyle _userBubble;
        private GUIStyle _assistantBubble;
        private GUIStyle _labelSmall;
        private bool _stylesInitialized;

        private Vector2 _previewScroll;
        private bool _previewFolded;

        private void OnEnable()
        {
            _config = LoadOrCreateConfig();

            var history = new ConversationHistory();
            foreach (var pm in _persistedMessages)
                history.Add(pm.ToChatMessage());

            _controller = new ConversationController(_config, history);
            _controller.OnConversationUpdated += OnConversationUpdated;
            LanguageSettings.OnLanguageChanged += Repaint;

            if (!string.IsNullOrEmpty(_persistedCodePreview))
                _controller.RestoreCodePreview(_persistedCodePreview);

            minSize = new Vector2(420, 580);
        }

        private void OnDisable()
        {
            _controller?.Cancel();
            if (_controller != null)
                _controller.OnConversationUpdated -= OnConversationUpdated;
            LanguageSettings.OnLanguageChanged -= Repaint;
        }

        private void OnConversationUpdated()
        {
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

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label(AssistantL10n.Title, EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();

                GUI.enabled = !_controller.IsLoading;
                if (GUILayout.Button(AssistantL10n.ClearButton, EditorStyles.toolbarButton, GUILayout.Width(80)))
                    TryClearHistory();

                if (GUILayout.Button(AssistantL10n.LanguageToggle, EditorStyles.toolbarButton, GUILayout.Width(50)))
                {
                    LanguageSettings.Toggle();
                    Repaint();
                }
                GUI.enabled = true;

                if (GUILayout.Button(AssistantL10n.ConfigButton, EditorStyles.toolbarButton, GUILayout.Width(70)))
                    Selection.activeObject = _config;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(AssistantL10n.ApiKeyLabel, GUILayout.Width(56));

                string current = _config.ApiKey;
                string entered = EditorGUILayout.PasswordField(current);
                if (entered != current)
                    _config.ApiKey = entered;

                if (!string.IsNullOrWhiteSpace(_config.ApiKey) && GUILayout.Button("✕", GUILayout.Width(22)))
                {
                    if (EditorUtility.DisplayDialog(
                        AssistantL10n.ClearApiTitle,
                        AssistantL10n.ClearApiMessage,
                        AssistantL10n.ClearYes,
                        AssistantL10n.ClearNo))
                    {
                        _config.ClearApiKey();
                    }
                }
            }
        }

        private void DrawChatHistory()
        {
            bool hasPreview = !string.IsNullOrEmpty(_controller.LastCodePreview);
            float reservedBottom = hasPreview ? 390 : 230;
            float historyHeight = position.height - reservedBottom;

            using (var scroll = new EditorGUILayout.ScrollViewScope(
                _chatScroll, GUILayout.Height(Mathf.Max(historyHeight, 120))))
            {
                _chatScroll = scroll.scrollPosition;

                if (_controller.Messages.Count == 0)
                {
                    EditorGUILayout.HelpBox(AssistantL10n.EmptyHint, MessageType.Info);
                    return;
                }

                foreach (var msg in _controller.Messages)
                    DrawMessageBubble(msg);

                if (_controller.IsLoading)
                {
                    string loadingLabel = _lastSentMode switch
                    {
                        GenerationMode.Script => AssistantL10n.LoadingScript,
                        GenerationMode.Scene => AssistantL10n.LoadingScene,
                        GenerationMode.Consult => AssistantL10n.LoadingConsult,
                        _ => AssistantL10n.LoadingDefault
                    };
                    GUILayout.Label(loadingLabel, _labelSmall);
                }
            }
        }

        private void DrawCodePreview()
        {
            if (string.IsNullOrEmpty(_controller.LastCodePreview)) return;

            var msgs = _controller.Messages;
            if (msgs.Count > 0 && msgs[msgs.Count - 1].Mode == GenerationMode.Consult) return;

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            using (new EditorGUILayout.HorizontalScope())
            {
                _previewFolded = !EditorGUILayout.Foldout(!_previewFolded, $"  {AssistantL10n.CodePreviewLabel}", true);

                if (GUILayout.Button(AssistantL10n.CopyButton, EditorStyles.miniButton, GUILayout.Width(65)))
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

        private void DrawInputArea()
        {
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(AssistantL10n.ScriptNameLabel, GUILayout.Width(130));
                _scriptName = EditorGUILayout.TextField(_scriptName);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(AssistantL10n.ModeLabel, GUILayout.Width(130));
                _modeOverride = (GenerationMode)EditorGUILayout.EnumPopup(_modeOverride);
                EditorGUILayout.LabelField(AssistantL10n.ModeAutoHint, _labelSmall, GUILayout.Width(120));
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField(AssistantL10n.PromptLabel);
            _prompt = EditorGUILayout.TextArea(_prompt, GUILayout.Height(60));
            EditorGUILayout.Space(4);

            if (!_config.IsValid)
                EditorGUILayout.HelpBox(AssistantL10n.ApiKeyWarning, MessageType.Warning);

            bool canSend = !_controller.IsLoading
                && !string.IsNullOrWhiteSpace(_prompt)
                && _config.IsValid;

            GUI.enabled = canSend;
            if (GUILayout.Button(
                _controller.IsLoading ? AssistantL10n.SendingButton : AssistantL10n.SendButton,
                GUILayout.Height(36)))
            {
                string snapshot = _prompt.Trim();
                _prompt = "";
                _lastSentMode = _modeOverride != GenerationMode.Unknown ? _modeOverride : GenerationMode.Unknown;
                _ = _controller.SendAsync(snapshot, _scriptName, _modeOverride);
            }
            GUI.enabled = true;
        }

        private void TryClearHistory()
        {
            if (EditorUtility.DisplayDialog(
                AssistantL10n.ClearTitle,
                AssistantL10n.ClearMessage,
                AssistantL10n.ClearYes,
                AssistantL10n.ClearNo))
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
