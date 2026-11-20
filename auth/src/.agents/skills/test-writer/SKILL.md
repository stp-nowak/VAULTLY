---
name: test-writer
description: Write high-quality NUnit tests for C# codebases. Use when Codex needs to create, expand, refactor, or review NUnit test suites, convert tests to NUnit, or design test cases for C# classes, services, or APIs.
---

# Test Writer

## Overview
Provide a repeatable workflow for producing clear, deterministic NUnit tests in C# that match the existing project style and maximize meaningful coverage.

## Quick Start
1. Inspect target code and any existing tests for conventions (names, helpers, mocking libraries).
2. Identify behaviors, inputs, outputs, exceptions, and side effects to test.
3. Choose test scope (unit vs integration) and isolation strategy.
4. Implement tests with NUnit attributes and Assert.That constraints.
5. Review for determinism, readability, and brittleness.

## Workflow
### 1) Scan the project
- Locate test projects and existing NUnit usage.
- Note naming patterns, fixture structure, and helper utilities.
- Detect mocking libraries or test data builders already in use.

### 2) Build a behavior matrix
- List happy path scenarios.
- Add boundary values and edge cases.
- Add invalid input and exception paths.
- For stateful code, include idempotency and state transition checks.

### 3) Choose test structure
- Use [TestFixture] if the project uses it.
- Prefer [TestCase] or [TestCaseSource] for data variations.
- Use [SetUp] and [TearDown] for shared setup, avoid shared mutable state.

### 4) Implement tests
- Use Arrange / Act / Assert structure, with comments only if clarity needs it.
- Prefer Assert.That with constraint syntax over legacy asserts.
- Use Assert.Throws or Assert.ThrowsAsync for exception paths.
- For async code, await the action inside the assertion.

### 5) Review quality
- Keep tests deterministic: avoid time, randomness, environment, or real IO in unit tests.
- Reduce brittleness: avoid strict string matching when structure or equivalence works.
- Make test names communicate behavior and expected outcome.

## Reference
Use `references/nunit-guide.md` for attributes, constraint examples, and common patterns.

## Output expectations
- Provide ready-to-paste C# test code with required using directives.
- State assumptions about frameworks or helpers when not explicit in the codebase.
- If something blocks correctness (missing types, unclear behavior), call it out.
