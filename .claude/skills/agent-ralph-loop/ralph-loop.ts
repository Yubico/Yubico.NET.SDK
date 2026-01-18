#!/usr/bin/env bun

import { join, dirname, basename } from "path";
import { readdirSync, existsSync, readFileSync } from "fs";

// --- Configuration & Constants ---

interface SkillInfo {
  name: string;
  description: string;
  mandatory: boolean;
}

interface Config {
  promptParts: string[];
  maxIterations: number;
  completionPromise: string | null;
  delay: number; // seconds
  learningMode: boolean;
  promptFile: string | null;
  model: string | null;
}

interface IterationMetrics {
  iteration: number;
  durationSeconds: number;
  phase: string | null;
  commitMessage: string | null;
  filesChanged: number;
  linesAdded: number;
  linesRemoved: number;
  fileList: string[];
}

const CONSTANTS = {
  STATE_FILE: "./docs/ralph-loop/state.md",
  LEARNING_DIR: "./docs/ralph-loop/learning",
  COLOR: {
    RESET: "\x1b[0m",
    RED: "\x1b[31m",
    GREEN: "\x1b[32m",
    YELLOW: "\x1b[33m",
    BLUE: "\x1b[34m",
    CYAN: "\x1b[36m",
  },
};

// --- Helper Functions ---

const sleep = (ms: number) => new Promise((resolve) => setTimeout(resolve, ms));

// Bun-native implementation of "tee" (pipe stream to console + file)
async function runCopilotWithTee(args: string[], logFile: string): Promise<string> {
  // Ensure directory exists
  // Bun doesn't have mkdir -p on file write automatically, so we use shell or node compat
  // using Bun's shell for ease:
  const dir = dirname(logFile);
  if (dir !== ".") await import("fs").then(fs => fs.mkdirSync(dir, { recursive: true }));

  // Start Copilot
  // We use "bash -c" to ensure complex args (like quotes) are handled if passed as a single string,
  // but sticking to array args is safer with Bun.spawn
  const proc = Bun.spawn(["copilot", ...args], {
    stdout: "pipe",
    stderr: "pipe", 
  });

  const file = Bun.file(logFile);
  const writer = file.writer();
  let fullOutput = "";

  // Helper to read a stream and tee it
  const readStream = async (reader: ReadableStreamDefaultReader<Uint8Array>) => {
    const decoder = new TextDecoder("utf-8", { fatal: false });
    while (true) {
      const { done, value } = await reader.read();
      if (done) {
        // Flush any remaining bytes
        const final = decoder.decode(new Uint8Array(), { stream: false });
        if (final) {
          process.stdout.write(final);
          writer.write(final);
          fullOutput += final;
        }
        break;
      }
      const chunk = decoder.decode(value, { stream: true });
      
      process.stdout.write(chunk); // Stream to terminal
      writer.write(chunk);         // Stream to file
      fullOutput += chunk;
    }
  };

  // Read stdout and stderr concurrently
  const streamPromises: Promise<void>[] = [];
  if (proc.stdout) streamPromises.push(readStream(proc.stdout.getReader()));
  if (proc.stderr) streamPromises.push(readStream(proc.stderr.getReader()));

  await Promise.all(streamPromises);
  await proc.exited;
  writer.end();
  
  return fullOutput;
}

// --- Main Class ---

// --- Skill Discovery ---

function discoverSkills(): SkillInfo[] {
  const skillsDir = ".claude/skills";
  if (!existsSync(skillsDir)) return [];

  const skills: SkillInfo[] = [];
  const dirs = readdirSync(skillsDir, { withFileTypes: true })
    .filter(d => d.isDirectory())
    .map(d => d.name);

  for (const dir of dirs) {
    const skillFile = join(skillsDir, dir, "SKILL.md");
    if (!existsSync(skillFile)) continue;

    const content = readFileSync(skillFile, "utf-8");
    
    // Parse YAML frontmatter
    const frontmatterMatch = content.match(/^---\n([\s\S]*?)\n---/);
    if (!frontmatterMatch) continue;

    const frontmatter = frontmatterMatch[1];
    const nameMatch = frontmatter.match(/^name:\s*(.+)$/m);
    const descMatch = frontmatter.match(/^description:\s*(.+)$/m);

    if (nameMatch && descMatch) {
      const name = nameMatch[1].trim();
      const description = descMatch[1].trim();
      // Detect mandatory skills by keywords in description
      const mandatory = description.toLowerCase().includes("required") || 
                        description.toLowerCase().includes("never use");
      skills.push({ name, description, mandatory });
    }
  }

  return skills;
}

function formatSkillsForPrompt(skills: SkillInfo[]): string {
  if (skills.length === 0) return "";

  const mandatory = skills.filter(s => s.mandatory);
  const optional = skills.filter(s => !s.mandatory);

  let output = `[AVAILABLE SKILLS - REVIEW BEFORE STARTING]

MANDATORY SKILLS (violating these is a critical error):
${mandatory.map(s => `- ${s.name}: ${s.description}`).join("\n")}

OTHER SKILLS:
${optional.map(s => `- ${s.name}: ${s.description}`).join("\n")}

SKILL RULES:
- BEFORE any build/test/commit action, check if a skill covers it
- Use \`skill invoke <name>\` or follow skill instructions
- Mandatory skills MUST be used - direct commands (dotnet build, dotnet test, git add .) are FORBIDDEN
`;

  return output;
}

class RalphLoop {
  private config: Config;
  private prompt: string = "";
  private iteration: number = 0;
  private reviewFile: string = "";
  private startTime: number = Date.now();
  private toolPatterns: string[] = [];
  private fileChanges: string[] = [];
  private iterationMetrics: IterationMetrics[] = [];
  private lastCommitHash: string = "";
  private isActive: boolean = true;
  private skillsPrompt: string = "";

  constructor(config: Config) {
    this.config = config;
    this.skillsPrompt = formatSkillsForPrompt(discoverSkills());
    this.setupSignalHandlers();
  }

  private setupSignalHandlers() {
    // Bun uses the standard process signal handlers
    process.on("SIGINT", async () => {
      console.log(`\n${CONSTANTS.COLOR.RED}🛑 Ralph loop interrupted at iteration ${this.iteration}${CONSTANTS.COLOR.RESET}`);
      await this.cleanup("interrupted");
      process.exit(0);
    });
  }

  // --- Initialization ---

  public async init() {
    // 1. Resolve Prompt
    if (this.config.promptFile) {
      const f = Bun.file(this.config.promptFile);
      if (!(await f.exists())) {
        console.error(`${CONSTANTS.COLOR.RED}❌ Error: Prompt file not found: ${this.config.promptFile}${CONSTANTS.COLOR.RESET}`);
        process.exit(1);
      }
      this.prompt = (await f.text()).trim();
    } else {
      this.prompt = this.config.promptParts.join(" ");
    }

    // Handle stdin piping if no prompt args
    // Bun.stdin.stream() is the native way, but checking isTTY is easiest via process.stdin
    if (!this.prompt && !process.stdin.isTTY) {
      this.prompt = await Bun.stdin.text();
    }

    if (!this.prompt) {
      this.showHelp();
      process.exit(1);
    }

    // 2. Setup State
    // Create state dir
    const stateDir = dirname(CONSTANTS.STATE_FILE);
    await import("fs").then(fs => fs.mkdirSync(stateDir, { recursive: true }));

    // Capture initial commit hash for tracking new commits per iteration
    this.lastCommitHash = this.getCurrentCommitHash();

    if (this.config.learningMode) {
      await import("fs").then(fs => fs.mkdirSync(CONSTANTS.LEARNING_DIR, { recursive: true }));
      const timestamp = new Date().toISOString().replace(/[:.]/g, "-").slice(0, 15);
      this.reviewFile = join(CONSTANTS.LEARNING_DIR, `review-${timestamp}.md`);
      await this.initializeReviewFile();
    }

    await this.updateStateFile();
    this.printStartupBanner();
  }

  private async initializeReviewFile() {
    const content = `# Ralph Loop Learning Review

Generated: ${new Date().toISOString()}
Prompt: ${this.prompt}
Max Iterations: ${this.config.maxIterations > 0 ? this.config.maxIterations : "unlimited"}
Completion Promise: ${this.config.completionPromise || "none"}

---

## Summary
## Iteration Metrics
## Suggested Skills
## Tool Usage Patterns
## File Modification Patterns
## Successful Strategies
## Failed Approaches
## Proposed Improvements
### To ralph-loop.sh
### To Prompts

---

## Iteration Log
`;
    await Bun.write(this.reviewFile, content);
  }

  private async updateStateFile() {
    const yaml = `---
active: ${this.isActive}
iteration: ${this.iteration}
max_iterations: ${this.config.maxIterations}
completion_promise: ${this.config.completionPromise ? `"${this.config.completionPromise}"` : "null"}
started_at: "${new Date(this.startTime).toISOString()}"
---

${this.prompt}
`;
    await Bun.write(CONSTANTS.STATE_FILE, yaml);
  }

  private printStartupBanner() {
    const skillCount = discoverSkills().length;
    const mandatoryCount = discoverSkills().filter(s => s.mandatory).length;
    console.log(`
${CONSTANTS.COLOR.CYAN}🔄 Ralph loop activated for Copilot CLI! (Bun Engine)${CONSTANTS.COLOR.RESET}

Max iterations: ${this.config.maxIterations > 0 ? this.config.maxIterations : "unlimited (infinite)"}
Completion promise: ${this.config.completionPromise ? this.config.completionPromise : "none (runs forever)"}
Model: ${this.config.model || "default"}
Delay between iterations: ${this.config.delay}s
Skills loaded: ${skillCount} (${mandatoryCount} mandatory)

Press Ctrl+C to cancel at any time.

To monitor: cat ${CONSTANTS.STATE_FILE}
${this.config.learningMode ? `Learning mode: ENABLED - Review file: ${this.reviewFile}` : ""}
═══════════════════════════════════════════════════════════`);
  }

  // --- Git & Metrics Helpers ---

  private getCurrentCommitHash(): string {
    const proc = Bun.spawnSync(["git", "rev-parse", "HEAD"]);
    return proc.success ? proc.stdout.toString().trim() : "";
  }

  private getNewCommits(sinceHash: string): Array<{ hash: string; message: string }> {
    if (!sinceHash) return [];
    const proc = Bun.spawnSync(["git", "log", `${sinceHash}..HEAD`, "--oneline", "--no-decorate"]);
    if (!proc.success) return [];
    return proc.stdout.toString().trim().split("\n").filter(Boolean).map(line => {
      const [hash, ...rest] = line.split(" ");
      return { hash, message: rest.join(" ") };
    });
  }

  private detectPhaseFromCommits(commits: Array<{ message: string }>): string | null {
    for (const commit of commits) {
      // Match patterns like "Phase 1", "phase 2", "PHASE_1_DONE"
      const phaseMatch = commit.message.match(/phase[\s_-]*(\d+)/i);
      if (phaseMatch) return `Phase ${phaseMatch[1]}`;
      
      // Match conventional commit scopes like "feat(core):", "test(piv):"
      const scopeMatch = commit.message.match(/^(?:feat|fix|test|refactor|docs)\(([^)]+)\)/i);
      if (scopeMatch) return scopeMatch[1];
    }
    return null;
  }

  private getFileChangeStats(baseHash: string): { filesChanged: number; linesAdded: number; linesRemoved: number; fileList: string[] } {
    let linesAdded = 0, linesRemoved = 0, filesChanged = 0;
    let fileList: string[] = [];

    // Strategy: Check both uncommitted changes AND committed changes since baseHash
    // 1. First get uncommitted changes (working tree vs HEAD)
    const uncommittedProc = Bun.spawnSync(["git", "diff", "--shortstat", "HEAD"]);
    if (uncommittedProc.success) {
      const stat = uncommittedProc.stdout.toString();
      const filesMatch = stat.match(/(\d+)\s+file/);
      const addMatch = stat.match(/(\d+)\s+insertion/);
      const delMatch = stat.match(/(\d+)\s+deletion/);
      filesChanged = filesMatch ? parseInt(filesMatch[1], 10) : 0;
      linesAdded = addMatch ? parseInt(addMatch[1], 10) : 0;
      linesRemoved = delMatch ? parseInt(delMatch[1], 10) : 0;
    }

    const uncommittedFilesProc = Bun.spawnSync(["git", "diff", "--name-only", "HEAD"]);
    if (uncommittedFilesProc.success) {
      fileList = uncommittedFilesProc.stdout.toString().trim().split("\n").filter(Boolean);
    }

    // 2. If we have a baseHash and it differs from HEAD, also get committed changes
    if (baseHash) {
      const currentHash = this.getCurrentCommitHash();
      if (baseHash !== currentHash) {
        const commitFilesProc = Bun.spawnSync(["git", "diff", "--name-only", `${baseHash}..HEAD`]);
        if (commitFilesProc.success) {
          const committedFiles = commitFilesProc.stdout.toString().trim().split("\n").filter(Boolean);
          // Merge file lists, avoiding duplicates
          fileList = [...new Set([...fileList, ...committedFiles])];
        }

        const commitStatProc = Bun.spawnSync(["git", "diff", "--shortstat", `${baseHash}..HEAD`]);
        if (commitStatProc.success) {
          const stat = commitStatProc.stdout.toString();
          const addMatch = stat.match(/(\d+)\s+insertion/);
          const delMatch = stat.match(/(\d+)\s+deletion/);
          // Only add line counts (file count derived from deduplicated fileList)
          linesAdded += addMatch ? parseInt(addMatch[1], 10) : 0;
          linesRemoved += delMatch ? parseInt(delMatch[1], 10) : 0;
        }
      }
    }

    // Derive file count from deduplicated file list to avoid double-counting
    filesChanged = fileList.length;

    return { filesChanged, linesAdded, linesRemoved, fileList };
  }

  private captureIterationMetrics(iter: number, durationSeconds: number): IterationMetrics {
    const newCommits = this.getNewCommits(this.lastCommitHash);
    const phase = this.detectPhaseFromCommits(newCommits);
    const commitMessage = newCommits.length > 0 ? newCommits[0].message : null;
    const stats = this.getFileChangeStats(this.lastCommitHash);
    
    // Update last commit hash for next iteration
    this.lastCommitHash = this.getCurrentCommitHash();

    return {
      iteration: iter,
      durationSeconds,
      phase,
      commitMessage,
      ...stats,
    };
  }

  private formatDuration(seconds: number): string {
    if (seconds < 60) return `${seconds}s`;
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    if (mins < 60) return `${mins}m ${secs}s`;
    const hours = Math.floor(mins / 60);
    const remainMins = mins % 60;
    return `${hours}h ${remainMins}m`;
  }

  // --- Learning Logic ---

  private async captureIterationLearning(iter: number, output: string, logFile: string, metrics: IterationMetrics) {
    if (!this.config.learningMode) return;

    // Store metrics
    this.iterationMetrics.push(metrics);

    const snippet = output.split("\n").slice(0, 50).join("\n");
    const truncated = output.split("\n").length > 50;
    
    // Build metrics summary for log entry
    const metricsLine = `Duration: ${this.formatDuration(metrics.durationSeconds)} | Files: ${metrics.filesChanged} | +${metrics.linesAdded}/-${metrics.linesRemoved}`;
    const phaseLine = metrics.phase ? `Phase: ${metrics.phase}` : "";
    const commitLine = metrics.commitMessage ? `Commit: "${metrics.commitMessage}"` : "";
    
    const entry = `
### Iteration ${iter}

**${metricsLine}**${phaseLine ? `\n${phaseLine}` : ""}${commitLine ? `\n${commitLine}` : ""}

\`\`\`
${snippet}
${truncated ? `... (truncated, see ${logFile} for full output)` : ""}
\`\`\`
`;
    // Append to file
    const existingContent = await Bun.file(this.reviewFile).text();
    await Bun.write(this.reviewFile, existingContent + entry);

    // Track Tools (regex)
    const toolRegex = /(bash|grep|view|edit|create|glob|git)/g;
    const matches = output.match(toolRegex);
    if (matches) {
      const counts: Record<string, number> = {};
      matches.forEach((m) => { counts[m] = (counts[m] || 0) + 1; });
      const topTools = Object.entries(counts)
        .sort(([, a], [, b]) => b - a)
        .slice(0, 5)
        .map(([k, v]) => `   - ${v} ${k}`)
        .join("\n");
      
      this.toolPatterns.push(`Iteration ${iter}:\n${topTools}`);
    }

    // Track File Changes via Git (legacy format for backward compat)
    if (metrics.fileList.length > 0) {
      const files = metrics.fileList.slice(0, 10).map(f => `   - ${f}`).join("\n");
      this.fileChanges.push(`Iteration ${iter}:\n${files}`);
    }
  }

  private async generateLearningSummary(endReason: string) {
    if (!this.config.learningMode) return;

    const duration = Math.floor((Date.now() - this.startTime) / 1000);
    const toolSummary = this.toolPatterns.join("\n");
    const fileSummary = this.fileChanges.join("\n");

    // Helper to escape pipe characters for markdown tables
    const escapeTableCell = (s: string) => s.replace(/\|/g, "\\|");

    // Build iteration metrics table for analysis prompt
    const metricsTable = this.iterationMetrics.length > 0 
      ? `| Iter | Duration | Phase | Files | +Lines | -Lines | Commit |
|------|----------|-------|-------|--------|--------|--------|
${this.iterationMetrics.map(m => {
  const commitCell = m.commitMessage 
    ? `"${escapeTableCell(m.commitMessage.slice(0, 40))}${m.commitMessage.length > 40 ? "..." : ""}"`
    : "-";
  return `| ${m.iteration} | ${this.formatDuration(m.durationSeconds)} | ${escapeTableCell(m.phase || "-")} | ${m.filesChanged} | +${m.linesAdded} | -${m.linesRemoved} | ${commitCell} |`;
}).join("\n")}`
      : "No metrics captured";

    // Calculate aggregates
    const totalFiles = this.iterationMetrics.reduce((sum, m) => sum + m.filesChanged, 0);
    const totalAdded = this.iterationMetrics.reduce((sum, m) => sum + m.linesAdded, 0);
    const totalRemoved = this.iterationMetrics.reduce((sum, m) => sum + m.linesRemoved, 0);
    const avgDuration = this.iterationMetrics.length > 0 
      ? Math.floor(this.iterationMetrics.reduce((sum, m) => sum + m.durationSeconds, 0) / this.iterationMetrics.length)
      : 0;
    const phases = [...new Set(this.iterationMetrics.map(m => m.phase).filter((p): p is string => p !== null))];

    const analysisPrompt = `Analyze this Ralph Loop session and update the review file.

Session Details:
- Iterations: ${this.iteration}
- Total Duration: ${this.formatDuration(duration)}
- Average Iteration: ${this.formatDuration(avgDuration)}
- Success: ${endReason}
- Prompt: ${this.prompt}

Aggregate Stats:
- Total Files Changed: ${totalFiles}
- Total Lines Added: ${totalAdded}
- Total Lines Removed: ${totalRemoved}
- Phases Worked: ${phases.length > 0 ? phases.join(", ") : "none detected"}

Iteration Metrics:
${metricsTable}

Tool Patterns Observed:
${toolSummary}

Files Changed:
${fileSummary}

Review file to update: ${this.reviewFile}

Please analyze the iteration logs in ./docs/ralph-loop/iteration-*.log and:
1. Fill in the '## Summary' section with key stats
2. Fill in the '## Iteration Metrics' section with the metrics table above
3. Under '## Suggested Skills', propose skills
4. Under '## Tool Usage Patterns', document frequent combinations
5. Under '## Successful Strategies', document what worked
6. Under '## Proposed Improvements', suggest script/prompt changes

IMPORTANT: Write all suggestions to ${this.reviewFile}. Do NOT auto-apply anything.
Output <promise>ANALYSIS_COMPLETE</promise> when done.`;

    console.log(`\n${CONSTANTS.COLOR.BLUE}🧠 Learning mode: Generating session analysis...${CONSTANTS.COLOR.RESET}\n`);

    const copilotArgs = ["-p", analysisPrompt, "--allow-all-tools"];
    if (this.config.model) copilotArgs.push("--model", this.config.model);

    try {
        await runCopilotWithTee(copilotArgs, join(CONSTANTS.LEARNING_DIR, "analysis.log"));
        console.log(`\n${CONSTANTS.COLOR.GREEN}📝 Learning review file ready: ${this.reviewFile}${CONSTANTS.COLOR.RESET}`);
    } catch (e) {
        console.error("Error running learning analysis:", e);
    }
  }

  private async cleanup(reason: string) {
    this.isActive = false;
    await this.updateStateFile();
    await this.generateLearningSummary(reason);
  }

  // --- The Loop ---

  public async start() {
    await this.init();

    while (true) {
      this.iteration++;
      const iterationStartTime = Date.now();

      if (this.config.maxIterations > 0 && this.iteration > this.config.maxIterations) {
        console.log(`\n${CONSTANTS.COLOR.RED}🛑 Ralph loop: Max iterations (${this.config.maxIterations}) reached.${CONSTANTS.COLOR.RESET}`);
        this.iteration--; 
        await this.cleanup("max_iterations_reached");
        return;
      }

      await this.updateStateFile();

      console.log(`
${CONSTANTS.COLOR.CYAN}🔄 ═══════════════════════════════════════════════════════════
🔄 Ralph iteration ${this.iteration} starting...
🔄 ═══════════════════════════════════════════════════════════${CONSTANTS.COLOR.RESET}
`);

      const logFile = `./docs/ralph-loop/iteration-${this.iteration}.log`;

      const iterationPrompt = `${this.skillsPrompt}
${this.prompt}

---
[Ralph Loop Context]
Iteration: ${this.iteration}
${this.config.completionPromise ? `Output <promise>${this.config.completionPromise}</promise> ONLY when the objective is fully verified complete.` : ""}
${this.config.maxIterations > 0 ? `Max iterations: ${this.config.maxIterations}` : ""}

[AUTONOMY DIRECTIVES]
1. You are in NON-INTERACTIVE mode. The user is not present.
2. NEVER ask questions, NEVER ask for clarification, and NEVER say "Let me know if you want me to...".
3. If a decision is ambiguous, pick the most standard/reasonable option and EXECUTE it immediately.
4. Use "git" to explore the codebase if you are lost.
5. Check your previous work in files and git history. Continue from where you left off.
6. REVIEW THE SKILLS LIST ABOVE before any build/test/commit operation.
---`;

      const copilotArgs = ["-p", iterationPrompt, "--allow-all-tools"];
      if (this.config.model) copilotArgs.push("--model", this.config.model);

      // Execute Copilot
      const output = await runCopilotWithTee(copilotArgs, logFile);
      
      // Capture metrics after iteration completes
      const iterationDuration = Math.floor((Date.now() - iterationStartTime) / 1000);
      const metrics = this.captureIterationMetrics(this.iteration, iterationDuration);
      
      // Print iteration summary
      console.log(`\n${CONSTANTS.COLOR.YELLOW}📊 Iteration ${this.iteration}: ${this.formatDuration(iterationDuration)} | ${metrics.filesChanged} files | +${metrics.linesAdded}/-${metrics.linesRemoved} lines${metrics.phase ? ` | ${metrics.phase}` : ""}${CONSTANTS.COLOR.RESET}`);
      
      await this.captureIterationLearning(this.iteration, output, logFile, metrics);

      // Check Promise
      if (this.config.completionPromise) {
        const match = output.match(/<promise>(.*?)<\/promise>/s);
        if (match) {
          const promiseText = match[1].trim();
          if (promiseText === this.config.completionPromise) {
            console.log(`
${CONSTANTS.COLOR.GREEN}✅ ═══════════════════════════════════════════════════════════
✅ Ralph loop: Detected <promise>${this.config.completionPromise}</promise>
✅ Task completed in ${this.iteration} iterations!
✅ ═══════════════════════════════════════════════════════════${CONSTANTS.COLOR.RESET}`);
            await this.cleanup("completed");
            return;
          }
        }
      }

      console.log(`\n🔄 Iteration ${this.iteration} complete. Waiting ${this.config.delay}s...`);
      await sleep(this.config.delay * 1000);
    }
  }

  public showHelp() {
    console.log(`
Ralph Loop for Copilot CLI (Bun Version)

USAGE:
  bun ralph-loop.ts [PROMPT...] [OPTIONS]
  ./ralph-loop.ts [PROMPT...] [OPTIONS]

ARGUMENTS:
  PROMPT...    Initial prompt to start the loop

OPTIONS:
  --max-iterations <n>           Maximum iterations (default: 0 = unlimited)
  --completion-promise '<text>'  Phrase signaling completion
  --delay <seconds>              Delay between iterations (default: 2)
  --learn                        Enable learning mode
  --prompt-file <file>           Read prompt from file
  --model <model>                Copilot model to use
  -h, --help                     Show this help message
    `);
  }
}

// --- Entry Point ---

function parseArgs(): Config {
  const args = Bun.argv.slice(2);
  const config: Config = {
    promptParts: [],
    maxIterations: 0,
    completionPromise: null,
    delay: 2,
    learningMode: false,
    promptFile: null,
    model: null,
  };

  for (let i = 0; i < args.length; i++) {
    switch (args[i]) {
      case "-h":
      case "--help":
        new RalphLoop(config).showHelp();
        process.exit(0);
      case "--max-iterations":
        if (i + 1 >= args.length || args[i + 1].startsWith("-")) {
          console.error("Error: --max-iterations requires a number");
          process.exit(1);
        }
        config.maxIterations = parseInt(args[++i], 10);
        if (isNaN(config.maxIterations)) {
          console.error("Error: --max-iterations must be a valid number");
          process.exit(1);
        }
        break;
      case "--completion-promise":
        if (i + 1 >= args.length || args[i + 1] === "") {
          console.error("Error: --completion-promise requires a non-empty value");
          process.exit(1);
        }
        config.completionPromise = args[++i];
        break;
      case "--delay":
        if (i + 1 >= args.length || args[i + 1].startsWith("-")) {
          console.error("Error: --delay requires a number");
          process.exit(1);
        }
        config.delay = parseInt(args[++i], 10);
        if (isNaN(config.delay) || config.delay < 0) {
          console.error("Error: --delay must be a valid non-negative number");
          process.exit(1);
        }
        break;
      case "--learn":
        config.learningMode = true;
        break;
      case "--prompt-file":
        if (i + 1 >= args.length) {
          console.error("Error: --prompt-file requires a file path");
          process.exit(1);
        }
        config.promptFile = args[++i];
        break;
      case "--model":
        if (i + 1 >= args.length) {
          console.error("Error: --model requires a model name");
          process.exit(1);
        }
        config.model = args[++i];
        break;
      default:
        if (args[i].startsWith("-")) {
          console.error(`Unknown option: ${args[i]}`);
          process.exit(1);
        }
        config.promptParts.push(args[i]);
    }
  }
  return config;
}

try {
  const config = parseArgs();
  const loop = new RalphLoop(config);
  await loop.start();
} catch (e) {
  console.error((e as Error).message);
  process.exit(1);
}