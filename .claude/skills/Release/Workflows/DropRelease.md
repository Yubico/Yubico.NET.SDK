# DropRelease Workflow

End-to-end Yubico .NET SDK release wizard. Drives 7 phases with explicit gates. the operator answers AskUserQuestion prompts; everything else is automated.

## State file

Location: `~/Releases/<version>/.state.json`. Created in phase 1, updated at every phase boundary, read by `/Release resume`.

```json
{
  "version": "1.16.1",
  "previousTag": "1.16.0",
  "releaseDate": "2026-04-29",
  "nativeShimsRebuild": false,
  "nativeShimsVersion": null,
  "nativeShimsRunId": null,
  "nativeShimsPublished": false,
  "nativeShimsBuildRef": null,
  "buildRunId": null,
  "releaseCommit": null,
  "tagPushed": false,
  "docsImageTag": null,
  "publishRetry": null,
  "currentPhase": 4,
  "complete": false,
  "releasePrNumber": null,
  "notesFile": null,
  "categorizedPRs": { "features": [], "bugfixes": [], "docs": [], "deps": [], "security": [], "misc": [] }
}
```

**Date storage convention**: `releaseDate` is always stored as ISO `YYYY-MM-DD`. The Phase 1 prompt collects it in human form (`Month Dth, YYYY`), but the skill normalizes to ISO before writing state. Display formatting is reapplied at write time for `whats-new.md` (long form: `April 29th, 2026`) and the Slack draft (long form). Always derive the display string from the ISO field — never store both.

**Categorization buckets**: All buckets above MUST be present in state (even empty). Phase 3 categorizes "everything else" into `misc`; Phase 7 includes `misc` in both the `whats-new.md` Miscellaneous section and the Slack draft (under a `Miscellaneous 🧰📌` heading) so PRs are never dropped.

The state file is the single source of truth for resume. Update it before any operation that could fail.

## Phase 1 — Pre-flight (cross-platform)

**Prerequisites**:
- `gh auth status` — must be authenticated
- `git remote -v` — confirm `origin` points to `Yubico/Yubico.NET.SDK`

**Steps**:
1. `git fetch --tags origin`
2. `git tag --sort=-v:refname | head -5` → show recent tags, parse latest as `previousTag`
3. `gh pr list --base develop --state open --json number,title,author --limit 20` → display, then `AskUserQuestion`: "Any of these need to merge before release?" Options: "All clear, proceed" / "Wait — I'll merge manually" / "Specific PRs blocking"
4. `AskUserQuestion`: "Confirm release version" — default option is `+1 patch` of `previousTag` (e.g., `1.16.0` → `1.16.1`); also offer `+1 minor`, `+1 major`, custom
5. `AskUserQuestion`: "Release date" — default today (in `Month Dth, YYYY` format matching whats-new.md style)
6. **Hardware test reminder** — print: "Before continuing, confirm you've tested PIV + SCP on real YubiKey hardware. The skill cannot do this for you." Gate with `AskUserQuestion`: "Hardware tests pass?" / "Skip (not recommended)"
7. **Code-signing YubiKey safety gate** — `AskUserQuestion`: "⚠️ IMPORTANT: Your code-signing YubiKey must be UNPLUGGED from this machine during phases 1–4. Integration tests that enumerate YubiKeys can run PIV/PGP resets against any connected key. Only plug it back in when Phase 5 (sign+publish) explicitly asks for it; no other YubiKey operation should touch the key. Is the code-signing YubiKey unplugged?" Options: "Yes, it's unplugged" / "Let me unplug it now". If the operator needs to unplug, wait for confirmation before proceeding.
8. Create `~/Releases/<version>/` and write initial `.state.json`

## Phase 2 — NativeShims gate (cross-platform, conditional)

**Detection**:
```bash
git diff <previousTag>..origin/develop -- Yubico.NativeShims/ --stat
```

**If output is empty** → set `nativeShimsRebuild: false` in state, print "✓ No NativeShims changes since <previousTag>, skipping rebuild", continue to phase 3.

**If output non-empty** →
1. Print the file list
2. `AskUserQuestion`: "NativeShims changed in N files. Rebuild and publish new NativeShims package?" Options: "Yes — rebuild and bump" / "No — current published NativeShims is sufficient" / "Show me the diff first" (in which case loop back after `git diff`)
3. If yes:
   - `AskUserQuestion`: "NativeShims version" — fetch latest from NuGet (`gh api /repos/Yubico/Yubico.NET.SDK/contents/Yubico.NativeShims/version.txt` or query NuGet API), default +1 patch
   - `AskUserQuestion`: "Build NativeShims from which branch?" Options:
     - "develop (build now)" — dispatch immediately; NativeShims must be signed+published before `build.yml`
     - "main (build after PR merge)" — defer dispatch to Phase 4; build from main after the release PR merges
   - Store choice in `state.nativeShimsBuildRef` (`"develop"` or `"main"`)
   - **If develop (immediate)**:
     - `gh workflow run build-nativeshims.yml --ref develop -f version=<nsVersion> -f push-to-dev=false`
     - Capture run ID: `gh run list --workflow=build-nativeshims.yml --limit 1 --json databaseId -q '.[0].databaseId'`
     - Update state: `nativeShimsRebuild: true`, `nativeShimsVersion`, `nativeShimsRunId`
     - **Poll in background** — NativeShims builds are cross-platform and typically take 15–25 minutes. Start a background Bash task:
       ```bash
       while true; do
         status=$(gh run view <runId> --json status,conclusion -q '[.status,.conclusion] | join(",")')
         if [[ "$status" == completed,* ]]; then echo "$status"; break; fi
         sleep 120
       done
       ```
       Print: "NativeShims build dispatched (run ID: `<id>`). Polling in background every 120s — you can continue working."
     - When background task completes, check conclusion. On `failure`: STOP, print `gh run view <runId> --log-failed`. On `success`: update status board.
     - **HARD GATE**: NativeShims MUST be signed (Phase 5 wizard) AND published to NuGet.org BEFORE Phase 4 dispatches `build.yml`. The skill enforces this by deferring `build.yml` dispatch in Phase 4 until Phase 5's NativeShims half completes.
   - **If main (deferred)**:
     - Skip dispatch. Print: "NativeShims build deferred to main. Will dispatch `build-nativeshims.yml --ref main` after PR merges in Phase 4."
     - Update state: `nativeShimsRebuild: true`, `nativeShimsVersion`, `nativeShimsRunId: null`
     - Proceed directly to Phase 3.

## Phase 3 — Release branch (cross-platform)

1. `git checkout develop && git pull origin develop`
2. `git checkout -b release/<version>` (per gitflow + project CLAUDE.md)
3. **Lockfile note**: No manual lockfile repin commit is needed on the release branch. The `build-artifacts` job in `build.yml` runs `dotnet restore --force-evaluate` on main, which auto-resolves to the latest stable NativeShims from nuget.org. The `enforce-branch-policy` job issues a warning (not an error) if a prerelease pin is detected, since the build self-corrects.
4. **Generate release notes draft**:
   - Get last release date from the annotated tag (NOT `gh release view --json publishedAt`, which returns GitHub release creation date, not the actual release cut):
     ```bash
     git tag -l --format='%(creatordate:iso-strict)' <previousTag>
     ```
   - List merged PRs since: `gh pr list --base develop --state merged --search "merged:>=<lastReleaseISO>" --json number,title,labels,url --limit 100`
   - Categorize by PR title prefix and labels (heuristics):
     - `feat:` / `feature/` / label `enhancement` → **Features** (`features` bucket)
     - `fix:` / `bugfix/` / label `bug` → **Bug Fixes** (`bugfixes` bucket)
     - `docs:` / `doc:` → **Documentation** (`docs` bucket)
     - `chore(deps):` / `build(deps):` / dependabot → **Dependencies / Maintenance** (`deps` bucket)
     - `security:` / `ci:` / `.github/workflows/` touched → **Security / CI** (`security` bucket)
     - everything else → **Miscellaneous** (`misc` bucket — never dropped)
   - Omit internal tooling PRs (release automation, Claude skills, CI-only changes) from user-facing notes in whats-new.md. Still include them in `state.categorizedPRs` for the Slack draft.
   - `security` bucket items (CodeQL, fuzzing, static analysis) are NOT rendered under a separate "Security:" header in whats-new.md. Fold them into Bug Fixes (if they fix bugs) or Miscellaneous (if they add tooling).
   - Cache full categorization (all 6 buckets including `misc`) into `state.categorizedPRs` for phase 7 Slack reuse
5. **Insert into `docs/users-manual/getting-started/whats-new.md`**:
   - **Read the PREVIOUS release's section first** as the style reference — match its voice, phrasing, and structure exactly
   - Insert new `### <version>` block under the appropriate `## 1.x.x Releases` heading (create the heading if needed)
   - **Style rules** (derived from existing whats-new.md patterns):
     - Bug Fixes: "Fixed an issue where..." pattern, passive voice
     - Documentation: passive voice ("Documentation has been updated/corrected/added to...")
     - Miscellaneous: passive voice ("...has been switched/added/updated")
     - Features: active voice is acceptable ("Added...", "Introduced...")
     - Dependencies: brief, passive ("Dependencies have been updated...")
   - Emit subsections only for non-empty buckets (Features, Bug Fixes, Documentation, Miscellaneous, Dependencies)
   - Show diff via `git diff docs/users-manual/getting-started/whats-new.md`
6. `AskUserQuestion`: "Release notes look correct?" Options: "Yes, commit" / "Let me edit first" / "Regenerate from PRs"
7. On approval: `git add docs/users-manual/getting-started/whats-new.md && git commit -m "docs: release notes for <version>"`
8. `git push -u origin release/<version>`
   - **Verify**: `git ls-remote --heads origin release/<version>` — must return a ref. If empty, push failed silently.
9. **Build the PR-body file** for `gh pr create` — extract just the new `### <version>` block from `whats-new.md` (between the new heading and the next `### ` heading) into a real temp file, then pass it:
   ```bash
   notes_file=$(mktemp -t release-notes-<version>.XXXXXX.md)
   awk -v v="### <version>" '
     $0 == v {flag=1; print; next}
     flag && /^### / {exit}
     flag {print}
   ' docs/users-manual/getting-started/whats-new.md > "$notes_file"
   gh pr create --base main --head release/<version> \
     --title "Release <version>" \
     --body-file "$notes_file"
   ```
   Capture the returned PR number into `state.releasePrNumber`. Keep the temp file path in state too so phase 6 can reuse it.
10. Print PR URL, instruct the operator to get reviewers

## Phase 4 — Merge + CI dispatch (cross-platform)

1. **Wait for merge** — poll `gh pr view <prNumber> --json state,mergedAt` every 60s until `state=MERGED`. Print poll updates. If the operator wants to abort polling and resume later, the state file already has the PR number — `/Release resume <version>` continues from here.
2. After merge: `git checkout main && git pull origin main`. Resolve the intended release commit from the merged release PR, require local `main` to point to it, and store it as `state.releaseCommit` before any release build dispatch:
   ```bash
   releaseCommit=$(gh pr view <prNumber> --json mergeCommit --jq .mergeCommit.oid)
   test -n "$releaseCommit"
   test "$(git rev-parse HEAD)" = "$releaseCommit" || { echo "main advanced beyond the release PR merge; stop and investigate" >&2; exit 1; }
   ```
3. **NativeShims ordering gate** — two paths depending on `state.nativeShimsBuildRef`:
   - **If `nativeShimsBuildRef == "main"` (deferred build)**:
     - Dispatch now: `gh workflow run build-nativeshims.yml --ref main -f version=<nsVersion> -f push-to-dev=false`
     - Capture run ID, update state
     - **Poll in background** (120s interval, ~15–25 min build):
       ```bash
       while true; do
         status=$(gh run view <runId> --json status,conclusion -q '[.status,.conclusion] | join(",")')
         if [[ "$status" == completed,* ]]; then echo "$status"; break; fi
         sleep 120
       done
       ```
      - On failure: STOP. On success: jump to Phase 5 NativeShims half; sign+publish, verify NuGet 200, then return here for step 4.
   - **If `nativeShimsBuildRef == "develop"` (already built in Phase 2)**:
     - Check if NativeShims signed+published: poll `https://www.nuget.org/packages/Yubico.NativeShims/<nsVersion>` — must return 200
     - If NOT published: jump to Phase 5 NativeShims half, then return here
   - **If `nativeShimsRebuild == false`**: skip, proceed to step 4.
4. Dispatch main build: `gh workflow run build.yml --ref main -f version=<version> -f push-to-docs=true` — capture run ID to state as `buildRunId`. Immediately fetch `gh run view <buildRunId> --json headSha --jq .headSha` and require it to equal `state.releaseCommit`; otherwise cancel/ignore that run and STOP. The `-f push-to-docs=true` triggers the docs upload job which produces the Docker image tag needed for Phase 6.
5. **Poll in background** (~7–10 min build). Same pattern as NativeShims:
   ```bash
   while true; do
     status=$(gh run view <buildRunId> --json status,conclusion -q '[.status,.conclusion] | join(",")')
     if [[ "$status" == completed,* ]]; then echo "$status"; break; fi
     sleep 60
   done
   ```
   Print: "Main build dispatched (run ID: `<id>`). Polling in background every 60s."
   On failure: STOP, print `gh run view <buildRunId> --log-failed`.
6. On success:
    - Reload `releaseCommit` from `state.releaseCommit`; do not derive it again from the current branch tip.
    - **Extract docs image tag** from the build run. The `deploy-docs.yml` `sed` command appends the tag after `/yesdk/yesdk-docserver:`, so `state.docsImageTag` must store ONLY the short tag (the commit SHA), NOT the full image URI. Use the already verified release commit:
      ```bash
      docsImageTag="$releaseCommit"
      ```
      Store in `state.docsImageTag`.
    - **Tag the release**:
      - **Branch sanity check**: `git branch --show-current` must equal `main`; `git rev-parse HEAD` must equal `state.releaseCommit`
      - `git tag -a <version> "$releaseCommit" -m "Release <version>"`
      - **Verify local tag target**: `test "$(git rev-list -n 1 <version>)" = "$releaseCommit"`
      - `git push origin <version>`
      - **Verify remote tag target**: `test "$(git ls-remote origin 'refs/tags/<version>^{}' | cut -f1)" = "$releaseCommit"`
      - Update state: `tagPushed: true`

## Phase 5 — Sign + publish (cross-platform)

Phase 5 accepts macOS, Windows, and Linux. It has two entry paths; derive the active path from state:

- **Entry path A — NativeShims-only** (from Phase 4 step 3 ordering check; `state.buildRunId == null` and `state.tagPushed == false`): main `build.yml` has NOT been dispatched. Sign and publish NativeShims, unplug the signing YubiKey, then return to Phase 4 step 4.
- **Entry path B — full release** (`state.buildRunId != null` and `state.tagPushed == true`): require a nonempty `state.releaseCommit`; the main build and pushed tag must both resolve to it. Sign and publish the main packages, then continue to Phase 6.

On resume, if `state.nativeShimsPublished == true` while the main build has not yet been dispatched, do **not** repeat the NativeShims half. Return directly to Phase 4 step 4.

**5a. Pre-flight asserts** (each is a hard gate unless marked best-effort):
- Operating system is macOS, Windows, or Linux.
- `gh auth status` succeeds with repository and workflow access.
- `go version` is at least the version required by the module's `go`/`toolchain` directive. Stop with upgrade instructions if it is older.
- Bash/zsh report verification requires `jq`; `command -v jq` must succeed. The `gh --jq` option does not provide a standalone `jq` executable.
- PowerShell artifact downloads require PowerShell 7.4 or newer because native-command `>` redirection is byte-preserving there. Check `$PSVersionTable.PSVersion`; older PowerShell must stop or use the bash/zsh path. The installed `gh api` has no `--output` flag.
- On Linux, the PC/SC runtime and development package needed to build PIV support is installed (for example, the distribution's `pcsc-lite` runtime and development package).
- Build the signer and independent verifier exactly once from their nested modules using `go -C`; invoke the staging binaries thereafter. Do not use root-level `go run` or `go build` for these modules.
- YubiKey presence is best-effort: run `ykman list`. If `ykman` is unavailable or no device is listed, prompt the operator to confirm that the production signing YubiKey is plugged in before continuing.
- Resolve `RELEASE_SIGN_KEY` from the session environment or ask for the key location (for example, `yubikey://9c?serial=...`). Do not write it to state or disk.
- Require `RELEASE_SIGN_CERTIFICATE`, `RELEASE_SIGN_ROOT`, and `RELEASE_SIGN_TIMESTAMP_ROOT`; each must name an existing PEM file. Do not persist their values in release state.
- Never persist or echo secrets or a PIN. Prefer the tool's interactive PIN prompt. Set `NUGET_SIGN_PIN` only in the current process environment when signing must be noninteractive, then unset it immediately after signing.
- Require `NUGET_API_KEY` in the current session before publishing. Prompt without echo if absent; never print it, pass it on a command line shown in logs with its value expanded, or save it in the state file.

**5b. Staging and tools**:

**bash / zsh**:
```bash
set -euo pipefail
staging="$HOME/Releases/<version>"
mkdir -p "$staging/nativeshims" "$staging/core"
signer="$staging/release-sign"
rm -f "$signer"
go -C build/release-sign build -o "$signer" .
verifier="$staging/release-sign-relic-verify"
rm -f "$verifier"
go -C build/release-sign/relic-verify build -o "$verifier" ./cmd/release-sign-relic-verify
hash_file() {
  if command -v sha256sum >/dev/null 2>&1; then sha256sum "$1" | cut -d ' ' -f1
  elif command -v shasum >/dev/null 2>&1; then shasum -a 256 "$1" | cut -d ' ' -f1
  else echo "no SHA-256 tool found" >&2; return 1
  fi
}
```

**PowerShell**:
```powershell
$staging = Join-Path $HOME "Releases\<version>"
New-Item -ItemType Directory -Force -Path "$staging\nativeshims","$staging\core" | Out-Null
$signer = Join-Path $staging 'release-sign.exe'
Remove-Item -Force -ErrorAction SilentlyContinue $signer
go -C build/release-sign build -o $signer .
if ($LASTEXITCODE -ne 0) { throw "release-sign build failed" }
$verifier = Join-Path $staging 'release-sign-relic-verify.exe'
Remove-Item -Force -ErrorAction SilentlyContinue $verifier
go -C build/release-sign/relic-verify build -o $verifier ./cmd/release-sign-relic-verify
if ($LASTEXITCODE -ne 0) { throw "release-sign-relic-verify build failed" }
```

**5c. Status board** — initialize and print after each step:
```
Release <version> — Sign & Publish

[ ] NativeShims build.yml         (run <id>)
[ ] NativeShims download
[ ] NativeShims signed
[ ] NativeShims published to NuGet
[ ] Main build.yml                (run <id>)
[ ] Main download
[ ] Main signed
[ ] Main published to NuGet
```
(Skip NativeShims rows if `nativeShimsRebuild: false`.)

**Publishing retry rule**: normal pushes never use `--skip-duplicate`; an unexpectedly existing version is a hard stop. Use `--skip-duplicate` only for a partial retry recorded in `state.publishRetry` after downloading the remote package and proving its SHA-256 hash equals the corresponding signed staged package. Record the package ID, version, and verified hash before retrying. If exact remote equivalence cannot be established, stop for manual recovery.

**5d. NativeShims half** (only if `nativeShimsRebuild: true` and `nativeShimsPublished: false`):
1. **Download artifact as zip** — use the GitHub API to download the NativeShims nupkg directly as a zip file (no extraction + re-zip):

   **bash / zsh**:
   ```bash
   native_source_digest=$(gh run view <nativeShimsRunId> --json headSha --jq .headSha)
   test -n "$native_source_digest"
   if [ "<nativeShimsBuildRef>" = "main" ]; then
     test "$native_source_digest" = "<state.releaseCommit>" || { echo "NativeShims main build does not match releaseCommit" >&2; exit 1; }
   fi
   ns_artifact_id=$(gh api "repos/Yubico/Yubico.NET.SDK/actions/runs/<nativeShimsRunId>/artifacts" \
     --jq '[.artifacts[] | select(.name | test("NativeShims"))][0].id')
   out_file="$staging/nativeshims/NativeShims-Package.zip"
   rm -f "$out_file"
   gh api "repos/Yubico/Yubico.NET.SDK/actions/artifacts/$ns_artifact_id/zip" > "$out_file"
   test -s "$out_file"
   ```

   **PowerShell**:
   ```powershell
   $nativeSourceDigest = gh run view $state.nativeShimsRunId --json headSha --jq .headSha
   if ([string]::IsNullOrWhiteSpace($nativeSourceDigest)) { throw "NativeShims headSha is missing" }
   if ($state.nativeShimsBuildRef -eq 'main' -and $nativeSourceDigest -ne $state.releaseCommit) {
     throw "NativeShims main build does not match releaseCommit"
   }
   $artifacts = gh api "repos/Yubico/Yubico.NET.SDK/actions/runs/$($state.nativeShimsRunId)/artifacts" | ConvertFrom-Json
   $nsArtifact = $artifacts.artifacts | Where-Object { $_.name -match 'NativeShims' } | Select-Object -First 1
   $outFile = "$staging\nativeshims\NativeShims-Package.zip"
   Remove-Item -Force -ErrorAction SilentlyContinue $outFile
   # PowerShell 7.4+ preserves bytes from native-command stdout.
   gh api "repos/Yubico/Yubico.NET.SDK/actions/artifacts/$($nsArtifact.id)/zip" > $outFile
   if (-not (Test-Path $outFile) -or (Get-Item $outFile).Length -eq 0) { throw "NativeShims artifact download failed" }
   ```
2. Keep the exact filename `NativeShims-Package.zip`.
3. Sign from the repository root:

   **bash / zsh**:
   ```bash
   "$signer" run \
     --component nativeshims \
     --working-directory "$staging/nativeshims" \
     --artifact "$staging/nativeshims/NativeShims-Package.zip" \
     --manifest build/release-sign/manifests/nativeshims.json \
     --source-digest "$native_source_digest" \
     --key "$RELEASE_SIGN_KEY" \
     --certificate "$RELEASE_SIGN_CERTIFICATE" \
     --root "$RELEASE_SIGN_ROOT" \
     --timestamp-root "$RELEASE_SIGN_TIMESTAMP_ROOT" \
     --independent-verifier "$verifier" || exit 1
   ```

   **PowerShell**:
   ```powershell
   & $signer run `
     --component nativeshims `
     --working-directory "$staging\nativeshims" `
     --artifact "$staging\nativeshims\NativeShims-Package.zip" `
     --manifest build/release-sign/manifests/nativeshims.json `
     --source-digest $nativeSourceDigest `
     --key $env:RELEASE_SIGN_KEY `
     --certificate $env:RELEASE_SIGN_CERTIFICATE `
     --root $env:RELEASE_SIGN_ROOT `
     --timestamp-root $env:RELEASE_SIGN_TIMESTAMP_ROOT `
     --independent-verifier $verifier
   if ($LASTEXITCODE -ne 0) { throw "NativeShims signing failed" }
   ```
   Let the tool prompt for the PIN. The signing tool verifies build attestations with `gh` and verifies Authenticode signatures with the independent verifier; do not replace either check with an in-process-only check. Do not add `--clean` on the happy path. Use it only for deliberate recovery after inspecting and removing or approving stale signed output.
4. Verify the report and package before publish. The report must bind the NativeShims workflow and source digest, mark every attestation verified, contain exactly one package at `state.nativeShimsVersion`, and match the output file's SHA-256 hash.

   **bash / zsh**:
   ```bash
   native_report="$staging/nativeshims/signed/report.json"
   test -f "$native_report"
   jq -e --arg digest "$native_source_digest" --arg version "<state.nativeShimsVersion>" '
     .sourceDigest == $digest and
     .signerWorkflow == "Yubico/Yubico.NET.SDK/.github/workflows/build-nativeshims.yml" and
     (.packages | length) == 1 and
     all(.packages[]; .attestationVerified == true and .version == $version)
   ' "$native_report" >/dev/null
   native_packages=("$staging"/nativeshims/signed/packages/*.nupkg)
   test "${#native_packages[@]}" -eq 1 && test -f "${native_packages[0]}"
   while IFS=$'\t' read -r filename expected; do
     output="$staging/nativeshims/signed/packages/$filename"
     test -f "$output"
     actual=$(hash_file "$output")
     expected=$(printf '%s' "$expected" | tr '[:upper:]' '[:lower:]')
     test "$actual" = "$expected"
   done < <(jq -r '.packages[] | [.filename, .outputSha256] | @tsv' "$native_report")
   ```

   **PowerShell**:
   ```powershell
   $nativeReportPath = "$staging\nativeshims\signed\report.json"
   if (-not (Test-Path $nativeReportPath)) { throw "NativeShims report missing" }
   $nativeReport = Get-Content -Raw $nativeReportPath | ConvertFrom-Json
   $nativePackages = @($nativeReport.packages)
   if ($nativeReport.sourceDigest -ne $nativeSourceDigest -or
       $nativeReport.signerWorkflow -ne 'Yubico/Yubico.NET.SDK/.github/workflows/build-nativeshims.yml' -or
       $nativePackages.Count -ne 1 -or
       @($nativePackages | Where-Object { -not $_.attestationVerified -or $_.version -ne $state.nativeShimsVersion }).Count -ne 0) {
     throw "NativeShims report validation failed"
   }
   $nativeOutputs = @(Get-ChildItem "$staging\nativeshims\signed\packages\*.nupkg")
   if ($nativeOutputs.Count -ne 1) { throw "Expected exactly one signed NativeShims package" }
   foreach ($package in $nativePackages) {
     $output = Join-Path "$staging\nativeshims\signed\packages" $package.filename
     if (-not (Test-Path $output) -or (Get-FileHash -Algorithm SHA256 $output).Hash -ine $package.outputSha256) {
       throw "NativeShims output hash mismatch: $($package.filename)"
     }
   }
   ```
5. Unplug the production signing YubiKey before publishing and before returning to Phase 4, where another build workflow will run.
6. Publish the one nupkg with the applicable exact command form:

   **bash / zsh**:
   ```bash
   for package in "$staging"/nativeshims/signed/packages/*.nupkg; do
     dotnet nuget push "$package" --api-key "$NUGET_API_KEY" --source https://api.nuget.org/v3/index.json || exit 1
   done
   ```

   **PowerShell**:
   ```powershell
   Get-ChildItem "$staging\nativeshims\signed\packages\*.nupkg" | ForEach-Object {
     dotnet nuget push $_.FullName --api-key $env:NUGET_API_KEY --source https://api.nuget.org/v3/index.json
     if ($LASTEXITCODE -ne 0) { throw "NativeShims NuGet push failed: $($_.FullName)" }
   }
   ```
7. **Verify live**: poll `https://www.nuget.org/packages/Yubico.NativeShims/<nsVersion>` via `WebFetch` until HTTP 200 (NuGet indexing latency: 1–5 min). Update the status board and set `state.nativeShimsPublished: true`.
8. **Loop back to Phase 4 step 4** to dispatch `build.yml` if not yet done. Re-entering Phase 5 for the main half requires a fresh prompt to plug in the signing YubiKey.

**5e. Main half**:
1. **Download artifacts as zips** — use the GitHub API to download directly as zip files:

   **bash / zsh**:
   ```bash
   main_source_digest=$(gh run view <buildRunId> --json headSha --jq .headSha)
   test -n "$main_source_digest"
   test "$main_source_digest" = "<state.releaseCommit>" || { echo "main build does not match releaseCommit" >&2; exit 1; }
   rm -f "$staging/core/Nuget-Packages.zip" "$staging/core/Symbols-Packages.zip"
   gh api "repos/Yubico/Yubico.NET.SDK/actions/runs/<buildRunId>/artifacts" \
     --jq '.artifacts[] | [.name, (.id | tostring)] | @tsv' |
   while IFS=$'\t' read -r name id; do
     case "$name" in
       "Nuget Packages") out_name="Nuget-Packages.zip" ;;
       "Symbols Packages") out_name="Symbols-Packages.zip" ;;
       *) continue ;;
     esac
     gh api "repos/Yubico/Yubico.NET.SDK/actions/artifacts/$id/zip" > "$staging/core/$out_name"
   done
   test -s "$staging/core/Nuget-Packages.zip"
   test -s "$staging/core/Symbols-Packages.zip"
   ```

   **PowerShell**:
   ```powershell
   $mainSourceDigest = gh run view $state.buildRunId --json headSha --jq .headSha
   if ([string]::IsNullOrWhiteSpace($mainSourceDigest)) { throw "Main build headSha is missing" }
   if ($mainSourceDigest -ne $state.releaseCommit) { throw "Main build does not match releaseCommit" }
   Remove-Item -Force -ErrorAction SilentlyContinue "$staging\core\Nuget-Packages.zip","$staging\core\Symbols-Packages.zip"
   $artifacts = gh api "repos/Yubico/Yubico.NET.SDK/actions/runs/$($state.buildRunId)/artifacts" | ConvertFrom-Json
   foreach ($a in $artifacts.artifacts) {
     switch -Regex ($a.name) {
       'Nuget Packages'   { $outName = "Nuget-Packages.zip" }
       'Symbols Packages' { $outName = "Symbols-Packages.zip" }
       default { continue }
     }
     # PowerShell 7.4+ preserves bytes from native-command stdout.
     gh api "repos/Yubico/Yubico.NET.SDK/actions/artifacts/$($a.id)/zip" > "$staging\core\$outName"
   }
   ```
   Verify both zips exist and are non-empty.
2. Sign:

   **bash / zsh**:
   ```bash
   "$signer" run \
     --component core \
     --working-directory "$staging/core" \
     --artifact "$staging/core/Nuget-Packages.zip" \
     --artifact "$staging/core/Symbols-Packages.zip" \
     --manifest build/release-sign/manifests/core.json \
     --source-digest "$main_source_digest" \
     --key "$RELEASE_SIGN_KEY" \
     --certificate "$RELEASE_SIGN_CERTIFICATE" \
     --root "$RELEASE_SIGN_ROOT" \
     --timestamp-root "$RELEASE_SIGN_TIMESTAMP_ROOT" \
     --independent-verifier "$verifier" || exit 1
   ```

   **PowerShell**:
   ```powershell
   & $signer run `
     --component core `
     --working-directory "$staging\core" `
     --artifact "$staging\core\Nuget-Packages.zip" `
     --artifact "$staging\core\Symbols-Packages.zip" `
     --manifest build/release-sign/manifests/core.json `
     --source-digest $mainSourceDigest `
     --key $env:RELEASE_SIGN_KEY `
     --certificate $env:RELEASE_SIGN_CERTIFICATE `
     --root $env:RELEASE_SIGN_ROOT `
     --timestamp-root $env:RELEASE_SIGN_TIMESTAMP_ROOT `
     --independent-verifier $verifier
   if ($LASTEXITCODE -ne 0) { throw "Core signing failed" }
   ```
   The signing tool verifies build attestations with `gh` and verifies Authenticode signatures with the independent verifier. Do not add `--clean` on the happy path; use it only for deliberate recovery after inspecting and removing or approving stale signed output.
3. Verify the report and all packages before publish. The report must bind the main workflow and `state.releaseCommit`, mark every attestation verified, contain four packages at `state.version`, and match every output file's SHA-256 hash.

   **bash / zsh**:
   ```bash
   core_report="$staging/core/signed/report.json"
   test -f "$core_report"
   jq -e --arg digest "$main_source_digest" --arg version "<state.version>" '
     .sourceDigest == $digest and
     .signerWorkflow == "Yubico/Yubico.NET.SDK/.github/workflows/build.yml" and
     (.packages | length) == 4 and
     all(.packages[]; .attestationVerified == true and .version == $version)
   ' "$core_report" >/dev/null
   core_nupkgs=("$staging"/core/signed/packages/*.nupkg)
   core_snupkgs=("$staging"/core/signed/packages/*.snupkg)
   test "${#core_nupkgs[@]}" -eq 2 && test -f "${core_nupkgs[0]}" && test -f "${core_nupkgs[1]}"
   test "${#core_snupkgs[@]}" -eq 2 && test -f "${core_snupkgs[0]}" && test -f "${core_snupkgs[1]}"
   while IFS=$'\t' read -r filename expected; do
     output="$staging/core/signed/packages/$filename"
     test -f "$output"
     actual=$(hash_file "$output")
     expected=$(printf '%s' "$expected" | tr '[:upper:]' '[:lower:]')
     test "$actual" = "$expected"
   done < <(jq -r '.packages[] | [.filename, .outputSha256] | @tsv' "$core_report")
   ```

   **PowerShell**:
   ```powershell
   $coreReportPath = "$staging\core\signed\report.json"
   if (-not (Test-Path $coreReportPath)) { throw "Core report missing" }
   $coreReport = Get-Content -Raw $coreReportPath | ConvertFrom-Json
   $corePackages = @($coreReport.packages)
   if ($coreReport.sourceDigest -ne $state.releaseCommit -or
       $coreReport.signerWorkflow -ne 'Yubico/Yubico.NET.SDK/.github/workflows/build.yml' -or
       $corePackages.Count -ne 4 -or
       @($corePackages | Where-Object { -not $_.attestationVerified -or $_.version -ne $state.version }).Count -ne 0) {
     throw "Core report validation failed"
   }
   if (@(Get-ChildItem "$staging\core\signed\packages\*.nupkg").Count -ne 2 -or
       @(Get-ChildItem "$staging\core\signed\packages\*.snupkg").Count -ne 2) {
     throw "Expected two nupkg and two snupkg outputs"
   }
   foreach ($package in $corePackages) {
     $output = Join-Path "$staging\core\signed\packages" $package.filename
     if (-not (Test-Path $output) -or (Get-FileHash -Algorithm SHA256 $output).Hash -ine $package.outputSha256) {
       throw "Core output hash mismatch: $($package.filename)"
     }
   }
   ```
4. Unplug the production signing YubiKey now, before Phase 6 or any tests or other workflows can run.
5. **Publish nupkgs first** (snupkgs must wait for NuGet indexing):

   **bash / zsh**:
   ```bash
   for package in "$staging"/core/signed/packages/*.nupkg; do
     dotnet nuget push "$package" --api-key "$NUGET_API_KEY" --source https://api.nuget.org/v3/index.json || exit 1
   done
   ```

   **PowerShell**:
   ```powershell
   Get-ChildItem "$staging\core\signed\packages\*.nupkg" | ForEach-Object {
     dotnet nuget push $_.FullName --api-key $env:NUGET_API_KEY --source https://api.nuget.org/v3/index.json
     if ($LASTEXITCODE -ne 0) { throw "NuGet package push failed: $($_.FullName)" }
   }
   ```
6. **Verify nupkgs live**: poll `https://www.nuget.org/packages/Yubico.YubiKey/<version>` AND `https://www.nuget.org/packages/Yubico.Core/<version>` via `WebFetch` until both return HTTP 200 (indexing latency: 1–10 min).
7. **Publish snupkgs only after both nupkgs are indexed**:

   **bash / zsh**:
   ```bash
   for package in "$staging"/core/signed/packages/*.snupkg; do
     dotnet nuget push "$package" --api-key "$NUGET_API_KEY" --source https://api.nuget.org/v3/index.json || exit 1
   done
   ```

   **PowerShell**:
   ```powershell
   Get-ChildItem "$staging\core\signed\packages\*.snupkg" | ForEach-Object {
     dotnet nuget push $_.FullName --api-key $env:NUGET_API_KEY --source https://api.nuget.org/v3/index.json
     if ($LASTEXITCODE -ne 0) { throw "Symbol package push failed: $($_.FullName)" }
   }
   ```
8. **Verify snupkgs live**: `WebFetch` the NuGet package pages for Yubico.YubiKey and Yubico.Core, confirm "Download symbols" appears (snupkg indexing can take up to 10 min).
9. Update final status board rows. Unset `NUGET_SIGN_PIN` if it was set and retain no PIN or API key outside the current session.

**5f. One-release Windows fallback**:

The old `build/sign-v2.ps1` flow is available on Windows for exactly one transition release. It is not automatic: explain why the cross-platform flow cannot proceed and require an explicit `AskUserQuestion` choice between "Stop and fix release-sign" and "Use the one-release Windows sign-v2 fallback". Record the operator's choice in the run transcript, not release state. The fallback requires the Sign CLI and `YUBICO_SIGNING_SHA256_FINGERPRINT`, and uses `Invoke-NuGetPackageSigningV2` with the same working directories and exact ZIP names (`NativeShims-Package.zip`, `Nuget-Packages.zip`, and `Symbols-Packages.zip`). The signed package-count, YubiKey unplug, normal no-skip `dotnet nuget push`, indexing, ordering, and draft-asset gates above still apply. `report.json` is required for the primary release-sign flow but is not produced by this fallback, so inspect every fallback package's NuGet metadata and require its version to equal `state.nativeShimsVersion` or `state.version` before publishing. `build/sign.ps1` is legacy and must not be selected as the primary or fallback path.

## Phase 6 — GitHub release (cross-platform)

1. **Prepare release body** — extract the new `### <version>` section from `whats-new.md` to a temp file. Reuse the temp file from Phase 3 step 9 if `state.notesFile` exists; otherwise regenerate. Then transform headers from plain text (`Bug Fixes:`) to bold markdown (`**Bug Fixes**:`) for GitHub rendering, and append the full changelog link:

   **bash / zsh**:
   ```bash
   notes_file="${state_notes_file:-$(mktemp -t release-notes-<version>.XXXXXX.md)}"
   if [ ! -s "$notes_file" ]; then
     awk -v v="### <version>" '
       $0 == v {flag=1; print; next}
       flag && /^### / {exit}
       flag {print}
     ' docs/users-manual/getting-started/whats-new.md > "$notes_file"
   fi
   # Transform plain headers to bold and append changelog (use sed -i'' for macOS compat)
   sed -i'' -e 's/^\([A-Z][A-Za-z /]*\):$/**\1**:/' "$notes_file"
   echo "" >> "$notes_file"
   echo "**Full Changelog**: https://github.com/Yubico/Yubico.NET.SDK/compare/<previousTag>...<version>" >> "$notes_file"
   ```

   **PowerShell**:
   ```powershell
   $notesFile = if ($state.notesFile -and (Test-Path $state.notesFile)) { $state.notesFile } else { New-TemporaryFile }
   if ((Get-Item $notesFile).Length -eq 0) {
     $whatsNew = Get-Content docs/users-manual/getting-started/whats-new.md
     $start = ($whatsNew | Select-String -Pattern "^### <version>$" | Select-Object -First 1).LineNumber
     $end = ($whatsNew[$start..($whatsNew.Length - 1)] | Select-String -Pattern "^### " | Select-Object -First 1).LineNumber
     $section = if ($end) { $whatsNew[($start - 1)..($start + $end - 2)] } else { $whatsNew[($start - 1)..($whatsNew.Length - 1)] }
     $section | Set-Content $notesFile
   }
   # Transform plain headers to bold and append changelog
   (Get-Content $notesFile) -replace '^([A-Z][A-Za-z /]*):$', '**$1**:' | Set-Content $notesFile
   Add-Content $notesFile "`n**Full Changelog**: https://github.com/Yubico/Yubico.NET.SDK/compare/<previousTag>...<version>"
   ```

   Do NOT use `--generate-notes` — it adds redundant auto-generated content on top of the curated notes.

2. **Create draft release WITH signed assets in one command** — assets attached to a draft are immutable after publish, so they MUST be attached before the draft is finalized. Include NativeShims only when `state.nativeShimsRebuild` is true.

   **bash / zsh**:
   ```bash
   assets=()
   if [ "<state.nativeShimsRebuild>" = "true" ]; then
     assets+=("$HOME"/Releases/<version>/nativeshims/signed/packages/*.nupkg)
   fi
   assets+=("$HOME"/Releases/<version>/core/signed/packages/*.nupkg)
   assets+=("$HOME"/Releases/<version>/core/signed/packages/*.snupkg)
   gh release create <version> \
     --draft \
     --title "<version>" \
     --notes-file "$notes_file" \
     "${assets[@]}"
   ```

   **PowerShell**:
   ```powershell
   $assets = @()
   if ($state.nativeShimsRebuild) {
     $assets += @(Get-ChildItem "$staging\nativeshims\signed\packages\*.nupkg").FullName
   }
   $assets += @(Get-ChildItem "$staging\core\signed\packages\*.nupkg").FullName
   $assets += @(Get-ChildItem "$staging\core\signed\packages\*.snupkg").FullName
   gh release create $state.version `
     --draft `
     --title $state.version `
     --notes-file $notesFile `
     @assets
   if ($LASTEXITCODE -ne 0) { throw "Draft GitHub release creation failed" }
   ```

   **Verify draft**: `gh release view <version> --json assets -q '.assets[].name'` — assert all expected files are listed.

3. `AskUserQuestion`: "Draft release ready at <URL>. Publish now?" Options: "Publish" / "Leave as draft" / "Open in browser first"

4. If "Publish":
   - `gh release edit <version> --draft=false`
   - **Verify published**: `gh release view <version> --json isDraft -q .isDraft` must return `false`

5. **Trigger docs deploy**: `gh workflow run deploy-docs.yml --ref main -f gitops-branch=prod -f image-tag=<docsImageTag>` — uses the image tag stored in `state.docsImageTag` from Phase 4 step 6. The `push-to-docs=true` flag in Phase 4's build.yml dispatch triggers the Upload docs job which builds and pushes the Docker image.

## Phase 7 — Closing (cross-platform)

1. **Merge main back to develop** (gitflow):
   ```bash
   git checkout develop && git pull
   git merge main --no-ff -m "Merge main back into develop after <version> release"
   git push origin develop
   ```
2. **Assert** `build/Versions.props:43` is still `<CommonVersion>0.0.0-dev</CommonVersion>` — print warning if drifted (we never edit it; if drifted, something else changed it)
3. **Generate Slack #ask-tla announcement** — print as fenced code block ready to copy. Use cached `categorizedPRs` from state. Exact format:

```
NET SDK <version> Release Announcement! 🎉🚀
Release: <Month Dth, YYYY> 📅
Distribution: 📦
NuGet:
- https://www.nuget.org/packages/Yubico.YubiKey/<version> 🔑
- https://www.nuget.org/packages/Yubico.Core/<version> 🧩
GitHub: https://github.com/Yubico/Yubico.NET.SDK/releases/tag/<version> 🐙
Latest release: https://github.com/Yubico/Yubico.NET.SDK/releases/latest ✨
---
<for each non-empty category, in this fixed order: Features → Bug Fixes → Documentation → Dependencies / Maintenance → Security / CI → Miscellaneous:>
<Category Name> <category emoji>
- <PR title> (#<num>)
  https://github.com/Yubico/Yubico.NET.SDK/pull/<num>
---
Full Changelog: <previousTag>...<version> 🧾🔍
https://github.com/Yubico/Yubico.NET.SDK/compare/<previousTag>...<version>
Track the progress: https://nugettrends.com/packages?months=36&ids=Yubico.YubiKey 📈🔥
```

Category emojis (Features → Security/CI match prior 1.15.1 announcement exactly; Miscellaneous added so `misc` PRs are never dropped):
- Features: ✨🎁
- Bug Fixes: 🛠️✅
- Documentation: 📚✍️
- Dependencies / Maintenance: 🔧🧼
- Security / CI: 🔒🤖
- Miscellaneous: 🧰📌

4. **Print closing checklist** (manual — skill cannot automate):
   - [ ] Post drafted message in Slack #ask-tla
   - [ ] Post on GitHub Discussions (link to release)
   - [ ] Close release in Jira
5. Mark `~/Releases/<version>/.state.json` as `currentPhase: 7, complete: true`

## Failure modes & recovery

- **CI build fails** → STOP, do not tag, do not proceed. Re-dispatch after fix.
- **Sign fails** → leave artifacts in staging; do not delete. Inspect, retry. State preserved.
- **NuGet publish 409 (already exists)** → hard stop. Treat it as a version conflict unless this is a state-recorded partial retry and the downloaded remote package is byte-for-byte hash-equivalent to the signed staged package; otherwise require manual recovery and never overwrite.
- **Tag push fails** (e.g., already exists) → STOP, investigate. Never force-push tags.
- **Resume on different machine** → `/Release resume <version>` reads `~/Releases/<version>/.state.json` and skips completed phases. Phase boundaries are the resume points.
