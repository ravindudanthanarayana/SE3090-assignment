using System.ComponentModel.DataAnnotations;
using SmartDesk.Domain.Enums;

namespace SmartDesk.Application.Dtos;

/// <summary>Dashboard KPIs. Scoped by role: an Employee sees only their own numbers.</summary>
public sealed record DashboardDto(
    int TotalTickets,
    int OpenTickets,
    int InProgressTickets,
    int ResolvedTickets,
    int ClosedTickets,
    int HighPriorityTickets,
    int EscalatedTickets,
    int SlaAtRiskTickets,
    int SlaBreachedTickets,
    int ActiveWorkflows,
    int PendingApprovals,
    IReadOnlyList<CountByLabelDto> ByStatus,
    IReadOnlyList<CountByLabelDto> ByPriority,
    IReadOnlyList<CountByLabelDto> ByCategory);

public sealed record CountByLabelDto(string Label, int Count);

public sealed record SlaReportDto(
    int OnTrack,
    int AtRisk,
    int Breached,
    double BreachRatePercent,
    IReadOnlyList<SlaCategoryBreakdownDto> ByCategory);

public sealed record SlaCategoryBreakdownDto(
    string CategoryName,
    int Total,
    int AtRisk,
    int Breached);

public sealed record AgentPerformanceDto(
    int UserId,
    string FullName,
    int AssignedTotal,
    int Resolved,
    int OpenNow,
    double AverageResolutionHours,
    int Breached,
    double BreachRatePercent);

public sealed class EscalateTicketRequest
{
    [Required, MinLength(10), MaxLength(1000)]
    public string Reason { get; set; } = string.Empty;
}

public sealed record SlaAtRiskTicketDto(
    int Id,
    string TicketNumber,
    string Title,
    TicketPriority Priority,
    TicketStatus Status,
    string? AssignedToName,
    DateTime SlaDueAt,
    double HoursRemaining,
    SlaState SlaState);
