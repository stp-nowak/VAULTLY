import { createContext, useContext } from 'react';

import type { AuthUser } from './types';

export type AuthStatus = 'authenticated' | 'checking' | 'unauthenticated';

export interface AuthState {
  errorMessage: string | null;
  sessionId: string | null;
  status: AuthStatus;
  user: AuthUser | null;
}

export interface AuthContextValue extends AuthState {
  /**
   * Resolves once the initial session check (`GET /api/auth/user`) has finished, with the state as it is then. Route
   * guards must use this result: the router context still holds the value from before the check.
   */
  ensureReady: () => Promise<AuthState>;
  isAuthenticated: boolean;
  /** Full-page navigation to the web API login endpoint. */
  login: (returnUrl?: string) => void;
  logout: () => Promise<void>;
}

export const initialAuthState: AuthState = {
  errorMessage: null,
  sessionId: null,
  status: 'checking',
  user: null,
};

export const AuthContext = createContext<AuthContextValue | null>(null);

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used inside AuthProvider.');
  }

  return context;
}
