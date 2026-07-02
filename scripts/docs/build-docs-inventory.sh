#!/usr/bin/env bash
set -euo pipefail
export LC_ALL=C

# Report-only active documentation inventory.
#
# The active-doc set comes from `dotnet toolchain.cs -- docs-list-active`, which
# uses the same DiscoverActiveDocumentationFiles() helper as docs-qa. This script
# must not duplicate the docs-qa boundary in Bash.

repo_root="$(git rev-parse --show-toplevel 2>/dev/null || pwd)"
cd "$repo_root"

output="docs/docs-inventory-report.md"
while [[ $# -gt 0 ]]; do
  case "$1" in
    --output)
      if [[ -z "${2:-}" ]]; then
        echo "ERROR: --output requires a path" >&2
        exit 2
      fi
      output="$2"
      shift 2
      ;;
    -h|--help)
      echo "Usage: build-docs-inventory.sh [--output PATH]"
      exit 0
      ;;
    *)
      echo "ERROR: unknown argument: $1" >&2
      echo "Usage: build-docs-inventory.sh [--output PATH]" >&2
      exit 2
      ;;
  esac
done

mkdir -p "$(dirname "$output")"
tmpd="$(mktemp -d 2>/dev/null || mktemp -d "${TMPDIR:-/tmp}/yubikit-docs-inventory.XXXXXX")"
trap 'rm -rf "$tmpd"' EXIT

raw_active_list="$tmpd/active-docs.raw.txt"
active_list="$tmpd/active-docs.txt"
dotnet toolchain.cs -- docs-list-active > "$raw_active_list"

: > "$active_list"
unexpected_output=0
while IFS= read -r candidate || [[ -n "$candidate" ]]; do
  [[ -z "$candidate" ]] && continue
  if [[ -f "$candidate" ]]; then
    printf '%s\n' "$candidate" >> "$active_list"
  else
    printf 'ERROR: unexpected docs-list-active output (not an active doc path): %s\n' "$candidate" >&2
    unexpected_output=1
  fi
done < "$raw_active_list"

if [[ $unexpected_output -ne 0 ]]; then
  exit 2
fi
if [[ ! -s "$active_list" ]]; then
  echo "ERROR: docs-list-active returned no active documentation files" >&2
  exit 2
fi

now_epoch="$(date +%s)"
generated_date="$(date -u +%Y-%m-%dT%H:%M:%SZ)"
doc_count="$(wc -l < "$active_list" | tr -d ' ')"
report_tmp="$tmpd/report.md"

lane_for() {
  case "$1" in
    README.md|CLAUDE.md|AGENTS.md|SECURITY.md|TOOLCHAIN.md|CONTRIBUTING.md|CHANGELOG.md|RELEASE_NOTES.md) printf 'root' ;;
    docs/architecture/*) printf 'architecture' ;;
    docs/migration/*) printf 'migration' ;;
    docs/usage/*) printf 'usage' ;;
    docs/troubleshooting/*) printf 'troubleshooting' ;;
    src/*/README.md|src/*/CLAUDE.md|src/*/*/README.md|src/*/*/CLAUDE.md) printf 'module' ;;
    docs/*) printf 'top-level-docs' ;;
    *) printf 'other' ;;
  esac
}

signals_for() {
  path="$1"
  age_days="$2"
  lines="$3"
  todo_count="$4"
  signals=""

  if [[ "$age_days" == "unknown" ]]; then
    signals="no git history"
  elif [[ "$age_days" -ge 365 ]]; then
    signals="${signals:+$signals; }last touched >=365d"
  elif [[ "$age_days" -ge 180 ]]; then
    signals="${signals:+$signals; }last touched >=180d"
  fi

  if [[ "$lines" -ge 1500 ]]; then
    signals="${signals:+$signals; }large doc >=1500 lines"
  fi

  if [[ "$todo_count" -gt 0 ]]; then
    signals="${signals:+$signals; }task markers: $todo_count"
  fi

  case "$path" in
    docs/architecture/*) signals="${signals:+$signals; }architecture lane" ;;
    docs/migration/*) signals="${signals:+$signals; }migration lane" ;;
  esac

  [[ -n "$signals" ]] && printf '%s' "$signals" || printf 'none'
}

{
  printf '# Active Documentation Inventory\n\n'
  printf '> Report-only snapshot. This file does not make staleness claims by itself; it lists signals for maintainer review.\n\n'
  printf '%s\n' "- Generated: \`$generated_date\`"
  printf '%s\n' '- Active-doc boundary source: `dotnet toolchain.cs -- docs-list-active`'
  printf '%s\n' "- Active documentation files: \`$doc_count\`"
  printf '%s\n\n' '- Excluded by docs-qa boundary: `docs/archive/**`, `docs/completed/**`, `docs/plans/**`, `docs/research/**`, `docs/reviews/**`, `docs/specs/**`, `docs/templates/**`'
  printf '## Summary\n\n'
  printf '| Lane | Count |\n'
  printf '| --- | ---: |\n'
  while IFS= read -r doc; do lane_for "$doc"; printf '\n'; done < "$active_list" \
    | sort \
    | uniq -c \
    | while read -r count lane; do printf '| %s | %s |\n' "$lane" "$count"; done
  printf '\n## Inventory\n\n'
  printf '| File | Lane | Lines | Last commit | Age (days) | Signals |\n'
  printf '| --- | --- | ---: | --- | ---: | --- |\n'
  while IFS= read -r doc; do
    [[ -z "$doc" ]] && continue
    lane="$(lane_for "$doc")"
    if [[ -f "$doc" ]]; then
      lines="$(wc -l < "$doc" | tr -d ' ')"
    else
      lines="0"
      printf 'WARN: active doc listed but not readable: %s\n' "$doc" >&2
    fi
    last_epoch="$(git log -1 --format=%ct -- "$doc" 2>/dev/null || true)"
    last_short="$(git log -1 --format=%h -- "$doc" 2>/dev/null || true)"
    last_date="$(git log -1 --format=%cs -- "$doc" 2>/dev/null || true)"
    if [[ -n "$last_epoch" ]]; then
      age_days="$(( (now_epoch - last_epoch) / 86400 ))"
      last_commit="${last_short} (${last_date})"
    else
      age_days="unknown"
      last_commit="unknown"
    fi
    todo_count="$(grep -Eic 'TODO|FIXME' "$doc" 2>/dev/null || true)"
    signals="$(signals_for "$doc" "$age_days" "$lines" "$todo_count")"
    printf '| `%s` | %s | %s | %s | %s | %s |\n' "$doc" "$lane" "$lines" "$last_commit" "$age_days" "$signals"
  done < "$active_list"
} > "$report_tmp"

mv "$report_tmp" "$output"

echo "active documentation inventory written to $output ($doc_count files)"
