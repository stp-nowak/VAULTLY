export interface AuthUser {
  email: string;
  id: string;
  name: string;
}

export interface AuthSession {
  createdAt: string;
  id: string;
  ipAddress: string | null;
  isCurrent: boolean;
  isRevoked: boolean;
  lastUsedAt: string;
  userAgent: string | null;
}

export interface IdentityDiscoveryDocument {
  authorizationEndpoint: string;
  issuer: string;
  jwksUri: string;
  tokenEndpoint: string;
}

export interface TokenExchangeResult {
  accessToken: string;
  expiresIn: number;
  sessionId: string;
  user: AuthUser;
}

export interface StoredAuthTransaction {
  codeVerifier: string;
  createdAt: number;
  redirectUri: string;
  state: string;
}
