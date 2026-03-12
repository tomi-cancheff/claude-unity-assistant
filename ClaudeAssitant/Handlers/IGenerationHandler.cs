// ============================================================
//  IGenerationHandler.cs
//  Strategy interface.  Every generation mode (Script, Scene, …)
//  implements this contract.  The window only talks to this
//  interface — adding a new mode never touches existing code.
//
//  Pattern: Strategy
// ============================================================

using System.Collections.Generic;
using System.Threading.Tasks;
using ClaudeAssistant.Core;
using ClaudeAssistant.Models;

namespace ClaudeAssistant.Handlers
{
    public interface IGenerationHandler
    {
        /// <summary>Human-readable label shown in the UI.</summary>
        string Label { get; }

        /// <summary>System prompt injected for this mode.</summary>
        string SystemPrompt { get; }

        /// <summary>
        /// Execute the generation pipeline: call the API, process
        /// the response, and produce side effects (save files, build scene…).
        /// </summary>
        Task<GenerationResult> HandleAsync(
            ClaudeConfig      config,
            List<ChatMessage> history,
            string            userPrompt,
            string            artifactName);
    }
}
