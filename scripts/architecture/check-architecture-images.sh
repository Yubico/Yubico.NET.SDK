#!/usr/bin/env bash
set -euo pipefail
export LC_ALL=C

# check-architecture-images.sh
#
# Verifies rendered architecture images are present and FRESH relative to their
# source Mermaid blocks, without requiring mmdc (so it runs in any CI job):
#
#   1. block/name count parity (diagrams file vs architecture-images.env).
#   2. every expected SVG and PNG exists.
#   3. every image referenced in the diagrams file's table exists.
#   4. freshness: for each block, sha256(source block) == manifest entry.
#      A changed Mermaid block without a re-render (which rewrites the manifest)
#      fails here. This is the mechanism behind ISA ISC-29.
#   5. manifest's pinned mermaid_cli_version matches architecture-images.env.
#
# Exit 0 = images present and fresh. Non-zero = drift / missing / stale.

here="$(cd "$(dirname "$0")" && pwd)"
repo_root="$(git rev-parse --show-toplevel 2>/dev/null || pwd)"
cd "$repo_root"

# shellcheck source=/dev/null
. "$here/architecture-images.env"
# shellcheck source=/dev/null
. "$here/lib-architecture-blocks.sh"

fail=0
err() { printf 'ERROR: %s\n' "$1" >&2; fail=1; }

[[ -f "$ARCH_DIAGRAMS_FILE" ]] || { echo "ERROR: missing $ARCH_DIAGRAMS_FILE" >&2; exit 2; }
[[ -f "$ARCH_MANIFEST" ]] || { echo "ERROR: missing manifest $ARCH_MANIFEST (run render-architecture.sh)" >&2; exit 2; }

# 1. count parity
n_blocks="$(arch_count_blocks "$ARCH_DIAGRAMS_FILE")"
n_names="${#ARCH_IMAGE_NAMES[@]}"
if [[ "$n_blocks" -ne "$n_names" ]]; then
  err "block/name count mismatch: $n_blocks mermaid blocks vs $n_names names in architecture-images.env"
fi

# 5. manifest pinned version parity
manifest_ver="$(sed -n 's/^mermaid_cli_version=//p' "$ARCH_MANIFEST" | head -n1)"
if [[ "$manifest_ver" != "$MERMAID_CLI_VERSION" ]]; then
  err "manifest mermaid_cli_version '$manifest_ver' != pinned '$MERMAID_CLI_VERSION' (re-render needed)"
fi

# 2 + 4. existence + freshness per block/name
i=0
while [[ $i -lt $n_names ]]; do
  name="${ARCH_IMAGE_NAMES[$i]}"
  svg="$ARCH_IMAGES_DIR/$name.svg"
  png="$ARCH_PNG_DIR/$name.png"
  [[ -f "$svg" ]] || err "missing SVG: $svg"
  [[ -f "$png" ]] || err "missing PNG: $png"

  if [[ $i -lt $n_blocks ]]; then
    want="$(arch_block_hash "$ARCH_DIAGRAMS_FILE" "$i")"
    have="$(awk -F'\t' -v n="$name" '$1==n {print $2}' "$ARCH_MANIFEST" | head -n1)"
    if [[ -z "$have" ]]; then
      err "no manifest entry for '$name' (run render-architecture.sh)"
    elif [[ "$want" != "$have" ]]; then
      err "stale image '$name': source block changed since last render (source=$want manifest=$have). Run render-architecture.sh."
    fi
  fi
  i=$((i+1))
done

# 3. every image referenced in the diagrams file exists (skip the sdk.* examples
#    used only in the regenerate command snippet).
while IFS= read -r ref; do
  case "$ref" in
    */sdk.svg|*/sdk.png|images/sdk.svg|images/png/sdk.png) continue ;;
  esac
  target="docs/architecture/$ref"
  [[ -f "$target" ]] || err "diagrams file references missing image: $ref"
done < <(grep -oE 'images/[A-Za-z0-9/._-]+\.(svg|png)' "$ARCH_DIAGRAMS_FILE" | sort -u)

if [[ $fail -eq 0 ]]; then
  echo "architecture images OK: $n_names diagrams present and fresh (manifest matches source blocks)."
fi
exit $fail
