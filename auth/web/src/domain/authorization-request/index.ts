// Public API of the authorization-request domain slice. Mirrors the AuthorizationRequest aggregate of the Identity
// bounded context (auth/src). Other layers import only from here, never from internal segments.
export { getAuthorizationRequest } from './api/authorizationRequestApi';
export { authorizationRequestQueryKeys, authorizationRequestQueryOptions } from './api/authorizationRequestQueries';
export type { AuthorizationRequest, AuthorizationRequestStatus, IdentityProvider } from './model/types';
