---
name: subagent-dev
description: Use when executing plans with independent tasks in current session - fresh agent per task
---

# Subagent-Driven Development

Execute plan by dispatching fresh subagent per task, with two-stage review after each: spec compliance review first, then code quality review.

**Core principle:** Fresh subagent per task + two-stage review (spec then quality) = high quality, fast iteration

## Use when

**Use when:**
- Have implementation plan with tasks
- Tasks are mostly independent
- Want to stay in current session
- Want automated review between tasks

**vs. Executing Plans (human checkpoints):**
- Same session (no context switch)
- Fresh subagent per task (no context pollution)
- Two-stage review after each task
- Faster iteration (no human-in-loop between tasks)

## The Process

1. **Read plan, extract all tasks, create TODO list**
2. **For each task:**
   - Dispatch implementer subagent
   - If questions → answer, re-dispatch
   - Implementer implements, tests, commits, self-reviews
   - Dispatch spec reviewer subagent
   - If spec issues → implementer fixes, re-review
   - Dispatch code quality reviewer subagent
   - If quality issues → implementer fixes, re-review
   - Mark task complete
3. **After all tasks:** Dispatch final code reviewer
4. **Use finishing-a-development-branch skill**

## Prompt Templates

### Skill Awareness Preamble

**Include at the start of ALL subagent prompts:**

```markdown
BEFORE STARTING: Read CLAUDE.md and review .claude/skills/

MANDATORY SKILLS (use these - NEVER use direct commands):
- build-project: `dotnet build.cs build` - NEVER `dotnet build`
- test-project: `dotnet build.cs test` - NEVER `dotnet test`
- commit: Follow guidelines - NEVER `git add .`
```

### Implementer Subagent

Use Task tool with agent_type: "general-purpose":

```markdown
BEFORE STARTING: Read CLAUDE.md and review .claude/skills/

MANDATORY SKILLS:
- build-project: `dotnet build.cs build` - NEVER `dotnet build`
- test-project: `dotnet build.cs test` - NEVER `dotnet test`
- commit: Follow guidelines - NEVER `git add .`

You are implementing Task N: [task name]

## Task Description
[FULL TEXT of task from plan - paste it here, don't make subagent read file]

## Context
[Scene-setting: where this fits, dependencies, architectural context]

## Before You Begin
If you have questions about requirements, approach, dependencies, or anything unclear - **ask them now.**

## Your Job
Once clear on requirements:
1. Implement exactly what the task specifies
2. Write tests (following TDD if task says to)
3. Verify implementation works
4. Commit your work
5. Self-review (see below)
6. Report back

Work from: [directory]

**While you work:** If you encounter something unexpected, **ask questions**. Don't guess.

## Self-Review Before Reporting
- Did I fully implement everything in the spec?
- Did I miss any requirements or edge cases?
- Are names clear and accurate?
- Did I avoid overbuilding (YAGNI)?
- Do tests actually verify behavior?

Fix any issues found before reporting.

## Report Format
- What you implemented
- What you tested and test results
- Files changed
- Self-review findings (if any)
- Any issues or concerns
```

### Spec Compliance Reviewer

Use Task tool with agent_type: "general-purpose":

```markdown
You are reviewing whether an implementation matches its specification.

## What Was Requested
[FULL TEXT of task requirements]

## What Implementer Claims They Built
[From implementer's report]

## CRITICAL: Do Not Trust the Report
Verify everything independently by reading actual code.

**Check for:**
- Missing requirements (skipped or not implemented)
- Extra/unneeded work (over-engineering, unrequested features)
- Misunderstandings (wrong interpretation)

**Verify by reading code, not by trusting report.**

Report:
- ✅ Spec compliant (if everything matches after code inspection)
- ❌ Issues found: [list what's missing or extra, with file:line references]
```

### Code Quality Reviewer

Use the requesting-code-review skill's code reviewer template with:
- WHAT_WAS_IMPLEMENTED: [from implementer's report]
- PLAN_OR_REQUIREMENTS: Task N from [plan-file]
- BASE_SHA: [commit before task]
- HEAD_SHA: [current commit]

## Example Workflow

```
You: I'm using Subagent-Driven Development to execute this plan.

[Read plan file: docs/plans/feature-plan.md]
[Extract all 5 tasks with full text]
[Create TODO list with all tasks]

Task 1: Hook installation script

[Dispatch implementation subagent with full task text + context]

Implementer: "Before I begin - should the hook be installed at user or system level?"

You: "User level (~/.config/superpowers/hooks/)"

Implementer: [implements, tests, commits, self-reviews]
  - Implemented install-hook command
  - Added tests, 5/5 passing
  - Committed

[Dispatch spec compliance reviewer]
Spec reviewer: ✅ Spec compliant - all requirements met

[Dispatch code quality reviewer]
Code reviewer: Strengths: Good test coverage. Issues: None. Approved.

[Mark Task 1 complete]

Task 2: Recovery modes
[Continue same pattern...]

[After all tasks]
[Dispatch final code-reviewer for entire implementation]
Final reviewer: All requirements met, ready to merge

[Use finishing-a-development-branch skill]
Done!
```

## Advantages

**vs. Manual execution:**
- Subagents follow TDD naturally
- Fresh context per task (no confusion)
- Subagent can ask questions

**vs. Executing Plans:**
- Same session (no handoff)
- Continuous progress (no waiting)
- Review checkpoints automatic

**Quality gates:**
- Self-review catches issues early
- Spec compliance prevents over/under-building
- Code quality ensures good implementation

## Red Flags

**Never:**
- Skip reviews (spec compliance OR code quality)
- Proceed with unfixed issues
- Dispatch multiple implementation subagents in parallel (conflicts)
- Make subagent read plan file (provide full text instead)
- Ignore subagent questions
- Start code quality review before spec compliance passes
- Move to next task while review has open issues

**If subagent asks questions:**
- Answer clearly and completely
- Provide additional context if needed
- Don't rush them into implementation

**If reviewer finds issues:**
- Implementer fixes them
- Reviewer reviews again
- Repeat until approved

## Integration

**Required workflow skills:**
- **writing-plans** - Creates the plan this skill executes
- **requesting-code-review** - Code review template for reviewer subagents
- **finishing-a-development-branch** - Complete development after all tasks

**Subagents should use:**
- **test-driven-development** - Subagents follow TDD for each task

**Alternative workflow:**
- **executing-plans** - Use for human checkpoints instead of automated review
