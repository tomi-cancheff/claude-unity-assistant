// ============================================================
//  ClaudeApiClient.cs
//  Singleton service responsible for all HTTP communication
//  with the Anthropic Messages API.
//
//  Pattern: Singleton service + async/await
// ============================================================

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ClaudeAssistant.Models;
using UnityEngine;
using UnityEngine.Networking;

namespace ClaudeAssistant.Core
{
    public class ClaudeApiClient
    {
        // ── Singleton ─────────────────────────────────────────

        private static ClaudeApiClient _instance;
        public static ClaudeApiClient Instance => _instance ??= new ClaudeApiClient();
        private ClaudeApiClient() { }

        // ── Constants ─────────────────────────────────────────

        private const string API_URL = "https://api.anthropic.com/v1/messages";
        private const string API_VERSION = "2023-06-01";

        // ── Public API ────────────────────────────────────────

        /// <summary>
        /// Sends a multi-turn conversation to the Claude API and returns the
        /// assistant reply as a plain string.
        /// </summary>
        /// <param name="config">User configuration (key, model, etc.).</param>
        /// <param name="history">Full conversation so far (user + assistant turns).</param>
        /// <param name="systemPrompt">High-level instruction prepended to every call.</param>
        public async Task<string> SendAsync(
            ClaudeConfig config,
            List<ChatMessage> history,
            string systemPrompt)
        {
            if (!config.IsValid)
                throw new InvalidOperationException("API key is missing. Open the Claude Assistant window and enter your key.");

            string requestBody = BuildRequestBody(config, history, systemPrompt);

            if (config.verboseLogging)
                Debug.Log($"[ClaudeAssistant] Sending request:\n{requestBody}");

            using var www = new UnityWebRequest(API_URL, "POST");
            www.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(requestBody));
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("x-api-key", config.ApiKey);
            www.SetRequestHeader("anthropic-version", API_VERSION);

            var operation = www.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

            string responseText = www.downloadHandler.text;

            if (www.result != UnityWebRequest.Result.Success)
            {
                string apiError = TryExtractApiError(responseText);
                throw new Exception(apiError ?? $"HTTP {www.responseCode}: {www.error}");
            }

            if (config.verboseLogging)
                Debug.Log($"[ClaudeAssistant] Response:\n{responseText}");

            return ParseAssistantReply(responseText);
        }

        // ── Private helpers ───────────────────────────────────

        private string BuildRequestBody(ClaudeConfig config, List<ChatMessage> history, string systemPrompt)
        {
            var messages = new List<ApiMessage>();

            foreach (var msg in history)
                messages.Add(new ApiMessage(msg.ApiRole, msg.Content));

            var request = new ApiRequest
            {
                model = config.ModelId,
                max_tokens = config.maxTokens,
                system = systemPrompt,
                messages = messages
            };

            return JsonUtility.ToJson(request);
        }

        private string ParseAssistantReply(string json)
        {
            var response = JsonUtility.FromJson<ApiResponse>(json);

            if (response?.content == null || response.content.Length == 0)
                throw new Exception("Claude returned an empty response.");

            return response.content[0].text;
        }

        private string TryExtractApiError(string json)
        {
            try
            {
                var err = JsonUtility.FromJson<ApiError>(json);
                return err?.error?.message;
            }
            catch { return null; }
        }
    }
}