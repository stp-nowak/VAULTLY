// Public API of the identity domain slice. Mirrors the Identity bounded context (auth/), reached via the web API.
// Other layers import only from here, never from internal segments.
export { buildLoginUrl } from './api/identityApi';
export { identityQueryKeys, sessionsQueryOptions } from './api/identityQueries';
export { useAuth } from './model/authContext';
export type { AuthContextValue, AuthState, AuthStatus } from './model/authContext';
export { AuthProvider } from './model/AuthProvider';
export type { AuthSession, AuthUser, CurrentUser } from './model/types';
export { SessionSummary } from './ui/SessionSummary';
