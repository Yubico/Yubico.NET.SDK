#!/usr/bin/env bash
# Local arm64 macOS NativeShims build for development — bypasses vcpkg.
# Uses brew OpenSSL@3 instead of vcpkg-bundled OpenSSL.
# Replaces the dylib in the consumed NuGet cache so Phase 3+ P/Invoke
# calls resolve the latest exports without waiting for a NuGet release.
#
# Reverts:  mv "$CACHE.original" "$CACHE"
# Re-apply: re-run this script.

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
NS_DIR="$REPO_ROOT/Yubico.NativeShims"

# Currently consumed NuGet version (kept in sync with Yubico.Core.csproj).
VERSION="$(sed -En 's/.*Yubico\.NativeShims" Version="([^"]+)".*/\1/p' "$REPO_ROOT/Yubico.Core/src/Yubico.Core.csproj" | head -1)"
if [ -z "$VERSION" ]; then
  echo "ERROR: could not detect consumed NativeShims version from Yubico.Core.csproj" >&2
  exit 1
fi
echo "Consumed NativeShims version: $VERSION"

CACHE="$HOME/.nuget/packages/yubico.nativeshims/$VERSION/runtimes/osx-arm64/native/libYubico.NativeShims.dylib"
if [ ! -f "$CACHE" ]; then
  echo "ERROR: NuGet cache dylib not found at $CACHE" >&2
  echo "Run 'dotnet restore' first." >&2
  exit 1
fi

# Configure + build
cd "$NS_DIR"
rm -rf build-local-arm64
cmake -S . -B build-local-arm64 \
  -DCMAKE_BUILD_TYPE=Release \
  -DCMAKE_OSX_ARCHITECTURES=arm64 \
  -DOPENSSL_ROOT_DIR=/opt/homebrew/opt/openssl@3 \
  -DOPENSSL_USE_STATIC_LIBS=FALSE
cmake --build build-local-arm64 -j

DYLIB="$NS_DIR/build-local-arm64/libYubico.NativeShims.dylib"

# Export-table parity check — fail fast if exports.llvm drifted from impl
bash "$NS_DIR/tests/check_exports.sh" "$DYLIB"

# Backup once, then override
if [ ! -f "$CACHE.original" ]; then
  cp "$CACHE" "$CACHE.original"
  echo "Backup created: $CACHE.original"
fi
cp "$DYLIB" "$CACHE"
echo "Override applied: $CACHE"

# Sanity report
echo "--- new dylib ---"
file "$DYLIB"
shasum -a 256 "$DYLIB"
