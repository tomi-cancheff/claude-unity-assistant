namespace ClaudeAssistant.Core
{
    public static class AssistantL10n
    {
        public static string Title => "Claude Game Assistant";
        public static string UserLabel => LanguageSettings.Current == AppLanguage.Spanish ? "🧑 Vos" : "🧑 You";
        public static string AssistantLabel => "🤖 Claude";
        public static string ConfigButton => "⚙ Config";
        public static string ClearButton => LanguageSettings.Current == AppLanguage.Spanish ? "🗑 Limpiar" : "🗑 Clear";
        public static string ApiKeyLabel => "API Key:";
        public static string ScriptNameLabel => LanguageSettings.Current == AppLanguage.Spanish ? "Nombre del script:" : "Script name:";
        public static string ModeLabel => LanguageSettings.Current == AppLanguage.Spanish ? "Modo:" : "Mode:";
        public static string ModeAutoHint => "(Unknown = auto)";
        public static string PromptLabel => LanguageSettings.Current == AppLanguage.Spanish ? "Describí lo que querés:" : "Describe what you want:";
        public static string SendButton => LanguageSettings.Current == AppLanguage.Spanish ? "✨ Enviar" : "✨ Send";
        public static string SendingButton => LanguageSettings.Current == AppLanguage.Spanish ? "⏳ Generando..." : "⏳ Generating...";
        public static string ApiKeyWarning => LanguageSettings.Current == AppLanguage.Spanish
            ? "⚠ Ingresá tu API key arriba. Se guarda en tu máquina, nunca en el proyecto."
            : "⚠ Enter your API key above. Stored on your machine, never in the project.";
        public static string ClearTitle => LanguageSettings.Current == AppLanguage.Spanish ? "Borrar conversación" : "Clear conversation";
        public static string ClearMessage => LanguageSettings.Current == AppLanguage.Spanish ? "¿Seguro que querés borrar todo el historial?" : "Are you sure you want to clear the history?";
        public static string ClearYes => LanguageSettings.Current == AppLanguage.Spanish ? "Sí" : "Yes";
        public static string ClearNo => "No";
        public static string ClearApiTitle => "Clear API Key";
        public static string ClearApiMessage => LanguageSettings.Current == AppLanguage.Spanish ? "¿Eliminar la API key de esta máquina?" : "Remove the stored API key from this machine?";
        public static string EmptyHint => LanguageSettings.Current == AppLanguage.Spanish
            ? "👋 Hola! Describí lo que querés crear.\n\nEjemplos:\n• \"Creá un nivel 2D con 6 plataformas en zigzag\"\n• \"Script de controlador de jugador con salto y coyote time\"\n• \"Armá una habitación 3D 10x10 con paredes y techo\"\n• \"Sistema de vida con daño e invencibilidad temporal\""
            : "👋 Hi! Describe what you want to create.\n\nExamples:\n• \"Create a 2D level with 6 zigzag platforms\"\n• \"Player controller script with jump and coyote time\"\n• \"Build a 3D room 10x10 with walls and ceiling\"\n• \"Health system with damage and temporary invincibility\"";
        public static string LoadingScript => LanguageSettings.Current == AppLanguage.Spanish ? "⚙️ Generando script..." : "⚙️ Generating script...";
        public static string LoadingScene => LanguageSettings.Current == AppLanguage.Spanish ? "🏗️ Construyendo escena..." : "🏗️ Building scene...";
        public static string LoadingConsult => LanguageSettings.Current == AppLanguage.Spanish ? "💬 Claude está respondiendo..." : "💬 Claude is responding...";
        public static string LoadingDefault => LanguageSettings.Current == AppLanguage.Spanish ? "⏳ Procesando..." : "⏳ Processing...";
        public static string CodePreviewLabel => LanguageSettings.Current == AppLanguage.Spanish ? "💾 Último código generado" : "💾 Last generated code";
        public static string CopyButton => LanguageSettings.Current == AppLanguage.Spanish ? "📋 Copiar" : "📋 Copy";
        public static string LanguageToggle => LanguageSettings.Current == AppLanguage.Spanish ? "🌐 EN" : "🌐 ES";
    }
}
