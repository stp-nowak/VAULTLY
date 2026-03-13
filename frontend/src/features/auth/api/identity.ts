import { parseDiscoveryDocument, parseSessionsResponse, parseTokenExchangeResult } from '@/features/auth/lib/parsers';
import { getIdentityBaseUrl } from '@/services/config';
import { fetchJson } from '@/services/http';
import type {
  AuthSession,
  IdentityDiscoveryDocument,
  TokenExchangeResult,
} from '@/types/auth';

export async function exchangeAuthorizationCode(input: {
  clientId: string;
  code: string;
  codeVerifier: string;
  redirectUri: string;
  tokenEndpoint: string;
}): Promise<TokenExchangeResult> {
  const payload = new URLSearchParams({
    client_id: input.clientId,
    code: input.code,
    code_verifier: input.codeVerifier,
    grant_type: 'authorization_code',
    redirect_uri: input.redirectUri,
  });

  const response = await fetchJson<unknown>(input.tokenEndpoint, {
    body: payload,
    credentials: 'include',
    headers: {
      'Content-Type': 'application/x-www-form-urlencoded',
    },
    method: 'POST',
  });

  return parseTokenExchangeResult(response);
}

export async function getIdentityDiscovery(): Promise<IdentityDiscoveryDocument> {
  const response = await fetchJson<unknown>(`${getIdentityBaseUrl()}/.well-known/openid-configuration`, {
    credentials: 'include',
  });

  return parseDiscoveryDocument(response);
}

export async function listSessions(accessToken: string): Promise<AuthSession[]> {
  const response = await fetchJson<unknown>(`${getIdentityBaseUrl()}/sessions`, {
    credentials: 'include',
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
  });

  return parseSessionsResponse(response);
}

export async function logoutSession(): Promise<void> {
  await fetchJson<null>(`${getIdentityBaseUrl()}/logout`, {
    credentials: 'include',
    method: 'POST',
  });
}

export async function refreshSession(): Promise<TokenExchangeResult> {
  const response = await fetchJson<unknown>(`${getIdentityBaseUrl()}/refresh`, {
    credentials: 'include',
    method: 'POST',
  });

  return parseTokenExchangeResult(response);
}
