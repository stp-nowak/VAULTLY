# NUnit guide

## Attributes
- [TestFixture] for a test class.
- [Test] for a single test.
- [TestCase] for inline parameterized tests.
- [TestCaseSource] for complex data sets.
- [SetUp] and [TearDown] per test.
- [OneTimeSetUp] and [OneTimeTearDown] per fixture.
- [Category("name")] for grouping.
- [Explicit] for opt-in execution.
- [Parallelizable] when safe.

## Assert.That constraint patterns
- Equality: Assert.That(actual, Is.EqualTo(expected))
- Null: Assert.That(actual, Is.Null)
- Not null: Assert.That(actual, Is.Not.Null)
- Boolean: Assert.That(flag, Is.True)
- String: Assert.That(text, Does.Contain("sub"))
- Collections: Assert.That(list, Has.Count.EqualTo(3))
- Equivalence: Assert.That(actual, Is.EquivalentTo(expected))
- Approx: Assert.That(value, Is.EqualTo(expected).Within(0.001))

## Exceptions
- Sync: Assert.Throws<ArgumentException>(() => subject.Call(bad))
- Async: Assert.ThrowsAsync<ArgumentException>(async () => await subject.CallAsync(bad))

## Parameterized tests
- Use [TestCase] for small data sets.
- Use [TestCaseSource(nameof(Cases))] for complex objects.

Example:
```csharp
[TestCase(0, 0)]
[TestCase(2, 4)]
public void Square_ReturnsExpected(int input, int expected)
{
    var result = MathHelpers.Square(input);

    Assert.That(result, Is.EqualTo(expected));
}

static IEnumerable<TestCaseData> Cases()
{
    yield return new TestCaseData(new User("a"), true).SetName("Valid user");
    yield return new TestCaseData(new User(""), false).SetName("Empty name");
}
```

## Async patterns
- Prefer async Task test methods.
- Await the action inside the assertion or use ThrowsAsync.

Example:
```csharp
[Test]
public async Task SaveAsync_WhenInvalid_Throws()
{
    var ex = Assert.ThrowsAsync<ValidationException>(async () =>
        await subject.SaveAsync(new Item()));

    Assert.That(ex.Message, Does.Contain("invalid"));
}
```

## Test naming
- Use a consistent pattern already in the repo.
- Common patterns: Method_WhenCondition_ExpectedResult, Given_When_Then.
- Keep names specific and behavior focused.

## Determinism checklist
- Avoid DateTime.Now or random without control.
- Avoid real network, file system, or database in unit tests.
- Prefer seams and fakes for external dependencies.
- Use fixed culture or invariant parsing when relevant.

## Fixture hygiene
- Keep shared state immutable.
- Reset static state in [TearDown] if unavoidable.
- Avoid order dependency between tests.
