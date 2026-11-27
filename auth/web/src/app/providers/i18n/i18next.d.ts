import 'i18next';

import type { namespaces } from './resources';

declare module 'i18next' {
  interface CustomTypeOptions {
    defaultNS: 'common';
    resources: { [Namespace in keyof typeof namespaces]: (typeof namespaces)[Namespace]['en'] };
  }
}
