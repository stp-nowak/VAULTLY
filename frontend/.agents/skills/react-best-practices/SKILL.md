---
name: react-best-practices
description: Performance and scalability guide for React 19 + Vite applications using TanStack Router and TanStack Query. Use when reviewing architecture, reducing bundle cost, or eliminating render and data-fetching bottlenecks.
---

# React Best Practices

This skill is for the documented frontend stack:

- **React 19**
- **TypeScript**
- **Vite**
- **TanStack Router**
- **TanStack Query**

Use it to review or implement frontend code that should stay fast, predictable, and scalable as the app grows.

## When to use this skill

**Use React Best Practices when:**
- Designing feature architecture for new React work
- Reviewing code for scalability or performance regressions
- Refactoring components, hooks, routes, or data-fetching logic
- Debugging slow renders, route transitions, or request waterfalls
- Reducing bundle size and avoiding unnecessary global state

**Key areas covered:**
- **Feature boundaries**: Keep code local and promote only when genuinely shared
- **Route-driven data**: Use TanStack Router loaders to avoid waterfalls
- **Server state ownership**: Let TanStack Query own async data and caching
- **Render hygiene**: Prevent avoidable re-renders without premature memoization
- **Bundle discipline**: Split by route and lazily load heavy code paths
- **List and interaction performance**: Virtualize large lists and defer non-urgent updates

## Quick reference

### Highest-value rules

1. **Eliminate waterfalls first** - parallelize independent work and fetch data before render when possible
2. **Keep server state out of client state** - use TanStack Query instead of duplicating API data in stores
3. **Keep state local** - global state is expensive; use it only for truly app-wide client concerns
4. **Split by route and feature** - lazy-load heavy routes, editors, charts, and admin-only code
5. **Profile before memoizing** - use `useMemo`, `useCallback`, and `React.memo` only where they pay for themselves

### Common patterns

**Route loader + Query cache prefetch:**
```typescript
const userQueryOptions = (userId: string) =>
  queryOptions({
    queryKey: ['users', userId],
    queryFn: () => fetchUser(userId),
  });

export const Route = createFileRoute('/users/$userId')({
  loader: ({ context, params }) =>
    context.queryClient.ensureQueryData(userQueryOptions(params.userId)),
  component: UserPage,
});
```

**Parallel async work:**
```typescript
const [profile, permissions, auditLog] = await Promise.all([
  fetchProfile(),
  fetchPermissions(),
  fetchAuditLog(),
]);
```

**Lazy-load heavy UI:**
```tsx
const ChartPanel = React.lazy(() => import('./ChartPanel'));

export function AnalyticsPage() {
  return (
    <Suspense fallback={<PageSkeleton />}>
      <ChartPanel />
    </Suspense>
  );
}
```

## Architecture rules

### 1. Organize by feature
- Co-locate components, hooks, query options, tests, and small utilities inside each feature
- Keep generic primitives in shared folders only when multiple features truly reuse them
- Avoid "everything in `components/`" growth that turns the app into a junk drawer

### 2. Keep responsibilities separate
- Components render UI and wire interactions
- Custom hooks encapsulate reusable stateful behavior
- Query options and API functions own server data access
- Pure utilities stay framework-agnostic and easy to test

### 3. Choose the smallest state scope that works
- `useState` / `useReducer` for local UI state
- TanStack Router search params for shareable URL state
- TanStack Query for server state
- Redux Toolkit or Context only for truly global client-side concerns

## Data-fetching rules

### 1. Prefer route-owned preloading
- Fetch route-critical data in loaders to avoid render-then-fetch waterfalls
- When possible, use the loader to warm the TanStack Query cache
- Keep a single source of truth for query keys and query functions

### 2. Do not duplicate server state
- Do not copy query data into Redux, Context, or local state just to "store it"
- Derive view state from query results instead of syncing multiple sources
- Use query invalidation or optimistic updates instead of manual mirrors

### 3. Design query keys carefully
- Use stable, structured query keys
- Include the parameters that actually change the data
- Keep query options close to the feature that owns them

## Render and interaction rules

### 1. Minimize unnecessary re-renders
- Keep state as close as possible to where it is used
- Split large components when unrelated parts re-render together
- Memoize only when props are stable and the component is expensive enough to justify it
- Avoid passing freshly created objects or callbacks through memoized boundaries on hot paths

### 2. Use effects sparingly
- Effects are for syncing with external systems, not for deriving values you can compute during render
- Prefer derived values, event handlers, and route/query APIs over effect-driven orchestration
- Keep dependency arrays accurate and narrow

### 3. Defer non-urgent updates
- Use transitions for heavy filtering, sorting, or route-adjacent UI work that should not block typing
- Debounce user input only when the product behavior calls for it, not as a blanket performance fix

## Bundle and loading rules

### 1. Lazy-load what is not needed immediately
- Route-level code splitting is the default
- Lazy-load rich text editors, charting libraries, image tooling, and admin-only modules
- Load analytics and non-critical integrations outside the critical render path

### 2. Be careful with imports
- Prefer libraries with good tree-shaking characteristics
- Avoid accidental "import the whole world" patterns from large utility or icon packages
- Be skeptical of barrel files in very large folders when they inflate bundles or blur ownership

### 3. Render less for long collections
- Virtualize long lists with `@tanstack/react-virtual`
- Paginate or window large datasets instead of rendering everything
- Use `content-visibility` and image lazy loading where appropriate

## Practical review checklist

- Is the feature organized by ownership rather than by vague shared folders?
- Is server data fetched once and owned by TanStack Query?
- Does route-critical data load before the screen tries to render it?
- Is state local unless it must be global?
- Are memoization tools used intentionally rather than everywhere?
- Are heavy components and libraries lazily loaded?
- Are long lists virtualized?
- Are loading, error, and empty states explicit?

## Common pitfalls to avoid

❌ **Do not:**
- Write loader logic and component query logic that fetch the same resource twice
- Mirror server state into Redux or Context "just in case"
- Use effects to derive data that can be computed during render
- Wrap everything in `useMemo` or `useCallback` without evidence
- Keep giant page components that own unrelated concerns
- Render hundreds of rows without virtualization or pagination

✅ **Do:**
- Fetch early, in parallel, and with clear ownership
- Keep feature logic close to the feature
- Profile before optimizing
- Ship explicit UX states for asynchronous screens
- Prefer simple code until measurement shows a real bottleneck

## Resources

- [React Documentation](https://react.dev)
- [TanStack Router Documentation](https://tanstack.com/router)
- [TanStack Query Documentation](https://tanstack.com/query)
