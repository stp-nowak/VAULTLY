import type { Resource } from 'i18next';

import { authorizationRequestTranslations } from '@/domain/authorization-request';
import { assignUserRoleTranslations } from '@/features/assign-user-role';
import { registerWithPasswordTranslations } from '@/features/register-with-password';
import { signInWithPasswordTranslations } from '@/features/sign-in-with-password';
import { signInWithProviderTranslations } from '@/features/sign-in-with-provider';
import { signOutTranslations } from '@/features/sign-out';
import { switchLanguageTranslations } from '@/features/switch-language';
import { toggleSignInMethodTranslations } from '@/features/toggle-sign-in-method';
import { adminConsoleTranslations } from '@/pages/admin-console';
import { adminSignInMethodsTranslations } from '@/pages/admin-sign-in-methods';
import { adminUsersTranslations } from '@/pages/admin-users';
import { loggedOutTranslations } from '@/pages/logged-out';
import { loginTranslations } from '@/pages/login';
import { registerTranslations } from '@/pages/register';
import { routeErrorTranslations } from '@/pages/route-error';
import { signInErrorTranslations } from '@/pages/sign-in-error';
import { commonTranslations, supportedLanguages, type Language } from '@/shared/i18n';
import { consoleShellTranslations } from '@/widgets/console-shell';

import { appTranslations } from './appTranslations';

/** Every namespace of the SPA in every language: `app`, `common` and one per slice, named after the slice's path. */
export const namespaces = {
  app: appTranslations,
  common: commonTranslations,
  'domain/authorization-request': authorizationRequestTranslations,
  'features/assign-user-role': assignUserRoleTranslations,
  'features/register-with-password': registerWithPasswordTranslations,
  'features/sign-in-with-password': signInWithPasswordTranslations,
  'features/sign-in-with-provider': signInWithProviderTranslations,
  'features/sign-out': signOutTranslations,
  'features/switch-language': switchLanguageTranslations,
  'features/toggle-sign-in-method': toggleSignInMethodTranslations,
  'pages/admin-console': adminConsoleTranslations,
  'pages/admin-sign-in-methods': adminSignInMethodsTranslations,
  'pages/admin-users': adminUsersTranslations,
  'pages/logged-out': loggedOutTranslations,
  'pages/login': loginTranslations,
  'pages/register': registerTranslations,
  'pages/route-error': routeErrorTranslations,
  'pages/sign-in-error': signInErrorTranslations,
  'widgets/console-shell': consoleShellTranslations,
} as const;

function namespacesIn(language: Language) {
  return Object.fromEntries(Object.entries(namespaces).map(([namespace, texts]) => [namespace, texts[language]]));
}

export const resources: Resource = Object.fromEntries(
  supportedLanguages.map((language) => [language, namespacesIn(language)]),
);
