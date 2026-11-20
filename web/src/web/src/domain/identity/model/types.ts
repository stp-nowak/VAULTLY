/** Signed-in user as returned by the web API (`GET /api/auth/user`). */
export interface AuthUser {
  email: string;
  id: string;
  name: string;
}

/** Current browser session: the user plus the id of the underlying Identity session. */
export interface CurrentUser {
  sessionId: string;
  user: AuthUser;
}

/** Identity session listed by `GET /api/auth/sessions`. */
export interface AuthSession {
  createdAt: string;
  id: string;
  ipAddress: string | null;
  isCurrent: boolean;
  isRevoked: boolean;
  lastUsedAt: string;
  userAgent: string | null;
}
