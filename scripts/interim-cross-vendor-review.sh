#!/usr/bin/env bash
# =============================================================================
# interim-cross-vendor-review.sh — Interim cross-vendor code review
# =============================================================================
#
# Temporary stand-in for the PAI DevTeam / Cato cross-vendor reviewers while the
# primary GPT-5.5 reviewer route is rate-limited. Runs the GitHub Copilot CLI as
# an OpenAI-family reviewer (GPT-5.4, high reasoning) so that an Anthropic-family
# primary (e.g. Vertex Opus 4.8) still gets an opposite-family review.
#
# This is REVIEW-ONLY: the `write` tool is denied so the reviewer cannot modify
# files. It may still run shell/read tools (e.g. `git show`, file reads), which
# is why --allow-all-tools is required for non-interactive Copilot mode.
#
# It does NOT replace the GPT-5.5 review permanently. Phases reviewed this way
# must queue a proper GPT-5.5 (DevTeam) and, where required, Cato review for when
# quota is restored, and record that follow-up in the phase learning note.
#
# Usage:
#   ./scripts/interim-cross-vendor-review.sh <prompt-file> [output-file]
#   ./scripts/interim-cross-vendor-review.sh --help
#
#   <prompt-file>   File containing the full review prompt (scope, intent,
#                   invariants, output format). If "-", the prompt is read
#                   from stdin.
#   [output-file]   Optional path to write the review to (also echoed to stdout).
#
# Environment overrides:
#   REVIEW_MODEL    Copilot model name      (default: gpt-5.4)
#   REVIEW_EFFORT   Reasoning effort level  (default: high)
#   REVIEW_TIMEOUT  Seconds before abort    (default: 420)
#
# Exit codes:
#   0   review completed
#   1   usage error
#   2   copilot CLI not found
#   124 reviewer timed out (treat as reviewer unavailable; record a waiver)
# =============================================================================

set -euo pipefail

MODEL="${REVIEW_MODEL:-gpt-5.4}"
EFFORT="${REVIEW_EFFORT:-high}"
TIMEOUT="${REVIEW_TIMEOUT:-420}"

if [[ "${1:-}" == "--help" || "${1:-}" == "-h" || $# -lt 1 ]]; then
    sed -n '2,42p' "$0"
    [[ $# -lt 1 ]] && exit 1
    exit 0
fi

PROMPT_FILE="$1"
OUTPUT_FILE="${2:-}"

if ! command -v copilot >/dev/null 2>&1; then
    echo "error: 'copilot' CLI not found on PATH. Install GitHub Copilot CLI." >&2
    exit 2
fi

if [[ "$PROMPT_FILE" == "-" ]]; then
    PROMPT="$(cat)"
elif [[ -f "$PROMPT_FILE" ]]; then
    PROMPT="$(cat "$PROMPT_FILE")"
else
    echo "error: prompt file not found: $PROMPT_FILE" >&2
    exit 1
fi

echo "interim reviewer: copilot model=$MODEL effort=$EFFORT timeout=${TIMEOUT}s (read-only)" >&2

run_review() {
    timeout "$TIMEOUT" copilot \
        -p "$PROMPT" \
        --model "$MODEL" \
        --reasoning-effort "$EFFORT" \
        --allow-all-tools \
        --deny-tool='write' \
        -s
}

if [[ -n "$OUTPUT_FILE" ]]; then
    run_review | tee "$OUTPUT_FILE"
else
    run_review
fi
