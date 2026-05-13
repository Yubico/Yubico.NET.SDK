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
  "tagPushed": false,
  "docsImageTag": null,
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
7. **Code-signing YubiKey safety gate** — `AskUserQuestion`: "⚠️ IMPORTANT: Your code-signing YubiKey must be UNPLUGGED from this machine during phases 1–4. Integration tests that enumerate YubiKeys can run PIV/PGP resets against any connected key. Only plug it back in when Phase 5 (sign+publish) explicitly asks for it — signtool and nuget-sign read the PIV certificate safely, but no other YubiKey operation should touch the key. Is the code-signing YubiKey unplugged?" Options: "Yes, it's unplugged" / "Let me unplug it now". If the operator needs to unplug, wait for confirmation before proceeding.
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
2. After merge: `git checkout main && git pull origin main`
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
     - On failure: STOP. On success: jump to Phase 5 NativeShims half (Windows-only); sign+publish, verify NuGet 200, then return here for step 4.
   - **If `nativeShimsBuildRef == "develop"` (already built in Phase 2)**:
     - Check if NativeShims signed+published: poll `https://www.nuget.org/packages/Yubico.NativeShims/<nsVersion>` — must return 200
     - If NOT published: jump to Phase 5 NativeShims half, then return here
   - **If `nativeShimsRebuild == false`**: skip, proceed to step 4.
4. Dispatch main build: `gh workflow run build.yml --ref main -f version=<version> -f push-to-docs=true` — capture run ID to state as `buildRunId`. The `-f push-to-docs=true` triggers the docs upload job which produces the Docker image tag needed for Phase 6.
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
   - **Extract docs image tag** from the build run. The `deploy-docs.yml` `sed` command appends the tag after `/yesdk/yesdk-docserver:`, so `state.docsImageTag` must store ONLY the short tag (the commit SHA), NOT the full image URI. Derive from the merge commit on main:
     ```bash
     docsImageTag=$(git rev-parse HEAD)
     ```
     Store in `state.docsImageTag`.
   - **Tag the release**:
     - **Branch sanity check**: `git branch --show-current` must equal `main`; `git log -1 --oneline` should be the merge commit
     - `git tag -a <version> -m "Release <version>"`
     - `git push origin <version>`
     - Update state: `tagPushed: true`

## Phase 5 — Sign + publish (Windows wizard, or hard-stop)

**Platform detection**:
```bash
# In bash:
case "$(uname -s)" in
  MINGW*|MSYS*|CYGWIN*) PLATFORM=windows ;;
  *) PLATFORM=$(uname -s | tr '[:upper:]' '[:lower:]') ;;
esac
```

### If PLATFORM != windows

STOP. Phase 5 can be reached via two entry paths — render the handoff text from actual state, not assumptions:

- **Entry path A — NativeShims-only** (from Phase 4 step 3 ordering check; `state.buildRunId == null` and `state.tagPushed == false`): main `build.yml` has NOT been dispatched yet. The operator must sign+publish NativeShims first, then resume returns flow to Phase 4 step 4.
- **Entry path B — full release** (`state.buildRunId != null` and `state.tagPushed == true`): main build is green and tag is pushed; only sign+publish + GitHub release remain.

Pseudocode for the handoff message (skill builds the strings from state):

```
═══ HANDOFF TO WINDOWS ═══
Release <version> needs sign+publish on Windows with your code-sign YubiKey.

Current state (from ~/Releases/<version>/.state.json):
- Entry path: <"NativeShims-only" if buildRunId == null else "full release">
- NativeShims rebuild: <nativeShimsRebuild>
- NativeShims run: <nativeShimsRunId or "n/a">
- NativeShims published: <nativeShimsPublished>
- Main build run: <buildRunId or "not yet dispatched">
- Tag pushed: <tagPushed>

What's left after sign+publish:
<if buildRunId == null:
   - Resume returns to Phase 4 step 4 (dispatch build.yml against main)
   - Then re-enter Phase 5 entry path B for the main packages
 else:
   - Phase 6 (GitHub release with signed assets)
   - Phase 7 (merge main back to develop, Slack draft)>

On your Windows machine:
1. Plug in your code-sign YubiKey
2. cd <this repo>
3. Invoke: /Release resume <version>

Do not proceed past this point on macOS/Linux.
═══
```
Exit cleanly. Do NOT mark phase 5 complete.

### If PLATFORM == windows

**5a. Pre-flight asserts** (each is a hard gate; on failure print fix instructions and stop):
- `gh auth status` — authenticated with `repo` + `workflow` scope
- `Get-Command signtool.exe` (PowerShell) — resolvable
- `Get-Command nuget.exe` — resolvable
- `$env:YUBICO_SIGNING_THUMBPRINT` — set; if not, AskUserQuestion to provide and persist for session
- YubiKey presence — best-effort: `Get-PnpDevice -Class SmartCard | Where-Object Status -eq 'OK'`. If empty, prompt: "No smart card detected — is YubiKey plugged in?"

**5b. Staging**:
```powershell
$staging = "$HOME\Releases\<version>"
New-Item -ItemType Directory -Force -Path "$staging\nativeshims","$staging\core"
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

**Pre-flight for publish (one-time per session)**: `Invoke-NuGetPackagePush` resolves the API key from `-ApiKey` parameter or falls back to `$env:NUGET_API_KEY`. Before phase 5d/5e push steps, assert:
```powershell
if ([string]::IsNullOrWhiteSpace($env:NUGET_API_KEY)) {
  # AskUserQuestion: paste API key (will be set in $env:NUGET_API_KEY for this session only)
}
```
Never echo the API key. Never persist it to the state file.

**5d. NativeShims half** (only if `nativeShimsRebuild: true`):
1. **Download artifact as zip** — use the GitHub API to download the NativeShims nupkg directly as a zip file (no extraction + re-zip):
   ```powershell
   $artifacts = gh api "repos/Yubico/Yubico.NET.SDK/actions/runs/$($state.nativeShimsRunId)/artifacts" | ConvertFrom-Json
   $nsArtifact = $artifacts.artifacts | Where-Object { $_.name -match 'NativeShims' } | Select-Object -First 1
   $outFile = "$staging\nativeshims\NativeShims-Package.zip"
   gh api "repos/Yubico/Yubico.NET.SDK/actions/artifacts/$($nsArtifact.id)/zip" > $outFile
   ```
   **WARNING**: NEVER name a zip `*.nupkg.zip` — `GetFileNameWithoutExtension` produces a name ending in `.nupkg` which collides with `Get-ChildItem -Filter "*.nupkg"` inside sign.ps1.
2. Verify zip exists and is non-empty: `(Get-Item $outFile).Length -gt 0`
3. Sign:
   ```powershell
   . ./build/sign.ps1
   Invoke-NuGetPackageSigning `
     -Thumbprint $env:YUBICO_SIGNING_THUMBPRINT `
     -WorkingDirectory "$staging\nativeshims" `
     -NativeShimsZip "NativeShims-Package.zip"
   ```
   YubiKey PIN prompt will surface; tell the operator to enter it.
4. Verify: `Get-ChildItem "$staging\nativeshims\signed\packages\*.nupkg"` non-empty.
5. Publish:
   ```powershell
   Get-ChildItem "$staging\nativeshims\signed\packages\*.nupkg" | ForEach-Object {
     Invoke-NuGetPackagePush -PackagePath $_.FullName -SkipDuplicate
   }
   ```
6. **Verify live**: poll `https://www.nuget.org/packages/Yubico.NativeShims/<nsVersion>` via `WebFetch` until HTTP 200 (NuGet indexing latency: 1–5 min). Update status board. Set `state.nativeShimsPublished: true`.
7. **Loop back to Phase 4 step 4** to dispatch `build.yml` if not yet done.

**5e. Main half**:
1. **Download artifacts as zips** — use the GitHub API to download directly as zip files:
   ```powershell
   $artifacts = gh api "repos/Yubico/Yubico.NET.SDK/actions/runs/$($state.buildRunId)/artifacts" | ConvertFrom-Json
   foreach ($a in $artifacts.artifacts) {
     switch -Regex ($a.name) {
       'Nuget Packages'   { $outName = "Nuget-Packages.zip" }
       'Symbols Packages' { $outName = "Symbols-Packages.zip" }
       default { continue }
     }
     gh api "repos/Yubico/Yubico.NET.SDK/actions/artifacts/$($a.id)/zip" > "$staging\core\$outName"
   }
   ```
   Verify both zips exist and are non-empty.
2. Sign:
   ```powershell
   Invoke-NuGetPackageSigning `
     -Thumbprint $env:YUBICO_SIGNING_THUMBPRINT `
     -WorkingDirectory "$staging\core" `
     -NuGetPackagesZip "Nuget-Packages.zip" `
     -SymbolsPackagesZip "Symbols-Packages.zip"
   ```
3. Verify: `Get-ChildItem "$staging\core\signed\packages\*.nupkg","$staging\core\signed\packages\*.snupkg"` non-empty.
4. **Publish nupkgs first** (snupkgs must wait for NuGet indexing):
   ```powershell
   Get-ChildItem "$staging\core\signed\packages\*.nupkg" | ForEach-Object {
     Invoke-NuGetPackagePush -PackagePath $_.FullName -SkipDuplicate
   }
   ```
5. **Verify nupkgs live**: poll `https://www.nuget.org/packages/Yubico.YubiKey/<version>` AND `https://www.nuget.org/packages/Yubico.Core/<version>` via `WebFetch` until both return HTTP 200 (indexing latency: 1–10 min).
6. **Publish snupkgs** (only after nupkgs are indexed):
   ```powershell
   Get-ChildItem "$staging\core\signed\packages\*.snupkg" | ForEach-Object {
     Invoke-NuGetPackagePush -PackagePath $_.FullName -SkipDuplicate
   }
   ```
7. **Verify snupkgs live**: `WebFetch` the NuGet package pages for Yubico.YubiKey and Yubico.Core, confirm "Download symbols" link appears (snupkg indexing can take up to 10 min).
8. Update final status board rows.

## Phase 6 — GitHub release (cross-platform; can run on Windows continuation or back on dev machine)

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

   **PowerShell** (when Phase 6 runs on Windows after sign+publish):
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

2. **Create draft release WITH signed assets in one command** — assets attached to a draft are immutable after publish, so they MUST be attached before the draft is finalized:
   ```bash
   gh release create <version> \
     --draft \
     --title "<version>" \
     --notes-file "$notes_file" \
     ~/Releases/<version>/nativeshims/signed/packages/*.nupkg \
     ~/Releases/<version>/core/signed/packages/*.nupkg \
     ~/Releases/<version>/core/signed/packages/*.snupkg
   ```
   (Include NativeShims nupkg only if `nativeShimsRebuild: true`.)

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
- **NuGet publish 409 (already exists)** → version conflict; abort entire release, never overwrite published packages.
- **Tag push fails** (e.g., already exists) → STOP, investigate. Never force-push tags.
- **Resume on different machine** → `/Release resume <version>` reads `~/Releases/<version>/.state.json` and skips completed phases. Phase boundaries are the resume points.
