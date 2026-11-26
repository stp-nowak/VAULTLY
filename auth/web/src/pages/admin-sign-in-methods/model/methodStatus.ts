import type { SignInMethod } from '@/domain/sign-in-settings';

export type MethodStatus = 'hidden' | 'notConfigured' | 'shown';

/** How a method appears to people signing in: offered, switched off by an admin, or not set up on this server. */
export function resolveMethodStatus(method: SignInMethod): MethodStatus {
  return method.isConfigured ? (method.isEnabled ? 'shown' : 'hidden') : 'notConfigured';
}
