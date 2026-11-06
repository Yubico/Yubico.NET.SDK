# Audit v2 close-out execution

Started 2026-10-31 from `origin/yubikit` at `ddf3cdc24984f598678b48f2a0f98d6be1a58bf9`.

**Delivery:** all 13 approved implementation/close-out tasks have focused PRs. No production PR was merged and no Jira case was changed. Individual branch checks passed; the combined candidate built warning-free but exposed one unchanged, input-dependent RSA test failure (details below), so this is not an unconditional audit sign-off.

Approved scope and decisions: [close-out review](audit-v2-closeout-review.md). Original issue/commit mapping: [traceability](audit-v2-jira-traceability.md).

## Delivery status

One GPT-6.1 Sol high-effort agent per task; isolated worktrees under `/tmp/opencode/audit-closeout/`. Orchestrator reviews and submits PRs after verification. No Jira mutations or merges are implied by implementation approval.

| Task | Finding / tracking | Branch suffix (`yubikit-audit-…`) | Status |
| --- | --- | --- | --- |
| OATH secret ownership | YESDK-1658 / 26o | oath-secret | `6ca266db`; 11 red /18 green, full3514; reviewed; [PR #726](https://github.com/Yubico/Yubico.NET.SDK/pull/726) |
| PC/SC reader-name cleanup | YESDK-1626 / #18 | pcsc-reader-cleanup | `013f3587`; 4 red /4 green, full3500, resilience91; reviewed; [PR #731](https://github.com/Yubico/Yubico.NET.SDK/pull/731) |
| Strict exclusive default | YESDK-1630 / #22 | pcsc-exclusive | `eef39969`; 6 red /22 green, full3511, resilience91; reviewed; [PR #725](https://github.com/Yubico/Yubico.NET.SDK/pull/725) |
| Device-info value lengths | YESDK-1629 / #21 | deviceinfo-lengths | `331d3dc1`; 64 red /94 green, full3564; scoped review passed with pre-existing mapping residuals; [PR #732](https://github.com/Yubico/Yubico.NET.SDK/pull/732) |
| Developer RP validator | YESDK-1623 / #15 | rp-validator | Final `e57a78df`; revised callback-owned RP validity approved;36 green/full3526; re-review passed; [PR #737](https://github.com/Yubico/Yubico.NET.SDK/pull/737) |
| Registration transports | YESDK-1608 / consideration 4 | registration-transports | Rebased `a026a1f6` + integration `aba4a0ec`;44 combined policy/transport tests and full3535 pass; [PR #733](https://github.com/Yubico/Yubico.NET.SDK/pull/733) now on #737, stack #738 |
| OpenPGP certificate errors | YESDK-1608 / consideration 2 | openpgp-cert-errors | `71e36985`; 5 red /22 green, full3518; reviewed; [PR #734](https://github.com/Yubico/Yubico.NET.SDK/pull/734) |
| Partial certificate bundles | YESDK-1608 / consideration 5 | certificate-bundle | `d1559f7d`; partial native handle cleanup red,2 green,full3498; reviewed; [PR #727](https://github.com/Yubico/Yubico.NET.SDK/pull/727) |
| OATH ordering | YESDK-1608 / consideration 7 | oath-ordering | `a65af969`; revised identity ordering approved;13 red /22 green,full3508; reviewed; [PR #728](https://github.com/Yubico/Yubico.NET.SDK/pull/728) |
| Migration CI permissions | YESDK-1608 / N4 | migration-ci-split | `5208116e`; actionlint/YAML/shell checks pass; reviewed; [PR #724](https://github.com/Yubico/Yubico.NET.SDK/pull/724) targets yubikit |
| Generated patch modes | YESDK-1608 / #19–N4 follow-up | docs-patch-validation | `41c4a073` + `1195e506`;51 combinations pass; reviewed; [PR #729](https://github.com/Yubico/Yubico.NET.SDK/pull/729) on #724, linked stack #730 |
| Factory-reset docs | YESDK-1608 / consideration 6 | factory-reset-docs | `a330bbc9`; build/docs checks pass; review precision note addressed; [PR #735](https://github.com/Yubico/Yubico.NET.SDK/pull/735) |
| HMAC challenge semantics | YESDK-1659 / 26p | hmac-closeout | `cecc4d2d`;8 baseline regressions pass, mutations3/1/6 fail, full3504; reviewed; [PR #736](https://github.com/Yubico/Yubico.NET.SDK/pull/736); fresh Beta103 hardware2 tests/12 vectors passed |

Independent PRs target `yubikit`. CI patch validation depends on CI split (#724→#729, stack #730). Combined verification identified a second concrete dependency: registration-transports tests must use the breaking RP-validator API, so transport PR #733 was rebased onto #737 (stack #738) and its fixture updated. Unrelated audit changes remain separate. Each fix includes its own tests/docs; CI split and documentation-only work retain their approved no-new-test exceptions.

Local integration verification branch/worktree: `yubikit-audit-integration-check` at `/home/rupicola/Code/y/yubikit-audit-closeout-integration`. Reviewed implementation commits are combined there to detect cross-PR interactions; it is not a combined feature PR and does not modify trunk.

## Unattended hardware results

On the local combined candidate `a3f85f76` (reviewed branches except the pending RP-validator and not-yet-added test-only HMAC commit):

- Management device-info checks: **5 executed, 0 failed, 0 skipped**, 0 warnings; `artifacts/toolchain/runs/20261031-141938-463-test/` in the integration worktree. Covered Beta103 over SmartCard, HID FIDO and HID OTP.
- OATH PUT/list/calculate/delete plus SHA-256/SHA-512 credential checks: **3 executed, 0 failed, 0 skipped**, 0 warnings; `artifacts/toolchain/runs/20261031-142000-470-test/`. Existing reset-before-use helpers ran on the authorized bound device; no human interaction was requested.
- **Coverage limitation:** Management discovery found all three keys but excluded Alpha125 and 5.4.3/9681620 with `UnrecoveredConnectionException` on HID interface initialization; only Beta103 was bound in that run. The subsequent OATH run authorized both Alpha125 and Beta103, still excluding 9681620. Placeholder-based OATH results do not establish a full per-key matrix. Passing counts do not imply all three devices were exercised; pre-test exclusions are recorded despite zero skipped test cases.
- OATH auth-retry seed bytes are proven by the new wire-level unit regression; the hardware checks above do not themselves exercise locked authentication retry.

26p final hardware check: **2 non-touch tests / 12 short/fixed HMAC vectors passed** on Beta103, firmware5.8.0, OTP HID; zero failures/skips/warnings. Serial was checked via fixture and applet before writes. Slot2 initially empty; two configurations and12 calculations were followed by two deletions, each verified unconfigured. No slot1, reset, touch or UV operations. All temporary fixture/config guards restored; `cecc4d2d` unchanged. Complete output: `/tmp/opencode/audit-closeout/hmac-verification/20261031-142259-863-test/01-test-YubiOtp.IntegrationTests.log`. This supersedes the earlier HMAC hardware deferral; other-device discovery limitations remain.

## Execution constraints

- All .NET work is serialized with `flock --close /tmp/opencode/audit-closeout-dotnet.lock` to avoid concurrent compiler/test saturation. `--close` prevents persistent compiler-server descendants from retaining the lock. Use repository build/test skills and inspect RESULT blocks.
- Production fixes require recorded failing regression evidence before implementation and targeted green results afterward. Full required pre-PR checks still apply; no empty-match or skipped test is a pass.
- Existing research documents and the untracked primary-tree `mise.toml` are preserved; implementation agents do not edit the primary checkout or Obsidian vault.
- Hardware access is assigned by the orchestrator, with no concurrent use of a device. All three connected devices were discovered by `ykman list` and their serials are in `src/Tests.Shared/appsettings.json`: **125** (5.8.0.alpha.2), **103** (5.8.0.beta.0), **9681620** (5.4.3).
- The user authorized state-changing/destructive operations on those test keys. No human touch, presence, interactive verification or insert/remove sequence is available; those tests remain explicitly pending. No hardware test run is claimed by discovery alone.

## Final reconciliation still required

### Combined candidate verification

Candidate `85154611` on local `yubikit-audit-integration-check` contains all approved changes. Initial combined compilation identified the new transport fixture's old PublicSuffixChecker setup; the transport agent adapted it to the required exact-origin/RP callback and PR #733 was stacked on #737.

- Clean full-solution build: **passed, 0 warnings**.
- Combined non-hardware unit run: **3,684 executed, 1 failed, 5 existing skips**. Failure: unchanged `PrivateKeyConsistencyTests.Rsa_PerturbedPrivateComponent_IsRejected("DP")` expected CryptographicException but received `NotSupportedException: Unsupported RSA length: 2032`. Both test and production crypto files are byte-unchanged from baseline. `AddOne` emits minimal-width DP bytes; the existing RSAPrivateKey constructor derives key size from DP byte length before consistency checking. Preserve as a separate audit #27 / YESDK-1635 follow-up, not a silently repaired test.
- One focused rerun: **4 cases passed, 0 warnings**. This demonstrates input-dependent behavior, not a fix or a green combined-suite result. No assertion was weakened.
- Combined complexity: **41 changed methods inspected; 0 new/worse threshold violations** (three unchanged debt methods).
- Documentation QA: **73 active documents and 10 package readmes passed**, 0 warnings.
- Logs in integration worktree: `20261031-220021-895-build+test+complexity+docs-qa`, `20261031-220257-799-complexity+docs-qa`, `20261031-220314-206-test` under `artifacts/toolchain/runs/`. Copies retained in primary `artifacts/audit-closeout-evidence/integration/`.

### Remaining non-patch decisions and discovered residuals

Additional pre-existing defects identified in device-info review (not fixed by YESDK-1629's field-length PR):

- `CapabilityMapper.FromApp` reads only the first two bytes of wider capability values; `[00,00,10]` loses the PIV bit. Acceptance of variable widths is not proof of correct numeric interpretation.
- FIPS tags `0x14`/`0x15` use application-bit mapping instead of the separate FIPS mapping; `[01]` maps to OTP rather than FIDO2.

Both need separately scoped triage/tests before any claim that device-info decoding is exhaustively corrected. Do not silently expand the field-length PR or mark these repaired.

The original PC/SC PR CI failure was investigated without source changes: its unchanged macOS-listener fake test can observe cleanup after a bounded Stop/Dispose wait has expired. Six local executions passed and the CI retry passed; exact CI timing was not reproduced. Diagnosis retained under `pcsc-exclusive/artifacts/evidence/pcsc-exclusive/ci-diagnosis.md` in the archived evidence. Do not claim this timing risk fixed by exclusive-default changes.

Infrastructure correction: plain flock allowed the compiler server to inherit the build lock, blocking queued checks after the original command exited. Orchestrator confirmed the holder, terminated the audit queue and orphaned compiler, and instructed agents to requeue needed checks with `--close`. Cancelled queued commands are not failed test results. The queued primary baseline build was cancelled in that recovery and has no result yet.

Replacement primary baseline build completed successfully: `dotnet toolchain.cs -- build`, **0 warnings**, log `artifacts/toolchain/runs/20261031-135336-113-build/run.log`. This verifies the original trunk baseline, not the individual implementation branches.

GitHub verification follow-up: PR #724 dependency submission failed restoring NativeShims with HTTP401/NU1301; its build/AOT checks passed. PR #725 had one failing Build run in the existing `MacOSHidListenerLifetimeTests.ConcurrentDispose_WaitsForSameCallbackDrainAsStop`, while its other build and all AOT checks passed. Both failed runs were rerun once; the exclusive-default agent is investigating the latter rather than attributing it to the patch without evidence. No credential configuration or test suppression was changed.

PR #724 dependency-submission rerun succeeded (`41203562837`, job `124547117974`), with restore and snapshot submission complete. No credential or source change was made; the original authentication failure is retained as history, not an outstanding failure on that rerun.

PR #725 Build rerun also succeeded (`41203963928`, job `124547115974`): build, unit tests, resilience and package steps passed without source/test changes. Original listener-lifetime failure remains in the record; a successful retry alone does not diagnose its cause.

Agent evidence copies are retained on disk under the primary checkout's ignored `artifacts/audit-closeout-evidence/<task>/`, in addition to worktree logs. This protects red/green evidence from normal toolchain five-run retention and temporary-filesystem pressure.

GitHub checkpoint after final submissions: all **13 implementation PRs have completed checks without failures**. Rebased transport PR #733's final build/coverage checks completed successfully via the watcher. Status snapshot: `artifacts/audit-closeout-evidence/github-pr-status.json`; final transport output retained as `registration-transports-pr-checks.log` there. This is branch CI evidence, not a green result for the separate combined candidate's input-dependent RSA test, nor a promise about later CI or merge readiness.

- PRF UV-policy coordination and unsigned-assertion-output support clarification remain deferred; no speculative implementation.
- Runtime scalar-copy upstream search/report remains separately tracked, not an SDK workaround or implicit risk acceptance.
- Preserve the Rust/python-fido2 registration-transports handoff.
- Review 26p measurements last; do not invent a production change if the approved behavior is already implemented.
- Report actual commits/PRs, exact checks, pending hardware evidence and deferred decisions. Obtain approval for the precise Jira-update batch.
