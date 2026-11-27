import { useState } from 'react';

import { Button, Card, Eyebrow, InfoTile, StatusNotice } from '@vaultly/ui';
import { useTranslation } from 'react-i18next';

import { useAuth } from '@/domain/identity';

export function LogoutCard() {
  const auth = useAuth();
  const { t } = useTranslation('features/logout');
  const [hasFailed, setHasFailed] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);

  return (
    <Card aria-labelledby="account-heading">
      <div>
        <Eyebrow>{t('eyebrow')}</Eyebrow>
        <h2 className="text-2xl" id="account-heading">
          {t('heading')}
        </h2>
      </div>

      <dl className="grid gap-3.5">
        <InfoTile label={t('signedInAs')} value={<strong>{auth.user?.email ?? t('unknownUser')}</strong>} />
      </dl>

      {hasFailed && <StatusNotice tone="error">{t('failed')}</StatusNotice>}

      <Button
        disabled={isSubmitting}
        onClick={() => {
          setHasFailed(false);
          setIsSubmitting(true);

          void auth
            .logout()
            .catch(() => {
              setHasFailed(true);
            })
            .finally(() => {
              setIsSubmitting(false);
            });
        }}
        variant="secondary"
      >
        {isSubmitting ? t('signingOut') : t('signOut')}
      </Button>
    </Card>
  );
}
