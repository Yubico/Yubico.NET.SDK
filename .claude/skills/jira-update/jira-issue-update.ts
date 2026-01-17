#!/usr/bin/env bun

/**
 * Jira Cloud REST API v3 - Update Issue Script
 *
 * Capabilities:
 * - Single or Multi-field updates (batches atomic changes).
 * - Support for changing Parent (Link to Epic / Reparent Subtask).
 * - Robust Alias Support (--desc/--description).
 * - Allows clearing fields by passing empty strings.
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

// Greedy Parser: Captures values until the next flag.
function getArg(flag: string, alias?: string): string | undefined {
  let index = args.indexOf(flag);
  if (index === -1 && alias) index = args.indexOf(alias);

  if (index === -1) return undefined; // Flag not found
  if (index === args.length - 1) return ""; // Flag found but no value (e.g. clearing)

  const values = [];
  for (let i = index + 1; i < args.length; i++) {
    const val = args[i];
    if (val.startsWith("--")) break;
    values.push(val);
  }
  return values.join(" ");
}

const issueKey = getArg("--key");
const summaryArg = getArg("--summary", "--title");
const descArg = getArg("--desc", "--description");
const statusArg = getArg("--status");
const commentArg = getArg("--comment");
const assigneeArg = getArg("--assignee");
const parentArg = getArg("--parent"); // New: Support for linking/reparenting

// --- Validation ---
if (!JIRA_DOMAIN || !JIRA_EMAIL || !JIRA_TOKEN) {
  console.error("❌ Error: Missing Environment Variables.");
  process.exit(1);
}

if (!issueKey) {
  console.error("❌ Error: Missing required argument '--key'.");
  process.exit(1);
}

// Check if ANY update action is specified
if (summaryArg === undefined && descArg === undefined && statusArg === undefined && commentArg === undefined && assigneeArg === undefined && parentArg === undefined) {
  console.error("❌ Error: No update actions specified.");
  console.error("Provide at least one: --summary, --desc, --status, --parent, --comment, or --assignee");
  process.exit(1);
}

const authString = Buffer.from(`${JIRA_EMAIL}:${JIRA_TOKEN}`).toString('base64');
const baseUrl = `https://${JIRA_DOMAIN}/rest/api/3`;
const commonHeaders = {
  "Authorization": `Basic ${authString}`,
  "Accept": "application/json",
  "Content-Type": "application/json"
};

// --- Actions ---

async function getMyAccountId(): Promise<string> {
  const resp = await fetch(`${baseUrl}/myself`, { headers: commonHeaders });
  if (!resp.ok) throw new Error("Failed to fetch current user ID");
  const data = await resp.json();
  return data.accountId;
}

async function batchUpdate(key: string) {
  // If no content updates (Summary, Desc, Parent, Comment), skip this function
  if (summaryArg === undefined && descArg === undefined && commentArg === undefined && parentArg === undefined) return;

  const bodyData: any = {};

  // 1. Fields (Summary, Description, Parent)
  if (summaryArg !== undefined || descArg !== undefined || parentArg !== undefined) {
    bodyData.fields = {};
    
    if (summaryArg !== undefined) bodyData.fields.summary = summaryArg;
    
    if (descArg !== undefined) {
      bodyData.fields.description = {
        type: "doc",
        version: 1,
        content: descArg ? [{ type: "paragraph", content: [{ type: "text", text: descArg }] }] : []
      };
    }

    // Handle Parent Link
    if (parentArg !== undefined) {
      // If empty string passed (""), we remove the parent link (set to null)
      // Otherwise, we set the object { key: "KEY-123" }
      if (parentArg === "") {
        bodyData.fields.parent = null; 
      } else {
        bodyData.fields.parent = { key: parentArg };
      }
    }
  }

  // 2. Comments (via 'update' verb)
  if (commentArg) {
    bodyData.update = {
      comment: [{
        add: {
          body: {
            type: "doc",
            version: 1,
            content: [{ type: "paragraph", content: [{ type: "text", text: commentArg }] }]
          }
        }
      }]
    };
  }

  console.log(`[Batch Update] Sending changes for ${key}...`);
  const resp = await fetch(`${baseUrl}/issue/${key}`, {
    method: "PUT",
    headers: commonHeaders,
    body: JSON.stringify(bodyData)
  });

  if (resp.status !== 204) {
    // 204 No Content is success for PUT
    throw new Error(`Batch update failed: ${await resp.text()}`);
  }
  console.log(`✅ Content/Parent updated.`);
}

async function updateAssignee(key: string, assignee: string) {
  let accountId: string | null = null;
  if (assignee.toLowerCase() === "me") accountId = await getMyAccountId();
  else if (assignee.toLowerCase() === "unassigned") accountId = null;
  else accountId = assignee;

  const resp = await fetch(`${baseUrl}/issue/${key}/assignee`, {
    method: "PUT",
    headers: commonHeaders,
    body: JSON.stringify({ accountId })
  });

  if (!resp.ok) throw new Error(`Assignee failed: ${await resp.text()}`);
  console.log(`✅ Assigned to: ${assignee}`);
}

async function transitionStatus(key: string, targetStatus: string) {
  const getResp = await fetch(`${baseUrl}/issue/${key}/transitions`, { headers: commonHeaders });
  if (!getResp.ok) throw new Error(`Fetch transitions failed: ${await getResp.text()}`);
  
  const data = await getResp.json();
  const match = data.transitions.find((t: any) => 
    t.to.name.toLowerCase() === targetStatus.toLowerCase() || 
    t.name.toLowerCase() === targetStatus.toLowerCase()
  );

  if (!match) {
    const available = data.transitions.map((t: any) => `"${t.to.name}"`).join(", ");
    throw new Error(`Cannot move to "${targetStatus}". Valid next steps: ${available}`);
  }

  const postResp = await fetch(`${baseUrl}/issue/${key}/transitions`, {
    method: "POST",
    headers: commonHeaders,
    body: JSON.stringify({ transition: { id: match.id } })
  });

  if (postResp.status !== 204) throw new Error(`Transition failed: ${await postResp.text()}`);
  console.log(`✅ Status changed to: ${match.to.name}`);
}

// --- Main ---
async function main() {
  try {
    // 1. Batch Update (Fields + Comments)
    await batchUpdate(issueKey!);
    
    // 2. Assignee Update (Separate Endpoint)
    if (assigneeArg !== undefined) await updateAssignee(issueKey!, assigneeArg);
    
    // 3. Status Transition (Separate Endpoint)
    if (statusArg !== undefined) await transitionStatus(issueKey!, statusArg);
    
    console.log(`Update Complete! 🔗 https://${JIRA_DOMAIN}/browse/${issueKey}`);
  } catch (error) {
    console.error("❌ Error during update:", error);
    process.exit(1);
  }
}

main();