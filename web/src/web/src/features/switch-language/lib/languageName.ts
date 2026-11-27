/** Name of a language written in that language, e.g. `pl` → `polski`. */
export function languageName(language: string): string {
  return new Intl.DisplayNames([language], { type: 'language' }).of(language) ?? language;
}
