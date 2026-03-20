using System;
using UnityEditor;

namespace ClaudeAssistant.Core
{
    public enum AppLanguage { Spanish, English }

    public static class LanguageSettings
    {
        private const string LanguagePrefKey = "ClaudeAssistant_Language";

        public static event Action OnLanguageChanged;

        public static AppLanguage Current { get; private set; } = AppLanguage.Spanish;

        static LanguageSettings()
        {
            int storedValue = EditorPrefs.GetInt(LanguagePrefKey, (int)AppLanguage.Spanish);
            if (Enum.IsDefined(typeof(AppLanguage), storedValue))
                Current = (AppLanguage)storedValue;
        }

        public static void Toggle() => Set(Current == AppLanguage.Spanish
            ? AppLanguage.English
            : AppLanguage.Spanish);

        public static void Set(AppLanguage lang)
        {
            Current = lang;
            EditorPrefs.SetInt(LanguagePrefKey, (int)Current);
            OnLanguageChanged?.Invoke();
        }
    }
}
