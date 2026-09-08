namespace SmartDesk.Application.BusinessRules;

public sealed record ArticleCandidate(int Id, string Title, string Body, int CategoryId, string[] Tags);

public sealed record ScoredArticle(int Id, double Score, string MatchReason);

/// <summary>
/// Deterministic keyword-and-category relevance scoring for knowledge articles.
/// Backs both GET /api/knowledge-articles/relevant and the SearchKnowledgeBase tool, so the
/// Solution agent can only ever see articles our own ranking produced.
/// </summary>
public static class ArticleRelevance
{
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the","a","an","and","or","but","is","are","was","were","to","of","in","on","for","with",
        "my","i","it","this","that","have","has","not","cannot","can","when","при","at","be","am"
    };

    public static IReadOnlyList<string> Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return [];
        return text
            .Split([' ', '\t', '\n', '\r', '.', ',', ';', ':', '!', '?', '(', ')', '[', ']', '/', '\\', '"', '\''],
                StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim().ToLowerInvariant())
            .Where(t => t.Length > 2 && !StopWords.Contains(t))
            .Distinct()
            .ToList();
    }

    public static IReadOnlyList<ScoredArticle> Score(
        string queryText,
        int? ticketCategoryId,
        IEnumerable<ArticleCandidate> articles)
    {
        var terms = Tokenize(queryText);

        return articles
            .Select(a =>
            {
                double score = 0;
                var reasons = new List<string>();

                if (ticketCategoryId.HasValue && a.CategoryId == ticketCategoryId.Value)
                {
                    score += 3.0;
                    reasons.Add("same category");
                }

                var titleHits = terms.Count(t => a.Title.Contains(t, StringComparison.OrdinalIgnoreCase));
                if (titleHits > 0)
                {
                    score += titleHits * 2.0;
                    reasons.Add($"{titleHits} title keyword match(es)");
                }

                var tagHits = terms.Count(t => a.Tags.Any(tag => tag.Equals(t, StringComparison.OrdinalIgnoreCase)));
                if (tagHits > 0)
                {
                    score += tagHits * 1.5;
                    reasons.Add($"{tagHits} tag match(es)");
                }

                var bodyHits = terms.Count(t => a.Body.Contains(t, StringComparison.OrdinalIgnoreCase));
                if (bodyHits > 0)
                {
                    score += bodyHits * 0.5;
                    reasons.Add($"{bodyHits} body keyword match(es)");
                }

                var reason = reasons.Count > 0 ? string.Join(", ", reasons) : "no direct match";
                return new ScoredArticle(a.Id, Math.Round(score, 2), reason);
            })
            .Where(s => s.Score > 0)
            .OrderByDescending(s => s.Score)
            .ThenBy(s => s.Id)
            .ToList();
    }
}
