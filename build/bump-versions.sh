#!/bin/bash

# bump-versions.sh - Update version numbers in project files
# Usage: ./bump-versions.sh <version>

set -e

if [ $# -ne 1 ]; then
    echo "Usage: $0 <version>"
    echo "Example: $0 1.14.0"
    exit 1
fi

VERSION="$1"

# Validate version format (basic semver check)
if ! [[ "$VERSION" =~ ^[0-9]+\.[0-9]+\.[0-9]+(-[a-zA-Z0-9]+(\.[0-9]+)?)?$ ]]; then
    echo "Error: Version must follow semver format (e.g., 1.14.0 or 1.14.0-alpha.1)"
    exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

VERSIONS_PROPS="$PROJECT_ROOT/build/Versions.props"
VCPKG_JSON="$PROJECT_ROOT/Yubico.NativeShims/vcpkg.json"

echo "Updating version to: $VERSION"

# Update build/Versions.props
if [ -f "$VERSIONS_PROPS" ]; then
    echo "Updating $VERSIONS_PROPS..."
    sed -i "s/<CommonVersion>.*<\/CommonVersion>/<CommonVersion>$VERSION<\/CommonVersion>/" "$VERSIONS_PROPS"
    echo "✓ Updated build/Versions.props"
else
    echo "Error: $VERSIONS_PROPS not found"
    exit 1
fi

# Update Yubico.NativeShims/vcpkg.json
if [ -f "$VCPKG_JSON" ]; then
    echo "Updating $VCPKG_JSON..."
    sed -i "s/\"version\": \".*\"/\"version\": \"$VERSION\"/" "$VCPKG_JSON"
    echo "✓ Updated Yubico.NativeShims/vcpkg.json"
else
    echo "Warning: $VCPKG_JSON not found, skipping..."
fi

echo "Version bump complete!"