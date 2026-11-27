import { Outlet } from '@tanstack/react-router';
import { TanStackRouterDevtools } from '@tanstack/react-router-devtools';

import { LanguageSelect } from '@/features/switch-language';
import { supportedLanguages } from '@/shared/i18n';

const canSwitchLanguage = supportedLanguages.length > 1;

export function RootLayout() {
  return (
    <>
      <Outlet />
      {canSwitchLanguage && <LanguageSelect className="fixed bottom-4 left-4 z-20" />}
      {import.meta.env.DEV && <TanStackRouterDevtools position="bottom-right" />}
    </>
  );
}
