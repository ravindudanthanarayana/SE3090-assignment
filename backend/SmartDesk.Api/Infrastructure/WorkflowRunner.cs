using SmartDesk.Application.Agents;

namespace SmartDesk.Api.Infrastructure;

/// <summary>
/// Runs an agent workflow outside the originating HTTP request.
///
/// Ticket creation must never block on, or fail because of, the AI subsystem. The workflow row is
/// created synchronously so the caller gets an id immediately; execution then continues in the
/// background with its own dependency-injection scope, because the request's scope (and its
/// DbContext) is disposed as soon as the response is written.
/// </summary>
public sealed class WorkflowRunner(IServiceScopeFactory scopeFactory, ILogger<WorkflowRunner> logger)
{
    public void StartInBackground(int workflowId)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var orchestrator = scope.ServiceProvider.GetRequiredService<WorkflowOrchestrator>();

                // RunAsync never throws: it records a safe failure on the workflow row instead.
                await orchestrator.RunAsync(workflowId, CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Background workflow {WorkflowId} crashed", workflowId);
            }
        });
    }
}
