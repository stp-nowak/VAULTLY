import { createContext, useContext } from 'react';

import type { AuthSession, AuthUser, IdentityDiscoveryDocument } from '@/types/auth';

export type AuthStatus = 'authenticated' | 'checking' | 'unauthenticated';

export interface AuthState {
  accessToken: string | null;
  errorMessage: string | null;
  sessionExpiresAt: number | null;
  sessionId: string | null;
  status: AuthStatus;
  user: AuthUser | null;
}

export interface AuthContextValue extends AuthState {
  beginLogin: (discovery: IdentityDiscoveryDocument) => Promise<void>;
  completeLogin: (input: { code: string; state: string }) => Promise<void>;
  ensureReady: () => Promise<void>;
  getAccessToken: () => Promise<string | null>;
  getSessions: () => Promise<AuthSession[]>;
  isAuthenticated: boolean;
  logout: () => Promise<void>;
}

export const initialAuthState: AuthState = {
  accessToken: null,
  errorMessage: null,
  sessionExpiresAt: null,
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
