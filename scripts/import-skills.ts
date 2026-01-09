#!/usr/bin/env bun

/**
 * import-skills.ts - Import skills/agents/prompts from GitHub repositories
 *
 * Flexible importer that can discover and import:
 * - Skills (directories with SKILL.md, README.md, or instruction files)
 * - Agents (markdown files with agent prompts)
 * - Prompt files (standalone .md files with prompts/instructions)
 *
 * Works with various repo structures - not limited to superpowers format.
 *
 * Usage:
 *   bun scripts/import-skills.ts <github-repo-url> [options]
 *
 * Examples:
 *   bun scripts/import-skills.ts https://github.com/obra/superpowers
 *   bun scripts/import-skills.ts obra/superpowers --skills brainstorming,tdd
 *   bun scripts/import-skills.ts some-user/prompt-library --list
 *   bun scripts/import-skills.ts owner/repo --scan  # Deep scan for importable content
 */

import { mkdir, writeFile } from "fs/promises";
import { join } from "path";

interface Options {
  repo: string | null;
  list: boolean;
  scan: boolean;
  skills: string[] | null;
  agents: string[] | null;
  prompts: string[] | null;
  skipSkills: boolean;
  skipAgents: boolean;
  skipPrompts: boolean;
  dryRun: boolean;
  outputDir: string;
  skillDirs: string[];
  agentDirs: string[];
}

interface RepoInfo {
  owner: string;
  repo: string;
}

interface GitHubFile {
  name: string;
  type: "file" | "dir";
  path: string;
}

interface Skill {
  name: string;
  basePath: string;
  path: string;
  files: string[];
}

interface Agent {
  name: string;
  basePath: string;
  path: string;
  filename: string;
}

interface PromptFile {
  name: string;
  path: string;
  filename: string;
}

interface ImportResult {
  skill?: string;
  agent?: string;
  prompt?: string;
  files?: string[];
  file?: string;
  status: "imported" | "dry-run" | "failed" | "skipped-exists";
  error?: string;
}

// Known skill file patterns (files that indicate a directory is a skill)
const SKILL_INDICATORS = [
  "SKILL.md",
  "skill.md",
  "README.md",
  "readme.md",
  "PROMPT.md",
  "prompt.md",
  "instructions.md",
  "INSTRUCTIONS.md",
];

// Known agent/prompt file patterns
const AGENT_PATTERNS = [
  /\.agent\.md$/i,
  /agent.*\.md$/i,
  /-agent\.md$/i,
  /_agent\.md$/i,
];

// Directories to skip during scanning
const SKIP_DIRS = [
  "node_modules",
  ".git",
  "dist",
  "build",
  "coverage",
  "__pycache__",
  ".venv",
  "vendor",
];

// Parse command line arguments
function parseArgs(args: string[]): Options {
  const options: Options = {
    repo: null,
    list: false,
    scan: false,
    skills: null,
    agents: null,
    prompts: null,
    skipSkills: false,
    skipAgents: false,
    skipPrompts: false,
    dryRun: false,
    outputDir: process.cwd(),
    skillDirs: [],
    agentDirs: [],
  };

  for (let i = 0; i < args.length; i++) {
    const arg = args[i];

    if (arg === "--list") {
      options.list = true;
    } else if (arg === "--scan") {
      options.scan = true;
    } else if (arg === "--skills") {
      options.skills = args[++i]?.split(",").map((s) => s.trim()) || [];
    } else if (arg === "--agents") {
      options.agents = args[++i]?.split(",").map((s) => s.trim()) || [];
    } else if (arg === "--prompts") {
      options.prompts = args[++i]?.split(",").map((s) => s.trim()) || [];
    } else if (arg === "--skip-skills") {
      options.skipSkills = true;
    } else if (arg === "--skip-agents") {
      options.skipAgents = true;
    } else if (arg === "--skip-prompts") {
      options.skipPrompts = true;
    } else if (arg === "--dry-run") {
      options.dryRun = true;
    } else if (arg === "--output-dir") {
      options.outputDir = args[++i];
    } else if (arg === "--skill-dirs") {
      // Custom directories to look for skills
      options.skillDirs = args[++i]?.split(",").map((s) => s.trim()) || [];
    } else if (arg === "--agent-dirs") {
      // Custom directories to look for agents
      options.agentDirs = args[++i]?.split(",").map((s) => s.trim()) || [];
    } else if (!arg.startsWith("--") && !options.repo) {
      options.repo = arg;
    }
  }

  return options;
}

// Parse GitHub repo from various formats
function parseGitHubRepo(input: string | null): RepoInfo | null {
  if (!input) return null;

  // Handle full URLs
  const urlMatch = input.match(/github\.com\/([^\/]+)\/([^\/\s]+)/);
  if (urlMatch) {
    return { owner: urlMatch[1], repo: urlMatch[2].replace(/\.git$/, "") };
  }

  // Handle owner/repo format
  const shortMatch = input.match(/^([^\/]+)\/([^\/]+)$/);
  if (shortMatch) {
    return { owner: shortMatch[1], repo: shortMatch[2] };
  }

  return null;
}

// Fetch JSON from GitHub API
async function fetchGitHubApi<T>(urlPath: string): Promise<T> {
  const token = process.env.GITHUB_TOKEN || process.env.GH_TOKEN;
  const headers: Record<string, string> = {
    "User-Agent": "import-skills-script",
    Accept: "application/vnd.github.v3+json",
  };

  if (token) {
    headers["Authorization"] = `token ${token}`;
  }

  const response = await fetch(`https://api.github.com${urlPath}`, { headers });

  if (!response.ok) {
    throw new Error(`GitHub API error: ${response.status}`);
  }

  return response.json();
}

// Fetch raw file content from GitHub
async function fetchRawFile(
  owner: string,
  repo: string,
  filePath: string,
  branch = "main"
): Promise<string> {
  const url = `https://raw.githubusercontent.com/${owner}/${repo}/${branch}/${filePath}`;
  const response = await fetch(url);

  if (response.status === 404 && branch === "main") {
    // Try 'master' branch if 'main' failed
    return fetchRawFile(owner, repo, filePath, "master");
  }

  if (!response.ok) {
    throw new Error(`Failed to fetch ${filePath}: ${response.status}`);
  }

  return response.text();
}

// List directory contents from GitHub
async function listGitHubDir(
  owner: string,
  repo: string,
  dirPath: string
): Promise<GitHubFile[]> {
  try {
    const contents = await fetchGitHubApi<GitHubFile[]>(
      `/repos/${owner}/${repo}/contents/${dirPath}`
    );
    return Array.isArray(contents) ? contents : [];
  } catch {
    return [];
  }
}

// Discover skills in a repository
async function discoverSkills(
  owner: string,
  repo: string,
  customDirs: string[] = []
): Promise<Skill[]> {
  const skills: Skill[] = [];

  // Default skill locations + custom dirs
  const skillDirs = [
    "skills",
    ".claude/skills",
    "claude/skills",
    "prompts",
    "agents",
    "workflows",
    "templates",
    ...customDirs,
  ];

  for (const baseDir of skillDirs) {
    const contents = await listGitHubDir(owner, repo, baseDir);

    for (const item of contents) {
      if (item.type === "dir") {
        // Check if directory contains any skill indicator file
        const skillFiles = await listGitHubDir(
          owner,
          repo,
          `${baseDir}/${item.name}`
        );
        const hasSkillIndicator = skillFiles.some((f) =>
          SKILL_INDICATORS.includes(f.name)
        );

        if (hasSkillIndicator) {
          skills.push({
            name: item.name,
            basePath: baseDir,
            path: `${baseDir}/${item.name}`,
            files: skillFiles.filter((f) => f.type === "file").map((f) => f.name),
          });
        }
      }
    }
  }

  return skills;
}

// Deep scan repository for skill-like directories
async function deepScanForSkills(
  owner: string,
  repo: string,
  currentPath = "",
  depth = 0,
  maxDepth = 3
): Promise<Skill[]> {
  if (depth > maxDepth) return [];

  const skills: Skill[] = [];
  const contents = await listGitHubDir(owner, repo, currentPath);

  for (const item of contents) {
    if (item.type === "dir") {
      // Skip known non-skill directories
      if (SKIP_DIRS.includes(item.name)) continue;

      const dirPath = currentPath ? `${currentPath}/${item.name}` : item.name;
      const dirContents = await listGitHubDir(owner, repo, dirPath);

      // Check if this directory looks like a skill
      const hasSkillIndicator = dirContents.some((f) =>
        SKILL_INDICATORS.includes(f.name)
      );

      if (hasSkillIndicator) {
        skills.push({
          name: item.name,
          basePath: currentPath || ".",
          path: dirPath,
          files: dirContents.filter((f) => f.type === "file").map((f) => f.name),
        });
      } else {
        // Recurse into subdirectories
        const subSkills = await deepScanForSkills(
          owner,
          repo,
          dirPath,
          depth + 1,
          maxDepth
        );
        skills.push(...subSkills);
      }
    }
  }

  return skills;
}

// Discover agents in a repository
async function discoverAgents(
  owner: string,
  repo: string,
  customDirs: string[] = []
): Promise<Agent[]> {
  const agents: Agent[] = [];

  // Default agent locations + custom dirs
  const agentDirs = [
    "agents",
    ".github/agents",
    "github/agents",
    ".claude/agents",
    "prompts/agents",
    ...customDirs,
  ];

  for (const baseDir of agentDirs) {
    const contents = await listGitHubDir(owner, repo, baseDir);

    for (const item of contents) {
      if (item.type === "file" && item.name.endsWith(".md")) {
        agents.push({
          name: item.name.replace(/\.(agent\.)?md$/, ""),
          basePath: baseDir,
          path: `${baseDir}/${item.name}`,
          filename: item.name,
        });
      }
    }
  }

  return agents;
}

// Discover standalone prompt files (not in skill directories)
async function discoverPromptFiles(
  owner: string,
  repo: string
): Promise<PromptFile[]> {
  const prompts: PromptFile[] = [];

  // Check root and common prompt locations
  const promptLocations = ["", "prompts", "templates", "instructions"];

  for (const location of promptLocations) {
    const contents = await listGitHubDir(owner, repo, location);

    for (const item of contents) {
      if (item.type === "file" && item.name.endsWith(".md")) {
        // Skip READMEs and common non-prompt files
        const lowerName = item.name.toLowerCase();
        if (
          lowerName === "readme.md" ||
          lowerName === "changelog.md" ||
          lowerName === "contributing.md" ||
          lowerName === "license.md"
        ) {
          continue;
        }

        prompts.push({
          name: item.name.replace(/\.md$/, ""),
          path: location ? `${location}/${item.name}` : item.name,
          filename: item.name,
        });
      }
    }
  }

  return prompts;
}

// Import a skill
async function importSkill(
  owner: string,
  repo: string,
  skill: Skill,
  outputDir: string,
  dryRun: boolean
): Promise<ImportResult> {
  const targetDir = join(outputDir, ".claude/skills", skill.name);

  console.log(`  Importing skill: ${skill.name}`);

  if (dryRun) {
    console.log(`    Would create: ${targetDir}/`);
    for (const file of skill.files) {
      console.log(`    Would download: ${file}`);
    }
    return { skill: skill.name, files: skill.files, status: "dry-run" };
  }

  // Create directory
  await mkdir(targetDir, { recursive: true });

  const downloadedFiles: string[] = [];

  // Download all files in the skill directory
  for (const file of skill.files) {
    try {
      const content = await fetchRawFile(owner, repo, `${skill.path}/${file}`);
      const targetPath = join(targetDir, file);
      await writeFile(targetPath, content);
      downloadedFiles.push(file);
      console.log(`    Downloaded: ${file}`);
    } catch (e) {
      const error = e instanceof Error ? e.message : String(e);
      console.log(`    Failed to download ${file}: ${error}`);
    }
  }

  return { skill: skill.name, files: downloadedFiles, status: "imported" };
}

// Import an agent
async function importAgent(
  owner: string,
  repo: string,
  agent: Agent,
  outputDir: string,
  dryRun: boolean
): Promise<ImportResult> {
  const targetDir = join(outputDir, ".github/agents");

  // Normalize filename to .agent.md format for Copilot CLI
  let targetFilename = agent.filename;
  if (!targetFilename.includes(".agent.")) {
    targetFilename = agent.name + ".agent.md";
  }

  const targetPath = join(targetDir, targetFilename);

  console.log(`  Importing agent: ${agent.name}`);

  if (dryRun) {
    console.log(`    Would create: ${targetPath}`);
    return { agent: agent.name, status: "dry-run" };
  }

  // Create directory
  await mkdir(targetDir, { recursive: true });

  try {
    const content = await fetchRawFile(owner, repo, agent.path);
    await writeFile(targetPath, content);
    console.log(`    Downloaded: ${targetFilename}`);
    return { agent: agent.name, file: targetFilename, status: "imported" };
  } catch (e) {
    const error = e instanceof Error ? e.message : String(e);
    console.log(`    Failed to download: ${error}`);
    return { agent: agent.name, status: "failed", error };
  }
}

// Import a standalone prompt file
async function importPrompt(
  owner: string,
  repo: string,
  prompt: PromptFile,
  outputDir: string,
  dryRun: boolean
): Promise<ImportResult> {
  // Import as a skill (create directory with the file)
  const targetDir = join(outputDir, ".claude/skills", prompt.name);
  const targetPath = join(targetDir, "SKILL.md");

  console.log(`  Importing prompt as skill: ${prompt.name}`);

  if (dryRun) {
    console.log(`    Would create: ${targetPath}`);
    return { prompt: prompt.name, status: "dry-run" };
  }

  await mkdir(targetDir, { recursive: true });

  try {
    let content = await fetchRawFile(owner, repo, prompt.path);

    // Add YAML frontmatter if missing
    if (!content.startsWith("---")) {
      const description = `Imported from ${prompt.path}`;
      content = `---\nname: ${prompt.name}\ndescription: ${description}\n---\n\n${content}`;
    }

    await writeFile(targetPath, content);
    console.log(`    Created: ${targetPath}`);
    return { prompt: prompt.name, file: "SKILL.md", status: "imported" };
  } catch (e) {
    const error = e instanceof Error ? e.message : String(e);
    console.log(`    Failed: ${error}`);
    return { prompt: prompt.name, status: "failed", error };
  }
}

// Print help
function printHelp(): void {
  console.log(`
import-skills.ts - Import skills/agents/prompts from GitHub repositories

Works with various repo structures - automatically discovers importable content.

Usage:
  bun scripts/import-skills.ts <github-repo-url> [options]

Examples:
  bun scripts/import-skills.ts https://github.com/obra/superpowers
  bun scripts/import-skills.ts obra/superpowers --skills brainstorming,tdd
  bun scripts/import-skills.ts some-user/prompt-library --list
  bun scripts/import-skills.ts owner/repo --scan  # Deep scan for content

Options:
  --list              List available skills/agents/prompts without importing
  --scan              Deep scan repository for skill-like directories
  --skills <names>    Comma-separated list of skills to import (default: all)
  --agents <names>    Comma-separated list of agents to import (default: all)
  --prompts <names>   Comma-separated list of prompt files to import
  --skip-skills       Don't import any skills
  --skip-agents       Don't import any agents
  --skip-prompts      Don't import standalone prompt files
  --dry-run           Show what would be imported without actually importing
  --output-dir <dir>  Base directory for output (default: current directory)
  --skill-dirs <dirs> Additional directories to search for skills (comma-separated)
  --agent-dirs <dirs> Additional directories to search for agents (comma-separated)

Skill Detection:
  Directories containing any of these files are treated as skills:
  - SKILL.md, skill.md
  - README.md, readme.md
  - PROMPT.md, prompt.md
  - instructions.md, INSTRUCTIONS.md

Agent Detection:
  Files matching these patterns in agent directories:
  - *.agent.md
  - agent*.md, *-agent.md, *_agent.md
`);
}

// Main function
async function main(): Promise<void> {
  const args = process.argv.slice(2);

  if (args.length === 0 || args.includes("--help") || args.includes("-h")) {
    printHelp();
    process.exit(0);
  }

  const options = parseArgs(args);
  const repoInfo = parseGitHubRepo(options.repo);

  if (!repoInfo) {
    console.error(
      "Error: Invalid GitHub repository. Use format: owner/repo or https://github.com/owner/repo"
    );
    process.exit(1);
  }

  const { owner, repo } = repoInfo;
  console.log(`\nScanning repository: ${owner}/${repo}\n`);

  // Discover available content
  let skills: Skill[];
  if (options.scan) {
    console.log("Deep scanning for skills (this may take a moment)...");
    skills = await deepScanForSkills(owner, repo);
  } else {
    console.log("Discovering skills...");
    skills = await discoverSkills(owner, repo, options.skillDirs);
  }

  console.log("Discovering agents...");
  const agents = await discoverAgents(owner, repo, options.agentDirs);

  console.log("Discovering prompt files...");
  const prompts = await discoverPromptFiles(owner, repo);

  // List mode - just show what's available
  if (options.list) {
    console.log(`\n=== Available Skills (${skills.length}) ===`);
    for (const skill of skills) {
      console.log(`  ${skill.name} (${skill.basePath})`);
      console.log(`    Files: ${skill.files.join(", ")}`);
    }

    console.log(`\n=== Available Agents (${agents.length}) ===`);
    for (const agent of agents) {
      console.log(`  ${agent.name}`);
      console.log(`    Path: ${agent.path}`);
    }

    if (prompts.length > 0) {
      console.log(`\n=== Standalone Prompt Files (${prompts.length}) ===`);
      for (const prompt of prompts) {
        console.log(`  ${prompt.name}`);
        console.log(`    Path: ${prompt.path}`);
      }
    }

    console.log("\nTo import specific items:");
    if (skills.length > 0) {
      console.log(
        `  bun scripts/import-skills.ts ${owner}/${repo} --skills ${skills
          .slice(0, 2)
          .map((s) => s.name)
          .join(",")}`
      );
    }
    if (agents.length > 0) {
      console.log(
        `  bun scripts/import-skills.ts ${owner}/${repo} --agents ${agents
          .slice(0, 2)
          .map((a) => a.name)
          .join(",")}`
      );
    }
    if (prompts.length > 0) {
      console.log(
        `  bun scripts/import-skills.ts ${owner}/${repo} --prompts ${prompts
          .slice(0, 2)
          .map((p) => p.name)
          .join(",")}`
      );
    }
    return;
  }

  // Filter skills to import
  let skillsToImport = skills;
  if (options.skipSkills) {
    skillsToImport = [];
  } else if (options.skills) {
    skillsToImport = skills.filter((s) => options.skills!.includes(s.name));
  }

  // Filter agents to import
  let agentsToImport = agents;
  if (options.skipAgents) {
    agentsToImport = [];
  } else if (options.agents) {
    agentsToImport = agents.filter((a) => options.agents!.includes(a.name));
  }

  // Filter prompts to import (default: none unless specified)
  let promptsToImport: PromptFile[] = [];
  if (!options.skipPrompts && options.prompts) {
    promptsToImport = prompts.filter((p) => options.prompts!.includes(p.name));
  }

  // Import
  const results = {
    skills: [] as ImportResult[],
    agents: [] as ImportResult[],
    prompts: [] as ImportResult[],
  };

  if (skillsToImport.length > 0) {
    console.log(`\n=== Importing ${skillsToImport.length} Skills ===`);
    for (const skill of skillsToImport) {
      const result = await importSkill(
        owner,
        repo,
        skill,
        options.outputDir,
        options.dryRun
      );
      results.skills.push(result);
    }
  }

  if (agentsToImport.length > 0) {
    console.log(`\n=== Importing ${agentsToImport.length} Agents ===`);
    for (const agent of agentsToImport) {
      const result = await importAgent(
        owner,
        repo,
        agent,
        options.outputDir,
        options.dryRun
      );
      results.agents.push(result);
    }
  }

  if (promptsToImport.length > 0) {
    console.log(`\n=== Importing ${promptsToImport.length} Prompts ===`);
    for (const prompt of promptsToImport) {
      const result = await importPrompt(
        owner,
        repo,
        prompt,
        options.outputDir,
        options.dryRun
      );
      results.prompts.push(result);
    }
  }

  // Summary
  console.log("\n=== Summary ===");
  console.log(
    `Skills imported: ${results.skills.filter((r) => r.status === "imported").length}`
  );
  console.log(
    `Agents imported: ${results.agents.filter((r) => r.status === "imported").length}`
  );
  if (results.prompts.length > 0) {
    console.log(
      `Prompts imported: ${results.prompts.filter((r) => r.status === "imported").length}`
    );
  }

  const totalImported =
    results.skills.filter((r) => r.status === "imported").length +
    results.agents.filter((r) => r.status === "imported").length +
    results.prompts.filter((r) => r.status === "imported").length;

  if (!options.dryRun && totalImported > 0) {
    console.log(
      "\n⚠️  IMPORTANT: Imported content may need adaptation for Copilot CLI."
    );
    console.log(
      '   Run the "importing-skills" skill to review and adapt the imports.'
    );
    console.log("   Common adaptations needed:");
    console.log("   - Replace Claude Code specific features (TodoWrite → update_todo)");
    console.log("   - Remove hooks (not supported in Copilot CLI)");
    console.log("   - Remove custom slash commands (not supported)");
    console.log("   - Update tool references for Copilot CLI compatibility");
  }

  // Output JSON for programmatic use
  const jsonOutput = join(options.outputDir, ".import-skills-result.json");
  await writeFile(
    jsonOutput,
    JSON.stringify(
      {
        source: { owner, repo },
        imported: results,
        needsAdaptation: !options.dryRun && totalImported > 0,
      },
      null,
      2
    )
  );
  console.log(`\nResults written to: ${jsonOutput}`);
}

main().catch((e) => {
  console.error("Error:", e.message);
  process.exit(1);
});
