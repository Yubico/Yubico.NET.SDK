#!/bin/bash

# Ralph Loop for Copilot CLI
# External bash loop implementation of the Ralph Wiggum technique
# Repeatedly pipes the same prompt to copilot until completion
# 
# Features learning mode: captures patterns, suggests skills, proposes improvements

set -euo pipefail

STATE_FILE=".copilot/ralph-loop/state.md"
LEARNING_DIR=".copilot/ralph-loop/learning"
REVIEW_FILE="$LEARNING_DIR/review-$(date +%Y%m%d-%H%M%S).md"

# Helper: portable sed in-place edit (works on macOS and Linux)
sed_inplace() {
  local pattern="$1"
  local file="$2"
  if sed -i.bak "$pattern" "$file" 2>/dev/null; then
    rm -f "${file}.bak"
  else
    sed "$pattern" "$file" > "${file}.tmp" && mv "${file}.tmp" "$file"
  fi
}

# Parse arguments
PROMPT_PARTS=()
MAX_ITERATIONS=0
COMPLETION_PROMISE=""
DELAY=2
LEARNING_MODE=false
PROMPT_FILE=""
MODEL=""

show_help() {
  cat << 'HELP_EOF'
Ralph Loop for Copilot CLI - Iterative self-referential development loop

USAGE:
  ./ralph-loop.sh [PROMPT...] [OPTIONS]

ARGUMENTS:
  PROMPT...    Initial prompt to start the loop (can be multiple words)

OPTIONS:
  --max-iterations <n>           Maximum iterations before auto-stop (default: 0 = unlimited/infinite)
  --completion-promise '<text>'  Promise phrase that signals completion
  --delay <seconds>              Delay between iterations (default: 2)
  --learn                        Enable learning mode (captures patterns for review)
  --prompt-file <file>           Read prompt from file instead of command line
  --model <model>                Copilot model to use (default: your configured default)
                                 Cheap: claude-haiku-4.5, gpt-5-mini, gpt-5.1-codex-mini
                                 Standard: claude-sonnet-4, gpt-5.1-codex
  -h, --help                     Show this help message

DESCRIPTION:
  Starts a Ralph Loop that repeatedly pipes the same prompt to Copilot CLI.
  Each iteration, Copilot sees its previous work in files and git history,
  allowing it to iteratively improve until completion.

  To signal completion, Copilot must output: <promise>YOUR_PHRASE</promise>

EXAMPLES:
  ./ralph-loop.sh "Build a todo API" --completion-promise 'DONE' --max-iterations 20
  ./ralph-loop.sh --max-iterations 10 "Fix the auth bug"
  ./ralph-loop.sh "Refactor cache layer" --learn  # enables learning mode
  ./ralph-loop.sh "Create tests" --learn --max-iterations 15
  ./ralph-loop.sh --prompt-file PROMPT.md --max-iterations 10  # read from file
  cat PROMPT.md | ./ralph-loop.sh --max-iterations 10  # read from pipe
  ./ralph-loop.sh "Fix bugs" --model claude-haiku-4.5  # use cheap model
  ./ralph-loop.sh "Build API" --completion-promise 'DONE'  # run infinitely until promise

STOPPING:
  - Ctrl+C to interrupt
  - Reaching --max-iterations
  - Detecting --completion-promise in output
  - Run ./cancel-ralph.sh from another terminal

MONITORING:
  # View current state:
  cat .copilot/ralph-loop/state.md

  # View iteration logs:
  ls -la .copilot/ralph-loop/iteration-*.log

LEARNING MODE:
  When --learn is enabled, Ralph captures:
  - Repeated tool patterns
  - Common file modifications
  - Successful strategies
  - Suggested skills and improvements
  
  Review files are saved to: .copilot/ralph-loop/learning/
  Nothing is auto-applied - human review required!
HELP_EOF
  exit 0
}

# Parse options and positional arguments
while [[ $# -gt 0 ]]; do
  case $1 in
    -h|--help)
      show_help
      ;;
    --max-iterations)
      if [[ -z "${2:-}" ]] || ! [[ "$2" =~ ^[0-9]+$ ]]; then
        echo "❌ Error: --max-iterations requires a positive integer" >&2
        exit 1
      fi
      MAX_ITERATIONS="$2"
      shift 2
      ;;
    --completion-promise)
      if [[ -z "${2:-}" ]]; then
        echo "❌ Error: --completion-promise requires a text argument" >&2
        exit 1
      fi
      COMPLETION_PROMISE="$2"
      shift 2
      ;;
    --delay)
      if [[ -z "${2:-}" ]] || ! [[ "$2" =~ ^[0-9]+$ ]]; then
        echo "❌ Error: --delay requires a positive integer" >&2
        exit 1
      fi
      DELAY="$2"
      shift 2
      ;;
    --learn)
      LEARNING_MODE=true
      shift
      ;;
    --prompt-file)
      if [[ -z "${2:-}" ]]; then
        echo "❌ Error: --prompt-file requires a file path" >&2
        exit 1
      fi
      if [[ ! -f "$2" ]]; then
        echo "❌ Error: Prompt file not found: $2" >&2
        exit 1
      fi
      PROMPT_FILE="$2"
      shift 2
      ;;
    --model)
      if [[ -z "${2:-}" ]]; then
        echo "❌ Error: --model requires a model name" >&2
        echo "   Cheap models: claude-haiku-4.5, gpt-5-mini, gpt-5.1-codex-mini" >&2
        echo "   Standard: claude-sonnet-4, gpt-5.1-codex" >&2
        exit 1
      fi
      MODEL="$2"
      shift 2
      ;;
    *)
      PROMPT_PARTS+=("$1")
      shift
      ;;
  esac
done

# Join all prompt parts with spaces (handle empty array)
PROMPT="${PROMPT_PARTS[*]:-}"

# If prompt file specified, read from it
if [[ -n "$PROMPT_FILE" ]]; then
  PROMPT=$(cat "$PROMPT_FILE")
fi

# If no prompt yet and stdin is not a terminal, read from pipe
if [[ -z "$PROMPT" ]] && [[ ! -t 0 ]]; then
  PROMPT=$(cat)
fi

# Validate prompt
if [[ -z "$PROMPT" ]]; then
  echo "❌ Error: No prompt provided" >&2
  echo "" >&2
  echo "   Provide prompt as argument or via --prompt-file:" >&2
  echo "     ./ralph-loop.sh \"Build a REST API for todos\"" >&2
  echo "     ./ralph-loop.sh --prompt-file PROMPT.md --max-iterations 20" >&2
  echo "" >&2
  echo "   For all options: ./ralph-loop.sh --help" >&2
  exit 1
fi

# Create state directory
mkdir -p "$(dirname "$STATE_FILE")"

# Initialize learning mode if enabled
if [[ "$LEARNING_MODE" == true ]]; then
  mkdir -p "$LEARNING_DIR"
  
  # Initialize review file
  cat > "$REVIEW_FILE" <<EOF
# Ralph Loop Learning Review

Generated: $(date -u +%Y-%m-%dT%H:%M:%SZ)
Prompt: $PROMPT
Max Iterations: $(if [[ $MAX_ITERATIONS -gt 0 ]]; then echo $MAX_ITERATIONS; else echo "unlimited"; fi)
Completion Promise: $(if [[ -n "$COMPLETION_PROMISE" ]]; then echo "$COMPLETION_PROMISE"; else echo "none"; fi)

---

## Summary

<!-- Auto-populated at loop end -->

## Suggested Skills

<!-- Skills that could be extracted from repeated patterns -->

## Tool Usage Patterns

<!-- Frequently used tool combinations -->

## File Modification Patterns

<!-- Files that were repeatedly modified -->

## Successful Strategies

<!-- What worked well -->

## Failed Approaches

<!-- What didn't work - useful for prompt refinement -->

## Proposed Improvements

### To ralph-loop.sh

<!-- Improvements to the loop script itself -->

### To Prompts

<!-- Better ways to phrase this type of task -->

---

## Iteration Log

EOF
fi

# Quote completion promise for YAML
if [[ -n "$COMPLETION_PROMISE" ]]; then
  COMPLETION_PROMISE_YAML="\"$COMPLETION_PROMISE\""
else
  COMPLETION_PROMISE_YAML="null"
fi

# Initialize state file
cat > "$STATE_FILE" <<EOF
---
active: true
iteration: 0
max_iterations: $MAX_ITERATIONS
completion_promise: $COMPLETION_PROMISE_YAML
started_at: "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
---

$PROMPT
EOF

# Output setup message
cat <<EOF
🔄 Ralph loop activated for Copilot CLI!

Max iterations: $(if [[ $MAX_ITERATIONS -gt 0 ]]; then echo $MAX_ITERATIONS; else echo "unlimited (infinite)"; fi)
Completion promise: $(if [[ -n "$COMPLETION_PROMISE" ]]; then echo "$COMPLETION_PROMISE"; else echo "none (runs forever)"; fi)
Model: $(if [[ -n "$MODEL" ]]; then echo "$MODEL"; else echo "default"; fi)
Delay between iterations: ${DELAY}s

Press Ctrl+C to cancel at any time.

To monitor: cat $STATE_FILE
$(if [[ "$LEARNING_MODE" == true ]]; then echo "Learning mode: ENABLED - Review file: $REVIEW_FILE"; fi)
═══════════════════════════════════════════════════════════
EOF

if [[ -n "$COMPLETION_PROMISE" ]]; then
  echo ""
  echo "CRITICAL - Completion Promise Requirements:"
  echo "  To complete this loop, Copilot must output:"
  echo "  <promise>$COMPLETION_PROMISE</promise>"
  echo ""
  echo "═══════════════════════════════════════════════════════════"
fi

# Main loop
ITERATION=0
TOOL_PATTERNS=""
FILE_CHANGES=""
START_TIME=$(date +%s)

# Learning helper functions
capture_iteration_learning() {
  local iter=$1
  local output=$2
  local log_file=$3
  
  if [[ "$LEARNING_MODE" != true ]]; then
    return
  fi
  
  # Append iteration summary to review file
  cat >> "$REVIEW_FILE" <<EOF

### Iteration $iter

\`\`\`
$(echo "$output" | head -50)
$(if [[ $(echo "$output" | wc -l) -gt 50 ]]; then echo "... (truncated, see $log_file for full output)"; fi)
\`\`\`

EOF

  # Track tool patterns (look for common tool invocations)
  local tools_used=$(echo "$output" | grep -oE '(bash|grep|view|edit|create|glob|git)' | sort | uniq -c | sort -rn | head -5 || true)
  if [[ -n "$tools_used" ]]; then
    TOOL_PATTERNS="$TOOL_PATTERNS
Iteration $iter:
$tools_used"
  fi
  
  # Track file changes via git
  local changed_files=$(git diff --name-only 2>/dev/null | head -10 || true)
  if [[ -n "$changed_files" ]]; then
    FILE_CHANGES="$FILE_CHANGES
Iteration $iter:
$changed_files"
  fi
}

generate_learning_summary() {
  local final_iteration=$1
  local success=$2
  
  if [[ "$LEARNING_MODE" != true ]]; then
    return
  fi
  
  local end_time=$(date +%s)
  local duration=$((end_time - START_TIME))
  
  # Generate the analysis prompt for Copilot to analyze the session
  local analysis_prompt="Analyze this Ralph Loop session and update the review file.

Session Details:
- Iterations: $final_iteration
- Duration: ${duration}s
- Success: $success
- Prompt: $PROMPT

Tool Patterns Observed:
$TOOL_PATTERNS

Files Changed:
$FILE_CHANGES

Review file to update: $REVIEW_FILE

Please analyze the iteration logs in .copilot/ralph-loop/iteration-*.log and:

1. Fill in the '## Summary' section with a brief overview
2. Under '## Suggested Skills', propose any skills that could be extracted:
   - Look for repeated multi-step patterns
   - Identify domain-specific workflows
   - Format as skill definitions ready to copy to .copilot/skills/
3. Under '## Tool Usage Patterns', document frequent tool combinations
4. Under '## File Modification Patterns', note which files were touched most
5. Under '## Successful Strategies', document what approaches worked
6. Under '## Failed Approaches', document what didn't work (useful for prompt tuning)
7. Under '## Proposed Improvements':
   - Suggest improvements to ralph-loop.sh itself
   - Suggest better prompt formulations for similar tasks

IMPORTANT: Write all suggestions to $REVIEW_FILE. Do NOT auto-apply anything.
The developer will review and decide what to implement.

Output <promise>ANALYSIS_COMPLETE</promise> when done."

  echo ""
  echo "🧠 Learning mode: Generating session analysis..."
  echo ""
  
  # Run a single Copilot pass to analyze and fill in the review file
  COPILOT_ARGS=("-p" "$analysis_prompt" "--allow-all-tools")
  if [[ -n "$MODEL" ]]; then
    COPILOT_ARGS=("--model" "$MODEL" "${COPILOT_ARGS[@]}")
  fi
  copilot "${COPILOT_ARGS[@]}" 2>&1 | tee "$LEARNING_DIR/analysis.log"
  
  echo ""
  echo "📝 Learning review file ready: $REVIEW_FILE"
  echo "   Please review suggested skills and improvements before applying."
}

cleanup() {
  echo ""
  echo "🛑 Ralph loop interrupted at iteration $ITERATION"
  
  # Generate learning summary on interrupt
  generate_learning_summary "$ITERATION" "interrupted"
  
  # Update state file to show inactive
  sed_inplace "s/^active: true/active: false/" "$STATE_FILE"
  exit 0
}

trap cleanup SIGINT SIGTERM

while true; do
  ITERATION=$((ITERATION + 1))
  
  # Check max iterations
  if [[ $MAX_ITERATIONS -gt 0 ]] && [[ $ITERATION -gt $MAX_ITERATIONS ]]; then
    echo ""
    echo "🛑 Ralph loop: Max iterations ($MAX_ITERATIONS) reached."
    
    # Generate learning summary
    generate_learning_summary "$((ITERATION - 1))" "max_iterations_reached"
    
    sed_inplace "s/^active: true/active: false/" "$STATE_FILE"
    exit 0
  fi
  
  # Update iteration in state file
  sed_inplace "s/^iteration: .*/iteration: $ITERATION/" "$STATE_FILE"
  
  echo ""
  echo "🔄 ═══════════════════════════════════════════════════════════"
  echo "🔄 Ralph iteration $ITERATION starting..."
  echo "🔄 ═══════════════════════════════════════════════════════════"
  echo ""
  
  # Create iteration log file
  LOG_FILE=".copilot/ralph-loop/iteration-${ITERATION}.log"
  
  # Build the iteration prompt with context
  ITERATION_PROMPT="$PROMPT

---
[Ralph Loop Context]
Iteration: $ITERATION
$(if [[ -n "$COMPLETION_PROMISE" ]]; then echo "To signal completion, output: <promise>$COMPLETION_PROMISE</promise>"; fi)
$(if [[ $MAX_ITERATIONS -gt 0 ]]; then echo "Max iterations: $MAX_ITERATIONS"; fi)

Check your previous work in files and git history. Continue from where you left off.
---"
  
  # Build copilot command with proper non-interactive flags
  # --allow-all-tools is required for non-interactive mode
  COPILOT_ARGS=("-p" "$ITERATION_PROMPT" "--allow-all-tools")
  if [[ -n "$MODEL" ]]; then
    COPILOT_ARGS=("--model" "$MODEL" "${COPILOT_ARGS[@]}")
  fi
  
  # Run copilot in non-interactive mode, stream output to terminal and file
  copilot "${COPILOT_ARGS[@]}" 2>&1 | tee "$LOG_FILE"
  OUTPUT=$(cat "$LOG_FILE")
  
  # Capture learning data
  capture_iteration_learning "$ITERATION" "$OUTPUT" "$LOG_FILE"
  
  # Check for completion promise in output
  if [[ -n "$COMPLETION_PROMISE" ]]; then
    # Extract text from <promise> tags
    PROMISE_TEXT=$(echo "$OUTPUT" | perl -0777 -pe 's/.*?<promise>(.*?)<\/promise>.*/$1/s; s/^\s+|\s+$//g; s/\s+/ /g' 2>/dev/null || echo "")
    
    if [[ -n "$PROMISE_TEXT" ]] && [[ "$PROMISE_TEXT" = "$COMPLETION_PROMISE" ]]; then
      echo ""
      echo "✅ ═══════════════════════════════════════════════════════════"
      echo "✅ Ralph loop: Detected <promise>$COMPLETION_PROMISE</promise>"
      echo "✅ Task completed in $ITERATION iterations!"
      echo "✅ ═══════════════════════════════════════════════════════════"
      
      # Generate learning summary on success
      generate_learning_summary "$ITERATION" "completed"
      
      sed_inplace "s/^active: true/active: false/" "$STATE_FILE"
      exit 0
    fi
  fi
  
  echo ""
  echo "🔄 Iteration $ITERATION complete. Waiting ${DELAY}s before next iteration..."
  sleep "$DELAY"
done
