import type { Language } from './languages';

/** The same keys as the English texts, worded freely. */
export type Messages<T> = { readonly [K in keyof T]: T[K] extends string ? string : Messages<T[K]> };

/** Texts of one namespace in every supported language; English defines the keys. */
export type Translations<T> = { readonly en: T } & Readonly<Record<Exclude<Language, 'en'>, Messages<T>>>;
