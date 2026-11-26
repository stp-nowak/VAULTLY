# @vaultly/ui

Shared design system for every Vaultly frontend (`web/src/web`, `auth/web`): Tailwind CSS v4 theme tokens,
base styles and React primitives. Part of the root pnpm workspace; apps depend on it with `"@vaultly/ui": "workspace:*"`.

## Commands (from the repository root)
- `pnpm install` — once for the whole workspace
- `pnpm --filter @vaultly/ui lint | typecheck | test`
- A change here also runs CI for every consumer (`ui.yml` + the app workflows watch `packages/ui/**`).

## Structure
```
src/
├── styles.css        # @theme tokens (colors, fonts, tracking, shadows, animations), @utility bg-panel, base layer, @source
├── lib/cn.ts         # clsx + tailwind-merge
├── icons/            # SVG graphics in the current text color: BrandMark (logo), ChevronDownIcon, SearchIcon
├── components/       # React primitives, one folder per kind:
│   ├── actions/      #   Button
│   ├── data/         #   Badge, InfoTile, Table
│   ├── feedback/     #   StatusNotice
│   ├── forms/        #   SearchField, Select, TextField
│   ├── layout/       #   Card, CenteredScreen
│   └── typography/   #   Eyebrow
└── index.ts          # Public API — the only entry point apps import from
```

## How apps consume it
```css
/* app stylesheet */
@import 'tailwindcss';
@import '@vaultly/ui/styles.css';
```
```tsx
import { Button, Card, cn } from '@vaultly/ui';
```
The package ships TypeScript/TSX source (`exports` → `src`); the consuming app's Vite/Tailwind compiles it.
`@source './'` in `styles.css` makes Tailwind scan this package's components from any app.

## Rules
- **Tokens are the single source of truth.** Apps never hard-code colors/fonts; new design values are added to
  `@theme` here and used as utilities (`bg-<token>`, `text-<token>`, `border-<token>` …).
- Only business-agnostic primitives live here (no API calls, routing, auth or app copy). A component belongs here
  once a second app needs it or it encodes a design-system pattern.
- Every component: function component, typed props, accepts `className` and merges it last via `cn`, accessible
  semantics (roles, labels), and a test in its folder's `<folder>.test.tsx` (e.g. `components/forms/forms.test.tsx`).
- Put a component in the folder of its kind; add a folder only for a new kind. Icons live in `icons/`, never inside
  a component file: they are `*Icon` components (decorative, `aria-hidden`, `currentColor`, size via `className`).
- No circular dependencies: `src/circularDependencies/circularDependencies.test.ts` (madge from `src/index.ts`)
  must stay green; `pnpm --filter @vaultly/ui deps:circular` writes a JSON report.
- Export everything public from `src/index.ts`; never deep-import package internals from apps.
- Breaking changes (renamed props/tokens) must update all consumers in the same PR.
- Publishing: the package is publish-ready in shape (`exports`, `peerDependencies`, `files`) but consumed from
  source in the workspace. To publish to a registry later, add a library build (Vite library mode + `.d.ts`) and a
  release workflow; no consumer code needs to change.
