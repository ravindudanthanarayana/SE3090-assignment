import '../../../core/api/api_client.dart';
import '../domain/workflow.dart';

/// Read access to the existing Agentic AI subsystem.
///
/// There is deliberately no method here that talks to a language model or to an agent tool.
/// The orchestrator, the five agents and the whole tool allow-list live inside ASP.NET Core;
/// this client can start a workflow and read what it recorded, and nothing else.
class AiRepository {
  const AiRepository(this._api);

  final ApiClient _api;

  /// Finds the workflow that belongs to a ticket. Ticket creation already starts one, so this
  /// is a lookup rather than a trigger. Returns null while the workflow row is still being
  /// created, which the UI shows as "starting".
  Future<WorkflowListItem?> latestForTicket(int ticketId) async {
    final json = await _api.get<Map<String, dynamic>>(
      '/api/ai/workflows',
      query: {'ticketId': ticketId, 'page': 1, 'pageSize': 1, 'sortDir': 'desc'},
    );
    final items = (json['items'] as List<dynamic>? ?? [])
        .map((e) => WorkflowListItem.fromJson(e as Map<String, dynamic>))
        .toList();
    return items.isEmpty ? null : items.first;
  }

  /// The full auditable execution summary: plan, every agent step, and every approval.
  Future<WorkflowDetail> workflow(int id) async {
    final json = await _api.get<Map<String, dynamic>>('/api/ai/workflows/$id');
    return WorkflowDetail.fromJson(json);
  }

  /// Convenience for the ticket screen: resolve the ticket's workflow in one call.
  Future<WorkflowDetail?> workflowForTicket(int ticketId) async {
    final item = await latestForTicket(ticketId);
    if (item == null) return null;
    return workflow(item.id);
  }
}
