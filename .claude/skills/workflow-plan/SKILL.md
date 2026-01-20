---
name: write-plan
description: Use when you have specs or requirements - creates implementation plan before coding
---

# Writing Plans

## Use when

- You have validated requirements (from brainstorming or spec)
- Need to break down a multi-step task into bite-sized pieces
- Want to hand off implementation to another agent or session
- Preparing for TDD-based implementation

## Overview

Write comprehensive implementation plans assuming the engineer has zero context for our codebase and questionable taste. Document everything they need to know: which files to touch for each task, code, testing, docs they might need to check, how to test it. Give them the whole plan as bite-sized tasks. DRY. YAGNI. TDD. Frequent commits.

Assume they are a skilled developer, but know almost nothing about our toolset or problem domain. Assume they don't know good test design very well.

**Context:** This should be run after brainstorming has produced a validated design.

**Research:** Use Context7 MCP to look up any library/API documentation, code patterns, or framework usage needed to understand the implementation requirements.

**Save plans to:** `docs/plans/YYYY-MM-DD-<feature-name>.md`

## Bite-Sized Task Granularity

**Each step is one action (2-5 minutes):**
- "Write the failing test" - step
- "Run it to make sure it fails" - step
- "Implement the minimal code to make the test pass" - step
- "Run the tests and make sure they pass" - step
- "Commit" - step

## Plan Document Header

**Every plan MUST start with this header:**

```markdown
# [Feature Name] Implementation Plan

**Goal:** [One sentence describing what this builds]

**Architecture:** [2-3 sentences about approach]

**Tech Stack:** [Key technologies/libraries]

---
```

## Task Structure

```markdown
### Task N: [Component Name]

**Files:**
- Create: `exact/path/to/file.cs`
- Modify: `exact/path/to/existing.cs:123-145`
- Test: `tests/exact/path/to/test.cs`

**Step 1: Write the failing test**

```csharp
[Fact]
public void SpecificBehavior_ExpectedResult()
{
    var result = Function(input);
    Assert.Equal(expected, result);
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test --filter "FullyQualifiedName~SpecificBehavior"`
Expected: FAIL with "function not defined"

**Step 3: Write minimal implementation**

```csharp
public Result Function(Input input)
{
    return expected;
}
```

**Step 4: Run test to verify it passes**

Run: `dotnet test --filter "FullyQualifiedName~SpecificBehavior"`
Expected: PASS

**Step 5: Commit**

```bash
git add tests/path/test.cs src/path/file.cs
git commit -m "feat: add specific feature"
```
```

## Remember

- Exact file paths always
- Complete code in plan (not "add validation")
- Exact commands with expected output
- Reference relevant skills
- DRY, YAGNI, TDD, frequent commits

## .NET-Specific Patterns

**Test commands:**
```bash
# Run specific test
dotnet test --filter "FullyQualifiedName~TestMethodName"

# Run all tests in a class
dotnet test --filter "FullyQualifiedName~TestClassName"

# Run all tests in a project
dotnet test Yubico.YubiKit.UnitTests/Yubico.YubiKit.UnitTests.csproj
```

**Build commands:**
```bash
# Build solution
dotnet build Yubico.YubiKit.sln

# Build specific project
dotnet build Yubico.YubiKit.Piv/Yubico.YubiKit.Piv.csproj
```

**Commit message conventions:**
- `feat:` - New feature
- `fix:` - Bug fix
- `refactor:` - Code restructuring
- `test:` - Adding/modifying tests
- `docs:` - Documentation changes

## Execution Handoff

After saving the plan, offer execution choice:

**"Plan complete and saved to `docs/plans/<filename>.md`. Ready to execute?"**

Execute tasks in order using:
- test-driven-development skill for each implementation task
- verification-before-completion skill before marking tasks done
- systematic-debugging skill if tests fail unexpectedly
