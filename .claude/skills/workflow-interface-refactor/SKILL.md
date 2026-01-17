---
name: interface-refactor
description: Use when refactoring classes to use interfaces for testability - audit tests BEFORE changing signatures
---

# Interface Refactoring

## Overview

Guide agents through interface consolidation refactoring safely, ensuring test compatibility is addressed **before** making production code changes.

**Core principle:** Audit all consumers (classes AND tests) BEFORE changing constructor signatures or interface methods.

## Use when

**Use this skill when:**
- Refactoring classes from concrete dependencies to interface dependencies
- Adding methods to interfaces that tests mock
- Changing constructor signatures that test mocks depend on
- Consolidating multiple interfaces into a single interface

**Don't use when:**
- Simple renaming (use IDE refactor tools)
- Adding new methods with no existing test coverage
- Internal implementation changes that don't affect signatures

## Process

### 1. Audit Test Impact FIRST

Before ANY code changes, find all test mocks:

```bash
# Find mock setups for the class/interface
grep -r "Mock<.*ClassName>" tests/ --include="*.cs"
grep -r "Substitute.For<ClassName>" tests/ --include="*.cs"
grep -r "Substitute.For<IInterfaceName>" tests/ --include="*.cs"

# Find constructor calls in tests
grep -r "new ClassName(" tests/ --include="*.cs"
```

Document which tests need updates.

### 2. Interface Change Checklist

When adding/modifying methods to interfaces:

- [ ] All implementing classes updated
- [ ] All test mocks updated to new signature
- [ ] All captured argument assertions updated
- [ ] Documentation updated if method is public

### 3. Update Mocks BEFORE Running Tests

When changing from concrete class to interface:

```csharp
// Before: Mocking concrete class (fragile)
var mock = Substitute.For<ConcreteSession>();

// After: Mocking interface (testable)
var mock = Substitute.For<ISession>();
```

When method signatures change:

```csharp
// Before: 3-arg method
mock.SendAsync(Arg.Any<byte[]>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
    .Returns(response);

// After: 2-arg method (simplified)
mock.SendAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
    .Returns(response);
```

### 4. Handle Captured Argument Assertions

If tests capture arguments for assertions, update parsing:

```csharp
// Before: Direct capture
mock.SendAsync(Arg.Do<byte[]>(x => captured = x), ...);
Assert.Equal(expected, captured);

// After: May need offset adjustment
mock.SendAsync(Arg.Do<ReadOnlyMemory<byte>>(x => captured = x.ToArray()), ...);
// If format changed (e.g., command byte prepended):
Assert.Equal(expected, captured.AsSpan(1).ToArray()); // Skip command byte
```

### 5. Run Tests Incrementally

After each batch of mock updates:

```bash
dotnet build.cs test --filter "FullyQualifiedName~<TestClassName>"
```

Track failure count: should decrease with each fix batch.

## Example: Concrete → Interface Refactor

**Scenario:** Refactor `AuthenticatorConfig` from `FidoSession` to `IFidoSession`

**Step 1: Audit**
```bash
grep -r "new AuthenticatorConfig" tests/ --include="*.cs"
# Found: AuthenticatorConfigTests.cs creates with FidoSession mock
```

**Step 2: Find mock pattern**
```csharp
// Current: Mocking sealed class (NSubstitute can't do this easily)
var session = Substitute.For<FidoSession>(...); // ❌ Fails - sealed class
```

**Step 3: Update production code**
```csharp
// Before
public class AuthenticatorConfig(FidoSession session, ...)

// After
public class AuthenticatorConfig(IFidoSession session, ...)
```

**Step 4: Update test mocks**
```csharp
// After
var session = Substitute.For<IFidoSession>(); // ✅ Works - interface
session.SendCborRequestAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
    .Returns(mockResponse);
```

**Step 5: Verify**
```bash
dotnet build.cs test --filter "FullyQualifiedName~AuthenticatorConfigTests"
```

## Common Mistakes

**❌ Change code first, fix tests reactively**
Multiple test failure cycles, wasted iterations

**✅ Audit tests first, batch mock updates**
One-shot fix, minimal iterations

**❌ Assume sealed classes can be mocked**
NSubstitute/Moq can't mock sealed classes without parameterless constructors

**✅ Use interfaces for testability**
Design classes to accept interface dependencies

**❌ Ignore captured argument format changes**
Tests pass but assertions are wrong

**✅ Verify assertion logic still valid**
Check if payload format changed (e.g., prepended command bytes)

## Verification

Before claiming refactor complete:

- [ ] All tests pass (not just compile)
- [ ] No test count decreased (didn't accidentally skip)
- [ ] Mocks use interface, not concrete class
- [ ] Captured argument assertions validated

## Related Skills

- `tdd` - For new features requiring tests
- `debug` - If test failures persist after mock updates
