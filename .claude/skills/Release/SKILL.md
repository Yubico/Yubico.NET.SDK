---
name: Release
description: Drives the Yubico .NET SDK release end-to-end — version gating, release branch, NativeShims ordering, CI dispatch, tagging, cross-platform sign+publish, GitHub release, post-release merge-back, and Slack #ask-tla announcement. USE WHEN release, drop release, ship release, cut release, publish release, release SDK, dotnet release, NuGet release, /Release, /Release resume.
---

# Release

Project-local skill for shipping a Yubico .NET SDK release. The operator invokes the skill, answers gating questions, and plugs in the code-sign YubiKey only for phase 5. Every other step (branch creation, CI dispatch, artifact download, signing, publishing, tagging, GitHub release, Slack draft) is automated or surfaces an explicit decision gate.

The skill works in two modes:
- **`/Release`** — full flow from phase 1 (pre-flight) onward
- **`/Release resume <version>`** — picks up at the current phase using cached state from `~/Releases/<version>/.state.json`.

Phase 5 uses the cross-platform tool documented in `build/release-sign/README.md`. It prompts once per signing run in an interactive terminal; a release without a NativeShims rebuild has one Core signing run.

## Workflow Routing

| Request Pattern | Route To |
|---|---|
| Drop release, ship release, cut release, publish release, /Release, /Release resume | `Workflows/DropRelease.md` |

## Examples

**Example 1: Full release**
```
User: "/Release"
→ Skill loads Workflows/DropRelease.md
→ Phase 1: confirms version 1.16.1, release date, no blocking PRs
→ Phase 2: detects no Yubico.NativeShims/ changes, skips NativeShims rebuild
→ Phase 3: creates release/1.16.1 from develop, drafts whats-new.md, opens PR to main
→ Phase 4: after PR merged, dispatches build.yml with version=1.16.1, polls until green, tags 1.16.1
→ Phase 5: downloads artifacts to ~/Releases/1.16.1/, runs build/release-sign, publishes to NuGet.org
→ Phase 6: creates draft GitHub release with signed assets, triggers deploy-docs.yml
→ Phase 7: merges main back to develop, prints Slack #ask-tla announcement ready to copy
```

**Example 2: Resume a release**
```
Operator: "/Release"
→ Phases 1-4 complete (release branch, PR, merge, tag)
→ Operator pauses after phase 4
→ State cached to ~/Releases/1.16.1/.state.json (run IDs, version, NativeShims flag)

Operator: "/Release resume 1.16.1"
→ Loads cached state, skips phases 1-4
→ Phase 5: downloads artifacts (NativeShims first if rebuilt), runs build/release-sign, publishes
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

- **Code-signing YubiKey must be unplugged during phases 1–4**: The operator's code-signing YubiKey must NOT be connected to the machine while any build or CI step runs. Integration tests that enumerate YubiKeys can accidentally run PIV/PGP resets against any connected key. The skill gates this: Phase 1 asks the operator to confirm the YubiKey is unplugged. Phase 5 is the ONLY phase where it should be plugged in. The skill must NEVER run integration tests itself.
- **Cross-platform sign step**: phase 5 follows `build/release-sign/README.md` and requires an installed `nuget-sign` CLI, Go, and `gh`
- **NativeShims ordering**: when rebuilt (from either develop or main), NativeShims signs + publishes to NuGet.org *before* main `build.yml` dispatches. The operator chooses whether to build from develop (immediate) or main (deferred to after PR merge).
- **Tag only after green CI**: `git tag` runs only after `build.yml` reports success — failed builds mean broken artifacts and a poisoned tag
- **No Versions.props edits**: version is passed as `build.yml` workflow_dispatch input; `<CommonVersion>0.0.0-dev</CommonVersion>` stays unchanged
- **Release notes never auto-committed**: skill drafts `docs/users-manual/getting-started/whats-new.md` and shows diff for approval before commit
- **Lockfile repin is CI-automated**: `build.yml` runs `--force-evaluate` on main to auto-resolve to latest stable NativeShims. No manual repin commit needed on the release branch.
- **Signed assets must be attached to draft release**: GitHub release assets are immutable after publish. All signed `.nupkg` and `.snupkg` files MUST be uploaded when creating the draft, not after publishing.
- **snupkgs push after nupkgs**: Symbol packages must be pushed to NuGet.org only after the corresponding nupkgs are indexed (HTTP 200 on package URL). Pushing simultaneously can fail.
- **Deterministic verification gates**: every CI dispatch, NuGet publish, tag push, and GitHub release operation must be followed by an explicit verification check (polling HTTP status, `gh release view`, `git ls-remote`, etc.). Non-deterministic LLM execution requires these guardrails.
