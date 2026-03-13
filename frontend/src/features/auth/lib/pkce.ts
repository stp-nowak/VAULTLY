import type { StoredAuthTransaction } from '@/types/auth';

const authTransactionStorageKey = 'vaultly.auth.transaction';

function toBase64Url(bytes: Uint8Array): string {
  let binary = '';
  bytes.forEach((byte) => {
    binary += String.fromCharCode(byte);
  });

  return btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/g, '');
}

async function sha256(value: string): Promise<Uint8Array> {
  const digest = await window.crypto.subtle.digest('SHA-256', new TextEncoder().encode(value));
  return new Uint8Array(digest);
}

function randomString(length: number): string {
  const bytes = new Uint8Array(length);
  window.crypto.getRandomValues(bytes);
  return toBase64Url(bytes).slice(0, length);
}

export async function buildPkcePair(): Promise<{ challenge: string; verifier: string }> {
  const verifier = randomString(96);
  const challenge = toBase64Url(await sha256(verifier));

  return { challenge, verifier };
}

export async function buildAuthorizationRequest(input: {
  authorizationEndpoint: string;
  clientId: string;
  redirectUri: string;
  scopes: string[];
}): Promise<{ authorizationUrl: string; transaction: StoredAuthTransaction }> {
  const { challenge, verifier } = await buildPkcePair();
  const state = randomString(48);
  const url = new URL(input.authorizationEndpoint);

  url.searchParams.set('response_type', 'code');
  url.searchParams.set('client_id', input.clientId);
  url.searchParams.set('redirect_uri', input.redirectUri);
  url.searchParams.set('scope', input.scopes.join(' '));
  url.searchParams.set('code_challenge', challenge);
  url.searchParams.set('code_challenge_method', 'S256');
  url.searchParams.set('state', state);

  return {
    authorizationUrl: url.toString(),
    transaction: {
      codeVerifier: verifier,
      createdAt: Date.now(),
      redirectUri: input.redirectUri,
      state,
    },
  };
}

export function clearStoredAuthTransaction(): void {
  window.sessionStorage.removeItem(authTransactionStorageKey);
}

export function loadStoredAuthTransaction(): StoredAuthTransaction | null {
  const raw = window.sessionStorage.getItem(authTransactionStorageKey);
  if (!raw) {
    return null;
  }

  try {
    const parsed = JSON.parse(raw) as StoredAuthTransaction;
    if (
      typeof parsed.codeVerifier !== 'string' ||
      typeof parsed.createdAt !== 'number' ||
      typeof parsed.redirectUri !== 'string' ||
      typeof parsed.state !== 'string'
    ) {
      return null;
    }

    return parsed;
  } catch {
    return null;
  }
}

export function storeAuthTransaction(transaction: StoredAuthTransaction): void {
  window.sessionStorage.setItem(authTransactionStorageKey, JSON.stringify(transaction));
}
