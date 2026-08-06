#!/usr/bin/env bash
# Yubico .NET SDK v2 — ALPHA feed bootstrap (macOS/Linux)
# Adds the anonymous public alpha NuGet feed and (best-effort) verifies build provenance.
set -euo pipefail

FEED_URL="https://yubico.github.io/Yubico.NET.SDK/alpha/index.json"
SRC_NAME="yubikit-alpha"
VERSION="2.0.0-alpha.1"

cat <<'BANNER'
============================================================
 Yubico .NET SDK v2 - ALPHA
 Pre-release, subject to change, and NOT yet security-audited
 by Yubico. No security guarantees. Package names/namespaces
 may change. Evaluation / hackathon use only.
============================================================
BANNER

read -r -p "Add the alpha feed and continue? [y/N] " reply
case "$reply" in
  [yY]) ;;
  *) echo "Aborted."; exit 1 ;;
esac

# Add the feed as an ADDITIONAL source (keep nuget.org for transitive deps like Yubico.NativeShims).
if dotnet nuget list source 2>/dev/null | grep -q "$FEED_URL"; then
  echo "Feed already registered."
else
  dotnet nuget add source "$FEED_URL" -n "$SRC_NAME"
fi

# Best-effort provenance verification (requires GitHub CLI; skipped if absent).
if command -v gh >/dev/null 2>&1; then
  echo "Verifying build provenance (best-effort)..."
  tmp="$(mktemp -d)"
  if curl -fsSL "https://yubico.github.io/Yubico.NET.SDK/alpha/flatcontainer/yubico.yubikit.core/${VERSION}/yubico.yubikit.core.${VERSION}.nupkg" -o "$tmp/core.nupkg"; then
    if gh attestation verify "$tmp/core.nupkg" --repo Yubico/Yubico.NET.SDK; then
      echo "Provenance verified."
    else
      echo "WARNING: provenance verification failed or unavailable for this alpha. Continuing."
    fi
  fi
  rm -rf "$tmp"
else
  echo "NOTE: GitHub CLI (gh) not found — skipping provenance check. These are unsigned alpha packages."
fi

cat <<EOF

Done. Install a package with, for example:
  dotnet add package Yubico.YubiKit.Core --version ${VERSION} --prerelease

Teardown:
  dotnet nuget remove source ${SRC_NAME}
EOF
