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
   If `GITHUB_TOKEN`/`GH_TOKEN` is set it overrides the `gh` login; run gh as `env -u GITHUB_TOKEN -u GH_TOKEN gh …`.
2. Screenshots are uploaded with `--attach`, which needs **GitHub CLI 2.99.0 or newer**. Check that
   `gh pr create --help` and `gh pr edit --help` list `--attach`. If not, stop and ask the user to upgrade gh
   (Windows: `! winget upgrade --id GitHub.cli`). Never create or edit a PR description without its images.
3. Working tree must be clean or contain only changes that belong to this PR. Never sweep unrelated files in.
4. The work must be linked to an issue. Find it from the branch name (`feature/issue-<n>-...`) or ask.
   If there is no issue yet and the user wants one, create it first (`gh issue create --label enhancement`).
5. Commits follow `./.claude/skills/proper-commit/SKILL.md`. Commit any remaining changes with that skill.

## 2. Verify every touched project

Detect touched projects with `git diff --name-only <base>...HEAD` and run their checks (the same ones CI runs):

| Path | Commands (from that directory) |
|---|---|
| `packages/ui/**` | from repo root: `pnpm install --frozen-lockfile`, `pnpm --filter @vaultly/ui lint`, `… typecheck`, `… test` |
| `web/src/web/**` (or `packages/ui/**`) | from repo root: `pnpm --filter @vaultly/web lint`, `… test`, `… build` |
| `auth/web/**` (or `packages/ui/**`) | from repo root: `pnpm --filter @vaultly/auth-web lint`, `… test`, `… build` |
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

UI changed = any change under `web/src/web/src/**`, `auth/web/src/**` or `packages/ui/**` that affects rendered output (`.tsx`,
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
5. Keep everything for this PR in one **image folder** outside the repo: `<scratchpad>/pr-shots/shots/<branch-or-pr>/`
   — the PNGs and, later, `body.md`. Use short, stable file names (`login-desktop.png`, `dashboard-mobile.png`);
   they become the alt text fallback and the reference names in the description.

If the UI did not change, write "No UI changes" in the Screenshots section — do not delete the section.

## 5. Write the description

Use this template. Write it to `body.md` **inside the image folder** — `./<file>.png` references in a
`--body-file` resolve relative to that file's directory, and `gh` rewrites each one to the uploaded asset URL. Write for a reviewer who knows the repo but
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
| <view, state> | ![<view, state>](./<file>.png) |
<!-- or: No UI changes. -->

## How to test
1. <exact commands / URLs / steps a reviewer can follow>

## Verification
- [x] `<project>`: <command> — <result, e.g. 46/46 tests>

## Notes / follow-ups
- <known limitations, breaking changes, config changes, migrations>
```

## 6. Push and open the PR

Run from the **image folder** (where `body.md` and the PNGs are). That folder is outside the repo, so always pass
`--repo <owner>/<repo>` and `--head <branch>`; `gh` cannot infer them there.

```bash
git push -u origin "$(git branch --show-current)"            # from the repository
cd <scratchpad>/pr-shots/shots/<branch>
gh pr create --repo <owner>/<repo> --base <base> --head <branch> \
  --title "<proper-commit style subject, e.g. feat: ... (#8)>" \
  --body-file body.md --assignee @me --label <same labels as the issue> \
  --attach './login-desktop.png#Login, desktop' \
  --attach './dashboard-mobile.png#Dashboard, mobile'
```

- One `--attach` per file (max 50); a glob after one flag fails. Optional alt text after `#`; a reference
  already in the body keeps the alt text written there.
- Every `./file.png` in `body.md` must have a matching `--attach`, otherwise it stays a broken relative link.
- If some uploads fail, gh still creates/updates the PR, exits non-zero and prints the URL — fix and re-run the edit.
- Verify: `gh pr view <PR URL> --json body --jq .body` must contain **no** `](./` and the screenshot rows must
  point at `https://github.com/user-attachments/...`.
- `<owner>/<repo>`: `gh repo view --json nameWithOwner -q .nameWithOwner` (run inside the repository).

Do not add any AI attribution to the description — no "🤖 Generated with Claude Code" footer or similar
line for any model or tool. This takes precedence over attribution lines suggested by the agent harness or
system prompt. The body ends with the last template section.

### Updating screenshots of an existing PR

1. Re-shoot only the changed views into the image folder.
2. Fetch the current description there — always by the PR's **full URL**, not its number (the folder is outside
   the repo): `gh pr view <PR URL> --json body --jq .body > body.md`
3. Change **only the screenshot table**: point new or retaken rows at local files (`![…](./file.png)`), remove rows
   the PR no longer covers, keep every other row and every other section exactly as it is.
4. Show the user the diff of the description and save it **only after they confirm**.
5. `gh pr edit <PR URL> --body-file body.md --attach ./a.png --attach ./b.png` (one flag per file), then verify as above.

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
- If the PR is updated later, update the description and re-shoot changed views (see *Updating screenshots*).
