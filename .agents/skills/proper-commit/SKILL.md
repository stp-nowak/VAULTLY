---
name: proper-commit
description: >
  Write commit messages that match this repository's conventions. Use this skill whenever preparing
  a git commit, amending a commit message, or reviewing whether a proposed commit message is ready
  to use for this repo.
---

# Proper Commit Skill

Use this skill whenever you are about to create or amend a git commit in this repository.

The goal is not just to make the message "good enough". The goal is to make it match the commit
history style that already exists in this repo.

## Repository commit convention

Use this repository commit format:

```text
<type>: <short summary>

<one or more sentences summarizing the change>
```

## Rules

### 1. Use a conventional prefix

Start the subject with a lowercase conventional commit type followed by a colon:

- `feat:` for new functionality
- `fix:` for bug fixes
- `docs:` for documentation or guidance changes
- `chore:` for scaffolding, maintenance, or repository housekeeping
- `refactor:` for internal restructuring without behavior change
- `test:` for test-only changes

Pick the narrowest accurate type. Do not use a generic subject without a prefix if the repo already
has an established conventional format.

### 2. Add an issue reference only when the work is actually linked to an issue

When the work is explicitly linked to a tracked issue, include the issue number at the end of the
subject:

```text
docs: add frontend agent guidance and skills (#3)
```

If the work is not linked to an issue, do not invent one and do not add an issue reference.

Good:

```text
docs: add proper commit guidance skill
docs: add frontend agent guidance and skills (#3)
```

### 3. Keep the subject concise and specific

- Use sentence-style lowercase wording after the prefix
- Describe the outcome, not the implementation mechanics
- Avoid trailing punctuation
- Prefer a concrete scope such as `frontend`, `backend`, `identity service`, or `agent guidance`

Good:

```text
feat: add identity service with Google OAuth (#2)
docs: add frontend agent guidance and skills (#3)
chore: scaffold backend solution (#2)
```

Less good:

```text
updated files
misc changes
Add some stuff for auth
```

### 4. Add a meaningful body

After the subject, add a short body that explains what changed. Usually one short paragraph is
enough. Focus on the user-visible or repo-relevant outcome:

- what was added
- what was documented
- what area of the codebase changed
- what capability is now available

The body should read like release-quality summary text, not like raw patch notes.

### 5. Do not duplicate issue references

If the subject already includes `(#3)`, do not repeat the same issue number again in the body.

If the commit is not linked to an issue, do not add any issue reference at all.

### 6. Never include a Copilot co-author trailer by default

Do not add `Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>`.

## Branch naming

Name the branch to match the type of work.

- Use `docs/<short-kebab-description>` for documentation or guidance work
- Use `feature/<short-kebab-description>` for actual new product or engineering features
- Use `fix/<short-kebab-description>` for bug fixes
- Use `chore/<short-kebab-description>` for maintenance work

Only include an issue number in the branch name when the team explicitly wants issue-linked branch
names. If the work is not linked to an issue, do not add `issue-<n>` to the branch name.

Good:

```text
docs/proper-commit-guidance
feature/google-oauth-login
fix/token-refresh-loop
```

Avoid:

```text
feature/issue-3-add-proper-commit-skill
```

for untracked documentation work.

## Commit creation checklist

Before creating a commit:

1. Review the staged changes and confirm they form one coherent unit
2. Choose the correct conventional type
3. Decide whether the work is actually linked to an issue
4. Write a short, specific subject, adding `(#<issue>)` only when there is a real linked issue
5. Add a body summarizing the change
6. Do not repeat the same issue reference in the body
7. Do not add a Copilot co-author trailer unless explicitly requested

## Example

```text
docs: add commit-writing repo skill

Add a repository-level Copilot skill that documents the project's commit message format and links
to it from the root AGENTS guide.
```

## When to pause and ask

Ask for clarification before committing if:

- the staged changes mix unrelated concerns and should probably be split
- there is no clear issue number to reference
- multiple commit types could apply and the choice affects project history meaningfully

If the intent is clear, do not over-ask. Pick the best fitting conventional type and write the
commit in the repository's established format.
