# Management edit B — configuration model and write options

## Scope and status

- Editor: Sol 6.1 (`github-copilot/gpt-6.1-sol`), 2026-11-03.
- Source worktree: `/tmp/opencode/management-001-edit-b`, baseline `da61d2e6`.
- Read the worktree root/module `CLAUDE.md` and module README, the primary checkout's current
  `workflow.md` and `records/style.md`, and this run's `plan.md`, `triage.md`, and `investigation-b.md`.
  Reviewed available skills; loaded `workflow-execute` and `workflow-verify`. Build/test/commit skills
  were not applicable because those operations were expressly excluded from this editor assignment.
- Source edits are documentation-only and ready for fresh independent review, not review-approved or
  integrated. No changes were copied into the primary checkout's source tree.
- No builds, tests, compiler checks, hardware operations, commits or delegation were performed. The
  coordinator owns integration, compiler/token/API validation and assignment of a fresh reviewer.

## Exact changed files

Only these source files were changed, both in the editor worktree:

1. `/tmp/opencode/management-001-edit-b/src/Management/src/DeviceConfig.cs`
2. `/tmp/opencode/management-001-edit-b/src/Management/src/SetDeviceConfigOptions.cs`

The only authored primary-checkout artifact is this report:
`docs/research/documentation-audit/modules/management/pass-1/management-001/edit-b.md`.

The source diff contains XML documentation changes and a final newline added to `DeviceConfig.cs`.
Manual full-diff inspection found no changed executable tokens, imports, directives, declarations,
constants, API shapes or example code. Mechanical compiler/token/API checks remain not-run here.
The existing copyright/license headers, tag values, length guard, setters, encoder order and cleanup
comment were preserved.

## Finding-to-change mapping

| Accepted ID | Edited units | Semantic result |
| --- | --- | --- |
| MGMT-DOC-001 (B-01/B-05) | `DeviceConfig` remarks, `UsbEnabled`, `NfcEnabled`, `DeviceFlags`, `Validate` | Non-null application masks and flags replace the whole set; cleared bits disable settings and null preserves them. NFC `None` requests all NFC application bits cleared. USB `None` is the actual validation rejection, not proof of device-aware supported-mask enforcement. Caller responsibility to preserve a supported USB application is explicit. |
| MGMT-DOC-002 (B-02/B-07) | All `SetDeviceConfigOptions` XML | Current code authorizes each protected command and neither removes the persistent lock nor unlocks a session. Null new code preserves the lock; a 16-byte value not all zero sets/replaces it; sixteen zero bytes remove it, with current-code authorization in the same operation when locked. Present empty/wrong-length buffers are rejected by the session, not treated as omission/removal. Reboot is an optional request contingent on device acceptance, not factory reset. |
| MGMT-DOC-003 (B-03/B-04) | `NfcRestricted` | States NFC-capable firmware 5.7-or-later applicability, restricted NDEF URL access, three seconds of USB power as recovery, and absence of a field-specific SDK preflight check. Null omits the setting; false sends an explicit zero byte. No false-byte recovery, reboot timing or queued-restoration contract is inferred. |
| MGMT-DOC-004 (B-06) | Type validation remarks and both timeout properties | Auto-eject is an inactivity timeout in CCID-only/TouchEject mode; assigning it does not set TouchEject. Zero disables auto-eject and restores the 15-second challenge-response default, respectively; null preserves current values. Retains 3600/60 as SDK ceilings, not firmware maxima. Documents assignment-time `ArgumentOutOfRangeException`. |
| MGMT-DOC-005 (B-08, optional ownership improvement used) | Internal `GetBytes` XML | Preserves one-byte prefix and fixed local order, including reboot/unlock first when present, without asserting a protocol-mandated order. Adds returned-buffer ownership/clearing responsibility and intermediate-buffer cleanup. No tag changes or vendor-reference typo copied. |

## Important semantic details

- The type's existing derive-from-`DeviceInfo` example remains intact. Its surrounding prose now
  states the prerequisites: another supported USB application must remain enabled and configuration
  must not be locked. This preserves the useful selective-change example without presenting removal
  of the last USB application as safe.
- `Validate` is described as the exact USB-`None` check. Its remarks distinguish null acceptance from
  device-support, mask-width and firmware validation. Setter range checks are described separately.
- `NfcEnabled` and `NfcRestricted` are separate controls. USB power removes the temporary restriction;
  it does not change the enabled-application mask. Public XML contains actionable recovery guidance,
  not the audit history or an inferred meaning for the false byte.
- Options borrow caller buffers. Keep them valid and unchanged through the full operation, including
  one-shot session creation; clear owned sensitive input in `finally` after success, failure or
  cancellation. Options do not copy/clear source memory. A retained options object still references it.
  These claims do not promise cleanup of every transport-layer copy.
- The internal returned configuration buffer is distinguished from caller lock-code buffers. The
  session clears the former after the write; callers own the latter. Existing local TLV disposal
  rationale was retained.

## Evidence inspected

Read the complete two owned files, then checked these implementation paths directly in the worktree:

- `src/Management/src/ManagementSession.cs:122–160`: lengths are checked when applying options;
  encoding precedes transmission; the returned encoded buffer is cleared in `finally`.
- `src/Management/src/IYubiKeyExtensions.cs:72–81`: session creation is awaited before options are
  consumed by the configuration call, supporting the full-operation borrowed-buffer lifetime.
- `src/Core/src/Utilities/Tlv.cs:42–50,81–89,92–135`: TLV construction copies into owned encoded memory;
  disposal zeroes that buffer.
- `src/Core/src/Utilities/TlvHelper.cs:95–137`: concatenation produces caller-owned encoded memory
  without disposing input TLVs. `DeviceConfig.GetBytes` owns both intermediate cleanup steps and
  deliberately returns a separate final buffer.

The accepted device/lock/timeout claims use the retained primary passages and applicability notes in
`investigation-b.md` E1–E4; local order is implementation evidence, not specification conformance.
The editor did not newly qualify devices or re-fetch vendor originals. The independent reviewer must
recheck consequential claims against originals, especially the timeout-zero clauses as required by triage.

For .NET borrowed-memory lifetime, consulted Context7 `/dotnet/docs` (resolve plus focused query),
2026-11-03. It returned the Memory/Span usage guideline Rule 4: a task-returning consumer must stop
using memory after completion, fault or cancellation. Source:
<https://github.com/dotnet/docs/blob/main/docs/standard/memory-and-spans/memory-t-usage-guidelines.md>.
This supports the stated caller cleanup boundary; source inspection establishes this SDK's behavior.

## Excerpts and compiler-check handoff

- **One current composable fragment:** `DeviceConfig` type remarks, the unchanged three-line `<code>`
  block reading device info, removing OTP from its USB mask, and applying it with `Reboot = true`.
  It is not a standalone program or hardware test. A meaningful compile-only harness should provide
  an explicitly typed `IManagementSession session` inside an `async Task` method and the Management
  and Core.Devices namespaces. Runtime prerequisites are now stated in the surrounding prose; do not
  execute this fragment merely to compile it.
- **No new code blocks or application-dependent sketches** were introduced. Lock-removal and cleanup
  additions are contract prose, not snippets that claim to generate, store or recover a secret.
- **XML compiler check:** new `cref` targets include `DeviceInfo.UsbSupported`, `NfcSupported`, existing
  flag/validation members, `CurrentLockCode`, and fully qualified
  `System.Security.Cryptography.CryptographicOperations.ZeroMemory`. Resolve these on the integrated
  documentation build; no compiler success is claimed here.
- **Non-executable wire descriptions:** explicit false zero, the one-byte length prefix and local TLV
  ordering are explanatory statements, not standalone compiler examples or firmware qualification.

## Verification

Executed in `/tmp/opencode/management-001-edit-b` after the final source edit:

```text
git diff --check
```

Result: successful exit (0), no output. Also read the complete scoped `git diff` and `git diff --stat`:
two files, 100 insertions and 22 deletions. The initial worktree `git status --short` was empty.
The whitespace check does not establish XML compilation, executable-token/API equivalence or device
behavior. All such gates remain for coordinator validation.

## Unresolved questions and re-review triggers

- MGMT-IMP-001: field-specific firmware/NFC support policy is still deferred. Reopen restriction XML
  when the SDK starts enforcing such requirements.
- MGMT-IMP-002: authoritative semantics and timing for tag `0x17` value zero remain unresolved. No
  claim that false either restores NFC or is ineffective was added. Reopen XML after firmware evidence
  or scoped authorized observations settle the contract.
- MGMT-IMP-003: request mask-width and device-supported-mask enforcement remain deferred. Reopen
  type/property/validation XML after that policy changes.
- Timeout-zero semantics require the assigned independent review against original E4 passages and
  E1's 15-second default. Device-specific range maxima remain unestablished; no expansion was made.
- MGMT-IMP-004 concerns test cleanup/restoration outside B ownership. No test assertions/comments were
  edited, and no new test or hardware evidence is implied by these documentation edits.

No additional consequential ambiguity was discovered beyond the existing triage ledger. Fresh
independent review, integration and final validation are still pending.
