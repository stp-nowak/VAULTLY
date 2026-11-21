import { Card, CenteredScreen, Eyebrow, StatusNotice } from '@vaultly/ui';

interface RouteErrorPageProps {
  /** User-facing explanation. Raw error messages are never shown: they may contain technical details. */
  message?: string;
}

/** Fallback page for unexpected route errors and unknown URLs. */
export function RouteErrorPage({ message = 'Something unexpected happened. Please try again.' }: RouteErrorPageProps) {
  return (
    <CenteredScreen>
      <Card aria-labelledby="route-error-heading" size="hero">
        <Eyebrow>Vaultly</Eyebrow>
        <h1 className="text-3xl" id="route-error-heading">
          Something went wrong.
        </h1>
        <StatusNotice tone="error">{message}</StatusNotice>
      </Card>
    </CenteredScreen>
  );
}
