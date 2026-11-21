import { Card, CenteredScreen, Eyebrow } from '@vaultly/ui';

/** Final page after signing out of the Identity service. */
export function LoggedOutPage() {
  return (
    <CenteredScreen>
      <Card aria-labelledby="logged-out-heading" size="hero">
        <Eyebrow>Vaultly</Eyebrow>
        <h1 className="text-3xl" id="logged-out-heading">
          You&apos;re signed out.
        </h1>
        <p className="text-muted">You can close this window.</p>
      </Card>
    </CenteredScreen>
  );
}
