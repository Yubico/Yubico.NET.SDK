#!/usr/bin/env bun

/**
 * Jira Cloud REST API v3 - Strict Component Search
 *
 * Design Philosophy:
 * - NO RAW JQL: The Agent provides variables, the script builds the syntax.
 * - Discovery: Added --exclude-status to help find active work.
 * - Robustness: Greedy parsing for multi-word statuses (e.g. "In Progress").
 */

// --- Configuration ---
const JIRA_DOMAIN = Bun.env.JIRA_DOMAIN;
const JIRA_EMAIL = Bun.env.JIRA_EMAIL;
const JIRA_TOKEN = Bun.env.JIRA_TOKEN;

// --- Robust Argument Parsing (Greedy) ---
// Captures "In Progress" as a single value even without quotes
const args = Bun.argv.slice(2);

function getArg(flag: string): string | undefined {
  const index = args.indexOf(flag);
  if (index === -1 || index === args.length - 1) return undefined;

  const values = [];
  for (let i = index + 1; i < args.length; i++) {
    const val = args[i];
    if (val.startsWith("--")) break;
    values.push(val);
  }
  return values.length > 0 ? values.join(" ") : undefined;
}

// --- Inputs ---
const projectKey = getArg("--project");
const statusArg = getArg("--status");          // Include these
const excludeStatusArg = getArg("--exclude-status"); // Exclude these (New!)
const assigneeArg = getArg("--assignee");
const textArg = getArg("--text");
const typeArg = getArg("--type");
const limitArg = getArg("--limit");
const pageTokenArg = getArg("--token");

const maxResults = limitArg ? parseInt(limitArg, 10) : 10; // Default 10 for better discovery

// --- Validation ---
if (!JIRA_DOMAIN || !JIRA_EMAIL || !JIRA_TOKEN) {
  console.error("❌ Error: Missing Environment Variables.");
  process.exit(1);
}

// Guardrail: Prevent dumping the whole database
if (!projectKey && !statusArg && !excludeStatusArg && !assigneeArg && !textArg) {
  console.error("❌ Error: You must provide at least one filter.");
  console.error("Usage: bun run jira-issue-search.ts --project YESDK --exclude-status Closed");
  process.exit(1);
}

// --- Strict JQL Construction ---
const jqlParts: string[] = [];

// 1. Project Scope
if (projectKey) {
  jqlParts.push(`project = "${projectKey}"`);
}

// 2. Issue Type
if (typeArg) {
  jqlParts.push(`issuetype = "${typeArg}"`);
}

// 3. Status Inclusion (e.g. "In Progress")
if (statusArg) {
  if (statusArg.includes(",")) {
    const list = statusArg.split(",").map(s => `"${s.trim()}"`).join(", ");
    jqlParts.push(`status in (${list})`);
  } else {
    jqlParts.push(`status = "${statusArg}"`);
  }
}

// 4. Status Exclusion (e.g. "Closed") - CRITICAL FOR DISCOVERY
if (excludeStatusArg) {
  if (excludeStatusArg.includes(",")) {
    const list = excludeStatusArg.split(",").map(s => `"${s.trim()}"`).join(", ");
    jqlParts.push(`status not in (${list})`);
  } else {
    jqlParts.push(`status != "${excludeStatusArg}"`);
  }
}

// 5. Assignee
if (assigneeArg) {
  if (assigneeArg.toLowerCase() === "me") {
    jqlParts.push(`assignee = currentUser()`);
  } else if (assigneeArg.toLowerCase() === "unassigned") {
    jqlParts.push(`assignee IS EMPTY`);
  } else {
    jqlParts.push(`assignee = "${assigneeArg}"`);
  }
}

// 6. Text Search (Summary/Description)
if (textArg) {
  jqlParts.push(`text ~ "${textArg}"`);
}

// 7. Sort Order (Newest Updated first is better for tracking active work)
const finalJql = jqlParts.join(" AND ") + " ORDER BY updated DESC";

console.error(`[DEBUG] Generated JQL: ${finalJql}`);

// --- Helper: Extract Text from ADF ---
function extractTextFromADF(content: any): string {
  if (!content) return "";
  if (typeof content === "string") return content;
  if (Array.isArray(content)) return content.map(extractTextFromADF).join("\n");
  let text = "";
  if (content.type === "text" && content.text) text += content.text;
  if (content.content) text += extractTextFromADF(content.content);
  return text;
}

// --- API Call ---
const authString = Buffer.from(`${JIRA_EMAIL}:${JIRA_TOKEN}`).toString('base64');
const searchUrl = `https://${JIRA_DOMAIN}/rest/api/3/search/jql`;

const payload: any = {
  jql: finalJql,
  maxResults: maxResults,
  fields: [
    "summary", "status", "issuetype", "priority", "assignee", 
    "description", "comment", "created", "updated", "parent"
  ]
};

if (pageTokenArg) payload.nextPageToken = pageTokenArg;

try {
  const response = await fetch(searchUrl, {
    method: "POST",
    headers: {
      "Authorization": `Basic ${authString}`,
      "Accept": "application/json",
      "Content-Type": "application/json",
    },
    body: JSON.stringify(payload),
  });

  if (!response.ok) {
    const err = await response.text();
    console.error(`❌ Jira API Error (${response.status}): ${err}`);
    process.exit(1);
  }

  const data = await response.json();

  if (!data.issues || data.issues.length === 0) {
    console.log(JSON.stringify({ message: "No issues found", issues: [] }, null, 2));
    process.exit(0);
  }

  const optimizedIssues = data.issues.map((issue: any) => {
    const f = issue.fields;
    const recentComments = f.comment?.comments?.slice(-3).map((c: any) => ({
      author: c.author?.displayName || "Unknown",
      body: extractTextFromADF(c.body),
      created: c.created
    })) || [];

    return {
      key: issue.key,
      type: f.issuetype?.name,
      status: f.status?.name,
      priority: f.priority?.name,
      summary: f.summary,
      assignee: f.assignee?.displayName || "Unassigned",
      description: f.description ? extractTextFromADF(f.description) : "",
      comments: recentComments
    };
  });

  console.log(JSON.stringify({
    meta: {
      count: data.issues.length,
      next_token: data.nextPageToken || null
    },
    issues: optimizedIssues
  }, null, 2));

} catch (error) {
  console.error("Network or Execution Error:", error);
  process.exit(1);
}