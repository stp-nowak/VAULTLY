---
name: pr-push
description: >
  Push the current branch and open a GitHub pull request with a before/after description and, when the UI
  changed, screenshots of the new views. Use whenever the user asks to "create a PR", "open a pull request",
  "push and make a PR", "pr-push", or finishes work that should go up for review.
---

# PR Push Skill

Creates a pull request that a reviewer can understand without opening the diff: what the system looked like
before, what it looks like after, how to verify it, and — for UI changes — screenshots of every new or changed view.

Run the steps in order. Do not skip verification or screenshots to save time.

## 1. Preconditions

1. `gh auth status` must show a logged-in account. If not, ask the user to run `! gh auth login` and stop.
2. Working tree must be clean or contain only changes that belong to this PR. Never sweep unrelated files in.
3. The work must be linked to an issue. Find it from the branch name (`feature/issue-<n>-...`) or ask.
   If there is no issue yet and the user wants one, create it first (`gh issue create --label enhancement`).
4. Commits follow `./.agents/skills/proper-commit/SKILL.md`. Commit any remaining changes with that skill.

## 2. Verify every touched project

Detect touched projects with `git diff --name-only <base>...HEAD` and run their checks (the same ones CI runs):

| Path | Commands (from that directory) |
|---|---|
| `packages/ui/**` | from repo root: `pnpm install --frozen-lockfile`, `pnpm --filter @vaultly/ui lint`, `… typecheck`, `… test` |
| `web/src/web/**` (or `packages/ui/**`) | from repo root: `pnpm --filter @vaultly/web lint`, `… test`, `… build` |
| `web/src/api/**` | `dotnet build Vaultly.Web.Api.slnx`, `dotnet test Vaultly.Web.Api.slnx` |
| `auth/**` | from `auth/src`: `dotnet build Vaultly.Auth.slnx`, `dotnet test Vaultly.Auth.slnx` |
| `backend/**` | from `backend/src`: `dotnet build`, `dotnet test` |

If anything fails, fix it (or stop and report). Record the results — they go into the PR description.
`pnpm` may not be on PATH on Windows; use `npx --yes pnpm@10 <command>`.

## 3. Choose the base branch

- Default base is `develop` (all previous PRs target it).
- If the branch is stacked on another unmerged branch (`git log --oneline origin/develop..HEAD` shows commits
  from another issue), target that branch instead so the diff contains only this PR's work, and say so in the
  description ("Stacked on #<n>; retarget to develop after it merges").

## 4. Screenshots (mandatory when the UI changed)

UI changed = any change under `web/src/web/src/**` or `packages/ui/**` that affects rendered output (`.tsx`,
styles, tokens, routes, copy). Capture **every new or changed view**, including meaningful states (empty, error, loading if visible,
authenticated vs. not). Desktop is required; add mobile (390×844) when layout changed.

1. Build and serve the production SPA (no dev tools overlay), from the repo root:
   `npx --yes pnpm@10 --filter @vaultly/web build && npx --yes pnpm@10 --filter @vaultly/web preview --port 4173 --strictPort` (background).
2. Install the capture tool outside the repo (do not add it to `package.json`):
   `npm install --prefix <scratchpad>/pr-shots playwright-core@1.49.1`
   and copy `scripts/capture-screenshots.mjs` from this skill into `<scratchpad>/pr-shots/`.
   It drives the locally installed Edge/Chrome (`channel: "msedge"` or `"chrome"`), no browser download.
3. Write a scenario file (see `scripts/scenarios.example.json`). The SPA talks only to `/api/*`, so mock those
   calls per scenario with realistic data — never real user data or real tokens.
4. Run `node <scratchpad>/pr-shots/capture-screenshots.mjs <scenarios.json>` and **open every PNG with Read to
   check it** (correct view, no error overlay, no blank page). Re-shoot if anything is off.
5. Publish images to the orphan branch `pr-assets` (keeps binaries out of the code history):

   ```bash
   BRANCH=$(git branch --show-current)
   WT=<scratchpad>/pr-assets
   git fetch origin pr-assets 2>/dev/null \
     && git worktree add "$WT" -B pr-assets origin/pr-assets \
     || { git worktree add --detach "$WT" && git -C "$WT" switch --orphan pr-assets; }
   mkdir -p "$WT/$BRANCH" && cp <shots>/*.png "$WT/$BRANCH/"
   git -C "$WT" add "$BRANCH" && git -C "$WT" commit -m "chore: add screenshots for $BRANCH"
   git -C "$WT" push origin pr-assets && git worktree remove "$WT"
   ```

   Embed as `![Login](https://github.com/<owner>/<repo>/blob/pr-assets/<branch>/<file>.png?raw=true)`
   (`gh repo view --json nameWithOwner -q .nameWithOwner` gives `<owner>/<repo>`).

If the UI did not change, write "No UI changes" in the Screenshots section — do not delete the section.

## 5. Write the description

Use this template (write to a temp file, pass with `--body-file`). Write for a reviewer who knows the repo but
not this change. Be concrete: name folders, endpoints, components. Keep "Before" and "After" parallel so each
row reads as a direct comparison.

```markdown
## Summary
<2–4 sentences: what and why.>

Closes #<issue>   <!-- or "Part of #<issue>" -->
<Stacked on #<n> / branch `...`; retarget to `develop` after it merges.>   <!-- only when stacked -->

## Before → After
| Area | Before | After |
|---|---|---|
| <area> | <how it worked> | <how it works now> |

## Changes by project
### `web/src/web` (SPA)
- ...
### `web/src/api` (web API)
- ...
### `auth`
- ...
### `backend`
- ...
### CI / tooling / docs
- ...
<!-- omit projects that were not touched -->

## Screenshots
| View | Screenshot |
|---|---|
| <view, state> | ![<view>](<url>) |
<!-- or: No UI changes. -->

## How to test
1. <exact commands / URLs / steps a reviewer can follow>

## Verification
- [x] `<project>`: <command> — <result, e.g. 46/46 tests>

## Notes / follow-ups
- <known limitations, breaking changes, config changes, migrations>
```

## 6. Push and open the PR

```bash
git push -u origin "$(git branch --show-current)"
gh pr create --base <base> --head "$(git branch --show-current)" \
  --title "<proper-commit style subject, e.g. feat: ... (#6)>" \
  --body-file <scratchpad>/pr-body.md --assignee @me --label <same labels as the issue>
```

End the body with the attribution line required by the session (if any).

## 7. Link the PR to the issue

The issue must show that a PR exists for it. Do all of the following:

1. **Closing keyword** in the PR body: `Closes #<issue>` (already in the template). GitHub links it in the issue's
   *Development* sidebar and closes the issue on merge — **only when the PR targets the default branch** (`main`).
2. Because PRs here target `develop` (or a stacked branch), also **comment on the issue** with the PR link; the
   reference appears in the issue timeline:
   `gh issue comment <issue> --body "Pull request: #<pr> — <one-line summary>"`
3. Reference the issue in the PR title (`(#<issue>)`, from proper-commit) and copy the issue's labels to the PR.
4. Check: `gh issue view <issue> --comments` shows the comment, and `gh pr view <pr> --json body` contains `Closes #<issue>`.
   If the user wants the *Development* sidebar entry for a non-default base, tell them to link it manually in the
   issue sidebar (Development → Link a pull request); there is no stable CLI for that.

Then report the PR URL to the user, plus anything that needs their attention (stacked base, failing checks,
open questions).

## Rules
- Never push to `develop`/`main` directly and never force-push someone else's branch.
- Never put secrets, tokens, cookies or real personal data in descriptions or screenshots.
- Screenshots must come from the code in this PR (build the current branch), not from mocks drawn by hand.
- If the PR is updated later, update the description and re-shoot changed views (`gh pr edit --body-file`).
