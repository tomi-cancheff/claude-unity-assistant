// ============================================================
//  ScriptGenerationHandler.cs
//  Concrete Strategy: generates one or more MonoBehaviour C#
//  scripts, strips markdown fences, and writes each to disk.
//
//  Multi-script support:
//    Claude is instructed to wrap each file in its own fenced
//    block with a // FileName: X.cs hint. CodeExtractor.ExtractAll()
//    parses all blocks. A hard limit (MAX_SCRIPTS) prevents runaway
//    generation and gives the user a clear error if exceeded.
//
//  Pattern: Strategy (implements IGenerationHandler)
// ============================================================

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ClaudeAssistant.Core;
using ClaudeAssistant.Models;
using ClaudeAssistant.Utils;
using UnityEditor;
using UnityEngine;

namespace ClaudeAssistant.Handlers
{
    public class ScriptGenerationHandler : IGenerationHandler
    {
        // Maximum scripts allowed per single generation request.
        // Beyond this the user gets a clear message to split the request.
        private const int MAX_SCRIPTS = 5;

        // ── IGenerationHandler ────────────────────────────────

        public string Label => "📄 Script";

        // SystemPrompt is built dynamically so Claude always uses the exact
        // name the user chose for both the C# class and the FileName hint.
        public string SystemPrompt => BuildSystemPrompt("GeneratedScript");

        private string BuildSystemPrompt(string scriptName) =>
            $@"Eres un experto en Unity3D y C#. Tu única tarea es generar scripts C# para Unity.

NOMBRE DEL SCRIPT: '{scriptName}'
- Usá '{scriptName}' como nombre exacto de la clase C#
- La primera línea de cada bloque debe ser: // FileName: {scriptName}.cs
- Si el sistema requiere múltiples scripts, el principal debe llamarse '{scriptName}'

REGLAS ESTRICTAS:
- Responde SOLO con bloques de código C# usando la sintaxis: ```csharp
- Sin explicaciones ni texto fuera de los bloques de código
- Cada script debe estar en su propio bloque ```csharp separado
- La PRIMERA línea de cada bloque debe ser un comentario con el nombre del archivo:
  // FileName: NombreDelScript.cs
- Usa [SerializeField] para variables configurables desde el Inspector
- Incluye los usings necesarios al inicio de cada script
- El código debe compilar sin errores en Unity 2021 LTS o superior
- Para 2D: Rigidbody2D, Physics2D, Vector2
- Para 3D: Rigidbody, Physics, Vector3
- Aplica principios SOLID donde corresponda
- Usa UnityEvent para desacoplar componentes cuando sea útil
- Agrega comentarios XML en métodos públicos
- Si el sistema requiere más de 5 scripts, indicalo con un comentario al inicio
  antes del primer bloque: // EXCEEDS_LIMIT";

        public async Task<GenerationResult> HandleAsync(
            ClaudeConfig config,
            List<ChatMessage> history,
            string userPrompt,
            string scriptName)
        {
            try
            {
                // Build a dynamic system prompt that includes the user's chosen
                // script name so Claude uses it for both the class name and the
                // FileName hint — Unity requires both to match.
                string systemPrompt = BuildSystemPrompt(SanitizeName(scriptName));

                string raw = await ClaudeApiClient.Instance.SendAsync(
                    config, history, systemPrompt);

                // Hard limit check — Claude signals this with a comment
                if (raw.Contains("// EXCEEDS_LIMIT"))
                {
                    return GenerationResult.Fail(
                        "La petición requiere generar más de 5 scripts, lo cual supera el límite actual.\n\n" +
                        "💡 Probá dividiendo el sistema en partes:\n" +
                        "  1. Pedí primero la clase base o el manager principal\n" +
                        "  2. Luego pedí los scripts dependientes de a uno o dos");
                }

                var scripts = CodeExtractor.ExtractAll(raw);

                if (scripts.Count == 0)
                    return GenerationResult.Fail("Claude devolvió una respuesta vacía o sin bloques de código.");

                // Double-check limit on our side regardless of Claude's hint
                if (scripts.Count > MAX_SCRIPTS)
                {
                    return GenerationResult.Fail(
                        $"Claude generó {scripts.Count} scripts, lo cual supera el límite de {MAX_SCRIPTS}.\n\n" +
                        "💡 Probá dividiendo el sistema en partes más pequeñas.");
                }

                // Single script — use the user-provided name as filename
                if (scripts.Count == 1)
                {
                    string safeName = SanitizeName(scriptName);
                    string code = scripts[0].code;
                    string path = SaveScript(config.scriptsOutputPath, safeName, code);
                    int lines = code.Split('\n').Length;

                    return GenerationResult.Ok(
                        raw: raw,
                        display: $"✅ Script generado correctamente.\n\n" +
                                      $"📄 {safeName}.cs  •  {lines} líneas\n" +
                                      $"📁 {path}\n\n" +
                                      $"Adjuntalo a un GameObject con Add Component → {safeName}",
                        codePreview: code,
                        artifactPath: path);
                }

                // Multiple scripts — use Claude's FileName hints
                var savedPaths = new List<string>();
                var previewBldr = new StringBuilder();
                var summaryBldr = new StringBuilder();
                summaryBldr.AppendLine($"✅ {scripts.Count} scripts generados correctamente.\n");

                foreach (var (fileName, code) in scripts)
                {
                    string safeName = SanitizeName(Path.GetFileNameWithoutExtension(fileName));
                    string path = SaveScript(config.scriptsOutputPath, safeName, code);
                    int lines = code.Split('\n').Length;

                    savedPaths.Add(path);
                    summaryBldr.AppendLine($"📄 {safeName}.cs  •  {lines} líneas");

                    // Preview shows all scripts separated by a header comment
                    previewBldr.AppendLine($"// ═══════════════════════════════");
                    previewBldr.AppendLine($"// {safeName}.cs");
                    previewBldr.AppendLine($"// ═══════════════════════════════");
                    previewBldr.AppendLine(code);
                    previewBldr.AppendLine();
                }

                summaryBldr.AppendLine($"\n📁 Guardados en: {config.scriptsOutputPath}");
                summaryBldr.AppendLine("\nAdjuntalos a los GameObjects correspondientes desde Add Component.");

                return GenerationResult.Ok(
                    raw: raw,
                    display: summaryBldr.ToString().Trim(),
                    codePreview: previewBldr.ToString().Trim(),
                    artifactPath: savedPaths[0]);
            }
            catch (System.Exception ex)
            {
                return GenerationResult.Fail(ex.Message);
            }
        }

        // ── Private helpers ───────────────────────────────────

        private string SaveScript(string outputPath, string scriptName, string code)
        {
            if (!Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);

            string fullPath = Path.Combine(outputPath, $"{scriptName}.cs");
            File.WriteAllText(fullPath, code);
            AssetDatabase.ImportAsset(fullPath);
            AssetDatabase.Refresh();
            return fullPath;
        }

        private static string SanitizeName(string name)
        {
            string clean = name.Replace(".cs", "").Trim();
            return string.IsNullOrWhiteSpace(clean) ? "GeneratedScript" : clean;
        }
    }
}