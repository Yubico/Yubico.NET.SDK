# B — correction follow-up review

2026-11-04; independent reviewer `github-copilot/gpt-6.1-sol`.

**Verdict: PASS for B's documentation review at the final hashes below.** RB-R1 is corrected
and accepted; expressly authorized RB-O1 is accepted. **No remaining required corrections.**
Together with the complete initial [review-b.md](review-b.md), this accepts all 13 B-owned
documentation files for the recorded scope. It does not pass implementation, compilation,
mechanical executable preservation, example, cryptographic-conformance or hardware gates.

## Follow-up scope and evidence

Read [correction-b.md](correction-b.md), amended [edit-b.md](edit-b.md), the complete current
`src/Ctap2/src/Pin/ClientPin.cs` (777 lines), and its complete current diff. Re-reviewed the full
class remarks, complete UV-token remarks and adjacent PIN-token contracts against actual complete
method bodies, not merely the two changed sentences. Initial root/module/test instructions,
campaign workflow/style and security-skill context remain applicable.

Independently fetched the **exact original CTAP 2.1 errata** again on **2026-11-04**, HTTP 200:
[Client to Authenticator Protocol (CTAP), Proposed Standard, June 21, 2022](https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-errata-20220621.html).
Read the complete original §6.5.5.7.1–3 issuance sections, including mismatch processing before
forced-change handling and the configured-UV check. No different revision or inherited status
assertion was substituted. Original review and retained evidence were not edited.

### RB-R1 — accepted

`ClientPin.cs:473–478` now tells the caller to use the UV method when GetInfo reports
`uv=true` and `pinUvAuthToken=true`, with built-in UV configured. It separately names the
**authenticator** implementation obligation; it does not turn that obligation into mandatory
platform invocation. The original §6.5.5.7.3 (`#getPinUvAuthTokenUsingUvWithPermissions`) states:

> This subCommand MUST be implemented if the authenticator returns both uv and pinUvAuthToken
> option IDs set to true in the authenticatorGetInfo response.

Actor: authenticator; normative MUST implement under those advertised conditions. Configured UV
is a separate prerequisite; the same algorithm returns NOT_ALLOWED if supported but unconfigured.
The revised text preserves the uvBioEnroll/uvAcfg permission conditions. It expressly disclaims
helper GetInfo query and capability/configuration/RP preflight, matching `ClientPin.cs:492–555`:
nonzero-permission check, key-agreement query, then one 0x06 token request, with no such preflight.
The following complete paragraphs retain possible authenticator UP interaction, generic-exchange
notification limits, no automatic UV retry/PIN fallback, and pcmr-only capability/lifetime conditions.
No new negotiation, retry or local enforcement policy is implied.

### Authorized RB-O1 — accepted

The complete added class paragraph, `ClientPin.cs:50–54`, directs a PIN change before PIN-token
issuance when GetInfo reports `forcePINChange=true`. It distinguishes **subcommands**, not protocol
versions or method-name assumptions. After correct-PIN verification, the original normative
authenticator algorithms state:

- §6.5.5.7.1 (`#getPinToken`), **legacy 0x05**:
  “If the value of the forcePINChange member of the authenticatorGetInfo response is true,
  authenticator returns CTAP2_ERR_PIN_INVALID error.” The following compatibility/platform-check
  explanation is a non-normative note, not implemented SDK preflight.
- §6.5.5.7.2 (`#getPinUvAuthTokenUsingPinWithPermissions`), **permission 0x09**:
  “If the value of the forcePINChange member of the authenticatorGetInfo response is true,
  authenticator returns CTAP2_ERR_PIN_POLICY_VIOLATION. Platform on receiving such error response
  SHOULD direct the user to change the PIN.” Authenticator status action and platform SHOULD
  guidance are distinct from helper behavior.

Both original flows also return PIN_INVALID in the prior mismatch/error branch when not blocked.
The new sentence therefore correctly says **PIN_INVALID alone proves neither an incorrect PIN
nor forced-change state**; it does not infer every PIN_INVALID has either particular cause. The
existing exception entries say incorrect PIN *can* cause CtapException, not that it is the only
cause. They do not contradict the explicit class qualification.

`GetPinTokenAsync` always encodes 0x05 (`ClientPin.cs:319–359`). The permission method selects
0x09 only when supplied GetInfo's pinUvAuthToken is true, otherwise calls legacy 0x05 and drops
requested permission/RP; it does not preflight forcePINChange or retry a failed 0x09
(`ClientPin.cs:396–459`). The full surrounding remarks preserve that fallback-versus-negotiation
distinction. Both corrections remain direct, useful API explanations rather than a new audit
chronology or intended-policy endorsement of deferred checks.

### D-14 status qualification — bounded confirmation, coordinator ledger work remains

Independently read the complete `SetMinPinLength_ForceChangePin_FullCycle` method,
`src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2AuthenticatorConfigTests.cs:190–285`.
Its forced-state assertion at lines 242–244 calls **GetPinTokenAsync**, expecting PinInvalid.
That is the legacy 0x05 path above; earlier use of a permission token for configuration does not
change this later call's subcommand. The alleged wrong-status assertion is consequently unsupported
by original CTAP 2.1. This is source/spec agreement, **not execution evidence** or acceptance of
the test's entire lifecycle. Separate setup mutation, partial restoration, cleanup and assertion-scope
concerns remain. Coordinator owns deduplication/withdrawal in current ledgers; no historical D
record, D source or shared ledger was edited here, and no new defect count is introduced.

## Verified final target — all 13 file SHA256 hashes

Baseline remains `5b09a9b6942ded3913b5c9340c3e126617e61a82`; worktree
`/tmp/opencode/ctap2-001-edit-b`. Read-only Python/hash checks compared current bytes to every
edited hash in immutable `review-b.md` and required the baseline-relative changed-path set to
equal exactly the 13 B inventory paths. **All other 12 reviewed hashes are unchanged.**

For ClientPin, reversing only the exact added RB-O1 paragraph and replaced RB-R1 paragraph
in memory reproduces original reviewed SHA256
`57790e29a23941dae2fca1773b79728a2b4427361b0d11df8f4c3545d9f1b7e6`.
This verifies no other ClientPin bytes changed since initial review; no reconstructed file was written.

| Exact reviewed path | Final file-byte SHA256 | Since initial review |
| --- | --- | --- |
| `src/Ctap2/src/Pin/ClientPin.cs` | `5a937547f76796fd6c72b5ff7a01d60c428e10098474fda747928605ce0407c0` | Only the two reviewed XML units changed |
| `src/Ctap2/src/Pin/ClientPinSubCommand.cs` | `b00e83166447955f42179ea19eac0135a05511ea101e4d142859666b20b4b3ca` | Unchanged |
| `src/Ctap2/src/Pin/IPinUvAuthProtocol.cs` | `207d521fbbcbbb073fc2cf1bc84427d6ad52b4ccb270c59cefcfe3d8abc88fd0` | Unchanged |
| `src/Ctap2/src/Pin/PinRetryStatus.cs` | `c30d9bff8dbdfcb3bdef6e9572345744f41f3d5d7028ef4a9775c2b86aefea8d` | Unchanged |
| `src/Ctap2/src/Pin/PinUvAuthHelpers.cs` | `a09c413760f942fab7b091f371adc6846a93137c793b9bf3a50bdfe6525c9179` | Unchanged |
| `src/Ctap2/src/Pin/PinUvAuthProtocolV1.cs` | `77c2a8c7a00f94089d88ec392c6f313a74fe2be98da5e0d40ff0528091ca5c41` | Unchanged |
| `src/Ctap2/src/Pin/PinUvAuthProtocolV2.cs` | `fb8a03ce6864b15138e56d91ff401bced6d51dc4aed18afa2e20430b21b752d7` | Unchanged |
| `src/Ctap2/src/Pin/UserVerificationRetryStatus.cs` | `60d7250eb4d67522b8c131c18c23b195f89188a53a410143774e2c83db3ef461` | Unchanged |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2EnhancedPinTests.cs` | `3a310e73455ca375f8352afc8dc6a0ddb4a53d5924b6305a029245db886cd26e` | Unchanged |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2PinManagementTests.cs` | `39602d50a59b4ed42d5bd798c4fbc780c5bccb730390997f179276c61d68d5b2` | Unchanged |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Pin/ClientPinTests.cs` | `83d96f8add7ede53fb09f33de8d7f29f39d6e5a364d85cfafc524064fa688f22` | Unchanged |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Pin/PinUvAuthProtocolV1Tests.cs` | `32d3b4edad5ad112979cc086b5130e72c1be92937dcccf54c27e2d3cc0e380f1` | Unchanged |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Pin/PinUvAuthProtocolV2Tests.cs` | `b1ec8a777138dfdae86eb80487488879ec32ac3c3124495b5f6874a14a676042` | Unchanged |

Independent exact `git diff --binary` SHA256:
`6740b13faeae2eee6196fa307b2f64cd4c2aa73d911c31136e4542491d7a560f`,
matching the correction record. These hashes identify the accepted documentation target, not
compiler/reference resolution or the coordinator's full baseline executable-preservation gate.

## Limits / next owner

This follow-up authored only `review-b-followup.md` in the primary checkout. No source edits,
builds, tests, hardware/PIN operations, commits, delegation or example compilation occurred.
Initial review/evidence remains immutable. Initial accepted dispositions for the unchanged 12
files remain in `review-b.md`; the two accepted corrections supersede its pending findings only.

No remaining B review corrections. Existing deferred implementation/security/test items and
cross-owner F/A/C/D/E/G dependencies remain, including forced-state status/protocol-path guidance.
Coordinator performs ledger deduplication, integration, mechanical preservation, compilation/docs/
example gates and final cross-layer review. Any changed source hash reopens its affected review.
