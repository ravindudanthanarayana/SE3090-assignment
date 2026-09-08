using SmartDesk.Domain.Entities;
using SmartDesk.Domain.Enums;

namespace SmartDesk.Application.BusinessRules;

/// <summary>
/// SLA deadlines and risk states are computed here in C#, never by an agent.
/// The Validation agent may *observe* the SLA state through the CheckSla tool, but the number is ours.
/// </summary>
public static class SlaCalculator
{
    /// <summary>Fraction of the SLA window still remaining below which a ticket is "at risk".</summary>
    public const double AtRiskThreshold = 0.25;

    /// <summary>Higher priority compresses the category's base SLA window.</summary>
    public static double PriorityMultiplier(TicketPriority priority) => priority switch
    {
        TicketPriority.Critical => 0.25,
        TicketPriority.High => 0.5,
        TicketPriority.Medium => 1.0,
        TicketPriority.Low => 2.0,
        _ => 1.0
    };

    public static DateTime CalculateDueAt(DateTime createdAtUtc, int categorySlaHours, TicketPriority priority)
    {
        var hours = Math.Max(1.0, categorySlaHours * PriorityMultiplier(priority));
        return createdAtUtc.AddHours(hours);
    }

    public static SlaState GetState(Ticket ticket, DateTime nowUtc)
    {
        // A finished ticket no longer has a live SLA clock.
        if (ticket.Status is TicketStatus.Resolved or TicketStatus.Closed or TicketStatus.Cancelled)
            return SlaState.NotApplicable;

        if (nowUtc >= ticket.SlaDueAt)
            return SlaState.Breached;

        var total = (ticket.SlaDueAt - ticket.CreatedAt).TotalHours;
        var remaining = (ticket.SlaDueAt - nowUtc).TotalHours;
        if (total <= 0)
            return SlaState.Breached;

        return remaining / total <= AtRiskThreshold ? SlaState.AtRisk : SlaState.OnTrack;
    }

    public static double HoursRemaining(Ticket ticket, DateTime nowUtc) =>
        Math.Round((ticket.SlaDueAt - nowUtc).TotalHours, 2);
}
