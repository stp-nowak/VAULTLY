import { Link } from '@tanstack/react-router';
import { useEffect, useId, useRef, useState } from 'react';

import { BrandMark, CloseIcon, cn, Eyebrow, MenuIcon } from '@vaultly/ui';

import { consoleRoutes, type ConsoleAdmin } from '@/domain/console-admin';

interface ConsoleShellProps {
  admin: ConsoleAdmin;
  children: React.ReactNode;
}

const navLink =
  'rounded-xl border border-transparent px-4 py-2.5 text-muted transition-colors hover:bg-surface-raised hover:text-ink';

const iconButton =
  'grid size-10 shrink-0 cursor-pointer place-items-center rounded-xl text-muted transition-colors hover:bg-surface-raised hover:text-ink';

/**
 * Frame of every admin console page: a sidebar with the brand, navigation and the signed-in admin. On narrow screens
 * the sidebar is a drawer that slides in from the left, opened from a menu button in a slim top bar.
 */
export function ConsoleShell({ admin, children }: ConsoleShellProps) {
  const menuId = useId();
  const closeButton = useRef<HTMLButtonElement>(null);
  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const closeMenu = () => setIsMenuOpen(false);

  useEffect(() => {
    if (!isMenuOpen) {
      return;
    }

    closeButton.current?.focus();
    const closeOnEscape = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        setIsMenuOpen(false);
      }
    };
    window.addEventListener('keydown', closeOnEscape);
    return () => window.removeEventListener('keydown', closeOnEscape);
  }, [isMenuOpen]);

  return (
    <div className="min-h-screen lg:grid lg:grid-cols-[16rem_minmax(0,1fr)]">
      <header className="sticky top-0 z-10 flex items-center gap-3 border-b border-line bg-sidebar px-3 py-2.5 backdrop-blur-lg lg:hidden">
        <button
          aria-controls={menuId}
          aria-expanded={isMenuOpen}
          aria-label="Open menu"
          className={iconButton}
          onClick={() => setIsMenuOpen(true)}
          type="button"
        >
          <MenuIcon />
        </button>
        <span className="font-display text-lg font-semibold tracking-display">Admin console</span>
      </header>

      {isMenuOpen ? (
        <div aria-hidden className="fixed inset-0 z-20 bg-canvas/70 backdrop-blur-sm lg:hidden" onClick={closeMenu} />
      ) : null}

      {/* Closed on narrow screens it is `invisible`, which also takes it out of the tab order and the accessibility tree. */}
      <aside
        className={cn(
          'fixed inset-y-0 left-0 z-30 flex w-72 max-w-[85vw] flex-col gap-8 border-r border-line bg-canvas p-6 shadow-panel',
          'transition-[translate,visibility] duration-200 ease-out',
          isMenuOpen ? 'visible translate-x-0' : 'invisible -translate-x-full',
          'lg:visible lg:sticky lg:top-0 lg:z-auto lg:h-screen lg:w-auto lg:max-w-none lg:translate-x-0 lg:bg-sidebar lg:shadow-none lg:backdrop-blur-lg',
        )}
        id={menuId}
      >
        <div className="flex items-center justify-between gap-3">
          <div className="flex items-center gap-3">
            <BrandMark />
            <p className="grid leading-tight">
              <Eyebrow>Vaultly</Eyebrow>
              <span className="font-display text-xl font-semibold tracking-display whitespace-nowrap">Admin console</span>
            </p>
          </div>
          <button aria-label="Close menu" className={cn(iconButton, 'lg:hidden')} onClick={closeMenu} ref={closeButton} type="button">
            <CloseIcon />
          </button>
        </div>

        <nav aria-label="Admin console" className="grid gap-1.5">
          <Link
            activeProps={{ className: 'border-line-strong bg-accent-soft text-ink' }}
            className={navLink}
            onClick={closeMenu}
            to="/admin/users"
          >
            Users
          </Link>
        </nav>

        <div className="mt-auto grid gap-2 border-t border-line pt-5 text-sm">
          <span className="break-all text-muted">{admin.email}</span>
          <a className="justify-self-start border-b border-line-strong pb-0.5 hover:border-accent" href={consoleRoutes.signOut}>
            Sign out
          </a>
        </div>
      </aside>

      <main className="min-w-0 px-6 py-10 lg:px-10">
        <div className="max-w-300">{children}</div>
      </main>
    </div>
  );
}
