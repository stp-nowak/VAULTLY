import { getRouteApi } from '@tanstack/react-router';
import { Eyebrow } from '@vaultly/ui';
import { Trans, useTranslation } from 'react-i18next';

import { LoginButton } from '@/features/login';
import { SiteHeader } from '@/widgets/site-header';

import { ExampleMonth } from './ExampleMonth';
import { FeatureList } from './FeatureList';

const landingRoute = getRouteApi('/');

/** Public home page for visitors without a session: introduces the app and offers to log in. */
export function LandingPage() {
  const { returnUrl } = landingRoute.useSearch();
  const { t } = useTranslation(['pages/landing', 'common']);

  return (
    <>
      <SiteHeader actions={<LoginButton className="min-h-11 px-6" returnUrl={returnUrl} />} />

      <main>
        <section className="mx-auto grid max-w-300 gap-14 px-6 pt-16 pb-20 lg:grid-cols-[minmax(0,7fr)_minmax(0,5fr)] lg:items-center lg:gap-16 lg:px-8 lg:pt-24 lg:pb-28">
          <div className="grid gap-7">
            <Eyebrow className="motion-safe:animate-rise">{t('common:brand.tagline')}</Eyebrow>
            <h1 className="text-[clamp(2.75rem,7vw,5.5rem)] leading-[0.98] [animation-delay:80ms] motion-safe:animate-rise">
              <Trans components={{ em: <em className="text-accent" /> }} i18nKey="common:brand.slogan" t={t} />
            </h1>
            <p className="max-w-[34rem] text-lg text-muted [animation-delay:160ms] motion-safe:animate-rise">
              {t('hero.body')}
            </p>
            <div className="flex flex-wrap items-center gap-x-7 gap-y-4 [animation-delay:240ms] motion-safe:animate-rise">
              <LoginButton className="px-8" returnUrl={returnUrl}>
                {t('hero.logIn')}
              </LoginButton>
              <a
                className="border-b border-line-strong pb-0.5 text-muted transition-colors hover:border-accent hover:text-ink"
                href="#features"
              >
                {t('hero.seeFeatures')}
              </a>
            </div>
          </div>

          <ExampleMonth className="[animation-delay:320ms] motion-safe:animate-rise lg:rotate-2" />
        </section>

        <FeatureList />
      </main>

      <footer className="border-t border-line">
        <p className="mx-auto max-w-300 px-6 py-8 text-sm text-muted lg:px-8">
          {t('footer', { brand: t('common:brand.name'), tagline: t('common:brand.tagline') })}
        </p>
      </footer>
    </>
  );
}
