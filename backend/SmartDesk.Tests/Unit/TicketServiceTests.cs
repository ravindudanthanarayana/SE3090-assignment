using SmartDesk.Application.Common;
using SmartDesk.Application.Dtos;
using SmartDesk.Application.Services;
using SmartDesk.Domain.Common;
using SmartDesk.Domain.Enums;
using SmartDesk.Infrastructure.Persistence;
using SmartDesk.Tests.Support;

namespace SmartDesk.Tests.Unit;

/// <summary>
/// Service-layer behaviour for Component A, including the authorization rules that a
/// [Authorize(Roles=...)] attribute cannot express - such as "an employee sees only their own tickets".
/// </summary>
public class TicketServiceTests : IDisposable
{
    private readonly FakeClock _clock = FakeClock.Default;
    private readonly AppDbContext _db;
    private readonly RecordingAudit _audit = new();
    private readonly RecordingNotifications _notifications = new();

    public TicketServiceTests() => _db = TestDb.Create(_clock.UtcNow);

    public void Dispose() => _db.Dispose();

    private TicketService ServiceAs(int userId, string role)
        => new(_db, new FakeCurrentUser(userId, role), _audit, _notifications, _clock);

    // ---- Creation -----------------------------------------------------------------------

    [Fact]
    public async Task Creating_a_ticket_computes_the_sla_deadline_server_side()
    {
        var service = ServiceAs(TestDb.EmployeeId, RoleNames.Employee);

        var ticket = await service.CreateAsync(new CreateTicketRequest
        {
            Title = "VPN will not connect",
            Description = "The VPN client fails after a password change.",
            CategoryId = TestDb.NetworkCategoryId,
            Priority = TicketPriority.High
        });

        // Network category is 8h; High halves it to 4h.
        Assert.Equal(_clock.UtcNow.AddHours(4), ticket.SlaDueAt);
        Assert.Equal(TicketStatus.New, ticket.Status);
        Assert.StartsWith("TKT-", ticket.TicketNumber);
    }

    [Fact]
    public async Task Creating_a_ticket_writes_a_history_entry_and_an_audit_row()
    {
        var service = ServiceAs(TestDb.EmployeeId, RoleNames.Employee);
        var ticket = await service.CreateAsync(NewTicketRequest());

        Assert.Contains(_db.TicketHistory, h => h.TicketId == ticket.Id && h.Field == "Status");
        Assert.Contains(_audit.Entries, e => e.Action == "TicketCreated");
    }

    [Fact]
    public async Task An_inactive_category_is_rejected()
    {
        var category = _db.TicketCategories.First(c => c.Id == TestDb.HardwareCategoryId);
        category.IsActive = false;
        await _db.SaveChangesAsync();

        var service = ServiceAs(TestDb.EmployeeId, RoleNames.Employee);
        var request = NewTicketRequest();
        request.CategoryId = TestDb.HardwareCategoryId;

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(request));
    }

    // ---- Authorization ------------------------------------------------------------------

    [Fact]
    public async Task An_employee_cannot_read_another_employees_ticket()
    {
        var ticket = TestDb.AddTicket(_db, _clock.UtcNow, _clock.UtcNow.AddHours(8), createdBy: TestDb.OtherEmployeeId);
        var service = ServiceAs(TestDb.EmployeeId, RoleNames.Employee);

        await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync(ticket.Id));
    }

    [Fact]
    public async Task An_employees_list_is_scoped_to_their_own_tickets()
    {
        TestDb.AddTicket(_db, _clock.UtcNow, _clock.UtcNow.AddHours(8), createdBy: TestDb.EmployeeId);
        TestDb.AddTicket(_db, _clock.UtcNow, _clock.UtcNow.AddHours(8), createdBy: TestDb.OtherEmployeeId);
        TestDb.AddTicket(_db, _clock.UtcNow, _clock.UtcNow.AddHours(8), createdBy: TestDb.OtherEmployeeId);

        var mine = await ServiceAs(TestDb.EmployeeId, RoleNames.Employee).QueryAsync(new TicketQuery());
        var all = await ServiceAs(TestDb.ManagerId, RoleNames.SupportManager).QueryAsync(new TicketQuery());

        Assert.Equal(1, mine.TotalCount);
        Assert.Equal(3, all.TotalCount);
    }

    [Fact]
    public async Task A_support_agent_cannot_change_the_status_of_someone_elses_ticket()
    {
        var ticket = TestDb.AddTicket(_db, _clock.UtcNow, _clock.UtcNow.AddHours(8),
            status: TicketStatus.Assigned, assignedTo: TestDb.AgentBId);

        var service = ServiceAs(TestDb.AgentAId, RoleNames.SupportAgent);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.ChangeStatusAsync(ticket.Id, new ChangeStatusRequest { Status = TicketStatus.InProgress }));
    }

    [Fact]
    public async Task Internal_comments_are_hidden_from_the_requester()
    {
        var ticket = TestDb.AddTicket(_db, _clock.UtcNow, _clock.UtcNow.AddHours(8),
            status: TicketStatus.Assigned, assignedTo: TestDb.AgentAId);

        await ServiceAs(TestDb.AgentAId, RoleNames.SupportAgent)
            .AddCommentAsync(ticket.Id, new CreateCommentRequest { Body = "Internal note", IsInternal = true });
        await ServiceAs(TestDb.AgentAId, RoleNames.SupportAgent)
            .AddCommentAsync(ticket.Id, new CreateCommentRequest { Body = "Visible update", IsInternal = false });

        var asEmployee = await ServiceAs(TestDb.EmployeeId, RoleNames.Employee).GetCommentsAsync(ticket.Id);
        var asAgent = await ServiceAs(TestDb.AgentAId, RoleNames.SupportAgent).GetCommentsAsync(ticket.Id);

        Assert.Single(asEmployee);
        Assert.Equal(2, asAgent.Count);
    }

    [Fact]
    public async Task An_employee_cannot_create_an_internal_comment()
    {
        var ticket = TestDb.AddTicket(_db, _clock.UtcNow, _clock.UtcNow.AddHours(8), createdBy: TestDb.EmployeeId);

        var comment = await ServiceAs(TestDb.EmployeeId, RoleNames.Employee)
            .AddCommentAsync(ticket.Id, new CreateCommentRequest { Body = "Let me in", IsInternal = true });

        Assert.False(comment.IsInternal);
    }

    // ---- Status workflow ------------------------------------------------------------------

    [Fact]
    public async Task An_invalid_status_transition_is_rejected_with_a_conflict()
    {
        var ticket = TestDb.AddTicket(_db, _clock.UtcNow, _clock.UtcNow.AddHours(8), status: TicketStatus.New);
        var service = ServiceAs(TestDb.ManagerId, RoleNames.SupportManager);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.ChangeStatusAsync(ticket.Id, new ChangeStatusRequest { Status = TicketStatus.Closed }));
    }

    [Fact]
    public async Task Resolving_without_a_resolution_is_rejected()
    {
        var ticket = TestDb.AddTicket(_db, _clock.UtcNow, _clock.UtcNow.AddHours(8), status: TicketStatus.InProgress);
        var service = ServiceAs(TestDb.ManagerId, RoleNames.SupportManager);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.ChangeStatusAsync(ticket.Id, new ChangeStatusRequest { Status = TicketStatus.Resolved }));
    }

    [Fact]
    public async Task Escalation_cannot_be_smuggled_in_through_the_status_endpoint()
    {
        var ticket = TestDb.AddTicket(_db, _clock.UtcNow, _clock.UtcNow.AddHours(8), status: TicketStatus.InProgress);
        var service = ServiceAs(TestDb.ManagerId, RoleNames.SupportManager);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.ChangeStatusAsync(ticket.Id, new ChangeStatusRequest { Status = TicketStatus.Escalated }));
    }

    [Fact]
    public async Task Resolving_records_the_resolution_and_notifies_the_requester()
    {
        var ticket = TestDb.AddTicket(_db, _clock.UtcNow, _clock.UtcNow.AddHours(8),
            status: TicketStatus.InProgress, assignedTo: TestDb.AgentAId);

        var result = await ServiceAs(TestDb.AgentAId, RoleNames.SupportAgent).ChangeStatusAsync(
            ticket.Id, new ChangeStatusRequest { Status = TicketStatus.Resolved, Resolution = "Reset the VPN profile." });

        Assert.Equal(TicketStatus.Resolved, result.Status);
        Assert.Equal("Reset the VPN profile.", result.Resolution);
        Assert.NotNull(result.ResolvedAt);
        Assert.Contains(_notifications.Sent, n => n.UserId == TestDb.EmployeeId);
    }

    // ---- Query features ---------------------------------------------------------------------

    [Fact]
    public async Task Search_matches_ticket_number_title_and_description()
    {
        TestDb.AddTicket(_db, _clock.UtcNow, _clock.UtcNow.AddHours(8), title: "Printer jam", description: "Paper stuck");
        TestDb.AddTicket(_db, _clock.UtcNow, _clock.UtcNow.AddHours(8), title: "VPN down", description: "Cannot connect");

        var service = ServiceAs(TestDb.ManagerId, RoleNames.SupportManager);

        Assert.Equal(1, (await service.QueryAsync(new TicketQuery { Search = "printer" })).TotalCount);
        Assert.Equal(1, (await service.QueryAsync(new TicketQuery { Search = "cannot connect" })).TotalCount);
        Assert.Equal(1, (await service.QueryAsync(new TicketQuery { Search = "TKT-000001" })).TotalCount);
    }

    [Fact]
    public async Task Filtering_and_sorting_and_paging_work_together()
    {
        for (var i = 0; i < 5; i++)
            TestDb.AddTicket(_db, _clock.UtcNow, _clock.UtcNow.AddHours(8),
                priority: i % 2 == 0 ? TicketPriority.High : TicketPriority.Low);

        var service = ServiceAs(TestDb.ManagerId, RoleNames.SupportManager);
        var page = await service.QueryAsync(new TicketQuery
        {
            Priority = TicketPriority.High, SortBy = "priority", SortDir = "desc", Page = 1, PageSize = 2
        });

        Assert.Equal(3, page.TotalCount);
        Assert.Equal(2, page.Items.Count);
        Assert.Equal(2, page.TotalPages);
        Assert.All(page.Items, i => Assert.Equal(TicketPriority.High, i.Priority));
    }

    [Fact]
    public async Task Page_size_is_capped_so_a_client_cannot_request_the_whole_table()
    {
        var query = new TicketQuery { PageSize = 100_000 };
        Assert.Equal(100, query.PageSize);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Sla_filters_return_the_right_buckets()
    {
        // Breached: due an hour ago.
        TestDb.AddTicket(_db, _clock.UtcNow.AddHours(-10), _clock.UtcNow.AddHours(-1), status: TicketStatus.InProgress);
        // At risk: 20h window with 2h left.
        TestDb.AddTicket(_db, _clock.UtcNow.AddHours(-18), _clock.UtcNow.AddHours(2), status: TicketStatus.InProgress);
        // On track: 20h window with 18h left.
        TestDb.AddTicket(_db, _clock.UtcNow.AddHours(-2), _clock.UtcNow.AddHours(18), status: TicketStatus.InProgress);

        var service = ServiceAs(TestDb.ManagerId, RoleNames.SupportManager);

        Assert.Equal(1, (await service.QueryAsync(new TicketQuery { SlaState = SlaState.Breached })).TotalCount);
        Assert.Equal(1, (await service.QueryAsync(new TicketQuery { SlaState = SlaState.AtRisk })).TotalCount);
        Assert.Equal(1, (await service.QueryAsync(new TicketQuery { SlaState = SlaState.OnTrack })).TotalCount);
    }

    // ---- Deletion ------------------------------------------------------------------------------

    [Fact]
    public async Task A_ticket_that_has_been_worked_on_cannot_be_deleted()
    {
        var ticket = TestDb.AddTicket(_db, _clock.UtcNow, _clock.UtcNow.AddHours(8), status: TicketStatus.InProgress);
        var service = ServiceAs(TestDb.ManagerId, RoleNames.Admin);

        await Assert.ThrowsAsync<ConflictException>(() => service.DeleteAsync(ticket.Id));
    }

    [Fact]
    public async Task A_new_ticket_can_be_deleted()
    {
        var ticket = TestDb.AddTicket(_db, _clock.UtcNow, _clock.UtcNow.AddHours(8), status: TicketStatus.New);
        await ServiceAs(TestDb.ManagerId, RoleNames.Admin).DeleteAsync(ticket.Id);

        Assert.Empty(_db.Tickets.Where(t => t.Id == ticket.Id));
    }

    private static CreateTicketRequest NewTicketRequest() => new()
    {
        Title = "VPN will not connect",
        Description = "The VPN client fails after a password change.",
        CategoryId = TestDb.NetworkCategoryId,
        Priority = TicketPriority.Medium
    };
}
