#!/usr/bin/env bash
set -euo pipefail
export LC_ALL=C

# check-architecture-map.sh
#
# Verifies that every piece of evidence declared in
# docs/architecture/sdk-architecture-map.yml still resolves against the current
# working tree:
#
#   - projects:      the listed .csproj file exists.
#   - project_edges: "A -> B" asserts A has a <ProjectReference> to B; verified
#                    by parsing A's csproj.
#   - forbidden_project_edges: "A -> B" asserts A does NOT reference B; fails if
#                    the edge appears (guards "app modules are independent").
#   - paths:         the listed file or directory exists.
#   - symbols:       the declaring file exists AND still declares the named C#
#                    type (class/interface/record/struct/enum <Name>), matched
#                    with an anchored, modifier-tolerant pattern.
#
# It also checks bidirectional coverage against the diagrams file: every
# "## Lx" / "### Lxb" diagram heading has a map entry and vice versa, with
# duplicate detection on both sides.
#
# Strictness: any non-comment, non-blank line inside the `diagrams:` block that
# does not match one of the recognized shapes is a hard error (prevents typos
# like `symbolz:` or misindented evidence from silently dropping checks).
#
# Exit 0 = all evidence resolves and coverage matches. Non-zero = drift.
#
# This is the anti-hallucination cornerstone of the architecture-docs lane:
# if a type moves or is renamed, this fails loudly instead of letting a
# diagram silently rot. Portable to bash 3.2 (macOS) and bash 4/5 (Linux CI).

repo_root="$(git rev-parse --show-toplevel 2>/dev/null || pwd)"
cd "$repo_root"

map_file="docs/architecture/sdk-architecture-map.yml"
diagrams_file="docs/architecture/sdk-architecture-diagrams.md"

fail=0
err() { printf 'ERROR: %s\n' "$1" >&2; fail=1; }
info() { printf '%s\n' "$1"; }

[[ -f "$map_file" ]] || { echo "ERROR: missing $map_file" >&2; exit 2; }
[[ -f "$diagrams_file" ]] || { echo "ERROR: missing $diagrams_file" >&2; exit 2; }

# --- Verify a C# type is declared in a file -------------------------------
# Anchored: start of line, optional access/other modifiers, then the kind and
# the exact type name followed by a non-identifier boundary. Reduces false
# positives from comments/strings/suffix words containing "class".
symbol_declared_in() {
  local symbol="$1" file="$2"
  [[ -f "$file" ]] || return 2
  grep -Eq \
    "^[[:space:]]*((public|internal|private|protected|sealed|abstract|static|partial|readonly|ref|file)[[:space:]]+)*(class|interface|record|struct|enum)([[:space:]]+(class|struct))?[[:space:]]+${symbol}([^A-Za-z0-9_]|$)" \
    "$file"
}

# --- Verify a <ProjectReference> edge A -> B ------------------------------
# A and B are repo-relative csproj paths. We check that A's csproj references a
# project whose basename matches B's basename (csproj references are relative
# and use backslashes/forward slashes; basename match is robust and sufficient
# because assembly names are unique in this repo).
project_edge_exists() {
  local from="$1" to="$2"
  [[ -f "$from" ]] || { err "project_edge source csproj not found: $from"; return 0; }
  [[ -f "$to" ]]   || { err "project_edge target csproj not found: $to"; return 0; }
  local to_base; to_base="$(basename "$to")"
  # Extract referenced csproj basenames from <ProjectReference Include="...">.
  if grep -oE 'ProjectReference[[:space:]]+Include="[^"]+"' "$from" \
     | sed -E 's/.*Include="//; s/"$//; s#\\#/#g' \
     | sed -E 's#.*/##' \
     | grep -qx "$to_base"; then
    return 0
  fi
  err "missing <ProjectReference> edge: $(basename "$from") -> $to_base"
  return 0
}

# --- Verify a <ProjectReference> edge A -> B does NOT exist ---------------
project_edge_absent() {
  local from="$1" to="$2"
  [[ -f "$from" ]] || { err "forbidden_project_edge source csproj not found: $from"; return 0; }
  local to_base; to_base="$(basename "$to")"
  if grep -oE 'ProjectReference[[:space:]]+Include="[^"]+"' "$from" \
     | sed -E 's/.*Include="//; s/"$//; s#\\#/#g' \
     | sed -E 's#.*/##' \
     | grep -qx "$to_base"; then
    err "forbidden <ProjectReference> edge present: $(basename "$from") -> $to_base"
  fi
  return 0
}

# --- Parse the map (bounded, structure-specific YAML reader) ---------------
# Fixed shape (2/4/6-space nesting):
#   diagrams:
#     <id>:                      # 2 spaces
#       title: "..."             # 4 spaces
#       projects:                # 4 spaces
#         - path                 # 6 spaces
#       project_edges:           # 4 spaces
#         - A -> B               # 6 spaces
#       paths: []                # 4 spaces (or list)
#       symbols: {}              # 4 spaces (or 6-space "Name: path" entries)

in_diagrams=0
current_diagram=""
section=""          # projects | project_edges | paths | symbols | ""
map_ids=""          # space-separated for bash 3.2 portability

while IFS= read -r raw || [[ -n "$raw" ]]; do
  line="${raw%%$'\r'}"

  # Blank line.
  [[ -z "${line//[[:space:]]/}" ]] && continue
  # Full-line comment (any indentation).
  case "${line#"${line%%[![:space:]]*}"}" in \#*) continue ;; esac

  # Top-level keys (0 indentation).
  if [[ "$line" =~ ^[A-Za-z_][A-Za-z0-9_]*: ]]; then
    if [[ "$line" == "diagrams:"* ]]; then
      in_diagrams=1
    else
      in_diagrams=0
    fi
    current_diagram=""
    section=""
    continue
  fi

  [[ $in_diagrams -eq 1 ]] || continue

  # Diagram id: exactly 2 leading spaces, "id:" with nothing after the colon.
  if [[ "$line" =~ ^\ \ ([A-Za-z0-9._-]+):[[:space:]]*$ ]]; then
    current_diagram="${BASH_REMATCH[1]}"
    map_ids="$map_ids $current_diagram"
    section=""
    continue
  fi

  # Section headers under a diagram: exactly 4 leading spaces.
  if [[ "$line" =~ ^\ \ \ \ (title|projects|project_edges|forbidden_project_edges|paths|symbols):[[:space:]]*(.*)$ ]]; then
    key="${BASH_REMATCH[1]}"
    rest="${BASH_REMATCH[2]}"
    if [[ "$key" == "title" ]]; then
      section=""
    elif [[ "$rest" == "{}" || "$rest" == "[]" ]]; then
      section=""      # inline-empty; no children expected
    else
      section="$key"
    fi
    continue
  fi

  # List item (projects/project_edges/paths): exactly 6 spaces then "- value".
  if [[ "$line" =~ ^\ \ \ \ \ \ -\ +(.+)$ ]]; then
    val="${BASH_REMATCH[1]}"
    val="${val%\"}"; val="${val#\"}"
    case "$section" in
      projects)
        [[ -f "$val" ]] || err "[$current_diagram] project not found: $val" ;;
      paths)
        [[ -e "$val" ]] || err "[$current_diagram] path not found: $val" ;;
      project_edges)
        if [[ "$val" =~ ^(.+)\ -\>\ (.+)$ ]]; then
          project_edge_exists "${BASH_REMATCH[1]}" "${BASH_REMATCH[2]}"
        else
          err "[$current_diagram] malformed project_edge (want 'A -> B'): $val"
        fi ;;
      forbidden_project_edges)
        if [[ "$val" =~ ^(.+)\ -\>\ (.+)$ ]]; then
          project_edge_absent "${BASH_REMATCH[1]}" "${BASH_REMATCH[2]}"
        else
          err "[$current_diagram] malformed forbidden_project_edge (want 'A -> B'): $val"
        fi ;;
      *)
        err "[$current_diagram] unexpected list item under section '$section': $val" ;;
    esac
    continue
  fi

  # Symbol mapping: exactly 6 spaces then "Name: path".
  if [[ "$line" =~ ^\ \ \ \ \ \ ([A-Za-z_][A-Za-z0-9_]*):\ +(.+)$ ]]; then
    if [[ "$section" != "symbols" ]]; then
      err "[$current_diagram] symbol entry outside 'symbols' section: $line"
      continue
    fi
    sym="${BASH_REMATCH[1]}"
    path="${BASH_REMATCH[2]}"
    path="${path%\"}"; path="${path#\"}"
    if symbol_declared_in "$sym" "$path"; then
      :
    else
      rc=$?
      if [[ $rc -eq 2 ]]; then
        err "[$current_diagram] symbol file not found: $sym -> $path"
      else
        err "[$current_diagram] symbol '$sym' no longer declared in: $path"
      fi
    fi
    continue
  fi

  # Anything else inside diagrams: is an unrecognized shape -> hard error.
  err "unrecognized map line (check indentation/typos): '$line'"
done < "$map_file"

# --- Bidirectional coverage vs the diagrams file --------------------------
# Diagram headings look like "## L0 — ..." or "### L3b — ...".
heading_levels=""
while IFS= read -r h; do
  if [[ "$h" =~ ^\#\#\#?\ +(L[0-9]+[a-z]?) ]]; then
    heading_levels="$heading_levels ${BASH_REMATCH[1]}"
  fi
done < <(grep -E '^#{2,3}[[:space:]]+L[0-9]' "$diagrams_file" || true)

# Map levels = prefix of each id up to first '-'.
map_levels=""
for id in $map_ids; do
  map_levels="$map_levels ${id%%-*}"
done

count_occurrences() { # needle haystack...
  local needle="$1"; shift
  local n=0 x
  for x in "$@"; do [[ "$x" == "$needle" ]] && n=$((n+1)); done
  echo "$n"
}

# Every heading level must have exactly one map entry.
for lvl in $heading_levels; do
  c="$(count_occurrences "$lvl" $map_levels)"
  if [[ "$c" -eq 0 ]]; then
    err "diagram heading '$lvl' has no entry in $map_file"
  elif [[ "$c" -gt 1 ]]; then
    err "diagram heading '$lvl' maps to $c entries in $map_file (expected 1)"
  fi
done

# Every map entry must correspond to exactly one heading level.
for ml in $map_levels; do
  c="$(count_occurrences "$ml" $heading_levels)"
  if [[ "$c" -eq 0 ]]; then
    err "map entry level '$ml' has no matching diagram heading in $diagrams_file"
  elif [[ "$c" -gt 1 ]]; then
    err "map entry level '$ml' matches $c diagram headings in $diagrams_file (expected 1)"
  fi
done

# Count entries for the summary.
n_ids=0; for _ in $map_ids; do n_ids=$((n_ids+1)); done
n_head=0; for _ in $heading_levels; do n_head=$((n_head+1)); done

if [[ $fail -eq 0 ]]; then
  info "architecture map OK: $n_ids diagram entries, all evidence + project edges resolve; coverage matches $n_head diagram heading level(s)."
fi

exit $fail
