---
name: Release
description: Drives the Yubico .NET SDK and Yubico.NativeShims releases end-to-end — version gating, release branch and release notes, CI release runs signed by Scribe, NuGet.org publishing approval, verification, post-release merge-back, and Slack #ask-tla announcement. USE WHEN release, drop release, ship release, cut release, publish release, release SDK, release NativeShims, dotnet release, NuGet release, /Release, /Release resume.
---

# Release

Project-local skill for shipping a Yubico .NET SDK or Yubico.NativeShims release. The operator answers gating questions, signs off on the release notes, and approves in two places: the Scribe signing jobs (two approvers per round) and the `nuget-org` GitHub environment. Building, signing, verification, tagging, publishing to NuGet.org, the GitHub release and the docs deploy all run in GitHub Actions — nothing is downloaded or signed locally.

The skill works in two modes:
- **`/Release`** — full flow from phase 1 (pre-flight) onward
- **`/Release resume <version>`** — picks up at the current phase using cached state from `~/Releases/<version>/.state.json`.

## Workflow Routing

| Request Pattern | Route To |
|---|---|
| Drop release, ship release, cut release, publish release, /Release, /Release resume | `Workflows/DropRelease.md` |

## Examples

**Example 1: SDK release**
```
User: "/Release"
→ Phase 1: scope "SDK", version 1.19.0, release date, no blocking PRs, hardware tests confirmed
→ Phase 3: creates release/1.19.0 from develop, drafts whats-new.md, opens PR to main
→ Phase 4: after merge, runs `gh workflow run build.yml --ref main -f release=true -f version=1.19.0`
           and watches it: Scribe approvals (assemblies, then packages), then the nuget-org approval
→ Phase 5: verifies tag, NuGet.org packages, GitHub release and docs deploy
→ Phase 6: merges main back to develop
→ Phase 7: prints the Slack #ask-tla announcement ready to copy
```

**Example 2: NativeShims-only release from develop**
```
User: "/Release"
→ Phase 1: scope "NativeShims only", version 1.18.2
→ Phase 2: develop also holds unreleased Core/YubiKey changes, so release from develop:
           `gh workflow run build-nativeshims.yml --ref develop -f release=true -f version=1.18.2`
→ Watches the run through two Scribe rounds and the nuget-org approval
→ Verifies nativeshims-1.18.2 on NuGet.org and GitHub, prints a short Slack note
```

**Example 3: NativeShims and SDK together**
```
User: "/Release"
→ Phase 1: scope "Both"
→ Phase 2: NativeShims released first (develop now, or main after the release PR merges)
→ Phases 3–7: SDK release as in example 1; the SDK build resolves the new NativeShims from NuGet.org
```

## Hard Constraints

- **Hardware tests are manual**: the skill reminds the operator to test on real YubiKeys and must NEVER run integration tests itself.
- **One release run at a time**; NativeShims is published before an SDK release that depends on it.
- **SDK releases only from `main`**; NativeShims releases from `develop` or `main`. The workflows enforce this.
- **No Versions.props edits**: the version is a workflow input; `<CommonVersion>0.0.0-dev</CommonVersion>` stays unchanged.
- **Release notes never auto-committed**: the skill drafts `docs/users-manual/getting-started/whats-new.md` and shows the diff for approval before commit. CI uses the `### <version>` section as the GitHub release notes.
- **Never approve `nuget-org` after a failed verification**, and never start a second release run for a partly published version — re-run the failed jobs instead.
- **Deterministic verification gates**: every dispatch, publish and release must be followed by an explicit check (run conclusion, NuGet.org HTTP 200, `gh release view`, `git ls-remote`). Non-deterministic LLM execution requires these guardrails.
