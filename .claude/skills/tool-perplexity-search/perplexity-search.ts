/**
 * ==============================================================================
 * PERPLEXITY AI SEARCH CLI (Bun Optimized)
 * ==============================================================================
 * A zero-dependency command-line tool to query Perplexity AI using Bun.
 *
 * PREREQUISITES:
 * 1. Install Bun: https://bun.sh
 * 2. Get an API Key: https://www.perplexity.ai/settings/api
 * 3. Create a .env file in the repository root:
 *    PERPLEXITY_API_KEY=pplx-xxxxxxxxxxxxxxxxxxxxx
 *
 * USAGE EXAMPLES:
 *
 * 1. Simple search (positional argument):
 *    $ bun .claude/skills/tool-perplexity-search/perplexity-search.ts "What is the capital of Estonia?"
 *
 * 2. Using flags for precision:
 *    $ bun .claude/skills/tool-perplexity-search/perplexity-search.ts -q "Explain String Theory" -m sonar-reasoning
 *
 * 3. Custom system prompt (roleplay or formatting constraints):
 *    $ bun .claude/skills/tool-perplexity-search/perplexity-search.ts "Write a Python script" --system "You are a senior backend engineer. Output code only."
 *
 * 4. Verbose mode (debug configuration):
 *    $ bun .claude/skills/tool-perplexity-search/perplexity-search.ts "Test" --verbose
 *
 * 5. View Help:
 *    $ bun .claude/skills/tool-perplexity-search/perplexity-search.ts --help
 *
 * ==============================================================================
 */

import { parseArgs } from "util";

// --- 1. Environment Configuration ---

// Bun automatically loads .env files on startup.
const PPLX_API_KEY = process.env.PERPLEXITY_API_KEY;
const PPLX_API_URL = "https://api.perplexity.ai/chat/completions";

if (!PPLX_API_KEY) {
  console.error("❌ Error: PERPLEXITY_API_KEY is missing.");
  console.error("Please add it to your environment variables or a .env file.");
  process.exit(1);
}

// --- 2. Argument Parsing ---

/**
 * Configuration for command line arguments using Node/Bun's native util.parseArgs.
 *
 * Options:
 * - query (q): The search term (can also be passed as a positional arg).
 * - model (m): Perplexity model ID (default: sonar-pro).
 * - system (s): The system instruction prompt.
 * - tokens (t): Max output tokens.
 * - verbose (v): Log configuration details before sending.
 * - help (h): Show usage guide.
 */
const { values, positionals } = parseArgs({
  args: Bun.argv,
  options: {
    query: { type: "string", short: "q" },
    model: { type: "string", short: "m", default: "sonar-pro" },
    system: {
      type: "string",
      short: "s",
      default:
        "You are a helpful assistant. Use web search tools to answer with up-to-date information.",
    },
    tokens: { type: "string", short: "t" }, // Parsed as string to validate safely
    verbose: { type: "boolean", short: "v", default: false },
    help: { type: "boolean", short: "h", default: false },
  },
  strict: true,
  allowPositionals: true,
});

// --- 3. Help Menu Handling ---

if (values.help) {
  console.log(`
🤖 Perplexity CLI Help
----------------------
Usage: bun .claude/skills/tool-perplexity-search/perplexity-search.ts [query] [options]

Options:
  -q, --query <text>    The question to ask (or use positional arg)
  -m, --model <id>      Model to use (default: sonar-pro)
                        (options: sonar, sonar-pro, sonar-reasoning)
  -s, --system <text>   System prompt/instructions
  -t, --tokens <num>    Maximum max_tokens for response
  -v, --verbose         Print debug info (model, settings)
  -h, --help            Show this help menu
  `);
  process.exit(0);
}

// --- 4. Query Resolution ---

// Determine query from flag OR the first positional argument after the script name.
const query = values.query || positionals.slice(2).join(" ");

if (!query) {
  console.error("❌ Error: No query provided.");
  console.error("Try: bun .claude/skills/tool-perplexity-search/perplexity-search.ts --help");
  process.exit(1);
}

// --- 5. Core Logic ---

type ChatMessage = {
  role: "system" | "user" | "assistant";
  content: string;
};

/**
 * Sends the request to the Perplexity API.
 *
 * @param queryText - The final user query string.
 * @returns The text content of the assistant's response.
 */
async function runPerplexitySearch(queryText: string): Promise<string> {
  // Debug Logging
  if (values.verbose) {
    console.log("--- ⚙️  Configuration ---");
    console.log(`[Model]:  ${values.model}`);
    console.log(`[Tokens]: ${values.tokens || "Auto"}`);
    console.log(`[System]: ${values.system}`);
    console.log("-----------------------");
  }

  const messages: ChatMessage[] = [
    { role: "system", content: values.system as string },
    { role: "user", content: queryText },
  ];

  // Construct Body
  const body: any = {
    model: values.model,
    messages,
  };

  // Conditionally add max_tokens only if user requested it
  if (values.tokens) {
    const tokens = parseInt(values.tokens as string, 10);
    if (isNaN(tokens) || tokens <= 0) throw new Error("Invalid token count provided. Must be a positive integer.");
    body.max_tokens = tokens;
  }

  // Network Request (Native Fetch) with 30s timeout
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), 30000);
  
  const response = await fetch(PPLX_API_URL, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${PPLX_API_KEY}`,
    },
    body: JSON.stringify(body),
    signal: controller.signal,
  });
  
  clearTimeout(timeout);

  // Error Handling
  if (!response.ok) {
    const errText = await response.text();
    throw new Error(
      `Perplexity API error: ${response.status} ${response.statusText}\nDetails: ${errText}`
    );
  }

  // Response Parsing
  const data: any = await response.json();

  // Perplexity structure: choices[0].message.content + citations array
  const content = data.choices?.[0]?.message?.content ?? JSON.stringify(data, null, 2);
  const citations: string[] = data.citations ?? [];
  
  return { content, citations };
}

// --- 6. Execution ---

(async () => {
  console.log(`🔍 Querying Perplexity: "${query}"...`);

  try {
    const { content, citations } = await runPerplexitySearch(query);
    console.log("\n=== 💡 Perplexity Answer ===\n");
    console.log(content);
    
    if (citations.length > 0) {
      console.log("\n=== 📚 Citations ===\n");
      citations.forEach((url, i) => {
        console.log(`[${i + 1}] ${url}`);
      });
    }
    
    console.log("\n============================");
  } catch (error) {
    console.error("\n❌ Fatal Error\n");
    if (error instanceof Error) {
      console.error(error.message);
    } else {
      console.error(error);
    }
    process.exit(1);
  }
})();
