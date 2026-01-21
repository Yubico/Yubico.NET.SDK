#!/usr/bin/env bun

/**
 * Jira Cloud REST API v3 - Get Project Statuses
 *
 * Capabilities:
 * - Fetches valid statuses for a specific project.
 * - Extracts "Status Category" (To Do, In Progress, Done) to provide semantic meaning.
 * - Deduplicates statuses across multiple issue types.
 *
 * References:
 * - Endpoint: https://developer.atlassian.com/cloud/jira/platform/rest/v3/api-group-projects/#api-rest-api-3-project-projectidorkey-statuses-get
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

const projectKey = getArg("--project");

// --- Validation ---
if (!JIRA_DOMAIN || !JIRA_EMAIL || !JIRA_TOKEN) {
  console.error("❌ Error: Missing Environment Variables.");
  process.exit(1);
}

if (!projectKey) {
  console.error("❌ Error: Missing required argument '--project'.");
  process.exit(1);
}

// --- API Call ---
const authString = Buffer.from(`${JIRA_EMAIL}:${JIRA_TOKEN}`).toString('base64');
// UPDATED: Using plural /statuses endpoint as per documentation
const url = `https://${JIRA_DOMAIN}/rest/api/3/project/${projectKey}/statuses`;

try {
  const response = await fetch(url, {
    method: "GET",
    headers: {
      "Authorization": `Basic ${authString}`,
      "Accept": "application/json"
    }
  });

  if (!response.ok) {
    console.error(`❌ Jira API Error (${response.status}): ${await response.text()}`);
    process.exit(1);
  }

  // API returns array of Issue Types, each with a list of valid statuses
  const data = await response.json();

  if (!Array.isArray(data)) {
    console.error("❌ Unexpected API response format.");
    process.exit(1);
  }

  // --- Processing: Extract & Categorize ---
  // Map: Status Name -> Category Name
  const statusMap = new Map<string, string>();

  data.forEach((issueType: any) => {
    if (issueType.statuses && Array.isArray(issueType.statuses)) {
      issueType.statuses.forEach((status: any) => {
        // Capture the name and its semantic category (e.g., "Closed" -> "Done")
        const category = status.statusCategory?.name || "Unknown";
        statusMap.set(status.name, category);
      });
    }
  });

  // Convert to sorted list of objects
  const uniqueStatuses = Array.from(statusMap.entries())
    .map(([name, category]) => ({ name, category }))
    .sort((a, b) => a.category.localeCompare(b.category) || a.name.localeCompare(b.name));

  console.log(JSON.stringify({
    project: projectKey,
    total_unique_statuses: uniqueStatuses.length,
    statuses: uniqueStatuses
  }, null, 2));

} catch (error) {
  console.error("Network Error:", error);
  process.exit(1);
}