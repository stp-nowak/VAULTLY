/** The admin signed in to the admin console. */
export interface ConsoleAdmin {
  email: string;
  id: string;
  name: string;
}

/** Whether the browser may use the admin console: an admin, a signed-in user without the role, or nobody. */
export type ConsoleAccess = { admin: ConsoleAdmin; status: 'admin' } | { status: 'forbidden' } | { status: 'signed-out' };

export type ConsoleAccessStatus = ConsoleAccess['status'];
