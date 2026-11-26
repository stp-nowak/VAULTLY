/** Lifecycle of an authorization request in the Identity service. Only `pending` requests can still sign in. */
export type AuthorizationRequestStatus = 'pending' | 'expired' | 'completed';

/** External identity provider the user can sign in with (e.g. Google). */
export interface IdentityProvider {
  id: string;
  name: string;
  /** Same-origin path that starts sign-in with this provider; a full-page navigation. */
  startUrl: string;
}

/**
 * Authorization request started by a client app (`/authorize`) or the admin console, as returned by
 * `GET /api/authorization-requests/{id}`.
 */
export interface AuthorizationRequest {
  clientName: string;
  id: string;
  /** Whether email and password sign-in and registration are offered. */
  passwordSignIn: boolean;
  providers: IdentityProvider[];
  status: AuthorizationRequestStatus;
}
