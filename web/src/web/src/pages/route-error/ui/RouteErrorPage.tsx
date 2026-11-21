import { Card, CenteredScreen, Eyebrow, StatusNotice } from '@vaultly/ui';

export function RouteErrorPage({ error }: { error: unknown }) {
  const message = error instanceof Error ? error.message : 'An unexpected error occurred.';

  return (
    <CenteredScreen>
      <Card size="hero">
        <Eyebrow>Vaultly</Eyebrow>
        <h1 className="text-3xl">Something went wrong.</h1>
        <StatusNotice tone="error">{message}</StatusNotice>
      </Card>
    </CenteredScreen>
  );
}
