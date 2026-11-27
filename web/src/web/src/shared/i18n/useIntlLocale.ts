import { useTranslation } from 'react-i18next';

import { intlLocales, toLanguage } from './languages';

/** `Intl` locale of the current language, for dates, numbers and currencies. */
export function useIntlLocale(): string {
  const { i18n } = useTranslation();
  return intlLocales[toLanguage(i18n.resolvedLanguage)];
}
