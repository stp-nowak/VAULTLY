/** Generic texts used across slices (namespace `common`). Slice-specific texts live in the slice's own `i18n/`. */
export const en = {
  actions: {
    next: 'Next',
    previous: 'Previous',
    search: 'Search',
    signIn: 'Sign in',
  },
  brand: {
    name: 'Vaultly',
  },
  errors: {
    tooManyAttempts: 'Too many attempts. Wait a minute and try again.',
    unexpected: 'Something went wrong.',
  },
  fields: {
    email: 'Email',
    name: 'Name',
    password: 'Password',
  },
  states: {
    loading: 'Loading…',
  },
} as const;
