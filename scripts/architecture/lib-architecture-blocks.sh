#!/usr/bin/env bash
# lib-architecture-blocks.sh
#
# Shared helpers for extracting ```mermaid fenced blocks from the diagrams file
# and hashing their source. Sourced by render-architecture.sh and
# check-architecture-images.sh so both agree on block indexing and freshness.
#
# Functions:
#   arch_extract_block <diagrams_file> <index0> > block.mmd
#       Write the (0-based) Nth mermaid block body (without the fences) to stdout.
#   arch_count_blocks <diagrams_file>
#       Echo the number of ```mermaid blocks.
#   arch_hash_stdin
#       Read stdin, emit a stable sha256 hex (portable: shasum or sha256sum).
#   arch_block_hash <diagrams_file> <index0>
#       Echo sha256 of the Nth block body.
#
# Bash 3.2 compatible.

arch_count_blocks() {
  local file="$1"
  # grep -c exits 1 on zero matches; guard so callers under `set -e` still get
  # a "0" and can emit their own count-mismatch diagnostics.
  grep -c '^```mermaid[[:space:]]*$' "$file" || true
}

arch_hash_file() {
  # sha256 of a file's bytes (artifact integrity for rendered images).
  local f="$1"
  if command -v sha256sum >/dev/null 2>&1; then
    sha256sum "$f" | awk '{print $1}'
  else
    shasum -a 256 "$f" | awk '{print $1}'
  fi
}

arch_extract_block() {
  local file="$1" want="$2"
  awk -v want="$want" '
    BEGIN { idx=-1; inblock=0 }
    /^```mermaid[[:space:]]*$/ { idx++; inblock=(idx==want)?1:0; next }
    /^```[[:space:]]*$/ {
      if (inblock) { inblock=0 }
      next
    }
    { if (inblock) print }
  ' "$file"
}

arch_hash_stdin() {
  if command -v sha256sum >/dev/null 2>&1; then
    sha256sum | awk '{print $1}'
  else
    shasum -a 256 | awk '{print $1}'
  fi
}

arch_block_hash() {
  local file="$1" want="$2"
  arch_extract_block "$file" "$want" | arch_hash_stdin
}
