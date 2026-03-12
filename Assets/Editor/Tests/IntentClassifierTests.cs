// ============================================================
//  IntentClassifierTests.cs
//  Unit tests for IntentClassifier using the Unity Test Framework.
//
//  HOW TO RUN:
//    Window → General → Test Runner → EditMode → Run All
//
//  These tests document known behavior AND known limitations
//  of the keyword-based classifier.
// ============================================================

using NUnit.Framework;
using ClaudeAssistant.Models;
using ClaudeAssistant.Utils;

namespace ClaudeAssistant.Tests
{
    public class IntentClassifierTests
    {
        // ── Scene prompts ─────────────────────────────────────

        [Test]
        [TestCase("Creá un nivel 2D con 6 plataformas en zigzag")]
        [TestCase("Genera un laberinto de 10x10 con cubos")]
        [TestCase("Armá una habitación 3D de 10x8 con paredes y techo")]
        [TestCase("Crea un piso con bordes y un hoyo al final")]
        [TestCase("Generá 5 esferas distribuidas en el escenario")]
        [TestCase("Coloca 3 luces direccionales en la escena")]
        [TestCase("Create a 3D room with walls and a ceiling")]
        [TestCase("Place some platforms in the level")]
        public void Classify_ScenePrompts_ReturnsSceneMode(string prompt)
        {
            var result = IntentClassifier.Classify(prompt);
            Assert.AreEqual(GenerationMode.Scene, result,
                $"Expected Scene for: \"{prompt}\"");
        }

        // ── Script prompts ────────────────────────────────────

        [Test]
        [TestCase("Script de controlador de movimiento 2D con salto")]
        [TestCase("Sistema de vida con daño e invencibilidad temporal")]
        [TestCase("Cámara que sigue al jugador con suavizado")]
        [TestCase("Enemigo con estados: patrulla, persecución y ataque")]
        [TestCase("Singleton de GameManager con sistema de puntuación")]
        [TestCase("Inventario con slots y drag and drop")]
        [TestCase("Player controller with jump and coyote time")]
        [TestCase("AI pathfinding script for enemy")]
        public void Classify_ScriptPrompts_ReturnsScriptMode(string prompt)
        {
            var result = IntentClassifier.Classify(prompt);
            Assert.AreEqual(GenerationMode.Script, result,
                $"Expected Script for: \"{prompt}\"");
        }

        // ── Edge cases ────────────────────────────────────────

        [Test]
        public void Classify_EmptyString_ReturnsUnknown()
        {
            Assert.AreEqual(GenerationMode.Unknown, IntentClassifier.Classify(""));
        }

        [Test]
        public void Classify_NullString_ReturnsUnknown()
        {
            Assert.AreEqual(GenerationMode.Unknown, IntentClassifier.Classify(null));
        }

        [Test]
        public void Classify_WhitespaceOnly_ReturnsUnknown()
        {
            Assert.AreEqual(GenerationMode.Unknown, IntentClassifier.Classify("   "));
        }

        [Test]
        public void Classify_UnrelatedText_ReturnsUnknown()
        {
            // No keywords from either list → Unknown
            Assert.AreEqual(GenerationMode.Unknown,
                IntentClassifier.Classify("Hola, buenos días, ¿cómo estás?"));
        }

        // ── Tie-breaking (scene wins on equal score) ──────────

        [Test]
        public void Classify_EqualSceneAndScriptKeywords_PrefersScene()
        {
            // "crea" (scene) + "script" (script) → tie → Scene wins
            var result = IntentClassifier.Classify("crea un script");
            Assert.AreEqual(GenerationMode.Scene, result,
                "Scene should win on tie (scene >= script)");
        }

        // ── Known limitations (documented) ───────────────────

        [Test]
        [Description("Known limitation: highly ambiguous prompts may be misclassified. " +
                     "This is a deliberate trade-off in favor of simplicity. " +
                     "Override via the Mode dropdown in the UI.")]
        public void Classify_AmbiguousPrompt_ClassifiesWithBestGuess()
        {
            // "sistema de spawn de enemigos en el nivel" has both scene + script keywords
            // We just assert it returns a non-Unknown value, not a specific mode
            var result = IntentClassifier.Classify("sistema de spawn de enemigos en el nivel");
            Assert.AreNotEqual(GenerationMode.Unknown, result,
                "Ambiguous prompts should still produce a best-guess, not Unknown.");
        }
    }
}
