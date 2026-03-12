// ============================================================
//  IntentClassifier.cs
//  Lightweight keyword classifier that decides whether a prompt
//  targets Script generation or Scene construction.
//
//  Deliberately simple — fast, deterministic, zero API cost.
//  Extend the keyword lists to tune accuracy.
//
//  Classification rules (in order):
//    1. If prompt contains a Script override keyword → Script always wins
//    2. Count Scene vs Script keyword matches
//    3. On tie → Script wins (safer default: scripts can always be attached,
//       scene construction is harder to undo mentally)
// ============================================================

using System.Text.RegularExpressions;
using ClaudeAssistant.Models;

namespace ClaudeAssistant.Utils
{
    public static class IntentClassifier
    {
        // ── Script override keywords (always win) ─────────────
        // If any of these appear, it's a Script regardless of scene words.

        private static readonly string[] ScriptOverrideKeywords =
        {
            "script", "monobehaviour", "clase", "class",
            "codigo", "código", "component", "componente"
        };

        // ── Scene keywords ────────────────────────────────────
        // Generic action verbs (crea, genera) removed — they appear in both
        // contexts and caused false positives. Only concrete scene nouns remain.

        private static readonly string[] SceneKeywords =
        {
            "nivel", "level", "escena", "scene", "layout",
            "plataforma", "plataformas", "plano", "terreno",
            "primitiva", "cubo", "esfera", "cilindro", "capsula",
            "pared", "paredes", "habitacion", "habitación",
            "mapa", "map", "laberinto", "maze",
            "build scene", "create scene", "add object", "place object",
            "coloca", "posiciona", "distribuye", "instancia"
        };

        // ── Script keywords ───────────────────────────────────

        private static readonly string[] ScriptKeywords =
        {
            "movimiento", "movement", "salto", "jump", "disparo", "shoot",
            "enemigo", "enemy", "jugador", "player", "controller",
            "camara", "cámara", "camera", "follow",
            "inventario", "inventory", "vida", "health", "daño", "damage",
            "ia", "ai", "pathfinding", "animacion", "animación",
            "sistema", "system", "mechanic", "mecánica", "mecanica",
            "manager", "singleton", "event", "evento",
            "rota", "rotar", "rotate", "gira", "girar",
            "obstáculo", "obstaculo", "obstacle",
            "velocidad", "speed", "fuerza", "force"
        };

        // ── Public API ────────────────────────────────────────

        /// <summary>
        /// Classifies the user's prompt into Scene, Script, or Unknown.
        /// </summary>
        public static GenerationMode Classify(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                return GenerationMode.Unknown;

            string lower = Normalize(prompt);

            // Rule 1: Script override — explicit script keywords always win
            foreach (var kw in ScriptOverrideKeywords)
                if (lower.Contains(kw)) return GenerationMode.Script;

            int sceneScore = CountMatches(lower, SceneKeywords);
            int scriptScore = CountMatches(lower, ScriptKeywords);

            if (sceneScore == 0 && scriptScore == 0)
                return GenerationMode.Unknown;

            // Rule 3: On tie, Script wins
            return sceneScore > scriptScore
                ? GenerationMode.Scene
                : GenerationMode.Script;
        }

        // ── Helpers ───────────────────────────────────────────

        private static string Normalize(string text) =>
            Regex.Replace(text.ToLowerInvariant(), @"\s+", " ").Trim();

        private static int CountMatches(string text, string[] keywords)
        {
            int count = 0;
            foreach (var kw in keywords)
                if (text.Contains(kw)) count++;
            return count;
        }
    }
}