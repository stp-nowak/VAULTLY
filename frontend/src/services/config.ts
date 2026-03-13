const defaultClientId = 'vaultly-frontend';
const defaultScopes = 'openid email profile';
const defaultCallbackPath = '/callback';

function normalizeBaseUrl(value?: string): string {
  return (value ?? '').trim().replace(/\/+$/, '');
}

function splitScopes(value: string): string[] {
  return value
    .split(/\s+/)
    .map((scope) => scope.trim())
    .filter(Boolean);
}

export const appConfig = {
  get callbackPath(): string {
    return import.meta.env.AUTH_CALLBACK_PATH?.trim() || defaultCallbackPath;
  },
  get identityBaseUrl(): string {
    return normalizeBaseUrl(import.meta.env.IDENTITY_BASE_URL);
  },
  get identityClientId(): string {
    return import.meta.env.IDENTITY_CLIENT_ID?.trim() || defaultClientId;
  },
  get identityScopes(): string[] {
    return splitScopes(import.meta.env.IDENTITY_SCOPES?.trim() || defaultScopes);
  },
};

export function getIdentityBaseUrl(): string {
  if (!appConfig.identityBaseUrl) {
    throw new Error(
      'Missing IDENTITY_BASE_URL. Copy .env.example to .env.local and point it at the identity service.',
    );
  }

  return appConfig.identityBaseUrl;
}

export function getRedirectUri(): string {
  return new URL(appConfig.callbackPath, window.location.origin).toString();
}
