---
name: Release
description: Drives the Yubico .NET SDK release end-to-end — version gating, release branch, NativeShims ordering, CI dispatch, tagging, Windows-wizard sign+publish, GitHub release, post-release merge-back, and Slack #ask-tla announcement. USE WHEN release, drop release, ship release, cut release, publish release, release SDK, dotnet release, NuGet release, /Release, /Release resume.
---

# Release

Project-local skill for shipping a Yubico .NET SDK release. The operator invokes the skill, answers gating questions, and (on Windows) plugs in the code-sign YubiKey. Every other step (branch creation, CI dispatch, artifact download, signing, publishing, tagging, GitHub release, Slack draft) is automated or surfaces an explicit decision gate.

The skill works in two modes:
- **`/Release`** — full flow from phase 1 (pre-flight) onward
- **`/Release resume <version>`** — picks up at the current phase using cached state from `~/Releases/<version>/.state.json`. Most commonly used to resume at phase 5 (sign+publish) when phases 1–4 ran on macOS/Linux and the operator switches to Windows for signing, but works at any phase boundary.

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
Operator (on macOS): "/Release"
→ Phases 1-4 complete (release branch, PR, merge, tag)
→ Phase 5 detects darwin → STOPS, prints handoff with build.yml run ID and instruction to run `/Release resume 1.16.1` on Windows
→ State cached to ~/Releases/1.16.1/.state.json (run IDs, version, NativeShims flag)

Operator (on Windows): "/Release resume 1.16.1"
→ Loads cached state, skips phases 1-4
→ Phase 5: downloads artifacts (NativeShims first if rebuilt), runs sign.ps1, publishes
→ Phases 6-7 complete normally
```

**Example 3: NativeShims-bearing release (build from develop)**
```
User: "/Release"
→ Phase 2 detects changes in Yubico.NativeShims/ since last tag
→ AskUserQuestion confirms rebuild + NativeShims version bump
→ AskUserQuestion: "Build from develop or main?" → operator chooses "develop"
→ Dispatches build-nativeshims.yml --ref develop, polls in background (120s, ~15-25 min)
→ HARD GATE: NativeShims must be signed AND published to NuGet.org BEFORE build.yml dispatches
→ Phase 5 status board shows both NativeShims and main package rows
```

**Example 4: NativeShims-bearing release (deferred to main)**
```
User: "/Release"
→ Phase 2 detects changes, operator chooses "Build from main (after PR merge)"
→ Phase 2 skips dispatch, proceeds to Phase 3 (release branch + PR)
→ Phase 4: after PR merges, dispatches build-nativeshims.yml --ref main, polls in background
→ On success: enters Phase 5 NativeShims half (sign+publish), verifies NuGet 200
→ Returns to Phase 4 step 4: dispatches build.yml --ref main with push-to-docs=true
→ Phase 5 main half: downloads artifacts as zips via API, signs, publishes nupkgs then snupkgs
→ Phase 6: draft GitHub release with signed assets attached, deploy docs to prod
```

## Hard Constraints

- **Code-signing YubiKey must be unplugged during phases 1–4**: The operator's code-signing YubiKey must NOT be connected to the machine while any build or CI step runs. Integration tests that enumerate YubiKeys can accidentally run PIV/PGP resets against any connected key. The skill gates this: Phase 1 asks the operator to confirm the YubiKey is unplugged. Phase 5 is the ONLY phase where it should be plugged in — `signtool.exe` and `nuget sign` read the PIV certificate safely but cannot coexist with stray test runs. The skill must NEVER run integration tests itself.
- **Windows-only sign step**: phase 5 refuses to run on non-Windows
- **NativeShims ordering**: when rebuilt (from either develop or main), NativeShims signs + publishes to NuGet.org *before* main `build.yml` dispatches. The operator chooses whether to build from develop (immediate) or main (deferred to after PR merge).
- **Tag only after green CI**: `git tag` runs only after `build.yml` reports success — failed builds mean broken artifacts and a poisoned tag
- **No Versions.props edits**: version is passed as `build.yml` workflow_dispatch input; `<CommonVersion>0.0.0-dev</CommonVersion>` stays unchanged
- **Release notes never auto-committed**: skill drafts `docs/users-manual/getting-started/whats-new.md` and shows diff for approval before commit
- **Lockfile repin is CI-automated**: `build.yml` runs `--force-evaluate` on main to auto-resolve to latest stable NativeShims. No manual repin commit needed on the release branch.
- **Signed assets must be attached to draft release**: GitHub release assets are immutable after publish. All signed `.nupkg` and `.snupkg` files MUST be uploaded when creating the draft, not after publishing.
- **snupkgs push after nupkgs**: Symbol packages must be pushed to NuGet.org only after the corresponding nupkgs are indexed (HTTP 200 on package URL). Pushing simultaneously can fail.
- **Deterministic verification gates**: every CI dispatch, NuGet publish, tag push, and GitHub release operation must be followed by an explicit verification check (polling HTTP status, `gh release view`, `git ls-remote`, etc.). Non-deterministic LLM execution requires these guardrails.
