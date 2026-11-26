/** A way to sign in to the Identity service, as the admin console shows it. */
export interface SignInMethod {
  /** e.g. `google`, `password`. */
  id: string;
  /** Whether admins allow it. */
  isEnabled: boolean;
  /** Whether this deployment is set up for it; an unconfigured method cannot be used even when enabled. */
  isConfigured: boolean;
  name: string;
}
