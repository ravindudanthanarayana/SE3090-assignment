using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using SmartDesk.Application.Dtos;
using SmartDesk.Domain.Enums;

namespace SmartDesk.Tests.Integration;

/// <summary>
/// Controller and API integration tests over real HTTP against real PostgreSQL, plus the
/// complete end-to-end workflow required by spec section 12.
/// </summary>
[Collection("postgres")]
public class ApiIntegrationTests(PostgresFixture fixture) : IAsyncLifetime
{
    private ApiFactory _factory = null!;
    private HttpClient _client = null!;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public Task InitializeAsync()
    {
        _factory = new ApiFactory(fixture.ConnectionString);
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    // ---- Infrastructure endpoints -----------------------------------------------------------

    [Fact]
    public async Task Health_endpoint_reports_the_database_as_reachable()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Swagger_document_is_served_and_lists_the_api()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = doc.RootElement.GetProperty("paths");

        Assert.True(paths.TryGetProperty("/api/tickets", out _));
        Assert.True(paths.TryGetProperty("/api/ai/workflows", out _));
        Assert.True(paths.TryGetProperty("/api/ai/approvals/{id}/decision", out _));
    }

    // ---- Authentication ------------------------------------------------------------------------

    [Fact]
    public async Task An_unauthenticated_request_to_a_protected_endpoint_is_401()
    {
        var response = await _client.GetAsync("/api/tickets");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_with_the_wrong_password_is_rejected_without_revealing_which_part_was_wrong()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = "manager@smartdesk.local", Password = "definitely-wrong" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        var detail = problem.GetProperty("detail").GetString()!;
        Assert.Equal("Invalid email address or password.", detail);
    }

    [Fact]
    public async Task Registering_creates_an_employee_and_never_a_privileged_role()
    {
        var email = $"newstarter-{Guid.NewGuid():N}@smartdesk.local";
        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email, Password = "Password123!", FullName = "New Starter", Department = "Sales"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(Json);
        Assert.Equal("Employee", auth!.User.Role);
        Assert.False(string.IsNullOrWhiteSpace(auth.Token));
    }

    [Fact]
    public async Task Registering_a_duplicate_email_is_a_conflict()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = "admin@smartdesk.local", Password = "Password123!", FullName = "Impostor"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Invalid_input_is_rejected_with_a_bad_request()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "not-an-email", password = "short", fullName = ""
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---- Authorization --------------------------------------------------------------------------

    [Fact]
    public async Task An_employee_cannot_reach_the_admin_user_list()
    {
        await AuthenticateAsync("employee1@smartdesk.local");
        var response = await _client.GetAsync("/api/users");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task A_support_agent_cannot_assign_a_ticket()
    {
        await AuthenticateAsync("agent1@smartdesk.local");
        var response = await _client.PostAsJsonAsync("/api/tickets/1/assign",
            new AssignTicketRequest { AssignedToUserId = 3, Reason = "Mine now" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task An_admin_can_reach_the_admin_user_list()
    {
        await AuthenticateAsync("admin@smartdesk.local");
        var response = await _client.GetAsync("/api/users");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ---- Query features over HTTP -------------------------------------------------------------------

    [Fact]
    public async Task Search_filter_sort_and_pagination_all_work_over_the_wire()
    {
        await AuthenticateAsync("manager@smartdesk.local");

        var page = await _client.GetFromJsonAsync<PagedResultDto<TicketListItemDto>>(
            "/api/tickets?search=vpn&sortBy=priority&sortDir=desc&page=1&pageSize=2", Json);

        Assert.NotNull(page);
        Assert.True(page!.TotalCount >= 1);
        Assert.True(page.Items.Count <= 2);
        Assert.Equal(1, page.Page);

        // Every returned row genuinely matches the search term.
        Assert.All(page.Items, i => Assert.Contains("vpn",
            $"{i.TicketNumber} {i.Title}".ToLowerInvariant()));
    }

    [Fact]
    public async Task An_employees_ticket_list_is_scoped_to_their_own_tickets_over_http()
    {
        await AuthenticateAsync("employee1@smartdesk.local");
        var mine = await _client.GetFromJsonAsync<PagedResultDto<TicketListItemDto>>("/api/tickets?pageSize=100", Json);

        await AuthenticateAsync("manager@smartdesk.local");
        var all = await _client.GetFromJsonAsync<PagedResultDto<TicketListItemDto>>("/api/tickets?pageSize=100", Json);

        Assert.True(mine!.TotalCount < all!.TotalCount);
    }

    [Fact]
    public async Task An_unknown_ticket_id_is_a_404()
    {
        await AuthenticateAsync("manager@smartdesk.local");
        var response = await _client.GetAsync("/api/tickets/999999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task An_invalid_status_transition_over_http_is_a_409()
    {
        await AuthenticateAsync("manager@smartdesk.local");

        var created = await CreateTicketAsync();
        var response = await _client.PostAsJsonAsync($"/api/tickets/{created.Id}/status",
            new ChangeStatusRequest { Status = TicketStatus.Closed });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // ---- The complete end-to-end workflow (spec section 12) ---------------------------------------------

    [Fact]
    public async Task End_to_end_employee_raises_a_ticket_the_agents_run_and_a_manager_approves()
    {
        // --- 1. An employee registers and signs in.
        var email = $"e2e-{Guid.NewGuid():N}@smartdesk.local";
        var register = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email, Password = "Password123!", FullName = "E2E Employee", Department = "Engineering"
        });
        register.EnsureSuccessStatusCode();
        var employee = (await register.Content.ReadFromJsonAsync<AuthResponse>(Json))!;
        SetToken(employee.Token);

        // --- 2. They raise a ticket. This is what starts the agent workflow.
        var ticket = await CreateTicketAsync(
            "VPN rejects my login after a password change",
            "Since changing my password the VPN client refuses my credentials and I cannot reach any internal system. I am completely blocked.");

        Assert.Equal(TicketStatus.New, ticket.Status);
        Assert.Null(ticket.AssignedToUserId);

        // --- 3. The workflow runs in the background and pauses for a human.
        await AuthenticateAsync("manager@smartdesk.local");
        var workflow = await WaitForWorkflowAsync(ticket.Id, WorkflowStatus.AwaitingApproval);

        // The five distinct agents all participated, in order.
        Assert.Equal(
            ["PlannerAgent", "TriageAgent", "SolutionAgent", "AssignmentAgent", "ValidationAgent"],
            workflow.Steps.OrderBy(s => s.StepOrder).Select(s => s.AgentName));
        Assert.All(workflow.Steps, s => Assert.Equal(AgentStepStatus.Succeeded, s.Status));

        // Tools were actually called, and each is one the calling agent was allowed to use.
        var allTools = workflow.Steps.SelectMany(s => s.ToolCalls).Select(t => t.ToolName).ToList();
        Assert.Contains("GetTicket", allTools);
        Assert.Contains("SearchKnowledgeBase", allTools);
        Assert.Contains("ScoreAssignmentCandidates", allTools);
        Assert.Contains("CheckSla", allTools);
        Assert.DoesNotContain("ExecuteApprovedAction", allTools);

        // --- 4. Nothing high-impact has happened yet.
        var pending = Assert.Single(workflow.Approvals, a => a.Status == ApprovalStatus.Pending);

        var beforeApproval = await _client.GetFromJsonAsync<TicketDetailDto>($"/api/tickets/{ticket.Id}", Json);
        Assert.Null(beforeApproval!.AssignedToUserId);

        // --- 5. The employee who raised it cannot approve it.
        SetToken(employee.Token);
        var forbidden = await _client.PostAsJsonAsync($"/api/ai/approvals/{pending.Id}/decision",
            new ApprovalDecisionRequest { Decision = ApprovalStatus.Approved });
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        // --- 6. A manager approves, and only then does PostgreSQL change.
        await AuthenticateAsync("manager@smartdesk.local");
        var decision = await _client.PostAsJsonAsync($"/api/ai/approvals/{pending.Id}/decision",
            new ApprovalDecisionRequest { Decision = ApprovalStatus.Approved, Note = "Agreed." });
        decision.EnsureSuccessStatusCode();

        var afterApproval = await _client.GetFromJsonAsync<TicketDetailDto>($"/api/tickets/{ticket.Id}", Json);
        Assert.NotNull(afterApproval!.AssignedToUserId);
        Assert.Equal(TicketStatus.Assigned, afterApproval.Status);

        // --- 7. The same approval cannot be decided twice.
        var again = await _client.PostAsJsonAsync($"/api/ai/approvals/{pending.Id}/decision",
            new ApprovalDecisionRequest { Decision = ApprovalStatus.Rejected });
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);

        // --- 8. The history and audit trail tell the whole story.
        var history = await _client.GetFromJsonAsync<List<TicketHistoryDto>>($"/api/tickets/{ticket.Id}/history", Json);
        Assert.Contains(history!, h => h.Field == "AssignedTo");
        Assert.Contains(history!, h => h.ChangedByName == "System / AI");

        var audit = await _client.GetFromJsonAsync<PagedResultDto<AuditLogDto>>(
            $"/api/audit-logs?entityType=Ticket&entityId={ticket.Id}&pageSize=50", Json);
        Assert.Contains(audit!.Items, a => a.Action == "TicketCreated");
        Assert.Contains(audit.Items, a => a.Action.Contains("Executed"));

        // --- 9. The initiating employee sees the updated state on their own ticket.
        SetToken(employee.Token);
        var asEmployee = await _client.GetFromJsonAsync<TicketDetailDto>($"/api/tickets/{ticket.Id}", Json);
        Assert.Equal(TicketStatus.Assigned, asEmployee!.Status);
        Assert.NotNull(asEmployee.AssignedToName);
    }

    [Fact]
    public async Task Rejecting_an_ai_recommendation_executes_nothing()
    {
        await AuthenticateAsync("employee2@smartdesk.local");
        var ticket = await CreateTicketAsync();

        await AuthenticateAsync("manager@smartdesk.local");
        var workflow = await WaitForWorkflowAsync(ticket.Id, WorkflowStatus.AwaitingApproval);
        var pending = workflow.Approvals.First(a => a.Status == ApprovalStatus.Pending);

        var decision = await _client.PostAsJsonAsync($"/api/ai/approvals/{pending.Id}/decision",
            new ApprovalDecisionRequest { Decision = ApprovalStatus.Rejected, Note = "Not appropriate." });
        decision.EnsureSuccessStatusCode();

        var after = await _client.GetFromJsonAsync<TicketDetailDto>($"/api/tickets/{ticket.Id}", Json);
        Assert.Null(after!.AssignedToUserId);
        Assert.Equal(TicketStatus.New, after.Status);

        var finalWorkflow = await _client.GetFromJsonAsync<WorkflowDetailDto>($"/api/ai/workflows/{workflow.Id}", Json);
        Assert.Equal(WorkflowStatus.Rejected, finalWorkflow!.Status);
    }

    [Fact]
    public async Task The_tool_allow_list_shows_that_no_agent_may_execute_an_approved_action()
    {
        await AuthenticateAsync("manager@smartdesk.local");
        var tools = await _client.GetFromJsonAsync<List<AgentToolInfoDto>>("/api/ai/tools", Json);

        var execute = Assert.Single(tools!, t => t.Name == "ExecuteApprovedAction");
        Assert.Empty(execute.AllowedForAgents);

        var getTicket = Assert.Single(tools!, t => t.Name == "GetTicket");
        Assert.Contains("TriageAgent", getTicket.AllowedForAgents);
    }

    // ---- Helpers -------------------------------------------------------------------------------------

    private async Task AuthenticateAsync(string email)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = email, Password = "Password123!" });
        response.EnsureSuccessStatusCode();

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(Json);
        SetToken(auth!.Token);
    }

    private void SetToken(string token) =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    private async Task<TicketDetailDto> CreateTicketAsync(
        string title = "VPN will not connect",
        string description = "The VPN client fails to connect after a password change and I cannot work.")
    {
        var response = await _client.PostAsJsonAsync("/api/tickets", new CreateTicketRequest
        {
            Title = title, Description = description, CategoryId = 1, Priority = TicketPriority.Medium
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TicketDetailDto>(Json))!;
    }

    /// <summary>Polls until the background workflow reaches the expected state, or fails with what it saw.</summary>
    private async Task<WorkflowDetailDto> WaitForWorkflowAsync(int ticketId, WorkflowStatus expected)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);
        WorkflowDetailDto? last = null;

        while (DateTime.UtcNow < deadline)
        {
            var list = await _client.GetFromJsonAsync<PagedResultDto<WorkflowListItemDto>>(
                $"/api/ai/workflows?ticketId={ticketId}", Json);

            if (list?.Items.Count > 0)
            {
                last = await _client.GetFromJsonAsync<WorkflowDetailDto>(
                    $"/api/ai/workflows/{list.Items[0].Id}", Json);

                if (last!.Status == expected) return last;
                if (last.Status is WorkflowStatus.Failed)
                    Assert.Fail($"Workflow failed instead of reaching {expected}: {last.ErrorMessage}");
            }

            await Task.Delay(300);
        }

        Assert.Fail($"Workflow did not reach {expected} within 30s. Last status: {last?.Status.ToString() ?? "none"}.");
        throw new InvalidOperationException("unreachable");
    }

    /// <summary>Deserialisation target for the API's pagination envelope (TotalPages is computed server-side).</summary>
    private sealed record PagedResultDto<T>(List<T> Items, int Page, int PageSize, int TotalCount, int TotalPages);
}
