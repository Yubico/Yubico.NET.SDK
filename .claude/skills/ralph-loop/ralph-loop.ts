#!/usr/bin/env bun

import { join, dirname } from "path";

// --- Configuration & Constants ---

interface Config {
  promptParts: string[];
  maxIterations: number;
  completionPromise: string | null;
  delay: number; // seconds
  learningMode: boolean;
  promptFile: string | null;
  model: string | null;
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
    const decoder = new TextDecoder();
    while (true) {
      const { done, value } = await reader.read();
      if (done) break;
      const chunk = decoder.decode(value);
      
      process.stdout.write(chunk); // Stream to terminal
      writer.write(chunk);         // Stream to file
      fullOutput += chunk;
    }
  };

  // Read stdout and stderr concurrently
  if (proc.stdout) readStream(proc.stdout.getReader());
  if (proc.stderr) readStream(proc.stderr.getReader());

  await proc.exited;
  writer.end();
  
  return fullOutput;
}

// --- Main Class ---

class RalphLoop {
  private config: Config;
  private prompt: string = "";
  private iteration: number = 0;
  private reviewFile: string = "";
  private startTime: number = Date.now();
  private toolPatterns: string[] = [];
  private fileChanges: string[] = [];
  private isActive: boolean = true;

  constructor(config: Config) {
    this.config = config;
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
    console.log(`
${CONSTANTS.COLOR.CYAN}🔄 Ralph loop activated for Copilot CLI! (Bun Engine)${CONSTANTS.COLOR.RESET}

Max iterations: ${this.config.maxIterations > 0 ? this.config.maxIterations : "unlimited (infinite)"}
Completion promise: ${this.config.completionPromise ? this.config.completionPromise : "none (runs forever)"}
Model: ${this.config.model || "default"}
Delay between iterations: ${this.config.delay}s

Press Ctrl+C to cancel at any time.

To monitor: cat ${CONSTANTS.STATE_FILE}
${this.config.learningMode ? `Learning mode: ENABLED - Review file: ${this.reviewFile}` : ""}
═══════════════════════════════════════════════════════════`);
  }

  // --- Learning Logic ---

  private async captureIterationLearning(iter: number, output: string, logFile: string) {
    if (!this.config.learningMode) return;

    const snippet = output.split("\n").slice(0, 50).join("\n");
    const truncated = output.split("\n").length > 50;
    
    const entry = `
### Iteration ${iter}

\`\`\`
${snippet}
${truncated ? `... (truncated, see ${logFile} for full output)` : ""}
\`\`\`
`;
    // Append to file
    const file = Bun.file(this.reviewFile);
    // Bun doesn't have a simple "append" string method on Bun.file, 
    // but the writer pattern works, or simple node compat appendFileSync
    const fs = await import("fs");
    fs.appendFileSync(this.reviewFile, entry);

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

    // Track File Changes via Git
    // Using Bun.spawnSync for quick git check
    const gitProc = Bun.spawnSync(["git", "diff", "--name-only"]);
    if (gitProc.success) {
      const diff = gitProc.stdout.toString();
      const files = diff.split("\n").filter(Boolean).slice(0, 10).map(f => `   - ${f}`).join("\n");
      if (files) {
        this.fileChanges.push(`Iteration ${iter}:\n${files}`);
      }
    }
  }

  private async generateLearningSummary(endReason: string) {
    if (!this.config.learningMode) return;

    const duration = Math.floor((Date.now() - this.startTime) / 1000);
    const toolSummary = this.toolPatterns.join("\n");
    const fileSummary = this.fileChanges.join("\n");

    const analysisPrompt = `Analyze this Ralph Loop session and update the review file.

Session Details:
- Iterations: ${this.iteration}
- Duration: ${duration}s
- Success: ${endReason}
- Prompt: ${this.prompt}

Tool Patterns Observed:
${toolSummary}

Files Changed:
${fileSummary}

Review file to update: ${this.reviewFile}

Please analyze the iteration logs in ./docs/ralph-loop/iteration-*.log and:
1. Fill in the '## Summary' section
2. Under '## Suggested Skills', propose skills
3. Under '## Tool Usage Patterns', document frequent combinations
4. Under '## Successful Strategies', document what worked
5. Under '## Proposed Improvements', suggest script/prompt changes

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

      const iterationPrompt = `${this.prompt}

---
[Ralph Loop Context]
Iteration: ${this.iteration}
${this.config.completionPromise ? `To signal completion, output: <promise>${this.config.completionPromise}</promise>` : ""}
${this.config.maxIterations > 0 ? `Max iterations: ${this.config.maxIterations}` : ""}

[AUTONOMY DIRECTIVES]
1. You are in NON-INTERACTIVE mode. The user is not present.
2. NEVER ask questions, NEVER ask for clarification, and NEVER say "Let me know if you want me to...".
3. If a decision is ambiguous, pick the most standard/reasonable option and EXECUTE it immediately.
4. Use "git" to explore the codebase if you are lost.
5. Check your previous work in files and git history. Continue from where you left off.
---`;

      const copilotArgs = ["-p", iterationPrompt, "--allow-all-tools"];
      if (this.config.model) copilotArgs.push("--model", this.config.model);

      // Execute Copilot
      const output = await runCopilotWithTee(copilotArgs, logFile);
      
      await this.captureIterationLearning(this.iteration, output, logFile);

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
        config.maxIterations = parseInt(args[++i], 10);
        break;
      case "--completion-promise":
        config.completionPromise = args[++i];
        break;
      case "--delay":
        config.delay = parseInt(args[++i], 10);
        break;
      case "--learn":
        config.learningMode = true;
        break;
      case "--prompt-file":
        config.promptFile = args[++i];
        break;
      case "--model":
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