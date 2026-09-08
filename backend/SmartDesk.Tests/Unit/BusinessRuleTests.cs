using SmartDesk.Application.Agents.Contracts;
using SmartDesk.Application.BusinessRules;
using SmartDesk.Domain.Entities;
using SmartDesk.Domain.Enums;

namespace SmartDesk.Tests.Unit;

/// <summary>
/// The deterministic rules that sit between AI advice and real changes.
/// These run with no database, no HTTP and no language model.
/// </summary>
public class TicketStatusMachineTests
{
    [Theory]
    [InlineData(TicketStatus.New, TicketStatus.Assigned, true)]
    [InlineData(TicketStatus.New, TicketStatus.Cancelled, true)]
    [InlineData(TicketStatus.Assigned, TicketStatus.InProgress, true)]
    [InlineData(TicketStatus.InProgress, TicketStatus.Resolved, true)]
    [InlineData(TicketStatus.Resolved, TicketStatus.Closed, true)]
    public void Allows_valid_transitions(TicketStatus from, TicketStatus to, bool expected)
        => Assert.Equal(expected, TicketStatusMachine.CanTransition(from, to));

    [Theory]
    [InlineData(TicketStatus.New, TicketStatus.Resolved)]      // cannot resolve unassigned work
    [InlineData(TicketStatus.New, TicketStatus.Closed)]        // cannot skip the whole lifecycle
    [InlineData(TicketStatus.Closed, TicketStatus.InProgress)] // closed is terminal
    [InlineData(TicketStatus.Cancelled, TicketStatus.Assigned)]// cancelled is terminal
    public void Rejects_invalid_transitions(TicketStatus from, TicketStatus to)
        => Assert.False(TicketStatusMachine.CanTransition(from, to));

    [Fact]
    public void Closed_and_cancelled_are_terminal()
    {
        Assert.Empty(TicketStatusMachine.AllowedNext(TicketStatus.Closed));
        Assert.Empty(TicketStatusMachine.AllowedNext(TicketStatus.Cancelled));
    }
}

public class SlaCalculatorTests
{
    private static readonly DateTime Now = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(TicketPriority.Critical, 6)]   // 24h base * 0.25
    [InlineData(TicketPriority.High, 12)]      // 24h base * 0.5
    [InlineData(TicketPriority.Medium, 24)]    // 24h base * 1.0
    [InlineData(TicketPriority.Low, 48)]       // 24h base * 2.0
    public void Higher_priority_compresses_the_sla_window(TicketPriority priority, int expectedHours)
    {
        var due = SlaCalculator.CalculateDueAt(Now, categorySlaHours: 24, priority);
        Assert.Equal(Now.AddHours(expectedHours), due);
    }

    [Fact]
    public void Sla_window_is_never_shorter_than_one_hour()
    {
        var due = SlaCalculator.CalculateDueAt(Now, categorySlaHours: 1, TicketPriority.Critical);
        Assert.Equal(Now.AddHours(1), due);
    }

    [Fact]
    public void Past_deadline_is_breached()
    {
        var ticket = Ticket(createdHoursAgo: 10, dueHoursAgo: 1);
        Assert.Equal(SlaState.Breached, SlaCalculator.GetState(ticket, Now));
    }

    [Fact]
    public void Final_quarter_of_the_window_is_at_risk()
    {
        // 20h window, 2h remaining -> 10% left, which is inside the 25% threshold.
        var ticket = Ticket(createdHoursAgo: 18, dueHoursIn: 2);
        Assert.Equal(SlaState.AtRisk, SlaCalculator.GetState(ticket, Now));
    }

    [Fact]
    public void Plenty_of_time_remaining_is_on_track()
    {
        // 20h window, 18h remaining -> 90% left.
        var ticket = Ticket(createdHoursAgo: 2, dueHoursIn: 18);
        Assert.Equal(SlaState.OnTrack, SlaCalculator.GetState(ticket, Now));
    }

    [Theory]
    [InlineData(TicketStatus.Resolved)]
    [InlineData(TicketStatus.Closed)]
    [InlineData(TicketStatus.Cancelled)]
    public void Finished_tickets_have_no_live_sla_clock(TicketStatus status)
    {
        var ticket = Ticket(createdHoursAgo: 50, dueHoursAgo: 40);
        ticket.Status = status;
        Assert.Equal(SlaState.NotApplicable, SlaCalculator.GetState(ticket, Now));
    }

    private static Ticket Ticket(int createdHoursAgo, int? dueHoursAgo = null, int? dueHoursIn = null) => new()
    {
        Status = TicketStatus.InProgress,
        CreatedAt = Now.AddHours(-createdHoursAgo),
        SlaDueAt = dueHoursAgo is int ago ? Now.AddHours(-ago) : Now.AddHours(dueHoursIn!.Value)
    };
}

public class AssignmentScorerTests
{
    [Fact]
    public void Skill_outweighs_a_small_workload_difference()
    {
        var ranked = AssignmentScorer.Rank([
            new CandidateInput(1, "Expert, busy", SkillLevelForCategory: 5, OpenTicketCount: 3),  // 50 - 9 = 41
            new CandidateInput(2, "Novice, idle", SkillLevelForCategory: 2, OpenTicketCount: 0)   // 20 - 0 = 20
        ]);

        Assert.Equal(1, ranked[0].UserId);
        Assert.Equal(41, ranked[0].Score);
    }

    [Fact]
    public void Workload_breaks_a_tie_on_equal_skill()
    {
        var ranked = AssignmentScorer.Rank([
            new CandidateInput(1, "Loaded", 4, 5),
            new CandidateInput(2, "Free", 4, 1)
        ]);

        Assert.Equal(2, ranked[0].UserId);
    }

    [Fact]
    public void Explanation_shows_the_arithmetic_so_a_manager_can_check_it()
    {
        var ranked = AssignmentScorer.Rank([new CandidateInput(1, "A", 3, 2)]);
        Assert.Contains("Skill level 3/5", ranked[0].Explanation);
        Assert.Contains("2 open ticket(s)", ranked[0].Explanation);
    }

    [Fact]
    public void Empty_candidate_list_returns_empty_rather_than_throwing()
        => Assert.Empty(AssignmentScorer.Rank([]));
}

public class ArticleRelevanceTests
{
    private static readonly ArticleCandidate[] Articles =
    [
        new(1, "Resolving VPN connection failures", "Restart the VPN client and verify credentials.", 1, ["vpn"]),
        new(2, "Replacing a laptop battery", "Book the device in for a hardware swap.", 2, ["laptop"])
    ];

    [Fact]
    public void Ranks_the_matching_article_first()
    {
        var scored = ArticleRelevance.Score("VPN connection keeps failing", ticketCategoryId: 1, Articles);
        Assert.Equal(1, scored[0].Id);
    }

    [Fact]
    public void Articles_with_no_overlap_are_excluded_entirely()
    {
        var scored = ArticleRelevance.Score("printer toner cartridge", ticketCategoryId: 3, Articles);
        Assert.Empty(scored);
    }

    [Fact]
    public void Stop_words_do_not_create_false_matches()
    {
        var scored = ArticleRelevance.Score("the and is for with", ticketCategoryId: 3, Articles);
        Assert.Empty(scored);
    }
}
