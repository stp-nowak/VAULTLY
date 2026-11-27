import { useState } from 'react';

import { Button } from '@vaultly/ui';
import { useTranslation } from 'react-i18next';

import type { IdentityProvider } from '@/domain/authorization-request';
import { navigateToUrl } from '@/shared/lib';

interface ProviderButtonsProps {
  providers: IdentityProvider[];
}

/**
 * One button per identity provider. Choosing a provider leaves the SPA (full-page navigation to the provider's
 * start URL); the buttons stay disabled meanwhile so the flow cannot be started twice.
 */
export function ProviderButtons({ providers }: ProviderButtonsProps) {
  const { t } = useTranslation('features/sign-in-with-provider');
  const [selectedProviderId, setSelectedProviderId] = useState<string | null>(null);

  return (
    <div className="grid gap-3">
      {providers.map((provider) => (
        <Button
          disabled={selectedProviderId !== null}
          key={provider.id}
          onClick={() => {
            setSelectedProviderId(provider.id);
            navigateToUrl(provider.startUrl);
          }}
        >
          {t('continueWith', { provider: provider.name })}
        </Button>
      ))}
    </div>
  );
}
