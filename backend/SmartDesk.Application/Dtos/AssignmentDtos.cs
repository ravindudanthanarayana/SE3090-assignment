using System.ComponentModel.DataAnnotations;
using SmartDesk.Domain.Enums;

namespace SmartDesk.Application.Dtos;

public sealed class AssignTicketRequest
{
    [Range(1, int.MaxValue)]
    public int AssignedToUserId { get; set; }

    [MaxLength(1000)]
    public string? Reason { get; set; }
}

public sealed class UpsertSkillRequest
{
    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }

    [Range(1, 5)]
    public int ProficiencyLevel { get; set; } = 3;
}

public sealed record SupportAgentDto(
    int UserId,
    string FullName,
    string Email,
    string? Department,
    bool IsActive,
    IReadOnlyList<AgentSkillDto> Skills,
    AgentWorkloadDto Workload);

public sealed record AgentSkillDto(int Id, int CategoryId, string CategoryName, int ProficiencyLevel);

public sealed record AgentWorkloadDto(
    int UserId,
    string FullName,
    int OpenCount,
    int InProgressCount,
    int AtRiskCount,
    int BreachedCount,
    int ResolvedLast30Days);

/// <summary>
/// Output of the deterministic assignment scorer. The same function backs both
/// GET /api/assignments/recommendation/{ticketId} and the Assignment agent's tool,
/// so the API and the agent can never disagree.
/// </summary>
public sealed record AssignmentCandidateDto(
    int UserId,
    string FullName,
    double Score,
    int SkillLevel,
    int CurrentOpenTickets,
    string Explanation);

public sealed record AssignmentRecommendationDto(
    int TicketId,
    IReadOnlyList<AssignmentCandidateDto> Candidates,
    int? TopCandidateUserId);

public sealed record TicketAssignmentDto(
    int Id,
    int AssignedToUserId,
    string AssignedToName,
    string? AssignedByName,
    string? Reason,
    AssignmentSource Source,
    DateTime CreatedAt);
