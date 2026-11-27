export const supportedLanguages = ['en'] as const;

export type Language = (typeof supportedLanguages)[number];

export const fallbackLanguage: Language = 'en';

/** Locale passed to `Intl` formatters for each language. */
export const intlLocales: Record<Language, string> = {
  en: 'en-GB',
};

export function toLanguage(value: string | undefined): Language {
  return supportedLanguages.find((language) => language === value) ?? fallbackLanguage;
}
