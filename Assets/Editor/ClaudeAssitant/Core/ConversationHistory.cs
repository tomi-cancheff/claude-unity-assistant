// ============================================================
//  ConversationHistory.cs
//  Manages the in-memory chat log for a single session.
//  Handles context-window trimming so the API never receives
//  a payload that's too large.
//
//  Pattern: Repository (stateful in-memory store)
// ============================================================

using System.Collections.Generic;
using System.Linq;
using ClaudeAssistant.Models;

namespace ClaudeAssistant.Core
{
    public class ConversationHistory
    {
        // ── Config ────────────────────────────────────────────

        /// <summary>Maximum turns kept in the window sent to the API.</summary>
        private const int MaxApiTurns = 20;

        // ── State ─────────────────────────────────────────────

        private readonly List<ChatMessage> _messages = new();

        // ── Public API ────────────────────────────────────────

        public IReadOnlyList<ChatMessage> Messages => _messages;

        public void Add(ChatMessage message) => _messages.Add(message);

        public void Clear() => _messages.Clear();

        /// <summary>
        /// Returns the slice of the history that will be sent to the API.
        /// Keeps the oldest user message for context anchoring, then the
        /// most recent <see cref="MaxApiTurns"/> turns.
        /// </summary>
        public List<ChatMessage> GetApiWindow()
        {
            if (_messages.Count <= MaxApiTurns)
                return _messages.ToList();

            // Always anchor with the very first user message
            var first = _messages.First(m => m.Role == MessageRole.User);
            var recent = _messages.TakeLast(MaxApiTurns - 1).ToList();

            if (!recent.Contains(first))
                recent.Insert(0, first);

            return recent;
        }
    }
}
