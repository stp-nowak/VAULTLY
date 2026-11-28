import { useCallback, useEffect, useMemo, useRef, useState } from 'react';

import { navigateToUrl } from '@/shared/lib';

import { buildLoginUrl, getCurrentUser, logout as logoutRequest } from '../api/identityApi';
import { AuthContext, initialAuthState, type AuthContextValue, type AuthState } from './authContext';

function resolveErrorMessage(error: unknown, fallback: string): string {
  return error instanceof Error && error.message.trim() ? error.message : fallback;
}

const signedOutState: AuthState = { errorMessage: null, sessionId: null, status: 'unauthenticated', user: null };

/**
 * Holds the signed-in user for the SPA. The session itself lives on the web API (HttpOnly cookie + Redis);
 * the SPA never sees or stores tokens, it only asks who is signed in.
 */
export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [state, setState] = useState<AuthState>(initialAuthState);
  // Route guards run before React re-renders with the new state, so they read the latest one from here.
  const latestState = useRef<AuthState>(initialAuthState);
  const initialization = useRef<Promise<void> | null>(null);

  const changeState = useCallback((next: AuthState) => {
    latestState.current = next;
    setState(next);
  }, []);

  const loadCurrentUser = useCallback(async () => {
    try {
      const current = await getCurrentUser();
      changeState(
        current
          ? { errorMessage: null, sessionId: current.sessionId, status: 'authenticated', user: current.user }
          : signedOutState,
      );
    } catch (error) {
      changeState({ ...signedOutState, errorMessage: resolveErrorMessage(error, 'Unable to restore the current session.') });
    }
  }, [changeState]);

  const ensureReady = useCallback(async () => {
    initialization.current ??= loadCurrentUser();
    await initialization.current;
    return latestState.current;
  }, [loadCurrentUser]);

  useEffect(() => {
    void ensureReady();
  }, [ensureReady]);

  const login = useCallback((returnUrl?: string) => {
    navigateToUrl(buildLoginUrl(returnUrl));
  }, []);

  const logout = useCallback(async () => {
    const logoutUrl = await logoutRequest();
    changeState(signedOutState);
    navigateToUrl(logoutUrl);
  }, [changeState]);

  const value = useMemo<AuthContextValue>(
    () => ({ ...state, ensureReady, isAuthenticated: state.status === 'authenticated', login, logout }),
    [ensureReady, login, logout, state],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
