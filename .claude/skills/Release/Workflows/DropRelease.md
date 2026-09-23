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
7. **Code-signing YubiKey safety gate** — `AskUserQuestion`: "⚠️ IMPORTANT: Your code-signing YubiKey must be UNPLUGGED from this machine during phases 1–4. Integration tests that enumerate YubiKeys can run PIV/PGP resets against any connected key. Only plug it back in when Phase 5 (sign+publish) explicitly asks for it. Is the code-signing YubiKey unplugged?" Options: "Yes, it's unplugged" / "Let me unplug it now". If the operator needs to unplug, wait for confirmation before proceeding.
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
      - On failure: STOP. On success: jump to Phase 5 NativeShims half; sign+publish, verify NuGet 200, then return here for step 4.
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

## Phase 5 — Sign + publish (cross-platform)

Follow `build/release-sign/README.md`. Hard gates before signing:

- `gh auth status` succeeds.
- `nuget-sign`, Go, and `dotnet` are available.
- `RELEASE_SIGN_KEY`, `RELEASE_SIGN_CERTIFICATE`, `RELEASE_SIGN_ROOT`, and
  `RELEASE_SIGN_TIMESTAMP_ROOT` identify the production key and certificate files.
- The code-signing YubiKey stays disconnected until the signing commands below;
  build the wrapper and download artifacts first.
- `NUGET_API_KEY` is set for this session and is never printed or persisted.

Build the wrapper once:

```sh
go -C build/release-sign build -o "$HOME/Releases/<version>/release-sign" .
```

Download the exact artifact ZIPs for each recorded workflow run and obtain that
run's `headSha` with `gh run view <run-id> --json headSha --jq .headSha`.
Connect the code-signing YubiKey only after these preparation steps.

The Go signer prompts for the PIV PIN once per invocation after checking
attestations and package identities; it reuses the PIN only for its signing child
processes. Invoke the signer directly when the agent has an attached terminal.
If the agent shell has no terminal input on macOS, invoke each command below as:

```sh
zsh build/release-sign/launch-macos.zsh \
  "$HOME/Releases/<version>/<component>/signing.status" \
  "$HOME/Releases/<version>/release-sign" run <the same flags as below>
```

Ensure the component working directory exists and the status file does not
before launching. The helper opens Terminal; the operator enters the PIN there,
never in chat or an argument. Poll for `signing.status`, require its exit code
to be `0`, then inspect `signed/report.json`. If the status file is missing or
nonzero, stop instead of publishing. On Windows or Linux without an attached
agent terminal, have the operator run the exact signer command in a local
interactive terminal and verify its report before continuing. If NativeShims
was not rebuilt, only the main Core invocation is needed: one PIN prompt for
both managed packages and their symbols. When NativeShims is rebuilt, its
earlier signing run has a separate prompt; never retain the PIN across the
intervening build.

For NativeShims, when rebuilt:

```sh
"$HOME/Releases/<version>/release-sign" run \
  --component nativeshims \
  --working-directory "$HOME/Releases/<version>/nativeshims" \
  --artifact NativeShims-Package.zip \
  --manifest build/release-sign/manifests/nativeshims.json \
  --source-digest <native-workflow-headSha> \
  --key "$RELEASE_SIGN_KEY" \
  --certificate "$RELEASE_SIGN_CERTIFICATE" \
  --root "$RELEASE_SIGN_ROOT" \
  --timestamp-root "$RELEASE_SIGN_TIMESTAMP_ROOT"
```

Publish its signed `.nupkg`, wait for NuGet indexing, set
`state.nativeShimsPublished: true`, then return to Phase 4 step 4 if the main
build has not run yet.

For the main packages:

```sh
"$HOME/Releases/<version>/release-sign" run \
  --component core \
  --working-directory "$HOME/Releases/<version>/core" \
  --artifact Nuget-Packages.zip \
  --artifact Symbols-Packages.zip \
  --manifest build/release-sign/manifests/core.json \
  --source-digest <main-workflow-headSha> \
  --key "$RELEASE_SIGN_KEY" \
  --certificate "$RELEASE_SIGN_CERTIFICATE" \
  --root "$RELEASE_SIGN_ROOT" \
  --timestamp-root "$RELEASE_SIGN_TIMESTAMP_ROOT"
```

Require `signed/report.json` and the expected package count before publication.
Publish `.nupkg` files first with `dotnet nuget push`, stop on any failure, wait
until both ordinary packages are indexed, and only then publish `.snupkg` files.
Never use `--skip-duplicate` during the normal release path. Verify the live
NuGet pages before marking phase 5 complete.

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
