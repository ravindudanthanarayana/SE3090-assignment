import 'package:flutter_test/flutter_test.dart';
import 'package:smartdesk_mobile/features/tickets/domain/ticket.dart';
import 'package:smartdesk_mobile/features/tickets/ui/tabs/history_tab.dart';

/// The history rows the backend writes are field-level diffs, not sentences. These pin the
/// wording the timeline builds from them - including the two cases where the generic
/// `field set to value` sentence would read badly or claim something the row does not say.
TicketHistoryEntry _entry({
  required String field,
  String? oldValue,
  String? newValue,
  String? changedByName,
  String? note,
}) =>
    TicketHistoryEntry(
      id: 1,
      field: field,
      oldValue: oldValue,
      newValue: newValue,
      changedByName: changedByName,
      note: note,
      createdAt: DateTime.utc(2026, 9, 9),
    );

void main() {
  test('the first Status row is the creation of the ticket', () {
    expect(
      TimelineEntryHeadline.of(_entry(field: 'Status', newValue: 'New')),
      'Ticket created',
    );
  });

  test('a later status change reads as a transition', () {
    expect(
      TimelineEntryHeadline.of(
        _entry(field: 'Status', oldValue: 'Assigned', newValue: 'InProgress'),
      ),
      'Status changed from Assigned to InProgress',
    );
  });

  test('AssignedTo does not print the raw user id it stores', () {
    // The row's newValue is "3". "Assigned to set to 3" is what a generic sentence would
    // produce, and it is both ugly and meaningless to an employee.
    final headline = TimelineEntryHeadline.of(_entry(field: 'AssignedTo', newValue: '3'));

    expect(headline, 'Assigned to a support agent');
    expect(headline, isNot(contains('3')));
  });

  test('clearing the assignee reads as unassigned', () {
    expect(
      TimelineEntryHeadline.of(_entry(field: 'AssignedTo', oldValue: '3')),
      'Unassigned',
    );
  });

  test('a priority change names both ends', () {
    expect(
      TimelineEntryHeadline.of(
        _entry(field: 'Priority', oldValue: 'Medium', newValue: 'High'),
      ),
      'Priority changed from Medium to High',
    );
  });

  test('a PascalCase field name is split into words', () {
    expect(
      TimelineEntryHeadline.of(_entry(field: 'EscalationReason', newValue: 'SLA at risk')),
      'Escalation reason set to SLA at risk',
    );
  });
}
