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
//  Overwrite protection:
//    When config.avoidOverwrite is true, never silently overwrites
//    an existing file. Appends _v2, _v3, etc. until a free path
//    is found.
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
        private const int MAX_SCRIPTS = 5;

        // ── IGenerationHandler ────────────────────────────────

        public string Label => "📄 Script";

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
            ClaudeConfig      config,
            List<ChatMessage> history,
            string            userPrompt,
            string            scriptName)
        {
            try
            {
                string systemPrompt = BuildSystemPrompt(SanitizeName(scriptName));

                string raw = await ClaudeApiClient.Instance.SendAsync(
                    config, history, systemPrompt);

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

                if (scripts.Count > MAX_SCRIPTS)
                {
                    return GenerationResult.Fail(
                        $"Claude generó {scripts.Count} scripts, lo cual supera el límite de {MAX_SCRIPTS}.\n\n" +
                        "💡 Probá dividiendo el sistema en partes más pequeñas.");
                }

                // ── Single script ─────────────────────────────
                if (scripts.Count == 1)
                {
                    string safeName = SanitizeName(scriptName);
                    string code     = scripts[0].code;
                    string path     = SaveScript(config, safeName, code);
                    int    lines    = code.Split('\n').Length;

                    // Use the actual saved name (may have been versioned)
                    string savedName = Path.GetFileNameWithoutExtension(path);

                    return GenerationResult.Ok(
                        raw:          raw,
                        display:      $"✅ Script generado correctamente.\n\n" +
                                      $"📄 {savedName}.cs  •  {lines} líneas\n" +
                                      $"📁 {path}\n\n" +
                                      $"Adjuntalo a un GameObject con Add Component → {savedName}",
                        codePreview:  code,
                        artifactPath: path);
                }

                // ── Multiple scripts ──────────────────────────
                var savedPaths  = new List<string>();
                var previewBldr = new StringBuilder();
                var summaryBldr = new StringBuilder();
                summaryBldr.AppendLine($"✅ {scripts.Count} scripts generados correctamente.\n");

                foreach (var (fileName, code) in scripts)
                {
                    string safeName  = SanitizeName(Path.GetFileNameWithoutExtension(fileName));
                    string path      = SaveScript(config, safeName, code);
                    string savedName = Path.GetFileNameWithoutExtension(path);
                    int    lines     = code.Split('\n').Length;

                    savedPaths.Add(path);
                    summaryBldr.AppendLine($"📄 {savedName}.cs  •  {lines} líneas");

                    previewBldr.AppendLine($"// ═══════════════════════════════");
                    previewBldr.AppendLine($"// {savedName}.cs");
                    previewBldr.AppendLine($"// ═══════════════════════════════");
                    previewBldr.AppendLine(code);
                    previewBldr.AppendLine();
                }

                summaryBldr.AppendLine($"\n📁 Guardados en: {config.scriptsOutputPath}");
                summaryBldr.AppendLine("\nAdjuntalos a los GameObjects correspondientes desde Add Component.");

                return GenerationResult.Ok(
                    raw:          raw,
                    display:      summaryBldr.ToString().Trim(),
                    codePreview:  previewBldr.ToString().Trim(),
                    artifactPath: savedPaths[0]);
            }
            catch (System.Exception ex)
            {
                return GenerationResult.Fail(ex.Message);
            }
        }

        // ── Private helpers ───────────────────────────────────

        /// <summary>
        /// Saves a script to disk. If config.avoidOverwrite is true and the
        /// file already exists, appends _v2, _v3, etc. until a free path is found.
        /// </summary>
        private string SaveScript(ClaudeConfig config, string scriptName, string code)
        {
            string outputPath = config.scriptsOutputPath;

            if (!Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);

            string basePath  = Path.Combine(outputPath, $"{scriptName}.cs");
            string finalPath = basePath;

            // Overwrite protection — find next available versioned name
            if (config.avoidOverwrite && File.Exists(basePath))
            {
                int version = 2;
                do
                {
                    finalPath = Path.Combine(outputPath, $"{scriptName}_v{version}.cs");
                    version++;
                }
                while (File.Exists(finalPath));

                Debug.Log($"[ClaudeAssistant] '{scriptName}.cs' already exists — saving as '{Path.GetFileName(finalPath)}'");
            }

            File.WriteAllText(finalPath, code);
            AssetDatabase.ImportAsset(finalPath);
            AssetDatabase.Refresh();
            return finalPath;
        }

        private static string SanitizeName(string name)
        {
            string clean = name.Replace(".cs", "").Trim();
            return string.IsNullOrWhiteSpace(clean) ? "GeneratedScript" : clean;
        }
    }
}