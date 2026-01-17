---
name: tdd
description: Use when implementing features or fixes - write failing test first, then minimal code to pass
---

# Test-Driven Development (TDD)

## Overview

Write the test first. Watch it fail. Write minimal code to pass.

**Core principle:** If you didn't watch the test fail, you don't know if it tests the right thing.

**Violating the letter of the rules is violating the spirit of the rules.**

## Use when

**Always:**
- New features
- Bug fixes
- Refactoring
- Behavior changes

**Exceptions (ask your human partner):**
- Throwaway prototypes
- Generated code
- Configuration files

Thinking "skip TDD just this once"? Stop. That's rationalization.

## The Iron Law

```
NO PRODUCTION CODE WITHOUT A FAILING TEST FIRST
```

Write code before the test? Delete it. Start over.

**No exceptions:**
- Don't keep it as "reference"
- Don't "adapt" it while writing tests
- Don't look at it
- Delete means delete

Implement fresh from tests. Period.

## Red-Green-Refactor Cycle

### RED - Write Failing Test

Write one minimal test showing what should happen.

**Good example (C#):**
```csharp
[Fact]
public void RetryOperation_RetriesThreeTimes_WhenOperationFails()
{
    int attempts = 0;
    var operation = () => {
        attempts++;
        if (attempts < 3) throw new InvalidOperationException("fail");
        return "success";
    };

    var result = RetryOperation(operation);

    Assert.Equal("success", result);
    Assert.Equal(3, attempts);
}
```
Clear name, tests real behavior, one thing.

**Requirements:**
- One behavior
- Clear name
- Real code (no mocks unless unavoidable)

### Verify RED - Watch It Fail

**MANDATORY. Never skip.**

```bash
dotnet test --filter "FullyQualifiedName~RetryOperation"
```

Confirm:
- Test fails (not errors)
- Failure message is expected
- Fails because feature missing (not typos)

**Test passes?** You're testing existing behavior. Fix test.

### GREEN - Minimal Code

Write simplest code to pass the test.

```csharp
public static T RetryOperation<T>(Func<T> fn)
{
    for (int i = 0; i < 3; i++)
    {
        try { return fn(); }
        catch when (i < 2) { }
    }
    throw new InvalidOperationException("unreachable");
}
```
Just enough to pass. Don't add features, refactor other code, or "improve" beyond the test.

### Verify GREEN - Watch It Pass

**MANDATORY.**

```bash
dotnet test --filter "FullyQualifiedName~RetryOperation"
```

Confirm:
- Test passes
- Other tests still pass
- Output pristine (no errors, warnings)

### REFACTOR - Clean Up

After green only:
- Remove duplication
- Improve names
- Extract helpers

Keep tests green. Don't add behavior.

## Common Rationalizations

| Excuse | Reality |
|--------|---------|
| "Too simple to test" | Simple code breaks. Test takes 30 seconds. |
| "I'll test after" | Tests passing immediately prove nothing. |
| "Already manually tested" | Ad-hoc ≠ systematic. No record, can't re-run. |
| "Deleting X hours is wasteful" | Sunk cost fallacy. Keeping unverified code is technical debt. |
| "TDD will slow me down" | TDD faster than debugging. Pragmatic = test-first. |

## Red Flags - STOP and Start Over

- Code before test
- Test after implementation
- Test passes immediately
- Can't explain why test failed
- "I already manually tested it"
- "This is different because..."

**All of these mean: Delete code. Start over with TDD.**

## Example: Bug Fix

**Bug:** Empty email accepted

**RED**
```csharp
[Fact]
public void SubmitForm_RejectsEmptyEmail()
{
    var result = SubmitForm(new FormData { Email = "" });
    Assert.Equal("Email required", result.Error);
}
```

**Verify RED**
```bash
$ dotnet test
FAIL: expected 'Email required', got null
```

**GREEN**
```csharp
public FormResult SubmitForm(FormData data)
{
    if (string.IsNullOrWhiteSpace(data.Email))
        return new FormResult { Error = "Email required" };
    // ...
}
```

**Verify GREEN**
```bash
$ dotnet test
PASS
```

## Verification Checklist

Before marking work complete:

- [ ] Every new function/method has a test
- [ ] Watched each test fail before implementing
- [ ] Each test failed for expected reason (feature missing, not typo)
- [ ] Wrote minimal code to pass each test
- [ ] All tests pass
- [ ] Output pristine (no errors, warnings)
- [ ] Edge cases and errors covered

Can't check all boxes? You skipped TDD. Start over.

## Final Rule

```
Production code → test exists and failed first
Otherwise → not TDD
```

No exceptions without your human partner's permission.
