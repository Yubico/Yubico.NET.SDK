#!/usr/bin/env bun

/**
 * Jira Cloud REST API v3 - Delete Issue Script
 *
 * Capabilities:
 * - Deletes a specific issue.
 * - Handles "Recursive" deletion (required if the issue has subtasks).
 *
 * References:
 * - Delete Issue: https://developer.atlassian.com/cloud/jira/platform/rest/v3/api-group-issues/#api-rest-api-3-issue-issueidorkey-delete
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

function hasFlag(flag: string): boolean {
  return args.includes(flag);
}

const issueKey = getArg("--key");
const recursive = hasFlag("--recursive"); // Matches API param: deleteSubtasks=true

// --- Validation ---
if (!JIRA_DOMAIN || !JIRA_EMAIL || !JIRA_TOKEN) {
  console.error("❌ Error: Missing Environment Variables.");
  process.exit(1);
}

if (!issueKey) {
  console.error("❌ Error: Missing required argument '--key'.");
  process.exit(1);
}

// --- API Call ---
const authString = Buffer.from(`${JIRA_EMAIL}:${JIRA_TOKEN}`).toString('base64');
const baseUrl = `https://${JIRA_DOMAIN}/rest/api/3/issue/${issueKey}`;

// If --recursive is used, we must append the query parameter
const url = recursive ? `${baseUrl}?deleteSubtasks=true` : baseUrl;

console.log(`[Delete] Attempting to delete ${issueKey} (Recursive: ${recursive})...`);

try {
  const response = await fetch(url, {
    method: "DELETE",
    headers: {
      "Authorization": `Basic ${authString}`,
      "Accept": "application/json"
    }
  });

  // 204 No Content = Success
  if (response.status === 204) {
    console.log(`✅ Success! Issue ${issueKey} has been deleted.`);
    process.exit(0);
  }

  // Handle Errors
  const errorText = await response.text();
  console.error(`❌ Jira API Error (${response.status}):`);
  
  if (response.status === 400 && errorText.includes("subtasks")) {
    console.error("Error: This issue has subtasks. You must use the --recursive flag to delete it.");
  } else if (response.status === 404) {
    console.error("Error: Issue not found. It may have already been deleted.");
  } else {
    console.error(errorText);
  }
  
  process.exit(1);

} catch (error) {
  console.error("❌ Network or Execution Error:", error);
  process.exit(1);
}