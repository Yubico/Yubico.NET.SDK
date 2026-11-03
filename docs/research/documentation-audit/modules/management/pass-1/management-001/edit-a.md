# Management edit A — session contracts and test explanations

## Status and boundaries

- Editor: fresh GPT-6.1 Sol (`github-copilot/gpt-6.1-sol`), 2026-11-03.
- Worktree: `/tmp/opencode/management-001-edit-a`; verified baseline
  `da61d2e6d754b4c155cbb2579202e910986f4b3a`; tracked tree initially clean.
- Status: accepted documentation changes applied locally, ready for coordinator integration and fresh
  independent review. This is not a review, build, API-compatibility, test, or hardware completion claim.
- Read root, Management, and Management-test instructions, module README, `docs/TESTING.md`, primary
  campaign workflow/style, plan, triage, and investigations A/B. Read all 21 assigned C# files completely.
  Narrow dependency checks included Core instructions, `SessionCreationOptions`, factory validation,
  initialization/SCP ordering, feature checks, and `DeviceConfig.Validate`.
- Consulted execution-plan, security-guidelines, and verification skills. No builds, tests, hardware
  operations, commits, delegation, formatting tools, executable edits, or edits to Core/shared files.
  Build/test/commit skills were not applicable because those actions were expressly prohibited.
- Source edits used `patch`; this report is the only write in the primary checkout. All other primary
  work remains the coordinator's or another owner's responsibility.

## Exact changed files and candidate mapping

Paths below are repository-relative. Five production files and nine test files changed; seven other
assigned files were read and retained unchanged.

| Changed file | Accepted IDs / candidates | Change |
| --- | --- | --- |
| `src/Management/src/IManagementSession.cs` | MGMT-DOC-001/002/006/007; A-01/02/03/09, B-01/02/05/07 | Complete type/read/write XML; omission/replacement, per-command lock authorization and removal, borrowed-code lifetime, actual firmware enforcement versus supported policy, overlap/cancellation/error boundaries, precise Bio MPE reset and F9 retention. |
| `src/Management/src/ManagementSession.cs` | MGMT-DOC-006; A-01/03/09 | Complete type/direct-factory XML; borrowed connection on success/failure, one-session guard, initialized return, SCP/metadata ordering, override versus reported metadata, unused prompt/borrowed SCP parameters, inherited operation XML. |
| `src/Management/src/IYubiKeyExtensions.cs` | MGMT-DOC-002/006; A-01/03, B-02/07 | Replace generic prose with one-shot versus reusable-session lifetimes, owned hidden connection, one transport/no failure fallback, options/SCP/override and operation references, secret lifetime through creation awaits, meaningful error families. |
| `src/Management/src/Backend/IManagementBackend.cs` | MGMT-DOC-009; A-11 | Specify one-byte body length followed by TLVs and borrowed payload lifetime. |
| `src/Management/src/Backend/OtpBackend.cs` | MGMT-DOC-009; A-11 | Attribute framing/outbound CRC to Core; remove backend inbound-CRC assertion and distinguish device-information reads. |
| `src/Management/tests/Yubico.YubiKit.Management.IntegrationTests/ManagementLockCodeTests.cs` | MGMT-DOC-008; A-06/07 | Attempted restoration, cached starting state/default selection, broad rejection assertion, no second-test restoration readback, and zeroing skipped by earlier cleanup failure. |
| `src/Management/tests/Yubico.YubiKit.Management.IntegrationTests/ManagementSessionAdvancedTests.cs` | MGMT-DOC-008/009/010; A-04/05/06/12/13/14 | Actual attribute/row binding/cache behavior, SCP03-only case, Bio/firmware and property assertion scope, FIPS decoded flags rather than certification, default versus pinned sessions, rig-specific latency threshold, no same-physical-key guarantee across rows. |
| `src/Management/tests/Yubico.YubiKit.Management.IntegrationTests/ManagementSessionCapabilityTests.cs` | MGMT-DOC-003/008; A-07/08, B-03/04 | Cached starting values, persisted-bit versus active-application behavior, restoration attempts/readback distinction, NFC-capable 5.7+ applicability, NDEF exception, USB-power activation, no invented touch/reboot/false-write guarantee. |
| `src/Management/tests/Yubico.YubiKit.Management.IntegrationTests/ManagementSessionSimpleTests.cs` | MGMT-DOC-008; A-05/06/07 | FIDO versus OTP HID support, unpinned one-shot coverage, timeout readback versus unprotected/unverified restoration, broad wrong-SCP assertion, accurate AuthorizedDevices loop scope. |
| `src/Management/tests/Yubico.YubiKit.Management.IntegrationTests/ManagementSessionTests.cs` | MGMT-DOC-006/008/009/010; A-04/06/09/10/12/14 | One independently bound row per attribute, supported read minimum not preflight proof, decoded-form-factor limitation, Bio metadata rather than biometric coverage, remove placeholder, explain ineffective two-component firmware filter without changing it. |
| `src/Management/tests/Yubico.YubiKit.Management.IntegrationTests/ReconfigurationDiscoveryInvariantTests.cs` | MGMT-DOC-008; A-06 | Explicit reboot actor/option, completed-scan bounds rather than guaranteed observation, preserve timing-independent contested-serial rationale, restoration attempt and actual readback, between-await deadline. Remove uncited historical scan counts/timing rather than presenting them as fresh evidence. |
| `src/Management/tests/Yubico.YubiKit.Management.UnitTests/AuditV2/ManagementAuditReproTests.cs` | MGMT-DOC-005/009; A-12, B-05/08 | Present immutable-record/USB-None/range/packet assertions, retain YESDK-1634 provenance, distinguish pinned sibling order from a protocol ordering requirement. |
| `src/Management/tests/Yubico.YubiKit.Management.UnitTests/IYubiKeyExtensionsTransportTests.cs` | MGMT-DOC-009; A-12 | Remove phase/lettered development narration; explain exposed-interface selection versus no retry after refusal, preserve fake-probe and error-code rationale. |
| `src/Management/tests/Yubico.YubiKit.Management.UnitTests/ManagementSessionTests.cs` | MGMT-DOC-008/009; A-06/07/12 | Pre-cancelled versus mid-exchange scope, successful fake-write-only zeroing proof and remaining cases, exception-type disposal assertion without obsolete eight-session history. |

Assigned files retained unchanged after complete reading:

- `src/Management/src/Backend/SmartCardBackend.cs`
- `src/Management/src/Backend/FidoHidBackend.cs`
- `src/Management/tests/Yubico.YubiKit.Management.UnitTests/FirmwareVersionTests.cs`
- `src/Management/tests/Yubico.YubiKit.Management.UnitTests/ManagementBackendLifecycleTests.cs`
- `src/Management/tests/Yubico.YubiKit.Management.UnitTests/ManagementSessionExchangeOverlapTests.cs`
- `src/Management/tests/Yubico.YubiKit.Management.IntegrationTests/IntegrationTestBase.cs`
- `src/Management/tests/Yubico.YubiKit.Management.IntegrationTests/ManagementHidConcurrencyTests.cs`

Their present explanatory units remain consistent with the accepted narrow dispositions in
`investigation-a.md`; retaining them does not establish hardware results or fill the deferred gaps.

## Examples, references, and retained rationale

- The sole owned XML `<code>` block is `IManagementSession.ResetDeviceAsync`: a **composable fragment**,
  not a complete program. Prerequisites are explicitly a discovered Bio MPE `IYubiKey device` and
  `CancellationToken cancellationToken`; supplied usings cover the Management/session/connection types.
  It pins SmartCard, uses `await using`, and warns about FIDO/PIV/shared-PIN destruction. Only its prose
  and warning comment changed. Compile-only verification is for the coordinator; no compilation occurred here.
- Executable tests remain tests, not newly authored documentation snippets. All test names, traits,
  attributes, strings, calls, assertions, helpers, and directives remain unchanged.
- Reset scope follows Astra's adjudicated original official R2/R3 fetches recorded in triage and
  investigation A: Bio MPE simultaneous FIDO/PIV/shared-PIN reset with retained F9 attestation slot.
  This editor did not repeat those fetches or infer Management lock/configuration erasure or lifecycle policy.
- The pinned `management.py` revision `4ca60f7` remains sibling-implementation evidence for local order,
  not firmware authority. No NFC-tag typo or protocol-order mandate was copied.
- Context7 resolved `/dotnet/docs` and queried asynchronous `Memory<T>` leases. Returned original:
  <https://github.com/dotnet/docs/blob/main/docs/standard/memory-and-spans/memory-t-usage-guidelines.md>,
  Rule 4: a task-returning consumer must not use memory after completed/faulted/cancelled terminal state;
  owners retain lifetime/destruction responsibility. This corroborates buffer-lifetime prose, not SDK
  transport-copy zeroing. Access date: 2026-11-03; living Microsoft guidance, not pinned firmware evidence.
- Preserved useful disposal-before-feature-gate, connection-ownership, framing, fake-wire, contested-serial,
  and test-issue rationale. Removed only stale history/unsupported claims or replaced them with present bounds.

## Deferred dependencies and unassigned follow-through

No runtime finding was fixed. Re-review these edited explanations when the corresponding decisions or
implementations change:

- MGMT-IMP-001/002/003: field-specific firmware/device gates, NFC false-byte behavior, and mask width/support
  validation. Reopen session validation XML and capability/audit test explanations; B owns the model/options XML.
- MGMT-IMP-004/005/006/007: failure-safe restoration/secret cleanup, failure/cancellation encoded-buffer
  coverage, actual transport pins, and precise error assertions. Reopen simple/lock/capability/unit comments.
- MGMT-IMP-008/009: read-info preflight/initialization policy and malformed firmware criteria/harness handling.
  Reopen read/factory XML and integration-filter explanations. The attribute still contains `"5.0"` by design
  for this docs-only edit; its executable correction is deferred, not silently included.
- MGMT-IMP-010/011/012: reset product/lifecycle/configuration-erasure decisions, genuine biometric/metadata
  operation coverage, and dormant internal SetMode wire intent. No family gate, post-reset invalidation,
  biometric test, or legacy operation changed. The removed Bio placeholder's real coverage gap remains here
  and in the canonical ledger.
- MGMT-XREF-001/002: Core FIPS mapping/width and flagged form-factor decoding. Comments qualify reliance on
  decoded metadata without declaring the wrong decoding correct or claiming certification. No Core edits.
- MGMT-XREF-003 and C-owned docs: shared helper/attribute/Tests.Shared explanations, Management README/CLAUDE,
  test guidance, migration, and ManagementTool must match these lifetime/filter/reset/cleanup contracts.
  These corrections are necessary integration dependencies but outside A's edit ownership; C/coordinator
  own them. No additional unassigned source correction was attempted.

## Local verification evidence

All checks below ran against the final source edits in `/tmp/opencode/management-001-edit-a` on 2026-11-03.

1. `git diff --check` — exit **0**, no output. Repeated after the last source patch.
2. `git status --short` — exactly the 14 modified C# paths listed above; no other worktree writes.
3. Inline Python static comparison: compare each changed file with `git show HEAD:<path>`, ignoring
   comments/whitespace while preserving quoted string/character contents; parse every XML-doc block as
   an XML fragment with `xml.etree.ElementTree`. Exit **0**; final output:

   ```text
   14 changed C# files: non-comment units unchanged (strings retained); XML comment blocks well-formed.
   Static text comparison only; not Roslyn compilation, API validation, or test execution.
   ```

The lightweight comparison is supporting evidence, not the coordinator's canonical Roslyn token/API gate.
`patch` also supplies final newlines where edited files previously lacked them; these are whitespace-only,
not executable or directive changes. Compiler-resolved XML references, scoped formatting, full repository
gates, snippet compilation, integration, and fresh independent review remain **not run by this editor**.
