import type { Resource } from 'i18next';

import { identityTranslations } from '@/domain/identity';
import { loginTranslations } from '@/features/login';
import { logoutTranslations } from '@/features/logout';
import { switchLanguageTranslations } from '@/features/switch-language';
import { dashboardTranslations } from '@/pages/dashboard';
import { landingTranslations } from '@/pages/landing';
import { routeErrorTranslations } from '@/pages/route-error';
import { commonTranslations, supportedLanguages, type Language } from '@/shared/i18n';
import { appShellTranslations } from '@/widgets/app-shell';

/** Every namespace of the SPA in every language: `common` plus one per slice, named after the slice's path. */
export const namespaces = {
  common: commonTranslations,
  'domain/identity': identityTranslations,
  'features/login': loginTranslations,
  'features/logout': logoutTranslations,
  'features/switch-language': switchLanguageTranslations,
  'pages/dashboard': dashboardTranslations,
  'pages/landing': landingTranslations,
  'pages/route-error': routeErrorTranslations,
  'widgets/app-shell': appShellTranslations,
} as const;

function namespacesIn(language: Language) {
  return Object.fromEntries(Object.entries(namespaces).map(([namespace, texts]) => [namespace, texts[language]]));
}

export const resources: Resource = Object.fromEntries(
  supportedLanguages.map((language) => [language, namespacesIn(language)]),
);
