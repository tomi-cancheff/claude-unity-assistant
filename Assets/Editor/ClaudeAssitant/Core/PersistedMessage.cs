// ============================================================
//  PersistedMessage.cs
//  Serializable DTO that mirrors ChatMessage for Unity's
//  serialization system. Lives on the EditorWindow via
//  [SerializeField] so it survives domain reloads (recompiles).
//
//  ChatMessage stays immutable — this is purely a persistence
//  layer. Convert to/from ChatMessage at the boundary.
// ============================================================

using System;
using UnityEngine;
using ClaudeAssistant.Models;

namespace ClaudeAssistant.Core
{
    [Serializable]
    public class PersistedMessage
    {
        [SerializeField] public string role;
        [SerializeField] public string content;
        [SerializeField] public string mode;
        [SerializeField] public string timestamp;

        // ── Factories ─────────────────────────────────────────

        public static PersistedMessage From(ChatMessage msg) => new PersistedMessage
        {
            role      = msg.Role.ToString(),
            content   = msg.Content,
            mode      = msg.Mode.ToString(),
            timestamp = msg.Timestamp.ToString("o")   // ISO 8601 round-trip
        };

        public ChatMessage ToChatMessage()
        {
            Enum.TryParse(role, out MessageRole    parsedRole);
            Enum.TryParse(mode, out GenerationMode parsedMode);

            DateTime.TryParse(timestamp,
                null,
                System.Globalization.DateTimeStyles.RoundtripKind,
                out DateTime parsedTime);

            return new ChatMessage(parsedRole, content, parsedMode, parsedTime);
        }
    }
}
