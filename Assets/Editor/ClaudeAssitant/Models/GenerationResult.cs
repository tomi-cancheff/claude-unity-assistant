// ============================================================
//  GenerationResult.cs
//  Value object returned by every IGenerationHandler.
//  Uses the static factory pattern for expressive construction.
//
//  Three distinct pieces of content:
//    DisplayText  → shown in the chat bubble (always human-readable)
//    CodePreview  → shown in the code panel (the actual generated code)
//    RawContent   → Claude's full unprocessed response (for debugging)
// ============================================================

namespace ClaudeAssistant.Models
{
    public class GenerationResult
    {
        public bool Success { get; private set; }
        public string RawContent { get; private set; }  // Claude's full response
        public string DisplayText { get; private set; }  // Friendly chat message
        public string CodePreview { get; private set; }  // Extracted code for the preview panel
        public string ErrorMessage { get; private set; }
        public string ArtifactPath { get; private set; }  // Saved file path

        private GenerationResult() { }

        public static GenerationResult Ok(
            string raw,
            string display,
            string codePreview = null,
            string artifactPath = null) =>
            new GenerationResult
            {
                Success = true,
                RawContent = raw,
                DisplayText = display,
                CodePreview = codePreview,
                ArtifactPath = artifactPath
            };

        public static GenerationResult Fail(string errorMessage) =>
            new GenerationResult
            {
                Success = false,
                ErrorMessage = errorMessage,
                DisplayText = $"❌ {errorMessage}"
            };
    }
}