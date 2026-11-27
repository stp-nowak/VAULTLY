import { Select } from '@vaultly/ui';
import { useTranslation } from 'react-i18next';

import { fallbackLanguage, supportedLanguages } from '@/shared/i18n';

import { languageName } from '../lib/languageName';

interface LanguageSelectProps {
  className?: string;
  languages?: readonly string[];
}

/** Switches the UI language; the choice is remembered in this browser. */
export function LanguageSelect({ className, languages = supportedLanguages }: LanguageSelectProps) {
  const { i18n, t } = useTranslation('features/switch-language');
  const options = languages.map((language) => ({ label: languageName(language), value: language }));

  return (
    <Select
      aria-label={t('label')}
      className={className}
      onValueChange={(language) => void i18n.changeLanguage(language)}
      options={options}
      value={i18n.resolvedLanguage ?? fallbackLanguage}
    />
  );
}
