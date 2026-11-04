# B — RB-R1 / authorized RB-O1 correction

2026-11-03; editor `github-copilot/gpt-6.1-sol`. **Ready for review-B follow-up; not independently
accepted yet.** Baseline remains `5b09a9b6942ded3913b5c9340c3e126617e61a82`. Source stays only in
`/tmp/opencode/ctap2-001-edit-b`; no edits were copied into primary source. Primary writes in this
follow-up are this record and an amendment to `edit-b.md`. `review-b.md`, original investigations,
triage and retained evidence are unchanged.

Read the complete review and current edit record, affected ClientPin XML and complete PIN/UV token
method bodies. Loaded receive-review; the earlier root/module/test instructions and security/verify
skills remain applicable. Independently fetched the exact original CTAP publication before wording,
then checked the D-14 statement against the actual config-test assertion. No other optional changes.
**No builds, tests, hardware/PIN operations, commits, delegation or example compilation.**

## Correction scope

Only `src/Ctap2/src/Pin/ClientPin.cs` changed since B's reviewed manifest, in two XML units:

| Review ID | Corrected unit / current lines | Disposition |
| --- | --- | --- |
| RB-R1 | `GetPinUvAuthTokenUsingUvAsync` opening remarks, 473–478 | Names caller prerequisites: GetInfo uv=true AND pinUvAuthToken=true, with built-in UV configured. Separately names authenticator MUST-implement obligation when both option IDs are true, not caller MUST-invoke. Explicitly no helper GetInfo query or capability/configuration/RP preflight. Existing permission-specific checks, single request/no automatic fallback, interaction and persistence qualifications retained. |
| RB-O1 (expressly authorized) | ClientPin class force-policy paragraph, 50–54 | Change PIN before PIN-token requests when forcePINChange=true. Original 2.1 distinguishes legacy 0x05 PIN_INVALID from permission 0x09 PIN_POLICY_VIOLATION after correct-PIN verification. PIN_INVALID alone establishes neither wrong PIN nor forced change. No local preflight/automatic retry claim. |

RB-R1 remains CTAP-DOC-005 / B-05 / existing CTAP-IMP-010. RB-O1 adds useful navigation for existing
force-policy/error documentation, not a new defect or retry policy. No runtime, API, assertion,
directive, string, import or other comment unit was changed in this follow-up.

## Independent original-source evidence

Access date **2026-11-03**. *Client to Authenticator Protocol (CTAP)*, **Proposed Standard,
June 21, 2022**, exact original CTAP 2.1 errata:
https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-errata-20220621.html

Fetched directly over HTTPS, response 200. Read the original issuance sections, including the
correct-PIN/mismatch processing before forced-change handling and the UV configuration check.
The first extraction included subsequent headings; a focused section-boundary extraction isolated
the exact passages below before editing. These excerpts retain original actors and strength; no
different revision was substituted to justify a status or invocation claim.

### §6.5.5.7.3 — `#getPinUvAuthTokenUsingUvWithPermissions`

> This subCommand MUST be implemented if the authenticator returns both uv and pinUvAuthToken
> option IDs set to true in the authenticatorGetInfo response.

Actor: **authenticator**, normative MUST implement under the advertised option condition. It is
not a platform/caller MUST invoke statement. Configuration is a separate operation prerequisite:

> If a built-in user verification method is supported but not configured, the authenticator
> returns CTAP2_ERR_NOT_ALLOWED.

The same original algorithm lists be with uvBioEnroll and acfg with uvAcfg in unauthorized-permission
checks. ClientPin's UV method locally rejects only None, obtains key agreement and sends 0x06; it
queries no GetInfo and checks neither advertised capability nor configured UV. The revised paragraph
matches that source and retains caller-selected protocol, interaction and caller-recovery boundaries.

### §6.5.5.7.1 — `#getPinToken` (legacy 0x05)

After PIN decryption/verification, the algorithm handles mismatches with blocked-state checks and
otherwise PIN_INVALID. It then resets pinRetries to maximum and states:

> If the value of the forcePINChange member of the authenticatorGetInfo response is true,
> authenticator returns CTAP2_ERR_PIN_INVALID error.

Normative authenticator action. The following **non-normative note** explains backwards compatibility
with CTAP2.0 platforms and that a CTAP2.1 platform checks forcePINChange before invocation. That
rationale is not evidence that PIN_INVALID exclusively identifies forced change or an incorrect PIN.

### §6.5.5.7.2 — `#getPinUvAuthTokenUsingPinWithPermissions` (0x09)

The same correct-PIN sequence precedes this separate forced-change branch:

> If the value of the forcePINChange member of the authenticatorGetInfo response is true,
> authenticator returns CTAP2_ERR_PIN_POLICY_VIOLATION. Platform on receiving such error response
> SHOULD direct the user to change the PIN.

Actors: authenticator returns this status; platform SHOULD direct a PIN change. This is not an SDK
preflight/retry implementation. The helper's permission method decides between 0x09 and legacy 0x05
using pinUvAuthToken from the supplied GetInfoAsync, preserving its selected protocol. A method name
or earlier permission-token acquisition does not prove the later forced-state request used 0x09.

## D-14 evidence qualification and cross-owner handoff

`investigation-d.md:228–243` preserves the original candidate and cautions to consult B before
diagnosing actual ClientPin behavior. Its status subclaim compares GetPinTokenAsync's expected
PinInvalid against the permission-token algorithm's PIN_POLICY_VIOLATION.

Independently read
`src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2AuthenticatorConfigTests.cs:190–285`:
after setting forceChangePin and asserting the flag, lines 242–244 call **GetPinTokenAsync** and
expect `CtapStatus.PinInvalid`. ClientPin.GetPinTokenAsync explicitly sends **0x05** regardless
of the protocol version. The earlier configuration authorization can use a permission token, but
does not change this later call. Thus **that alleged status mismatch is not supported by original
CTAP 2.1**; the assertion aligns with its legacy forced-change branch. This is inspected evidence,
not a claim the test ran or the whole lifecycle is qualified.

Route this bounded correction to D/coordinator without rewriting historical findings or editing
D-owned source. Keep D-14's separate setup mutation, best-effort restoration, cleanup and assertion
scope concerns; no blanket D-14 closure or new defect count. Re-review relevant
`AuthenticatorConfig`/`AuthenticatorInfo` force-policy XML, config-test comments and F/A/G PIN/error/
prompt guidance if they claim all token paths use PIN_POLICY_VIOLATION or all PinInvalid means
wrong PIN. Direct callers still choose recovery; no automatic retry policy is added to ClientPin.

## Preservation checks and new hashes

Static Python check required `git diff --name-only` to equal exactly the 13 B inventory paths,
loaded the 13 edited hashes from `review-b.md`, and compared current bytes. **Only ClientPin.cs
changed since that review; all other 12 hashes are identical.** For each of the 13 files, compared
baseline/current nonblank lines excluding whole-line `//` comments: **all executable lines identical**,
including strings, directives, APIs and assertions. Parsed all contiguous `///` blocks under an XML
wrapper: **91 well-formed blocks**. `git diff --check` exited 0. No compiler/cref resolution or tests
were involved. The two corrected XML units and adjacent unchanged remarks were read after patching.

| Target | Previous reviewed SHA256 | Current SHA256 |
| --- | --- | --- |
| `src/Ctap2/src/Pin/ClientPin.cs` file bytes | `57790e29a23941dae2fca1773b79728a2b4427361b0d11df8f4c3545d9f1b7e6` | `5a937547f76796fd6c72b5ff7a01d60c428e10098474fda747928605ce0407c0` |
| Overall B patch, exact command `git diff --binary` in editor worktree | `b8fc9814c9d5754dd72436b9315127cc30818f7306418de5ee5cabf7b3ce13e0` | `6740b13faeae2eee6196fa307b2f64cd4c2aa73d911c31136e4542491d7a560f` |

Current baseline-relative diff is **13 files, 301 insertions / 148 deletions**. Previous checks/hashes
are retained in `edit-b.md` as initial edit evidence; its amendment points to this correction target.
Source comments only; no new snippet, test, placeholder or changed typed prerequisite. Existing
snippet classifications, all F/A/D/E/G/C dependencies and deferred implementation findings remain.
Review-B follow-up should inspect the complete revised class/UV remarks, original clauses and the
D-14 status qualification. Independent acceptance and coordinator compilation/docs gates remain pending.
