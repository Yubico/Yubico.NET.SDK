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
    local archive="$TEMP_DIR/package/buildTransitive/static/$rid/$archive_name"

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

assert_msbuild_fails() {
    local log_name="$1"
    local expected_message="$2"
    shift 2

    if dotnet msbuild "$TEST_PROJECT" \
        -nologo \
        -t:Validate \
        -p:TargetsPath="$TEMP_DIR/package/build/Yubico.NativeShims.targets" \
        -p:TransitiveTargetsPath="$TEMP_DIR/package/buildTransitive/Yubico.NativeShims.targets" \
        -p:PublishAot=true \
        "$@" > "$TEMP_DIR/$log_name.log" 2>&1; then
        echo "ERROR: $log_name validation unexpectedly succeeded" >&2
        exit 1
    fi

    grep -Fq "$expected_message" "$TEMP_DIR/$log_name.log"
}

mkdir -p "$TEMP_DIR/package/build" "$TEMP_DIR/package/buildTransitive"
cp "$TARGETS_SOURCE" "$TEMP_DIR/package/build/Yubico.NativeShims.targets"
cp "$TARGETS_SOURCE" "$TEMP_DIR/package/buildTransitive/Yubico.NativeShims.targets"

dotnet msbuild "$TEST_PROJECT" \
    -nologo \
    -t:ValidateNonAot \
    -p:TargetsPath="$TEMP_DIR/package/build/Yubico.NativeShims.targets" \
    -p:TransitiveTargetsPath="$TEMP_DIR/package/buildTransitive/Yubico.NativeShims.targets" \
    -p:PublishAot=false \
    -p:RuntimeIdentifier=osx-arm64

dotnet msbuild "$TEST_PROJECT" \
    -nologo \
    -t:ValidateNonAot \
    -p:TargetsPath="$TEMP_DIR/package/build/Yubico.NativeShims.targets" \
    -p:TransitiveTargetsPath="$TEMP_DIR/package/buildTransitive/Yubico.NativeShims.targets" \
    -p:RuntimeIdentifier=osx-arm64

validate_rid win-x64 Yubico.NativeShims.lib
validate_rid win-x86 Yubico.NativeShims.lib
validate_rid win-arm64 Yubico.NativeShims.lib
validate_rid linux-x64 libYubico.NativeShims.a
validate_rid linux-arm64 libYubico.NativeShims.a
validate_rid osx-x64 libYubico.NativeShims.a
validate_rid osx-arm64 libYubico.NativeShims.a
validate_rid linux-riscv64 libYubico.NativeShims.future.a

dotnet msbuild "$TEST_PROJECT" \
    -nologo \
    -t:ValidatePublishAssets \
    -p:TargetsPath="$TEMP_DIR/package/build/Yubico.NativeShims.targets" \
    -p:TransitiveTargetsPath="$TEMP_DIR/package/buildTransitive/Yubico.NativeShims.targets" \
    -p:PublishAot=true \
    -p:RuntimeIdentifier=osx-arm64 \
    -p:FakeSidecar="$TEMP_DIR/libYubico.NativeShims.dylib" \
    -p:FakeSameNameOtherAsset="$TEMP_DIR/other/libYubico.NativeShims.dylib" \
    -p:FakeOtherAsset="$TEMP_DIR/keep.dylib"

dotnet msbuild "$TEST_PROJECT" \
    -nologo \
    -t:ValidateNonAotPublishAssets \
    -p:TargetsPath="$TEMP_DIR/package/build/Yubico.NativeShims.targets" \
    -p:TransitiveTargetsPath="$TEMP_DIR/package/buildTransitive/Yubico.NativeShims.targets" \
    -p:PublishAot=false \
    -p:RuntimeIdentifier=osx-arm64 \
    -p:FakeSidecar="$TEMP_DIR/libYubico.NativeShims.dylib" \
    -p:FakeSameNameOtherAsset="$TEMP_DIR/other/libYubico.NativeShims.dylib" \
    -p:FakeOtherAsset="$TEMP_DIR/keep.dylib"

assert_msbuild_fails \
    missing-rid \
    "requires RuntimeIdentifier when PublishAot is true" \
    -p:ExpectedArchive=unused

assert_msbuild_fails \
    unsupported-platform \
    "does not support the platform for RuntimeIdentifier 'freebsd-x64'" \
    -p:RuntimeIdentifier=freebsd-x64 \
    -p:ExpectedArchive=unused

: > "$TEMP_DIR/package/buildTransitive/static/linux-x64/duplicate.a"
assert_msbuild_fails \
    duplicate \
    "expected exactly one static archive for RuntimeIdentifier 'linux-x64'" \
    -p:RuntimeIdentifier=linux-x64 \
    -p:ExpectedArchive=unused
rm "$TEMP_DIR/package/buildTransitive/static/linux-x64/duplicate.a"

rm "$TEMP_DIR/package/buildTransitive/static/osx-arm64/libYubico.NativeShims.a"
assert_msbuild_fails \
    missing \
    "does not provide a static archive for RuntimeIdentifier 'osx-arm64'" \
    -p:RuntimeIdentifier=osx-arm64 \
    -p:ExpectedArchive=unused

assert_no_nuspec_glob() {
    # Classic nuget.exe pack does not strip the literal source-directory
    # prefix from recursive "**" globs the way "dotnet pack" does, which
    # silently nests runtime assets one level too deep and breaks RID-based
    # asset resolution. Per-RID <file> entries must be explicit and
    # generated dynamically by the packing workflow instead.
    if grep -Eq '<file[^>]*\*\*' "$NUSPEC"; then
        echo "ERROR: nuspec must not contain wildcard glob file entries" >&2
        exit 1
    fi
}

assert_no_nuspec_glob

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
