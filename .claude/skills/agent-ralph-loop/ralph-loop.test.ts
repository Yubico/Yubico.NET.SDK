/**
 * Unit tests for ralph-loop-utils.ts
 * Run with: bun test ralph-loop.test.ts
 */

import { describe, expect, test } from "bun:test";
import {
  isProgressFile,
  parseProgressFile,
  formatProgressContext,
  deriveSessionSlug,
  formatSkillsForPrompt,
  formatDuration,
  detectPhaseFromCommits,
  parseSkillFile,
  parseArgs,
  type Config,
  type SkillInfo,
} from "./ralph-loop-utils";

// --- isProgressFile ---

describe("isProgressFile", () => {
  test("returns true for valid progress file", () => {
    const content = `---
type: progress
feature: Test Feature
---
# Content`;
    expect(isProgressFile(content)).toBe(true);
  });

  test("returns false for non-progress type", () => {
    const content = `---
type: prd
feature: Test Feature
---
# Content`;
    expect(isProgressFile(content)).toBe(false);
  });

  test("returns false for missing frontmatter", () => {
    const content = `# Just markdown
No frontmatter here`;
    expect(isProgressFile(content)).toBe(false);
  });

  test("returns false for malformed frontmatter", () => {
    const content = `---
type: progress
feature: Test
# Missing closing ---`;
    expect(isProgressFile(content)).toBe(false);
  });

  test("handles type with extra whitespace", () => {
    const content = `---
type:   progress  
feature: Test
---`;
    expect(isProgressFile(content)).toBe(true);
  });

  test("handles Windows CRLF line endings", () => {
    const content = "---\r\ntype: progress\r\nfeature: Test\r\n---\r\n# Content";
    expect(isProgressFile(content)).toBe(true);
  });
});

// --- parseProgressFile ---

describe("parseProgressFile", () => {
  const validProgressFile = `---
type: progress
feature: OAuth Implementation
prd: docs/prd/oauth.md
status: in-progress
---

# OAuth Implementation Progress

## Phase 1: Core Types (P0)

**Goal:** Define core OAuth types

- Src: \`src/oauth/types.ts\`
- Test: \`test/oauth/types.test.ts\`

- [x] 1.1: Create Token interface
- [ ] 1.2: Create Client interface
- [ ] 1.3: Add validation helpers

## Phase 2: API Integration (P1)

**Goal:** Integrate with OAuth provider

- Src: \`src/oauth/client.ts\`
- Test: \`test/oauth/client.test.ts\`

- [ ] 2.1: Implement token fetch
- [ ] 2.2: Implement token refresh
`;

  test("parses valid progress file", () => {
    const result = parseProgressFile(validProgressFile);

    expect(result.isProgressFile).toBe(true);
    expect(result.feature).toBe("OAuth Implementation");
    expect(result.prd).toBe("docs/prd/oauth.md");
    expect(result.status).toBe("in-progress");
  });

  test("parses phases correctly", () => {
    const result = parseProgressFile(validProgressFile);

    expect(result.phases).toHaveLength(2);
    expect(result.phases[0].name).toBe("Core Types");
    expect(result.phases[0].priority).toBe(0);
    expect(result.phases[1].name).toBe("API Integration");
    expect(result.phases[1].priority).toBe(1);
  });

  test("parses phase goals and files", () => {
    const result = parseProgressFile(validProgressFile);

    expect(result.phases[0].goal).toBe("Define core OAuth types");
    expect(result.phases[0].files.src).toBe("src/oauth/types.ts");
    expect(result.phases[0].files.test).toBe("test/oauth/types.test.ts");
  });

  test("parses tasks with completion status", () => {
    const result = parseProgressFile(validProgressFile);

    const phase1Tasks = result.phases[0].tasks;
    expect(phase1Tasks).toHaveLength(3);
    expect(phase1Tasks[0].id).toBe("1.1");
    expect(phase1Tasks[0].completed).toBe(true);
    expect(phase1Tasks[1].id).toBe("1.2");
    expect(phase1Tasks[1].completed).toBe(false);
  });

  test("identifies current phase and task", () => {
    const result = parseProgressFile(validProgressFile);

    expect(result.currentPhase?.name).toBe("Core Types");
    expect(result.currentTask?.id).toBe("1.2");
    expect(result.currentTask?.description).toBe("Create Client interface");
  });

  test("sorts phases by priority", () => {
    const content = `---
type: progress
feature: Test
---

## Phase 1: Low Priority (P2)
- [ ] 1.1: Task A

## Phase 2: High Priority (P0)
- [ ] 2.1: Task B

## Phase 3: Medium Priority (P1)
- [ ] 3.1: Task C
`;
    const result = parseProgressFile(content);

    expect(result.phases[0].priority).toBe(0);
    expect(result.phases[1].priority).toBe(1);
    expect(result.phases[2].priority).toBe(2);
  });

  test("returns default state for non-progress file", () => {
    const content = `# Just markdown`;
    const result = parseProgressFile(content);

    expect(result.isProgressFile).toBe(false);
    expect(result.phases).toHaveLength(0);
    expect(result.currentPhase).toBeNull();
  });

  test("handles all tasks completed", () => {
    const content = `---
type: progress
feature: Done Feature
---

## Phase 1: Complete (P0)
- [x] 1.1: Task A
- [x] 1.2: Task B
`;
    const result = parseProgressFile(content);

    expect(result.isProgressFile).toBe(true);
    expect(result.currentPhase).toBeNull();
    expect(result.currentTask).toBeNull();
  });

  test("handles letter-prefixed task IDs", () => {
    const content = `---
type: progress
feature: Test
---

## Phase 1: Setup (P0)
- [ ] S.1: Setup task
- [ ] S.2: Another setup
`;
    const result = parseProgressFile(content);

    expect(result.phases[0].tasks[0].id).toBe("S.1");
    expect(result.phases[0].tasks[1].id).toBe("S.2");
  });
});

// --- formatProgressContext ---

describe("formatProgressContext", () => {
  test("formats active task context", () => {
    const state = parseProgressFile(`---
type: progress
feature: Test
---

## Phase 1: Core (P0)
**Goal:** Build core functionality
- Src: \`src/core.ts\`
- Test: \`test/core.test.ts\`
- [ ] 1.1: First task
- [ ] 1.2: Second task
`);
    const result = formatProgressContext(state);

    expect(result).toContain("**Phase:** Core (P0)");
    expect(result).toContain("**Goal:** Build core functionality");
    expect(result).toContain("**Current Task:** 1.1: First task");
    expect(result).toContain("- Src: `src/core.ts`");
  });

  test("formats completion context when all tasks done", () => {
    const state = parseProgressFile(`---
type: progress
feature: Test
---

## Phase 1: Core (P0)
- [x] 1.1: Done task
`);
    const result = formatProgressContext(state);

    expect(result).toContain("All tasks complete!");
    expect(result).toContain("dotnet build.cs build");
  });

  test("shows remaining tasks in phase", () => {
    const state = parseProgressFile(`---
type: progress
feature: Test
---

## Phase 1: Core (P0)
**Goal:** Test
- [x] 1.1: Done
- [ ] 1.2: Remaining A
- [ ] 1.3: Remaining B
`);
    const result = formatProgressContext(state);

    expect(result).toContain("- [ ] 1.2: Remaining A");
    expect(result).toContain("- [ ] 1.3: Remaining B");
    expect(result).not.toContain("1.1: Done");
  });
});

// --- deriveSessionSlug ---

describe("deriveSessionSlug", () => {
  const baseConfig: Config = {
    promptParts: [],
    maxIterations: 0,
    completionPromise: null,
    delay: 2,
    learningMode: false,
    promptFile: null,
    model: null,
    session: null,
  };

  test("uses explicit session name", () => {
    const config = { ...baseConfig, session: "My Feature" };
    expect(deriveSessionSlug(config)).toBe("my-feature");
  });

  test("sanitizes session name", () => {
    const config = { ...baseConfig, session: "Feature @#$ Test!!!" };
    expect(deriveSessionSlug(config)).toBe("feature-test-");
  });

  test("extracts slug from prompt file name", () => {
    const config = { ...baseConfig, promptFile: "docs/prompts/oauth-impl.md" };
    expect(deriveSessionSlug(config)).toBe("oauth-impl");
  });

  test("removes date prefix from prompt file", () => {
    const config = {
      ...baseConfig,
      promptFile: "docs/2026-01-18-feature-name.md",
    };
    expect(deriveSessionSlug(config)).toBe("feature-name");
  });

  test("truncates long session names", () => {
    const config = {
      ...baseConfig,
      session: "a".repeat(100),
    };
    expect(deriveSessionSlug(config).length).toBe(50);
  });

  test("falls back to timestamp format", () => {
    const result = deriveSessionSlug(baseConfig);
    // Should match ISO timestamp pattern: 2026-01-19T15-01-45
    expect(result).toMatch(/^\d{4}-\d{2}-\d{2}T\d{2}-\d{2}-\d{2}$/);
  });

  test("session flag takes priority over prompt file", () => {
    const config = {
      ...baseConfig,
      session: "explicit-name",
      promptFile: "docs/file-name.md",
    };
    expect(deriveSessionSlug(config)).toBe("explicit-name");
  });
});

// --- formatSkillsForPrompt ---

describe("formatSkillsForPrompt", () => {
  test("returns empty string for no skills", () => {
    expect(formatSkillsForPrompt([])).toBe("");
  });

  test("formats mandatory and optional skills separately", () => {
    const skills: SkillInfo[] = [
      { name: "build", description: "REQUIRED for building", mandatory: true },
      { name: "test", description: "NEVER use dotnet test", mandatory: true },
      { name: "debug", description: "Debugging helper", mandatory: false },
    ];
    const result = formatSkillsForPrompt(skills);

    expect(result).toContain("MANDATORY SKILLS");
    expect(result).toContain("- build: REQUIRED for building");
    expect(result).toContain("OTHER SKILLS");
    expect(result).toContain("- debug: Debugging helper");
  });

  test("includes skill rules", () => {
    const skills: SkillInfo[] = [
      { name: "test", description: "Test runner", mandatory: false },
    ];
    const result = formatSkillsForPrompt(skills);

    expect(result).toContain("SKILL RULES");
    expect(result).toContain("FORBIDDEN");
  });
});

// --- formatDuration ---

describe("formatDuration", () => {
  test("formats seconds only", () => {
    expect(formatDuration(45)).toBe("45s");
  });

  test("formats minutes and seconds", () => {
    expect(formatDuration(125)).toBe("2m 5s");
  });

  test("formats hours, minutes", () => {
    expect(formatDuration(3725)).toBe("1h 2m");
  });

  test("handles zero", () => {
    expect(formatDuration(0)).toBe("0s");
  });

  test("handles exactly one minute", () => {
    expect(formatDuration(60)).toBe("1m 0s");
  });

  test("handles exactly one hour", () => {
    expect(formatDuration(3600)).toBe("1h 0m");
  });
});

// --- detectPhaseFromCommits ---

describe("detectPhaseFromCommits", () => {
  test("detects phase from 'Phase N' pattern", () => {
    const commits = [{ message: "Complete Phase 1 implementation" }];
    expect(detectPhaseFromCommits(commits)).toBe("Phase 1");
  });

  test("detects phase from 'phase_N' pattern", () => {
    const commits = [{ message: "phase_2_done" }];
    expect(detectPhaseFromCommits(commits)).toBe("Phase 2");
  });

  test("detects scope from conventional commit", () => {
    const commits = [{ message: "feat(oauth): add token refresh" }];
    expect(detectPhaseFromCommits(commits)).toBe("oauth");
  });

  test("detects scope from test commit", () => {
    const commits = [{ message: "test(piv): add PIN validation tests" }];
    expect(detectPhaseFromCommits(commits)).toBe("piv");
  });

  test("returns null for no pattern match", () => {
    const commits = [{ message: "misc: update readme" }];
    expect(detectPhaseFromCommits(commits)).toBeNull();
  });

  test("returns first match from multiple commits", () => {
    const commits = [
      { message: "chore: cleanup" },
      { message: "Phase 3 complete" },
      { message: "feat(core): add feature" },
    ];
    expect(detectPhaseFromCommits(commits)).toBe("Phase 3");
  });

  test("handles empty commits array", () => {
    expect(detectPhaseFromCommits([])).toBeNull();
  });
});

// --- parseSkillFile ---

describe("parseSkillFile", () => {
  test("parses valid skill file", () => {
    const content = `---
name: build-project
description: REQUIRED for building .NET code
---
# Build Project Skill`;
    const result = parseSkillFile(content);

    expect(result).not.toBeNull();
    expect(result!.name).toBe("build-project");
    expect(result!.description).toBe("REQUIRED for building .NET code");
    expect(result!.mandatory).toBe(true);
  });

  test("detects mandatory from 'NEVER use'", () => {
    const content = `---
name: test-project
description: NEVER use dotnet test directly
---`;
    const result = parseSkillFile(content);

    expect(result!.mandatory).toBe(true);
  });

  test("detects optional skill", () => {
    const content = `---
name: debug
description: Debugging helper tool
---`;
    const result = parseSkillFile(content);

    expect(result!.mandatory).toBe(false);
  });

  test("returns null for missing name", () => {
    const content = `---
description: Some description
---`;
    expect(parseSkillFile(content)).toBeNull();
  });

  test("returns null for missing frontmatter", () => {
    const content = `# No frontmatter`;
    expect(parseSkillFile(content)).toBeNull();
  });

  test("handles Windows CRLF line endings", () => {
    const content = "---\r\nname: build-project\r\ndescription: REQUIRED for building .NET code\r\n---\r\n# Build";
    const result = parseSkillFile(content);

    expect(result).not.toBeNull();
    expect(result!.name).toBe("build-project");
    expect(result!.mandatory).toBe(true);
  });
});

// --- parseArgs ---

describe("parseArgs", () => {
  test("parses prompt parts", () => {
    const { config, error } = parseArgs(["do", "something", "cool"]);

    expect(error).toBeNull();
    expect(config.promptParts).toEqual(["do", "something", "cool"]);
  });

  test("parses --max-iterations", () => {
    const { config, error } = parseArgs(["--max-iterations", "5", "prompt"]);

    expect(error).toBeNull();
    expect(config.maxIterations).toBe(5);
  });

  test("parses --completion-promise", () => {
    const { config, error } = parseArgs(["--completion-promise", "DONE"]);

    expect(error).toBeNull();
    expect(config.completionPromise).toBe("DONE");
  });

  test("parses --delay", () => {
    const { config, error } = parseArgs(["--delay", "10"]);

    expect(error).toBeNull();
    expect(config.delay).toBe(10);
  });

  test("parses --learn flag", () => {
    const { config, error } = parseArgs(["--learn"]);

    expect(error).toBeNull();
    expect(config.learningMode).toBe(true);
  });

  test("parses --prompt-file", () => {
    const { config, error } = parseArgs(["--prompt-file", "task.md"]);

    expect(error).toBeNull();
    expect(config.promptFile).toBe("task.md");
  });

  test("parses --model", () => {
    const { config, error } = parseArgs(["--model", "gpt-4"]);

    expect(error).toBeNull();
    expect(config.model).toBe("gpt-4");
  });

  test("parses --session", () => {
    const { config, error } = parseArgs(["--session", "my-session"]);

    expect(error).toBeNull();
    expect(config.session).toBe("my-session");
  });

  test("parses combined options", () => {
    const { config, error } = parseArgs([
      "--max-iterations",
      "10",
      "--learn",
      "--model",
      "claude",
      "my",
      "prompt",
    ]);

    expect(error).toBeNull();
    expect(config.maxIterations).toBe(10);
    expect(config.learningMode).toBe(true);
    expect(config.model).toBe("claude");
    expect(config.promptParts).toEqual(["my", "prompt"]);
  });

  test("returns help error for -h", () => {
    const { error } = parseArgs(["-h"]);
    expect(error).toBe("help");
  });

  test("returns help error for --help", () => {
    const { error } = parseArgs(["--help"]);
    expect(error).toBe("help");
  });

  test("returns error for missing --max-iterations value", () => {
    const { error } = parseArgs(["--max-iterations"]);
    expect(error).toBe("--max-iterations requires a number");
  });

  test("returns error for invalid --max-iterations", () => {
    const { error } = parseArgs(["--max-iterations", "abc"]);
    expect(error).toBe("--max-iterations must be a valid number");
  });

  test("returns error for negative --delay", () => {
    const { error } = parseArgs(["--delay", "-5"]);
    expect(error).toContain("--delay");
  });

  test("returns error for unknown option", () => {
    const { error } = parseArgs(["--unknown-flag"]);
    expect(error).toBe("Unknown option: --unknown-flag");
  });

  test("uses default values", () => {
    const { config, error } = parseArgs([]);

    expect(error).toBeNull();
    expect(config.maxIterations).toBe(0);
    expect(config.delay).toBe(2);
    expect(config.learningMode).toBe(false);
    expect(config.completionPromise).toBeNull();
  });
});
