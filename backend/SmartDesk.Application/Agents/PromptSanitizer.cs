using System.Text.RegularExpressions;

namespace SmartDesk.Application.Agents;

/// <summary>
/// Prepares untrusted ticket text for a prompt (spec section 9.10).
///
/// This is defence in depth, not the primary defence. Prompt injection is defeated structurally:
/// an agent can only emit a fixed JSON shape, can only name tools in its own allow-list, and cannot
/// change any record - every write goes through BusinessRuleEngine and human approval. Sanitising
/// simply removes the easiest attempts and stops crafted text from breaking our own delimiters.
/// </summary>
public static partial class PromptSanitizer
{
    public const int MaxContentLength = 8000;

    [GeneratedRegex(@"</?untrusted_user_content>", RegexOptions.IgnoreCase)]
    private static partial Regex DelimiterPattern();

    /// <summary>Wraps user-supplied text so the model is told explicitly that it is data, not instructions.</summary>
    public static string WrapUntrusted(string label, string content)
    {
        return $"<untrusted_user_content source=\"{Escape(label)}\">\n{Sanitize(content)}\n</untrusted_user_content>";
    }

    public static string Sanitize(string? content)
    {
        if (string.IsNullOrWhiteSpace(content)) return string.Empty;

        // Stop crafted text from closing our delimiter and escaping the untrusted block.
        var cleaned = DelimiterPattern().Replace(content, "[removed]");

        // Drop control characters, keeping newlines and tabs so the text stays readable.
        cleaned = new string(cleaned
            .Where(c => !char.IsControl(c) || c is '\n' or '\r' or '\t')
            .ToArray());

        cleaned = cleaned.Trim();
        if (cleaned.Length > MaxContentLength)
            cleaned = cleaned[..MaxContentLength] + "\n[truncated]";

        return cleaned;
    }

    private static string Escape(string label) =>
        new(label.Where(char.IsLetterOrDigit).ToArray());
}
