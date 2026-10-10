# Audit v2 verification: common brief for all agents

You are verifying findings from a security audit of the Yubico .NET SDK v2 ("yubikit" branch).
The audit document is at `docs/plans/Audit NET SDK v2.0 Pre-Release.md` inside your worktree
(markdown table; some cells are very long, read with python or grep if the Read tool truncates at 2000 chars:
`python3 -c "l=open('docs/plans/Audit NET SDK v2.0 Pre-Release.md').read().splitlines(); print(l[N])"` where N = 51 + finding number).

The audit was done at commit 65e36386. Your worktree is at yubikit HEAD fdfcd6fd (10 commits later, includes
breaking refactors "webauthn: move suffix checker into required client options" and "applets: clarify public return contracts").
Line numbers in the audit may have drifted. Report the HEAD location.

## Your job, per finding

1. Re-trace the claimed source-to-sink flow in code at HEAD. Read the actual code; do not trust the audit text.
2. Find the governing normative text and quote it verbatim with section number. Local plain-text spec copies (grep them):
   - CTAP 2.3 PS: specs/ctap23.txt
   - WebAuthn L3: specs/webauthn3.txt
   - RFC 9580 (OpenPGP): specs/rfc9580.txt
   - YKOATH protocol: specs/ykoath.txt
   - Yubico PIV extensions: specs/piv-yubico.txt
   - Others (NIST SP 800-73-5, GlobalPlatform SCP03 Amd D / SCP11 Amd F, PC/SC, RFC 8032, RFC 5869): fetch with webfetch/exa if needed.
   If a finding has no normative spec (e.g. memory hygiene, API footgun), cite the SDK's own contract instead:
   XML doc comments, README/CLAUDE.md in the module, docs/, and repo security guidelines (CLAUDE.md "Security" sections).
3. Compare with canonical reference implementations (local clones, read-only):
   - python-fido2 (fido2/ctap2/*.py, fido2/client/*.py) - Yubico's reference FIDO/WebAuthn client
   - libfido2/src
   - yubikey-manager (yubikit/*.py: piv.py, oath.py, openpgp.py, management.py, securitydomain.py, yubiotp.py)
   - yubikit-android, yubikit-swift
   - Legacy .NET SDK v1: Yubico.NET.SDK-Legacy
   Report how the reference behaves (file:line) - this is strong evidence for what "correct" means.
4. Reproduce with a failing unit test (no hardware) wherever possible. Put repro tests in the module's existing
   UnitTests project, in a new file named `AuditV2/<Module>AuditReproTests.cs` (or similar, one file per finding group),
   each test tagged `[Trait("Audit", "YESDK-16xx")]`. Tests must assert the CORRECT (spec) behaviour so that they FAIL
   today and will PASS once fixed. Name them `YESDK16xx_<ShortDescription>`.
   Look at nearby existing unit tests for conventions (xUnit v3, test helpers, RecordingSmartCardConnection,
   fake backends). Keep tests small and precise.
5. Hardware repro: if a finding genuinely needs a YubiKey, WRITE the integration test in the module's
   IntegrationTests project (file `AuditV2/...`) using `[Theory] [WithYubiKey(...)]`, tag `[Trait("Audit","YESDK-16xx")]`,
   and add `[Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]` if it needs touch/PIN entry by a human.
   Build it, but DO NOT RUN IT. Other agents share the same physical YubiKeys; the orchestrator runs all hardware
   tests serially afterwards. Give the exact filter command to run it.
   Allow-listed devices currently plugged in: 5.7.4 and 5.8.0 test keys. FIDO2 reset is approved but only in the
   final user-present session; don't depend on it unless necessary and say so.
6. Verdict, one of: CONFIRMED | CONFIRMED-WITH-CORRECTION (explain what the audit got wrong) | PARTIAL |
   NOT-REPRODUCED | ALREADY-FIXED-AT-HEAD | BY-DESIGN (then say what doc change is needed).
   Also give your own severity opinion vs the audit's.
7. Remediation plan: concrete fix (files, approach, a short code sketch if helpful), public API impact
   (breaking or not), the test that proves the fix, spec clause satisfied, dependencies on other findings,
   and open design decisions that need a human (list options, recommend one). DO NOT implement the fix.

## Rules

- Build/test only via the toolchain, never `dotnet build`/`dotnet test` directly:
  - `dotnet toolchain.cs -- build --project <Name>`
  - `dotnet toolchain.cs -- test --project <Name> --filter "FullyQualifiedName~AuditV2"`
  - Judge results by the per-project `total:` / `failed:` lines, not the final summary line (it counts projects).
  - Unit test filters: exclude hardware: `Category!=RequiresHardware&Category!=RequiresUserPresence`.
- Work ONLY inside your assigned worktree. Do not touch other worktrees or the main checkout.
- Do not commit. Do not modify production code (src/*/src). Only add test files (and csproj edits only if strictly needed).
- Warnings are errors in this repo; make your tests compile cleanly.
- Never log or print secrets. Tests may use known test keys/PINs.
- If a finding is wrong, say so plainly. Accuracy over agreement with the auditor.

## Output

Write your full report to `<scratch>/<AGENT-LETTER>.md`
and also return it as your final message. Per finding use this template:

```
### #N <title> (YESDK-16xx) - audit severity X
- Verdict: ...   | My severity: ...
- Location at HEAD: path:line (+ drift notes vs audit)
- Spec / contract: "<verbatim quote>" — <doc> §x.y
- Reference implementations: python-fido2 file:line does ..., libfido2 ..., ykman ...
- Evidence: short trace of the code path
- Repro: test file:TestName — result (FAILED as expected / passed = not reproduced) + one-line failure message
- Hardware repro (if any): test name, needs touch? y/n, command
- Remediation: ...
- API impact: none | additive | breaking (what)
- Proving test: ...
- Depends on / interacts with: #...
- Open decisions: ...
```
End with: list of files you added, and the exact commands to run your unit repros and (separately) hardware repros.
