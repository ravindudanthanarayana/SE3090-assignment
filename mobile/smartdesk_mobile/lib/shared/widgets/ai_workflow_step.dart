import 'package:flutter/material.dart';

import '../../core/theme/app_theme.dart';
import '../../features/ai/state/workflow_providers.dart';
import '../../features/tickets/domain/enums.dart';

/// One line in the AI Support checklist. The icon reflects what the backend recorded for
/// that agent - nothing here is simulated or advanced by a client-side timer.
class AiWorkflowStepTile extends StatelessWidget {
  const AiWorkflowStepTile({super.key, required this.step, this.showAgentName = true});

  final WorkflowStepView step;

  /// The raw backend agent name is shown under the friendly label, so the checklist can be
  /// traced straight back to `AgentNames` in the C# code during a demonstration.
  final bool showAgentName;

  @override
  Widget build(BuildContext context) {
    final c = context.colors;

    final (icon, color, child) = switch (step.status) {
      AgentStepStatus.succeeded => (Icons.check_circle, c.ok, null),
      AgentStepStatus.running => (
          null,
          c.accent,
          SizedBox(
            height: 15,
            width: 15,
            child: CircularProgressIndicator(strokeWidth: 2, color: c.accent),
          )
        ),
      AgentStepStatus.failed => (Icons.error, c.danger, null),
      AgentStepStatus.skipped => (Icons.remove_circle_outline, c.fgSubtle, null),
      _ => (Icons.radio_button_unchecked, c.fgSubtle, null),
    };

    final isPending = step.status == AgentStepStatus.pending;

    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 7),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 20,
            height: 20,
            child: Center(child: child ?? Icon(icon, size: 18, color: color)),
          ),
          const SizedBox(width: 10),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  step.label,
                  style: TextStyle(
                    fontSize: 14.5,
                    fontWeight: isPending ? FontWeight.w500 : FontWeight.w600,
                    color: isPending ? c.fgSubtle : c.fg,
                  ),
                ),
                if (showAgentName)
                  Text(
                    step.agentName,
                    style: TextStyle(fontSize: 11.5, color: c.fgSubtle, fontFamily: 'monospace'),
                  ),
                if (step.errorMessage != null)
                  Padding(
                    padding: const EdgeInsets.only(top: 2),
                    child: Text(
                      step.errorMessage!,
                      style: TextStyle(fontSize: 12, color: c.danger),
                    ),
                  ),
              ],
            ),
          ),
          if (step.durationMs != null && step.durationMs! > 0)
            Text(
              '${(step.durationMs! / 1000).toStringAsFixed(1)}s',
              style: TextStyle(fontSize: 11.5, color: c.fgSubtle),
            ),
        ],
      ),
    );
  }
}
