/**
 * Full-page navigation that leaves the SPA (e.g. to an external sign-in provider).
 * Wrapped so tests can replace it; jsdom does not implement navigation.
 */
export function navigateToUrl(url: string): void {
  window.location.assign(url);
}
