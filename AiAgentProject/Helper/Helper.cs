using System.Text;
using System.Text.RegularExpressions;

namespace AiAgentProject.Helper;

public static  class Helper
{
        // --- Helpers ---------------------------------------------------------------
    public static bool IsBase64Payload(string input)
    {
        // Heuristic: only base64 chars and length multiple of 4
        if (string.IsNullOrWhiteSpace(input)) return false;
        var s = input.Trim();
        if (s.Length < 16) return false; // too short to be an encoded instruction
        if (s.Length % 4 != 0) return false;
        if (!Regex.IsMatch(s, "^[A-Za-z0-9+/=]+$")) return false;
        return TryDecodeBase64(s, out var decoded) && LooksLikeNaturalLanguage(decoded);
    }

    private static bool TryDecodeBase64(string input, out string decoded)
    {
        try
        {
            var bytes = Convert.FromBase64String(input);
            decoded = Encoding.UTF8.GetString(bytes);
            return true;
        }
        catch
        {
            decoded = string.Empty;
            return false;
        }
    }
    
    public static readonly string[] ForbiddenPhrases = new[]
    {
        "ignore previous instruction",
        "act as",
        "forget everything",
        "bypass",
        "jailbreak",
        "you are now"
    };

    public static string SanitizeInput(string input)
    {
        if (ForbiddenPhrases.Any(p => input.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0))
        {
            return "User input contained disallowed instructions. Request ignored for safety.";
        }

        return input;
    }

    public static bool LooksLikeNaturalLanguage(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        // Basic heuristic: contains spaces and common words or punctuation
        int spaces = text.Count(c => c == ' ');
        if (spaces < 3) return false;
        var lower = text.ToLowerInvariant();
        string[] hints = { "dear", "assistant", "write", "article", "start the", "please", "help", "explain" };
        return hints.Any(h => lower.Contains(h));
    }
    // --------------------------------------------------------------------------
}