# Plan: Publish SDK 2.0 Preview to GitHub Packages

## Context

Internal teams need to start testing the 2.0 SDK from the `yubikit-applets` (and `yubikit`) branches.
Currently the CI workflow only builds and runs tests — no packages are published anywhere.
Publishing to `https://nuget.pkg.github.com/Yubico/index.json` (GitHub Packages) lets internal teams
add that feed and consume `Yubico.YubiKit.*` preview packages without waiting for a public release.

## Files to Change

| File | What changes |
|------|-------------|
| `.github/workflows/build.yml` | Add `yubikit-applets` trigger, `packages: write` permission, pack+publish steps |
| `Directory.Packages.props` | Bump version baseline from `1.0.0-preview.1` → `2.0.0-preview.1` |
| `nuget.config` | No changes — restore sources stay nuget.org only |

## Changes Detail

### 1. `.github/workflows/build.yml`

**Add `yubikit-applets` to branch triggers:**
```yaml
on:
  push:
    branches: [ yubikit, yubikit-applets ]
  pull_request:
    branches: [ yubikit, yubikit-applets ]
```

**Add `packages: write` permission** (required for GitHub Packages push):
```yaml
jobs:
  build-and-test:
    runs-on: ubuntu-latest
    permissions:
      contents: read
      packages: write
```

**Add pack + publish steps after the existing test step:**
```yaml
      - name: Pack NuGet packages
        run: dotnet toolchain.cs pack --package-version 2.0.0-preview.${{ github.run_number }}

      - name: Publish to GitHub Packages
        if: github.event_name == 'push'
        run: |
          dotnet nuget push "artifacts/packages/*.nupkg" \
            --source https://nuget.pkg.github.com/Yubico/index.json \
            --api-key ${{ secrets.GITHUB_TOKEN }} \
            --skip-duplicate
```

Key decisions:
- Version: `2.0.0-preview.<run_number>` — monotonically increasing, no manual bumping
- `if: github.event_name == 'push'` — only publish on push, not on PR builds
- Uses `GITHUB_TOKEN` (automatically provided by GitHub Actions) — no secrets setup needed
- `--skip-duplicate` — safe to re-run if a version already exists
- Uses `dotnet nuget push` directly (not `toolchain.cs publish`) to bypass the local-feed `setup-feed` dependency

### 2. `Directory.Packages.props`

Change line 6:
```xml
<!-- Before -->
<YubiKitVersion>1.0.0-preview.1</YubiKitVersion>

<!-- After -->
<YubiKitVersion>2.0.0-preview.1</YubiKitVersion>
```

This sets the local/manual build version to 2.0. CI overrides to `2.0.0-preview.<run_number>`.

### 3. `nuget.config`

**No changes to the restore sources.** The SDK does not consume its own packages, so `Yubico_GH` is not needed as a restore source here. The CI publish step pushes directly to the URL — no source registration needed.

The commented-out `Yubico_GH` block stays as-is for documentation purposes.

**Why not uncomment it?**
- `<packageSourceMapping>` only works with `PackageReference` (CPM). It is silently ignored for `packages.config` (legacy) consumers — this causes confusing split behavior.
- Adding `Yubico_GH` as a restore source here would require `GITHUB_TOKEN` to be set for anyone running `dotnet restore`, breaking both legacy consumers and CI environments without the token.
- Internal teams consuming the 2.0 packages should add the `Yubico_GH` source to **their own** `nuget.config` (with appropriate credentials), not inherit it from this repo.

## Verification

1. Push a commit to `yubikit-applets` → CI should trigger
2. Check Actions tab → `build-and-test` job completes with a **Publish to GitHub Packages** step
3. Navigate to `https://github.com/orgs/Yubico/packages` → `Yubico.YubiKit.*` packages appear as `2.0.0-preview.<N>`
4. In a test project: add the `Yubico_GH` source with a GitHub PAT and restore the `2.0.0-preview.*` packages
5. Confirm PRs do NOT publish (only push events trigger the publish step)

## What This Does NOT Change

- The `develop` branch build (unaffected — separate workflow or no workflow)
- The `toolchain.cs` script (no changes needed)
- How external/public releases work (future concern)
