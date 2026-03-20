// ============================================================
//  SceneGenerationHandler.cs
//  Concrete Strategy: generates an Editor-only C# script that
//  constructs GameObjects/layouts in the active scene, executes
//  it via MenuItem reflection, then optionally deletes it.
//
//  The generated script uses Undo so every action is reversible
//  (Ctrl+Z) inside the Unity editor.
//
//  Pattern: Strategy + Command (via Unity Undo system)
// ============================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using ClaudeAssistant.Core;
using ClaudeAssistant.Models;
using ClaudeAssistant.Utils;
using UnityEditor;
using UnityEngine;

namespace ClaudeAssistant.Handlers
{
    public class SceneGenerationHandler : IGenerationHandler
    {
        public string Label => "🏗️ Scene";

        public string SystemPrompt =>
            $@"Eres un experto en Unity3D. Tu única tarea es generar scripts de Editor que construyan escenas.

REGLAS ESTRICTAS:
- Responde SOLO con código C# dentro de un bloque ```csharp ... ```
- El script DEBE ser una clase estática llamada exactamente: {SceneGenerationConstants.ClassName}
- Debe tener exactamente este MenuItem: [MenuItem(""{SceneGenerationConstants.MenuPath}"")]
- Usa SIEMPRE Undo.RegisterCreatedObjectUndo() para cada GameObject creado
- Organiza objetos en la jerarquía con GameObjects vacíos como contenedores
- Usa primitivas de Unity: GameObject.CreatePrimitive(PrimitiveType.X)
- Para 2D: agrega BoxCollider2D / CircleCollider2D y Rigidbody2D donde sea necesario
- Para 3D: agrega MeshCollider / BoxCollider y Rigidbody donde sea necesario
- Aplica materiales solo si están en Resources/, si no, deja el default
- Siempre incluye: using UnityEngine; using UnityEditor;
- Sin explicaciones fuera del bloque de código" +
            (LanguageSettings.Current == AppLanguage.English
                ? "\n\nRespond in English. All comments, variable names hints and messages should be in English."
                : string.Empty);

        public async Task<GenerationResult> HandleAsync(
            ClaudeConfig config,
            List<ChatMessage> history,
            string userPrompt,
            string artifactName)
        {
            try
            {
                string raw = await ClaudeApiClient.Instance.SendAsync(
                    config, history, SystemPrompt);

                string code = CodeExtractor.Extract(raw);
                if (string.IsNullOrWhiteSpace(code))
                    return GenerationResult.Fail("Claude devolvió una respuesta vacía.");

                string scriptPath = SaveEditorScript(config.editorScriptsPath, code);

                if (config.autoExecuteSceneScripts)
                    ScheduleExecution(scriptPath, config);

                string status = config.autoExecuteSceneScripts
                    ? "✅ Escena generada y ejecutada.\n\n" +
                      "🏗️ Los GameObjects fueron creados en la Hierarchy.\n" +
                      "↩️ Podés deshacer con Ctrl+Z si algo no quedó bien."
                    : $"✅ Script de escena guardado.\n\n" +
                      $"▶️ Ejecutalo manualmente desde el menú:\n" +
                      $"ClaudeAssistant → Internal → ExecuteScene";

                return GenerationResult.Ok(
                    raw: raw,
                    display: status,
                    codePreview: code,
                    artifactPath: scriptPath);
            }
            catch (Exception ex)
            {
                return GenerationResult.Fail(ex.Message);
            }
        }

        private string SaveEditorScript(string outputPath, string code)
        {
            if (!Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);

            string fullPath = Path.Combine(outputPath, SceneGenerationConstants.FileName);

            if (File.Exists(fullPath))
            {
                AssetDatabase.DeleteAsset(fullPath);
                AssetDatabase.Refresh();
            }

            File.WriteAllText(fullPath, code);
            AssetDatabase.ImportAsset(fullPath);
            AssetDatabase.Refresh();
            return fullPath;
        }

        private void ScheduleExecution(string scriptPath, ClaudeConfig config)
        {
            SceneScriptExecutor.Schedule(scriptPath, config.editorScriptsPath);
        }
    }

    public class SceneScriptExecutor : AssetPostprocessor
    {
        private static string _pendingScriptPath;
        private static string _editorScriptsFolder;

        public static void Schedule(string scriptPath, string editorFolder)
        {
            _pendingScriptPath = scriptPath;
            _editorScriptsFolder = editorFolder;
        }

        private static void OnPostprocessAllAssets(
            string[] imported, string[] deleted,
            string[] moved, string[] movedFrom)
        {
            if (string.IsNullOrEmpty(_pendingScriptPath)) return;

            string normalized = _pendingScriptPath.Replace("\\", "/");
            if (!Array.Exists(imported, p => p.Replace("\\", "/") == normalized)) return;

            try
            {
                var method = FindGeneratedMethod();
                if (method != null)
                {
                    method.Invoke(null, null);
                    Debug.Log("[ClaudeAssistant] Scene built successfully. Press Ctrl+Z to undo.");
                }
                else
                {
                    Debug.LogWarning("[ClaudeAssistant] Could not find generated method. Run it manually via the menu.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ClaudeAssistant] Scene execution failed: {ex.Message}");
            }
            finally
            {
                CleanUp();
            }
        }

        private static MethodInfo FindGeneratedMethod()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(SceneGenerationConstants.ClassName);
                if (type == null) continue;

                foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (method.GetCustomAttribute<MenuItem>() != null)
                        return method;
                }
            }
            return null;
        }

        private static void CleanUp()
        {
            _pendingScriptPath = null;
            string folder = _editorScriptsFolder;

            EditorApplication.delayCall += () =>
            {
                string path = Path.Combine(folder, SceneGenerationConstants.FileName);
                if (File.Exists(path))
                {
                    AssetDatabase.DeleteAsset(path);
                    AssetDatabase.Refresh();
                }
            };
        }
    }
}
