import { Eyebrow } from '@vaultly/ui';

import { SessionSummary, useAuth } from '@/domain/identity';
import { LogoutCard } from '@/features/logout';
import { AppShell } from '@/widgets/app-shell';

export function DashboardPage() {
  const auth = useAuth();
  const firstName = auth.user?.name.split(' ')[0];

  return (
    <AppShell>
      <div className="grid max-w-240 gap-6">
        <header className="grid gap-3.5 pt-5 pb-2">
          <Eyebrow>Settings</Eyebrow>
          <h2 className="text-[clamp(2rem,4vw,3rem)] leading-tight">
            {firstName ? `Welcome back, ${firstName}.` : 'Welcome back.'}
          </h2>
          <p className="text-muted">Review where you are signed in and manage your account.</p>
        </header>

        <SessionSummary />
        <LogoutCard />
      </div>
    </AppShell>
  );
}
