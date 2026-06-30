#!/usr/bin/env bash
set -euo pipefail
export LC_ALL=C

usage() {
  cat <<'USAGE'
Usage: build-migration-context.sh --mode preview|update --v1-baseline REF --v2-branch REF --base-range RANGE --output-dir DIR

Build deterministic context files for the v1-to-v2 migration documentation assistant.
USAGE
}

mode=""
v1_baseline=""
v2_branch=""
base_range=""
output_dir=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --mode)
      mode="${2:-}"
      shift 2
      ;;
    --v1-baseline)
      v1_baseline="${2:-}"
      shift 2
      ;;
    --v2-branch)
      v2_branch="${2:-}"
      shift 2
      ;;
    --base-range)
      base_range="${2:-}"
      shift 2
      ;;
    --output-dir)
      output_dir="${2:-}"
      shift 2
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      echo "Unknown argument: $1" >&2
      usage >&2
      exit 2
      ;;
  esac
done

if [[ "$mode" != "preview" && "$mode" != "update" ]]; then
  echo "--mode must be preview or update" >&2
  exit 2
fi

if [[ -z "$v1_baseline" || -z "$v2_branch" || -z "$base_range" || -z "$output_dir" ]]; then
  echo "Missing required argument" >&2
  usage >&2
  exit 2
fi

mkdir -p "$output_dir"
max_diff_bytes="${MIGRATION_CONTEXT_MAX_DIFF_BYTES:-250000}"

state_path="docs/migration/.state.yml"
if [[ -f "$state_path" ]]; then
  state_content="$(cat "$state_path")"
else
  state_content="format_version: unknown
v1_baseline: $v1_baseline
v2_branch: $v2_branch
last_analyzed_commit: unavailable
state_file: absent"
fi

head_sha="$(git rev-parse HEAD)"
git diff --name-only "$base_range" > "$output_dir/changed-files.txt"
full_diff="$output_dir/.diff.full.tmp"
git diff --no-ext-diff "$base_range" > "$full_diff"
diff_bytes="$(wc -c < "$full_diff" | tr -d ' ')"
diff_truncated="false"
if [[ "$diff_bytes" -gt "$max_diff_bytes" ]]; then
  head -c "$max_diff_bytes" "$full_diff" > "$output_dir/diff.patch"
  {
    printf '\n\n'
    printf '[migration-context truncated diff: original_bytes=%s max_bytes=%s]\n' "$diff_bytes" "$max_diff_bytes"
  } >> "$output_dir/diff.patch"
  diff_truncated="true"
else
  mv "$full_diff" "$output_dir/diff.patch"
fi
rm -f "$full_diff"

awk '
  /^[+-]/ && !/^\+\+\+/ && !/^---/ {
    marker = substr($0, 1, 1)
    line = substr($0, 2)
    if (line ~ /^[[:space:]]*(public|protected)[[:space:]]+/) {
      if (line ~ /[[:space:]](class|struct|record|interface|enum)[[:space:]]+/ || line ~ /[A-Za-z0-9_<>,\[\]\?]+[[:space:]]+[A-Za-z_][A-Za-z0-9_]*[[:space:]]*(\(|\{|=>)/) {
        print marker line
      }
    }
  }
' "$output_dir/diff.patch" > "$output_dir/public-api-candidates.txt"

awk '
  /^\+/ && !/^\+\+\+/ {
    line = substr($0, 2)
    if (line ~ /^[[:space:]]*(public|protected)[[:space:]]+/) print line
  }
' "$output_dir/diff.patch" > "$output_dir/api-added.txt"

awk '
  /^-/ && !/^---/ {
    line = substr($0, 2)
    if (line ~ /^[[:space:]]*(public|protected)[[:space:]]+/) print line
  }
' "$output_dir/diff.patch" > "$output_dir/api-removed.txt"

grep -E '^[+-].*<PackageReference|^[+-].*<ProjectReference|^[+-].*<PackageId>|^[+-].*<RootNamespace>|^[+-].*<AssemblyName>' "$output_dir/diff.patch" \
  | grep -Ev '^(\+\+\+|---)' > "$output_dir/package-changes.txt" || true

grep -E '^[+-][[:space:]]*(namespace|using)[[:space:]]+Yubico\.' "$output_dir/diff.patch" \
  | grep -Ev '^(\+\+\+|---)' > "$output_dir/namespace-changes.txt" || true

grep -Ei 'ApplicationSession|Create[A-Za-z0-9]+SessionAsync|Session\.CreateAsync|IApplicationSession|IYubiKeyExtensions|ConnectAsync|AvailableConnections|SupportsConnection' "$output_dir/diff.patch" \
  > "$output_dir/session-patterns.txt" || true

tmp_symbols="$output_dir/.symbols.tmp"
grep -Eo '\b[A-Za-z_][A-Za-z0-9_]{2,}\b' "$output_dir/diff.patch" \
  | grep -Ev '^(abstract|async|await|class|const|enum|event|false|get|init|interface|internal|namespace|new|null|override|private|protected|public|readonly|record|return|sealed|set|static|struct|this|true|using|virtual|void)$' \
  | sort -u \
  | head -n 200 > "$tmp_symbols" || true

grep -Ff "$tmp_symbols" docs/migration/v1-to-v2-map.yml 2>/dev/null \
  | head -n 200 > "$output_dir/existing-map-hits.txt" || true

grep -Ff "$tmp_symbols" docs/migration/v1-to-v2.md docs/migration/v1-to-v2-changelog.md 2>/dev/null \
  | head -n 200 > "$output_dir/docs-coverage-hits.txt" || true

: > "$output_dir/v1-symbol-candidates.txt"
while IFS= read -r symbol; do
  [[ -z "$symbol" ]] && continue
  if [[ "$(wc -l < "$output_dir/v1-symbol-candidates.txt")" -ge 300 ]]; then
    break
  fi

  matches="$(git grep -n --no-color -F -- "$symbol" "$v1_baseline" -- 'Yubico.Core' 'Yubico.YubiKey' 2>/dev/null | head -n 3 || true)"
  if [[ -n "$matches" ]]; then
    printf '%s\n' "$matches" >> "$output_dir/v1-symbol-candidates.txt"
  fi
done < "$tmp_symbols"

sort -u "$output_dir/v1-symbol-candidates.txt" -o "$output_dir/v1-symbol-candidates.txt"
rm -f "$tmp_symbols"

last_analyzed="$(printf '%s\n' "$state_content" | sed -n 's/^last_analyzed_commit:[[:space:]]*//p' | head -n 1)"
changed_file_count="$(wc -l < "$output_dir/changed-files.txt" | tr -d ' ')"
if [[ ! -s "$output_dir/changed-files.txt" ]]; then
  changed_file_count="0"
fi

cat > "$output_dir/migration-state.txt" <<EOF
mode: $mode
v1_baseline: $v1_baseline
v2_branch: $v2_branch
base_range: $base_range
head_sha: $head_sha
state_last_analyzed_commit: $last_analyzed

--- docs/migration/.state.yml ---
$state_content
EOF

cat > "$output_dir/context-summary.md" <<EOF
# Migration Context Summary

- Mode: $mode
- V1 baseline: $v1_baseline
- V2 branch: $v2_branch
- Analyzed range: $base_range
- HEAD: $head_sha
- Changed file count: $changed_file_count
- State last analyzed commit: $last_analyzed
- Diff bytes before truncation: $diff_bytes
- Diff truncated: $diff_truncated
- Max diff bytes: $max_diff_bytes

## Files

- changed-files.txt: files changed in the analyzed range.
- diff.patch: deterministic git diff for the analyzed range, truncated when it exceeds the configured size limit.
- public-api-candidates.txt: added or removed public/protected C# declarations found in the diff.
- api-added.txt: added public/protected declarations.
- api-removed.txt: removed public/protected declarations.
- package-changes.txt: package, project, namespace, and assembly metadata changes.
- namespace-changes.txt: Yubico namespace import/declaration changes.
- session-patterns.txt: session, device, connection, and transport related diff lines.
- existing-map-hits.txt: current migration map lines matching diff-derived symbols.
- docs-coverage-hits.txt: current migration guide/changelog lines matching diff-derived symbols.
- v1-symbol-candidates.txt: bounded v1 baseline grep matches for symbols found in the diff.
- migration-state.txt: workflow mode, refs, range, HEAD, and migration state content.

Use this context only for the requested migration documentation task. If evidence is incomplete, mark findings manual-review instead of inventing mappings.
EOF
