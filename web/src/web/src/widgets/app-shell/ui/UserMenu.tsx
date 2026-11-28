import { Link } from '@tanstack/react-router';
import { cn, SettingsIcon } from '@vaultly/ui';
import { useEffect, useId, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';

import { useAuth } from '@/domain/identity';
import { LogoutMenuItem } from '@/features/logout';

import { initialsOf } from '../lib/initials';

const menuItem =
  'flex w-full cursor-pointer items-center gap-3 rounded-xl px-3 py-2.5 text-left text-ink hover:bg-surface-raised focus:bg-surface-raised focus:outline-none disabled:cursor-wait';

/** The signed-in user's initials; opens a menu with who is signed in, settings and logging out. */
export function UserMenu() {
  const auth = useAuth();
  const { t } = useTranslation('widgets/app-shell');
  const [isOpen, setIsOpen] = useState(false);
  const root = useRef<HTMLDivElement>(null);
  const button = useRef<HTMLButtonElement>(null);
  const menuId = useId();
  const name = auth.user?.name ?? '';
  const email = auth.user?.email ?? '';

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    const closeOutside = (event: PointerEvent) => {
      if (!root.current?.contains(event.target as Node)) {
        setIsOpen(false);
      }
    };
    root.current?.querySelector<HTMLElement>('[role="menuitem"]')?.focus();
    document.addEventListener('pointerdown', closeOutside);
    return () => document.removeEventListener('pointerdown', closeOutside);
  }, [isOpen]);

  const onMenuKeyDown = (event: React.KeyboardEvent<HTMLDivElement>) => {
    const items = [...event.currentTarget.querySelectorAll<HTMLElement>('[role="menuitem"]')];
    const position = items.indexOf(document.activeElement as HTMLElement);
    const moves: Record<string, () => HTMLElement | undefined> = {
      ArrowDown: () => items[(position + 1) % items.length],
      ArrowUp: () => items[(position - 1 + items.length) % items.length],
      End: () => items.at(-1),
      Home: () => items[0],
    };

    if (event.key === 'Escape') {
      setIsOpen(false);
      button.current?.focus();
      return;
    }

    const next = moves[event.key]?.();
    if (next) {
      event.preventDefault();
      next.focus();
    }
  };

  return (
    <div className="relative" ref={root}>
      <button
        aria-controls={menuId}
        aria-expanded={isOpen}
        aria-haspopup="menu"
        aria-label={t('userMenu.open', { name })}
        className={cn(
          'grid size-10 cursor-pointer place-items-center rounded-xl border border-line-strong bg-accent-soft',
          'text-sm font-bold text-accent transition-colors hover:border-accent focus:outline-none focus-visible:border-accent',
        )}
        onClick={() => setIsOpen(!isOpen)}
        ref={button}
        type="button"
      >
        {initialsOf(name, email)}
      </button>

      {isOpen && (
        <div
          aria-label={t('userMenu.label')}
          className="absolute top-full right-0 z-30 mt-2 grid w-72 gap-1 rounded-2xl border border-line-strong bg-panel p-2 shadow-panel backdrop-blur-xl"
          id={menuId}
          onKeyDown={onMenuKeyDown}
          role="menu"
        >
          <div className="flex items-center gap-3 px-2 py-2">
            <span className="grid size-10 shrink-0 place-items-center rounded-xl bg-accent-soft text-sm font-bold text-accent">
              {initialsOf(name, email)}
            </span>
            <span className="grid min-w-0">
              <span className="truncate font-bold">{name}</span>
              <span className="truncate text-sm text-muted">{email}</span>
            </span>
          </div>
          <hr className="my-1 border-line" />
          <Link className={menuItem} onClick={() => setIsOpen(false)} role="menuitem" to="/app/settings">
            <SettingsIcon className="size-5 text-muted" />
            {t('userMenu.settings')}
          </Link>
          <LogoutMenuItem className={menuItem} />
        </div>
      )}
    </div>
  );
}
