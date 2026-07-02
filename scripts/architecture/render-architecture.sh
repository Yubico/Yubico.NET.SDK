#!/usr/bin/env bash
set -euo pipefail
export LC_ALL=C

# render-architecture.sh
#
# Renders every ```mermaid block in the diagrams file to a deterministically
# named SVG (vector, for slides) and PNG (3x raster), using a pinned Mermaid
# CLI version, and writes a freshness manifest mapping each image to the
# sha256 of its SOURCE mermaid block.
#
# The manifest is what makes "image drift" detectable: check-architecture-images.sh
# recomputes source-block hashes and compares them to the manifest, so a changed
# Mermaid block without a re-render fails CI (mmdc output itself is not
# byte-deterministic, so we hash the source, not the image).
#
# Usage:
#   scripts/architecture/render-architecture.sh          # render all + write manifest
#   scripts/architecture/render-architecture.sh --check  # dry: fail if mmdc missing/mismatched version
#
# Requires @mermaid-js/mermaid-cli (mmdc) at the pinned version (see
# architecture-images.env). CI installs it; locally: npm i -g @mermaid-js/mermaid-cli.

here="$(cd "$(dirname "$0")" && pwd)"
repo_root="$(git rev-parse --show-toplevel 2>/dev/null || pwd)"
cd "$repo_root"

# shellcheck source=/dev/null
. "$here/architecture-images.env"
# shellcheck source=/dev/null
. "$here/lib-architecture-blocks.sh"

check_only=0
[[ "${1:-}" == "--check" ]] && check_only=1

# --- locate mmdc + verify pinned version ---------------------------------
# Probe candidates and pick the one whose --version equals the pin, so a stale
# global mmdc does not shadow a correctly-versioned local install.
mmdc_bin=""
installed_ver=""
for cand in "mmdc" "npx --no-install @mermaid-js/mermaid-cli"; do
  # Only consider a candidate that can actually run.
  ver="$($cand --version 2>/dev/null | tr -d '[:space:]' || true)"
  [[ -z "$ver" ]] && continue
  if [[ "$ver" == "$MERMAID_CLI_VERSION" ]]; then
    mmdc_bin="$cand"; installed_ver="$ver"; break
  fi
  # Remember the first runnable candidate for a helpful mismatch message.
  [[ -z "$installed_ver" ]] && installed_ver="$ver"
done

if [[ -z "$mmdc_bin" ]]; then
  if [[ -z "$installed_ver" ]]; then
    echo "ERROR: mmdc not found. Install @mermaid-js/mermaid-cli@${MERMAID_CLI_VERSION}." >&2
  else
    echo "ERROR: no mmdc at pinned version '$MERMAID_CLI_VERSION' (found '$installed_ver')." >&2
    echo "       Renders must be reproducible. Install the pinned version or bump architecture-images.env deliberately." >&2
  fi
  exit 2
fi

n_blocks="$(arch_count_blocks "$ARCH_DIAGRAMS_FILE")"
n_names="${#ARCH_IMAGE_NAMES[@]}"
if [[ "$n_blocks" -ne "$n_names" ]]; then
  echo "ERROR: block/name count mismatch: $n_blocks mermaid blocks vs $n_names names in architecture-images.env." >&2
  echo "       Update ARCH_IMAGE_NAMES to match the diagrams file (order-sensitive)." >&2
  exit 2
fi

if [[ $check_only -eq 1 ]]; then
  echo "render preflight OK: mmdc $installed_ver, $n_blocks blocks == $n_names names."
  exit 0
fi

mkdir -p "$ARCH_IMAGES_DIR" "$ARCH_PNG_DIR"
tmpd="$(mktemp -d)"
trap 'rm -rf "$tmpd"' EXIT

manifest_tmp="$tmpd/manifest"
: > "$manifest_tmp"
{
  echo "# architecture image freshness manifest"
  echo "# image_basename<TAB>source_block_sha256<TAB>svg_sha256<TAB>png_sha256"
  echo "# regenerate with scripts/architecture/render-architecture.sh"
  echo "mermaid_cli_version=$MERMAID_CLI_VERSION"
} >> "$manifest_tmp"

i=0
while [[ $i -lt $n_blocks ]]; do
  name="${ARCH_IMAGE_NAMES[$i]}"
  block_file="$tmpd/$name.mmd"
  arch_extract_block "$ARCH_DIAGRAMS_FILE" "$i" > "$block_file"

  if [[ ! -s "$block_file" ]]; then
    echo "ERROR: empty mermaid block at index $i ($name)." >&2
    exit 2
  fi

  echo "rendering [$i] $name ..."
  # SVG (transparent) + PNG (3x, white bg)
  $mmdc_bin -i "$block_file" -o "$ARCH_IMAGES_DIR/$name.svg" \
    -c "$ARCH_MERMAID_CONFIG" -b transparent >/dev/null 2>&1
  $mmdc_bin -i "$block_file" -o "$ARCH_PNG_DIR/$name.png" \
    -c "$ARCH_MERMAID_CONFIG" -b white -s 3 >/dev/null 2>&1

  h="$(arch_hash_stdin < "$block_file")"
  svg_h="$(arch_hash_file "$ARCH_IMAGES_DIR/$name.svg")"
  png_h="$(arch_hash_file "$ARCH_PNG_DIR/$name.png")"
  printf '%s\t%s\t%s\t%s\n' "$name" "$h" "$svg_h" "$png_h" >> "$manifest_tmp"
  i=$((i+1))
done

mv "$manifest_tmp" "$ARCH_MANIFEST"
echo "rendered $n_blocks diagrams to $ARCH_IMAGES_DIR (+png/), manifest updated."
