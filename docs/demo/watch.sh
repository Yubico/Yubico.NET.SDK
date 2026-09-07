#!/usr/bin/env bash
# Live-editing loop for the deck.
#
#   ./watch.sh
#
# Edit any file in slides/ (or header.md) and the browser reloads by itself.
# Marp's watch mode injects a livereload client into deck.html, so there is
# nothing to click and no cache to bust.
#
# Two processes run:
#   1. a small watcher that re-concatenates slides/*.md -> deck.md on change
#   2. marp --watch, which rebuilds deck.dev.html and pushes the reload
#
# Output goes to deck.dev.html, NOT deck.html. Marp's watch mode injects a
# livereload websocket client into its output; writing that into the committed
# deck.html would dirty the working tree on every session. deck.dev.html is
# gitignored and removed on exit.
#
# The PDF is NOT regenerated on every keystroke -- run ./build.sh when you want
# a fresh deck.pdf (and a clean deck.html).
#
# Ctrl-C stops both.

set -euo pipefail
cd "$(dirname "$0")"

PORT=${PORT:-8791}

cleanup() {
  [ -n "${WATCH_PID:-}" ] && kill "$WATCH_PID" 2>/dev/null || true
  [ -n "${HTTP_PID:-}"  ] && kill "$HTTP_PID"  2>/dev/null || true
  wait 2>/dev/null || true
  rm -f deck.dev.html
  echo
  echo "stopped. run ./build.sh to refresh deck.html and deck.pdf."
}
trap cleanup EXIT INT TERM

# Build once so deck.md exists before marp starts watching it.
./build.sh md >/dev/null

# --- 1. rebuild deck.md whenever a source slide changes --------------------
python3 - "$PWD" <<'PY' &
import hashlib, glob, os, subprocess, sys, time

root = sys.argv[1]
os.chdir(root)

def sources():
    return sorted(glob.glob("slides/*.md")) + ["header.md"]

def fingerprint():
    h = hashlib.sha256()
    for f in sources():
        try:
            st = os.stat(f)
            h.update(f.encode())
            h.update(str(st.st_mtime_ns).encode())
            h.update(str(st.st_size).encode())
        except FileNotFoundError:
            h.update(b"gone:" + f.encode())
    return h.hexdigest()

last = fingerprint()
while True:
    time.sleep(0.4)
    now = fingerprint()
    if now != last:
        last = now
        try:
            out = subprocess.run(["./build.sh", "md"], capture_output=True, text=True)
            msg = (out.stdout or out.stderr).strip().splitlines()
            print("  \033[36m·\033[0m " + (msg[-1] if msg else "rebuilt deck.md"), flush=True)
        except Exception as e:
            print(f"  \033[31m!\033[0m rebuild failed: {e}", flush=True)
PY
WATCH_PID=$!

# --- 2. static server, so inline-SVG zoom works (file:// is CORS-blocked) ---
if ! curl -s -o /dev/null --max-time 1 "http://localhost:$PORT/diagrams.html"; then
  python3 -m http.server "$PORT" --bind 127.0.0.1 >/dev/null 2>&1 &
  HTTP_PID=$!
fi

cat <<EOF

  editing  slides/*.md   ->  deck.html reloads automatically
  deck     http://localhost:$PORT/deck.dev.html
  diagrams http://localhost:$PORT/diagrams.html

  ./build.sh   regenerate deck.pdf as well
  Ctrl-C       stop

EOF

# --- 3. marp watch: rebuilds deck.html + livereload -------------------------
npx --yes @marp-team/marp-cli@latest deck.md \
  --watch --html --allow-local-files -o deck.dev.html
