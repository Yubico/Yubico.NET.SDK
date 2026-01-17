---
name: import-skill
description: Use when adding skills from external GitHub repos - adapts for Copilot CLI compatibility
---

# Importing Skills from GitHub

## Use when

- Importing skills/agents from external repositories (e.g., obra/superpowers)
- Adapting imported skills for Copilot CLI compatibility
- Need to review and transform skill files for this environment

## Overview

Import skills and agents from external GitHub repositories (like obra/superpowers) and adapt them for Copilot CLI compatibility.

**Two-phase process:**
1. **Script phase** - Run `import-skills.js` to download files
2. **Adaptation phase** - Review and adapt for Copilot CLI

## Phase 1: Run the Import Script

### List available skills first
```bash
bun scripts/import-skills.ts <repo> --list
```

Example:
```bash
bun scripts/import-skills.ts obra/superpowers --list
```

### Import specific skills
```bash
bun scripts/import-skills.ts <repo> --skills skill1,skill2
```

### Import everything
```bash
bun scripts/import-skills.ts <repo>
```

### Dry run (see what would happen)
```bash
bun scripts/import-skills.ts <repo> --dry-run
```

## Phase 2: Adapt for Copilot CLI

After importing, review each skill and make these adaptations:

### Feature Compatibility Matrix

| Feature | Claude Code | Copilot CLI | Adaptation |
|---------|-------------|-------------|------------|
| Skills | ✅ `.claude/skills/` | ✅ `.claude/skills/` | None needed |
| Agents | ✅ Various | ✅ `.github/agents/*.agent.md` | Rename files |
| TodoWrite | ✅ Built-in | ❌ Use `update_todo` tool | Replace references |
| Slash commands | ✅ Custom commands | ❌ Built-in only | Remove or convert to skills |
| Hooks | ✅ hooks.json | ❌ Not supported | Remove |
| Task tool | ✅ Subagents | ✅ Task tool | Compatible |
| MCP servers | ✅ | ✅ | Compatible |

### Common Adaptations

**1. Replace TodoWrite with update_todo:**
```markdown
# Before (Claude Code)
Create TodoWrite and proceed

# After (Copilot CLI)
Create TODO list using update_todo tool and proceed
```

**2. Remove hook references:**
Delete any mentions of `hooks.json`, `SessionStart`, or hook scripts.

**3. Remove slash command references:**
```markdown
# Before
Run /superpowers:brainstorm to start

# After
Use the brainstorming skill to start
```

**4. Update agent file naming:**
Ensure agents are named `*.agent.md` in `.github/agents/`

**5. Adjust .NET-specific commands:**
Update test/build commands to match this project:
```bash
# Tests
dotnet test Yubico.YubiKit.sln

# Build
dotnet build Yubico.YubiKit.sln
```

### Review Checklist

For each imported skill, verify:

- [ ] No TodoWrite references (use update_todo)
- [ ] No hook references
- [ ] No custom slash command references
- [ ] Agent files named correctly (*.agent.md)
- [ ] Tool references are Copilot CLI compatible
- [ ] Build/test commands match this project
- [ ] No superpowers-specific meta skills (using-superpowers)

## Workflow Example

```
User: Import skills from obra/superpowers

You: I'll use the importing-skills skill for this.

# Phase 1: Discover what's available
node scripts/import-skills.js obra/superpowers --list

# Phase 1: Import selected skills
node scripts/import-skills.js obra/superpowers --skills executing-plans,finishing-a-development-branch

# Phase 2: Review imports
[Read each imported SKILL.md]
[Check for incompatible features]
[Make adaptations as needed]

# Report
Imported and adapted:
- executing-plans: Replaced TodoWrite → update_todo
- finishing-a-development-branch: Updated test command for .NET
```

## Skills to Skip

Don't import these (not useful or incompatible):

| Skill | Reason |
|-------|--------|
| using-superpowers | Superpowers-specific intro, not needed |
| writing-skills | Meta-skill for superpowers repo |

## Post-Import Verification

After adaptation, verify skills work:

1. List skills: `/skills list`
2. Test skill activation: Ask to use the skill
3. Verify no errors in skill loading

## Integration

**This skill pairs with:**
- **writing-plans** - Import then use to plan implementation
- **brainstorming** - Brainstorm which skills to import
