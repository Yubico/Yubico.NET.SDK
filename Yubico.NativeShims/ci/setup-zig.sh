#!/usr/bin/env bash
set -euo pipefail

if [ "$#" -ne 1 ]; then
    echo "usage: $0 <zig-target>" >&2
    exit 2
fi
if [ -z "${GITHUB_ENV:-}" ] || [ -z "${GITHUB_PATH:-}" ]; then
    echo "ERROR: setup-zig.sh must run in GitHub Actions" >&2
    exit 2
fi

ZIG_TARGET="$1"
ZIG_VERSION="0.15.2"
ZIG_ARCHIVE="zig-x86_64-linux-${ZIG_VERSION}.tar.xz"
ZIG_URL="https://ziglang.org/download/${ZIG_VERSION}/${ZIG_ARCHIVE}"
ZIG_PUBLIC_KEY="RWSGOq2NVecA2UPNdBUZykf1CCb147pkmdtYxgb3Ti+JO/wCYvhbAb/U"
WRAPPER_DIR="$HOME/zig-wrappers"

trap 'rm -f "$ZIG_ARCHIVE" "$ZIG_ARCHIVE.minisig"' EXIT

sudo apt-get update -qq
sudo apt-get install -y minisign
wget -q "$ZIG_URL"
wget -q "$ZIG_URL.minisig"
minisign -Vm "$ZIG_ARCHIVE" -P "$ZIG_PUBLIC_KEY"

tar -xf "$ZIG_ARCHIVE"
sudo rm -rf /usr/local/zig
sudo mv "zig-x86_64-linux-${ZIG_VERSION}" /usr/local/zig
echo "/usr/local/zig" >> "$GITHUB_PATH"

mkdir -p "$WRAPPER_DIR"
cat > "$WRAPPER_DIR/zig-cc" <<EOF
#!/usr/bin/env bash
exec /usr/local/zig/zig cc -target "$ZIG_TARGET" -O2 -s "\$@"
EOF
cat > "$WRAPPER_DIR/zig-c++" <<EOF
#!/usr/bin/env bash
exec /usr/local/zig/zig c++ -target "$ZIG_TARGET" -O2 -s "\$@"
EOF
chmod +x "$WRAPPER_DIR/zig-cc" "$WRAPPER_DIR/zig-c++"

SMOKE_SRC="$(mktemp -u --suffix=.c)"
trap 'rm -f "$ZIG_ARCHIVE" "$ZIG_ARCHIVE.minisig" "$SMOKE_SRC" "$SMOKE_SRC.out"' EXIT
echo 'int main(void) { return 0; }' > "$SMOKE_SRC"
"$WRAPPER_DIR/zig-cc" "$SMOKE_SRC" -o "$SMOKE_SRC.out"
echo "CC=$WRAPPER_DIR/zig-cc" >> "$GITHUB_ENV"
echo "CXX=$WRAPPER_DIR/zig-c++" >> "$GITHUB_ENV"
