import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../features/ai/data/ai_repository.dart';
import '../features/auth/data/auth_repository.dart';
import '../features/tickets/data/ticket_repository.dart';
import 'api/api_client.dart';
import 'storage/secure_token_store.dart';

/// The composition root.
///
/// Every dependency is created once here and read through Riverpod, so a widget never
/// constructs an HTTP client or a storage handle of its own, and a test can override any
/// layer by overriding one provider.

final secureTokenStoreProvider = Provider<SecureTokenStore>((ref) => SecureTokenStore());

final apiClientProvider = Provider<ApiClient>(
  (ref) => ApiClient(ref.watch(secureTokenStoreProvider)),
);

final authRepositoryProvider = Provider<AuthRepository>(
  (ref) => AuthRepository(ref.watch(apiClientProvider)),
);

final ticketRepositoryProvider = Provider<TicketRepository>(
  (ref) => TicketRepository(ref.watch(apiClientProvider)),
);

final aiRepositoryProvider = Provider<AiRepository>(
  (ref) => AiRepository(ref.watch(apiClientProvider)),
);
