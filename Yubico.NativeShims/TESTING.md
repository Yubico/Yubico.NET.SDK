# Testing NativeShims glibc Compatibility

This document describes how to test the glibc compatibility of the compiled NativeShims library.

## Background

Issue #334 reported that NativeShims requires glibc 2.38+, which is too new for many stable Linux distributions. The library should target glibc 2.28 for broader compatibility.

## Quick Test (Recommended)

The fastest way to verify glibc compatibility is to check the symbol versions in the compiled library:

```bash
cd Yubico.NativeShims

# Download artifacts from GitHub Actions (or use locally built library)
# Then run the compatibility check:

chmod +x test-glibc-compatibility.sh
./test-glibc-compatibility.sh linux-x64/libYubico.NativeShims.so
```

**Expected Output:**
```
================================================
NativeShims glibc Compatibility Test
================================================

Testing library: linux-x64/libYubico.NativeShims.so

1. Checking if file is a valid ELF binary...
✓ Valid ELF binary

2. Checking GLIBC version requirements...

Required GLIBC versions:
   2 0x0  10  GLIBC_2.2.5
   1 0x0  11  GLIBC_2.28

Highest GLIBC version required: 2.28
✓ Compatible with GLIBC 2.28+ (requires 2.28)

3. Detailed symbol version analysis...
      2 GLIBC_2.2.5
      1 GLIBC_2.28

4. Dynamic library dependencies...
 0x0000000000000001 (NEEDED)             Shared library: [libpcsclite.so.1]
 0x0000000000000001 (NEEDED)             Shared library: [libcrypto.so.3]
 0x0000000000000001 (NEEDED)             Shared library: [libc.so.6]

5. Checking for symbols that might require newer glibc...
✓ No symbols requiring glibc > 2.28 found

================================================
SUMMARY
================================================
Library: linux-x64/libYubico.NativeShims.so
Maximum GLIBC required: 2.28
Status: ✓ COMPATIBLE with glibc 2.28

This library should work on:
  - Ubuntu 18.04+ (glibc 2.27)
  - Debian 10+ (glibc 2.28)
  - RHEL 8+ (glibc 2.28)
  - CentOS 8+ (glibc 2.28)
  - MX Linux 21+ (glibc 2.31)
```

## Full Runtime Test (Docker Required)

For a complete end-to-end test that actually loads the library in .NET on various distributions:

```bash
cd Yubico.NativeShims

chmod +x test-runtime-compatibility.sh
./test-runtime-compatibility.sh linux-x64/libYubico.NativeShims.so
```

This will:
1. Create a minimal .NET 6 application that loads NativeShims
2. Test it in Docker containers with different glibc versions:
   - Ubuntu 18.04 (glibc 2.27)
   - Ubuntu 20.04 (glibc 2.31)
   - Debian 10 (glibc 2.28)
   - CentOS 8 (glibc 2.28)
3. Verify the library loads and functions work

**Note:** This test requires Docker to be installed and running.

## Testing GitHub Actions Artifacts

After the `build-nativeshims` workflow completes:

1. Go to the workflow run in GitHub Actions
2. Download the `linux-x64` artifact
3. Extract the zip file
4. Run the test script:

```bash
unzip linux-x64.zip -d linux-x64/
chmod +x test-glibc-compatibility.sh
./test-glibc-compatibility.sh linux-x64/libYubico.NativeShims.so
```

## Manual Verification (No Scripts)

You can also manually check the glibc version requirements:

```bash
# Check symbol versions
readelf -V linux-x64/libYubico.NativeShims.so | grep GLIBC

# Find maximum version
readelf -V linux-x64/libYubico.NativeShims.so | grep -oP 'GLIBC_\K[0-9.]+' | sort -V | tail -1

# Expected output: 2.28 (or lower)
```

## What to Look For

### ✅ Good Signs (glibc 2.28 compatible)
- Maximum GLIBC version is 2.28 or lower
- No symbols requiring GLIBC_2.29 or higher
- Test passes on Ubuntu 18.04 and Debian 10

### ❌ Bad Signs (NOT glibc 2.28 compatible)
- Maximum GLIBC version is 2.29 or higher
- Symbols requiring GLIBC_2.3x or higher appear
- Test fails on Ubuntu 18.04 or Debian 10

## Common Issues

### Issue: "readelf: command not found"
**Solution:** Install binutils:
```bash
sudo apt-get install binutils
```

### Issue: Docker tests fail to build
**Solution:** Ensure Docker is installed and running:
```bash
docker --version
sudo systemctl start docker
```

### Issue: Library requires glibc > 2.28
**Solution:** This means Zig compilation is not targeting the correct glibc version. Check:
1. Zig wrapper scripts are using `-target <arch>-linux-gnu.2.28`
2. Environment variables CC/CXX are set correctly
3. CMake is picking up the Zig wrappers

## Related Files

- `.github/workflows/build-nativeshims.yml` - GitHub Actions workflow that compiles NativeShims
- `cmake/aarch64-linux-gnu.toolchain.cmake` - ARM64 cross-compilation toolchain
- `build-linux-amd64.sh` - Local build script for AMD64
- `build-linux-arm64.sh` - Local build script for ARM64

## References

- Issue #334: [BUG] Yubico.NativeShims.so requires glibc 2.38+ on Linux
- glibc version history: https://sourceware.org/glibc/wiki/Glibc%20Timeline
- Zig cross-compilation: https://ziglang.org/learn/overview/#cross-compiling-is-a-first-class-use-case
