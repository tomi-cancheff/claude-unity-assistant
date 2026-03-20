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
        private const int MAX_SCRIPTS = 5;

        public string Label => "📄 Script";

        public string SystemPrompt => BuildSystemPrompt("GeneratedScript");

        private static string LanguageInstruction => LanguageSettings.Current == AppLanguage.English
            ? "\n\nRespond in English. All comments, variable names hints and messages should be in English."
            : string.Empty;

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
  antes del primer bloque: // EXCEEDS_LIMIT" + LanguageInstruction;

        public async Task<GenerationResult> HandleAsync(
            ClaudeConfig config,
            List<ChatMessage> history,
            string userPrompt,
            string scriptName)
        {
            try
            {
                string systemPrompt = BuildSystemPrompt(SanitizeName(scriptName));

                string raw = await ClaudeApiClient.Instance.SendAsync(
                    config, history, systemPrompt);

                if (raw.Contains("// EXCEEDS_LIMIT"))
                {
                    return GenerationResult.Fail(LanguageSettings.Current == AppLanguage.English
                        ? "The request requires generating more than 5 scripts, which exceeds the current limit.\n\n" +
                          "💡 Try splitting the system into parts:\n" +
                          "  1. First request the base class or main manager\n" +
                          "  2. Then request the dependent scripts one or two at a time"
                        : "La petición requiere generar más de 5 scripts, lo cual supera el límite actual.\n\n" +
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

                if (scripts.Count == 1)
                {
                    string safeName = SanitizeName(scriptName);
                    string code = scripts[0].code;
                    string path = SaveScript(config.scriptsOutputPath, safeName, code);
                    int lines = code.Split('\n').Length;

                    string displayText = LanguageSettings.Current == AppLanguage.English
                        ? $"✅ Script generated successfully.\n\n" +
                          $"📄 {safeName}.cs  •  {lines} lines\n" +
                          $"📁 Saved to: {path}\n\n" +
                          $"Attach it to a GameObject via Add Component → {safeName}"
                        : $"✅ Script generado correctamente.\n\n" +
                          $"📄 {safeName}.cs  •  {lines} líneas\n" +
                          $"📁 Guardado en: {path}\n\n" +
                          $"Adjuntalo a un GameObject con Add Component → {safeName}";

                    return GenerationResult.Ok(
                        raw: raw,
                        display: displayText,
                        codePreview: code,
                        artifactPath: path);
                }

                var savedPaths = new List<string>();
                var previewBldr = new StringBuilder();
                var summaryBldr = new StringBuilder();
                summaryBldr.AppendLine(LanguageSettings.Current == AppLanguage.English
                    ? $"✅ {scripts.Count} scripts generated successfully.\n"
                    : $"✅ {scripts.Count} scripts generados correctamente.\n");

                foreach (var (fileName, code) in scripts)
                {
                    string safeName = SanitizeName(Path.GetFileNameWithoutExtension(fileName));
                    string path = SaveScript(config.scriptsOutputPath, safeName, code);
                    int lines = code.Split('\n').Length;

                    savedPaths.Add(path);
                    summaryBldr.AppendLine(LanguageSettings.Current == AppLanguage.English
                        ? $"📄 {safeName}.cs  •  {lines} lines"
                        : $"📄 {safeName}.cs  •  {lines} líneas");

                    previewBldr.AppendLine("// ═══════════════════════════════");
                    previewBldr.AppendLine($"// {safeName}.cs");
                    previewBldr.AppendLine("// ═══════════════════════════════");
                    previewBldr.AppendLine(code);
                    previewBldr.AppendLine();
                }

                summaryBldr.AppendLine(LanguageSettings.Current == AppLanguage.English
                    ? $"\n📁 Saved to: {config.scriptsOutputPath}"
                    : $"\n📁 Guardados en: {config.scriptsOutputPath}");
                summaryBldr.AppendLine(LanguageSettings.Current == AppLanguage.English
                    ? "\nAttach each script to the corresponding GameObjects via Add Component."
                    : "\nAdjuntalos a los GameObjects correspondientes desde Add Component.");

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
