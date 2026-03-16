// ============================================================
//  ConsultHandler.cs
//  Concrete Strategy: answers Unity/C# questions in natural
//  language. Does not generate files or modify the scene —
//  the response IS the result.
//
//  Pattern: Strategy (implements IGenerationHandler)
// ============================================================

using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ClaudeAssistant.Core;
using ClaudeAssistant.Models;
using UnityEngine;

namespace ClaudeAssistant.Handlers
{
    public class ConsultHandler : IGenerationHandler
    {
        // ── IGenerationHandler ────────────────────────────────

        public string Label => "💬 Consulta";

        public string SystemPrompt =>
            @"Eres un experto en Unity3D, C# y desarrollo de videojuegos. Tu rol es responder preguntas,
explicar conceptos y dar recomendaciones claras en español.

TEMAS QUE DOMINÁS:
- Scripting en Unity: MonoBehaviour, ScriptableObjects, Coroutines, eventos, interfaces
- Editor de Unity: Baking de luces, Occlusion Culling, NavMesh, Profiler, configuración de proyecto
- Optimización: draw calls, batching estático/dinámico, LOD, compresión de texturas, memory pooling
- Física: Rigidbody, colisiones, capas, Physics settings
- Animaciones: Animator Controller, blend trees, Animation Rigging
- Buenas prácticas de C# en Unity: patrones de diseño, SOLID, evitar garbage, serialización
- Comparaciones y diferencias entre enfoques o componentes de Unity

REGLAS:
- Respondé siempre en español, de forma clara y concisa
- Usá ejemplos de código cortos cuando ilustren mejor la respuesta
- Si hay múltiples enfoques válidos, mencioná los trade-offs
- No generes scripts completos — eso es tarea del Script Mode
- Si la pregunta es ambigua, respondé la interpretación más útil y aclarala
- Evitá usar markdown (**, ##, *, _) — el texto se muestra en un editor de Unity sin renderizado";

        public async Task<GenerationResult> HandleAsync(
            ClaudeConfig      config,
            List<ChatMessage> history,
            string            userPrompt,
            string            artifactName)
        {
            try
            {
                string raw = await ClaudeApiClient.Instance.SendAsync(
                    config, history, SystemPrompt);

                if (string.IsNullOrWhiteSpace(raw))
                    return GenerationResult.Fail("Claude devolvió una respuesta vacía.");

                return GenerationResult.Ok(
                    raw: raw,
                    display: StripMarkdown(raw));
            }
            catch (System.Exception ex)
            {
                return GenerationResult.Fail(ex.Message);
            }
        }

        // ── Private helpers ───────────────────────────────────

        private static string StripMarkdown(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            // Code blocks (```...```) — strip fences, keep inner content
            text = Regex.Replace(text, @"```[^\n]*\n?([\s\S]*?)```",
                m => m.Groups[1].Value.Trim(), RegexOptions.Multiline);

            // Horizontal rules (---, ***, ___)
            text = Regex.Replace(text, @"^\s*[-*_]{3,}\s*$", "", RegexOptions.Multiline);

            // Headers (## Header → Header)
            text = Regex.Replace(text, @"^#{1,6}\s+", "", RegexOptions.Multiline);

            // Bold (**text** or __text__)
            text = Regex.Replace(text, @"\*\*(.+?)\*\*", "$1");
            text = Regex.Replace(text, @"__(.+?)__", "$1");

            // Italic (*text* or _text_)
            text = Regex.Replace(text, @"\*(.+?)\*", "$1");
            text = Regex.Replace(text, @"_(.+?)_", "$1");

            // Inline code (`text`)
            text = Regex.Replace(text, @"`(.+?)`", "$1");

            // Blockquotes (> text)
            text = Regex.Replace(text, @"^>\s?", "", RegexOptions.Multiline);

            // Collapse 3+ blank lines into 2
            text = Regex.Replace(text, @"\n{3,}", "\n\n");

            return text.Trim();
        }
    }
}
