#!/usr/bin/env bun

/**
 * Jira Cloud REST API v3 - Update Issue Script
 *
 * Capabilities:
 * - Batches Summary, Description, and Comments into a single atomic PUT request.
 * - separate handling for Transitions and Assignees (as they use distinct endpoints/logic).
 *
 * References:
 * - Edit Issue: https://developer.atlassian.com/cloud/jira/platform/rest/v3/api-group-issues/#api-rest-api-3-issue-issueidorkey-put
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

const issueKey = getArg("--key");
const summaryArg = getArg("--summary");
const descArg = getArg("--desc");
const statusArg = getArg("--status");
const commentArg = getArg("--comment");
const assigneeArg = getArg("--assignee");

// --- Validation ---
if (!JIRA_DOMAIN || !JIRA_EMAIL || !JIRA_TOKEN) {
  console.error("Error: Missing Environment Variables.");
  process.exit(1);
}

if (!issueKey) {
  console.error("Error: Missing required argument '--key'.");
  process.exit(1);
}

if (!summaryArg && !descArg && !statusArg && !commentArg && !assigneeArg) {
  console.error("Error: No update actions specified.");
  process.exit(1);
}

const authString = Buffer.from(`${JIRA_EMAIL}:${JIRA_TOKEN}`).toString('base64');
const baseUrl = `https://${JIRA_DOMAIN}/rest/api/3`;
const commonHeaders = {
  "Authorization": `Basic ${authString}`,
  "Accept": "application/json",
  "Content-Type": "application/json"
};

// --- Helper: Get Account ID for "me" ---
async function getMyAccountId(): Promise<string> {
  const resp = await fetch(`${baseUrl}/myself`, { headers: commonHeaders });
  if (!resp.ok) throw new Error("Failed to fetch current user ID");
  const data = await resp.json();
  return data.accountId;
}

// --- Action 1: Batch Update (Summary, Desc, Comment) ---
async function batchUpdate(key: string) {
  // If no content/comment updates, skip this step
  if (!summaryArg && !descArg && !commentArg) return;

  const bodyData: any = {};

  // 1. Handle Fields (Direct Value Replacement)
  if (summaryArg || descArg) {
    bodyData.fields = {};
    if (summaryArg) bodyData.fields.summary = summaryArg;
    
    if (descArg) {
      // API v3 requires Atlassian Document Format (ADF)
      bodyData.fields.description = {
        type: "doc",
        version: 1,
        content: [{ type: "paragraph", content: [{ type: "text", text: descArg }] }]
      };
    }
  }

  // 2. Handle Operations (Adding a Comment via "update" verb)
  if (commentArg) {
    bodyData.update = {
      comment: [
        {
          add: {
            body: {
              type: "doc",
              version: 1,
              content: [{ type: "paragraph", content: [{ type: "text", text: commentArg }] }]
            }
          }
        }
      ]
    };
  }

  console.log(`[Batch Update] Sending changes for ${key}...`);
  
  const resp = await fetch(`${baseUrl}/issue/${key}`, {
    method: "PUT",
    headers: commonHeaders,
    body: JSON.stringify(bodyData)
  });

  if (resp.status !== 204) {
    const errorText = await resp.text();
    throw new Error(`Batch update failed (${resp.status}): ${errorText}`);
  }
  
  console.log(`✅ Content & Comment updated.`);
}

// --- Action 2: Update Assignee ---
async function updateAssignee(key: string, assignee: string) {
  let accountId: string | null = null;
  
  if (assignee.toLowerCase() === "me") {
    accountId = await getMyAccountId();
  } else if (assignee.toLowerCase() === "unassigned") {
    accountId = null;
  } else {
    // If user provided a raw AccountID, use it directly
    accountId = assignee;
  }

  const resp = await fetch(`${baseUrl}/issue/${key}/assignee`, {
    method: "PUT",
    headers: commonHeaders,
    body: JSON.stringify({ accountId })
  });

  if (!resp.ok) {
    throw new Error(`Assignee update failed: ${await resp.text()}`);
  }
  console.log(`✅ Assigned to: ${assignee}`);
}

// --- Action 3: Transition Status ---
async function transitionStatus(key: string, targetStatus: string) {
  // 1. Fetch available transitions for this specific issue
  const getResp = await fetch(`${baseUrl}/issue/${key}/transitions`, { headers: commonHeaders });
  if (!getResp.ok) throw new Error(`Fetch transitions failed: ${await getResp.text()}`);
  
  const data = await getResp.json();
  
  // 2. Fuzzy match the status name to an ID
  const match = data.transitions.find((t: any) => 
    t.to.name.toLowerCase() === targetStatus.toLowerCase() || 
    t.name.toLowerCase() === targetStatus.toLowerCase()
  );

  if (!match) {
    const available = data.transitions.map((t: any) => `"${t.to.name}"`).join(", ");
    throw new Error(`Cannot move to "${targetStatus}". Valid next steps: ${available}`);
  }

  // 3. Execute the transition
  const postResp = await fetch(`${baseUrl}/issue/${key}/transitions`, {
    method: "POST",
    headers: commonHeaders,
    body: JSON.stringify({ transition: { id: match.id } })
  });

  if (postResp.status !== 204) {
    throw new Error(`Transition failed: ${await postResp.text()}`);
  }
  console.log(`✅ Status changed to: ${match.to.name}`);
}

// --- Main Execution ---
async function main() {
  try {
    // Execute updates. 
    // We run batchUpdate first (content/comments), then structural changes (assignee/status).
    await batchUpdate(issueKey!);
    
    if (assigneeArg) await updateAssignee(issueKey!, assigneeArg);
    if (statusArg) await transitionStatus(issueKey!, statusArg);
    
    console.log(`Update Complete! 🔗 https://${JIRA_DOMAIN}/browse/${issueKey}`);
  } catch (error) {
    console.error("❌ Error during update:", error);
    process.exit(1);
  }
}

main();