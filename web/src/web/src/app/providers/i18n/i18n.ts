import i18next from 'i18next';
import LanguageDetector from 'i18next-browser-languagedetector';
import { initReactI18next } from 'react-i18next';

import { fallbackLanguage, supportedLanguages } from '@/shared/i18n';

import { namespaces, resources } from './resources';

i18next.on('languageChanged', () => {
  document.documentElement.lang = i18next.resolvedLanguage ?? fallbackLanguage;
  document.title = i18next.t('common:brand.name');
});

void i18next
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    defaultNS: 'common',
    detection: {
      caches: ['localStorage'],
      lookupLocalStorage: 'vaultly.language',
      order: ['localStorage', 'navigator'],
    },
    fallbackLng: fallbackLanguage,
    initAsync: false,
    interpolation: { escapeValue: false },
    load: 'languageOnly',
    ns: Object.keys(namespaces),
    resources,
    supportedLngs: supportedLanguages,
  });

export { i18next as i18n };
