#!/usr/bin/env bash
set -euo pipefail
export LC_ALL=C

# build-architecture-context.sh
#
# Deterministic context builder for the architecture-docs automation lane.
# Mirrors scripts/migration/build-migration-context.sh in shape and philosophy:
# scripts find evidence; AI writes prose. The AI must classify/edit diagrams
# from THESE files, not from intuition about the codebase.
#
# It analyzes a commit range on the v2 branch and produces, under an output dir:
#   - context-summary.md        human-readable run summary
#   - changed-files.txt         files changed in the range
#   - diff.patch                deterministic diff (size-bounded, truncation-noted)
#   - project-graph.txt         current <ProjectReference> edges (A -> B)
#   - architecture-symbols.txt  mapped symbols and whether each changed in range
#   - affected-diagrams.txt     diagrams whose evidence intersects the change set
#   - state.txt                 mode/refs/range/HEAD + docs/architecture/.state.yml
#
# "affected-diagrams" is the key output: it is derived by intersecting the
# changed files/symbols against docs/architecture/sdk-architecture-map.yml, so
# the AI is told exactly which diagrams a change could have invalidated.
#
# Portable to bash 3.2 (macOS) and bash 4/5 (Linux CI).

usage() {
  cat <<'USAGE'
Usage: build-architecture-context.sh --v2-branch REF --base-range RANGE --output-dir DIR [--mode preview|update]

Build deterministic context for the architecture-docs assistant.
  --v2-branch   authoritative v2 ref (e.g. origin/yubikit)
  --base-range  git range to analyze (e.g. origin/yubikit...HEAD or SHA..HEAD)
  --output-dir  directory to write context files into
  --mode        preview (PR) or update (post-merge); default: update
USAGE
}

mode="update"
v2_branch=""
base_range=""
output_dir=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --mode)       mode="${2:-}"; shift 2 ;;
    --v2-branch)  v2_branch="${2:-}"; shift 2 ;;
    --base-range) base_range="${2:-}"; shift 2 ;;
    --output-dir) output_dir="${2:-}"; shift 2 ;;
    -h|--help)    usage; exit 0 ;;
    *) echo "Unknown argument: $1" >&2; usage >&2; exit 2 ;;
  esac
done

if [[ "$mode" != "preview" && "$mode" != "update" ]]; then
  echo "--mode must be preview or update" >&2; exit 2
fi
if [[ -z "$v2_branch" || -z "$base_range" || -z "$output_dir" ]]; then
  echo "Missing required argument" >&2; usage >&2; exit 2
fi

repo_root="$(git rev-parse --show-toplevel 2>/dev/null || pwd)"
cd "$repo_root"

map_file="docs/architecture/sdk-architecture-map.yml"
state_path="docs/architecture/.state.yml"
[[ -f "$map_file" ]] || { echo "ERROR: missing $map_file" >&2; exit 2; }

mkdir -p "$output_dir"
max_diff_bytes="${ARCH_CONTEXT_MAX_DIFF_BYTES:-250000}"

head_sha="$(git rev-parse HEAD)"

# --- validate the range resolves (fail closed, never fail open) -----------
# A bad/unfetched ref must NOT silently produce "no diagram impact".
if ! git diff --name-only "$base_range" >/dev/null 2>&1; then
  echo "ERROR: --base-range '$base_range' does not resolve (unfetched ref or bad range?)" >&2
  exit 3
fi

# --- changed files + bounded diff ----------------------------------------
git diff --name-only "$base_range" > "$output_dir/changed-files.txt"

full_diff="$output_dir/.diff.full.tmp"
git diff --no-ext-diff "$base_range" > "$full_diff"
diff_bytes="$(wc -c < "$full_diff" | tr -d ' ')"
diff_truncated="false"
if [[ "$diff_bytes" -gt "$max_diff_bytes" ]]; then
  head -c "$max_diff_bytes" "$full_diff" > "$output_dir/diff.patch"
  {
    printf '\n\n'
    printf '[architecture-context truncated diff: original_bytes=%s max_bytes=%s]\n' "$diff_bytes" "$max_diff_bytes"
  } >> "$output_dir/diff.patch"
  diff_truncated="true"
else
  mv "$full_diff" "$output_dir/diff.patch"
fi
rm -f "$full_diff"

# --- current project-reference graph -------------------------------------
# (grep returns 1 when a csproj has no ProjectReference, e.g. Core; guard so
#  set -e does not abort the loop.)
: > "$output_dir/project-graph.txt"
while IFS= read -r csproj; do
  from_base="$(basename "$csproj")"
  refs="$(grep -oE 'ProjectReference[[:space:]]+Include="[^"]+"' "$csproj" 2>/dev/null || true)"
  [[ -z "$refs" ]] && continue
  printf '%s\n' "$refs" \
    | sed -E 's/.*Include="//; s/"$//; s#\\#/#g; s#.*/##' \
    | while IFS= read -r to_base; do
        [[ -n "$to_base" ]] && printf '%s -> %s\n' "$from_base" "$to_base"
      done
done < <(find src -name '*.csproj' -path '*/src/*' 2>/dev/null | sort) \
  >> "$output_dir/project-graph.txt"

# --- parse the map: collect "diagram|symbol|path" and "diagram|path" ------
# We reuse the same fixed 2/4/6-space shape as check-architecture-map.sh.
symbol_index="$output_dir/.symbol-index.tmp"   # lines: diagram<TAB>symbol<TAB>path
path_index="$output_dir/.path-index.tmp"       # lines: diagram<TAB>path
proj_index="$output_dir/.proj-index.tmp"       # lines: diagram<TAB>csproj
: > "$symbol_index"; : > "$path_index"; : > "$proj_index"

in_diagrams=0
current_diagram=""
section=""
while IFS= read -r raw || [[ -n "$raw" ]]; do
  line="${raw%%$'\r'}"
  [[ -z "${line//[[:space:]]/}" ]] && continue
  case "${line#"${line%%[![:space:]]*}"}" in \#*) continue ;; esac

  if [[ "$line" =~ ^[A-Za-z_][A-Za-z0-9_]*: ]]; then
    [[ "$line" == "diagrams:"* ]] && in_diagrams=1 || in_diagrams=0
    current_diagram=""; section=""; continue
  fi
  [[ $in_diagrams -eq 1 ]] || continue

  if [[ "$line" =~ ^\ \ ([A-Za-z0-9._-]+):[[:space:]]*$ ]]; then
    current_diagram="${BASH_REMATCH[1]}"; section=""; continue
  fi
  if [[ "$line" =~ ^\ \ \ \ (title|projects|project_edges|forbidden_project_edges|paths|symbols):[[:space:]]*(.*)$ ]]; then
    key="${BASH_REMATCH[1]}"; rest="${BASH_REMATCH[2]}"
    if [[ "$key" == "title" || "$rest" == "{}" || "$rest" == "[]" ]]; then section=""; else section="$key"; fi
    continue
  fi
  if [[ "$line" =~ ^\ \ \ \ \ \ -\ +(.+)$ ]]; then
    val="${BASH_REMATCH[1]}"; val="${val%\"}"; val="${val#\"}"
    case "$section" in
      projects) printf '%s\t%s\n' "$current_diagram" "$val" >> "$proj_index" ;;
      paths)    printf '%s\t%s\n' "$current_diagram" "$val" >> "$path_index" ;;
      project_edges|forbidden_project_edges)
        # Index both csproj endpoints as affected-evidence so an edge-only
        # diagram still reacts to a csproj change even if not listed under
        # `projects`.
        if [[ "$val" =~ ^(.+)\ -\>\ (.+)$ ]]; then
          printf '%s\t%s\n' "$current_diagram" "${BASH_REMATCH[1]}" >> "$proj_index"
          printf '%s\t%s\n' "$current_diagram" "${BASH_REMATCH[2]}" >> "$proj_index"
        else
          echo "ERROR: [$current_diagram] malformed $section (want 'A -> B'): $val" >&2
          exit 4
        fi ;;
      *)
        echo "ERROR: [$current_diagram] unexpected list item under section '$section': $val" >&2
        exit 4 ;;
    esac
    continue
  fi
  if [[ "$line" =~ ^\ \ \ \ \ \ ([A-Za-z_][A-Za-z0-9_]*):\ +(.+)$ ]]; then
    if [[ "$section" == "symbols" ]]; then
      sym="${BASH_REMATCH[1]}"; p="${BASH_REMATCH[2]}"; p="${p%\"}"; p="${p#\"}"
      printf '%s\t%s\t%s\n' "$current_diagram" "$sym" "$p" >> "$symbol_index"
    else
      echo "ERROR: [$current_diagram] mapping entry outside 'symbols' section: $line" >&2
      exit 4
    fi
    continue
  fi

  # Unrecognized non-comment, non-blank line inside diagrams: -> fail closed
  # (parity with check-architecture-map.sh strictness; prevents dropped evidence).
  echo "ERROR: unrecognized map line in build-architecture-context (check indentation/typos): '$line'" >&2
  exit 4
done < "$map_file"

# --- architecture-symbols.txt: did each mapped symbol's file change? ------
# A symbol is "changed" if its declaring file appears in changed-files.txt.
: > "$output_dir/architecture-symbols.txt"
changed_files_file="$output_dir/changed-files.txt"
is_changed() { grep -qxF "$1" "$changed_files_file" 2>/dev/null; }

while IFS="$(printf '\t')" read -r diagram sym path; do
  [[ -z "$diagram" ]] && continue
  if is_changed "$path"; then
    printf '%s\t%s\t%s\tCHANGED\n' "$diagram" "$sym" "$path" >> "$output_dir/architecture-symbols.txt"
  else
    printf '%s\t%s\t%s\tunchanged\n' "$diagram" "$sym" "$path" >> "$output_dir/architecture-symbols.txt"
  fi
done < "$symbol_index"

# --- affected-diagrams.txt: intersect change set with map evidence --------
# A diagram is affected if ANY of its symbol files, structural paths, or listed
# csproj files intersect the changed file set. For paths (directories), a change
# is any changed file whose path starts with that directory.
affected_tmp="$output_dir/.affected.tmp"; : > "$affected_tmp"

mark_affected() { printf '%s\n' "$1" >> "$affected_tmp"; }

# symbols (exact file match)
while IFS="$(printf '\t')" read -r diagram sym path; do
  [[ -z "$diagram" ]] && continue
  if is_changed "$path"; then mark_affected "$diagram"; fi
done < "$symbol_index"

# projects (exact file match)
while IFS="$(printf '\t')" read -r diagram path; do
  [[ -z "$diagram" ]] && continue
  if is_changed "$path"; then mark_affected "$diagram"; fi
done < "$proj_index"

# paths (prefix match: any changed file under the directory/file)
while IFS="$(printf '\t')" read -r diagram path; do
  [[ -z "$diagram" ]] && continue
  # Normalize any trailing slash so "dir" and "dir/" behave identically
  # (contract parity with check-architecture-map.sh, which accepts both).
  path="${path%/}"
  while IFS= read -r cf; do
    [[ -z "$cf" ]] && continue
    if [[ "$cf" == "$path" || "$cf" == "$path"/* ]]; then
      mark_affected "$diagram"; break
    fi
  done < "$changed_files_file"
done < "$path_index"

sort -u "$affected_tmp" > "$output_dir/affected-diagrams.txt" 2>/dev/null || : > "$output_dir/affected-diagrams.txt"
affected_count="$(wc -l < "$output_dir/affected-diagrams.txt" | tr -d ' ')"
[[ -s "$output_dir/affected-diagrams.txt" ]] || affected_count=0

rm -f "$symbol_index" "$path_index" "$proj_index" "$affected_tmp"

# --- state.txt ------------------------------------------------------------
if [[ -f "$state_path" ]]; then state_content="$(cat "$state_path")"; else state_content="(no $state_path)"; fi
last_analyzed="$(printf '%s\n' "$state_content" | sed -n 's/^last_analyzed_commit:[[:space:]]*//p' | head -n 1)"
changed_file_count="$(wc -l < "$changed_files_file" | tr -d ' ')"
[[ -s "$changed_files_file" ]] || changed_file_count=0

cat > "$output_dir/state.txt" <<EOF
mode: $mode
v2_branch: $v2_branch
base_range: $base_range
head_sha: $head_sha
state_last_analyzed_commit: $last_analyzed

--- $state_path ---
$state_content
EOF

# --- context-summary.md ---------------------------------------------------
cat > "$output_dir/context-summary.md" <<EOF
# Architecture Context Summary

- Mode: $mode
- V2 branch: $v2_branch
- Analyzed range: $base_range
- HEAD: $head_sha
- State last analyzed commit: $last_analyzed
- Changed file count: $changed_file_count
- Diff bytes before truncation: $diff_bytes
- Diff truncated: $diff_truncated
- Max diff bytes: $max_diff_bytes
- Affected diagrams: $affected_count

## Files

- changed-files.txt: files changed in the analyzed range.
- diff.patch: deterministic diff for the range (truncated when oversized).
- project-graph.txt: current <ProjectReference> edges (A -> B) across src/*/src/*.csproj.
- architecture-symbols.txt: each mapped symbol, its file, and CHANGED/unchanged in range.
- affected-diagrams.txt: diagrams whose mapped evidence intersects the change set.
- state.txt: mode/refs/range/HEAD + docs/architecture/.state.yml content.

Use ONLY this context for the architecture-docs task. If affected-diagrams.txt is
empty, no mapped evidence changed: treat as a no-diagram-impact range. Do not
"notice" architecture changes that are not represented in these files; if you
believe a diagram is affected but it is not listed, record it as manual-review
instead of silently editing.
EOF

echo "architecture context written to $output_dir (affected diagrams: $affected_count)"
