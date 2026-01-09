---
name: requesting-code-review
description: Use when completing tasks, implementing major features, or before merging to verify work meets requirements
---

# Requesting Code Review

Dispatch code-reviewer agent to catch issues before they cascade.

**Core principle:** Review early, review often.

## When to Request Review

**Mandatory:**
- After each task in subagent-driven development
- After completing major feature
- Before merge to main

**Optional but valuable:**
- When stuck (fresh perspective)
- Before refactoring (baseline check)
- After fixing complex bug

## How to Request

**1. Get git SHAs:**
```bash
BASE_SHA=$(git rev-parse HEAD~1)  # or origin/main
HEAD_SHA=$(git rev-parse HEAD)
```

**2. Dispatch code-reviewer agent:**

Use Task tool with agent_type: "general-purpose" and the code-reviewer prompt template.

**Template placeholders:**
- `{WHAT_WAS_IMPLEMENTED}` - What you just built
- `{PLAN_OR_REQUIREMENTS}` - What it should do
- `{BASE_SHA}` - Starting commit
- `{HEAD_SHA}` - Ending commit
- `{DESCRIPTION}` - Brief summary

**3. Act on feedback:**
- Fix Critical issues immediately
- Fix Important issues before proceeding
- Note Minor issues for later
- Push back if reviewer is wrong (with reasoning)

## Code Reviewer Prompt Template

```markdown
You are a Senior Code Reviewer. Review the implementation against requirements.

## What Was Implemented
{WHAT_WAS_IMPLEMENTED}

## Requirements/Plan
{PLAN_OR_REQUIREMENTS}

## Changes to Review
Base: {BASE_SHA}
Head: {HEAD_SHA}

Review using: git diff {BASE_SHA}..{HEAD_SHA}

## Your Job

1. **Plan Alignment Analysis**:
   - Compare implementation against requirements
   - Identify deviations (justified improvements or problems?)
   - Verify all planned functionality implemented

2. **Code Quality Assessment**:
   - Adherence to established patterns
   - Error handling, type safety, defensive programming
   - Code organization, naming, maintainability
   - Test coverage and quality

3. **Architecture and Design**:
   - SOLID principles and architectural patterns
   - Separation of concerns, loose coupling
   - Integration with existing systems

4. **Issue Categorization**:
   - **Critical** (must fix): Breaks functionality, security issues
   - **Important** (should fix): Quality issues, missing tests
   - **Suggestions** (nice to have): Style, minor improvements

## Output Format

**Strengths:**
- [What was done well]

**Issues:**
- Critical: [list]
- Important: [list]
- Suggestions: [list]

**Assessment:** [Ready to proceed / Needs fixes first]
```

## Example

```
[Just completed Task 2: Add verification function]

You: Let me request code review before proceeding.

BASE_SHA=$(git log --oneline | grep "Task 1" | head -1 | awk '{print $1}')
HEAD_SHA=$(git rev-parse HEAD)

[Dispatch code-reviewer agent with template]

[Agent returns]:
  Strengths: Clean architecture, real tests
  Issues:
    Important: Missing progress indicators
    Minor: Magic number (100) for reporting interval
  Assessment: Ready to proceed

You: [Fix progress indicators]
[Continue to Task 3]
```

## Integration with Workflows

**Subagent-Driven Development:**
- Review after EACH task
- Catch issues before they compound
- Fix before moving to next task

**Executing Plans:**
- Review after each batch (3 tasks)
- Get feedback, apply, continue

**Ad-Hoc Development:**
- Review before merge
- Review when stuck

## Red Flags

**Never:**
- Skip review because "it's simple"
- Ignore Critical issues
- Proceed with unfixed Important issues
- Argue with valid technical feedback

**If reviewer wrong:**
- Push back with technical reasoning
- Show code/tests that prove it works
- Request clarification
