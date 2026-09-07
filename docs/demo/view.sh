#!/usr/bin/env bash
# Serve the deck locally and open the diagram viewer.
#
# Serving over HTTP (rather than opening file://) lets the viewer inline each SVG,
# so pan/zoom transforms the vector itself and stays crisp at any magnification.
# Opening diagrams.html directly from disk still works, but falls back to <img>.
#
#   ./view.sh            -> diagram viewer
#   ./view.sh deck       -> the deck in the browser
#   ./view.sh --port 9000

set -euo pipefail
cd "$(dirname "$0")"

PORT=8791
TARGET=diagrams.html

while [ $# -gt 0 ]; do
  case "$1" in
    deck)     TARGET=deck.html; shift ;;
    --port)   PORT="$2"; shift 2 ;;
    *)        echo "usage: ./view.sh [deck] [--port N]" >&2; exit 2 ;;
  esac
done

# Reuse an already-running server on this port if there is one.
if ! curl -s -o /dev/null --max-time 1 "http://localhost:$PORT/$TARGET"; then
  echo "starting server on :$PORT"
  python3 -m http.server "$PORT" --bind 127.0.0.1 >/dev/null 2>&1 &
  echo $! > .view-server.pid
  for _ in $(seq 1 40); do
    curl -s -o /dev/null --max-time 1 "http://localhost:$PORT/$TARGET" && break
    sleep 0.1
  done
else
  echo "reusing server on :$PORT"
fi

echo "  diagrams : http://localhost:$PORT/diagrams.html"
echo "  deck     : http://localhost:$PORT/deck.html"
echo "  stop     : ./view.sh-stop  (or kill \$(cat .view-server.pid))"
open "http://localhost:$PORT/$TARGET"
