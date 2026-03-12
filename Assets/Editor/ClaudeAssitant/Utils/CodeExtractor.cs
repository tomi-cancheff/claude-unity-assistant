// ============================================================
//  CodeExtractor.cs
//  Strips markdown code fences from Claude's response so the
//  raw C# can be written directly to .cs files.
//
//  Two extraction modes:
//    Extract()    → single script (legacy, backwards compatible)
//    ExtractAll() → multiple named scripts from one response
//
//  Naming convention Claude must follow for multi-script:
//    ```csharp // FileName: SpawnManager.cs
//    ...
//    ```
// ============================================================

using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ClaudeAssistant.Utils
{
    public static class CodeExtractor
    {
        // Matches: ```csharp ... ``` with optional language tag
        private static readonly Regex CodeFencePattern =
            new Regex(@"```(?:csharp|cs|unity)?\s*([\s\S]*?)```",
                      RegexOptions.IgnoreCase | RegexOptions.Multiline);

        // Matches the filename hint Claude writes as first comment:
        //   // FileName: MyClass.cs
        private static readonly Regex FileNameHintPattern =
            new Regex(@"//\s*FileName\s*:\s*([\w]+(?:\.cs)?)",
                      RegexOptions.IgnoreCase);

        /// <summary>
        /// Returns the code inside the first fenced block, or the whole
        /// response if no fences are found (backwards compatible).
        /// </summary>
        public static string Extract(string rawResponse)
        {
            if (string.IsNullOrWhiteSpace(rawResponse))
                return string.Empty;

            var match = CodeFencePattern.Match(rawResponse);
            return match.Success
                ? match.Groups[1].Value.Trim()
                : rawResponse.Trim();
        }

        /// <summary>
        /// Extracts all fenced code blocks from a multi-script response.
        /// Each entry is (fileName, code). If Claude didn't include a
        /// FileName hint, falls back to "Script_N".
        /// </summary>
        public static List<(string fileName, string code)> ExtractAll(string rawResponse)
        {
            var results = new List<(string, string)>();

            if (string.IsNullOrWhiteSpace(rawResponse))
                return results;

            int index = 1;
            foreach (Match match in CodeFencePattern.Matches(rawResponse))
            {
                string code = match.Groups[1].Value.Trim();
                if (string.IsNullOrWhiteSpace(code)) continue;

                // Try to read the FileName hint from the first line of code
                string fileName = $"Script_{index}";
                var hint = FileNameHintPattern.Match(code);
                if (hint.Success)
                {
                    fileName = hint.Groups[1].Value.Trim();
                    if (!fileName.EndsWith(".cs"))
                        fileName += ".cs";
                }

                results.Add((fileName, code));
                index++;
            }

            return results;
        }
    }
}