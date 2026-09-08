using SmartDesk.Domain.Enums;

namespace SmartDesk.Application.BusinessRules;

/// <summary>
/// The only place ticket status transitions are decided. Deterministic and outside the LLM
/// (spec: "Do not let the LLM decide security permissions" / business rules).
/// </summary>
public static class TicketStatusMachine
{
    private static readonly Dictionary<TicketStatus, TicketStatus[]> Allowed = new()
    {
        [TicketStatus.New] = [TicketStatus.Assigned, TicketStatus.Cancelled, TicketStatus.Escalated],
        [TicketStatus.Assigned] = [TicketStatus.InProgress, TicketStatus.OnHold, TicketStatus.Escalated, TicketStatus.Resolved],
        [TicketStatus.InProgress] = [TicketStatus.OnHold, TicketStatus.Resolved, TicketStatus.Escalated],
        [TicketStatus.OnHold] = [TicketStatus.InProgress, TicketStatus.Escalated],
        [TicketStatus.Escalated] = [TicketStatus.InProgress, TicketStatus.Resolved],
        [TicketStatus.Resolved] = [TicketStatus.Closed, TicketStatus.InProgress],
        [TicketStatus.Closed] = [],
        [TicketStatus.Cancelled] = []
    };

    public static IReadOnlyList<TicketStatus> AllowedNext(TicketStatus current) =>
        Allowed.TryGetValue(current, out var next) ? next : [];

    public static bool CanTransition(TicketStatus from, TicketStatus to) =>
        AllowedNext(from).Contains(to);

    /// <summary>Statuses that still count as active work for workload and dashboard counts.</summary>
    public static readonly TicketStatus[] OpenStatuses =
    [
        TicketStatus.New, TicketStatus.Assigned, TicketStatus.InProgress,
        TicketStatus.OnHold, TicketStatus.Escalated
    ];

    public static bool IsOpen(TicketStatus status) => OpenStatuses.Contains(status);
}
