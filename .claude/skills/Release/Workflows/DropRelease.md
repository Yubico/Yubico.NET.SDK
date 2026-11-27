# DropRelease Workflow

End-to-end Yubico .NET SDK release wizard. The operator answers AskUserQuestion prompts, approves the Scribe signing jobs and the GitHub `nuget-org` deployment; everything else runs in GitHub Actions. Nothing is downloaded or signed locally.

## How a release runs in CI

`build-nativeshims.yml` and `build.yml` have a release mode (`-f release=true -f version=<v>`). A release run:

1. Fails fast in a preflight if the branch is not allowed, the tag exists or the version is already on NuGet.org (SDK: also if `whats-new.md` has no `### <version>` section).
2. Builds and tests as usual.
3. **Scribe round 1** — Authenticode-signs the Windows binaries (NativeShims DLLs, or the Core and YubiKey assemblies) before packing.
4. Packs from the signed binaries.
5. **Scribe round 2** — NuGet-signs the packages (and SDK symbol packages).
6. Verifies every signature on Windows against the pinned certificate and attests the signed packages.
7. **Waits for approval of the `nuget-org` environment**, then tags the built commit, publishes to NuGet.org (Trusted Publishing), waits for indexing and creates the GitHub release with the signed packages attached. An SDK release also posts the announcement in GitHub Discussions (Announcements, titled `<version>`, built by `.github/scripts/sdk-discussion-body.sh` from the `whats-new.md` section) and deploys the docs to prod.

Each Scribe round needs two approvals in Scribe (approvers are notified on Slack) within about 5h45m, or the run fails and must be re-run (a new submission). Only one release runs at a time. If Scribe is unavailable, wait for it and re-run the failed jobs; there is no local signing fallback.

| Release | Branch | Tag | GitHub release |
|---|---|---|---|
| NativeShims | `develop` or `main` | `nativeshims-<v>` | "Yubico.NativeShims <v>" |
| SDK (Core + YubiKey) | `main` | `<v>` | "<v>" |

NativeShims is released from `develop` when `develop` holds Core/YubiKey changes that are not ready for `main`. When a release needs both, NativeShims is released first; the SDK build on `main` then resolves it from NuGet.org.

## State file

Location: `~/Releases/<version>/.state.json` (for a NativeShims-only release, `~/Releases/nativeshims-<nsVersion>/`). Created in phase 1, updated at every phase boundary, read by `/Release resume`.

```json
{
  "scope": "sdk",
  "version": "1.16.1",
  "previousTag": "1.16.0",
  "releaseDate": "2026-04-29",
  "nativeShimsVersion": null,
  "nativeShimsRef": null,
  "nativeShimsRunId": null,
  "buildRunId": null,
  "currentPhase": 4,
  "complete": false,
  "releasePrNumber": null,
  "categorizedPRs": { "features": [], "bugfixes": [], "docs": [], "deps": [], "security": [], "misc": [] }
}
```

`scope` is `sdk`, `nativeshims` or `both`.

**Date storage convention**: `releaseDate` is always stored as ISO `YYYY-MM-DD`. The Phase 1 prompt collects it in human form (`Month Dth, YYYY`), but the skill normalizes to ISO before writing state. Display formatting is reapplied at write time for `whats-new.md` (long form: `April 29th, 2026`) and the Slack draft (long form). Always derive the display string from the ISO field — never store both.

**Categorization buckets**: All buckets above MUST be present in state (even empty). Phase 3 categorizes "everything else" into `misc`; Phase 7 includes `misc` in both the `whats-new.md` Miscellaneous section and the Slack draft (under a `Miscellaneous 🧰📌` heading) so PRs are never dropped.

The state file is the single source of truth for resume. Update it before any operation that could fail.

## Watching a release run

Used by phases 2 and 4. After `gh workflow run`, capture the run ID (`gh run list --workflow=<file> --limit 1 --json databaseId -q '.[0].databaseId'`), store it in state, and poll in the background every 30s. Scribe rounds can be approved within a minute, so a slower poll misses them:

```bash
while true; do
  status=$(gh run view <runId> --json status,conclusion -q '[.status,.conclusion] | join(",")')
  if [[ "$status" == completed,* ]]; then echo "$status"; break; fi
  sleep 30
done
```

While it runs, tell the operator when action is needed. Check job **steps**, not just jobs — Scribe round 1 is the `Sign assemblies` step inside the `Build artifacts` job:
- an in-progress step or job whose name starts with `Sign` (`gh run view <runId> --json jobs -q '.jobs[] | select(.status=="in_progress") | .name as $j | .steps[] | select(.status=="in_progress") | "\($j): \(.name)"'`): "Scribe signing job submitted — approvals needed in Scribe (two approvers, Slack notifies them)." Print the run URL; it is the reference shown in Scribe.
- `gh api repos/Yubico/Yubico.NET.SDK/actions/runs/<runId>/pending_deployments` non-empty: "Signed packages verified. Approve the `nuget-org` deployment to publish: <run URL>."

On `failure`: STOP and print `gh run view <runId> --log-failed`. A failed publish job can be re-run with `gh run rerun <runId> --failed`; tagging, pushing, the GitHub release and the Discussions announcement skip what already exists. Never start a new release run for a version that is already partly published.

## Phase 1 — Pre-flight

**Prerequisites**:
- `gh auth status` — must be authenticated
- `git remote -v` — confirm `origin` points to `Yubico/Yubico.NET.SDK`

**Steps**:
1. `git fetch --tags origin`
2. `git tag --sort=-v:refname | head -5` → show recent tags, parse the latest SDK tag (`X.Y.Z`) as `previousTag`
3. `AskUserQuestion`: "What are we releasing?" Options: "SDK (Yubico.Core + Yubico.YubiKey)" / "NativeShims only" / "Both (NativeShims first)". Hint with `git diff <previousTag>..origin/develop --stat -- Yubico.NativeShims/` whether NativeShims changed. Store as `scope`.
4. For `sdk`/`both`: `gh pr list --base develop --state open --json number,title,author --limit 20` → display, then `AskUserQuestion`: "Any of these need to merge before release?" Options: "All clear, proceed" / "Wait — I'll merge manually" / "Specific PRs blocking"
5. For `sdk`/`both`: `AskUserQuestion`: "Confirm release version" — default `+1 patch` of `previousTag`; also offer `+1 minor`, `+1 major`, custom
6. For `nativeshims`/`both`: `AskUserQuestion`: "NativeShims version" — default `+1 patch` of the latest `nativeshims-*` tag (or the latest version on NuGet.org)
7. For `sdk`/`both`: `AskUserQuestion`: "Release date" — default today (in `Month Dth, YYYY` format matching whats-new.md style)
8. **Hardware test reminder** — print: "Before continuing, confirm you've tested PIV + SCP on real YubiKey hardware. The skill cannot do this for you." Gate with `AskUserQuestion`: "Hardware tests pass?" / "Skip (not recommended)". The skill must NEVER run integration tests itself.
9. Create the release directory and write initial `.state.json`

## Phase 2 — NativeShims release (scope `nativeshims` or `both`)

1. `AskUserQuestion`: "Release NativeShims from which branch?" Options:
   - "develop (release now)" — required when develop has Core/YubiKey changes that must not reach main yet
   - "main (after the release PR merges)" — only for scope `both`; then continue with phase 3 and come back here from phase 4
2. Dispatch: `gh workflow run build-nativeshims.yml --ref <branch> -f release=true -f version=<nsVersion>`
3. Watch the run (see above). Expect two Scribe rounds and the `nuget-org` approval.
4. **Verify**: `curl -sfI https://api.nuget.org/v3-flatcontainer/yubico.nativeshims/<nsVersion>/yubico.nativeshims.<nsVersion>.nupkg` returns 200, `gh release view nativeshims-<nsVersion>` succeeds.
5. Scope `nativeshims`: go to phase 7 (Slack draft only; no merge-back unless released from main). Scope `both`: continue with phase 3 (or phase 4 if NativeShims waits for main).

## Phase 3 — Release branch (scope `sdk` or `both`)

1. `git checkout develop && git pull origin develop`
2. `git checkout -b release/<version>` (per gitflow + project CLAUDE.md)
3. **Lockfile note**: No manual lockfile repin commit is needed on the release branch. The `build-artifacts` job in `build.yml` runs `dotnet restore --force-evaluate` on main, which auto-resolves to the latest stable NativeShims from nuget.org. The `enforce-branch-policy` job issues a warning (not an error) if a prerelease pin is detected, since the build self-corrects. If this release needs a new NativeShims, it must already be on NuGet.org (phase 2) before the SDK release run.
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
   - Write the new section exactly in the format of `Templates/whats-new-section.md`: `### <version>`, a `Release date:` line, one of the headings `Features:`, `Bug Fixes:`, `Documentation:`, `Miscellaneous:`, `Dependencies:` per non-empty bucket, single-line `- ` bullets each ending in `([#n](url))` (or `([#n](url), [#m](url))`), no sub-bullets, and the `_________` separator. CI builds both the GitHub release notes and the Discussions announcement from this format.
   - Read the previous release's section for voice and phrasing
   - Insert new `### <version>` block under the appropriate `## 1.x.x Releases` heading (create the heading if needed)
   - **Style rules** (derived from existing whats-new.md patterns):
     - Bug Fixes: "Fixed an issue where..." pattern, passive voice
     - Documentation: passive voice ("Documentation has been updated/corrected/added to...")
     - Miscellaneous: passive voice ("...has been switched/added/updated")
     - Features: active voice is acceptable ("Added...", "Introduced...")
     - Dependencies: brief, passive ("Dependencies have been updated...")
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
   Capture the returned PR number into `state.releasePrNumber`. CI builds the GitHub release notes from the same `whats-new.md` section, so the section heading must be exactly `### <version>`.
10. Print PR URL, instruct the operator to get reviewers

## Phase 4 — Merge + SDK release (scope `sdk` or `both`)

1. **Wait for merge** — poll `gh pr view <prNumber> --json state,reviewDecision,mergeStateStatus` every 60s. `main` restricts pushes with an empty allow-list, so an approved PR with all checks green still reports `BLOCKED`. Once `reviewDecision=APPROVED` and no check is failing, ask the operator for permission and merge with `gh pr merge <prNumber> --merge --admin`. Never use `--admin` before an approval. If the operator wants to stop and resume later, the state file already has the PR number.
2. After merge: `git checkout main && git pull origin main`
3. If scope is `both` and NativeShims waits for main: run phase 2 now from `main`, and continue only once NativeShims is on NuGet.org.
4. **Release date**: if today differs from `state.releaseDate`, update the `Release date:` line in `whats-new.md` on `main` (PR) before dispatching. CI uses it for the GitHub release notes and the Discussions announcement.
5. Dispatch: `gh workflow run build.yml --ref main -f release=true -f version=<version>` — store `buildRunId`.
6. Watch the run (see above). Expect two Scribe rounds and the `nuget-org` approval.

## Phase 5 — Verify the release

1. **Tag**: `git ls-remote --tags origin <version>` returns the merge commit on main.
2. **NuGet.org**: `https://api.nuget.org/v3-flatcontainer/yubico.core/<version>/yubico.core.<version>.nupkg` and the same for `yubico.yubikey` return 200.
3. **GitHub release**: `gh release view <version> --json isDraft,assets -q '[.isDraft, [.assets[].name]]'` — published, with both `.nupkg` and both `.snupkg`.
4. **Docs**: the run's `Deploy docs` job succeeded.
5. **Discussions**: the `Announce the release in GitHub Discussions` step succeeded; its log has the post URL.

## Phase 6 — Merge main back to develop

```bash
git checkout develop && git pull
git merge main --no-ff -m "Merge main back into develop after <version> release"
git push origin develop
```

Assert `build/Versions.props` still has `<CommonVersion>0.0.0-dev</CommonVersion>` — print a warning if drifted.

## Phase 7 — Closing

1. **Generate Slack #ask-tla announcement** — print as fenced code block ready to copy. Use cached `categorizedPRs` from state. Exact format:

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

For a NativeShims-only release, post a short note instead: version, `https://www.nuget.org/packages/Yubico.NativeShims/<nsVersion>` and the GitHub release link.

2. **Print closing checklist** (manual — skill cannot automate):
   - [ ] Post drafted message in Slack #ask-tla
   - [ ] Close release in Jira
3. Mark `.state.json` as `currentPhase: 7, complete: true`

## Failure modes & recovery

- **Preflight fails** → fix the cause (version, branch, existing tag) and dispatch again.
- **Build or tests fail** → nothing was signed or tagged. Fix, merge, dispatch again.
- **Scribe round times out or is rejected** → re-run the workflow run (`gh run rerun <runId> --failed`); it is a new Scribe submission needing new approvals.
- **Signature verification fails** → STOP. Do not approve `nuget-org`. Investigate the signed artifacts in the run.
- **Publish job fails part-way** → `gh run rerun <runId> --failed`. The tag, NuGet.org packages and GitHub release are only created if missing.
- **NuGet.org rejects the package (certificate)** → the signing certificate must be registered on the NuGet.org package owner account (rotation due before 2027-02-26).
- **Resume on different machine** → `/Release resume <version>` reads the state file and skips completed phases. Phase boundaries are the resume points.
