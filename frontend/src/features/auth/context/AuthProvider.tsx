import { useCallback, useEffect, useMemo, useRef, useState } from 'react';

import {
  exchangeAuthorizationCode,
  getIdentityDiscovery,
  listSessions,
  logoutSession,
  refreshSession,
} from '@/features/auth/api/identity';
import {
  buildAuthorizationRequest,
  clearStoredAuthTransaction,
  loadStoredAuthTransaction,
  storeAuthTransaction,
} from '@/features/auth/lib/pkce';
import {
  AuthContext,
  initialAuthState,
  type AuthContextValue,
  type AuthState,
} from '@/features/auth/context/AuthContext';
import { appConfig, getIdentityBaseUrl, getRedirectUri } from '@/services/config';
import { HttpError } from '@/services/http';
import type { AuthUser, IdentityDiscoveryDocument } from '@/types/auth';

function resolveErrorMessage(error: unknown, fallback: string): string {
  if (error instanceof Error && error.message.trim()) {
    return error.message;
  }

  return fallback;
}

function shouldRefresh(expiresAt: number | null): boolean {
  if (expiresAt === null) {
    return true;
  }

  return Date.now() >= expiresAt - 60_000;
}

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [state, setState] = useState<AuthState>(initialAuthState);
  const initializationPromise = useRef<Promise<void> | null>(null);
  const refreshPromise = useRef<Promise<string | null> | null>(null);

  const applyAuthenticatedState = useCallback(
    (result: { accessToken: string; expiresIn: number; sessionId: string; user: AuthUser }) => {
      setState({
        accessToken: result.accessToken,
        errorMessage: null,
        sessionExpiresAt: Date.now() + result.expiresIn * 1000,
        sessionId: result.sessionId,
        status: 'authenticated',
        user: result.user,
      });
    },
    [],
  );

  const clearSession = useCallback((errorMessage: string | null = null) => {
    setState({
      accessToken: null,
      errorMessage,
      sessionExpiresAt: null,
      sessionId: null,
      status: 'unauthenticated',
      user: null,
    });
  }, []);

  const refreshAccessToken = useCallback(async (): Promise<string | null> => {
    if (refreshPromise.current) {
      return refreshPromise.current;
    }

    refreshPromise.current = (async () => {
      try {
        getIdentityBaseUrl();
      } catch (error) {
        clearSession(resolveErrorMessage(error, 'Identity configuration is missing.'));
        return null;
      }

      try {
        const result = await refreshSession();
        applyAuthenticatedState(result);
        return result.accessToken;
      } catch (error) {
        if (error instanceof HttpError && error.status === 401) {
          clearSession(null);
          return null;
        }

        clearSession(resolveErrorMessage(error, 'Unable to restore the current session.'));
        return null;
      } finally {
        refreshPromise.current = null;
      }
    })();

    return refreshPromise.current;
  }, [applyAuthenticatedState, clearSession]);

  const ensureReady = useCallback(async () => {
    if (!initializationPromise.current) {
      initializationPromise.current = (async () => {
        await refreshAccessToken();
      })();
    }

    await initializationPromise.current;
  }, [refreshAccessToken]);

  useEffect(() => {
    void ensureReady();
  }, [ensureReady]);

  const beginLogin = useCallback(async (discovery: IdentityDiscoveryDocument) => {
    const { authorizationUrl, transaction } = await buildAuthorizationRequest({
      authorizationEndpoint: discovery.authorizationEndpoint,
      clientId: appConfig.identityClientId,
      redirectUri: getRedirectUri(),
      scopes: appConfig.identityScopes,
    });

    storeAuthTransaction(transaction);
    window.location.assign(authorizationUrl);
  }, []);

  const completeLogin = useCallback(
    async (input: { code: string; state: string }) => {
      const transaction = loadStoredAuthTransaction();
      if (!transaction) {
        throw new Error('No login transaction was found. Start the login flow again.');
      }

      if (transaction.state !== input.state) {
        clearStoredAuthTransaction();
        throw new Error('The returned login state did not match the stored PKCE transaction.');
      }

      const discovery = await getIdentityDiscovery();
      const result = await exchangeAuthorizationCode({
        clientId: appConfig.identityClientId,
        code: input.code,
        codeVerifier: transaction.codeVerifier,
        redirectUri: transaction.redirectUri,
        tokenEndpoint: discovery.tokenEndpoint,
      });

      clearStoredAuthTransaction();
      applyAuthenticatedState(result);
    },
    [applyAuthenticatedState],
  );

  const getAccessToken = useCallback(async (): Promise<string | null> => {
    if (state.status === 'authenticated' && state.accessToken && !shouldRefresh(state.sessionExpiresAt)) {
      return state.accessToken;
    }

    return refreshAccessToken();
  }, [refreshAccessToken, state.accessToken, state.sessionExpiresAt, state.status]);

  const getSessions = useCallback(async () => {
    const accessToken = await getAccessToken();
    if (!accessToken) {
      throw new Error('Your session has ended. Sign in again to continue.');
    }

    return listSessions(accessToken);
  }, [getAccessToken]);

  const logout = useCallback(async () => {
    try {
      getIdentityBaseUrl();
    } catch (error) {
      clearSession(resolveErrorMessage(error, 'Identity configuration is missing.'));
      return;
    }

    try {
      await logoutSession();
      clearSession(null);
    } catch (error) {
      if (error instanceof HttpError && error.status === 401) {
        clearSession(null);
        return;
      }

      throw error;
    }
  }, [clearSession]);

  const value = useMemo<AuthContextValue>(
    () => ({
      ...state,
      beginLogin,
      completeLogin,
      ensureReady,
      getAccessToken,
      getSessions,
      isAuthenticated: state.status === 'authenticated',
      logout,
    }),
    [beginLogin, completeLogin, ensureReady, getAccessToken, getSessions, logout, state],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
