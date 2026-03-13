---
name: test-writer
description: Testing guidance for React 19 + Vite applications using Vitest, React Testing Library, TanStack Router, TanStack Query, and MSW. Use when writing or reviewing frontend tests.
---

# React Testing Skill

## Tools
- **Vitest** — test runner (integrates natively with Vite)
- **React Testing Library (RTL)** — component testing
- **@testing-library/user-event** — realistic user interactions
- **MSW (Mock Service Worker)** — API mocking

---

## Philosophy

- Test **behavior**, not implementation — assert what the user sees and does
- Avoid testing internal state, refs, or component method calls
- One test file per component/feature, co-located next to the source file (`*.test.tsx`)
- Prefer integration tests (component + hooks + utils together) over isolated unit tests
- Prefer **MSW** and real providers over mocking TanStack Query hooks or Router internals
- Create fresh test providers per test so cache, router state, and mocks do not bleed across cases

---

## What to Test

| Layer | What to test |
|---|---|
| Components | Renders correctly, responds to user interactions, shows correct states (loading/error/empty/success) |
| Custom hooks | Correct return values and state transitions (use `renderHook`) |
| Utils / services | Pure logic — unit test these directly |
| Routes | Navigation, loader data rendering, search params, guards, and error boundaries |

---

## Component Tests

```tsx
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { UserCard } from './UserCard';

describe('UserCard', () => {
  it('renders the user name', () => {
    render(<UserCard name="Alice" role="Admin" />);
    expect(screen.getByText('Alice')).toBeInTheDocument();
  });

  it('calls onDelete when delete button is clicked', async () => {
    const onDelete = vi.fn();
    render(<UserCard name="Alice" role="Admin" onDelete={onDelete} />);

    await userEvent.click(screen.getByRole('button', { name: /delete/i }));

    expect(onDelete).toHaveBeenCalledTimes(1);
  });
});
```

**Rules:**
- Query by **role** first (`getByRole`), then label, then text — never by class or test IDs unless unavoidable
- Use `userEvent` over `fireEvent` — it simulates real browser interactions
- Wrap async interactions in `await`
- Assert visible outcomes rather than implementation details like prop callbacks unless that callback is the behavior being exposed

---

## Mocking API Calls (MSW)

Define handlers in `src/mocks/handlers.ts`:

```ts
import { http, HttpResponse } from 'msw';

export const handlers = [
  http.get('/api/users', () => {
    return HttpResponse.json([{ id: 1, name: 'Alice' }]);
  }),
];
```

Set up the server in `src/mocks/server.ts`:

```ts
import { setupServer } from 'msw/node';
import { handlers } from './handlers';

export const server = setupServer(...handlers);
```

Configure globally in `vitest.setup.ts`:

```ts
import { server } from './src/mocks/server';

beforeAll(() => server.listen());
afterEach(() => server.resetHandlers());
afterAll(() => server.close());
```

Override handlers per test when needed:

```ts
server.use(
  http.get('/api/users', () => HttpResponse.json([], { status: 500 }))
);
```

---

## Testing Hooks

```tsx
import { renderHook, act } from '@testing-library/react';
import { useCounter } from './useCounter';

it('increments the counter', () => {
  const { result } = renderHook(() => useCounter());

  act(() => result.current.increment());

  expect(result.current.count).toBe(1);
});
```

---

## Testing with TanStack Router

Wrap components under test with the router context. Use `createMemoryHistory` + `RouterProvider` for isolated route tests:

```tsx
import { createRouter, RouterProvider, createMemoryHistory } from '@tanstack/react-router';
import { routeTree } from '../routeTree.gen';

function renderWithRouter(initialPath = '/') {
  const router = createRouter({
    routeTree,
    history: createMemoryHistory({ initialEntries: [initialPath] }),
  });
  return render(<RouterProvider router={router} />);
}
```

When a route depends on loader data, prefer exercising the route through the router instead of mocking the route component in isolation.

---

## Testing with TanStack Query

Wrap components with a fresh `QueryClient` per test to avoid state bleed:

```tsx
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

function renderWithQuery(ui: React.ReactElement) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>{ui}</QueryClientProvider>
  );
}
```

Do not share a singleton `QueryClient` across tests.

If you are testing a route that uses both TanStack Router and TanStack Query, build a single helper that wires both providers with a fresh `QueryClient` per test.

---

## Vitest Config

```ts
// vitest.config.ts
import { defineConfig } from 'vitest/config';

export default defineConfig({
  test: {
    environment: 'jsdom',
    globals: true,
    setupFiles: ['./vitest.setup.ts'],
  },
});
```

---

## Coverage

Run with `vitest --coverage` (uses `@vitest/coverage-v8`).

Focus coverage on:
- All non-trivial utility functions (aim for ~100%)
- Critical user flows in components
- Error/edge states

Don't chase 100% overall coverage — prioritize meaningful tests over metric padding.
