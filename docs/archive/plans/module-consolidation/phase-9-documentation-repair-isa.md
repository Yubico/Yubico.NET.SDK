---
task: Phase 9 Documentation Repair Pass
slug: phase-9-documentation-repair
effort: E3
phase: complete
progress: 32/32
mode: algorithm
started: 2026-06-06T00:00:00Z
updated: 2026-06-06T00:05:00Z
---

# Phase 9 ISA: Documentation Repair Pass

## Problem

Module documentation still contains stale v1 API names, broken relative links, raw `dotnet test` guidance, old test infrastructure names, and examples that no longer match the source. This undermines the consolidation goal because agents and humans may follow docs that contradict the current SDK shape.

## Vision

The documentation repair pass makes the most harmful stale guidance disappear without turning Phase 9 into a full docs rewrite. A reader should see current toolchain commands, valid relative links, and source-backed module examples, while broad API narrative rewrites remain deferred unless they can be verified locally.

## Out of Scope

- No SDK source-code refactors.
- No hardware or integration tests.
- No exhaustive rewrite of every module README.
- No new operation-specific command classes or command-like abstractions.
- No speculative API examples that are not source-backed.
- No broad cleanup of archived or historical plans unless they affect active guidance.

## Principles

- Documentation must match executable source reality.
- Prefer small source-backed corrections over broad prose churn.
- Keep module docs aligned with the flat-flow house style.
- Remove stale instructions instead of replacing them with longer scaffolding.

## Constraints

- Work only on branch `yubikit-consolidation`.
- Use `dotnet toolchain.cs` examples; never raw `dotnet test` or raw `dotnet build` as user-facing guidance.
- Documentation changes must be verifiable with `rg`, `Read`, or focused build commands.
- Preserve unrelated untracked Core note files.
- Keep integration and hardware behavior read-only by default; this phase does not run hardware tests.

## Goal

Repair the highest-confidence stale documentation discovered in Phase 9 inventory, focused on broken links, toolchain command drift, current test infrastructure naming, and current module API examples. Complete the phase with source-backed verification, cross-vendor/advisor review, a learning note, a commit, and a compact continuation summary.

## Criteria

- [x] ISC-1: Branch status shows `## yubikit-consolidation` before documentation edits.
- [x] ISC-2: The two unrelated Core YubiKey note files remain unstaged.
- [x] ISC-3: Phase 9 scope is recorded in this phase ISA.
- [x] ISC-4: Anti: no SDK source `.cs` file is modified.
- [x] ISC-5: Anti: no hardware or integration test command is run.
- [x] ISC-6: README/CLAUDE inventory findings are captured from read-only inspection.
- [x] ISC-7: Broken module-root `../CLAUDE.md` links are corrected.
- [x] ISC-8: Broken test-doc `../../../docs/TESTING.md` links are corrected.
- [x] ISC-9: Active module docs do not recommend raw `dotnet test` commands.
- [x] ISC-10: Active example READMEs do not recommend raw `dotnet build` commands.
- [x] ISC-11: FIDO2 test docs use `IFidoHidConnection`, not `IFidoConnection`.
- [x] ISC-12: FIDO2 test docs use current `WithCredProtect` builder naming.
- [x] ISC-13: FIDO2 test docs use current `WithHmacSecret` builder naming.
- [x] ISC-14: FIDO2 module doc test command example uses toolchain syntax.
- [x] ISC-15: YubiHsm test doc describes YubiHSM Auth on YubiKey, not standalone YubiHSM hardware.
- [x] ISC-16: Tests.Shared CLAUDE connection example uses `ConnectAsync<TConnection>()`.
- [x] ISC-17: Management CLAUDE examples do not use `YubiKeyDevice.FindBySerialNumber`.
- [x] ISC-18: Management CLAUDE examples do not use `YubiKeyDevice.FindAll`.
- [x] ISC-19: Management CLAUDE backend interface example matches current backend method names.
- [x] ISC-20: Tests.Shared README no longer presents `[YubiKeyTheory]` as current infrastructure.
- [x] ISC-21: Tests.Shared README presents `[Theory]` plus `[WithYubiKey]` as current infrastructure.
- [x] ISC-22: SecurityDomain test README uses toolchain command examples.
- [x] ISC-23: Management example README uses actual repo-relative project path.
- [x] ISC-24: PIV example README uses actual repo-relative project path.
- [x] ISC-25: FIDO example README uses toolchain build guidance.
- [x] ISC-26: PIV example README does not name obsolete `SetRetryLimitsAsync`.
- [x] ISC-27: Root README no longer uses nonexistent `DeviceRepository` quick-start API.
- [x] ISC-28: Core README no longer uses nonexistent `DeviceRepository` quick-start API.
- [x] ISC-29: Core README transport names match current connection interface names.
- [x] ISC-30: Focused build verification covers docs-referenced modules or example projects where practical.
- [x] ISC-31: Phase 9 learning note records scope, verification, review, and deferred items.
- [x] ISC-32: Final git diff contains only intended documentation artifacts.

## Test Strategy

| isc | type | check | threshold | tool |
| --- | --- | --- | --- | --- |
| ISC-1 | branch | `git status --short --branch` | shows `## yubikit-consolidation` | Bash |
| ISC-2 | worktree | `git status --short` | Core note files remain untracked only | Bash |
| ISC-3 | file | read this ISA | scope present | Read |
| ISC-4 | diff | `git diff --name-only` | no `.cs` files | Bash |
| ISC-5 | command history | verify no integration command run | no integration command recorded | inspection |
| ISC-6 | inventory | task results and grep output | README and CLAUDE findings present | Task/Grep |
| ISC-7..29 | docs | grep stale/new patterns | stale absent or current present | Grep/Read |
| ISC-30 | build | focused toolchain builds | exit 0 | Bash |
| ISC-31 | file | read learning note | required sections present | Read |
| ISC-32 | diff | `git diff --stat` and `git diff --name-only` | only intended docs | Bash |

## Features

| name | description | satisfies | depends_on | parallelizable |
| --- | --- | --- | --- | --- |
| Scope and inventory | Capture Phase 9 doc drift from read-only inspection | ISC-1, ISC-2, ISC-3, ISC-6 | none | true |
| Link and command repair | Fix broken doc links and toolchain command drift | ISC-7, ISC-8, ISC-9, ISC-10, ISC-14, ISC-22, ISC-25 | Scope and inventory | true |
| Source-backed API doc repair | Fix high-confidence stale API names and examples | ISC-11..13, ISC-15..21, ISC-23..29 | Scope and inventory | true |
| Verification and closeout | Run focused verification, review, learning, and diff checks | ISC-30, ISC-31, ISC-32 | Link and command repair, Source-backed API doc repair | false |

## Decisions

- 2026-06-06 refined: Phase 9 is documentation-only. Large source refactors and hardware tests are out of scope.
- 2026-06-06 refined: Fix high-confidence stale guidance first; defer exhaustive README rewrites if source-backed verification would expand the phase too far.
- 2026-06-06 delegation: README/example and CLAUDE/test-doc inventories were split into two read-only exploration agents because the surfaces are independent.

## Verification

- ISC-1: Bash — `git status --short --branch` showed `## yubikit-consolidation`.
- ISC-2: Bash — `git status --short --branch` showed only the two known untracked Core YubiKey note files outside Phase 9 docs.
- ISC-3: Read — this ISA records Phase 9 documentation-only scope, criteria, features, and test strategy.
- ISC-4: Bash — `git diff --name-only -- "*.cs"` returned no files.
- ISC-5: Inspection — no `dotnet toolchain.cs -- test --integration` or hardware command was run in this phase.
- ISC-6: Task/Grep — read-only README and CLAUDE inventory agents returned prioritized stale-doc findings with file references.
- ISC-7: Grep — stale broken module-root link pattern for `](../CLAUDE.md)` is absent from targeted module root docs after correction; remaining `../CLAUDE.md` links are test-to-module parent links.
- ISC-8: Grep — broken test-doc `../../../docs/TESTING.md` links were replaced with `../../../../docs/TESTING.md`.
- ISC-9: Grep — `^dotnet (test|build)(\s|$)` under `src/**/*.md` returned no matches.
- ISC-10: Grep — active example READMEs no longer contain raw `dotnet build` command lines.
- ISC-11: Grep — stale active names pattern under `src/**/*.md` returned no `IFidoConnection` matches.
- ISC-12: Grep — stale active names pattern under `src/**/*.md` returned no `AddCredProtect` matches.
- ISC-13: Grep — stale active names pattern under `src/**/*.md` returned no `AddHmacSecret` matches.
- ISC-14: Read/Grep — FIDO2 CLAUDE uses `dotnet toolchain.cs -- test --project Fido2 --smoke --filter "RequiresUserPresence!=true"`.
- ISC-15: Read — `src/YubiHsm/tests/CLAUDE.md` now says integration tests require a YubiKey with the YubiHSM Auth applet and firmware 5.4.3+.
- ISC-16: Grep — stale active names pattern returned no `OpenConnectionAsync`; Tests.Shared CLAUDE uses `ConnectAsync<ISmartCardConnection>()`.
- ISC-17: Grep — stale active names pattern returned no `YubiKeyDevice.FindBySerialNumber`.
- ISC-18: Grep — stale active names pattern returned no `YubiKeyDevice.FindAll`.
- ISC-19: Read — Management CLAUDE backend example lists current `ReadConfigAsync`, `WriteConfigAsync(ReadOnlyMemory<byte>)`, and `DeviceResetAsync` methods.
- ISC-20: Grep — stale active names pattern returned no `[YubiKeyTheory]`.
- ISC-21: Read — Tests.Shared README documents `[Theory]` plus `[WithYubiKey]` and `WithYubiKeyAttribute : DataAttribute`.
- ISC-22: Read — SecurityDomain tests README uses `dotnet toolchain.cs -- test --project SecurityDomain` examples.
- ISC-23: Read — ManagementTool README uses `dotnet run --project src/Management/examples/ManagementTool/ManagementTool.csproj`.
- ISC-24: Read — PivTool README uses `dotnet run --project src/Piv/examples/PivTool/PivTool.csproj`.
- ISC-25: Read — FidoTool README uses `dotnet toolchain.cs -- build --project Fido2`.
- ISC-26: Grep — stale active names pattern returned no `SetRetryLimitsAsync`.
- ISC-27: Grep — root README returned no `DeviceRepository`, `Fido2Session`, or `MakeCredentialParameters` matches.
- ISC-28: Grep — Core README returned no `DeviceRepository` matches.
- ISC-29: Grep — Core README returned no `IFidoConnection`, `IOtpConnection`, or `OpenConnectionAsync` matches.
- ISC-30: Bash — `dotnet toolchain.cs -- build --project Core`, `Fido2`, `Management`, `SecurityDomain`, and `Piv` all passed with 0 warnings and 0 errors.
- ISC-31: Read — `docs/plans/module-consolidation/phase-9-documentation-repair-learnings.md` records scope, verification, advisor/Cato review, and deferred items.
- ISC-32: Bash — `git diff --name-only` lists documentation/plan artifacts only; `git diff --name-only -- "*.cs"` returned no files.

## Changelog

- 2026-06-06 — conjectured: a focused docs pass could repair high-confidence stale guidance without replacing whole module docs; refuted by: advisor/Cato showed PIV and Tests.Shared active docs were so stale that concise replacement was safer than piecemeal edits; learned: documentation repair should choose the smallest source-backed artifact shape, which can be replacement when stale content dominates; criterion now: active docs with dominant stale API guidance may be replaced if grep/build/Cato verify the new claims.
