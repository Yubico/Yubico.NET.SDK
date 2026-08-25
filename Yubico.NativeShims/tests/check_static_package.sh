#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
NATIVE_SHIMS_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
TARGETS_SOURCE="$NATIVE_SHIMS_DIR/msbuild/Yubico.NativeShims.Aot.targets"
NUSPEC="$NATIVE_SHIMS_DIR/Yubico.NativeShims.nuspec"
TEST_PROJECT="$SCRIPT_DIR/StaticPackageValidation.proj"
TEMP_DIR="$(mktemp -d)"
trap 'rm -rf "$TEMP_DIR"' EXIT

assert_nuspec_entry() {
    local source_path="$1"
    local target_path="$2"

    if ! grep -Fq "src=\"$source_path\" target=\"$target_path\"" "$NUSPEC"; then
        echo "ERROR: nuspec entry missing: $source_path -> $target_path" >&2
        exit 1
    fi
}

validate_rid() {
    local rid="$1"
    local archive_name="$2"
    local archive="$TEMP_DIR/package/runtimes/$rid/native/static/$archive_name"

    mkdir -p "$(dirname "$archive")"
    : > "$archive"

    dotnet msbuild "$TEST_PROJECT" \
        -nologo \
        -t:Validate \
        -p:TargetsPath="$TEMP_DIR/package/build/Yubico.NativeShims.targets" \
        -p:TransitiveTargetsPath="$TEMP_DIR/package/buildTransitive/Yubico.NativeShims.targets" \
        -p:PublishAot=true \
        -p:RuntimeIdentifier="$rid" \
        -p:ExpectedArchive="$archive"
}

mkdir -p "$TEMP_DIR/package/build" "$TEMP_DIR/package/buildTransitive"
cp "$TARGETS_SOURCE" "$TEMP_DIR/package/build/Yubico.NativeShims.targets"
cp "$TARGETS_SOURCE" "$TEMP_DIR/package/buildTransitive/Yubico.NativeShims.targets"

dotnet msbuild "$TEST_PROJECT" \
    -nologo \
    -t:ValidateNonAot \
    -p:TargetsPath="$TEMP_DIR/package/build/Yubico.NativeShims.targets" \
    -p:TransitiveTargetsPath="$TEMP_DIR/package/buildTransitive/Yubico.NativeShims.targets" \
    -p:PublishAot=false

validate_rid win-x64 Yubico.NativeShims.lib
validate_rid win-x86 Yubico.NativeShims.lib
validate_rid win-arm64 Yubico.NativeShims.lib
validate_rid linux-x64 libYubico.NativeShims.a
validate_rid linux-arm64 libYubico.NativeShims.a
validate_rid osx-x64 libYubico.NativeShims.a
validate_rid osx-arm64 libYubico.NativeShims.a

if dotnet msbuild "$TEST_PROJECT" \
    -nologo \
    -t:Validate \
    -p:TargetsPath="$TEMP_DIR/package/build/Yubico.NativeShims.targets" \
    -p:TransitiveTargetsPath="$TEMP_DIR/package/buildTransitive/Yubico.NativeShims.targets" \
    -p:PublishAot=true \
    -p:ExpectedArchive=unused > "$TEMP_DIR/missing-rid.log" 2>&1; then
    echo "ERROR: PublishAot without a RID unexpectedly succeeded" >&2
    exit 1
fi
grep -Fq "requires RuntimeIdentifier when PublishAot is true" "$TEMP_DIR/missing-rid.log"

if dotnet msbuild "$TEST_PROJECT" \
    -nologo \
    -t:Validate \
    -p:TargetsPath="$TEMP_DIR/package/build/Yubico.NativeShims.targets" \
    -p:TransitiveTargetsPath="$TEMP_DIR/package/buildTransitive/Yubico.NativeShims.targets" \
    -p:PublishAot=true \
    -p:RuntimeIdentifier=linux-musl-x64 \
    -p:ExpectedArchive=unused > "$TEMP_DIR/unsupported.log" 2>&1; then
    echo "ERROR: unsupported PublishAot RID unexpectedly succeeded" >&2
    exit 1
fi
grep -Fq "does not provide a static archive for RuntimeIdentifier 'linux-musl-x64'" "$TEMP_DIR/unsupported.log"

rm "$TEMP_DIR/package/runtimes/osx-arm64/native/static/libYubico.NativeShims.a"
if dotnet msbuild "$TEST_PROJECT" \
    -nologo \
    -t:Validate \
    -p:TargetsPath="$TEMP_DIR/package/build/Yubico.NativeShims.targets" \
    -p:TransitiveTargetsPath="$TEMP_DIR/package/buildTransitive/Yubico.NativeShims.targets" \
    -p:PublishAot=true \
    -p:RuntimeIdentifier=osx-arm64 \
    -p:ExpectedArchive=unused > "$TEMP_DIR/missing.log" 2>&1; then
    echo "ERROR: missing PublishAot archive unexpectedly succeeded" >&2
    exit 1
fi
grep -Fq "static archive is missing for RuntimeIdentifier 'osx-arm64'" "$TEMP_DIR/missing.log"

for rid in win-x64 win-x86 win-arm64; do
    assert_nuspec_entry \
        "$rid/static/Yubico.NativeShims.lib" \
        "runtimes/$rid/native/static/Yubico.NativeShims.lib"
done

for rid in linux-x64 linux-arm64 osx-x64 osx-arm64; do
    assert_nuspec_entry \
        "$rid/static/libYubico.NativeShims.a" \
        "runtimes/$rid/native/static/libYubico.NativeShims.a"
done

assert_nuspec_entry \
    "msbuild/Yubico.NativeShims.Aot.targets" \
    "build/Yubico.NativeShims.targets"
assert_nuspec_entry \
    "msbuild/Yubico.NativeShims.Aot.targets" \
    "buildTransitive/Yubico.NativeShims.targets"
assert_nuspec_entry \
    "msbuild/Yubico.NativeShims.targets" \
    "build/net472/Yubico.NativeShims.targets"
assert_nuspec_entry \
    "msbuild/Yubico.NativeShims.targets" \
    "buildTransitive/net472/Yubico.NativeShims.targets"

echo "Static package validation passed."
