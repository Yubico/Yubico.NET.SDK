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
  "buildRunId": null,
  "tagPushed": false,
  "currentPhase": 4,
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
   - `gh workflow run build-nativeshims.yml --ref develop -f version=<nsVersion>` — capture run ID
   - Poll: `gh run list --workflow=build-nativeshims.yml --limit 1 --json databaseId,status,conclusion` until status=`completed`. Print poll progress every 30s
   - On `failure`: STOP, print logs URL, do not proceed
   - On `success`: update state with `nativeShimsRebuild: true`, `nativeShimsVersion`, `nativeShimsRunId`
   - **HARD GATE**: NativeShims MUST be signed (phase 5 wizard) AND published to NuGet.org BEFORE phase 4 dispatches `build.yml`. The skill enforces this by deferring `build.yml` dispatch in phase 4 until phase 5's NativeShims half completes — see phase 4 ordering note.

## Phase 3 — Release branch (cross-platform)

1. `git checkout develop && git pull origin develop`
2. `git checkout -b release/<version>` (per gitflow + project CLAUDE.md)
3. **NativeShims lockfile repin** (only if `nativeShimsRebuild: true` in state and NativeShims published as stable):
   - Per project CLAUDE.md (NuGet floating version + lockfile pattern): `Yubico.Core/src/Yubico.Core.csproj` uses `Version="1.*-*"` and resolves via `Yubico.Core/src/packages.lock.json`
   - `enforce-branch-policy` job in `.github/workflows/build.yml` HARD-FAILS on main if lockfile pins a `-prerelease` Yubico.NativeShims
   - Repin against nuget.org-only environment (so the local internal feed doesn't shadow the just-published stable):
     ```bash
     dotnet restore Yubico.Core/src/Yubico.Core.csproj --force-evaluate --source https://api.nuget.org/v3/index.json
     ```
   - `git diff Yubico.Core/src/packages.lock.json` — confirm one-line change to stable Yubico.NativeShims `<nsVersion>`
   - DO NOT edit `Yubico.Core.csproj` itself
   - Commit: `git add Yubico.Core/src/packages.lock.json && git commit -m "build: repin NativeShims to <nsVersion> stable"`
4. **Generate release notes draft**:
   - Get last release date: `gh release view <previousTag> --json publishedAt -q .publishedAt`
   - List merged PRs since: `gh pr list --base develop --state merged --search "merged:>=<lastReleaseISO>" --json number,title,labels,url --limit 100`
   - Categorize by PR title prefix and labels (heuristics):
     - `feat:` / `feature/` / label `enhancement` → **Features** (`features` bucket)
     - `fix:` / `bugfix/` / label `bug` → **Bug Fixes** (`bugfixes` bucket)
     - `docs:` / `doc:` → **Documentation** (`docs` bucket)
     - `chore(deps):` / `build(deps):` / dependabot → **Dependencies / Maintenance** (`deps` bucket)
     - `security:` / `ci:` / `.github/workflows/` touched → **Security / CI** (`security` bucket)
     - everything else → **Miscellaneous** (`misc` bucket — never dropped)
   - Cache full categorization (all 6 buckets including `misc`) into `state.categorizedPRs` for phase 7 Slack reuse
5. **Insert into `docs/users-manual/getting-started/whats-new.md`**:
   - Read current file
   - Insert new `### <version>` block under the appropriate `## 1.16.x Releases` heading (create the heading if needed)
   - Match the existing format exactly (Release date, Features, Bug Fixes, Documentation, Misc, Dependencies subsections — emit Misc subsection whenever `misc` bucket is non-empty)
   - Show diff via `git diff docs/users-manual/getting-started/whats-new.md`
6. `AskUserQuestion`: "Release notes look correct?" Options: "Yes, commit" / "Let me edit first" / "Regenerate from PRs"
7. On approval: `git add docs/users-manual/getting-started/whats-new.md && git commit -m "docs: release notes for <version>"`
8. `git push -u origin release/<version>`
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
3. **Ordering check** — if `nativeShimsRebuild: true` in state AND NativeShims hasn't been signed+published yet (no NuGet 200 on `https://www.nuget.org/packages/Yubico.NativeShims/<nsVersion>`):
   - Print: "⚠ NativeShims must publish to NuGet.org before main build dispatches"
   - Jump to phase 5 NativeShims half (Windows-only); after that completes and NuGet shows live, return here
4. `gh workflow run build.yml --ref main -f version=<version>` — capture run ID to state as `buildRunId`
5. Poll until `completed`. On failure: STOP, print logs URL.
6. On success: tag the release
   - **Branch sanity check** (per memory `check-branch-before-amend.md`-adjacent caution): `git branch --show-current` must equal `main`; `git log -1 --oneline` should be the merge commit
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
1. `gh run download <nativeShimsRunId> --dir "$staging\nativeshims"` — confirms artifact zip lands
2. Identify the zip name (look for `*nativeshims*.zip`)
3. Sign:
   ```powershell
   . ./build/sign.ps1
   Invoke-NuGetPackageSigning `
     -Thumbprint $env:YUBICO_SIGNING_THUMBPRINT `
     -WorkingDirectory "$staging\nativeshims" `
     -NativeShimsZip "<zipname>"
   ```
   YubiKey PIN prompt will surface; tell the operator to enter it.
4. Verify: `Get-ChildItem "$staging\nativeshims\signed\packages\*.nupkg"` non-empty.
5. Publish (per `build/sign.ps1` — `Invoke-NuGetPackagePush` requires `-PackagePath`, optional `-ApiKey`/`$env:NUGET_API_KEY`, optional `-Source`/`-SkipDuplicate`):
   ```powershell
   Invoke-NuGetPackagePush `
     -PackagePath "$staging\nativeshims\signed\packages" `
     -SkipDuplicate
   ```
   `-SkipDuplicate` is defensive against re-runs; the source defaults to `https://api.nuget.org/v3/index.json`.
6. Verify live: poll `https://www.nuget.org/packages/Yubico.NativeShims/<nsVersion>` until 200 (NuGet indexing latency: 1–5 min). Update status board. Set `state.nativeShimsPublished: true`.
7. **Loop back to phase 4 step 4** to dispatch `build.yml` if not yet done.

**5e. Main half**:
1. `gh run download <buildRunId> --dir "$staging\core"`
2. Identify zips by listing artifacts: `gh run view <buildRunId> --json artifacts -q '.artifacts[].name'` — typically `Nuget Packages` and `Symbols Packages` (confirm exact names per CI run; spaces in artifact names are quoted by `gh`).
3. Sign:
   ```powershell
   Invoke-NuGetPackageSigning `
     -Thumbprint $env:YUBICO_SIGNING_THUMBPRINT `
     -WorkingDirectory "$staging\core" `
     -NuGetPackagesZip "<nuget-zip-name>" `
     -SymbolsPackagesZip "<symbols-zip-name>"
   ```
4. Verify: `Get-ChildItem "$staging\core\signed\packages\*.nupkg","$staging\core\signed\packages\*.snupkg"` non-empty.
5. Publish:
   ```powershell
   Invoke-NuGetPackagePush `
     -PackagePath "$staging\core\signed\packages" `
     -SkipDuplicate
   ```
6. Verify live: poll `https://www.nuget.org/packages/Yubico.YubiKey/<version>` AND `https://www.nuget.org/packages/Yubico.Core/<version>` until both 200.
7. Update final status board rows.

## Phase 6 — GitHub release (cross-platform; can run on Windows continuation or back on dev machine)

1. **Create draft release** — extract the new `### <version>` section from `whats-new.md` to a real temp file (no bash process substitution; works on Windows PowerShell, macOS, and Linux). Reuse the temp file already produced in Phase 3 step 9 if its path is still in `state.notesFile` and the file exists; otherwise regenerate:

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
   gh release create <version> \
     --draft \
     --title "<version>" \
     --notes-file "$notes_file" \
     --generate-notes
   ```

   **PowerShell** (when phase 6 runs on Windows after sign+publish):
   ```powershell
   $notesFile = if ($state.notesFile -and (Test-Path $state.notesFile)) { $state.notesFile } else { New-TemporaryFile }
   if ((Get-Item $notesFile).Length -eq 0) {
     $whatsNew = Get-Content docs/users-manual/getting-started/whats-new.md
     $start = ($whatsNew | Select-String -Pattern "^### <version>$" | Select-Object -First 1).LineNumber
     $end = ($whatsNew[$start..($whatsNew.Length - 1)] | Select-String -Pattern "^### " | Select-Object -First 1).LineNumber
     $section = if ($end) { $whatsNew[($start - 1)..($start + $end - 2)] } else { $whatsNew[($start - 1)..($whatsNew.Length - 1)] }
     $section | Set-Content $notesFile
   }
   gh release create <version> --draft --title "<version>" --notes-file $notesFile --generate-notes
   ```

   `--generate-notes` adds the auto "what's new" + full changelog appendix on top of the section pulled from `whats-new.md`.
2. **Upload signed assets**: for each signed `.nupkg` and `.snupkg` in `~/Releases/<version>/{nativeshims,core}/signed/packages/`:
   ```bash
   gh release upload <version> <file>
   ```
3. **Trigger docs deploy**: `gh workflow run deploy-docs.yml --ref main` — read workflow first to confirm input names; if it has `environment` input, set to `prod`
4. `AskUserQuestion`: "Draft release ready at <URL>. Publish now?" Options: "Publish" / "Leave as draft" / "Open in browser first"
5. If "Publish": `gh release edit <version> --draft=false`

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
