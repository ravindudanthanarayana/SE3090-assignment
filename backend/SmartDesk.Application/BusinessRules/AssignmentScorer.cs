namespace SmartDesk.Application.BusinessRules;

/// <summary>Everything the scorer needs about one candidate support agent.</summary>
public sealed record CandidateInput(
    int UserId,
    string FullName,
    int SkillLevelForCategory,
    int OpenTicketCount);

public sealed record CandidateScore(
    int UserId,
    string FullName,
    double Score,
    int SkillLevel,
    int OpenTicketCount,
    string Explanation);

/// <summary>
/// Deterministic skill-versus-workload ranking. Used by BOTH
/// GET /api/assignments/recommendation/{ticketId} and the Assignment agent's ScoreAssignmentCandidates tool,
/// so a manager and the agent can never see different numbers. No LLM involved.
/// </summary>
public static class AssignmentScorer
{
    private const double SkillWeight = 10.0;
    private const double LoadPenalty = 3.0;

    public static IReadOnlyList<CandidateScore> Rank(IEnumerable<CandidateInput> candidates)
    {
        return candidates
            .Select(c =>
            {
                var score = Math.Round(c.SkillLevelForCategory * SkillWeight - c.OpenTicketCount * LoadPenalty, 2);
                var explanation =
                    $"Skill level {c.SkillLevelForCategory}/5 for this category (+{c.SkillLevelForCategory * SkillWeight}), " +
                    $"{c.OpenTicketCount} open ticket(s) (-{c.OpenTicketCount * LoadPenalty}).";
                return new CandidateScore(c.UserId, c.FullName, score, c.SkillLevelForCategory, c.OpenTicketCount, explanation);
            })
            .OrderByDescending(c => c.Score)
            .ThenBy(c => c.OpenTicketCount)
            .ThenBy(c => c.UserId)
            .ToList();
    }
}
