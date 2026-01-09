#!/bin/bash

# Cancel Ralph Loop for Copilot CLI
# Removes state file and reports cancellation

set -euo pipefail

STATE_FILE=".copilot/ralph-loop/state.md"

if [[ ! -f "$STATE_FILE" ]]; then
  echo "No active Ralph loop found."
  echo "State file not found: $STATE_FILE"
  exit 0
fi

# Read iteration from state file
ITERATION=$(grep '^iteration:' "$STATE_FILE" | sed 's/iteration: *//' || echo "unknown")

# Remove state file
rm "$STATE_FILE"

# Clean up backup files if any
rm -f "${STATE_FILE}.bak" "${STATE_FILE}.tmp"

echo "🛑 Cancelled Ralph loop (was at iteration $ITERATION)"
echo ""
echo "Iteration logs preserved in: .copilot/ralph-loop/iteration-*.log"
echo "To clean up logs: rm .copilot/ralph-loop/iteration-*.log"
