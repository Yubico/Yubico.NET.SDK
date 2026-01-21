/**
 * Pure utility functions extracted from ralph-loop.ts for testability.
 */

// --- Types ---

export interface SkillInfo {
  name: string;
  description: string;
  mandatory: boolean;
}

export interface ProgressTask {
  id: string;
  description: string;
  completed: boolean;
  lineNumber: number;
}

export interface ProgressPhase {
  name: string;
  priority: number; // 0, 1, or 2
  goal: string;
  files: { src?: string; test?: string };
  tasks: ProgressTask[];
}

export interface ProgressFileState {
  isProgressFile: boolean;
  feature: string;
  prd?: string;
  status: string;
  phases: ProgressPhase[];
  currentPhase: ProgressPhase | null;
  currentTask: ProgressTask | null;
  rawContent: string;
}

export interface Config {
  promptParts: string[];
  maxIterations: number;
  completionPromise: string | null;
  delay: number;
  learningMode: boolean;
  promptFile: string | null;
  model: string | null;
  session: string | null;
}

// --- Pure Functions ---

/**
 * Detects if content is a progress file by checking for YAML frontmatter with type: progress
 */
export function isProgressFile(content: string): boolean {
  const frontmatterMatch = content.match(/^---\n([\s\S]*?)\n---/);
  if (!frontmatterMatch) return false;
  return /^type:\s*progress\s*$/m.test(frontmatterMatch[1]);
}

/**
 * Parses a progress file's YAML frontmatter and markdown structure into a typed state object
 */
export function parseProgressFile(content: string): ProgressFileState {
  const result: ProgressFileState = {
    isProgressFile: false,
    feature: "",
    status: "in-progress",
    phases: [],
    currentPhase: null,
    currentTask: null,
    rawContent: content,
  };

  // Parse YAML frontmatter
  const frontmatterMatch = content.match(/^---\n([\s\S]*?)\n---/);
  if (!frontmatterMatch) return result;

  const frontmatter = frontmatterMatch[1];
  if (!/^type:\s*progress\s*$/m.test(frontmatter)) return result;

  result.isProgressFile = true;

  // Extract frontmatter fields
  const featureMatch = frontmatter.match(/^feature:\s*(.+)$/m);
  if (featureMatch) result.feature = featureMatch[1].trim();

  const prdMatch = frontmatter.match(/^prd:\s*(.+)$/m);
  if (prdMatch) result.prd = prdMatch[1].trim();

  const statusMatch = frontmatter.match(/^status:\s*(.+)$/m);
  if (statusMatch) result.status = statusMatch[1].trim();

  // Parse phases - look for ## Phase N: Name (P0/P1/P2)
  const lines = content.split("\n");
  let currentPhase: ProgressPhase | null = null;

  for (let i = 0; i < lines.length; i++) {
    const line = lines[i];

    // Phase header: ## Phase 1: Name (P0)
    const phaseMatch = line.match(/^##\s+Phase\s+\d+:\s*(.+?)\s*\(P(\d)\)\s*$/i);
    if (phaseMatch) {
      if (currentPhase) result.phases.push(currentPhase);
      currentPhase = {
        name: phaseMatch[1].trim(),
        priority: parseInt(phaseMatch[2], 10),
        goal: "",
        files: {},
        tasks: [],
      };
      continue;
    }

    if (!currentPhase) continue;

    // Goal line: **Goal:** text
    const goalMatch = line.match(/^\*\*Goal:\*\*\s*(.+)$/);
    if (goalMatch) {
      currentPhase.goal = goalMatch[1].trim();
      continue;
    }

    // Files - Src: `path`
    const srcMatch = line.match(/^-\s*Src:\s*`(.+)`/);
    if (srcMatch) {
      currentPhase.files.src = srcMatch[1];
      continue;
    }

    // Files - Test: `path`
    const testMatch = line.match(/^-\s*Test:\s*`(.+)`/);
    if (testMatch) {
      currentPhase.files.test = testMatch[1];
      continue;
    }

    // Task: - [ ] 1.1: Description or - [x] S.1: Description
    const taskMatch = line.match(/^-\s*\[([ x])\]\s*([A-Za-z0-9]+\.\d+):\s*(.+)$/);
    if (taskMatch) {
      currentPhase.tasks.push({
        id: taskMatch[2],
        description: taskMatch[3].trim(),
        completed: taskMatch[1] === "x",
        lineNumber: i,
      });
    }
  }

  // Don't forget the last phase
  if (currentPhase) result.phases.push(currentPhase);

  // Sort phases by priority
  result.phases.sort((a, b) => a.priority - b.priority);

  // Find current phase (first with incomplete tasks)
  for (const phase of result.phases) {
    const incompleteTask = phase.tasks.find((t) => !t.completed);
    if (incompleteTask) {
      result.currentPhase = phase;
      result.currentTask = incompleteTask;
      break;
    }
  }

  return result;
}

/**
 * Formats progress state into a context string for the AI prompt
 */
export function formatProgressContext(state: ProgressFileState): string {
  if (!state.currentPhase || !state.currentTask) {
    return `
[PROGRESS FILE STATUS]
All tasks complete! Verify everything passes, then output the completion promise.

Final verification:
1. Run: \`dotnet build.cs build\` - must exit 0
2. Run: \`dotnet build.cs test\` - all tests must pass
3. Check: No regressions in existing tests
`;
  }

  return `
[CURRENT TASK CONTEXT]
**Phase:** ${state.currentPhase.name} (P${state.currentPhase.priority})
**Goal:** ${state.currentPhase.goal}
**Files:**
- Src: \`${state.currentPhase.files.src || "TBD"}\`
- Test: \`${state.currentPhase.files.test || "TBD"}\`

**Current Task:** ${state.currentTask.id}: ${state.currentTask.description}

**Progress File:** Re-read the progress file to see full context and update it after completing this task.

**Remaining in this phase:**
${state.currentPhase.tasks
  .filter((t) => !t.completed)
  .map((t) => `- [ ] ${t.id}: ${t.description}`)
  .join("\n")}
`;
}

/**
 * Derives a session slug from config options
 */
export function deriveSessionSlug(config: Config): string {
  // Priority 1: Explicit --session flag
  if (config.session) {
    return config.session
      .toLowerCase()
      .replace(/[^a-z0-9-]/g, "-")
      .replace(/-+/g, "-")
      .slice(0, 50);
  }

  // Priority 2: Extract from prompt file name
  if (config.promptFile) {
    const filename = config.promptFile.split("/").pop()?.replace(/\.md$/, "") || "";
    // Remove date prefix if present (e.g., "2026-01-18-feature-name" -> "feature-name")
    const withoutDate = filename.replace(/^\d{4}-\d{2}-\d{2}-?/, "");
    if (withoutDate) {
      return withoutDate
        .toLowerCase()
        .replace(/[^a-z0-9-]/g, "-")
        .replace(/-+/g, "-")
        .slice(0, 50);
    }
  }

  // Priority 3: Timestamp fallback
  return new Date().toISOString().replace(/[:.]/g, "-").slice(0, 19);
}

/**
 * Formats skills info into a prompt section
 */
export function formatSkillsForPrompt(skills: SkillInfo[]): string {
  if (skills.length === 0) return "";

  const mandatory = skills.filter((s) => s.mandatory);
  const optional = skills.filter((s) => !s.mandatory);

  return `[AVAILABLE SKILLS - REVIEW BEFORE STARTING]

MANDATORY SKILLS (violating these is a critical error):
${mandatory.map((s) => `- ${s.name}: ${s.description}`).join("\n")}

OTHER SKILLS:
${optional.map((s) => `- ${s.name}: ${s.description}`).join("\n")}

SKILL RULES:
- BEFORE any build/test/commit action, check if a skill covers it
- Use \`skill invoke <name>\` or follow skill instructions
- Mandatory skills MUST be used - direct commands (dotnet build, dotnet test, git add .) are FORBIDDEN
`;
}

/**
 * Formats a duration in seconds to a human-readable string
 */
export function formatDuration(seconds: number): string {
  if (seconds < 60) return `${seconds}s`;
  const mins = Math.floor(seconds / 60);
  const secs = seconds % 60;
  if (mins < 60) return `${mins}m ${secs}s`;
  const hours = Math.floor(mins / 60);
  const remainMins = mins % 60;
  return `${hours}h ${remainMins}m`;
}

/**
 * Detects phase from commit messages
 */
export function detectPhaseFromCommits(
  commits: Array<{ message: string }>
): string | null {
  for (const commit of commits) {
    // Match patterns like "Phase 1", "phase 2", "PHASE_1_DONE"
    const phaseMatch = commit.message.match(/phase[\s_-]*(\d+)/i);
    if (phaseMatch) return `Phase ${phaseMatch[1]}`;

    // Match conventional commit scopes like "feat(core):", "test(piv):"
    const scopeMatch = commit.message.match(
      /^(?:feat|fix|test|refactor|docs)\(([^)]+)\)/i
    );
    if (scopeMatch) return scopeMatch[1];
  }
  return null;
}

/**
 * Parses skill info from SKILL.md content
 */
export function parseSkillFile(content: string): SkillInfo | null {
  const frontmatterMatch = content.match(/^---\n([\s\S]*?)\n---/);
  if (!frontmatterMatch) return null;

  const frontmatter = frontmatterMatch[1];
  const nameMatch = frontmatter.match(/^name:\s*(.+)$/m);
  const descMatch = frontmatter.match(/^description:\s*(.+)$/m);

  if (!nameMatch || !descMatch) return null;

  const name = nameMatch[1].trim();
  const description = descMatch[1].trim();
  const mandatory =
    description.toLowerCase().includes("required") ||
    description.toLowerCase().includes("never use");

  return { name, description, mandatory };
}

/**
 * Parses command line arguments into a Config object.
 * Returns { config, error } where error is set if parsing failed.
 */
export function parseArgs(
  args: string[]
): { config: Config; error: string | null } {
  const config: Config = {
    promptParts: [],
    maxIterations: 0,
    completionPromise: null,
    delay: 2,
    learningMode: false,
    promptFile: null,
    model: null,
    session: null,
  };

  for (let i = 0; i < args.length; i++) {
    switch (args[i]) {
      case "-h":
      case "--help":
        return { config, error: "help" };
      case "--max-iterations":
        if (i + 1 >= args.length || args[i + 1].startsWith("-")) {
          return { config, error: "--max-iterations requires a number" };
        }
        config.maxIterations = parseInt(args[++i], 10);
        if (isNaN(config.maxIterations)) {
          return { config, error: "--max-iterations must be a valid number" };
        }
        break;
      case "--completion-promise":
        if (
          i + 1 >= args.length ||
          args[i + 1] === "" ||
          args[i + 1].startsWith("-")
        ) {
          return {
            config,
            error: "--completion-promise requires a non-empty value",
          };
        }
        config.completionPromise = args[++i];
        break;
      case "--delay":
        if (i + 1 >= args.length || args[i + 1].startsWith("-")) {
          return { config, error: "--delay requires a number" };
        }
        config.delay = parseInt(args[++i], 10);
        if (isNaN(config.delay) || config.delay < 0) {
          return {
            config,
            error: "--delay must be a valid non-negative number",
          };
        }
        break;
      case "--learn":
        config.learningMode = true;
        break;
      case "--prompt-file":
        if (i + 1 >= args.length) {
          return { config, error: "--prompt-file requires a file path" };
        }
        config.promptFile = args[++i];
        break;
      case "--model":
        if (i + 1 >= args.length) {
          return { config, error: "--model requires a model name" };
        }
        config.model = args[++i];
        break;
      case "--session":
        if (i + 1 >= args.length) {
          return { config, error: "--session requires a session name" };
        }
        config.session = args[++i];
        break;
      default:
        if (args[i].startsWith("-")) {
          return { config, error: `Unknown option: ${args[i]}` };
        }
        config.promptParts.push(args[i]);
    }
  }
  return { config, error: null };
}
