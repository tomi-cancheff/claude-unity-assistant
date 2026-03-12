// ============================================================
//  ClaudeApiModels.cs
//  Serialization models for the Anthropic Messages API.
//  Kept intentionally thin — pure data, no logic.
// ============================================================

using System;
using System.Collections.Generic;

namespace ClaudeAssistant.Models
{
    // ── Request ─────────────────────────────────────────────

    [Serializable]
    public class ApiRequest
    {
        public string model;
        public int max_tokens;
        public string system;
        public List<ApiMessage> messages = new();
    }

    [Serializable]
    public class ApiMessage
    {
        public string role;
        public string content;

        public ApiMessage(string role, string content)
        {
            this.role    = role;
            this.content = content;
        }
    }

    // ── Response ─────────────────────────────────────────────

    [Serializable]
    public class ApiResponse
    {
        public string id;
        public string type;
        public ApiContent[] content;
        public ApiUsage usage;
    }

    [Serializable]
    public class ApiContent
    {
        public string type;
        public string text;
    }

    [Serializable]
    public class ApiUsage
    {
        public int input_tokens;
        public int output_tokens;
    }

    // ── Error ────────────────────────────────────────────────

    [Serializable]
    public class ApiError
    {
        public ApiErrorBody error;
    }

    [Serializable]
    public class ApiErrorBody
    {
        public string type;
        public string message;
    }
}
