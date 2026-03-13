# React App Agent Skill

## Stack
- **React 19** + **TypeScript** (strict mode)
- **Vite** (bundler + dev server)
- **TanStack Router** (file-based routing)

---

## Project Structure

```
src/
├── assets/           # Static files
├── components/       # Shared/reusable UI components
│   └── ui/           # Primitive UI elements (Button, Input, etc.)
├── features/         # Feature-based modules (co-locate components, hooks, utils)
│   └── [feature]/
│       ├── components/
│       ├── hooks/
│       └── api/
├── hooks/            # Global shared hooks
├── layouts/          # Layout wrapper components
├── lib/              # Third-party lib configs (e.g. queryClient.ts)
├── routes/           # TanStack Router route files
├── services/         # API clients / data fetching logic
├── stores/           # Global state (Redux slices / Context)
├── types/            # Global TypeScript types & interfaces
└── utils/            # Pure utility functions
```

> **Rule**: Keep code close to where it's used. Only promote to a higher level when truly shared.

---

## TypeScript

- Always use `strict: true` in `tsconfig.json`
- Prefer `interface` for object shapes, `type` for unions/intersections
- Never use `any` — use `unknown` + type guards when type is truly unknown
- Define API response types in `src/types/` or co-locate in `services/`
- Use `satisfies` operator to validate objects against types without widening
- Export types separately from values (`export type { Foo }`)

---

## Component Patterns

- **Default**: functional components with explicit return types
- One component per file; filename = component name (PascalCase)
- Keep components small and focused — extract logic into custom hooks
- Use **composition over configuration** — prefer `children` and slot patterns over large prop APIs
- Avoid prop drilling beyond 2 levels — lift to context or a store

```tsx
// Good
interface Props {
  title: string;
  children: React.ReactNode;
}

export function Card({ title, children }: Props) {
  return (
    <div>
      <h2>{title}</h2>
      {children}
    </div>
  );
}
```

---

## Performance

- **Code-split** routes automatically via TanStack Router's lazy loading
- **Lazy load** heavy components with `React.lazy` + `Suspense`
- Use `useMemo` / `useCallback` **only when profiling shows a problem** — don't pre-optimize
- Virtualize long lists (`@tanstack/react-virtual`)
- Keep state as local as possible — global state is a last resort
- Avoid anonymous functions and object literals in JSX props on hot render paths
- Use `React.memo` for pure components that receive stable props and re-render often

---

## State Management

| Scope | Tool |
|---|---|
| Local UI state | `useState` / `useReducer` |
| Server/async state | TanStack Query |
| Global client state | Redux Toolkit (or React Context for simple cases) |
| URL state | TanStack Router search params |

- Do **not** sync server data into client state — let TanStack Query own it
- Derive state instead of duplicating it
- Use **Redux Toolkit (RTK)** — never plain Redux
  - Define state in `createSlice`, scoped per feature (`src/features/[feature]/slice.ts`)
  - Use `createSelector` for memoized derived state
  - Avoid non-serializable values (class instances, functions) in the store
  - Don't use RTK Query if TanStack Query is already in the stack — pick one

---

## TanStack Router

- Use **file-based routing** (`src/routes/`)
- Define loaders for data fetching at the route level (avoids waterfall)
- Use **search params** for shareable UI state (filters, pagination)
- Protect private routes with `beforeLoad` guards
- Always define route params and search params with TypeScript via `Route.createFileRoute()`
- When a route owns server data, preload it into TanStack Query in the loader instead of fetching the same resource twice

```tsx
// src/routes/users/$userId.tsx
export const Route = createFileRoute('/users/$userId')({
  loader: ({ params }) => fetchUser(params.userId),
  component: UserPage,
});
```

---

## Data Fetching

- Use **TanStack Query** for all server state
- Define query keys as constants in `src/services/`
- Co-locate query/mutation definitions with their feature
- For route-owned data, prefer loader-driven prefetch + Query cache hydration over duplicate component-level requests
- Handle loading, error, and empty states explicitly in every data-dependent component

---

## Error Handling

- Wrap route trees and feature boundaries in `ErrorBoundary` components
- TanStack Router supports `errorComponent` per route — use it
- Never swallow errors silently; always log or surface them

---

## Styling

- Use a consistent approach project-wide (Tailwind CSS recommended with the stack)
- Avoid inline styles except for dynamic values
- Use CSS variables / design tokens for colors, spacing, typography
- Keep className logic clean with `clsx` or `tailwind-merge`

---

## Code Quality

- ESLint + `eslint-plugin-react-hooks` + `eslint-plugin-react-compiler` (React 19)
- Prettier for formatting
- Husky + lint-staged for pre-commit checks
- No `console.log` in production code — use a logger utility

---

## Sub-Skills

For specific topics, refer to the dedicated skill files:

| Topic | File |
|---|---|
| React performance and architecture | `.agents\skills\react-best-practices\SKILL.md` |
| Writing tests | `.agents\skills\test-writer\SKILL.md` |
| Frontend design direction | `.agents\skills\frontend-design\SKILL.md` |
