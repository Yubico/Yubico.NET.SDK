#!/usr/bin/env bun

/**
 * Jira Cloud REST API v3 - Create Issue Script
 *
 * References:
 * - Create Issue Endpoint: https://developer.atlassian.com/cloud/jira/platform/rest/v3/api-group-issues/#api-rest-api-3-issue-post
 * - Atlassian Document Format (ADF): https://developer.atlassian.com/cloud/jira/platform/apis/document/structure/
 */

// --- Configuration & Env Vars ---
const JIRA_DOMAIN = Bun.env.JIRA_DOMAIN; // e.g., "mycompany.atlassian.net"
const JIRA_EMAIL = Bun.env.JIRA_EMAIL;
const JIRA_TOKEN = Bun.env.JIRA_TOKEN; // API Token created at https://id.atlassian.com/manage-profile/security/api-tokens

// --- Argument Parsing ---
const args = Bun.argv.slice(2);

function getArg(flag: string): string | undefined {
  const index = args.indexOf(flag);
  return (index > -1 && args[index + 1]) ? args[index + 1] : undefined;
}

const projectKey = getArg("--project");
const summary = getArg("--summary");
const issueType = getArg("--type") || "Task"; // Common types: Bug, Story, Epic, Task, Sub-task
const description = getArg("--desc") || "";
const labelsArg = getArg("--labels") || ""; // Comma-separated labels
const parentKey = getArg("--parent"); // Parent issue key (for subtasks)

// --- Validation ---
if (!JIRA_DOMAIN || !JIRA_EMAIL || !JIRA_TOKEN) {
  console.error("Error: Missing Environment Variables.");
  console.error("Please set JIRA_DOMAIN, JIRA_EMAIL, and JIRA_TOKEN.");
  process.exit(1);
}

if (!projectKey || !summary) {
  console.error("Error: Missing required arguments.");
  console.error("Usage: bun run jira-issue-create.ts --project <KEY> --summary <TITLE> [--type <TYPE>] [--desc <DESCRIPTION>] [--labels <LABEL1,LABEL2>] [--parent <PARENT-KEY>]");
  process.exit(1);
}

// --- Labels Processing ---
// Always include the automation label, plus any additional labels
const labels = ["jira-agent-skill-automation"];
if (labelsArg) {
  const additionalLabels = labelsArg.split(",").map(l => l.trim()).filter(l => l.length > 0);
  labels.push(...additionalLabels);
}

// --- ADF Payload Construction ---
// API v3 REQUIRES the description to be in Atlassian Document Format (ADF).
// We construct a minimal valid ADF document with a single paragraph.
const descriptionADF = description
  ? {
      type: "doc",
      version: 1,
      content: [
        {
          type: "paragraph",
          content: [
            {
              type: "text",
              text: description,
            },
          ],
        },
      ],
    }
  : undefined;

const bodyData = {
  fields: {
    project: {
      key: projectKey,
    },
    summary: summary,
    issuetype: {
      name: issueType,
    },
    labels: labels,
    // Only include description if provided
    ...(descriptionADF && { description: descriptionADF }),
    // Only include parent if provided (for subtasks)
    ...(parentKey && { parent: { key: parentKey } }),
  },
};

// --- API Call ---
const authString = Buffer.from(`${JIRA_EMAIL}:${JIRA_TOKEN}`).toString('base64');
const url = `https://${JIRA_DOMAIN}/rest/api/3/issue`;

const parentInfo = parentKey ? ` | Parent: ${parentKey}` : "";
console.log(`[Drafting Issue] Project: ${projectKey} | Type: ${issueType} | Summary: "${summary}" | Labels: [${labels.join(", ")}]${parentInfo}`);

try {
  const response = await fetch(url, {
    method: "POST",
    headers: {
      "Authorization": `Basic ${authString}`,
      "Accept": "application/json",
      "Content-Type": "application/json",
    },
    body: JSON.stringify(bodyData),
  });

  const responseText = await response.text();

  if (!response.ok) {
    console.error(`Jira API Error (${response.status}):`);
    console.error(responseText); // Helpful to see specific ADF validation errors
    process.exit(1);
  }

  const result = JSON.parse(responseText);
  console.log(`Success! Issue Created: ${result.key}`);
  console.log(`Link: https://${JIRA_DOMAIN}/browse/${result.key}`);

} catch (error) {
  console.error("Network or Execution Error:", error);
  process.exit(1);
}
