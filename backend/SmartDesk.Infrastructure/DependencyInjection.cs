using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartDesk.Application.Abstractions;
using SmartDesk.Application.Agents;
using SmartDesk.Application.Agents.Tools;
using SmartDesk.Application.Common;
using SmartDesk.Application.Services;
using SmartDesk.Infrastructure.Ai;
using SmartDesk.Infrastructure.Notifications;
using SmartDesk.Infrastructure.Persistence;
using SmartDesk.Infrastructure.Security;

namespace SmartDesk.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Wires the whole backend. Secrets are read from configuration, which in production is bound to
    /// environment variables (DATABASE_CONNECTION_STRING, JWT_SECRET, AI_API_KEY, NOTIFICATION_API_KEY).
    /// Nothing here contains a default secret.
    /// </summary>
    public static IServiceCollection AddSmartDesk(this IServiceCollection services, IConfiguration config)
    {
        services.AddSmartDeskDatabase(config);
        services.AddSmartDeskSecurity(config);
        services.AddSmartDeskAi(config);
        services.AddSmartDeskNotifications(config);
        services.AddSmartDeskServices();
        return services;
    }

    public static IServiceCollection AddSmartDeskDatabase(this IServiceCollection services, IConfiguration config)
    {
        var connectionString =
            config["DATABASE_CONNECTION_STRING"]
            ?? config.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "No database connection string. Set DATABASE_CONNECTION_STRING or ConnectionStrings:Default.");

        // Accepts both the postgresql:// URI that Neon gives you and Npgsql's key-value form.
        connectionString = ConnectionStringNormalizer.Normalize(connectionString);

        services.AddDbContext<AppDbContext>(o => o
            .UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure(3)));

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IAuditService, AuditService>();
        return services;
    }

    public static IServiceCollection AddSmartDeskSecurity(this IServiceCollection services, IConfiguration config)
    {
        var jwt = new JwtOptions();
        config.GetSection(JwtOptions.SectionName).Bind(jwt);

        // The environment variable always wins over appsettings, so a committed file can never
        // accidentally supply the production signing key.
        var envSecret = config["JWT_SECRET"];
        if (!string.IsNullOrWhiteSpace(envSecret)) jwt.Secret = envSecret;

        if (string.IsNullOrWhiteSpace(jwt.Secret) || jwt.Secret.Length < 32)
            throw new InvalidOperationException("JWT_SECRET must be set and at least 32 characters long.");

        services.AddSingleton(jwt);
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<IClock, SystemClock>();
        return services;
    }

    public static IServiceCollection AddSmartDeskAi(this IServiceCollection services, IConfiguration config)
    {
        var llm = new LlmOptions();
        config.GetSection(LlmOptions.SectionName).Bind(llm);

        var envKey = config["AI_API_KEY"];
        if (!string.IsNullOrWhiteSpace(envKey)) llm.ApiKey = envKey;

        // Without a key we fall back to the scripted client rather than failing to start.
        // The system stays fully demonstrable; the difference is visible in /api/ai/workflows.
        if (string.IsNullOrWhiteSpace(llm.ApiKey)) llm.Provider = "scripted";

        services.AddSingleton(llm);
        services.Configure<AgentOptions>(config.GetSection(AgentOptions.SectionName));

        if (llm.Provider.Equals("gemini", StringComparison.OrdinalIgnoreCase))
        {
            services.AddHttpClient<ILlmClient, GeminiLlmClient>(c => c.Timeout = TimeSpan.FromSeconds(60));
        }
        else
        {
            services.AddSingleton<ILlmClient>(new ScriptedLlmClient());
        }

        // --- The tool allow-list. A tool is reachable only if it is registered here.
        services.AddScoped<IAgentTool, GetTicketTool>();
        services.AddScoped<IAgentTool, SearchKnowledgeBaseTool>();
        services.AddScoped<IAgentTool, GetSupportAgentsTool>();
        services.AddScoped<IAgentTool, GetAgentWorkloadTool>();
        services.AddScoped<IAgentTool, ScoreAssignmentCandidatesTool>();
        services.AddScoped<IAgentTool, CheckSlaTool>();
        services.AddScoped<IAgentTool, RequestHumanApprovalTool>();
        services.AddScoped<IAgentTool, ExecuteApprovedActionTool>();
        services.AddScoped<ToolRegistry>();

        // --- The agents. Four specialists plus the group-owned planner.
        services.AddScoped<PlannerAgent>();
        services.AddScoped<IWorkflowAgent, TriageAgent>();
        services.AddScoped<IWorkflowAgent, SolutionAgent>();
        services.AddScoped<IWorkflowAgent, AssignmentAgent>();
        services.AddScoped<IWorkflowAgent, ValidationAgent>();
        services.AddScoped<WorkflowOrchestrator>();

        return services;
    }

    public static IServiceCollection AddSmartDeskNotifications(this IServiceCollection services, IConfiguration config)
    {
        var options = new NotificationOptions();
        config.GetSection(NotificationOptions.SectionName).Bind(options);

        var envKey = config["NOTIFICATION_API_KEY"];
        if (!string.IsNullOrWhiteSpace(envKey)) options.ApiKey = envKey;
        if (string.IsNullOrWhiteSpace(options.ApiKey)) options.Provider = "null";

        var redirect = config["NOTIFICATION_REDIRECT_TO"];
        if (!string.IsNullOrWhiteSpace(redirect)) options.RedirectAllTo = redirect.Trim();

        services.AddSingleton(options);

        if (options.Provider.Equals("resend", StringComparison.OrdinalIgnoreCase))
            services.AddHttpClient<IEmailProvider, ResendEmailProvider>(c =>
                c.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds));
        else
            services.AddSingleton<IEmailProvider, NullEmailProvider>();

        services.AddScoped<INotificationService, NotificationService>();
        return services;
    }

    public static IServiceCollection AddSmartDeskServices(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<TicketService>();
        services.AddScoped<AssignmentService>();
        services.AddScoped<KnowledgeService>();
        services.AddScoped<ReportingService>();
        services.AddScoped<ApprovalService>();
        services.AddScoped<WorkflowService>();
        services.AddScoped<AdminService>();
        services.AddScoped<IApprovalActionExecutor, ApprovalActionExecutor>();
        return services;
    }
}
