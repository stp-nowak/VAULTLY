// Public API of the authorization-request domain slice. Mirrors the AuthorizationRequest aggregate of the Identity
// bounded context (auth/src). Other layers import only from here, never from internal segments.
export { authorizationRequestRoutes, getAuthorizationRequest } from './api/authorizationRequestApi';
export { authorizationRequestQueryKeys, authorizationRequestQueryOptions } from './api/authorizationRequestQueries';
export { parseSignInResponse } from './api/parsers';
export { signInFailureOf } from './lib/signInFailure';
export type { SignInFailure } from './lib/signInFailure';
export { resolveSignInState } from './model/signInState';
export type { SignInNotice, SignInState } from './model/signInState';
export type { AuthorizationRequest, AuthorizationRequestStatus, IdentityProvider } from './model/types';
