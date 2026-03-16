// ============================================================
//  ChatMessage.cs
//  Immutable data model representing a single conversation turn.
// ============================================================

using System;

namespace ClaudeAssistant.Models
{
    public enum MessageRole { User, Assistant, System }

    public enum GenerationMode { Script, Scene, Consult, Unknown }

    [Serializable]
    public class ChatMessage
    {
        public readonly MessageRole Role;
        public readonly string Content;
        public readonly DateTime Timestamp;
        public readonly GenerationMode Mode;

        public ChatMessage(MessageRole role, string content, GenerationMode mode = GenerationMode.Unknown)
        {
            Role = role;
            Content = content;
            Timestamp = DateTime.Now;
            Mode = mode;
        }

        /// <summary>
        /// Reconstruction constructor used by PersistedMessage when
        /// restoring history after a domain reload.
        /// </summary>
        internal ChatMessage(MessageRole role, string content, GenerationMode mode, DateTime timestamp)
        {
            Role = role;
            Content = content;
            Timestamp = timestamp;
            Mode = mode;
        }

        /// <summary>Converts to the API format expected by Anthropic.</summary>
        public string ApiRole => Role == MessageRole.User ? "user" : "assistant";
    }
}