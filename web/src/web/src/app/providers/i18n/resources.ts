import type { Resource } from 'i18next';

import { accountTranslations } from '@/domain/account';
import { identityTranslations } from '@/domain/identity';
import { transactionTranslations } from '@/domain/transaction';
import { closeAccountTranslations } from '@/features/account/close';
import { openAccountTranslations } from '@/features/account/open';
import { renameAccountTranslations } from '@/features/account/rename';
import { loginTranslations } from '@/features/login';
import { logoutTranslations } from '@/features/logout';
import { switchLanguageTranslations } from '@/features/switch-language';
import { changeTransactionTranslations } from '@/features/transaction/change';
import { deleteTransactionTranslations } from '@/features/transaction/delete';
import { recordTransactionTranslations } from '@/features/transaction/record';
import { accountPageTranslations } from '@/pages/account';
import { accountsTranslations } from '@/pages/accounts';
import { netWorthPageTranslations } from '@/pages/net-worth';
import { overviewTranslations } from '@/pages/overview';
import { landingTranslations } from '@/pages/landing';
import { routeErrorTranslations } from '@/pages/route-error';
import { settingsTranslations } from '@/pages/settings';
import { transactionsTranslations } from '@/pages/transactions';
import { commonTranslations, supportedLanguages, type Language } from '@/shared/i18n';
import { accountListTranslations } from '@/widgets/account-list';
import { appShellTranslations } from '@/widgets/app-shell';
import { netWorthTranslations } from '@/widgets/net-worth';
import { transactionListTranslations } from '@/widgets/transaction-list';

/** Every namespace of the SPA in every language: `common` plus one per slice, named after the slice's path. */
export const namespaces = {
  common: commonTranslations,
  'domain/account': accountTranslations,
  'domain/identity': identityTranslations,
  'domain/transaction': transactionTranslations,
  'features/account/close': closeAccountTranslations,
  'features/account/open': openAccountTranslations,
  'features/account/rename': renameAccountTranslations,
  'features/login': loginTranslations,
  'features/logout': logoutTranslations,
  'features/switch-language': switchLanguageTranslations,
  'features/transaction/change': changeTransactionTranslations,
  'features/transaction/delete': deleteTransactionTranslations,
  'features/transaction/record': recordTransactionTranslations,
  'pages/account': accountPageTranslations,
  'pages/accounts': accountsTranslations,
  'pages/landing': landingTranslations,
  'pages/net-worth': netWorthPageTranslations,
  'pages/overview': overviewTranslations,
  'pages/route-error': routeErrorTranslations,
  'pages/settings': settingsTranslations,
  'pages/transactions': transactionsTranslations,
  'widgets/account-list': accountListTranslations,
  'widgets/app-shell': appShellTranslations,
  'widgets/net-worth': netWorthTranslations,
  'widgets/transaction-list': transactionListTranslations,
} as const;

function namespacesIn(language: Language) {
  return Object.fromEntries(Object.entries(namespaces).map(([namespace, texts]) => [namespace, texts[language]]));
}

export const resources: Resource = Object.fromEntries(
  supportedLanguages.map((language) => [language, namespacesIn(language)]),
);
