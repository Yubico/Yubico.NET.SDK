#!/usr/bin/env bash
# Builds the v2 SDK demo deck from the topic-keyed slide files.
#
#   ./build.sh          -> deck.md, deck.html, deck.pdf
#   ./build.sh md       -> deck.md only (fast; no node/npx needed)
#   ./build.sh map      -> print which deck slide numbers each source file owns
#
# SOURCE OF TRUTH is slides/*.md, concatenated in filename order behind
# header.md. Never hand-edit deck.md, deck.html or deck.pdf -- they are
# regenerated and your edits will be lost.
#
#   ./watch.sh          live-editing loop with browser auto-reload
#   ./view.sh           serve over HTTP and open the diagram viewer

set -euo pipefail
cd "$(dirname "$0")"

# Print the source-file -> slide-number map. Use this to keep README.md honest
# after adding, deleting or reordering slides.
if [ "${1:-}" = "map" ]; then
  python3 - <<'PY'
import glob, re
n = 0
rows = []
for f in sorted(glob.glob("slides/*.md")):
    blocks = open(f).read().split("\n---\n")
    start = n + 1
    n += len(blocks)
    m = re.search(r"^#+ (.+)$", blocks[0], re.M)
    t = re.sub(r"\*\*|`", "", m.group(1)) if m else "(lead/title)"
    rows.append((f.replace("slides/", ""),
                 str(start) if len(blocks) == 1 else f"{start}-{n}",
                 t[:46]))
w = max(len(r[0]) for r in rows)
print(f"{'file'.ljust(w)}  {'slides':>7}  topic")
for a, b, c in rows:
    print(f"{a.ljust(w)}  {b:>7}  {c}")
print(f"\n{n} slides")
PY
  exit 0
fi

OUT=deck.md

{
  cat header.md
  for f in slides/*.md; do
    printf '\n'
    cat "$f"
    printf '\n\n---\n'
  done
} > "$OUT"

# Drop the trailing slide separator so the deck does not end on a blank slide.
printf '%s\n' "$(sed -e '$ d' "$OUT")" > "$OUT.tmp" && mv "$OUT.tmp" "$OUT"

# Click-to-zoom on diagrams. These live in deck.md rather than being patched
# into deck.html afterwards, so `marp --watch` preserves them across rebuilds.
if [ -f assets/vendor/panzoom.min.js ] && [ -f assets/deck-zoom.js ]; then
  cat >> "$OUT" <<'HTML'

<script src="assets/vendor/panzoom.min.js"></script>
<script src="assets/deck-zoom.js"></script>
HTML
fi

SLIDES=$(( $(grep -c '^---$' "$OUT") - 1 ))   # minus the front-matter close
echo "wrote $OUT ($SLIDES slides)"

if [ "${1:-all}" = "md" ]; then
  exit 0
fi

npx --yes @marp-team/marp-cli@latest "$OUT" --html --allow-local-files -o deck.html
npx --yes @marp-team/marp-cli@latest "$OUT" --pdf  --allow-local-files -o deck.pdf

echo "wrote deck.html and deck.pdf"
