#!/usr/bin/env bun

/**
 * Jira Cloud REST API v3 - Parameterized Search (v2)
 *
 * * BREAKING CHANGE UPDATE:
 * - Uses new endpoint: /rest/api/3/search/jql
 * - Uses Cursor Pagination (nextPageToken) instead of Offsets.
 *
 * Capabilities:
 * - Construct safe JQL internally.
 * - Optimized "Context Economy": returns flattened text and limited comments.
 *
 * References:
 * - Search (POST): https://developer.atlassian.com/cloud/jira/platform/rest/v3/api-group-issue-search/#api-rest-api-3-search-jql-post
 */

// --- Configuration ---
const JIRA_DOMAIN = Bun.env.JIRA_DOMAIN;
const JIRA_EMAIL = Bun.env.JIRA_EMAIL;
const JIRA_TOKEN = Bun.env.JIRA_TOKEN;

// --- Argument Parsing ---
const args = Bun.argv.slice(2);
function getArg(flag: string): string | undefined {
  const index = args.indexOf(flag);
  return (index > -1 && args[index + 1]) ? args[index + 1] : undefined;
}

// Filters
const projectKey = getArg("--project");
const statusArg = getArg("--status");
const assigneeArg = getArg("--assignee");
const textArg = getArg("--text");
const typeArg = getArg("--type");

// Pagination (New Cursor Logic)
const limitArg = getArg("--limit");
const pageTokenArg = getArg("--token"); // Replaces --offset
const maxResults = limitArg ? parseInt(limitArg, 10) : 5;

// --- Validation ---
if (!JIRA_DOMAIN || !JIRA_EMAIL || !JIRA_TOKEN) {
  console.error("Error: Missing Environment Variables.");
  process.exit(1);
}

if (!projectKey && !statusArg && !assigneeArg && !textArg && !typeArg) {
  console.error("Error: You must provide at least one search filter.");
  process.exit(1);
}

// --- Internal JQL Construction ---
const jqlParts: string[] = [];

if (projectKey) jqlParts.push(`project = "${projectKey}"`);
if (typeArg) jqlParts.push(`issuetype = "${typeArg}"`);

if (statusArg) {
  if (statusArg.includes(",")) {
    const list = statusArg.split(",").map(s => `"${s.trim()}"`).join(", ");
    jqlParts.push(`status in (${list})`);
  } else {
    jqlParts.push(`status = "${statusArg}"`);
  }
}

if (assigneeArg) {
  if (assigneeArg.toLowerCase() === "me") {
    jqlParts.push(`assignee = currentUser()`);
  } else if (assigneeArg.toLowerCase() === "unassigned") {
    jqlParts.push(`assignee IS EMPTY`);
  } else {
    jqlParts.push(`assignee = "${assigneeArg}"`);
  }
}

if (textArg) {
  jqlParts.push(`text ~ "${textArg}"`);
}

const finalJql = jqlParts.join(" AND ") + " ORDER BY priority DESC, created DESC";

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
// UPDATED ENDPOINT: /search/jql
const searchUrl = `https://${JIRA_DOMAIN}/rest/api/3/search/jql`;

const payload: any = {
  jql: finalJql,
  maxResults: maxResults,
  fields: [
    "summary",
    "status",
    "issuetype",
    "priority",
    "assignee",
    "description",
    "comment",
    "created",
    "parent"
  ]
  // REMOVED: validateQuery (not supported in new endpoint)
};

// Add cursor if provided
if (pageTokenArg) {
  payload.nextPageToken = pageTokenArg;
}

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
    console.error(`Jira API Error (${response.status}): ${err}`);
    process.exit(1);
  }

  const data = await response.json();

  if (!data.issues || data.issues.length === 0) {
    console.log(JSON.stringify({ message: "No issues found", issues: [] }, null, 2));
    process.exit(0);
  }

  // Optimize Output
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

  // Return issues + the token for the next page
  const result = {
    meta: {
      count: data.issues.length,
      next_token: data.nextPageToken || null // Agent uses this to fetch more
    },
    issues: optimizedIssues
  };

  console.log(JSON.stringify(result, null, 2));

} catch (error) {
  console.error("Network or Execution Error:", error);
  process.exit(1);
}