export type UserRole = 'admin' | 'user';

/** A user as listed in the admin console. */
export interface UserSummary {
  createdAt: string;
  email: string;
  emailVerified: boolean;
  id: string;
  name: string;
  role: UserRole;
  /** Linked providers, e.g. `google`. */
  signInMethods: string[];
}

/** One page of users, newest first. */
export interface UserPage {
  items: UserSummary[];
  page: number;
  pageSize: number;
  /** Users matching the search across all pages. */
  total: number;
}

export interface UserListParams {
  /** 1-based. */
  page: number;
  search?: string;
}
