// ============================================================
//  SceneGenerationConstants.cs
//  Single source of truth for the class name and MenuItem path
//  used by both SceneGenerationHandler and SceneScriptExecutor.
//
//  Changing a value here propagates automatically to all usages.
// ============================================================

namespace ClaudeAssistant.Handlers
{
    internal static class SceneGenerationConstants
    {
        /// <summary>
        /// Exact class name Claude must use in the generated scene script.
        /// Used by SceneGenerationHandler (prompt injection) and
        /// SceneScriptExecutor (reflection lookup + file cleanup).
        /// </summary>
        internal const string ClassName = "ClaudeGeneratedSceneBuilder";

        /// <summary>
        /// Exact MenuItem path Claude must register in the generated script.
        /// Must match what SceneScriptExecutor looks for via reflection.
        /// </summary>
        internal const string MenuPath = "ClaudeAssistant/Internal/ExecuteScene";

        /// <summary>Generated script filename derived from ClassName.</summary>
        internal const string FileName = ClassName + ".cs";
    }
}
