# SHARED revision 2 — focused independent re-review

**PASS — R1–R6 are resolved for the full 26-path SHARED successor at the exact hashes in
[review-identities.json](review-identities.json).** No mandatory correction remains in this focused return.

Accepted product patch SHA-256:
`8e743d0336f0d0338d6a726f63ececcd37a3877cb7da6741626e169d8b99c56c`.
Focused correction patch SHA-256:
`a34a75042e69e0f0355402c6508b5aa47b3be26c14472af2e106e60aaefd94f9`.
Base remains `da66f5d01ba594993974ddd26723e8c300c0a454`; runtime source remains
`fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`.

This is the original independent reviewer's focused correction review, not another whole-guide audit.
It combines fresh acceptance of the seven corrected product paths with the original content acceptance
of nineteen byte-identical paths. It does not grant example compilation, runtime qualification,
whole-applet acceptance or Core Pass-1 publication. Only this review and its identity record were written.

## Correction decisions

| Review concern / original candidates | Successor check and disposition |
| --- | --- |
| **R1 — current examples**, CORE-B6-C001/FPT-067, CORE-C-C017/FPT-099, CORE-B4-C010/FPT-057 | **PASS.** Root `CLAUDE.md:503–524` supplies a complete Core-unit-test-context class, actual fake namespace, `new PcscProtocol(connection)`, synchronous using scopes in protocol-before-connection disposal order, and numerical **1390 complete-command** expectation. With extended support disabled, configuring 3.5.0 retains the initial NEO ceiling; it does not transmit. `docs/TESTING.md:549–567` now borrows a typed fake `IYubiKey`, owns/disposes the returned `ISmartCardConnection`, propagates the token and checks its actual `Type`. It invents no IsConnected member or device-health proof. `PcscProtocol.cs:44–70,162–195`, `SmartCardMaxApduSizes.cs:17–34`, fake declaration/body `:19–72`, `IYubiKey.cs:195–196`, `ISmartCardConnection.cs:19–72` and `IConnection.cs:26–29` support these shapes. Both examples remain **uncompiled**. |
| **R2 — grouped acquisition**, CORE-B1-C012/FPT-033; related FPT-026/048 | **PASS.** `src/Core/CLAUDE.md:915` now describes sorted/deduplicated incremental claims, earlier-discovery wait before a later held member, cancellation precedence, before-native-open refusal and reverse rollback. It explicitly rejects whole-set atomic preflight/invocation-time refusal and preserves route-specific native-release limits. This matches `DeviceConnectionRegistry.cs:94–124,193–245`; no stronger admission policy or Windows/custom release proof is introduced. |
| **R3 — PIN-peer profile**, CORE-C-C015/FPT-097; CORE-CTAP-X006 / ctap2-001:CTAP-IMP-007 | **PASS.** `src/Core/CLAUDE.md:116–123` separates required profile from current coordinate-byte/32-byte checks and provider P-256 import. It names omitted required kty/crv/alg and optional-label checks as a validation gap, not intended forward compatibility. `PinUvAuthHelpers.cs:58–117` supports this; generating correct platform labels is not peer validation. Retained CTAP **2.1 Proposed Standard**, 15 June 2021, **21 June 2022 errata edition**, §6.5.6 ecdh/getPublicKey and §6.5.7 inheritance (`ctap21.txt:8227–8230,8272–8292,8321–8325`) remains protocol authority, not SDK enforcement evidence. No authenticator token-acceptance claim is added. |
| **R4 — late work/provider lifetime**, FPT-008/023/090/080 | **PASS.** `docs/usage/device-discovery.md:45` now promises no caller-owned monitor/start-stop lifetime, not no background cleanup; its native/late-work qualifications remain at `:133–138`. `docs/LOGGING.md:170–192` borrows `ILoggerFactory` without disposing it and requires an outer application/provider lifetime covering late enumeration and later captured-static-logger uses. Global restoration is explicitly not provider drain or logger replacement. `FindYubiKeys.cs:43,230–271` and `YubiKitLogging.cs:38–39,52–66` support these distinctions. The complete helper retains scoped restoration on failure without prematurely ending the borrowed factory lifetime. |
| **R5 — original CBOR byte**, CORE-B5-C002/FPT-059 | **PASS.** `docs/architecture/connection-ownership-and-contention.md:193–205` and `docs/architecture/raw-access-tiers.md:107–114` now state the exact **original 0x10 plus leading result 0x00** late-success condition. Encoded **0x90** can correlate but miss classification and surface cancellation; this is named a current normalization gap. `RawCtapHidSession.cs:64–74` forwards unchanged and `FidoHidProtocol.cs:290–310,384–410` separates exact success classification from masked correlation. No-response CANCEL, original-response drain and indefinite silence remain qualified. Retained CTAP **2.3 Proposed Standard**, 26 February 2026, §11.2.9.1.5 (`ctap23.txt:13960–13969`) assigns the canceled-request response and no-CANCEL-response duties to the authenticator; the SDK race policy is not presented as that normative rule. |
| **R6 — complete/mixed example scope**, FPT-099/081/066 | **PASS.** Original body **65** is now `compilable-method-unchecked`, with typed borrowed raw session, request/response cleanup and illustrative selected-applet/command prerequisite retained. Original body **118** is now mixed historical Before/current After, with exact separately hashed segments; only Before is excluded, while After remains compilation-eligible with supplied connection/AID/token and owned raw-session scope. Neither complete body changed or was replaced by extracted matching lines. New complete R1/R4 bodies are also classified as unchecked, not compiler-verified. |

The source-evidence record's **13 files and every stored source range** were independently compared
against actual files, range text/hashes and, for product code, the pinned Git source. The relevant
implementation and original CTAP passages support the decisions above; no source revision/status,
actor, normative strength or API binding is inferred from an editor assertion alone.

## Exact preservation and static checks

Independent read-only checks, rerun for this successor, found:

- Exactly **7** product paths changed from the first reviewed subject; they are exactly the seven
  correction-required paths. All **19** previously content-reviewed changed paths remain byte-identical.
- All **17** original editor/reviewer/report preservation entries match their hashes, including the
  original review, identity record and patch. No historical receipt was rewritten to describe successor bytes.
- All **33** path dispositions' current/base/source-pin hashes match; the population remains
  **26 edit / 7 retain**. The seven retains acquire no new whole-file certification.
- The full successor patch is byte-identical to the actual base-to-current product diff and matches
  the requested hash. Both successor and correction patches pass read-only reverse-application checks.
- All **193** current fence bodies were independently re-extracted from actual Markdown; full text,
  opening/closing/body coordinates and SHA-256 match. The five focused example decisions join to
  the original bodies. Body 65 remains
  `9642d27320b61b9da546aee4260d55f32829d95c80f5e3677ae7b2c6357d3119`;
  full mixed body 118 remains
  `048e4a43422e260a9df70e5e85b7e1576cb644c625850265b5811e1ca4e311d5`.
  Both Before/After sub-body coordinate/hash receipts match their unchanged full body.
- Current classifications are **58 non-C# illustrations, 127 composable fragments, 1 complete unchecked
  test class, 5 complete unchecked methods, 1 mixed historical/current body and 1 historical body**.
  These are scope dispositions, not compilation or execution credit.
- `git diff --check`, successor reverse check and correction reverse check each exit **0**.
  HEAD remains at the editing base and the Git index is unchanged. No unclosed fence was found.

No additional tooling file, product edit, editor-record edit, build/test/compiler/formatter,
native/device/cryptographic operation, Git mutation or subagent action was performed.

## Acceptance limit and handoff

All 26 successor path hashes are content-accepted for the reviewed SHARED edit scope. Original
candidate identities, held facets, source/profile qualifications and outside-owner referrals remain
unchanged. This PASS does not make known defects intended policy, run held experiments, or close
provider/native erasure, producer/ABI, physical release, cross-process PIN-state or device/SCP gaps.
The future Windows concurrency delta and PIV physical deferrals retain their existing status.

Meaningful current-example/compiler binding and final boundary cross-checks, sampling, coordinator
validation and publication remain coordinator gates. Finish Core Pass 1 and the precise deferred-owner
handoff, then stop; no new audit, tooling programme or Pass-2 project is started here.
