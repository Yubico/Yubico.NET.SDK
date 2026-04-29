---
name: Release
description: Drives the Yubico .NET SDK release end-to-end — version gating, release branch, NativeShims ordering, CI dispatch, tagging, Windows-wizard sign+publish, GitHub release, post-release merge-back, and Slack #ask-tla announcement. USE WHEN release, drop release, ship release, cut release, publish release, release SDK, dotnet release, NuGet release, /Release, /Release resume.
---

# Release

Project-local skill for shipping a Yubico .NET SDK release. The skill is the operator — Dennis only invokes it, answers gating questions, and (on Windows) plugs in his code-sign YubiKey. Every other step (branch creation, CI dispatch, artifact download, signing, publishing, tagging, GitHub release, Slack draft) is automated or surfaces an explicit decision gate.

The skill works in two modes:
- **`/Release`** — full flow from phase 1 (pre-flight) onward
- **`/Release resume <version>`** — picks up at phase 5 (sign+publish) using cached state from `~/Releases/<version>/.state.json`. Used when phases 1–4 ran on macOS/Linux and the operator switches to Windows for signing.

The Windows-only constraint (`build/sign.ps1` + smart-card YubiKey + `signtool.exe`) is enforced at phase 5 — the skill detects platform and either runs the full wizard (Windows) or stops with a handoff (macOS/Linux).

## Workflow Routing

| Request Pattern | Route To |
|---|---|
| Drop release, ship release, cut release, publish release, /Release, /Release resume | `Workflows/DropRelease.md` |

## Examples

**Example 1: Full release on Windows**
```
User: "/Release"
→ Skill loads Workflows/DropRelease.md
→ Phase 1: confirms version 1.16.1, release date, no blocking PRs
→ Phase 2: detects no Yubico.NativeShims/ changes, skips NativeShims rebuild
→ Phase 3: creates release/1.16.1 from develop, drafts whats-new.md, opens PR to main
→ Phase 4: after PR merged, dispatches build.yml with version=1.16.1, polls until green, tags 1.16.1
→ Phase 5 (Windows): downloads artifacts to ~/Releases/1.16.1/, runs sign.ps1, publishes to NuGet.org
→ Phase 6: creates draft GitHub release with signed assets, triggers deploy-docs.yml
→ Phase 7: merges main back to develop, prints Slack #ask-tla announcement ready to copy
```

**Example 2: Cross-machine release (start macOS, finish Windows)**
```
User (on macOS): "/Release"
→ Phases 1-4 complete (release branch, PR, merge, tag)
→ Phase 5 detects darwin → STOPS, prints handoff with build.yml run ID and instruction to run `/Release resume 1.16.1` on Windows
→ State cached to ~/Releases/1.16.1/.state.json (run IDs, version, NativeShims flag)

User (on Windows): "/Release resume 1.16.1"
→ Loads cached state, skips phases 1-4
→ Phase 5: downloads artifacts (NativeShims first if rebuilt), runs sign.ps1, publishes
→ Phases 6-7 complete normally
```

**Example 3: NativeShims-bearing release**
```
User: "/Release"
→ Phase 2 detects changes in Yubico.NativeShims/ since last tag
→ AskUserQuestion confirms rebuild + NativeShims version bump
→ Dispatches build-nativeshims.yml first, polls
→ HARD GATE: NativeShims must be signed AND published to NuGet.org BEFORE build.yml dispatches
→ Phase 5 status board shows both NativeShims and main package rows
```

## Hard Constraints

- **Windows-only sign step**: phase 5 refuses to run on non-Windows
- **NativeShims ordering**: when rebuilt, NativeShims signs + publishes to NuGet.org *before* main `build.yml` dispatches
- **Tag only after green CI**: `git tag` runs only after `build.yml` reports success — failed builds mean broken artifacts and a poisoned tag
- **No Versions.props edits**: version is passed as `build.yml` workflow_dispatch input; `<CommonVersion>0.0.0-dev</CommonVersion>` stays unchanged
- **Release notes never auto-committed**: skill drafts `docs/users-manual/getting-started/whats-new.md` and shows diff for approval before commit
