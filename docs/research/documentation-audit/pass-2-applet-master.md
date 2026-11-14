# Deferred applet work: Pass 2 master review

**Integrated review draft, 2026-11-13. All nine applet sections are authored; independent ledger-level
review and a targeted semantic/source sample are recorded, not full semantic certification.
User review is unstarted for every applet. Implementation remains deferred.**

Review **one applet at a time**, in the order below. Each section provides local context, a review map,
actionable cards and scoped coverage dispositions. Read its map with the selected card: severity,
certainty and no-brainer judgments can be in the map rather than repeated in the card body. Load deeper
source or frozen evidence only when that card needs it. Do not treat this document as one implementation
batch or as a count of confirmed bugs.

The [final reconciliation](#final-coverage-reconciliation) provides the card-count/state tracker,
canonical shared referrals and acceptance workflow. The [integration review inputs](pass-2-applet-master-coverage.md)
record bounded checks and review limits. The [independent review](pass-2-applet-master-review.md) records
the ledger-level comparison and targeted sample; its remaining per-applet groups are lazy human-review
followups, loaded only when that applet is selected, not missing known finding IDs. R1's bounded correction
is recorded in the [correction receipt](pass-2-applet-master-correction-r1.md); reviewer closure remains
pending. No source fix, hardware operation, build, SDK test, commit or push is authorized by this document.

## Review order and inventory

This follows the campaign's handoff order. Counts below describe original review groups, **not independent
bugs**. A group can contain overlapping observations, optional enhancements and evidence questions.
Cross-layer occurrences must link to a canonical owner instead of adding duplicate defect counts.

| Applet | Lightweight inventory from authoritative handoff | Draft state | Authoritative deferred sources |
| --- | --- | --- | --- |
| [OATH](#oath) | Six unresolved DEC groups; all 14 IMP groups resolved; broader inherited test and qualification facets retained | **Drafted; unreviewed; implementation deferred** | [Handoff](oath.md), [decisions/resolutions](records/implementation-followups.md), [wave-two completion](records/runs/fixes-wave-002/completion.md) |
| [WebAuthn](#webauthn) | All 21 WA-IMP groups; 12 Core boundary groups reconciled; WA-XREF-001 documentation resolved | **Drafted; unreviewed; implementation deferred** | [Handoff](webauthn.md), [triage](records/runs/webauthn-001/triage.md), [CTAP boundary correction](modules/core/pass-1/core-001/boundaries/Ctap2/followup-WebAuthn.md) |
| [Management](#management) | All 17 MGMT-IMP groups; two Core metadata referrals; three Core boundary groups reconciled; XREF-003 documentation resolved | **Drafted; unreviewed; implementation deferred** | [Handoff](management.md), [triage](modules/management/pass-1/management-001/triage.md), [Core boundary](modules/core/pass-1/core-001/boundary-Management.md) |
| [CTAP2](#ctap2) | 47 IMP groups covered; rejected 017 excluded; DEC-002/003, reader dependency and all 23 Core boundary groups reconciled | **Drafted; unreviewed; implementation deferred** | [Handoff](ctap2.md), [triage](modules/ctap2/pass-1/ctap2-001/triage.md), [addendum with errata](modules/ctap2/pass-1/ctap2-001/triage-addendum.md) |
| [Security Domain](#security-domain) | All eight SD-IMP groups; five SD-DEP questions and eight Core boundary groups reconciled; profile, producer and physical qualifications retained | **Drafted; unreviewed; implementation deferred** | [Handoff](security-domain.md), [findings](modules/security-domain/pass-1/security-domain-001/findings.md), [Core boundary](modules/core/pass-1/core-001/boundary-SecurityDomain.md) |
| [YubiOTP](#yubiotp) | All 12 OTP-IMP groups, five OTP-DEP questions and nine Core boundary groups reconciled; one-shot ownership and physical qualifications retained | **Drafted; unreviewed; implementation deferred** | [Handoff](yubiotp.md), [findings](modules/yubiotp/pass-1/yubiotp-001/findings.md), [Core boundary](modules/core/pass-1/core-001/boundary-YubiOtp.md) |
| [YubiHSM Auth](#yubihsm-auth) | All eight HSM-IMP groups, three HSM-DEP questions and seven Core boundary groups reconciled; response/profile, native and physical qualifications retained | **Drafted; unreviewed; implementation deferred** | [Handoff](yubihsm.md), [findings](modules/yubihsm/pass-1/yubihsm-001/findings.md), [cryptogram investigation](modules/yubihsm/pass-1/yubihsm-001/cryptogram-investigation.md) |
| [OpenPGP](#openpgp) | All 14 PGP-IMP groups; four PGP-CORE questions, eight Core boundary groups and 21 documentation dependency labels reconciled; confirmed wire, ownership and physical qualifications retained | **Drafted; unreviewed; implementation deferred** | [Handoff](openpgp.md), [findings](modules/openpgp/pass-1/openpgp-001/findings.md), [Core boundary](modules/core/pass-1/core-001/boundary-OpenPgp.md) |
| [PIV](#piv) | All ten PIVIMP groups, three PIVCORE questions, six Core boundary groups and 11 routed provenance keys reconciled; post-PR745 exclusions and Windows response-only rollback retained | **Drafted; unreviewed; implementation deferred** | [Handoff](piv.md), [findings](modules/piv/pass-1/piv-001/findings.md), [original definitions](modules/piv/pass-1/piv-001/investigation.md) |

The inventory describes **original coverage groups**, not the number of displayed cards. All nine
sections are now authored. Their ledgers preserve unresolved facets and scoped exclusions; integration
does not establish independent semantic completeness, new runtime evidence or a new all-applet audit.

### Shared authority and coverage rules

- Start with the [campaign index](README.md), module handoff and latest closeout. Historical pending text
  does not override a later closeout. Follow the approved [voice](records/style.md).
- Frozen [Core deferred handoff](modules/core/pass-1/core-001/deferred-findings.md),
  [candidate reconciliation](modules/core/pass-1/core-001/triage/final/candidate-reconciliation.json),
  [inherited reconciliation](modules/core/pass-1/core-001/triage/final/inherited-reconciliation.json) and
  [continuity](modules/core/pass-1/core-001/triage/final/reconciliation-4/deferred-finding-continuity.json)
  preserve original IDs, facet boundaries and reopening conditions. They are not a new accepted bug total.
- The [Windows successor](integrations/windows-upstream-20261113/deferred-findings-successor.md) is
  additive: old built-in Windows report mechanisms were superseded, while named native/platform,
  independent-copy, workflow, physical and applet qualifications remain open. Do not import the entire
  Core backlog into this applet master.
- Each section supplies a module-local coverage ledger: every original deferred ID → card or explicit
  resolved/retracted/superseded exclusion; every Core boundary ID → canonical applet card or narrow shared
  dependency. Partial resolution must name both the superseded mechanism and remaining facet.
- Keep PIV tool RSA/Ed25519, cleanup/workflow and physical deferrals open at their recorded scopes. Do not
  reopen OATH's 14 IMP resolutions or the retracted HSM retry-message subclaim.
- Leave frozen records and unrelated user files unchanged. This master is the active review narrative.

### Evidence identity and card conventions

The inspected C# baseline is clean commit **`6ce355468047e8004fd51cf1425808f6e8242ed3`**.
The section authors report checking selected BEFORE excerpts against that source, with path, symbol
and line range. Their verification notes are author-time receipts, not independent review or execution.
Integration preserves those excerpts without rerunning every authorship check. Recheck the selected
source window if a card is changed or implemented; a handoff alone does not establish a current mechanism.

Local Python repository: **`/home/rupicola/Code/y/yubikey-manager`**, commit
**`4ca60f706af930459138d8dc0f0f953480e1c7a4`**. `git status --short` was empty at inspection. The
immediate parent directory was listed once to locate it. `py:` paths below are relative to that repository
and commit. Python is implementation precedent, **not normative authority, .NET memory-lifetime
equivalence or runtime proof**. External packages, including Python `fido2`, are not assumed inspected.

All cards inherit **user review: unstarted; implementation: deferred**. AFTER is **proposed, unapplied and
uncompiled**. Literal C# is identified as such. `text` blocks are schematic decision-dependent flow;
uppercase names are explicit placeholders, **not SDK APIs**. No implementation authorization follows
from selecting a schematic alternative.

Severity is impact-based and separate from certainty: **High / Medium / Low** for bugs or risks;
**qualification, no assigned defect severity** for nonbug evidence work. Certainty distinguishes source
observation, historical device evidence and unsettled intent. **No-brainer: yes** is reserved for a
mechanical change with a settled contract; a compound group can still require decisions.

Eventual implementation completion requires accepted intent, selected scope, meaningful behavior tests and
independent code/documentation review. Record actual revision, test commands/results and evidence limits.
Qualification closure instead requires identified device/firmware/transport/OS/state, authorized scope,
actual executed outcomes and independent cleanup. A skip is not a pass. Builds/tests use repository
skills; do not create validation-only or skipped placeholder tests.

## OATH

### Context and review map

OATH provides TOTP and HOTP over SmartCard. Password protection has two different states: the device can
have a password configured while a particular session is already unlocked. An access key is derived on
the host and used for mutual HMAC authentication; it is not the user's password sent to the device.
Outgoing credential IDs use a host naming convention inside a 64-byte UTF-8 budget. HOTP calculation
can change a stored counter, unlike ordinary time-based TOTP calculation.

The [handoff](oath.md) and [wave-two completion](records/runs/fixes-wave-002/completion.md) close all 14
numbered IMP findings within scope. They leave six DEC groups, wider counter/API policy, inherited
test-scaffolding debt and bounded device/Core qualification. This section does **not** recast all of
those as newly proven bugs.

| Review card | Kind | Severity | Certainty | No-brainer |
| --- | --- | --- | --- | --- |
| [OATH-DEC-001](#oath-dec-001--state-after-failed-device-authentication) | Session-policy decision | Medium if unsafe reuse/incorrect state is accepted | Current ordering source-confirmed; desired terminality unresolved | No |
| [OATH-DEC-002](#oath-dec-002--raw-access-key-contract) | Input/support contract | Low | Source-confirmed admission; device range unqualified | No |
| [OATH-DEC-003](#oath-dec-003--uri-extensions-and-wider-hotp-counters) | Parser/API decisions | Low | Current parser/range source-confirmed; intended extensions unresolved | No |
| [OATH-DEC-004](#oath-dec-004--fourteen-byte-storage-padding) | Rationale/profile qualification | No assigned defect severity | Practice corroborated; rationale unresolved | No |
| [OATH-DEC-006](#oath-dec-006--hotp-operation-and-marker-semantics) | Device operation qualification | No assigned defect severity | Wire path confirmed; advancement/failure semantics incomplete | No |
| [OATH-DEC-007](#oath-dec-007--secure-channel-gate-provenance) | Firmware/profile qualification | No assigned defect severity | Selected sibling policy confirmed; earliest support unresolved | No |
| [CORE-OATH-X005](#core-oath-x005--inherited-test-resource-and-restoration-work) | Test resource/oracle work | Low for public test values; Medium if reused with real secrets or failure masks state | Representative current gaps source-confirmed; remaining sites inherited | No for the group; individual owner-scope moves are mechanical |
| [Secure-reset qualification](#oath-secure-reset-and-discovery-qualification) | Live device evidence | No assigned defect severity | Historical setup failure; secure RESET never reached | No |

### OATH-DEC-001 — State after failed device authentication

**Original:** [DEC-001](records/implementation-followups.md#dec-001--authentication-failure-provenance-and-session-state).
The provenance correction is already resolved: device-HMAC mismatch is BadResponseException, not proof
of a wrong user password. Only **session usability after failed device verification** remains.

**Impact and decision:** a failed mutual-authentication check must not be treated as successful unlock.
Current source correctly avoids publishing unlock on mismatch. Decide whether to retain a locked
session for retry, retire it until fresh creation, or permit only an explicit recovery path. Medium is
conditional on the selected state/reuse policy; no bypass or exploit has been demonstrated.

**BEFORE — `src/Oath/src/OathSession.cs`, `ValidateAsync`, lines 745–750:**

```csharp
if (!CryptographicOperations.FixedTimeEquals(expectedResponse, deviceResponse))
    throw new BadResponseException("The OATH device authentication response did not match verification.");

IsLocked = false;
CryptographicOperations.ZeroMemory(_challenge);
_challenge = [];
```

**Proposed AFTER — schematic, terminal option shown; retry option must be decided instead if selected:**

```text
if device HMAC does not match:
    preserve BadResponse provenance and clear operation-owned temporaries
    MARK_SESSION_TERMINAL using the chosen lifecycle contract
    reject later protected calls until fresh creation
else:
    publish unlocked state and clear the completed challenge
```

**Python:** `py:yubikit/oath.py:333–351`, `OathSession.validate`, raises BadResponseError on mismatch
before clearing `_challenge`. Applicable error/state ordering; not a .NET terminality mandate or a
device observation. Do not reopen fixed status wording.

**Acceptance/dependencies:** fake wrong/missing device HMAC, preserved error/status provenance, retained
lock/challenge state or terminal admission according to the accepted alternative, then next-call and
fresh-session behavior. Link [shared workflow/state](#oath-shared-dependencies) if lifecycle changes.
Re-review Validate/error XML, unlock/password guides and migration recovery claims. Deeper
device-status history is available on request, not needed to review this remaining state choice.

### OATH-DEC-002 — Raw access-key contract

**Original:** [DEC-002](records/implementation-followups.md#dec-002--accepted-raw-access-key-contract).
The normal workflow derives a 16-byte access key. Nonempty raw-key admission does not establish
arbitrary-length device support. This is a Low API/support question, not an observed authentication defect.

**BEFORE — `src/Oath/src/OathSession.cs`, `ValidateAsync:702–704` and `SetKeyAsync:786–787`:**

```csharp
if (key.Length == 0)
    throw new ArgumentException("Key must not be empty.", nameof(key));
```

`SetKeyAsync:798–803` then copies the entire raw key after its type byte into TAG_KEY. It does not
truncate to the standard password-derived width.

**Proposed AFTER — schematic alternatives, not a new input restriction:**

```text
standard-only contract: admit exactly the selected 16-byte password-derived key form
OR explicit raw-key contract: admit lengths justified by a named device/profile matrix
apply the same chosen boundary to Validate and SetKey before transmission
never normalize, consume or clear caller-owned key input
```

**Python:** `py:yubikit/oath.py:333–376`, `validate`/`set_key`, forwards raw key without a fixed-length
guard. Applicable permissive comparator; its PBKDF2 workflow still produces 16 bytes. It supplies no
proof that every length works across devices.

**Acceptance:** exact outgoing key TLVs and no transmission for rejected inputs; caller-input retention,
success/failure cleanup and applicable qualified-device lengths if raw support is chosen. Re-review
Validate/SetKey/DeriveKey contracts and all raw/password examples together. No-brainer: **no**.

### OATH-DEC-003 — URI extensions and wider HOTP counters

**Original:** [DEC-003](records/implementation-followups.md#dec-003--define-provisioning-uri-validation-policy)
plus the explicitly deferred **wider-counter/API remainder of resolved IMP-007**. Keep this as one linked
review group, not another defect count. Current URI policy accepts missing HOTP counter as zero, query
issuer precedence and digits 6–8. Strict Google URI syntax versus documented extensions is unsettled.
REQUIRED and SHOULD conditions must not be conflated.

**BEFORE — `src/Oath/src/CredentialData.cs`, `ParseUri`, lines 179–183 and 217–222:**

```csharp
string? queryIssuer = queryParams["issuer"];
if (queryIssuer is not null)
{
    issuer = queryIssuer;
}
```

```csharp
string? counterParam = queryParams["counter"];
int counter = counterParam is not null
    ? int.Parse(counterParam, System.Globalization.CultureInfo.InvariantCulture)
    : OathConstants.DefaultImf;

ValidateRequestNumbers(oathType, digits, period, counter);
```

**Proposed AFTER — schematic, choose extensions and numeric representation before coding:**

```text
parse non-secret URI fields first
require a HOTP counter OR retain explicit documented default-zero extension
reject conflicting issuers OR retain explicit documented query precedence
admit chosen URI digit syntax independently of the device's 6..8 request range
choose Int32-only API OR a wider carrier with exact supported four-byte IMF encoding
only then decode the secret; preserve existing temporary-secret cleanup and ID round trips
```

**Python:** `py:yubikit/oath.py:118–146`, `CredentialData.parse_uri`, defaults counter and prefers query
issuer; `put_credential:407–408` packs a positive counter with `struct.pack(">I", d.counter)`. This
corroborates permissive URI policy and a four-byte unsigned outgoing field. Python ints differ from
the C# nonnegative Int32 surface; wire width alone does not settle desired API or device acceptance.

**Acceptance:** missing/zero/max counter, issuer agreement/disagreement, extra colons and 6/7/8 digits;
boundary wire vectors and overflow behavior if widening. Verify no PUT on rejected outgoing values and
actual secret-owner cleanup. Keep all fixed Base32/identity/numeric defects closed. Re-review ParseUri,
Counter/Digits/ID XML, URI examples and migration extensions. Low severity; no-brainer: **no**.

### OATH-DEC-004 — Fourteen-byte storage padding

**Original:** [DEC-004](records/implementation-followups.md#dec-004--establish-the-provenance-of-fourteen-byte-storage-padding).
The SDK pads storage secrets shorter than 14 bytes. RFC 2104 does not supply that threshold. The open
question is historical/device rationale and applicability, not demonstrated broken HMAC computation.

**BEFORE — `src/Oath/src/CredentialData.cs`, `PadSecret`, lines 334–341; constant in `OathConstants.cs:61`:**

```csharp
if (secret.Length >= OathConstants.HmacMinimumKeySize)
{
    return secret;
}

byte[] padded = new byte[OathConstants.HmacMinimumKeySize];
secret.CopyTo(padded.AsSpan());
return padded;
```

**Proposed AFTER:** **no runtime change selected**. Verification procedure: obtain an original vendor,
history or applicable device statement for the 14-byte storage threshold, distinguish storage practice
from cryptographic minimum/entropy, then either retain it or propose a separately reviewed naming/wire
change. Do not remove padding merely because the present protocol page lacks a minimum.

**Python:** `py:yubikit/oath.py:397–398`, `OathSession.put_credential`, shortens and applies
`secret.ljust(HMAC_MINIMUM_KEY_SIZE, b"\0")`. Applicable practice; no historical hardware rationale.

**Acceptance:** evidence with exact provenance/applicability; selected constant terminology; exact
short/equal/long secret wire and HMAC vectors only if code is later changed. Preserve independent
processed-secret ownership from resolved IMP-003. Re-review key-processing rationale. Qualification,
**no assigned defect severity**; no-brainer: **no**. Deeper historical lookup available on request.

### OATH-DEC-006 — HOTP operation and marker semantics

**Original:** [DEC-006](records/implementation-followups.md#dec-006--establish-hotp-operation-and-marker-details).
Full versus truncated individual calculation, failure-time consumption and aggregate HOTP+touch marker
precedence are incompletely qualified. Two different successive outputs do not identify exact counters.

**BEFORE — `src/Oath/src/OathSession.cs`, `CalculateCodeAsync`, lines 480–486 and 497–499:**

```csharp
else
{
    challenge = [];
}

using var nameTlv = new Tlv(OathConstants.TagName, credential.Id);
using var challengeTlv = new Tlv(OathConstants.TagChallenge, challenge);
```

```csharp
// P2=0x01 requests truncated response
var command = new ApduCommand(0x00, OathConstants.InsCalculate, 0x00, 0x01, data);
var response = await SendAsync(command, token).ConfigureAwait(false);
```

**Proposed AFTER:** code is **not applicable until observations settle the contract**. Procedure:
provision a dedicated HOTP credential with known seed/counter; independently compute expected successive
values; record full/truncated individual calls, failures before/after admission and aggregate calls.
Separately observe HOTP+touch markers and request outcomes with human coordination. Record counter
consumption on failed tasks instead of assuming failure means no mutation. Only then select code/API
changes and corresponding failure/marker tests.

**Python:** `py:yubikit/oath.py:491–566`, `calculate_all` maps TAG_HOTP/TAG_TOUCH; `calculate_code`
sends empty HOTP challenge and requests truncation. Applicable wire/marker interpretation; not proof of
device failure-consumption or every combined-policy outcome.

**Acceptance:** identified firmware/transport/state, independent counter oracle, exact marker bytes and
command/outcome sequence, cleanup. Re-review calculation/HOTP/null-result/presence descriptions without
reopening resolved structural parser fixes. Qualification, **no assigned defect severity**;
no-brainer: **no**. Deeper device sequence context available on request.

### OATH-DEC-007 — Secure-channel gate provenance

**Original:** [DEC-007](records/implementation-followups.md#dec-007--establish-oath-secure-channel-gate-provenance).
The user selected the Python YubiKey 5 policy reference. Historical rationale and earliest physical
support remain unresolved; Core's generic SCP03 floor does not replace OATH's floor.

**BEFORE — `src/Oath/src/OathSession.cs:36`, `FeatureScp03`:**

```csharp
private static readonly Feature FeatureScp03 = new("SCP03 for OATH", 5, 6, 3);
```

The effective-version gate is applied to **any non-null SCP parameters**, despite the field name. Core
separately gates SCP03 at 5.3.0 and SCP11 at 5.7.2. Overrides/development policy are not physical firmware proof.

**Proposed AFTER:** **retain the selected gate; no code change proposed yet**. Procedure: obtain exact
family/firmware rationale and supported matrix, distinguish workaround/policy from earliest hardware
support, then review any proposed gate/name change together with factory/profile preflight and actual
compatible-device evidence. Do not infer touch/SHA-512 preflight gates or a general OATH-open floor.

**Python:** `py:yubikit/oath.py:279–282`, `OathSession.__init__` rejects SCP for
`5.0.0 <= version < 5.6.3`. This establishes the selected YubiKey 5 comparator policy, not earliest
physical support, NEO support or an identical universal version predicate.

**Acceptance:** source/profile evidence, exact override/detected version tests if changing policy and
authorized compatible-device outcomes. Re-review factory/features/requirements and shared SCP gates
without broadening scope. Qualification, **no assigned defect severity**; no-brainer: **no**.
Historical rationale available on request.

### CORE-OATH-X005 — Inherited test resource and restoration work

**Canonical owner:** `core-001:CORE-OATH-X005`, test facets only, from the frozen
[candidate reconciliation](modules/core/pass-1/core-001/triage/final/candidate-reconciliation.json).
This is separate test scaffolding, **not reopening the 14 fixed product findings**.

Remaining recorded scopes include undisposed CTS helpers, untracked UTF-8 password inputs, AuditV2 raw
PUT seed/payload copies, DELETE status/readback gaps, borrowed fake snapshot/response disposal and cleanup
that can mask the first failure or assume password rotation succeeded. Representative current source
confirms the first-input and restoration mechanisms; the rest retains frozen per-site evidence pending
selection rather than claiming a fresh full test audit.

**BEFORE — `src/Oath/tests/Yubico.YubiKit.Oath.IntegrationTests/OathPasswordChangeTests.cs`,
`NewToken:37–38`, `WithDerivedKeyAsync:54–64`:**

```csharp
private static CancellationToken NewToken(int timeoutSeconds = 30) =>
    new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)).Token;
```

```csharp
var key = session.DeriveKey(Encoding.UTF8.GetBytes(password));
try
{
    await action(key);
}
finally
{
    CryptographicOperations.ZeroMemory(key);
}
```

`PasswordChange` cleanup at lines 122–136 always opens with `newPassword`; an earlier rotation failure
can leave the old password active and cleanup can replace the first error.

**Proposed AFTER — literal focused replacement for the derivation owner scope, uncompiled:**

```csharp
byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
byte[]? key = null;
try
{
    key = session.DeriveKey(passwordBytes);
    await action(key);
}
finally
{
    if (key is not null)
        CryptographicOperations.ZeroMemory(key);
    CryptographicOperations.ZeroMemory(passwordBytes);
}
```

**Proposed AFTER — schematic for the remaining compound test scope:**

```text
caller owns CTS through every awaited operation; pass token rather than losing CTS owner
track completed persistent password transition before choosing cleanup authentication
finally: drain pending tasks; independently attempt required cleanup and final readback
retain primary failure and report secondary cleanup failures without hiding either
own/clear raw PUT seed and payload; check DELETE status and actual absence
dispose borrowed fakes from the test owner, not by changing session connection ownership
```

**Python:** `py:yubikit/oath.py:326–331`, `OathSession.derive_key`, returns derived immutable bytes
from a password string. Its synchronous immutable-byte model supplies **no equivalent .NET CTS or
retained-array zeroing oracle**. Do not copy that ownership model as a secure .NET pattern.

**Acceptance:** meaningful action-throw/derive-failure storage tests, exact command/status assertions,
held-task failure cleanup, completed-transition tracking and independent applet readback in a selected
authorized lane. Public test passwords are not observed secret compromise. Low fixture reliability,
Medium if adapted to real secrets or masking consequential state. Group no-brainer: **no**; the displayed
password-owner move is mechanical, while restoration/exception policy needs review. Re-review exact
helper/test comments, test prerequisites and claims of restoration/zeroing. Full remaining-site context
is available on request.

### OATH secure-reset and discovery qualification

**State:** unreviewed/deferred qualification; **no assigned defect severity**. BEFORE/AFTER code is not
applicable because the remaining task is evidence, not another terminal-reset implementation.

- Plain-channel terminal reset was exercised. Serial 125 returned `6A88` during both recorded
  INITIALIZE UPDATE variants (`80 50 FF 00` and `80 50 00 00`). **EXTERNAL AUTHENTICATE and RESET were
  never reached.** Offline secure reset tests are not live proof.
- Python `py:yubikit/oath.py:316–324`, `OathSession.reset`, reselects and rehandshakes on the same
  connection. C# intentionally uses a terminal contract; **do not propose Python reset as its AFTER**.
- Future procedure: re-establish authorized device/firmware/transport/SCP configuration; perform bounded
  initialization checks first; if successful, separately record authentication, RESET status, terminal
  session admission, teardown and fresh reopen with independent state readback. Do not replace unknown
  initialization cause with a generic gate explanation.
- `CORE-001` serial 9681620 two-key discovery remains separate. The key worked alone; release of native
  ownership during the historical multi-key failure was not proven. Current Windows owner delivery is
  not evidence that this original failure was reproduced or closed.

Completion needs actual source-bound outcomes for the selected lane, with device/OS boundaries,
blocked/skipped paths and cleanup retained. Touch/cross-platform claims remain separately qualified.
Deeper SCP initialization and discovery history is available on request; no new experiment is selected.

### OATH shared dependencies

These are narrow **linked closure rows**, not assignments for the full Core backlog. Current truthful
documentation can remain accepted while their implementation/profile/qualification choices are open.
Use the [canonical shared referral index](#canonical-shared-referral-index) to find matching owners;
the local rows retain OATH's necessary context and closure boundaries.

| Canonical source owner / facet | Why OATH needs the link | Closure boundary; proposed work not implemented |
| --- | --- | --- |
| `core-001:CORE-B5-C001` session readiness/getter state; `CORE-OATH-X001` caller occurrence | Cached lock/password/device/version observations can remain after terminal disposal; Salt is a session-owned borrowed view that is cleared. Inherited IsAuthenticated is SCP setup history, not OATH unlock/live health. | Decide terminal getter/state contract only if changing it; test actual retained views/flags and next-call admission. Existing terminal guidance is not a new bug. Python `validate`/`locked` observes challenge state, not .NET disposal. |
| `core-001:CORE-B5-C006` whole workflow; `CORE-OATH-X002` caller occurrence | CalculateAll followups, password-provider→Validate→retry, parsing and callbacks can outlive one drained exchange. | Choose whole-operation versus caller-serialized policy before adding a lease; held followup/callback/disposal tests with terminal drain and input/service retention. Python synchronous OathSession calls do not settle async callback policy. |
| `core-001:CORE-C-C001`–`C004` relevant credential-reader/input-owner facets; `CORE-OATH-X004` occurrence | Password-reader limits are UTF-16 code units before UTF-8 conversion; zeroing returned key does not clear immutable password strings or independent inputs. OATH presets are not FIDO PIN rules. | Scope chosen mutable-input/length/ownership contract at the reader and OATH call sites; do not sweep in unrelated crypto. Python `derive_key` uses a string; different ownership, not an erasure guarantee. |
| `core-001:CORE-B6-C015` SCP task/reselect qualification; `CORE-OATH-X003` occurrence | Selection precedes SCP; plain SELECT through an existing secure graph is not recovery. Exact SCP03 release/review and SCP11 review/final/vendor authority differ. | Keep terminal reset settled; qualify actual channel/profile and fresh reopen only in authorized selected work. Historical 6A88 is pre-reset, not secure reset proof. |
| `core-001:CORE-B6-C002`/`C007` and `CORE-B5-C005` independent producer-copy facets, only where OATH authentication/calculation material flows through them | OATH's fixed final raw-response clearing does not clear earlier APDU/TLV/transport/report/SCP copies. Windows staging cleanup does not close independent returned reports. | Trace selected owners, aliases and actual-last-use clearing with retained-array tests; no end-to-end/native erasure claim. Do not reopen scoped IMP-001/011 merely because lower layers differ. |
| Original `CORE-001`, Core known-member/claim and native-release qualification; `CORE-OATH-X006` occurrence | Multi-key discovery failure and known-member/process bounds are outside OATH protocol fixes. | Preserve historical device identity and unresolved release proof; separately authorize any reproduction/platform lane. Selection prose corrections are already accepted; no blanket global-exclusivity promise. |

### OATH coverage ledger and exclusions

| Authoritative original identity | Current disposition in this master | Canonical card / reason |
| --- | --- | --- |
| OATH IMP-001–014 | **Excluded from deferred product cards: all 14 scoped resolutions retained** | Latest handoff/completion supersedes historical pending entries. Authentication/input/protocol/CLI/reset fixes are not reopened. |
| DEC-001 | **Partial resolution retained** | Provenance resolved; broader failure-state policy → OATH-DEC-001. |
| DEC-002/003/004/006/007 | **Deferred; all six unresolved DEC groups covered including DEC-001** | Corresponding six cards above. |
| DEC-005 | **Excluded: resolved** | Nonnegative Int64 timestamp/exclusive-end policy stays closed. |
| IMP-007 wider-counter/URI remainder | **Deferred residual, not a new finding or reopening of fixed narrowing** | OATH-DEC-003 explicitly carries chosen carrier/range/URI review. |
| IMP-010 / live SCP reset evidence | **Implementation resolved; qualification open** | Secure-reset qualification card; terminal C# policy intentionally differs from Python. |
| DOC-001 | **Excluded: resolved documentation** | Linux PC/SC setup reference completed; no new setup campaign. |
| DOC-002 | **Shared documentation hygiene, outside OATH product scope** | Migration YAML syntax failure remains recorded; no edit or closure here. |
| Original CORE-001 | **Open, linked Core qualification** | Shared dependency and secure-reset/discovery card; not OATH unsupported-firmware evidence. |
| `CORE-OATH-X001` | **Documentation clarified; shared state choice retained** | CORE-B5-C001 linked row; no new product defect assigned. |
| `CORE-OATH-X002` | **Shared workflow policy remains open** | CORE-B5-C006 linked row, with actual OATH followups/callbacks. |
| `CORE-OATH-X003` | **Protected terminal reset retained; profile/live qualification open** | OATH-DEC-007 + secure-reset card + CORE-B6-C015 link. |
| `CORE-OATH-X004` | **Reader unit/ownership dependency retained** | Relevant C-C001–C004 link only; previous reader corrections not reopened. |
| `CORE-OATH-X005` | **Deferred test-scaffolding/oracle facets** | Canonical test card above; original source comments already corrected are not extra bugs. |
| `CORE-OATH-X006` | **Selection/example documentation correction retained; known-member/physical limits open** | CORE-001/shared claim qualification link; no new discovery defect or example implementation demand. |
| Windows successor protected OATH dispositions | **Unchanged** | All 14 IMP resolutions, 6A88 pre-auth/reset limit and two-key failure remain; new report-owner software tests do not close these device qualifications. |

**OATH coverage result:** six remaining DEC groups, one canonical inherited compound test card and one
secure-reset/discovery qualification card are drafted; all six original OATH/Core boundary IDs and
resolved exclusions are accounted. These are **eight review cards, not eight newly accepted defects**.
Human acceptance, source fixes and fresh runtime/device verification remain unstarted.

## WebAuthn

### Context and review map

WebAuthn is the client ceremony layer above CTAP2, not a browser or an RP server. Registration creates
a credential; authentication obtains assertions. The application authorizes the origin/RP pair and
hands responses to an RP that independently verifies them. A returned assertion, a successful CTAP
status and an accepted account login are three different outcomes. A failure after credential creation
or a blob write does not roll back that side effect.

The client probes and narrows a nonempty allow list, but can enumerate discoverable assertions when
the list is absent. It eagerly builds responses and extension outputs before returning matches.
`SelectAsync` retrieves a cached response: it neither contacts the key nor filters disclosure fields.
Core drains individual exchanges, not this entire workflow or a late PIN-prompt continuation.

This fresh section covers all **21 original WA-IMP groups**, plus the remaining bounded lifecycle and
qualification work. It preserves the [handoff](webauthn.md), [deferred register](records/runs/webauthn-001/triage.md#deferred-item-register),
[Core boundary](modules/core/pass-1/core-001/boundary-WebAuthn.md) and
[completed CTAP caller correction](modules/core/pass-1/core-001/boundaries/Ctap2/followup-WebAuthn.md).
Original descriptor-omission attribution is corrected: **CTAP2.0 §5.2** permits actual-singleton omission;
**2.1/2.3 §6.2.2** require the descriptor. WA-XREF-001 is resolved documentation, not an open prose bug.
The inventory's introductory OATH-only checkpoint predates this appended WebAuthn section.

**Status for every card below:** review unreviewed; implementation deferred; no new SDK or device
execution. BEFORE is literal current source at `6ce355468047e8004fd51cf1425808f6e8242ed3`.
AFTER blocks are proposed and unapplied; `text` blocks describe selected alternatives or required flow,
not verified API syntax. Re-review dependencies include the exact original register row, named XML/tests,
README/CLAUDE and extension architecture where applicable. Deeper original traces/citations are available
on request; the core review meaning and acceptance boundary remain here.

#### Python precedent boundary

The clean local Python pin was independently rechecked as **`4ca60f706af930459138d8dc0f0f953480e1c7a4`**.
Only relevant FIDO management equivalents were inspected. The following labels carry that exact pin
and `py:` repository identity into each card; **none means python-fido2 source was inspected**.

| Label | Actual inspected path/symbol | Applicable precedent and explicit limit |
| --- | --- | --- |
| P0 | `py:ykman/_cli/fido.py:35–44,115–116`, imports and `fido`; `py:pyproject.toml:21` | CTAP2/ClientPin are delegated to external `fido2` (`>=2.0, <3`). No local WebAuthn ceremony, CCDToString, PRF, previewSign, attestation-result or browser serializer equivalent was established. Those implementations cannot be attributed to this pin. |
| P1 | `py:ykman/_cli/fido.py:157–179`, `info` | Calls `ClientPin(ctap2)` without choosing a protocol and reads configured PIN/retry/minimum state. This is delegation, not inspected protocol negotiation, Unicode enforcement or a .NET async/zeroing oracle. |
| P2 | `py:ykman/_cli/fido.py:983–1000`, `enable_ep_attestation` | Checks `ep` and enables authenticator configuration via `Config`. Enabling enterprise attestation is not forwarding a WebAuthn makeCredential enterprise request or providing Indirect anonymization. |

| Card | Kind / severity rationale | Certainty | No-brainer |
| --- | --- | --- | --- |
| WA-IMP-001 | Attestation support/policy; Medium interoperability/privacy-policy risk | Missing forwarding/service source-confirmed | No |
| WA-IMP-002 | Error/isolation policy; Medium recovery and side-effect ambiguity | Catch/support boundaries source-confirmed | No |
| WA-IMP-003 | Deadline policy; Low input/result inconsistency | Cooperative mechanism source-confirmed; intended return policy unsettled | No |
| WA-IMP-004 | Hardware oracle; Low false failures | Missing guard source-confirmed; no device run | No for fixture/matrix; guard itself mechanical |
| WA-IMP-005 | Serialization bug plus builder policy; Low exact-byte interoperability gap | Control escapes source-confirmed | Yes for exact controls; no for builder/surrogate contract |
| WA-IMP-006 | Decoder ownership; Medium raw/parsed integrity risk if caller mutates | Borrowing source-confirmed; misuse conditional | No |
| WA-IMP-007 | Default-value bug; Low equality/collection failure | Source-confirmed | No until default carrier/hash contract selected |
| WA-IMP-008 | Zeroing oracle; Low evidence gap, not proven product leak | Actual test/source confirmed | Yes |
| WA-IMP-009 | PIN/UV compatibility; Medium unsupported token/key agreement paths | V2-only and incomplete preflight source-confirmed | No |
| WA-IMP-010 | Credential association bug; Medium empty ID and extension/RP handoff failure | Both ID-loss sites source-confirmed | No for version/error policy; known-singleton propagation is straightforward |
| WA-IMP-011 | PRF security/conformance; High wrong verified/unverified derived secret | Source and frozen normative mismatch confirmed | No; coordinated policy remains required |
| WA-IMP-012 | Protection enforcement; High unexpectedly weaker credential creation | Source-confirmed; CTAP SHOULD NOT distinct from SDK promise | No |
| WA-IMP-013 | Sensitive ownership; High residual derived secrets/plaintext | Selected allocations confirmed; no exploit/native erasure claim | No for API group; individual temporary clears mechanical |
| WA-IMP-014 | Required blob support; Medium unusable created credential | Effective-RK/key-output gaps source-confirmed | No |
| WA-IMP-015 | Blob-write request constraint; Medium unintended write/selection scope | Missing original-list check source-confirmed | Yes for exact cardinality; placement must precede side effects |
| WA-IMP-016 | Blob result bug; Medium lost absent/empty distinction | Assignment confirmed; isolated historical .NET conversion evidence | No for full output/error contract; explicit nullable conversion mechanical |
| WA-IMP-017 | Optional support decision; no assigned defect severity | Selected route/token policy confirmed | No |
| WA-IMP-018 | Input intent; Low false enablement | Presence-only admission confirmed | No until false/carrier policy selected |
| WA-IMP-019 | Draft compatibility; Low empty-handle round-trip rejection | Constructor confirmed; generic TBS validity algorithm-dependent | No for full constructor contract |
| WA-IMP-020 | Minimal fixture coverage; Low missing decoder oracle | Extra field confirmed | Yes |
| WA-IMP-021 | Selected-ID test oracle; Low false-positive association | Fallback-to-second confirmed | Yes |
| Whole-ceremony lifetime | Shared lifecycle decision; Medium if concurrent disposal is admitted | Current order confirmed; no runtime race demonstrated here | No |
| Ceremony/platform qualification | Evidence work; no assigned defect severity | Historical/no-run limits retained | No |

### WA-IMP-001 — Enterprise forwarding and Indirect attestation

Enterprise and Indirect currently pass through the returned statement. Enterprise never enters the
backend request, while Indirect has no anonymization service. Medium reflects a failed requested
attestation policy, not proof that an enterprise certificate was leaked. Do not change the separate
None policy: non-self statements are removed; packed/zero-AAGUID/no-certificate self attestation remains.

**BEFORE — `src/WebAuthn/src/Client/WebAuthnClient.Registration.cs`, `BuildMakeCredentialRequest:205–207`:**

```csharp
Options = CtapOptions(decision, requireResidentKey),
PinUvAuthParam = pinUvAuthParam,
PinUvAuthProtocol = pinUvAuthProtocol
```

**Proposed AFTER — schematic; decide native support policy first:**

```text
Enterprise: select permitted native enterprise-attestation mode and capability/RP policy;
            carry it through client request, backend and CTAP encoder before makeCredential
Indirect: select an actual anonymization service OR an explicit unsupported native contract
preserve None/self-attestation behavior and never describe Direct passthrough as anonymization
```

**Python:** P2 is device-configuration precedent only; P0 supplies no local ceremony/anonymization
equivalent. **No-brainer: no.** Acceptance needs exact request bytes for selected modes, unsupported
capability behavior, RP authorization and statement/RawAttestationObject agreement, without automatic
retry after creation. Re-review AttestationPreference, creation options, backend request/forwarding and
attestation guides; coordinate the CTAP enterprise-mode owner instead of inventing a boolean wire flag.

### WA-IMP-002 — Ceremony errors and extension isolation

GetInfo and token acquisition can escape the ceremony mapper. Output work is deliberately outside
UV escalation; moving it into that retry block could create a second credential after an output error.
Per-processor CBOR/InvalidOperation failures are omitted, other failures propagate, and unsupported
previewSign is silently omitted. Medium concerns inconsistent recovery and hidden optional results;
error normalization or strictness is an API decision, not automatically a new security defect.

**BEFORE — `src/WebAuthn/src/Client/WebAuthnClient.Ceremony.cs`, `RunWithUvEscalationAsync:131–136`:**

```csharp
var decision = UvDecisionLogic.Decide(info, preference, auth.PinAvailable, permissions);
await auth.AcquireAsync(decision, cancellationToken).ConfigureAwait(false);

try
{
    return await attempt(decision, cancellationToken).ConfigureAwait(false);
```

**Proposed AFTER — schematic alternatives:**

```text
choose one stage-aware public error contract for discovery, token, command and output failure
preserve caller cancellation and original CTAP status/inner exception
keep output construction OUTSIDE command retry; never rerun creation on output failure
choose strict failure OR documented per-extension omission, including unsupported previewSign
for blob reads, select failed-read output presence separately from assertion success
```

**Python:** P0, no inspected equivalent mapper/processor policy. **No-brainer: no.** Acceptance injects
failure at each stage, malformed/missing previewSign inner output and unsupported capability, proving
exact public outcome and command count. Do not substitute the credential's outer public key for a
missing signing key. Link WA-IMP-016 and shared notification outcome; re-review both public methods,
error types, previewSign codecs, both extension sessions and optional-result/blob guidance.

### WA-IMP-003 — Timeout input and success-race policy

Timeout starts after validation and the synchronous RP callback. It cancels cooperatively; an operation
can return success despite an expired timer. Oversized delay fails through the framework rather than
a selected WebAuthn validation contract. Low is API inconsistency, not a guaranteed unsafe session or
an assertion that cancellation instantly releases hardware.

**BEFORE — `src/WebAuthn/src/Client/CeremonyDeadline.cs`, constructor:37–41:**

```csharp
if (timeout is { } value)
{
    _timer = new CancellationTokenSource(value);
    _linked = CancellationTokenSource.CreateLinkedTokenSource(_timer.Token, caller);
    Token = _linked.Token;
```

**Proposed AFTER — schematic alternatives:**

```text
select accepted input range/default/clamping policy before starting any timer
retain cooperative success-can-win OR reject a completed result when lifetime expired
in either option: drain actual work, clear unpublished sensitive results, preserve caller cancellation
do not extend timer coverage to a blocking RP callback without a separate callback contract
```

**Python:** P0, no equivalent ceremony deadline; synchronous management calls are not a .NET timer
oracle. **No-brainer: no.** Acceptance needs deterministic held-stage success/timeout/caller-cancel
races, delay boundaries and cleanup after unpublished success. Native drain/recovery is a shared
dependency, not solved by a token check. Re-review both Timeout properties, deadline/public exceptions,
PIN waits and the historical five-second-plus-two-second-slack hardware-test assumption.

### WA-IMP-004 — Discouraged-UV hardware applicability

Discouraged is not permission to override `alwaysUv` or a key's mandatory makeCredential verification.
The test normalizes PIN state before asserting UV is clear but does not guard `makeCredUvNotRqd` and
`alwaysUv`. Low is a nonportable oracle; a conforming mandatory-UV device can fail this test correctly.

**BEFORE — `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.IntegrationTests/WebAuthnClientTests.cs`,
`MakeCredential_UvDiscouragedAndNoPinProvided_CreatesNonVerifiedCredential:254–256`:**

```csharp
Assert.False(
    response.AuthenticatorData.UserVerified,
    "User verification must be clear: the relying party asked for UV=Discouraged.");
```

**Proposed AFTER — schematic fixture procedure, not a placeholder skipped test:**

```text
before PIN mutation: record GetInfo, makeCredUvNotRqd, alwaysUv, transport and approved fixture state
execute this no-UV assertion only on the qualified applicable lane
exercise mandatory-UV policy in a separate meaningful lane; record nonapplicability, never as a pass
restore selected credential/PIN state independently and retain primary and cleanup failures
```

**Python:** P1 reads configured capabilities/state but supplies no WebAuthn UV oracle. **No-brainer:
no for the matrix; the explicit capability guard is mechanical.** Acceptance includes fake policy
cases and an approved actual device lane, with UP distinct from a fresh touch. Re-review the test,
NormalizePinAsync prerequisites, UV guidance and qualification below; no state-changing run is authorized.

### WA-IMP-005 — Exact client-data serialization

The JSON is parseable, but short escapes for U+0008/000C/000A/000D/0009 differ from CCDToString's
required four-digit escapes. Other `char.IsControl` handling must also match the exact algorithm,
not a broader Unicode category. The hash binds these bytes. Low is exact-byte conformance/interoperability;
normal fixed ceremony types and URL origins are not evidence of a general exploitable injection.
The low-level builder's arbitrary nonblank type and crossOrigin/topOrigin/surrogate policy remain separate.

**BEFORE — `src/WebAuthn/src/Client/WebAuthnClientData.cs`, `AppendJsonString:169–177`:**

```csharp
case '\n':
    sb.Append("\\n");
    break;
case '\r':
    sb.Append("\\r");
    break;
case '\t':
    sb.Append("\\t");
    break;
```

**Proposed AFTER — schematic exact-encoding rule plus distinct decision:**

```text
emit the pinned CCDToString escapes: quote/backslash, and U+0000..U+001F as four lowercase hex digits
preserve required key order, base64url challenge, crossOrigin and conditional topOrigin
decide strict surrogate/type/context admission in Create versus ceremony validation separately
hash the exact returned UTF-8 bytes; do not assume a generic JSON serializer matches this algorithm
```

**Python:** P0, no local serializer equivalent; do not claim external fido2/JSON byte parity.
**No-brainer: yes for the exact control rule, no for the wider builder contract.** Acceptance compares
literal JSON and SHA-256 vectors for all low controls, quote/backslash, non-ASCII and selected surrogate
policy, through both direct builder and ceremonies. Re-review builder XML/tests and conformance wording.

### WA-IMP-006 — Attestation snapshot versus borrowed views

Decode copies authenticator data but borrows outer CBOR and statement slices. Caller mutation can
make RawCbor, modeled fields and Encode disagree. Medium is conditional integrity/lifetime risk, not
signature/trust verification. A blanket clone without a chosen owner is not the entire response-lifetime fix.

**BEFORE — `src/WebAuthn/src/Attestation/WebAuthnAttestationObject.cs`, `Decode:103–104`:**

```csharp
var offset = cbor.Length - bytesRemainingBefore;
attStmtRawCbor = cbor.Slice(offset, bytesConsumed);
```

**Proposed AFTER — schematic alternatives:**

```text
snapshot contract: establish one owned complete input snapshot BEFORE parsing; slice only that owner
OR explicit borrowed contract: retain required immutable-input lease with a separately selected API
preserve complete unknown outer response bytes in RawCbor; Encode may remain modeled re-encoding
coordinate statement/generated-key views and failure cleanup; parsing is still not trust verification
```

**Python:** P0, no local attestation decoder/owned-view equivalent. **No-brainer: no.** Acceptance mutates
original input after decode and observes all retained raw/statement/authData/Encode surfaces under the
chosen contract, including malformed decode and unknown outer data. Link CTAP-IMP-027/WA-IMP-013;
re-review Decode/RawCbor/Create/Encode and generated signing-attestation ownership guidance.

### WA-IMP-007 — Default Aaguid equality and width

An uninitialized value reports Guid.Empty yet exposes zero bytes and is unequal to itself. Low concerns
value semantics, hash collections and consumers expecting sixteen bytes. It is independent of the
CTAP endian-converter portability question; registration constructs its AAGUID explicitly.

**BEFORE — `src/WebAuthn/src/Cose/Aaguid.cs`, `AsSpan:65`:**

```csharp
public ReadOnlySpan<byte> AsSpan() => _bytes ?? [];
```

**BEFORE — `src/WebAuthn/src/Cose/Aaguid.cs`, `Equals:93–94`:**

```csharp
public bool Equals(Aaguid other) =>
    _bytes is not null && other._bytes is not null && _bytes.AsSpan().SequenceEqual(other._bytes);
```

**Proposed AFTER — schematic; select default-zero carrier first:**

```text
default represents sixteen zero bytes, equal to explicitly constructed Guid.Empty
make span/value/string/equality/operators/hash agree, including default and boxed values
select inline-value storage OR immutable zero fallback with coherent existing storage
keep big-endian transport conversion review under CTAP-IMP-025, not this default fix
```

**Python:** P1 prints external `ctap2.info.aaguid`; no local C# default-struct/equality equivalent.
**No-brainer: no until carrier/hash choice.** Acceptance verifies reflexivity, symmetry, equality/hash
consistency, dictionary lookup and explicit sixteen-byte zero/nonzero vectors. Re-review Aaguid XML/default
advice and tests without claiming big-endian runtime qualification.

### WA-IMP-008 — Observe the actual shared-secret owner

The product owner zeroes its adopted array. The test instead checks a pre-disposal copy and the access
guard, so it cannot detect removal of zeroing. Low is an evidence gap, not a new assertion of a product
leak. Retain the actual owner view rather than create another secret copy.

**BEFORE — `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/Client/WebAuthnBackendTests.cs`,
`EncapsulateAsync_DerivesSharedSecretFromAuthenticatorKeyAgreement_AndZeroesOnDispose:78–82`:**

```csharp
Assert.Contains(secret.SharedSecret.ToArray(), b => b != 0);
var copy = secret.SharedSecret.ToArray();
secret.Dispose();
Assert.Throws<ObjectDisposedException>(() => secret.SharedSecret);
Assert.NotEqual(copy, new byte[copy.Length]); // Checks the pre-disposal copy, not the owner's cleared storage.
```

**Proposed AFTER — literal focused test replacement, unapplied/uncompiled:**

```csharp
var retained = secret.SharedSecret;
using (secret)
{
    bool hasNonzeroByte = false;
    foreach (byte value in retained.Span)
        hasNonzeroByte |= value != 0;
    Assert.True(hasNonzeroByte);
    secret.Dispose();
    Assert.Throws<ObjectDisposedException>(() => secret.SharedSecret);
    foreach (byte value in retained.Span)
        Assert.Equal((byte)0, value);
}
```

The proposed scope disposes the owner even if an assertion fails and creates no additional secret
array. The load-bearing oracle is **retained owner storage after disposal**, not a freed pool read.
**Python:** P0/P1 supply no .NET retained-array erasure equivalent. **No-brainer: yes.** Acceptance must
fail if PinUvSharedSecret.ZeroMemory is removed while the getter guard remains, and keep failure-path
owner disposal. Re-review the test's zeroing summary; do not promote it to native/provider erasure proof.

### WA-IMP-009 — Protocol negotiation and built-in UV capability

The backend creates protocol 2 for token acquisition and PRF key agreement. A legacy command fallback
does not negotiate protocol 1. Built-in UV chooses a token method based on `uv` without full permission-token
capability preflight. Medium is real interoperability failure on affected authenticators; PINless operations
needing neither token nor PRF agreement can still work.

**BEFORE — `src/WebAuthn/src/Client/WebAuthnBackend.cs`, `EnsureProtocolInitialized:287–289`:**

```csharp
if (_protocol is null)
{
    _protocol = new PinUvAuthProtocolV2();
```

**Proposed AFTER — schematic decision-dependent negotiation:**

```text
select supported PIN/UV protocol from actual GetInfo intersection and cache under chosen lifetime
share that selected protocol across token, PRF encryption/authentication and tag-length handling
check configured built-in UV AND permission-token capability before selecting that token method
define Required failure / Preferred fallback and legacy command support without conflating protocols
```

**Python:** P1 calls external ClientPin without a protocol argument; negotiation internals are **not
inspected precedent**. **No-brainer: no.** Acceptance covers V1-only/V2/both/neither, PIN and built-in UV
with/without token capability, PRF request/result lengths and exact command bytes. Link CTAP-IMP-006 /
CTAP-XREF-003 reader/profile work and cache/lifecycle dependencies; re-review backend, UV table,
ExtensionContext, token/PRF and prerequisites. No biometric device qualification is claimed.

### WA-IMP-010 — Preserve the selected credential association

A successful probe can identify a narrowed singleton even when the original WebAuthn list had several
entries. Matcher and response construction both discard that known association if the final descriptor
is absent. Medium concerns an empty match/response ID, per-credential extension association and RP
handoff. Never repair it by guessing the first original descriptor, nor fabricate a discoverable ID.

**BEFORE — `src/WebAuthn/src/Client/Authentication/CredentialMatcher.cs`, `MatchAsync:60–61`:**

```csharp
var firstCredId = firstResponse.Credential?.Id ?? ReadOnlyMemory<byte>.Empty;
results.Add((firstCredId, firstResponse.User, firstResponse));
```

**BEFORE — `src/WebAuthn/src/Client/WebAuthnClient.Authentication.cs`, `BuildAuthenticationResponseAsync:217`:**

```csharp
var credentialId = ctapResponse.Credential?.Id ?? ReadOnlyMemory<byte>.Empty;
```

**Proposed AFTER — schematic; association is settled, edition/error policy needs selection:**

```text
carry successfully probed descriptor / actual singleton request association through MatchAsync
use one resolved ID for MatchedCredential, AuthenticationResponse and credential-specific processing
on allowed CTAP2.0 singleton omission: recover only that known ID
on contradictory descriptor, later-edition omission, dummy-probe miss or unassociated GetNext:
    apply selected malformed-response policy; never guess an original first entry
```

**Python:** P0, no local ceremony matcher/recovery equivalent. **No-brainer: no for the full group.**
Acceptance uses original singleton and original multi-list narrowed to the second ID, final omission,
conflicting/unknown descriptor, dummy miss and discoverable/GetNext fixtures. Assert match ID equals
response ID and actual extension/RP association. Link CTAP structural parsing but keep its owner distinct;
re-review filter/matcher/MatchedCredential/result XML and guide. WA-XREF-001 stays closed.

### WA-IMP-011 — Enforce the verified WebAuthn PRF variant

CTAP hmac-secret has verified and unverified credential PRFs. WebAuthn L3 REC 2026-08-25 §10.1.4
requires the verified one, overriding the caller UV preference when necessary. Both ceremonies currently
pass the unchanged preference. High reflects different derived secrets for the same credential/input
and failure to require the stipulated verification; it is not evidence of a cryptographic primitive break.
Explicit caller Required is a workaround, not acceptance of this policy gap.

**BEFORE — `src/WebAuthn/src/Client/WebAuthnClient.Authentication.cs`, `GetAssertionCoreAsync:118–120`:**

```csharp
var (matches, extensions) = await RunWithUvEscalationAsync(
    info, options.UserVerification, permissions, auth, hasPreviewSign, AttemptAsync, cancellationToken)
    .ConfigureAwait(false);
```

**Proposed AFTER — schematic; coordinated policy, no fabricated permission:**

```text
resolve effective PRF-evaluation UV requirement before decision/token/probe/final request
override to Required for verified hmac-secret evaluation in authentication and creation-time evaluation
do not treat an extra permission bit, salt encryption or advertised support as proof of actual UV
validate actual response UV before publishing derived results under selected failure policy
retain enablement-only versus evaluation distinctions and optional unsupported-extension behavior
```

**Python:** P0, no inspected local PRF/UV-equivalent implementation. **No-brainer: no; sibling/client
policy coordination remains explicitly open.** Acceptance compares same-credential/input vectors across
caller Discouraged/Preferred/Required, PIN/built-in/unavailable UV and creation-time evaluation; no
unverified PRF result may escape. Link WA-IMP-009/013 and CTAP hmac-secret owner. Re-review both ceremonies,
PRF input/processor/result XML, tests, README/CLAUDE, architecture and relevant CTAP guidance together.

### WA-IMP-012 — Credential protection must be enforced before creation

The support filter omits credProtect even for an enforced request. Supported paths forward the policy
integer but do not implement client enforcement. CTAP2.3 §12.1 assigns the platform a **SHOULD NOT**
create without requested policies 2/3 when enforcement is true; this is distinct from the SDK's stronger
requested failure contract. High concerns creation with unexpectedly weaker protection. Checking output
after creation detects a problem but is not equivalent to preventing it.

**BEFORE — `src/WebAuthn/src/Extensions/Processing/RegistrationExtensionSession.cs`, `Create:64`:**

```csharp
if (inputs.CredProtect is not null && context.Supports(ExtensionIdentifiers.CredProtect)) processors.Add(new CredProtectProcessor(inputs.CredProtect));
```

**Proposed AFTER — schematic policy alternatives:**

```text
before makeCredential: evaluate requested policy/enforcement and actual supported candidate
if enforcement cannot be honored: reject this native candidate under selected contract
send only the CTAP policy integer; enforcement is a platform decision, not an authenticator wire flag
validate returned actual policy under accepted stronger-policy ordering, not unconditional equality
define absent/unsatisfactory output error and persistent-credential cleanup without rerunning creation
```

**Python:** P0, no local enforcement equivalent. **No-brainer: no.** Acceptance proves no creation for
unsupported enforced policies, exact weaker/equal/stronger policy behavior, enforcement false and missing
output outcomes. Link the canonical CTAP builder/enforcement dependency rather than count its retained
boolean as a second defect. Re-review input/output, processor/session/codec, CTAP builder and guides.

### WA-IMP-013 — Own and clear sensitive results and temporaries

PRF results are derived secrets; blob/credBlob data can be sensitive application plaintext. The codec
clears aggregate plaintext but creates independent range temporaries and result arrays; result records
have no cleanup owner. ComputeSalt's concatenation retains input, while lower CTAP builders/storage own
additional copies. High reflects residual sensitive material, not observed compromise. Clearing a caller
copy or disposing the client does not clear those originals; directly constructed records may instead
wrap caller-owned memory.

**BEFORE — `src/WebAuthn/src/Extensions/Codec/PrfCodec.cs`, `DecryptResults:79–82`:**

```csharp
OutputLength => new PrfEvaluationResults(plaintext[..OutputLength].ToArray()),
2 * OutputLength => new PrfEvaluationResults(
    plaintext[..OutputLength].ToArray(),
    plaintext[OutputLength..].ToArray()),
```

**BEFORE — `src/WebAuthn/src/Extensions/Inputs/PrfInput.cs`, `ComputeSalt:99–101`:**

```csharp
var hash = new byte[32];
System.Security.Cryptography.SHA256.HashData(data, hash);
return hash;
```

**Proposed AFTER — schematic ownership decision plus mechanical cleanup scope:**

```text
choose sealed disposable result owners / explicit transfer contract for SDK-produced PRF/blob secrets
copy directly from plaintext spans into adopted result storage, without array-range intermediate clones
finally clear aggregate plaintext and ComputeSalt's input concatenation; clear failed result allocations
transfer only on completed response construction; clear unpublished earlier results if a later output fails
trace CTAP builder/storage/authData/report/APDU/SCP copies separately at their actual owners/last use
never zero borrowed caller input or promise native/provider end-to-end erasure
```

**Python:** P0/P1, no .NET mutable owner/erasure equivalent. Framework owner/consumer guidance was
corroborated through Context7 `/dotnet/docs`; it is not execution evidence. **No-brainer: no for the
result API; individual finally/direct-span moves are mechanical.** Acceptance retains actual arrays on
success/disposal/failure, multi-result partial construction and cancelled blob work; verifies caller
input remains untouched and valid unknown response data survives. Link CTAP-IMP-004/027/035 and shared
producer-copy facets. Re-review all PRF/blob/credBlob result/input/client lifetime XML and examples;
WA-IMP-006 snapshot policy remains distinct.

### WA-IMP-014 — Required largeBlob must yield a usable credential

Device capability alone is insufficient for the selected largeBlobKey route: effective `rk` must be
true and registration must return the requested key. Current Required admits a nonresident credential
or missing key and reports false after creation. Medium is an unusable persistent credential, not an
assumption that the native single-device SDK implements browser candidate selection.

**BEFORE — `src/WebAuthn/src/Extensions/Processing/Processors/LargeBlobProcessor.cs`, `ForRegistration:75`:**

```csharp
return new LargeBlobProcessor(input, context, requestKey: supported && requireResidentKey);
```

**Proposed AFTER — schematic; select RK and post-creation policy:**

```text
Required: force effective discoverable creation OR reject incompatible RK request before transmission
require selected route capabilities, request the key with rk:true, validate returned key/support
if key is missing: report selected failure without a second makeCredential; record persistent side effect
Preferred: preserve explicitly selected downgrade/optional-result behavior
```

**Python:** P0, no local WebAuthn required-support equivalent. **No-brainer: no.** Acceptance spans
all resident-key preferences, required/preferred support, capability absence and key absence/invalidity,
with exact preflight command count and response outcome. Link CTAP response validation, WA-IMP-002/013/017;
re-review Support enum/input/output, effective-RK/credProps consistency, processor and tests.

### WA-IMP-015 — Blob writes require one original allowed credential

WebAuthn L3 §10.1.5 checks exactly one element in the **original** allowCredentials for a write.
Narrowing a multi-entry request later does not satisfy this input rule; absent discoverable lists are
not equivalent either. Medium is an unintended write/selection scope, not a claim of writes to every match.

**BEFORE — `src/WebAuthn/src/Extensions/Processing/Processors/LargeBlobProcessor.cs`, `ForAuthentication:93`:**

```csharp
return IsSupported(context) ? new LargeBlobProcessor(input, context, requestKey: true) : null;
```

**Proposed AFTER — schematic settled constraint:**

```text
where original authentication options are available, before backend/probe/token/write side effects:
    if Write is present and original AllowCredentials count is not exactly one: NotSupported
then use that sole credential through normal association and storage flow
do not bypass the check merely because the extension capability is absent or final CTAP list is singleton
```

**Python:** P0, no local equivalent cardinality check. **No-brainer: yes for the exact constraint.**
Acceptance proves zero side-effect calls for null/empty/multiple original lists and one selected-ID write
for the singleton, including empty-but-present write data and unsupported capability. These are workflow
behavior tests, not generic null validation. Link WA-IMP-010/014/017; re-review request/write XML,
authentication session/processor, no-list fixtures and guide.

### WA-IMP-016 — Distinguish missing, empty and failed blob reads

Historical isolated .NET evidence confirmed that the nullable array passed below becomes present empty
memory. That is conversion evidence, not a device read. Missing key, absent stored data, stored empty
blob and failed read are not one state. A CTAP read error can also fail response construction after
assertion success; InvalidOperation follows another isolation path. Medium concerns misleading results
and recovery, with output presence/error policy still to select.

**BEFORE — `src/WebAuthn/src/Extensions/Processing/Processors/LargeBlobProcessor.cs`, authentication `PrepareOutputsAsync:148–150`:**

```csharp
var storage = _context.Backend.CreateLargeBlobStorage(token: null);
var blob = await storage.GetBlobAsync(largeBlobKey, cancellationToken).ConfigureAwait(false);
outputs.LargeBlob = new LargeBlobAuthenticationOutput(Blob: blob);
```

**Proposed AFTER — schematic full policy, explicit conversion shown conceptually:**

```text
initialize the chosen requested-read output container, independently of nullable data
map null byte[] explicitly to null ReadOnlyMemory<byte>?, not implicit array conversion
preserve non-null zero-length data as present empty memory
choose failed-read => present extension / absent blob OR selected typed failure, preserving cancellation
keep unsupported route/missing key meanings separate and coordinate owning plaintext cleanup
```

**Python:** P0, no nullable-memory/read-output equivalent. **No-brainer: no for the full group; explicit
null branching is mechanical.** Acceptance exercises null, empty, populated, missing key, unsupported
route, CTAP failure, InvalidOperation and cancellation, through actual public result construction.
Link WA-IMP-002/013/017; re-review Blob/Written/container optionality, processor/session and guide.

### WA-IMP-017 — Optional direct largeBlob and pinless storage writes

The SDK selects largeBlobKey + authenticatorLargeBlobs; CTAP's alternative direct largeBlob route is
not supported. Those authenticator routes are mutually exclusive. The storage path requires a token
for every write, although CTAP permits an unconfigured-authenticator exception when alwaysUv is not
enabled. This is a **feature-support decision, no assigned defect severity**, not mandatory implementation
of every optional extension or proof that a tested YubiKey produces unsigned direct output.

**BEFORE — `src/WebAuthn/src/Extensions/Processing/Processors/LargeBlobProcessor.cs`, `IsSupported:49–50`:**

```csharp
public static bool IsSupported(ExtensionContext context) =>
    context.Supports(ExtensionIdentifiers.LargeBlobKey) && context.OptionEnabled("largeBlobs");
```

**Proposed AFTER — schematic alternatives; no route selected for implementation:**

```text
retain explicit storage-only/token-required contract OR add separately selected direct-route support
if adding direct route: preserve unsigned assertion outputs in CTAP first, then translate owned results
independently retain token-only writes OR permit the exact unconfigured/alwaysUv-disabled exception
never infer pinless permission from Discouraged alone or mix mutually exclusive device routes
```

**Python:** P0, no local WebAuthn route/pinless-write equivalent. **No-brainer: no.** Acceptance for
retention is explicit supported-profile documentation and reviewed disposition; adding support requires
fake route/capability/unsigned-output/token-policy wire cases and authorized matching hardware if
available. Canonical CTAP-IMP-030 carries raw/key8 preservation and old `audit-v2-20261022-20261031:#4`
/ YESDK-1612; no duplicate bug or historical-device closure. Link WA-IMP-009/013–016 and CTAP storage policy.

### WA-IMP-018 — Honor minimum-PIN input intent or replace its carrier

A nonnull MinPinLengthInput creates a processor even when Requested is false; the codec always requests
true. Low is ignored caller intent. The output reports existing minimum policy for authorized RP IDs
in Unicode code points; it does not set a PIN policy or validate the ceremony's PIN.

**BEFORE — `src/WebAuthn/src/Extensions/Processing/RegistrationExtensionSession.cs`, `Create:66`:**

```csharp
if (inputs.MinPinLength is not null && context.Supports(ExtensionIdentifiers.MinPinLength)) processors.Add(new MinPinLengthProcessor());
```

**Proposed AFTER — schematic alternatives:**

```text
retain reused input and activate only when Requested is true
OR replace it with a separately reviewed presence-only WebAuthn carrier and explicit migration
keep null omission, unsupported capability and unauthorized-RP output absence distinct
```

**Python:** P1 reads GetInfo.min_pin_length, not a WebAuthn requested extension; no equivalent carrier.
**No-brainer: no until false/carrier policy.** Acceptance inspects outgoing extension maps for null/false/
true/supported/unsupported and authorized/unauthorized result fixtures. Re-review aggregate parameter,
processor/codec and reused CTAP input, without importing universal Core/FIDO PIN constraints.

### WA-IMP-019 — Empty previewSign handle round trips

Vendor sign-extension draft v4 allows an empty generated handle, but the signing-params constructor
rejects it. Low is round-trip interoperability. Generic required TBS/argument byte fields do not settle
whether every algorithm accepts zero-length TBS or empty argument encoding. Raw pass-through is not
COSE_Sign_Args validation, and generated attestation remains parsed rather than trust-verified.

**BEFORE — `src/WebAuthn/src/Extensions/PreviewSign/PreviewSignSigningParams.cs`, constructor:78–83:**

```csharp
if (keyHandle.Length == 0)
{
    throw new WebAuthnClientError(
        WebAuthnClientErrorCode.InvalidRequest,
        "previewSign KeyHandle must not be empty");
}
```

**Proposed AFTER — schematic; separate settled handle permission from algorithm policy:**

```text
permit a present empty generated handle and preserve it through signing request encoding
select TBS/argument validation at the chosen algorithm/profile boundary, not a universal empty-byte rule
preserve null-versus-present additionalArgs and caller-owned input lifetime
never infer production ARKG support or trusted attestation from a successful parser round trip
```

**Python:** P0, no local previewSign/draft equivalent. **No-brainer: no for the full constructor contract;
the empty-handle restriction removal is narrow after selection.** Acceptance round-trips generated empty
and nonempty handles through actual codec/backend request, with algorithm-appropriate TBS/args and
unchanged caller storage. Link CTAP-IMP-029/039 experimental profile/accessor owners, not a duplicate
algorithm bug; re-review GeneratedSigningKey, params/exceptions, draft examples and tests.

### WA-IMP-020 — Minimal flags-only inner signing attestation

The inner fixture includes algorithm key 3 although draft v4 locates the selected algorithm in the
outer registration output; inner signing-key attestation supplies flags. Low is a missing minimal
decoder oracle, not proof that the current decoder fails it. Keep outer selected algorithm, public-key
algorithm and policy flags distinct.

**BEFORE — `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/Extensions/PreviewSign/PreviewSignCodecTests.cs`,
`BuildAttestationObject:241–246`:**

```csharp
extensionWriter.WriteStartMap(2);
extensionWriter.WriteInt32(3);  // alg
extensionWriter.WriteInt32(-7);  // ES256
extensionWriter.WriteInt32(4);   // flags
extensionWriter.WriteInt32((byte)flags);
extensionWriter.WriteEndMap();
```

**Proposed AFTER — literal minimal-fixture variant, unapplied/uncompiled; retain a separate extra-field case:**

```csharp
extensionWriter.WriteStartMap(1);
extensionWriter.WriteInt32(4);
extensionWriter.WriteInt32((byte)flags);
extensionWriter.WriteEndMap();
```

**Python:** P0, no local previewSign fixture equivalent. **No-brainer: yes.** Acceptance uses the minimal
inner fixture in actual decode/repack tests, asserting key/handle, flags and outer selected algorithm;
also preserve tolerated extra-field coverage without calling key4 an outer standard optional field or
a device-beta observation. Link CTAP previewSign owner and WA-IMP-019; re-review fixture commentary.

### WA-IMP-021 — Assert returned-ID membership before choosing a key

The two-credential integration test chooses the second verification key for **any** returned ID other
than the first. A bogus or empty ID can pass if the second signature verifies. Low is a false-positive
association oracle; it is separate from, but exposes, WA-IMP-010's empty-ID mechanism.

**BEFORE — `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.IntegrationTests/PreviewSignTests.cs`,
`TwoCredentialAllowList_SignsWithTheSelectedCredentialsEntry:254–256`:**

```csharp
var (returned, message) = response.CredentialId.Span.SequenceEqual(first.CredentialId.Span)
    ? (first, messageA)
    : (second, messageB);
```

**Proposed AFTER — literal focused replacement, unapplied/uncompiled:**

```csharp
bool isFirst = response.CredentialId.Span.SequenceEqual(first.CredentialId.Span);
bool isSecond = response.CredentialId.Span.SequenceEqual(second.CredentialId.Span);
Assert.True(isFirst || isSecond, "Returned credential ID must belong to the original allow list.");
var (returned, message) = isFirst ? (first, messageA) : (second, messageB);
```

**Python:** P0, no local selected-signature/ID test equivalent. **No-brainer: yes.** Acceptance includes
an offline unknown/empty-ID negative oracle and actual selected key/message assertions; an eventual
hardware run requires authorized PIN/UP/credential setup and independent cleanup. Assertion existence
is not experimental ARKG firmware qualification. Re-review test summary and returned-ID evidence.

### WebAuthn whole-ceremony lifetime — linked CORE-B5-C006

**Canonical shared mechanism:** `core-001:CORE-B5-C006`; caller occurrence `CORE-WA-X002`, not a new
Core defect. The client has only a bool guard, and the backend disposes its shared PIN protocol before
awaiting CTAP session disposal. Per-exchange drain cannot protect a later probe/token/GetNext/blob/
output continuation. Medium is conditional unsafe concurrent disposal/admission; current caller-serialized,
fully awaited guidance is accepted and no race experiment ran here.

**BEFORE — `src/WebAuthn/src/Client/WebAuthnClient.cs`, `DisposeAsync:105–109`:**

```csharp
if (!_disposed)
{
    await _backend.DisposeAsync().ConfigureAwait(false);
    _disposed = true;
}
```

**Proposed AFTER — schematic alternatives, no invented lease API:**

```text
retain caller-serialized fully awaited ceremonies with explicit unsupported concurrent disposal
OR select whole-ceremony admission/terminal gate and a shared disposal task
for joined option: close admission; await actual continuations; dispose protocol/session after last use
define callback reentrancy/self-disposal and late-prompt tracking separately; never wait on oneself
keep caller inputs/prompts alive until actual final continuation, including after cancelled wait
```

**Python:** P0's synchronous management delegation supplies no async callback/disposal-join equivalent.
**No-brainer: no.** Acceptance holds probe→final, token, GetNext and output tasks; tests overlapping
dispose/ceremony admission, failed cleanup and callbacks under the selected policy, with exactly one
teardown and no early secret destruction. Link [OATH shared workflow owner](#oath-shared-dependencies),
WA-IMP-003/009/013 and CTAP-X002. Re-review client/backend lifetime XML, both guides, prompts, migration
and construction/ceremony tests. Windows native-owner success does not close this workflow policy.

### Native selected-user disclosure — CTAP-DEC-002 referral

**Canonical owner:** `ctap2-001:CTAP-DEC-002` (CTAP-IMP-026/G-011), occurrence `CORE-WA-X008`.
This is one linked decision, **not a 22nd WA-IMP or an accepted privacy vulnerability**. Source actor
trace is complete: modeled User is retained through cached selection; no SDK RP upload/serializer exists.
The authenticator's no-identifying-fields-without-UV rule and platform picker's only-id return have
different actors/conditions. The maintainer must decide whether native output is raw picker/application
data or an intended platform-to-WebAuthn/browser-equivalent disclosure boundary.

**BEFORE — `src/WebAuthn/src/Client/WebAuthnClient.Authentication.cs`, `BuildAuthenticationResponseAsync:219–220`:**

```csharp
// User from CTAP response can be used directly
var user = ctapResponse.User;
```

**Proposed AFTER — decision-dependent alternatives, not field stripping authorization:**

```text
raw native DTO: preserve modeled User for application picker; application owns RP disclosure serializer
OR intended browser-equivalent output: keep internal picker data separate, apply selected outgoing contract
in either model: independently enforce applicable authenticator UV/privacy response checks in CTAP
name serializer actor and account binding; cached selection alone is not a privacy transformation
```

**Python:** P0, no local browser/native response serializer equivalent. **Severity:** decision work,
no assigned defect severity pending actor/contract selection. **No-brainer: no.** Acceptance names each
actor and permitted output for UV/no-UV, single/multiple accounts and allow/discoverable paths, then
asserts exact selected DTO/serializer fields and RP account binding. Coordinate CTAP's card instead of
duplicating it later. User/descriptor XML resolution remains closed; re-review only chosen policy surfaces.

### WebAuthn ceremony and platform qualification

**Qualification, no assigned defect severity; no-brainer: no.** BEFORE/AFTER code is **N/A**: the task
is source-bound execution evidence, not a patch inferred from test names. Python P0/P1/P2 supplies no
equivalent browser ceremony, native completion or cross-platform evidence.

Pass 1 ran no live WebAuthn ceremony, PIN setup, credential mutation, reset or cross-platform lane.
Later Core/integration managed tests and bounded Ctap2 native results are not a WebAuthn/RP ceremony
matrix. The Windows successor supersedes old built-in synchronous report ownership and satisfies the
new staging-owner source/managed-test prerequisite; it does **not** close actual HID-driver races,
unknown-release retention, external/custom providers, independent returned-report copies or all ABI/OS lanes.
Historical Windows first-open failure and attended 17-pass/1-fail aggregate remain bounded and unexplained.
Reported macOS arm64 JIT FIDO/PCSC at NativeShims 1.18.1 on macOS 15.7.7 is not execution on this pin;
raw Mac artifacts/loaded-image proof, AOT, listener/touch/removal and blocked routes remain separate.

**Future procedure and acceptance:**

1. Select exact approved commands, source/artifacts, devices/firmware, transport, OS/architecture/JIT-AOT,
   PIN/configuration and applicable extension capabilities. Do not normalize a real user's PIN or delete
   broad credential sets merely to enter a test helper. Identify RP challenge/account/signature oracles.
2. Qualify registration/authentication with UP and UV, explicit/prompt PIN, retries/decline/cancellation,
   allow/discoverable selection and selected PRF/credProtect/blob/previewSign flows. Built-in UV requires
   an actual applicable device; advertisement is not proof. Verify RP acceptance independently of CTAP status.
3. Exercise selected timeout/native cancel/drain/terminal/reopen/removal outcomes with human coordination,
   recording late callbacks, cleanup and blocked/skipped paths. SmartCard, HID and native release are
   separate observations; a Completed presence notification precedes typed/output/RP success.
4. Record primary outcome and independent persistent-state cleanup/readback, actual source/command/results
   and limits. Preserve serial 9681620's two-key discovery failure, serial 125's pre-auth/reset `6A88`,
   and the user's separate PIV physical deferrals. No PIV PIN isolation/power-cycle/touch/removal lane or
   F9 material replacement is authorized by this WebAuthn plan.

This consumes the [Windows successor](integrations/windows-upstream-20261113/deferred-findings-successor.md#coordinator-completion-boundary)
without relabeling historical native passes as current WebAuthn qualification. Link original CORE-001
and the [OATH discovery/SCP qualification](#oath-secure-reset-and-discovery-qualification), not duplicate
those failures as WebAuthn protocol bugs. Deeper original platform/source profiles are available on request.

### WebAuthn shared dependencies and Core boundary coverage

These rows are **closure referrals, not additional review-card defect counts**. Each retains the exact
original facet and owner from frozen reconciliation; applicable source repairs/tests belong to that owner.
The linked [OATH shared table](#oath-shared-dependencies) is canonical for already introduced general
workflow, reader, producer-copy and known-member mechanisms. WebAuthn-specific meaning remains below.
A dependency row's AFTER is its stated future procedure/closure boundary; no generic patch is invented.

| Original Core boundary group | Canonical card/referral and remaining facet | Closure procedure and re-review trigger |
| --- | --- | --- |
| `CORE-WA-X001` | Known-member/process exclusion; shared Core claim owner, not new registry bug. Current membership prose corrections retained. | Coordinate disjoint scopes/direct factories/other processes. Qualify native release/admission only in selected lane; reopen factory/Constraints/discovery/raw tiers on grouping or route change. Windows old B4-C001 mechanism superseded; native unknown-release and custom-provider B1-C005 remain open. |
| `CORE-WA-X002` | Whole-ceremony card → `CORE-B5-C006`, CTAP-X002; accepted lifetime prose is not implementation closure. | Decide admission/reentrancy/shared disposal, then held whole-workflow and actual callback/resource tests. |
| `CORE-WA-X003` | WA-IMP-009 plus CTAP-IMP-006 / CTAP-XREF-003 and relevant Core C-C001–C004 reader-owner/profile facets. | Minimum code points, UTF-8 byte maximum, UTF-16 reader units, strict encoding/NFC/final-NUL policy have different owners. Choose responsibility without silently normalizing an existing PIN; test actual encoded/profile bytes and prompt owner transfer. Core OATH presets are not FIDO rules. |
| `CORE-WA-X004` | Notification outcome/callback dependency → CTAP-X020 and Core notification owner; not a new WebAuthn progress API. | Preserve request-failure disabling resolution, uncancelled terminal callback and throwing-logger cleanup concern. Request-only fake intent tests are not native/presence/terminal oracles. Qualify actual outcomes and callback failure/drain; reopen user-interaction and backend cleanup only on selected repair. |
| `CORE-WA-X005` | WA-IMP-003; native cancel/drain/recovery → Core B5-C002/C003, B4-C001 and B6-C003. | Keep timer return policy separate from actual transport recovery/native release. Windows owner supersession is partial; selected driver/failure/reopen outcomes remain unqualified. |
| `CORE-WA-X006` | WA-IMP-006/008/013/016; canonical CTAP-IMP-004/027/035 and Core B6-C002/C006/C007, B5-C005 independent-copy facets. | Select each owner/view/transfer; observe actual retained storage and failure last use. New Windows staging clears do not erase returned reports, CTAP builder/authData, WebAuthn or SCP copies. No end-to-end/native-provider erasure closure. |
| `CORE-WA-X007` | WA-IMP-010 and test WA-IMP-021, distinct from CTAP structural parser owner. | Propagate known narrowed-singleton association through both result sites; preserve edition-specific omission/error tests and resolved WA-XREF-001/CTAP-XREF-001 prose. |
| `CORE-WA-X008` | Native User referral → CTAP-DEC-002 / CTAP-IMP-026. | Actor trace settled; maintainer disclosure/serializer contract still required. No silent field stripping or new vulnerability count. |
| `CORE-WA-X009` | CTAP/Core cache/version/reset owners; no new WebAuthn cache defect. Backend lazy GetInfo and fresh ClientPin state may differ; no facade reset/invalidation. | Select freshness/reset policy at canonical owner and re-review actual consumer decision/transport hints. Detected versus effective versions and cached metadata are not live health, CTAP edition, earliest SCP support or host reachability. Core B6-C015/profile authority and selected SCP qualification remain separate. |
| `CORE-WA-X010` | WA-IMP-007/019; CTAP-IMP-025/029/039 and Core provider/catalog/ARKG owners remain distinct. | Select draft/vendor wire and algorithm dispatch before changes. Keep Core draft-09 math versus CTAP draft-10 representation, seed marker -65700, endian versus default Aaguid, unverified attestation and nonproduction ARKG qualifications. General catalogs/provider routes do not prove strict peer support. |
| `CORE-WA-X011` | WA-IMP-001/002/005/009/011/012/014–019; direct-output dependency CTAP-IMP-030 / old #4. | Each local card covers its original unresolved facet; transport/copy integration closes none of these applet policies automatically. Optional direct-route support is not a mandatory new feature. |
| `CORE-WA-X012` | WA-IMP-004/008/020/021 test cards. Prior request-only, re-mint provenance and single-route comment corrections retained. | Strengthen actual guard/view/minimal-inner/member-ID assertions only under original test owners. Fake Version=2/tagLength=16 is noncryptographic; incomplete GetInfo fakes and successful single-exchange tests do not establish physical/profile/workflow qualification. Re-review helper/test claims after actual assertion/run changes. |

### WebAuthn coverage ledger and exclusions

| Authoritative original identity | Disposition | Canonical coverage |
| --- | --- | --- |
| WA-IMP-001 | Deferred | Enterprise/Indirect card; None/self policy retained |
| WA-IMP-002 | Deferred | Stage mapping, blob-read isolation and unsupported/malformed previewSign card |
| WA-IMP-003 | Deferred | Timeout range/race card plus native recovery referral |
| WA-IMP-004 | Deferred test work | Discouraged-UV applicability card plus approved qualification lane |
| WA-IMP-005 | Deferred | Exact serializer card plus low-level builder/surrogate decision |
| WA-IMP-006 | Deferred | Attestation snapshot/borrowed contract card |
| WA-IMP-007 | Deferred | Default Aaguid semantics card; not CTAP endian closure |
| WA-IMP-008 | Deferred test work | Actual shared-secret retained-view card |
| WA-IMP-009 | Deferred | Protocol negotiation/built-in token preflight card |
| WA-IMP-010 | Deferred | Known selected association through matcher and response card |
| WA-IMP-011 | Deferred coordinated policy | Verified PRF card; Required workaround is not acceptance |
| WA-IMP-012 | Deferred | Pre-creation credProtect enforcement and actual-policy decision card |
| WA-IMP-013 | Deferred | Sensitive result/input/intermediate owners card, narrow CTAP/Core copies referred |
| WA-IMP-014 | Deferred | Required blob effective-RK and missing-key card |
| WA-IMP-015 | Deferred | Original allow-list write cardinality card |
| WA-IMP-016 | Deferred | Absent/empty/failed read-result card; historical isolated conversion evidence retained |
| WA-IMP-017 | Deferred optional-support decision | Direct route and pinless storage alternatives card |
| WA-IMP-018 | Deferred input decision | Requested=false versus presence-only carrier card |
| WA-IMP-019 | Deferred | Empty generated handle and algorithm-specific TBS/args boundaries card |
| WA-IMP-020 | Deferred test work | Minimal flags-only inner fixture card |
| WA-IMP-021 | Deferred test work | Returned-ID membership before verification-key selection card |
| WA-XREF-001 / CTAP-XREF-001 | **Excluded: resolved documentation** | Corrected User fields and descriptor-edition prose retained; recovery/actor decisions remain separate |
| `CORE-WA-X001`–`X012` | All 12 accounted; documentation corrections retained, original runtime/decision/qualification facets open | Individual rows above; no additive Core bug count |
| CTAP-DEC-002 / CTAP-IMP-026 / G-011 | **Open canonical CTAP decision, referred once** | Native User card; no new WA-IMP allocation or automatic stripping |
| CTAP-IMP-004/006/025/027/029/030/035/039; CTAP-XREF-003 | **Open canonical dependencies, not WebAuthn-assigned duplicate fixes** | Ownership, PIN profile/reader, endian, experimental algorithms and unsigned direct output referrals above |
| `audit-v2-20261022-20261031:#4` / YESDK-1612 | **Deferred optional-profile/raw-output remainder** | CTAP-IMP-030 → WA-IMP-017; tested YubiKeys did not produce the direct output, not proof of decoder support |
| Core logging FULL-032 / SAMPLE-WA-002 original clean claim | **Excluded: original sample credit retracted; corrected body accepted/compiled** | Public LoggerFactory route is current; do not reopen fixed accessibility or relabel original CS1929 as wrapper-only failure |
| Windows B4-C001/B4-C007 affected mechanisms | **Partial supersession retained** | Old built-in route replaced; new source/managed staging prerequisite satisfied. Native driver/release and independent report/result-copy qualification remain open |
| Original CORE-001 and protected OATH/PIV facets | **Linked qualification only; no reopened applet fixes** | Original two-key/6A88 failure limits and PIV user physical/material deferrals remain untouched |
| DOC-002 migration YAML failure | **Outside WebAuthn product scope; historical open hygiene retained** | No map edit or closure in this section |

**WebAuthn coverage result:** **21 WA cards (17 product/policy groups and four test groups), one
whole-ceremony shared-lifecycle card and one ceremony/platform qualification card: 23 local review cards**.
One additional **CTAP-DEC-002 referral** is displayed for usable actor context and counted only at its
canonical CTAP owner. All 21 original deferred WA IDs, all 12 Core boundary groups, the 31-key inherited
boundary map and resolved/retracted exclusions are accounted without inventing independent defect totals.
Human review, implementation selection and fresh execution remain unstarted.

## Management

### Context and review map

Management reads device metadata and writes persistent, device-wide configuration. USB/NFC masks replace
whole application sets; null settings are omitted. A 16-byte configuration lock code authorizes each
protected write. It is separate from SCP channel authentication and from applet PINs. A reboot request
can remove interfaces used by other applets; combined reset is documented for Bio Multi-protocol Edition
(MPE), not every key that passes a firmware comparison.

The [Pass-1 triage](modules/management/pass-1/management-001/triage.md) retains 17 implementation/test/policy
groups and two Core metadata dependencies. The [completed Core source-boundary follow-ups](modules/core/pass-1/core-001/boundaries/Management/boundary-completion.md)
add three boundary groups without creating duplicate bugs. Their accepted documentation is retained;
the engineering and qualification facets below remain open.

Selected C# windows were rechecked at **`6ce355468047e8004fd51cf1425808f6e8242ed3`**. Python Management
and its actual config/info CLI counterparts were inspected at **`4ca60f706af930459138d8dc0f0f953480e1c7a4`**;
the local Python worktree is clean. Every `py:` citation in this section uses that pin. Python's
immutable buffers, synchronous CLI and different connection ownership are not .NET cleanup/cancellation
precedent. No new library, device or platform behavior is inferred from comparator agreement.

| Card | Kind and impact-based severity | Certainty | No-brainer |
| --- | --- | --- | --- |
| [MGMT-IMP-001](#mgmt-imp-001--field-specific-nfc-preflight) | Field-support policy; Low | Missing preflight source-confirmed; selected enforcement unresolved | No |
| [MGMT-IMP-002](#mgmt-imp-002--nfc-false-value-and-recovery) | Wire/device qualification; no assigned defect severity | Zero encoding confirmed; device effect unknown | No |
| [MGMT-IMP-003](#mgmt-imp-003--request-width-and-supported-masks) | Request narrowing/reachability risk; Medium | Narrowing confirmed; hardware lockout not observed | No for policy; preventing silent narrowing is mechanical once width is accepted |
| [MGMT-IMP-004](#mgmt-imp-004--test-restoration-and-independent-secret-cleanup) | Test cleanup/state; Medium | Current failure ordering and two default-key owners confirmed | No for group; unconditional zeroing/owner scopes are mechanical |
| [MGMT-IMP-005](#mgmt-imp-005--encoded-buffer-failure-oracle) | Missing failure oracle; no assigned production-defect severity | Production finally exists; only success observed by test | Yes for selected test scope |
| [MGMT-IMP-006](#mgmt-imp-006--actual-operation-transport-coverage) | Coverage mismatch; Low | Unpinned reads confirmed; some other rows already pin correctly | Yes for tests intended to exercise a concrete transport |
| [MGMT-IMP-007](#mgmt-imp-007--negative-test-error-provenance) | False security-test assurance; Medium | Broad exception assertions confirmed; device status unqualified | No until exact intended outcomes are established |
| [MGMT-IMP-008](#mgmt-imp-008--legacy-read-and-initialization-policy) | Support/preflight policy; Low | Unused 4.1 gate confirmed | No |
| [MGMT-IMP-009](#mgmt-imp-009--malformed-firmware-test-filter) | Ignored test criterion; Low | Two-component value and ignore policy confirmed | Yes for local criterion; no for shared policy |
| [MGMT-IMP-010](#mgmt-imp-010--combined-reset-product-and-lifecycle-contract) | Destructive-operation policy risk; High | Family/lifecycle gaps confirmed; out-of-family erasure not observed | No |
| [MGMT-IMP-011](#mgmt-imp-011--meaningful-metadata-and-feature-oracles) | Coverage/qualification; no assigned defect severity | Current metadata-only oracles confirmed | No |
| [MGMT-IMP-012](#mgmt-imp-012--dormant-legacy-mode-wire-choice) | Dormant support decision; Low if enabled unchanged | P1 difference confirmed; no public mode operation | No |
| [MGMT-IMP-013](#mgmt-imp-013--standalone-tool-selection-policy) | Optional tool eligibility policy; Low | SmartCard policy current; unused-selector mechanism removed | No |
| [MGMT-IMP-014](#mgmt-imp-014--command-help-and-reachable-features) | CLI contract mismatch; Low | Trailing arguments ignored; top-level help works | No for feature/argument contract; help correction is mechanical after selection |
| [MGMT-IMP-015](#mgmt-imp-015--lock-prompt-secret-lifetime) | Sample secret retention; Medium | Strings and pre-cleanup confirmation failure confirmed | No for input design; exceptional owner cleanup is mechanical |
| [MGMT-IMP-016](#mgmt-imp-016--tool-cancellation-propagation) | Tool cancellation/error contract; Low | Menu/wrapper gaps confirmed; shared selector facet superseded | No for result policy; token forwarding is mechanical |
| [MGMT-IMP-017](#mgmt-imp-017--request-results-and-executable-labels) | Misleading result/metadata labels; Medium for FIPS, Low for reboot/flags | Current output confirmed; obsolete host-string facet absent | No for reboot API; labels are mechanical |
| [MGMT-XREF-001 referral](#mgmt-xref-001--core-fips-tag-decoding-referral) | Core decoder/filter risk; Medium | Wrong integration and helper width confirmed; normative width gap retained | No for group |
| [MGMT-XREF-002 referral](#mgmt-xref-002--core-flagged-form-factor-referral) | Core representation/filter risk; Low | Flagged enum mismatch confirmed | No |
| [CORE-MGMT-X003](#core-mgmt-x003--bound-factory-and-reset-oracles) | Test-oracle gap; Low | Constructor probe cannot attach; no stale-guard defect asserted | Yes for a genuinely bound behavioral oracle |
| [Management qualification](#management-device-and-platform-qualification) | Device/profile/platform evidence; no assigned defect severity | Historical evidence only; no new execution | No |

### MGMT-IMP-001 — Field-specific NFC preflight

`SetDeviceConfigAsync` checks only the operation's 5.0 floor. A caller can request restriction below
5.7 or on hardware without NFC. Low reflects inconsistent unsupported-field failures, not a demonstrated
security bypass. Decide whether the SDK preflights both true and explicit false, or documents a deliberate
device-authoritative route. Do not interpret an absent NFC application mask as proof of absent hardware.

**BEFORE — `src/Management/src/ManagementSession.cs`, `SetDeviceConfigAsync`, lines 171–173:**

```csharp
ThrowIfDisposed();
EnsureSupports(FeatureSetConfig);
ArgumentNullException.ThrowIfNull(config);
```

**Proposed AFTER — decision-dependent flow:**

```text
if NfcRestricted is present:
    selected strict policy: check effective 5.7 firmware and established NFC hardware evidence
    OR selected device-authoritative policy: transmit and retain layer-specific rejection
keep null omission, overrides and major-zero development policy explicit
```

**Python:** `py:ykman/_cli/config.py:501–521`, `nfc`, checks NFC transport and firmware before true
restriction; `py:yubikit/management.py:679–692`, `write_device_config`, has only the generic write gate.
This is CLI policy precedent, not proof the library enforces field support or that false restores NFC.

**Acceptance/dependencies:** exact true/false/null wire/no-wire cases on accepted firmware/hardware
evidence, ordinary rejection provenance, and no hidden stale-cache substitution. Coordinate Core feature
policy and IMP-002. Re-review `DeviceConfig.NfcRestricted`, session/interface XML, requirements/restriction
guides, contributor instructions, active migration and tool/test support claims after contract selection.

### MGMT-IMP-002 — NFC false-value and recovery

False emits `17 01 00`; null emits no restriction TLV; true emits `17 01 01`. Only the encoding is
established. The existing test reads back true over SmartCard, then sends its cached starting bool without
restoration readback or an NFC access check. It does not prove that false re-enables NFC.

**BEFORE — `src/Management/src/DeviceConfig.cs`, `GetBytes`, lines 203–204:**

```csharp
if (NfcRestricted is { } nfcRestricted)
    tlvs.Add(new Tlv(TagNfcRestricted, [nfcRestricted ? (byte)0x01 : (byte)0x00]));
```

**Proposed AFTER — code N/A until evidence/intent selection.** Either retain explicit false with a
qualified effect, omit false by accepted policy, or reject an unsupported request. None is selected by
this draft. Qualification has no assigned defect severity and is not a no-brainer implementation change.

**Procedure/acceptance:** obtain a versioned firmware definition or an authorized NFC-capable 5.7+
true/false/null matrix. Record starting restriction, transport, USB-power duration and any reboot request
independently. Check reported state, actual NFC application access and retained NDEF URL. Use documented
three-second USB-power recovery, fresh scopes and independent cleanup; no invented touch/authentication
prerequisite. Stop if restoration cannot be established. Tests must not depend on false for recovery
before that effect is qualified.

**Python:** `py:yubikit/management.py:272–273`, `DeviceConfig.get_bytes`, emits true only; false/null are
omitted. `py:ykman/_cli/config.py:515–525`, `nfc`, requests true without reboot and names USB-power recovery.
Omission is not evidence that zero is invalid or effective. Re-review property/guide, capability-test
restoration and tool UI with IMP-001/004 only after selecting the supported contract.

### MGMT-IMP-003 — Request width and supported masks

Validation rejects exactly USB `None`, before narrowing to a 16-bit wire value. For example,
`(DeviceCapabilities)0x10000` is nonzero but encodes USB `00 00`; flags also narrow to one byte. Medium
reflects a source-derived possibility of sending an unintended replacement set, not observed hardware
lockout. Unknown in-width bits, supported-device checks and preserving a reachable application are
different decisions; do not reject every unknown bit merely to fix lossy narrowing.

**BEFORE — `src/Management/src/DeviceConfig.cs`, `Validate`, lines 162–163; `GetBytes`, lines 185–189:**

```csharp
if (UsbEnabled == DeviceCapabilities.None)
    throw new InvalidOperationException("At least one USB capability must be enabled.");
```

```csharp
if (UsbEnabled is { } usb)
    tlvs.Add(new Tlv(TagUsbEnabled, UInt16BigEndian((ushort)usb)));

if (NfcEnabled is { } nfc)
    tlvs.Add(new Tlv(TagNfcEnabled, UInt16BigEndian((ushort)nfc)));
```

**Proposed AFTER — alternatives, no invented validation API:**

```text
reject negative/out-of-wire-width masks and flags before numeric conversion
preserve unknown in-width bits OR restrict them under an explicit request policy
device-aware option: reject unsupported requested apps and empty supported USB intersection
OR retain caller responsibility, but never silently narrow a nonzero request to zero
```

**Python:** `py:yubikit/management.py:258–269`, `get_bytes`, requests integer encodings rather than C#
casts; `py:ykman/_cli/config.py:270–295`, `_configure_applications`, checks supported masks, preserves
unchanged bits and prevents an empty named USB set. Those are CLI safeguards, not a universal SDK policy.

**Acceptance:** exact bytes/no transmission for narrowing cases, NFC None versus null, unknown in-width
bits, negative/all-bits values and replacement masks; use independent supported metadata fixtures, not
the faulty FIPS decoder. Coordinate Core wider capability/raw representation (A-C006/C016), IMP-001 and
tool choices. Re-review model/Validate/session XML, read-modify-write/recovery examples, active migration
and audit-test claims. No-brainer for the whole policy: **no**.

### MGMT-IMP-004 — Test restoration and independent secret cleanup

Lock-code zeroing follows fallible device cleanup. If the removal write or readback fails, the test-owned
code remains uncleared; some simple timeout tests skip restoration entirely after a failed assertion.
Medium covers test-secret lifetime and persistent fixture contamination. The undisposed SCP03 defaults
are **public test keys**, not observed private-key disclosure or a production session-key leak.

**BEFORE — `src/Management/tests/Yubico.YubiKit.Management.IntegrationTests/ManagementLockCodeTests.cs`,
`LockCode_ConfigChangeWithLockCode_Succeeds`, lines 135–140:**

```csharp
await mgmt.SetDeviceConfigAsync(
    restoreConfig,
    new SetDeviceConfigOptions { CurrentLockCode = lockCode, NewLockCode = clearCode });

// A preceding cleanup failure skips this zeroing; it needs an independent finally.
CryptographicOperations.ZeroMemory(lockCode);
```

`LockCode_SetAndClear:79–88` has the same ordering, with a fallible read/assert before zeroing.
`ManagementSessionSimpleTests.SetDeviceConfigAsync_WithManagementSession:152–160` restores only after
the changed-value assertion; the extension variant `179–187` likewise lacks finally/readback.

**BEFORE — `src/Management/tests/Yubico.YubiKit.Management.UnitTests/IYubiKeyExtensionsTransportTests.cs`,
`CreateManagementSessionAsync_ScpRequestedAndCcidHeld_DoesNotFallBackToPlaintextHid`, lines 182–184:**

```csharp
_ = await Assert.ThrowsAsync<ConnectionInUseException>(() =>
    device.CreateManagementSessionAsync(
        new SessionCreationOptions { ScpKeyParameters = Scp03KeyParameters.Default }, Ct));
```

The second default owner is `ManagementSessionAdvancedTests.ModernFeatures:73–77`, an inline
`scpKeyParams: Scp03KeyParameters.Default`. SimpleTests' successful SCP case already uses a local using
scope. Keep those paths distinct; a session borrows parameters and cannot fix missing caller disposal.

**Proposed AFTER — selected cleanup structure, schematic:**

```text
own SCP parameters before the consuming session/helper; dispose after its actual completion
read/save each field to change; preserve a usable recovery transport
try change + meaningful assertions
finally try restoration + fresh restored-field readback
        finally clear operation-owned secret inputs, even if restoration/readback throws
retain both primary and cleanup failures; preserve a recoverable lock code securely until removal succeeds
```

**Python:** `py:yubikit/management.py:665–692`, `write_device_config`, accepts per-command codes;
`py:ykman/_cli/config.py:221–233`, `set_lock_code`, reports the write outcome but supplies no .NET
finally/disposable-test oracle. Do not copy immutable Python secret storage or generated-code output.

**Acceptance/dependencies:** inject failed mutation/assertion/restoration/readback and observe actual
owned arrays independently; prove default-key scopes end on failed open too. For hardware, record
restored fields and failed recovery explicitly, retaining the code in the selected secure recovery route
without leaving unmanaged test buffers indefinitely. IMP-002 governs NFC recovery; Core B5-C006 governs
whole-task drain and B6-C007/C009 parameter aliases. Re-review lock/simple/capability/SCP test comments,
test guidance and cleanup/restoration claims after the selected repair. No new hardware run is authorized.

### MGMT-IMP-005 — Encoded-buffer failure oracle

Production already clears its encoding after actual backend completion in finally (`ManagementSession.cs:
194–202`). The test observes only a successful write. This is missing failure/cancellation evidence,
not a source-confirmed production leak; no production-defect severity is assigned.

**BEFORE — `src/Management/tests/Yubico.YubiKit.Management.UnitTests/ManagementSessionTests.cs`,
`SetDeviceConfigAsync_ZeroesEncodedConfigAfterBackendWrite`, lines 99–105:**

```csharp
await session.SetDeviceConfigAsync(
    config,
    new SetDeviceConfigOptions { CurrentLockCode = lockCode },
    cancellationToken: TestContext.Current.CancellationToken);

Assert.True(backend.SawNonZeroConfig);
Assert.True(backend.CapturedConfig.Span.ToArray().All(static b => b == 0));
```

**Proposed AFTER — test flow, no production change assumed:**

```text
backend retains the actual borrowed encoding, then holds/fails/cancels its write
while held: encoding remains valid; only after actual task termination: entire encoding is zero
source lock-code array remains unchanged by the session; test owner clears it independently
assert original failure/cancellation and cleanup, without observing a clone as the cleanup oracle
```

**Python:** `py:yubikit/management.py:690–692`, `write_device_config`, forwards immutable bytes; no
equivalent retained-array erasure test/guarantee. Local behavior-test expansion is a **no-brainer** once
the above narrow scope is selected. Acceptance must cover both current/new code TLVs and failure after
borrow, not only argument validation/pre-cancellation. Coordinate IMP-004 and Core independent producer
copies; reopen zeroing-test summary and ownership/security claims without promising end-to-end erasure.

### MGMT-IMP-006 — Actual operation transport coverage

A `WithYubiKey` connection filter identifies a device exposing an interface; it does not choose the
session transport. Management prefers SmartCard, then FIDO HID, then OTP HID. An HID-labeled one-shot
read on a composite device may therefore use SmartCard. Low reflects false interface coverage.

**BEFORE — `src/Management/tests/Yubico.YubiKit.Management.IntegrationTests/ManagementSessionSimpleTests.cs`,
`GetDeviceInfo_WithHidFidoDevice_ReturnsValidInfo`, lines 100–104:**

```csharp
Assert.Equal(ConnectionType.HidFido, state.ConnectionType);

var deviceInfo = await state.Device.GetDeviceInfoAsync();
Assert.NotEqual(0, deviceInfo.SerialNumber);
Assert.Equal(state.SerialNumber, deviceInfo.SerialNumber);
```

**Proposed AFTER — test flow:**

```text
for a concrete transport row, pass that concrete preference to session/helper/one-shot creation
for session/helper tests, assert the session's actual ConnectionType before the live read
for one-shot tests, verify actual selected connection through the existing routing seam
```

**Python:** `py:yubikit/management.py:604–615`, `ManagementSession.__init__`, dispatches on the connection
already supplied. `py:ykman/_cli/config.py:92–102`, `config`, uses a different order and retries failed
opens; do not transplant that fallback policy into C# or treat device eligibility as transport proof.

**Acceptance:** inspect/fix the corresponding OTP `SimpleTests:124–130` and Advanced `CcidOnly:399–411`
and performance helper intent. Existing typed SmartCard/FIDO creation, pinned HID reset rejection and
`AdvancedTests.GetDeviceInfo_AllTransports:422–439` already pin/assert appropriately: retain them, not
duplicate fixes. Assert actual per-row wire/identity, never infer one physical key across independently
bound rows. Re-review simple/advanced summaries and transport/helper coverage reports. No-brainer: **yes**
for tests whose accepted purpose is concrete-interface coverage.

### MGMT-IMP-007 — Negative-test error provenance

The missing-lock and wrong-SCP tests accept any exception. Unrelated discovery, initialization or
transport failure can satisfy the supposed security assertion. Medium is test assurance, not a proven
authentication bypass. A guessed universal status would merely replace one weak oracle with another.

**BEFORE — `src/Management/tests/Yubico.YubiKit.Management.IntegrationTests/ManagementLockCodeTests.cs`,
`LockCode_SetAndClear_ProtectsConfigurationChanges`, lines 66–67:**

```csharp
await Assert.ThrowsAnyAsync<Exception>(async () =>
    await mgmt.SetDeviceConfigAsync(configWithoutCode));
```

`ManagementSessionSimpleTests.CreateManagementSession_WithScp03WrongKeys:235–241` also uses
`ThrowsAnyAsync<Exception>` around creation. Wrong bytes do not identify the failed handshake phase.

**Proposed AFTER — error-oracle flow:**

```text
establish successful supported initialization/current lock state or known provisioned SCP setup
inject/observe the intended missing-code or wrong-key command failure
assert exact layer, raw device status when available, phase and preserved inner cause
an unrelated transport/probe failure must fail the test, not satisfy the negative assertion
```

**Python:** `py:yubikit/management.py:560–564`, `_ManagementSmartCardBackend.write_config/device_reset`,
passes through APDU outcomes; `py:ykman/_cli/config.py:230–233`, `set_lock_code`, catches broad failures
and adds a “Wrong current code?” hint. That hint is not device provenance and should not become an oracle.

**Acceptance/dependencies:** meaningful positive controls plus deliberately unrelated failure vectors,
actual command evidence and independent cleanup under IMP-004. Core B6 setup/wire-status distinctions
remain canonical; historical `6A88` before EXTERNAL AUTHENTICATE is not executed reset or wrong-user-code
proof. Re-review negative-test summaries and authentication/configuration error guidance after selecting
and qualifying the expected outcomes. No-brainer: **no** until those outcomes are known.

### MGMT-IMP-008 — Legacy read and initialization policy

`FeatureDeviceInfo` declares 4.1.0 but neither public read nor factory probe enforces it. Current prose
correctly calls it a support minimum, not guaranteed host rejection. Low reflects predictable-support
policy/error inconsistency, not an observed device defect. Adding a read gate inside the shared reader
would also change discovery and the factory's best-effort probe; choose these boundaries separately.

**BEFORE — `src/Management/src/ManagementSession.cs`, `GetDeviceInfoAsync`, lines 156–160:**

```csharp
ThrowIfDisposed();
var protocol = Protocol ?? throw new InvalidOperationException("Management session is not initialized.");
// Query current fields while retaining initialization's firmware default; non-final qualifier
// precedence belongs to DeviceInfo.CreateFromTlvs. This read does not publish discovery metadata.
return DeviceInfoReader.ReadAsync(protocol, _version, cancellationToken);
```

**Proposed AFTER — unselected alternatives:**

```text
public read policy: effective FeatureDeviceInfo preflight OR documented device-authoritative read
factory probe policy: keep best-effort metadata discovery OR skip below established detected floor
define override/major-zero development cases without skipping mandatory transport preparation
do not silently impose the public applet gate on Core discovery
```

**Python:** `py:yubikit/management.py:642–645`, `read_device_info`, explicitly checks 4.1; its internal
`_do_read_device_info` does not. Initialization specially probes `(0,0,1)` (`617–619`). Applicable public
versus internal distinction; not equivalent to C#'s always-attempted initial metadata or override policy.

**Acceptance/dependencies:** exact probe/read/no-wire behavior for below-floor, supported, overridden
and development firmware; initial metadata failure/cancellation, tag/default/non-final qualifier
precedence and later read error provenance. Core Feature and reader/cache policy remain separate owners.
Re-review read XML, requirements/table, initialization and legacy tests. No-brainer: **no**.

### MGMT-IMP-009 — Malformed firmware test filter

The local test uses `"5.0"`, which the three-component parser does not parse. Shared filtering silently
ignores the null parse result. The first row can bind an older device and fail its intended modern-or-
USB-A assertion. Low is accidental test selection; strict handling for all consumers is a separate policy.

**BEFORE — `src/Management/tests/Yubico.YubiKit.Management.IntegrationTests/ManagementSessionTests.cs`,
`MultipleConfigurations_TestRunsOnMatchingDevices` attribute, line 138:**

```csharp
[WithYubiKey(MinFirmware = "5.0")]
```

**Proposed AFTER — literal local correction, unapplied:**

```csharp
[WithYubiKey(MinFirmware = "5.0.0")]
```

**Python:** N/A for .NET declarative fixture binding. `py:yubikit/management.py:644`, `read_device_info`,
uses a three-component numeric requirement; it is not evidence for ignoring malformed C# strings.
**Acceptance:** exercise the real selection/filter path with older and modern candidates, independently
bound alternate form-factor row and actual encoded/test intent—not a validation-only parser test. The
local correction is a **no-brainer**. Canonical Core A-C014 / Tests.Shared owns reject-versus-ignore
policy (`YubiKeyTestInfrastructure.FilterDevices:79–83`); if selected, preserve diagnostic/selection
provenance and re-review shared attribute/filter XML and module coverage claims. Retain resolved XREF-003
binding/cache explanations.

### MGMT-IMP-010 — Combined reset product and lifecycle contract

Reset checks effective firmware 5.6 and SmartCard, but not Bio MPE applicability. Success does not retire
readiness, refresh metadata or establish a new SCP channel. High is the risk of offering a destructive
operation on misleading eligibility/erasure claims; no out-of-family erasure has been demonstrated.
Configuration-lock recovery and general Management setting erasure remain unknown, not promised effects.

**BEFORE — `src/Management/src/ManagementSession.cs`, `ResetDeviceAsync`, lines 209–215:**

```csharp
ThrowIfDisposed();
EnsureSupports(FeatureDeviceReset);
return _backend switch
{
    SmartCardBackend smartCard => smartCard.DeviceResetAsync(cancellationToken).AsTask(),
    _ => throw new NotSupportedException("Device reset is only available over SmartCard (CCID) connections.")
};
```

`DeviceReset.IsResetSupported:47–49` compares firmware only. Unified
`src/Cli.Commands/src/Management/ManagementResetCommand.cs:32–33` advertises SmartCard and both HID
transports despite SDK HID rejection; its confirmation at `56–57` includes configuration erasure.
These caller facets belong here, not to an independent generic reset defect.

**Proposed AFTER — alternatives requiring a product/lifecycle decision:**

```text
offer reset only for selected authoritative Bio MPE evidence and an actual SmartCard route
OR retain expert device-authoritative dispatch with explicit scope and unsupported-family errors
confirmed success: terminal dispose/reopen OR selected explicit recovery contract
uncertain outcome: preserve primary error and avoid claiming readiness or completed erasure
never infer Management lock recovery; never import OATH's already-settled reset policy automatically
```

**Python:** `py:yubikit/management.py:756–767`, `device_reset`, checks backend type, not family or terminal
state. `py:ykman/_cli/config.py:118–152`, `reset`, checks CCID and uses reset-blocked metadata or Bio plus
PIV support as eligibility clues. This is useful UI precedent, not normative authority for those clues
on C#'s current decoder or for its broad “all data” output.

**Acceptance/dependencies:** accepted product evidence, overrides/development version, HID no-reset-wire,
exact command/status and confirmed/uncertain/failed cleanup state. Qualification must check combined
FIDO/PIV/shared-PIN effects and retained F9 at the authorized device scope; do not replace its attestation
material. Distinguish reset sent from reset completed and native release/re-enumeration. Core B5-C006,
B6-C011/C012/C015, metadata XREFs and the qualification card apply. Re-review reset API/helper/menu,
unified command eligibility/confirmation, features/security, active migration and lifecycle docs together.
No-brainer: **no**.

### MGMT-IMP-011 — Meaningful metadata and feature oracles

Bio rows assert reported form factor/firmware, not biometric operations. FIPS rows compare the same
decoded masks used to select them; “comprehensive” tests read USB/NFC booleans without asserting them.
This is coverage/qualification debt, not a proven missing SDK feature or certification defect.

**BEFORE — `src/Management/tests/Yubico.YubiKit.Management.IntegrationTests/ManagementSessionAdvancedTests.cs`,
`BiometricFeatures_BioKeys_HaveExpectedCapabilities`, lines 95–99:**

```csharp
// Compare the reported form factor with the row's filter.
Assert.Equal(FormFactor.UsbABiometricKeychain, deviceInfo.FormFactor);

// Bio keys typically have modern firmware
Assert.True(deviceInfo.FirmwareVersion.IsAtLeast(5, 0, 0));
```

**Proposed AFTER — code N/A until the intended feature/evidence scope is selected.** Select independent
metadata vectors at Core (including FIPS/flagged form factors, qualifier/components and optional fields)
and actual applet-operation tests at their owning modules. Preserve useful consistency tests under
accurate names; do not add skipped biometric placeholders or equate reported approval with certification.

**Python:** `py:yubikit/management.py:315–317`, `DeviceInfo._is_bio`, is a form-factor predicate;
`375–380` parses FIPS masks, not approved operations. `py:ykman/_cli/info.py:197–205`, `info`, displays
reported application approval. None is a biometric/certification oracle.

**Acceptance/procedure:** inventory the exact intended claims before adding assertions; independent
expected metadata bytes and selected supported operation outcomes, device/firmware/transport/state,
authorization and cleanup when executing. AdvancedPiv `193–211` currently accepts PIV over USB **or** NFC
in its filter but asserts USB PIV: choose a USB-specific criterion or an either-transport assertion,
not blanket “fully supported” credit. Core XREF-001/002 and X002 ownership must close their own facets.
Re-review Bio/FIPS/property summaries and module coverage reports. Severity: **none assigned**;
no-brainer: **no**.

### MGMT-IMP-012 — Dormant legacy mode wire choice

The internal SmartCard mode backend uses P1 zero; Python uses `0x11` for non-NEO Management. There is
no current public C# legacy mode operation, so this is a dormant support decision, not a public regression.
Low applies only if the path is enabled unchanged. Select retention/removal/legacy support before repair.

**BEFORE — `src/Management/src/Backend/SmartCardBackend.cs`, `SetModeAsync`, lines 64–68:**

```csharp
Cla = 0,
Ins = InsSetMode,
P1 = 0,
P2 = 0,
Data = data
```

**Proposed AFTER — conditional literal only if non-NEO Management legacy support is selected:**

```csharp
Cla = 0,
Ins = InsSetMode,
P1 = 0x11,
P2 = 0,
Data = data
```

**Python:** `py:yubikit/management.py:511–555`, `P1_DEVICE_CONFIG` and
`_ManagementSmartCardBackend.set_mode`, uses `00 16 11 00` for non-NEO and a different OTP applet command
for NEO. `ManagementSession.set_mode:743–752` packs legacy `<BBH`; modern firmware instead translates to
DeviceConfig. A one-byte change is not a complete public legacy implementation or NEO parity claim.

**Acceptance/dependencies:** if selected, establish version/app-selection and four-byte little-endian
mode payload intent, exact command vectors and qualified legacy devices; otherwise explicitly retire
the dormant primitive without affecting modern TLV writes. Coordinate OTP's canonical legacy-relocation
gap and Core APDU profile, then re-review backend summary and any future mode API/migration explanation.
Preserve point-in-time migration snapshots. No-brainer: **no**.

### MGMT-IMP-013 — Standalone tool selection policy

The old unused all-transport `ManagementDeviceSelector` is absent. The active standalone adapter now
delegates to the shared selector but **deliberately preserves SmartCard eligibility and RequireChoice**.
HID-only keys remain ineligible for this tool despite SDK support. That is Low optional product scope,
not a confirmed selection bug; do not widen a reset-capable tool because the SDK supports HID.

**BEFORE — `src/Management/examples/ManagementTool/Cli/Prompts/DeviceSelector.cs`, `Selector`, lines 15–16:**

```csharp
private static readonly SharedSelector Selector = new(
    [ConnectionType.SmartCard], NonInteractiveSelection.RequireChoice);
```

**Proposed AFTER — policy alternatives, not a new selector API:**

```text
retain intentional SmartCard-only standalone tool; record accepted non-goal for HID-only keys
OR choose per-task eligibility: metadata/config permit all supported transports, reset only SmartCard
retain complete composite discovery, explicit multiple-device choice and single-open/no-fallback SDK policy
```

**Python:** `py:ykman/_cli/config.py:66–103`, `config`, advertises three connection types and retries in
SmartCard → OTP → FIDO order; reset additionally requires CCID (`128–129`). Applicable task distinction,
not C# fallback or discovery-scope precedent.

**Acceptance:** hardware-free HID-only/composite/multiple-device/no-match cases through the current
shared selection seam, actual later task transport and no reset offer over HID. If SmartCard scope is
accepted, close the optional expansion by explicit policy—not by calling HID metadata unsupported in
the SDK. Re-review module/tool selection/no-device instructions only on scope change. Removed unused
selector mechanism is **superseded**; remaining policy is **unselected**. No-brainer: **no**.

### MGMT-IMP-014 — Command help and reachable features

The program recognizes info/config and top-level `-h`, `--help`, `help`, but uses only `args[0]`.
`info --help` runs info despite usage promising command help. NFC restriction has a helper but no
menu/dispatch action. Low is CLI mismatch and optional feature reachability, not missing SDK support.
Preserve the C-MOD-R4 erratum: “only two recognized tokens” was never the correct baseline claim.

**BEFORE — `src/Management/examples/ManagementTool/Program.cs`, `DispatchCliAsync`, lines 23–31:**

```csharp
var command = args[0].ToLowerInvariant();

return command switch
{
    "info" => await InfoCommand.ExecuteAsync(),
    "config" => await ConfigCommand.ExecuteAsync(),
    "-h" or "--help" or "help" => PrintUsageAndReturn(),
    _ => PrintUnknownCommand(command)
};
```

**Proposed AFTER — unselected CLI alternatives:**

```text
implement command-specific help/argument rejection before device discovery
OR explicitly support no trailing options and remove the false command-help instruction
expose an evidence-qualified NFC restriction task OR accept/document the helper as programmatic-only
```

**Python:** `py:ykman/_cli/config.py:115–118,157–174,487–525`, `reset`, `set_lock_code`, `nfc`, declares
distinct actual commands and a reachable restrict option with support checks. The Click declarations
are task-shape precedent only; no new Click/Spectre API or help execution was inspected here.

**Acceptance/dependencies:** table-driven args dispatch, no I/O for help/invalid arguments, truthful
exit/help text and optional feature routing. Preserve working top-level help. IMP-001/002 governs any
new restriction task; IMP-016 governs cancellation. Re-review executable help, command/menu/task guide
and feature-helper examples after intent selection. No-brainer for the whole group: **no**.

### MGMT-IMP-015 — Lock prompt secret lifetime

The sample obtains immutable secret strings and creates normalized string copies; `.Secret()` masks
display, not memory. In confirmation, the second prompt can throw before cleanup owns the first parsed
16-byte array. Medium reflects host-side sample secret retention, not demonstrated extraction or SDK
wire disclosure. Existing FixedTimeEquals is correct and must not be replaced by ordinary comparison.

**BEFORE — `src/Management/examples/ManagementTool/Cli/Prompts/LockCodePrompt.cs`,
`PromptForNewLockCode`, lines 106–116:**

```csharp
var lockCode = PromptForLockCode(prompt);
if (lockCode is null)
{
    return null;
}

AnsiConsole.WriteLine();
var confirmInput = AnsiConsole.Prompt(
    new TextPrompt<string>("[blue]Confirm lock code:[/]")
        .Secret()
        .AllowEmpty());
```

`PromptForLockCode:75–78` also uses a string prompt; `TryParseHex:159–160` makes replacement/uppercase
copies. Clearing returned arrays does not clear these strings.

**Proposed AFTER — input design decision, not a new SDK API:**

```text
acquire bounded hex input into clearable storage; parse without immutable secret strings
place first result under cleanup ownership before any confirmation/prompt/output that can throw
fixed-time compare decoded bytes; transfer only the accepted result to caller
clear first result unless transferred, confirmation and input scratch on all outcomes
define backspace, separators, overflow, cancellation and noninteractive behavior explicitly
```

**Python:** `py:ykman/_cli/config.py:203–213`, `set_lock_code`, uses hidden string prompts;
`575–582`, `_parse_lock_code`, converts from hex. This corroborates CLI UX, **not safe-memory precedent**;
its generated-code printing (`195–201`) is not a practice to copy.

**Acceptance/dependencies:** actual retained storage after invalid parse, mismatch, prompt exception,
output exception, cancellation and successful ownership transfer; caller clears the transferred result
after the actual configuration task. Design stays bounded and auditable; use Core's selected credential
reader/owner contract where appropriate rather than inventing another universal PIN profile. Re-review
prompt XML, sample security/lock procedure and caller cleanup examples. No-brainer: **no** for design,
**yes** for protecting existing byte owners before fallible confirmation once scope is selected.

### MGMT-IMP-016 — Tool cancellation propagation

Interactive callbacks discard the console token and wrappers turn OperationCanceledException into
generic failure. Low reflects an ineffective/ambiguous tool cancellation request; accepted configuration
may already persist, so cancellation never implies rollback. The shared selector's old swallow facet
is **superseded**: current `EnsureInfoAsync:197–200` excludes OperationCanceledException from its catch.

**BEFORE — `src/Management/examples/ManagementTool/Program.cs`, `RunInteractiveAsync`, lines 50–57:**

```csharp
.AddItem("Device Info", ct => DeviceInfoMenu.RunAsync())
.AddItem("USB Capabilities", ct => CapabilitiesMenu.RunAsync(Transport.Usb))
.AddItem("NFC Capabilities", ct => CapabilitiesMenu.RunAsync(Transport.Nfc))
.AddItem("Timeouts", ct => TimeoutsMenu.RunAsync())
.AddItem("Device Flags", ct => DeviceFlagsMenu.RunAsync())
.AddItem("Lock Code", ct => LockCodeMenu.RunAsync())
.AddItem("Factory Reset", ct => ResetMenu.RunAsync())
.RunAsync(cts.Token);
```

**BEFORE — `src/Management/examples/ManagementTool/Features/DeviceConfiguration.cs`,
`SetCapabilitiesAsync`, lines 84–87:**

```csharp
catch (Exception ex)
{
    return ConfigResult.Failed($"Failed to set capabilities: {ex.Message}");
}
```

**Proposed AFTER — result policy alternatives:**

```text
forward menu token through actual selectors, session creation and awaited operations
preserve cancellation by propagation OR a deliberately distinct cancellation result/exit contract
do not retry another interface or describe a canceled persistent write as rolled back
drain the whole task before disposing resources/clearing borrowed inputs
```

**Python:** N/A for async CancellationToken propagation. `py:ykman/_cli/config.py:313–322`,
`_configure_applications`, is synchronous and converts broad operation failures; it supplies no native
interrupt/drain or C# menu-token contract.

**Acceptance/dependencies:** actual token identity at menu/selector/create/write, pre-cancel no dispatch,
admitted-operation outcome, no generic-error substitution under chosen result policy and cleanup after
termination. Include current configuration/info/reset wrappers, not a new broad sibling audit. Keep the
already-fixed shared selector and single-route behavior. Core B5-C006/cancellation plus Windows successor
govern underlying lifetime; re-review sample error/cancellation/ownership guidance after selected changes.
No-brainer: **no** for contract; token forwarding is mechanical.

### MGMT-IMP-017 — Request results and executable labels

`RebootRequired` stores the caller's reboot request, not observed need/completion. Executable output
calls manufacturing `IsFips` a mode and omits CCID-only conditions for touch eject. Medium applies to
misleading security interpretation; Low to reboot/flag UX. Old retired test-host strings are absent in
current Management source; the original class is now correctly named `ManagementSessionSimpleTests`.
Do not revive that obsolete string correction.

**BEFORE — `src/Management/examples/ManagementTool/Features/Results/ConfigResult.cs`, `Succeeded`,
lines 42–43:**

```csharp
public static ConfigResult Succeeded(bool rebootRequired = false) =>
    new() { Success = true, RebootRequired = rebootRequired };
```

**BEFORE — `src/Management/examples/ManagementTool/Commands/InfoCommand.cs`, `ExecuteAsync`, line 47:**

```csharp
OutputHelpers.WriteKeyValue("FIPS Mode", info.IsFips ? "Yes" : "No");
```

**Proposed AFTER — literal label correction; result API alternative remains schematic:**

```csharp
OutputHelpers.WriteKeyValue("FIPS manufacturing indicator", info.IsFips ? "Yes" : "No");
```

```text
result choice: rename to an accepted request-oriented contract OR keep the member with explicit request semantics
never manufacture observed pending/required/completed reboot state from the request flag
touch-eject output names CCID-only mode with OTP/U2F/FIDO2 disabled
```

**Python:** `py:ykman/_cli/config.py:289–298`, `_configure_applications`, computes an interface-change
reboot request, but does not observe completion. `py:ykman/_cli/info.py:197–205`, `info`, displays separate
reported application approval rather than labeling `is_fips` an active mode. These are comparator
distinctions, not certification or a mandate to adopt Python's reboot heuristic.

**Acceptance/dependencies:** actual requested true/false success/failure result and user output;
matching labels in InfoCommand/DeviceInfoMenu, touch-eject descriptions in DeviceFlagsMenu and result
consumers. No stale-host string changes are needed; reset eligibility/overbroad erasure is IMP-010.
Core XREF-001 governs decoded approval; fixing labels alone does not fix it. Re-review result XML,
tool result/security/flag guidance and unified output on selected changes. No-brainer for labels: **yes**;
for result shape/heuristic: **no**.

### MGMT-XREF-001 — Core FIPS-tag decoding referral

**Canonical owner: `core-001:CORE-A-C004`**, also the historical `deviceinfo-FIPS-residual` occurrence.
Do not count it again as a Management decoder bug. FIPS tag bits are distinct from ordinary application
bits: a FIPS `01` currently becomes OTP rather than FIDO2. Medium is wrong metadata/selection/security
interpretation, not a certification finding. Switching to the unused helper alone is insufficient:
nonzero one-byte input still reaches its Int16 read.

**BEFORE — `src/Core/src/Devices/DeviceInfo.cs`, `CreateFromTlvs`, lines 204–205; `ReadCapabilities`,
line 290; `CapabilityMapper.FromFips`, lines 390–393:**

```csharp
var fipsCapabilities = ReadCapabilities(tlvDict, TAG_FIPS_CAPABLE);
var fipsApprovedCapabilities = ReadCapabilities(tlvDict, TAG_FIPS_APPROVED);
```

```csharp
return CapabilityMapper.FromApp(value);
```

```csharp
if (value.IsEmpty) return 0;
if (value.Length == 1 && value.Span[0] == 0) return DeviceCapabilities.None;

int fips = BinaryPrimitives.ReadInt16BigEndian(value.Span);
```

**Proposed AFTER — canonical Core design, unselected:**

```text
decode FIPS tags through their distinct mapping with accepted field-width/unknown-bit policy
test actual integrated tags, not only the dead helper or fixtures encoded as ordinary app masks
preserve manufacturing indicator versus reported approval versus approved-operation qualification
```

**Python:** `py:yubikit/management.py:82–94`, `CAPABILITY._from_fips`, and `375–380`,
`DeviceInfo.parse_tlvs`, route both tags through that mapping after integer conversion. Strong
implementation precedent; the frozen primary research still lacks authoritative current 0x14/0x15
width/mapping definitions. No new specification authority is supplied by this draft.

**Acceptance/referral:** canonical owner obtains named normative/vendor evidence or explicitly accepts
a bounded implementation profile; integrated nonzero/combined/absent/width/unknown vectors and real
filter/display consequences. Preserve the wire-source gap and separate physical approval qualification.
Re-review Core metadata and Tests.Shared filters, Management FIPS test summaries and output/guide
qualifications after that change. No-brainer for the group: **no**; no separate Management repair selected.

### MGMT-XREF-002 — Core flagged form-factor referral

**Canonical owner: `core-001:CORE-A-C005`.** The raw `81` produces `IsFips=true` and enum value 129,
not named UsbAKeychain=1. Exact shared filters miss that base form factor and labels can be Unknown.
Low reflects selection/display mismatch; Bio MPE reset must not rely on a broad Bio form factor alone.

**BEFORE — `src/Core/src/Devices/DeviceInfo.cs`, `CreateFromTlvs`, lines 194–197:**

```csharp
int formFactorTagData = ReadByte(tlvDict, TAG_FORMFACTOR);
var isFips = (formFactorTagData & 0x80) != 0;
var isSky = (formFactorTagData & 0x40) != 0;
var formFactor = (FormFactor)formFactorTagData;
```

**Proposed AFTER — representation alternatives:**

```text
choose normalized base form factor plus preserved raw product byte/unknown value
OR retain raw enum deliberately and make selected base comparisons explicit
do not silently discard unknown product/base bits or map every unknown to zero
```

**Python:** `py:yubikit/management.py:180–185`, `FORM_FACTOR.from_code`, masks the low nibble and maps
unknowns to UNKNOWN; `345–348`, `DeviceInfo.parse_tlvs`, separately extracts flags. Applicable known-base
precedent, not the chosen C# forward-compatible unknown policy. Frozen Core originals corroborate named
`41/81` variants; new policy still needs approval.

**Acceptance/referral:** canonical Core vectors for normal/flagged/unknown bytes, preserved raw evidence,
equality/hash and actual shared filter/formatter outcomes. Re-review FormFactor/DeviceInfo XML, selector
and test-fixture contracts, Management display/filter instructions and reset eligibility consumer use.
No-brainer: **no**; refer rather than duplicating the Core implementation backlog.

### CORE-MGMT-X003 — Bound factory and reset oracles

The final ProbeSession constructor in the unsupported-connection test never attaches a guard holder,
so it cannot detect a stale attachment. Current production rejects unsupported types before attachment;
this is Low test-oracle debt, not a demonstrated stale-guard defect. The reset fake also checks only
single command/INS, leaving complete bytes/status and lifecycle outside its proof.

**BEFORE — `src/Management/tests/Yubico.YubiKit.Management.UnitTests/ManagementSessionTests.cs`,
`CreateAsync_UnsupportedConnection_DoesNotLeaveSessionAttached`, lines 42–44:**

```csharp
// This direct constructor does not attach a guard holder. The assertion above checks rejection,
// but this probe cannot detect a stale attachment; that requires a separately bound factory probe.
await using var probe = new ProbeSession(connection);
```

**Proposed AFTER — behavioral test flow, no new public factory:**

```text
use a genuinely bound test factory on the same fake connection after refused/failed creation
assert exact successful holder acquisition, borrowed connection still owned by caller and zero unexpected I/O
extend reset proof at the accepted wire/lifecycle scope, not only a reflection/INS check
```

**Python:** N/A for Core's .NET ConnectionSessionGuard/factory binding;
`py:yubikit/management.py:604–615`, constructor, has no equivalent guard test contract. Its reset sends
INS 1F (`563–564`), corroborating only command identity, not C# Le/SCP formatting or terminal policy.

**Acceptance/dependencies:** meaningful factory reuse/error/cleanup oracle with actual guard admission,
not another input-validation-only test. Reset byte/status expectations depend on IMP-010 and canonical
Core B6 wire decisions. Whole-reader overlap assertion-failure drain remains B5-C006, not covered by this
probe. Re-review these exact test claims when changing factory/guard/reset framing. No-brainer: **yes**
for the narrow bound oracle; no unselected product lifecycle change follows.

### Management shared dependencies and Core boundary coverage

The [frozen deferred handoff](modules/core/pass-1/core-001/deferred-findings.md),
[candidate](modules/core/pass-1/core-001/triage/final/candidate-reconciliation.json)/
[inherited](modules/core/pass-1/core-001/triage/final/inherited-reconciliation.json) reconciliations and
[continuity](modules/core/pass-1/core-001/triage/final/reconciliation-4/deferred-finding-continuity.json)
retain original facet identities. These are **narrow Management closure dependencies**, not all-Core
assignments or extra accepted bug totals. The [revision-two boundary review](modules/core/pass-1/core-001/editing/applet-boundaries/revision-2/review.md)
accepted the local documentation corrections; earlier “pending B4/B6” and review-failure text remains
historical, with additive [B4](modules/core/pass-1/core-001/boundaries/Management/followup-B4.md)/
[B6](modules/core/pass-1/core-001/boundaries/Management/followup-B6.md) completion authority.

| Original group / actual Management dependency | Canonical owner and remaining closure | Re-review trigger |
| --- | --- | --- |
| `CORE-MGMT-X001`; MGMT-XREF-003 / A-C013 | Documentation corrected: fixture starts from a separate live read, first-match rows, content equality and selected fields. No runtime row-policy defect added. Core A owns future binding/cache/serial acquisition; actual scalar versus deep-memory comparison stays bounded. | Binding/cache/equality/serial lookup change: shared fixture instructions, reconfiguration/scoped/advanced test comments and discovery guide. |
| `CORE-MGMT-X002`; A-C003/C016 | Returned firmware uses captured `_version` default except non-final qualifier; timeout memory aliases input. Current qualifications are resolved prose; independent metadata storage/raw representation/equality/hash policy remains at Core A. | Accepted owner/raw/precedence change: full initial/public-read vectors and retained-input mutation/disposal behavior, then read XML, result/timeout UI, snapshot comparisons and guides. Do not clear borrowed metadata storage mechanically. |
| `CORE-MGMT-X003` | Local bound-factory/reset-oracle card; source rejection-before-attach remains true. | Actual factory/guard/cleanup/reset-wire repair and exact behavioral execution. |
| A-C004/C005 and historical FIPS residual | Two XREF referral cards above, one canonical owner each; A-C006 wider application decoding is adjacent, not another Management FIPS bug. | Integrated independent metadata fixtures, selected width/unknown/raw policy and actual filter/display behavior. |
| A-C014 / MGMT-IMP-009 | Local criterion correction versus canonical shared reject/ignore policy. | Accepted shared filter change or selected local test criterion, with real binding/coverage assertions. |
| A-C017/C021 and known-member/process claims; original CORE-001 | Core cache/identity/claim owners retain configuration-sensitive PID/evidence expiry, serial-less identity and disjoint-view limits. Management should end scopes and rediscover the intended key; completed snapshots are not every transient state or physical release proof. | Reconfiguration/group/cache changes or separately authorized physical lane; preserve historical two-key serial 9681620 failure. |
| B5-C006/C008 and exact reader/profile owners | A multi-page DeviceInfo read has per-page exchanges, not a whole public-operation lease. Parsing/later pages and assertion-failure cleanup can outlive exchange drain. Chosen whole-workflow admission and real fault/drain oracles remain Core B5; no new Management guard API selected. | Reader/disposal/cancellation/test-cleanup changes; await whole tasks before input/connection release and review overlap/scoped/monitor/reconfiguration claims. |
| B6-C001/C003/C011/C012; B5-C002/C003 | SmartCard read/reset empty data, Le, protected continuations/R-MAC and recovery/profile decisions remain Core B6/B5. Plain metadata pagination is not protected APDU continuation interoperability. Public operations use INS 1C/1D/1F, not a later INS82 exception path. | Selected exact wire/profile/failure oracle; re-review SELECT/read/write/reset backend and secure examples without transplanting synthetic expectations as conformance. |
| B6-C004/C006/C007/C009/C010/C015; SD-DEP-002 occurrence | Core owns partial handshake cleanup, parameter containment/aliases, optional DEK setup limitation, trust/actor and draft/vendor profile. Management borrows inputs only through initialization. Initial metadata precedes SCP; setup flag does not authorize lock writes or prove ongoing health. SD's retained-reset parameters are a different caller policy. | Core owner repair/profile selection plus actual Management factory failure/first protected command; re-review borrowed input/security/setup/override guidance. Refer SD-DEP-002 to Security Domain's canonical card, not a duplicated Management defect. |
| B6-C002/C005/C013, B5-C005 independent report copies | Management clears TLV/intermediate/final configuration owners locally. Lock bytes in independent formatter/report/SCP copies retain their own last-use cleanup; ordinary metadata is not secret merely because returned by the key. Mutable response/status and warning/error provenance remain Core. | Selected source-view/transfer owner tests, actual retained-array last use and precise error outcomes; reopen lock-code/security/error docs without universal erasure claims. |
| Windows B4-C001/C007; B1-C005; selected B4-Q/profile/ABI/access/listener facets | [Successor](integrations/windows-upstream-20261113/deferred-findings-successor.md#coordinator-completion-boundary) supersedes old built-in synchronous/report-release mechanism; source/managed staging prerequisite passed. FIDO rooted overlapped owner and OTP synchronous feature owner have distinct drain/wake/count limits. Arbitrary custom-provider disposal, returned report copies, Windows driver/unknown-release and relevant native ABI/access/listener/source qualification remain open. | Exact merged-source/driver/token/architecture/failure/reopen lane. Do not repeat the old built-in route as current, invent a feature-transfer count, or turn owner tests into physical release/Management task proof. |

### Management device and platform qualification

**Qualification only; no assigned defect severity. BEFORE/AFTER code: N/A.** The Management Pass-1
hardware lane performed **zero live Management operations**. Its successful build/unit/example
compilation is historical software evidence, not configuration/reset/NFC/OS conformance. Core boundary
source completion and Windows integration managed tests are different evidence dimensions; selected
Ctap2/YubiOtp owner journeys do not qualify all Management transports or its combined reset.

Keep retained failures intact: original raw xUnit2 example-identity diagnostics were preserved alongside
successful real SDK/MSBuild contexts, not hidden as a wrapper-only explanation. Later Core example
runner/context corrections keep their failed attempts and successful successor identities. Serial
9681620's two-key failure is unexplained; serial 125's two `6A88` INITIALIZE UPDATE outcomes occurred
before EXTERNAL AUTHENTICATE/RESET. They are not executed Management secure resets or proof of unsupported
firmware. Windows historical first-open failure and attended 17-pass/1-fail aggregate remain their own
source/rig scope. No test, build, native operation or hardware command ran for this draft.

**Procedure and acceptance:** select a bounded task, not a broad Management integration run.

1. Identify exact source/artifact, device serial/family/firmware, transport, OS/architecture/token/driver,
   starting persisted configuration, lock/SCP provisioning and authorization. The committed allow list
   remains the hardware authorization boundary; do not invent an extra environment-variable gate.
2. First qualify sequential read-only metadata over explicitly pinned supported transports, with independent
   expected fields. Finish discovery/monitor lifetimes before live comparisons; no inferred attended
   hotplug/native race result. Preserve detected/default/qualifier/effective version differences.
3. If selected, save every mutated field and a viable recovery route; test write/readback separately from
   active interfaces, CCID ejection/touch conditions, NFC access/NDEF and actual reboot/re-enumeration.
   IMP-002 controls the false-byte/recovery evidence; keep 3600/60 SDK timeout limits unless new intent is
   explicitly accepted. Those limits are not firmware maxima.
4. Only an explicitly selected Bio MPE reset lane may establish combined application/shared-PIN effects,
   F9 retention and post-reset session/native state. Management lock/configuration effects remain unknown
   unless independently observed; never use reset as automatic test cleanup or lost-code recovery.
5. Record actual commands/outcomes, cancellation/failure phase and restored readback or failed recovery.
   Attempt hardware cleanup in finally, clear secrets independently and await real task/owner drain.
   Native release, another process's access, physical power cycle, touch/hotplug and SCP/profile conformity
   need their separately selected oracles; managed disposal/open admission alone is not proof.

**Python:** `py:yubikit/management.py` and `py:ykman/_cli/config.py` supply the exact protocol/task
comparators named on individual cards, not this device/OS matrix. Python equality, masked input, report
ownership or broad “all data” reset output supplies no new physical qualification. Re-review public
read/write/reset/cancellation/ownership/security claims only to the accepted evidence scope.

**Unnumbered support observation — package-resolution portability.** The frozen C investigation records
an old relative Spectre HintPath as unselected support work, not a confirmed build failure. It remains
alongside the package reference at the current baseline. No defect severity is assigned without a
reproduced resolution failure; deciding the supported package-resolution context is not a no-brainer.

**BEFORE — `src/Management/examples/ManagementTool/ManagementTool.csproj`, Spectre assembly reference,
lines 25–28:**

```xml
<ItemGroup>
  <Reference Include="Spectre.Console">
    <HintPath>..\..\..\..\..\..\.nuget\packages\spectre.console\0.49.1\lib\net8.0\Spectre.Console.dll</HintPath>
  </Reference>
```

**Proposed AFTER — N/A pending support decision.** If selected, reproduce the intended clean restore/
package-resolution context, decide whether to remove or consolidate the redundant reference, then
compile the actual tool and record loaded compile references. Re-review its build/setup instructions
only if supported behavior changes. **Python: N/A** for this .NET/MSBuild assembly-reference mechanism;
the inspected Python CLI is not an equivalent dependency-resolution oracle. This is one retained
support note, not an additional numbered Management bug or implementation authorization.

### Management coverage ledger and exclusions

| Original deferred ID/facet | Disposition in this draft | Canonical review location |
| --- | --- | --- |
| MGMT-IMP-001 | Deferred field-support policy | Field-specific NFC preflight card |
| MGMT-IMP-002 | Deferred byte-effect/recovery qualification | NFC false-value card; no code choice selected |
| MGMT-IMP-003 | Deferred width/unknown/support policy | Request width/masks card; adjacent Core wider response owner linked |
| MGMT-IMP-004 | Deferred cleanup/restoration/default-SCP inputs | Test restoration card; both current default owners and already-correct simple scope distinguished |
| MGMT-IMP-005 | Deferred failure/cancel cleanup oracle, not production leak | Encoded-buffer oracle card |
| MGMT-IMP-006 | Deferred selected unpinned transport coverage | Actual transport card; already-pinned rows retained |
| MGMT-IMP-007 | Deferred precise negative-test outcomes | Error provenance card |
| MGMT-IMP-008 | Deferred public/internal legacy-read policy | Legacy read card; canonical Feature/reader boundary linked |
| MGMT-IMP-009 | Deferred local criterion and shared malformed-filter policy | Malformed filter card / Core A-C014 |
| MGMT-IMP-010 | Deferred family/transport/UI/lifecycle/effects | Combined reset card and qualification lane |
| MGMT-IMP-011 | Deferred meaningful coverage and operation qualification | Metadata/feature-oracle card; USB-or-NFC criterion mismatch included |
| MGMT-IMP-012 | Dormant, unselected; not a public regression | Legacy wire card; conditional AFTER only |
| MGMT-IMP-013 | Partial supersession: unused selector absent; SmartCard scope intentional but policy unselected | Standalone selection card; no automatic HID widening |
| MGMT-IMP-014 | Deferred help/arguments/optional task routing | CLI card; top-level-help C-MOD-R4 correction retained |
| MGMT-IMP-015 | Deferred immutable-input and exceptional cleanup design | Lock prompt card |
| MGMT-IMP-016 | Partial supersession: current shared selector preserves cancellation; menu/wrapper facets open | Cancellation card and canonical whole-task/native owners |
| MGMT-IMP-017 | Partial exclusion: obsolete host strings absent; request/result/FIPS/flags facets open | Executable labels/result card; reset shared facet is IMP-010 |
| MGMT-XREF-001 and historical deviceinfo-FIPS-residual | Open nonadditive Core referral | CORE-A-C004 referral card |
| MGMT-XREF-002 | Open nonadditive Core referral | CORE-A-C005 referral card |
| MGMT-XREF-003 | **Excluded: original documentation resolved**, later Core boundary prose also accepted | Current first-match/separate-live-fixture/cache explanations retained; reopen only after selected binding/cache changes |
| CORE-MGMT-X001 | **Excluded: accepted local documentation correction**; future cache/equality/lookup facets referred | Shared dependency row, not another fixture bug |
| CORE-MGMT-X002 | **Partial resolution: documentation accepted**; Core storage/raw/version facets remain | Shared dependency row / Core A-C003/C016 and IMP-008 |
| CORE-MGMT-X003 | **Partial resolution: oracle-limiting prose accepted**; actual bound-oracle work deferred | Bound-factory/reset card |
| All nine original Management boundary inherited keys | Accounted without added bugs | Six inherited MGMT IMP/XREF keys plus XREF-003, SD-DEP-002 canonical SD referral and FIPS-residual alias; [frozen map](modules/core/pass-1/core-001/boundaries/Management/inherited-dispositions.json) retained |
| MGMT-DOC-001–012; B-08 local TLV cleanup/order | **Excluded: accepted documentation/clean scope**, not deferred production leak/order defect | Preserve 0x0E NFC-enabled tag despite vendor conflicting row; local order is not protocol-mandated conformance |
| B-06 timeout-range expansion | **Not selected**; historical maintainer intent not firmware authority | Preserve current 3600/60 limits and zero/CCID conditions; qualify physical behavior separately |
| C Spectre HintPath support observation | Unselected portability observation, not confirmed build failure | Retained support note above: literal project BEFORE, AFTER N/A and bounded package-resolution procedure. |
| C obsolete API-design-skill module path | **Excluded from applet engineering**: instruction-maintenance referral, not consumer/runtime defect | Preserve original observation; no skill edit or new audit selected |
| Windows old B4-C001/C007 mechanisms | **Superseded at named built-in scope**, staging software prerequisite satisfied | New owner path retained; custom/native/independent-copy/Management physical qualification stays open |
| Historical validation/failures and original CORE-001/6A88 boundaries | **Preserved, not relabeled as fresh execution or resolved cause** | Management qualification card and narrow Core dependencies |

**Management coverage result:** **17 original MGMT cards, one local Core-boundary test-oracle card and
one device/platform qualification card: 19 local review cards**. Two usable Core metadata referrals are
displayed and counted only at their canonical Core owners: **21 displayed cards, not 21 defects**.
All 20 original IMP/XREF IDs, all three Core boundary groups and all nine inherited keys have a card,
narrow referral or explicit scoped exclusion. Human review, implementation selection and fresh execution
remain unstarted. Next applet in master order is **CTAP2**; this author stops after Management.

## CTAP2

### Context, evidence and review map

CTAP2 sends authenticator commands and parses their responses. WebAuthn owns client ceremony policy;
the relying party (RP) still verifies registration and sign-in. GetInfo advertisements, firmware
thresholds, configured PIN/biometric state, issued token permissions and actual product support are
different facts. A successful CTAP exchange or touch notification does not establish RP acceptance.

The [handoff](ctap2.md), [closeout](modules/ctap2/pass-1/ctap2-001/closeout.md),
[main ledger](modules/ctap2/pass-1/ctap2-001/triage.md) and
[corrected supplemental reconciliation](modules/ctap2/pass-1/ctap2-001/triage-addendum.md)
retain **47 deferred IMP groups, two open DEC groups and one reader referral**. The cards below
recheck selected mechanisms at `6ce355468047e8004fd51cf1425808f6e8242ed3`; they are not a new wide audit.
Original A–G evidence, primary passages, failures, correction history and precise per-path documentation
triggers remain authoritative at those linked records. Load deeper context only for the selected card.

All AFTER blocks in this section are **schematic, decision-dependent where indicated, proposed,
unapplied and uncompiled**. They describe changes to review, not existing SDK APIs. BEFORE blocks are
literal current-source windows, sometimes deliberately small representatives of a compound group.
Each card's acceptance also includes re-review of its original ledger's exact XML, guide, test and
sample triggers; those maps are incorporated rather than repeated. Severity measures potential impact,
not demonstrated exploitability. Only a settled local mechanism is called a no-brainer.

| Review lane | Cards | Main consequence / review choice |
| --- | --- | --- |
| Session, decoder and PIN boundaries | IMP-001–011 | Separate strict security/schema checks from caller/raw policy and secret ownership |
| Bio, administration and evidence | IMP-012–016, 018–025 | Correct standard wire/results; choose lifetime/state policy; retain real test limitations |
| Credentials and extensions | IMP-026–034 | Native account representation, owners, structural validation and valid-unknown preservation |
| Storage and experimental helpers | IMP-035–043 | Local clearing, sequence/range policy, experimental profile and algorithm contracts |
| Executable FidoTool | IMP-044–048 | Optional RP demo versus actual help, selection, cancellation and destructive-target defects |
| Decisions and shared qualification | DEC-002/003; boundary ledger | Actor intent, exact product/profile evidence, Core copies/workflows and native qualification |

### Inspected Python precedent register

Every `P` reference below names actual source at
`/home/rupicola/Code/y/yubikey-manager`, pin **`4ca60f706af930459138d8dc0f0f953480e1c7a4`**;
HEAD matched and the working tree was clean. These references are application precedent only.

| Ref | Actual path / symbol / lines | Applicability and limit |
| --- | --- | --- |
| P0 | `py:ykman/_cli/fido.py:35–44`, imports; `fido:115–116`; `py:pyproject.toml:21`, fido2 dependency | CTAP2, ClientPin, credential management, configuration and biometric engines come from external `fido2`. That library was **not installed/inspected** for this work. No local CTAP wire/parser/KDF/DER/ARKG/extension/storage/result-owner equivalent was established; do not attribute those implementations to this pin. |
| P1 | `py:ykman/_cli/fido.py`, `change_pin:431–460` | Reads device min/max PIN length and configured state, then validates Python string length. Useful input-layer/device-policy precedent, not proof of strict UTF-8/NFC/final-NUL admission or .NET secret erasure. |
| P2 | `py:ykman/_cli/fido.py`, `_init_bio:810–822`, `bio_enroll:848–882` | Tests **presence** of bioEnroll, obtains a bio token, drives capture and handles specific timeout/database errors. Wire/schema/crypto are external; no automatic cancellation/rollback or physical interoperability proof. |
| P3 | `py:ykman/_cli/fido.py`, `_ctap2_fingerprint:218–219`, `reset:225–350` | Reopens the retained device, restores SCP if requested, compares GetInfo without encIdentifier when serial is absent, and distinguishes selected reset statuses. Fingerprint equality is not unique physical identity; force bypass and broad fallback remain, so this is not a safe reset blueprint. |

P0 is the explicit **no inspected local equivalent** disposition for protocol and .NET lifetime cards.
It supplies neither negative proof about external fido2 nor normative authority. P1–P3 can inform a
decision only at their named application boundary.

### CTAP-IMP-001 — GetInfo validity and absent encrypted fields

**Context/impact:** initialization and feature consumers trust decoded GetInfo. Missing required
Versions/AAGUID becomes empty; absent encrypted byte arrays become *present empty nullable memory*.
Length/range/full-envelope gaps are separate from preserving valid unknown fields. **Medium:** malformed
metadata or false presence can misroute capability/crypto work; no device exploit established.
**Source-confirmed; no-brainer: no** for the combined strictness/absence API decision. **Python: P0.**

**BEFORE — `src/Ctap2/src/AuthenticatorInfo.cs`, `Parse` (called by Decode), `528–530,556`:**

```csharp
Versions = versions ?? [],
Extensions = extensions ?? [],
Aaguid = aaguid ?? [],
```

```csharp
EncIdentifier = encIdentifier,
```

**AFTER:**

```text
validate required Versions/AAGUID and selected numeric/byte widths
preserve valid unknown map fields; reject malformed standalone trailing input
represent missing encrypted fields as absent, distinct from encoded empty bytes
```

**Acceptance/dependencies:** valid unknown fields survive; missing/wrong-length AAGUID, invalid ranges,
trailing data and missing/empty encrypted fields produce selected distinct outcomes. Coordinate IMP-002,
028/043 and WebAuthn metadata assumptions. Nested readers need not exhaust parent input.

### CTAP-IMP-002 — Initialization version and firmware policy

**Context/impact:** GetInfo firmware gates USB SmartCard before the effective override. Missing or
major-zero sentinel firmware can pass optimistic feature policy; unsupported version advertisements
are not independently rejected. **Medium:** a session may appear supported without qualified protocol
or firmware evidence. **Source-confirmed policy gap; no-brainer: no. Python: P0.**

**BEFORE — `src/Ctap2/src/Ctap2Session.cs`, `InitializeAsync:160–165`:**

```csharp
var info = await GetInfoCoreAsync(backend, cancellationToken).ConfigureAwait(false);
var detectedFirmwareVersion = info.FirmwareVersion ?? new FirmwareVersion();

if (Connection is ISmartCardConnection smartCardConnection)
{
    EnsureSmartCardTransportSupported(smartCardConnection.Transport, detectedFirmwareVersion);
```

**AFTER:**

```text
choose accepted CTAP revision and sentinel policy explicitly
keep detected GetInfo, Management/HID observations and effective override distinct
apply the selected USB gate without converting optimistic metadata into support proof
```

**Acceptance/dependencies:** absent/sentinel/known-old/new firmware, unsupported/unknown version strings,
USB versus ATR-classified NFC and override timing have meaningful initialization outcomes. No transport
fallback after failure. Coordinate IMP-001, DEC-003 and Core configuration dependency below.

### CTAP-IMP-003 — Typed request limits versus raw responsibility

**Context/impact:** canonical CBOR encoding does not enforce CTAP schema depth or device maxMsgSize.
Typed callers can produce a request the authenticator rejects; raw callers intentionally construct
their own wire. **Medium** for reliability/resource robustness, not a proven bypass.
**Source-confirmed absence; no-brainer: no. Python: P0.**

**BEFORE — `src/Ctap2/src/Cbor/Ctap2SessionRequestEncoding.cs`, `BuildMakeCredentialRequest:79`:**

```csharp
return builder.Build();
```

**AFTER:**

```text
typed route: enforce selected depth/schema/message limits at the selected layer
raw route: preserve explicitly documented caller responsibility OR opt-in validation
do not reject valid unknown response values as a request-side shortcut
```

**Acceptance/dependencies:** exact maximum and one-over requests, nested extension payloads and actual
device-advertised limits exercise meaningful encoding/send boundaries. Decide whether checks need fresh
GetInfo and who owns that sequence. Coordinate IMP-002/019/036 and Core framing, not a duplicate Core bug.

### CTAP-IMP-004 — Builder clones and raw buffer lifetime

**Context/impact:** clearing a sent request does not clear captured WithBytes/encoded-value clones,
writer storage, transport reports or returned responses. Raw Send borrows its input.
**Medium:** SDK-owned secret copies can outlive intended use; no end-to-end erasure/exploit demonstrated.
**Source-confirmed; no-brainer: no** for owner/reset design. **Python: P0.**

**BEFORE — `src/Ctap2/src/Cbor/CtapRequestBuilder.cs`, `WithBytes:91–93`:**

```csharp
var copy = value.ToArray();
_parameters[key] = writer => writer.WriteByteString(copy);
return this;
```

**AFTER:**

```text
choose explicit builder-owned clone lifetime and post-Build/reuse/disposal semantics
clear owned clones on every selected terminal path; never clear borrowed caller input
retain WithValue deferred-read lifetime and separately track writer/transport/result copies
```

**Acceptance/dependencies:** actual retained arrays show clearing after success, encoding failure and
abandoned/reused builders according to the chosen contract. Coordinate IMP-024/027/035, WA-IMP-013,
Core report/APDU copies and exceptional cleanup; one cleared clone cannot close the group.

### CTAP-IMP-005 — Local PIN crypto exceptional cleanup

**Context/impact:** V1/V2 failed decryption can abandon plaintext; V1 full MAC stack storage lacks
finally clearing; post-ECDH dictionary allocation can fail before Encapsulate adopts Z.
**Medium:** secret remnants on exceptional exits. **Source-confirmed; no-brainer: yes** for clearing
those owned buffers, not provider-wide erasure or a new owner API. **Python: P0.**

**BEFORE — `src/Ctap2/src/Pin/PinUvAuthProtocolV1.cs`, `Decrypt:174–179`:**

```csharp
using (var decryptor = aes.CreateDecryptor())
{
    decryptor.TransformBlock(inputArray, 0, inputArray.Length, plaintext, 0);
}

return plaintext;
```

**AFTER:**

```text
retain output until successful transfer; clear untransferred plaintext in finally
finally-clear V1 full MAC; guard raw Z immediately after derivation, before allocations
```

**Acceptance/dependencies:** meaningful fault paths observe the actual untransferred output/MAC/Z
storage, preserve the original crypto exception and keep successful caller outputs usable. Recheck both
protocols and helper allocation ordering; coordinate IMP-007/035 and PRF without claiming provider erasure.

### CTAP-IMP-006 — PIN units and input enforcement

**Context/impact:** ClientPin checks 4–63 **bytes**, while the Core reader preset counts UTF-16 units.
Neither alone proves UTF-8/NFC/code points, device minimum or no final NUL.
**Medium:** valid non-ASCII PINs can be rejected or invalid/policy-incompatible inputs sent; existing-PIN
normalization could change the secret. **Source-confirmed; no-brainer: no. Python: P1, with P0 limit.**

**BEFORE — `src/Ctap2/src/Pin/ClientPin.cs`, `ValidatePin:573–576`:**

```csharp
if (pin.Length < PinMinLength)
{
    throw new ArgumentException(
        $"PIN must be at least {PinMinLength} bytes.", paramName);
```

**AFTER:**

```text
choose input-layer versus ClientPin enforcement; validate without silently normalizing existing PIN
distinguish UTF-16 input limits, Unicode scalar count, strict UTF-8 bytes and device minimum
apply new-PIN/current-PIN policy separately, including final NUL and normalization requirements
```

**Acceptance/dependencies:** multibyte, supplementary, malformed UTF-8/surrogate, normalized/non-normalized,
final-NUL and raised-minimum cases verify the actual transmitted bytes and no unintended PIN change.
**CTAP-XREF-003 / CORE-CTAP-X003** is this same reader allocation decision, not another Core bug;
OpenPGP keeps its own profile. Coordinate WebAuthn prompts and FidoPinHelper string exposure.

### CTAP-IMP-007 — Security-critical peer COSE labels

**Context/impact:** PIN ECDH checks coordinate storage/width and imports P-256, but does not check
kty=2, crv=1, alg=-25 or selected optional-label restrictions. Open generic COSE carriers are not this
security-critical peer boundary. **Low:** a peer map with missing, wrong-type or wrong-value required
labels can proceed if its X/Y are 32-byte arrays accepted by the provider as a P-256 point. The bounded
impact is acceptance of a schema-invalid key-agreement response instead of rejecting it at this boundary;
no operational interoperability failure has been demonstrated. Changing only ignored labels for the same
accepted X/Y does not select another curve or derivation algorithm. Provider point validation is retained;
weaker cryptography, off-curve acceptance, token authorization bypass and secret compromise are not
established. Required-label validation remains a protocol-correctness and security-defense fix, not a
proven exploit repair. **Source-confirmed; no-brainer: yes** for required labels, with exception/optional-label
policy still to select. **Python: P0.**

**BEFORE — `src/Ctap2/src/Pin/PinUvAuthHelpers.cs`, `PerformEcdhKeyAgreement:63–66`:**

```csharp
// Check coordinate storage here; required kty/alg/crv labels are not yet validated.
if (!peerCoseKey.TryGetValue(CoseEC2X, out var xObj) || xObj is not byte[] peerX)
{
    throw new ArgumentException("Peer COSE key missing or invalid X coordinate (-2).", nameof(peerCoseKey));
```

**AFTER:**

```text
validate required peer label values/types and selected optional-parameter restrictions before import
retain exact coordinate width and provider point validation; preserve chosen exception provenance
```

**Acceptance/dependencies:** correct vector, missing/wrong-type/wrong-value labels, extra optional labels,
invalid coordinates and import-versus-derivation errors. Existing fixtures missing alg must be corrected,
not accepted as interoperability evidence. Coordinate IMP-009/029, hmac-secret/PRF and Core catalog boundary.

### CTAP-IMP-008 — Retry absence, ranges and UV fields

**Context/impact:** absent retry state/count becomes false/zero; signed counts are accepted; UV parser
accepts a power-cycle field not defined by the cited CTAP2.1 UV response.
**Medium:** prompts can mistake unknown state for exhaustion or recovery advice.
**Source-confirmed; no-brainer: no** for presence/model policy. **Python: P0.**

**BEFORE — `src/Ctap2/src/Pin/ClientPin.cs`, `ParsePinRetriesResponse:614–615`:**

```csharp
int retries = 0;
var powerCycleRequired = false;
```

**AFTER:**

```text
distinguish required retry count from optional unknown power-cycle state
select unsigned/range/complete-envelope rules and revision-specific UV field handling
do not convert missing evidence into a definite block/recovery state
```

**Acceptance/dependencies:** absent count/state, zero/positive/negative/out-of-range, unknown fields,
trailing input and unsupported UV power-cycle field exercise actual parser-to-prompt outcomes.
Coordinate WebAuthn retry mappings and IMP-020/022; preserve temporary versus exhausted PIN block.

### CTAP-IMP-009 — Token response shape and crypto evidence

**Context/impact:** ClientPin decrypts a returned byte string without validating the selected protocol's
token result length/full response. Fake decrypt round trips do not prove crypto/wire interoperability.
**High:** malformed authentication material can reach downstream authorization consumers; no bypass
demonstrated. **Source-confirmed shape gap; no-brainer: yes** for settled protocol widths, not a
certification claim. **Python: P0.**

**BEFORE — `src/Ctap2/src/Pin/ClientPin.cs`, `GetPinTokenAsync:349–350`:**

```csharp
var encryptedToken = ParsePinTokenResponse(response);
return _protocol.Decrypt(sharedSecret, encryptedToken);
```

**AFTER:**

```text
validate selected protocol/subcommand ciphertext and plaintext token shapes
clear invalid decrypted output before throwing; transfer only accepted token storage
```

**Acceptance/dependencies:** independent V1/V2 vectors, invalid IV/block/result lengths, malformed/trailing
maps and real decrypt failure verify transmitted/authenticated bytes and error cleanup. Coordinate
IMP-005/007/010/022; device token issuance/permission/lifetime remains separate qualification.

### CTAP-IMP-010 — Token capability, permission, RP and persistence policy

**Context/impact:** legacy command fallback keeps the caller-selected protocol but drops requested
permissions/RP. Built-in UV and persistent pcmr-only combinations have additional gates not fully
preflighted. **Medium:** callers can overestimate issued authorization or encounter device failures;
fallback itself is not automatically a bug. **Source-confirmed; no-brainer: no. Python: P0; P2 is only
an application example requesting a bio permission.**

**BEFORE — `src/Ctap2/src/Pin/ClientPin.cs`, `GetPinUvAuthTokenUsingPinAsync:410–415`:**

```csharp
var info = await _session.GetInfoAsync(cancellationToken).ConfigureAwait(false);
bool supportsTokenWithPermissions = info.Options.TryGetValue("pinUvAuthToken", out var supported) && supported;

if (!supportsTokenWithPermissions)
{
    return await GetPinTokenAsync(pin, cancellationToken).ConfigureAwait(false);
```

**AFTER:**

```text
select helper-versus-caller capability/permission/RP enforcement and legacy fallback contract
represent requested scope separately from actual issued token guarantees
select persistent pcmr-only representation and built-in UV/configured-state gates
```

**Acceptance/dependencies:** negotiated protocol versus command fallback, missing RP for MC/GA, invalid
permission unions, UV unset/unsupported and persistent restrictions verify actual requests and explicit
outcomes. Include FidoTool PinManagement's GA-without-rpId occurrence. WA-IMP-009 remains client-owned;
IMP-016/019/043 consume this policy, not duplicate authorization findings.

### CTAP-IMP-011 — Exclude uv when token auth is sent

**Context/impact:** typed options encode uv independently from pinUvAuthParam; FidoTool creation can
request both. Even uv:false is a present key. WebAuthn's token path already omits it.
**Medium:** invalid requests fail on conforming devices. **Source-confirmed; no-brainer: yes** for
mutual exclusion and correcting selected samples; choose rejection versus normalization explicitly.
**Python: P0.**

**BEFORE — `src/Ctap2/src/Cbor/Ctap2SessionRequestEncoding.cs`, `WriteMakeCredentialOptions:162–165`:**

```csharp
if (options.UserVerification.HasValue)
{
    writer.WriteTextString("uv");
    writer.WriteBoolean(options.UserVerification.Value);
```

**AFTER:**

```text
reject the uv-plus-token-auth combination OR explicitly omit uv under an accepted contract
sample token path leaves UserVerification absent; built-in UV uses its separate route
```

**Acceptance/dependencies:** MC/GA with uv:true, uv:false, absent uv and absent/present auth bytes capture
exact wire; correct invalid compound fixtures and FidoTool Make/Get calls. Preserve existing WebAuthn
omission and historical makeCredential up:false resolution. Coordinate IMP-010/022.

### CTAP-IMP-012 — Standard biometric request wire

**Context/impact:** standard subcommands are shifted, sensor request omits subCommand=7, authenticated
transcripts omit modality, and timeout is top-level key6 instead of authenticated parameter-map key3.
**High:** enrollment/admin commands can invoke the wrong operation or fail; no physical enrollment
success established. **Source-confirmed; no-brainer: yes** for the standard path, **no** for an
unevidenced preview profile. **Python: P2/P0; external FPBioEnrollment bytes are not inspected.**

**BEFORE — `src/Ctap2/src/BioEnrollment/BioEnrollmentSubCommand.cs`, `EnrollBegin:37`:**

```csharp
public const byte EnrollBegin = 0x02;
```

**AFTER:**

```text
standard begin/next/cancel/enumerate/rename/remove = 1/2/3/4/5/6; sensor = 7
authenticate modality || subCommand || encoded parameters; put timeout in parameters key3
keep preview routing separate until its exact profile/detection/authorization is evidenced
```

**Acceptance/dependencies:** exact bytes and auth transcripts for every standard operation, sensor no-auth,
timeout omitted/present and explicit cancel. Correct fake fixtures, then separately authorize device
qualification. Coordinate IMP-013–015/024/047, DEC-003; .NET cancellation is not enrollment cancel.

### CTAP-IMP-013 — Biometric inventory schema and unsupported errors

**Context/impact:** inventory decodes flat fields rather than key7's template array, and InvalidCommand
becomes empty inventory instead of unsupported. **Medium:** existing enrollments disappear from UI and
unsupported devices appear empty. **Source-confirmed; no-brainer: yes** for standard schema/error
distinction, with chosen public error contract. **Python: P2/P0.**

**BEFORE — `src/Ctap2/src/BioEnrollment/FingerprintBioEnrollment.cs`, `EnumerateEnrollmentsAsync:228–231`:**

```csharp
catch (CtapException ex) when (ex.Status == CtapStatus.InvalidCommand)
{
    // Unsupported command is currently normalized to empty; standard no-enrollment is InvalidOption.
    return [];
```

**AFTER:**

```text
decode standard templateInfos array with each ID and optional name
distinguish standard no-enrollment InvalidOption from unsupported InvalidCommand and other failures
```

**Acceptance/dependencies:** multiple templates, optional names, malformed entries, true no-enrollment,
unsupported and transport/cancellation failures retain exact outcomes. Coordinate IMP-012/014/046/047;
Python's external enumeration is not a verified decoder oracle.

### CTAP-IMP-014 — Enrollment completion and identity

**Context/impact:** absent sample fields default to Good/zero, so malformed data can report completion;
next responses need not repeat begin's template ID. Sensor required fields also silently default.
**Medium:** loops can stop falsely or continue with the wrong/empty ID.
**Source-confirmed; no-brainer: no** for operation-aware result design. **Python: P2/P0.**

**BEFORE — `src/Ctap2/src/BioEnrollment/BioEnrollmentModels.cs`, `EnrollmentSampleResult.Decode:273–275`:**

```csharp
ReadOnlyMemory<byte> templateId = default;
var lastStatus = FingerprintSampleStatus.Good;
var remaining = 0;
```

**AFTER:**

```text
validate required fields for sensor/begin/next operation separately
retain begin ID in the selected workflow/result contract; next need not re-emit it
only valid remaining-count zero projects completion; persistence remains independently verified
```

**Acceptance/dependencies:** missing status/count/ID, negative counts, next without ID, poor sample and
continuation failure drive the actual loop. Coordinate IMP-012/013/015/019/022/047 and DEC-003.

### CTAP-IMP-015 — Biometric status names and vendor fields

**Context/impact:** final enum names/values conflict with the standard; maxTemplateCount's sensor field
has no retained original vendor definition. **Medium:** misleading feedback/recovery and capability
claims. **Source-confirmed standard mismatch; vendor provenance unresolved; no-brainer: no** for the
compound API/profile decision. **Python: P2/P0.**

**BEFORE — `src/Ctap2/src/BioEnrollment/BioEnrollmentModels.cs`, `FingerprintSampleStatus:99,105,111`:**

```csharp
StorageFull = 0x0B,
```

```csharp
NoUserActivity = 0x0C,
```

```csharp
NoUserPresence = 0x0D
```

**AFTER:**

```text
select names/values from the pinned standard table; preserve valid unknown feedback
vendor-only sensor fields require a pinned original definition or explicit unknown/raw treatment
```

**Acceptance/dependencies:** table-backed statuses including already-exists/activity/lift values,
unknown status and vendor sensor fields produce truthful model/UI labels. Coordinate IMP-012/014,
IMP-045's unknown-kind UI facet and DEC-003; no sensor metadata proves enrollment support.

### CTAP-IMP-016 — Configuration expressiveness and capability gates

**Context/impact:** active first-three standard subcommands are correct; unused local vendor constants
lack proof. SetMinPinLength cannot omit length, clear an RP list with empty, or express explicit false.
**Low** for expressiveness, **Medium** if unsupported policy operations are silently assumed accepted.
**Source-confirmed restrictions; no-brainer: no. Python: P0.**

**BEFORE — `src/Ctap2/src/Config/AuthenticatorConfig.cs`, `SetMinPinLengthAsync:170–174`:**

```csharp
public async Task SetMinPinLengthAsync(
    int newMinPinLength,
    IReadOnlyList<string>? rpIds = null,
    bool forceChangePin = false,
    CancellationToken cancellationToken = default)
```

**AFTER:**

```text
choose omitted/present-false/present-empty request representation and helper preflight responsibility
retain active standard subcommands; remove/qualify unused vendor constants only with selected evidence
read current alwaysUv before toggle; re-read after uncertainty instead of blindly retrying
```

**Acceptance/dependencies:** exact omission/false/empty/list-replacement bytes, advertised per-subcommand
support and toggle uncertain outcomes. minPinLength disclosure remains reporting, not RP policy setting;
WA-IMP-018 is separate false handling. Coordinate IMP-006/010/024 and DEC-003.

### CTAP-IMP-018 — Disposed CredentialManagement helper

**Context/impact:** Dispose sets an unread flag; later operations still use borrowed session/protocol/token.
**Low:** misleading lifetime admission, not proof of token leak or automatic authorization termination.
**Source-confirmed; no-brainer: no** until disposed-helper contract is selected. **Python: P0.**

**BEFORE — `src/Ctap2/src/CredentialManagement/CredentialManagement.cs`, `Dispose:293–294`:**

```csharp
// Clearing borrowed memory here would invalidate other caller-held views.
_disposed = true;
```

**AFTER:**

```text
choose reject-after-dispose OR an explicitly non-owning lifetime API without a misleading marker
do not dispose session/protocol or clear borrowed token merely to make Dispose effective
```

**Acceptance/dependencies:** every helper operation after disposal has the chosen behavior, disposal is
idempotent and borrowed owners remain usable/unchanged. Keep whole-workflow disposal separate and
retain the rejected cloned-token-leak inference. Coordinate IMP-019/024 and shared lifetime dependency.

### CTAP-IMP-019 — Administrative counts, hashes and sequences

**Context/impact:** missing totals yield one entry; malformed totals/hash widths are not fully checked;
begin/next can interleave with other operations. **Medium:** incomplete inventory, runaway continuation
or wrong RP routing. **Source-confirmed gaps; no-brainer: no** for sequence policy, while required
response validity is narrower. **Python: P0.**

**BEFORE — `src/Ctap2/src/CredentialManagement/CredentialManagement.cs`, `EnumerateRelyingPartiesAsync:149–153`:**

```csharp
for (var i = 1; i < firstRp.TotalRpCount.Value; i++)
{
    response = await SendCredentialManagementCommandAsync(nextPayload, cancellationToken)
        .ConfigureAwait(false);
    results.Add(RelyingPartyInfo.Decode(response));
```

**AFTER:**

```text
validate operation-required counts/totals/hash widths and selected practical continuation bounds
choose caller serialization OR whole-sequence admission; preserve first-response ReportedTotal
on next failure throw with owned-result cleanup; do not pretend partial inventory is complete
```

**Acceptance/dependencies:** first/next/empty/error, missing/negative/huge totals and mismatched hashes,
interleaving schedule and continuation cleanup. Use the returned RP hash unchanged, not truncated display
text. Coordinate IMP-014/018/024/036 and shared Core workflow facet.

### CTAP-IMP-020 — Read-only tests with mutating setup

**Context/impact:** the session helper defaults to PIN normalization before the callback, including
GetInfo. It can set/change a PIN or consume a retry. **Medium:** unattended or mis-scoped tests mutate
device state. **Source-confirmed; no-brainer: no** for lane/default policy. **Python: P1 is an explicit
PIN command, not a read-only test-helper precedent.**

**BEFORE — `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/TestExtensions/Ctap2TestStateExtensions.cs`,
`WithCtap2SessionAsync:172–175`:**

```csharp
public async Task WithCtap2SessionAsync(
    Func<Ctap2Session, Task> action,
    bool normalizePin = true,
    ProtocolConfiguration? configuration = null,
```

**AFTER:**

```text
separate non-mutating session setup from explicitly authorized known-PIN setup/recovery
choose helper defaults/call-site lanes and traits; inspect full setup before classifying a test read-only
```

**Acceptance/dependencies:** fake command traces establish that a read-only lane sends no PIN mutation
or token verification; authorized PIN lanes retain explicit prerequisites and human coordination.
Smoke filters are not safety guarantees; no new GetInfo approval gate. Coordinate IMP-021/022 and
qualification procedure below before any hardware execution.

### CTAP-IMP-021 — Test secret cleanup and state restoration

**Context/impact:** normalization discards a token and clears temporary PIN only on success; tests can
restore config/PIN only on happy paths. **Medium:** secret remnants and altered test-device state or
masked primary failures. Public fixed test values alone have lower confidentiality impact.
**Source-confirmed representative sites; other sites inherited; no-brainer: no** for recovery policy,
yes for local owned-array finally clearing. **Python: P0; P1 gives no .NET teardown guarantee.**

**BEFORE — `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/TestExtensions/Ctap2TestStateExtensions.cs`,
`NormalizePinAsync:132–133`:**

```csharp
_ = await clientPin.GetPinTokenAsync(KnownTestPin, cancellationToken)
    .ConfigureAwait(false);
```

**AFTER:**

```text
adopt/clear returned test tokens and temporary PINs in finally
record pre-state and uncertain mutations; attempt only authorized independent restoration
report restoration failure separately without hiding the primary test/command failure
```

**Acceptance/dependencies:** failure after first PIN change, assertion failure, cancellation and teardown
failure preserve both outcomes and actual buffer observations. Verify restored state independently;
some policy changes may not be reversible without reset. Include owned test plaintext/comparisons,
IMP-022/042 and Core assertion-failure drain rather than counting every fixture as another bug.

### CTAP-IMP-022 — Meaningful protocol, deletion and feature oracles

**Context/impact:** tests can assert nonempty bytes, fake crypto or sensor info, return without feature
exercise, or fail to isolate deletion/cap behavior. **Qualification/test gap, no assigned defect
severity** unless a selected assertion change exposes a runtime bug. **Source-confirmed representative;
broader inventory inherited; no-brainer: no. Python: P0/P2 do not prove SDK tests.**

**BEFORE — `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/AuditV2/Ctap2LargeBlobAuditReproTests.cs`,
`YESDK1621_SdkWriteIsReadablePerSpec:99`:**

```csharp
matched = output.ToArray().AsSpan().SequenceEqual("audit-interop"u8);
```

**AFTER:**

```text
select behavior oracles for real delete/re-read, independent crypto vectors and actual feature exercise
separate fixed-public-payload comparison from secret comparison/cleanup under IMP-021
isolate cap from authenticated-AAD changes; report unsupported/not-exercised, never normal-return pass
```

**Acceptance/dependencies:** name exact assertions/transcripts and exercised branches. Preserve original
force-change status correction: legacy0x05 expects PIN_INVALID; permission0x09 expects
PIN_POLICY_VIOLATION. No change justified by the withdrawn assertion mismatch. Coordinate IMP-009,
012–015/020/021/037/041 and source-bound qualification, not certification from test names.

### CTAP-IMP-023 — SmartCard SELECT and empty-response provenance

**Context/impact:** every SELECT ApduException becomes unsupported FIDO AID, including other APDU causes;
empty CTAP response becomes Other. **Medium:** troubleshooting and WebAuthn mapping can confuse transport,
malformed data and actual unsupported application. **Source-confirmed; no-brainer: no** for exception
policy. **Python: P0; P3 distinguishes selected errors but also has a broad fallback.**

**BEFORE — `src/Ctap2/src/Backend/SmartCardBackend.cs`, `SelectApplicationAsync:61–65`:**

```csharp
catch (ApduException ex)
{
    throw new NotSupportedException(
        "FIDO2 over SmartCard is not supported because the authenticator did not expose the FIDO2 AID.",
        ex);
```

**AFTER:**

```text
map only selected evidence-backed no-AID statuses to unsupported; retain other APDU provenance
classify empty response under selected malformed-response contract, not fabricated device status
```

**Acceptance/dependencies:** actual no-AID, warning/error/transport/cancellation and empty response
exercise backend-to-caller mappings while retaining status/inner cause. Coordinate WA-IMP-002,
IMP-047/048 and Core APDU error-owner scope; APDU failure is not CTAP timeout/cancel.

### CTAP-IMP-024 — Administrative owned request temporaries

**Context/impact:** final credential-management request clearing does not cover payload, auth tag,
auth transcript and bio/config helper copies. **Medium:** SDK-owned token-derived/request secrets remain.
**Source-confirmed; no-brainer: yes** for named owned-buffer finally clearing, without redesigning
borrowed-token ownership. **Python: P0.**

**BEFORE — `src/Ctap2/src/CredentialManagement/CredentialManagement.cs`, `BuildUpdateUserPayload:431–436`:**

```csharp
var subCommandParams = BuildUpdateUserParams(credentialId, user);
var messageToAuth = new byte[1 + subCommandParams.Length];
messageToAuth[0] = subCommand;
subCommandParams.Span.CopyTo(messageToAuth.AsSpan(1));

var pinUvAuthParam = _protocol.Authenticate(_pinUvAuthToken.Span, messageToAuth);
```

**AFTER:**

```text
finally-clear owned params/transcript/auth/payload/request at actual last use and failure exits
keep session/protocol/token borrowed; track builder/writer storage under IMP-004 separately
```

**Acceptance/dependencies:** actual-array success and Authenticate/Encode/send-failure observers cover
credential management, config and bio paths, preserving primary exceptions and borrowed input.
Coordinate IMP-004/012/018/019/035 and Core intermediate copies, not a new count per allocation.

### CTAP-IMP-025 — AAGUID endian portability

**Context/impact:** default Guid byte serialization has a mixed-endian convention independent of host;
the converter reverses components only on little-endian hosts. **Low:** wrong AAGUID on big-endian
execution. **Code-inferred portability defect; little-endian framework fixture passed historically;
no big-endian execution. No-brainer: yes** for removing native-endian dependence. **Python: P0.**

**BEFORE — `src/Ctap2/src/Cbor/AaguidConverter.cs`, `ToBigEndianBytes:77–82`:**

```csharp
if (BitConverter.IsLittleEndian)
{
    // The required reversal is currently applied only on little-endian hosts.
    bytes[0..4].Reverse();  // time_low
    bytes[4..6].Reverse();  // time_mid
    bytes[6..8].Reverse();  // time_hi_and_version
```

**AFTER:**

```text
use explicit network-order Guid conversion OR unconditional first-4/2/2 convention reversal
apply symmetric conversion both ways without testing native machine byte order
```

**Acceptance/dependencies:** known network-order vectors and round trips test both directions without
host-dependent branching; platform qualification is separately named if executed. DEC-001's uncertainty
is resolved into this IMP, not an open decision. WA-IMP-007 default/equality remains independent.

### CTAP-IMP-026 — Request/response user representation

**Context/impact:** public ctor requires nonempty id and both names; parser permits absent names but
rejects empty id. CTAP empty-account/privacy allowances differ from WebAuthn handle policy.
**Low:** valid CTAP inputs/results cannot be represented; privacy/serializer intent is DEC-002, not
an automatically accepted vulnerability. **Source-confirmed; no-brainer: no. Python: P0.**

**BEFORE — `src/Ctap2/src/Credentials/PublicKeyCredentialTypes.cs`, `PublicKeyCredentialUserEntity:323–325,331–332`:**

```csharp
if (id.IsEmpty)
{
    throw new ArgumentException("User ID cannot be empty.", nameof(id));
```

```csharp
ArgumentException.ThrowIfNullOrEmpty(name);
ArgumentException.ThrowIfNullOrEmpty(displayName);
```

**AFTER:**

```text
choose distinct request/response representation or explicit optional-name/empty-id contracts
retain WebAuthn account-handle policy at its layer; do not automatically strip native User fields
```

**Acceptance/dependencies:** absent names, empty/maximum handle, make/get and UV/no-UV conditional user
rules round-trip under accepted profile. Coordinate IMP-028 and DEC-002; existing User/descriptor prose
resolution stays closed and WA-IMP-010 ID recovery stays separate.

### CTAP-IMP-027 — Credential and extension result owners

**Context/impact:** typed responses create independent authData/key/extension arrays and borrow other
slices without a disposable result owner. Decode failure is after backend transfer. StoredCredentialInfo
already owns/clears its private clone, not the original response. **Medium:** secret-bearing output
copies outlive use. **Source-confirmed; no-brainer: no** for owner/view API. **Python: P0.**

**BEFORE — `src/Ctap2/src/Credentials/MakeCredentialResponse.cs`, `Decode:187–189`:**

```csharp
case 2: // authData
    authDataRaw = reader.ReadByteString();
    authData = AuthenticatorData.Parse(authDataRaw);
```

**AFTER:**

```text
select coherent disposable response owners, borrowed views and transfer/failure cleanup
preserve complete unknown response data within that lifetime, not extra unowned RawData clones
document which original transport/APDU storage remains outside each owner
```

**Acceptance/dependencies:** actual retained secret/unknown/attestation views clear on failure/disposal;
borrowed source remains unchanged and public fields retain chosen usability. Coordinate IMP-004/030/034/035,
WA-IMP-006/013 and Core copies; session disposal is not result disposal or end-to-end erasure.

### CTAP-IMP-028 — Structural versus contextual credential decoding

**Context/impact:** ED suffix is not validated as one map; trailing non-ED bytes are ignored; attested
key can be any CBOR value; make lacks AT check; get uint32 count casts to signed int. Standalone parsers
lack request/version context. **Medium:** malformed results/counts reach client workflows.
**Source-confirmed; no-brainer: no** for combined contextual API, yes for settled structure/range checks.
**Python: P0.**

**BEFORE — `src/Ctap2/src/Credentials/GetAssertionResponse.cs`, `Decode:200–202`:**

```csharp
case 5: // numberOfCredentials
    numberOfCredentials = (int)reader.ReadUInt32();
    break;
```

**AFTER:**

```text
validate authData suffix/AT/key structure and represent count without overflow
standalone decode exhausts its envelope; nested decode consumes only its own value
choose separate request/revision-aware conditional checks; preserve valid unknown COSE identifiers
```

**Acceptance/dependencies:** ED wrong type/trailing value, non-ED trailing bytes, absent AT, non-map key,
zero/overflow counts and conditional user/descriptor fields. Coordinate IMP-001/007/026/030/034;
WA-IMP-010 actual-singleton recovery remains a separate client association problem.

### CTAP-IMP-029 — Experimental ARKG representation and key validation

**Context/impact:** SDK emits kty2 and byte-string-wrapped nested maps; draft-10 uses kty=-65537/direct
maps and permits optional int/text derived algorithm. Decoder accepts alternate outer kty and padded
coordinates without nested curve check. **High** for using unqualified key material as crypto input;
**experimental compatibility/security decision, not production exploit evidence. No-brainer: no. Python: P0.**

**BEFORE — `src/Ctap2/src/Cose/CoseArkgP256SeedKey.cs`, `Decode:101–105`:**

```csharp
// Require EC2 carriers; generic decoding does not establish curve=1 or valid P-256 points.
var kemKey = CoseKey.Decode(kemKeyCbor) as CoseEc2Key
    ?? throw new InvalidOperationException("ARKG KEM public key is not CoseEc2Key");

var blKey = CoseKey.Decode(blKeyCbor) as CoseEc2Key
```

**AFTER:**

```text
select pinned vendor profile OR a named draft target, then reconcile kty/map/dkalg encoding
validate security-critical nested curve/width/point/profile before reconstruction/derivation
retain explicit experimental non-production status and valid generic unknown-key escape hatches
```

**Acceptance/dependencies:** selected profile vectors, wrong kty/wrapping/curve/width/point, optional
derived-algorithm cases and encode/decode round trips. **Seed -65700 agreement is resolved**, not a defect.
Coordinate IMP-039–041, DEC-003 and Core ARKG math/provider/Rust qualifications; sibling agreement is
not normative authority or proof of complete profile conformance.

### CTAP-IMP-030 — Unsigned outputs and complete raw envelope

**Context/impact:** make models unsigned key6 but drops other outer fields; get skips key8 and lacks a
complete envelope escape hatch. **Medium:** valid extension output is unavailable to callers.
**Source-confirmed; no-brainer: no** for support/ownership API; no obligation to implement every extension.
**Python: P0.**

**BEFORE — `src/Ctap2/src/Credentials/GetAssertionResponse.cs`, `Decode:209–211`:**

```csharp
default:
    reader.SkipValue();
    break;
```

**AFTER:**

```text
choose get unsigned-key8 support and complete raw-envelope preservation with IMP-027 ownership
keep make key6/get key8 and signed authData extensions distinct
```

**Acceptance/dependencies:** known/unknown unsigned outputs, valid unknown outer fields and borrowed/owned
failure lifetimes. Keep `audit-v2-20261022-20261031:#4` as this same dropped-output identity; old device
absence/API removal does not close it. Coordinate IMP-028/032/034 and WA-IMP-017; unsigned-output
first-introduction history remains unclaimed under DEC-003.

### CTAP-IMP-031 — credBlob size and empty-input contract

**Context/impact:** DTO restricts 1–32 bytes, raw builder accepts empty/oversize; device maxCredBlobLength
is not a universal 32-byte maximum. **Low:** inconsistent admission/interoperability; sensitive content
also depends on WA-IMP-012 credProtect enforcement. **Source-confirmed; no-brainer: no. Python: P0.**

**BEFORE — `src/Ctap2/src/Extensions/CredBlob/CredBlobInput.cs`, `Blob` init accessor, `64–67`:**

```csharp
if (value.Length is < 1 or > 32)
{
    throw new ArgumentException("CredBlob must be between 1 and 32 bytes", nameof(Blob));
}
```

**AFTER:**

```text
choose DTO/raw-builder empty and size responsibility using actual advertised capacity
separate storage input admission from client-side sensitive-content protection/enforcement
```

**Acceptance/dependencies:** empty, 32, advertised larger maximum and one-over values capture exact
encoding/device-policy outcomes without inventing a universal maximum. Coordinate IMP-003/010 and
WA-IMP-012, the canonical enforcement owner; credProtect's ignored enforcePolicy flag is not another
new CTAP bug. Do not turn the current composition into a safe sensitive-storage recipe.

### CTAP-IMP-032 — Legacy largeBlob convenience shape

**Context/impact:** signed synthetic key/written mapping conflates envelope largeBlobKey with unsigned
direct largeBlob output; registration boolean becomes Written. **Medium:** output reports the wrong
meaning/route. **Source-confirmed convenience bug; no-brainer: no** for remove/rework/direct-feature
choice. **Python: P0.**

**BEFORE — `src/Ctap2/src/Extensions/Shared/ExtensionOutput.cs`, `TryGetLargeBlob:190–194`:**

```csharp
if (reader.PeekState() == CborReaderState.Boolean)
{
    var supported = reader.ReadBoolean();
    output = new LargeBlobOutput { Written = supported };
    return true;
```

**AFTER:**

```text
remove/rework synthetic convenience output OR separately model standard direct unsigned output
keep envelope key5/7 largeBlobKey and shared-storage route separate from supported/written/read output
```

**Acceptance/dependencies:** registration support versus actual write/read shape, compressed/originalSize
direct output if selected, envelope keys and synthetic fixture labels. Coordinate IMP-030/034/036 and
WA-IMP-014–017 facade gaps. Direct makeCredential support input exists in CTAP; absent SDK support
is not proof the protocol forbids it or every current YubiKey lacks it.

### CTAP-IMP-033 — Ceremony-specific hmac-secret accessors

**Context/impact:** assertion byte-string accessor throws on valid registration boolean; MC has no keyed
accessor. **Low:** a Try-style convenience call fails on another valid ceremony shape.
**Source-confirmed; no-brainer: no** for accessor contract; no mandatory MC feature inferred. **Python: P0.**

**BEFORE — `src/Ctap2/src/Extensions/Shared/ExtensionOutput.cs`, `TryGetHmacSecret:164–165`:**

```csharp
output = HmacSecretOutput.Decode(data);
return true;
```

**AFTER:**

```text
choose ceremony-specific registration/assertion/MC accessors OR explicit shape-aware Try behavior
route by actual extension key and validate selected protocol output lengths
```

**Acceptance/dependencies:** registration true/false, assertion bstr, wrong shape and keyed hmac-secret-mc
fixtures (not a direct unkeyed bstr test). Coordinate IMP-009/027/034/035 and WA-IMP-011 PRF policy;
there is no CTAP wire prf key and salt authentication is not token authorization.

### CTAP-IMP-034 — Preserve valid raw extension values

**Context/impact:** reader overload stores empty values; memory overload reconstructs and drops tags,
floats/simple values and rejects unsigned values above Int64. **Medium:** valid future output is lost
or misrepresented. **Source-confirmed; no-brainer: yes** for preserving values, **no** for owner and
duplicate/malformed policy. **Python: P0.**

**BEFORE — `src/Ctap2/src/Extensions/Shared/ExtensionOutput.cs`, `Decode(CborReader):288–291`:**

```csharp
var key = reader.ReadTextString();
// This overload intentionally retains only the identifier, not a decodable value.
reader.SkipValue();
extensions[key] = ReadOnlyMemory<byte>.Empty;
```

**AFTER:**

```text
preserve each complete encoded value, with selected owner/view lifetime
define duplicate/malformed handling; do not reconstruct valid unknown values through a finite codec
```

**Acceptance/dependencies:** byte-exact tags/floats/simple/full-uint/nested values across both overloads,
duplicate and malformed values, actual slice lifetime. Coordinate IMP-027/028/030/032 and WebAuthn raw
maps. LargeBlobData already retains arbitrary unknown encoded values; do not copy this restriction
into IMP-037's distinct origSize/integer-key parser.

### CTAP-IMP-035 — Storage, salt and metadata temporary cleanup

**Context/impact:** Set/Delete discard decrypted membership-probe plaintext; storage auth/request copies,
compression stream storage, salt construction and metadata failure output have separate missing guards.
**Medium:** named SDK-owned secrets outlive use. **Source-confirmed representative sites; no-brainer: yes**
for local clearing, with transfer/owner decisions separate. **Python: P0.**

**BEFORE — `src/Ctap2/src/Extensions/Shared/ExtensionBuilder.cs`, `BuildHmacSecretInput:226–230`:**

```csharp
// Encrypt salts
var saltEnc = protocol.Encrypt(sharedSecret, saltData);

// Compute auth tag
var saltAuth = protocol.Authenticate(sharedSecret, saltEnc);
```

**AFTER:**

```text
finally-clear saltData and untransferred outputs when Authenticate/DTO construction fails
adopt/clear successful membership probes; clear compression backing and metadata failure output
clear fragment auth/payload/request copies at actual last use without clearing borrowed inputs
```

**Acceptance/dependencies:** success and Encrypt/Authenticate/compression/send/metadata-failure actual-array
observers cover each named site. Coordinate IMP-004/005/024/027/036/043, WA-IMP-013 and Core copies;
provider/native copies and independently returned extension results are not closed by local finally.

### CTAP-IMP-036 — Shared blob sequences and malformed fragments

**Context/impact:** cached limits and sequential whole-array read/write lack whole-flow coordination;
missing/empty response becomes initial array, pathological fragment sizes/termination are unguarded.
**Medium:** silent data replacement, nontermination or lost concurrent updates. **Source-confirmed;
no-brainer: no** for lock/conflict/recovery/pinless policy; required shape/termination checks are narrower.
**Python: P0.**

**BEFORE — `src/Ctap2/src/LargeBlobs/LargeBlobStorage.cs`, `ParseReadResponse:496–498`:**

```csharp
if (response.Length == 0)
{
    return ReadOnlyMemory<byte>.Empty;
```

**AFTER:**

```text
validate required fragment response and complete shape separately from CTAP initial-array hash rule
bound fragment sizes/offsets/termination; choose limit refresh and whole-flow conflict/admission policy
after partial write uncertainty re-read; do not promise rollback, resume or atomicity
```

**Acceptance/dependencies:** maxMsgSize<=64, oversized/missing/nonterminating fragments, bad hash versus
malformed response, two writers and interrupted write. Retain 70-byte LE-offset auth transcript.
Coordinate IMP-003/019/035/037 and Core workflow; WA-IMP-017 token-only write restriction is not
automatic authorization for a new pinless feature.

### CTAP-IMP-037 — Blob uint ranges, preservation and asymmetric cap

**Context/impact:** origSize/integer keys use signed Int64; unknown **values** already retain arbitrary
encoded slices borrowing input. Decrypt skips >1MiB while Encrypt accepts larger data; modeled entries
are reconstructed. **Medium:** valid foreign entries fail or own writes become unreadable.
**Source-confirmed; no-brainer: no** for cap/preservation policy, yes for chosen full-uint support.
**Python: P0.**

**BEFORE — `src/Ctap2/src/LargeBlobs/LargeBlobData.cs`, `LargeBlobEntry.TryDecode:239–241`:**

```csharp
case KeyOrigSize when reader.PeekState() == CborReaderState.UnsignedInteger:
    origSize = reader.ReadInt64();
    break;
```

**AFTER:**

```text
choose full uint64 origSize/integer-key handling and complete-entry/raw preservation lifetime
select symmetric write/read cap OR explicit documented asymmetry with meaningful outcomes
retain arbitrary unknown encoded values and their borrowed-input lifetime
```

**Acceptance/dependencies:** Int64 boundary/full uint, unknown keys/values, foreign entries, cap-adjacent
write/read and canonical reserialization exercise independent format/cap paths. Original cap fixture
also changed AAD, so it did not isolate cap rejection. Coordinate IMP-022/035/036; no all-client
interoperability or whole-array atomicity claim.

### CTAP-IMP-038 — previewSign extraction contract and inner flags

**Context/impact:** decoder tolerates missing outer flags as beta behavior though policy flags are
inside inner attestation; extraction catches only selected parse exceptions and performs no trust/binding
verification. **Medium** for misleading extraction/error/policy use, not proven forged attestation.
**Source-confirmed; no-brainer: no. Python: P0.**

**BEFORE — `src/Ctap2/src/Extensions/PreviewSign/PreviewSignCbor.cs`, `TryExtractGeneratedKey:486–488`:**

```csharp
var previewSignCbor = response.UnsignedExtensionOutputs["previewSign"];
var innerAttestation = DecodeUnsignedRegistrationOutput(previewSignCbor);
var authData = Credentials.AuthenticatorData.Parse(innerAttestation.AuthData);
```

**AFTER:**

```text
select syntactic extraction false-versus-throw and structural validation contract
locate policy flags in inner attestation; do not infer trust or key/account binding from extraction
adding an RP/attestation verifier is a separate feature decision, not this fix
```

**Acceptance/dependencies:** missing/malformed outer/inner output, absent AT, wrong key carrier and
ArgumentException/CborContentException/InvalidOperationException boundaries. Coordinate IMP-027–030/040,
WA-IMP-006 and DEC-003. Retain experimental warning and parse-versus-verification distinction.

### CTAP-IMP-039 — Propagate derived algorithm, not seed marker

**Context/impact:** one generated-key accessor passes seed Algorithm(-65700); the other passes actual
DerivedKeyAlgorithm (typically -9). **Medium:** callers get inconsistent algorithm metadata and can
dispatch incorrectly. **Source-confirmed; no-brainer: yes** for propagation, **no** for generalized
algorithm dispatch. **Python: P0.**

**BEFORE — `src/Ctap2/src/Extensions/PreviewSign/PreviewSignRegistrationOutput.cs`, `TryGetGeneratedKey:98–102`:**

```csharp
return new PreviewSignGeneratedKey(
    KeyHandle,
    arkgKey.BlPublicKey,
    arkgKey.KemPublicKey,
    arkgKey.Algorithm);
```

**AFTER:**

```text
pass the decoded seed's DerivedKeyAlgorithm in this accessor as in PreviewSignCbor extraction
separately reject/dispatch unsupported derivation/signature algorithms under an accepted contract
```

**Acceptance/dependencies:** both accessors assert exact algorithm with handle/points/context; a point-only
KAT does not close propagation. Coordinate IMP-029/038/041, WebAuthn output mapping and Core math;
seed/signing/derived identifiers remain different.

### CTAP-IMP-040 — previewSign construction and operation policy

**Context/impact:** ctor rejects empty handle/TBS despite draft allowances; init can replace checked
values; additionalArgs accepts arbitrary non-map bytes; builder chooses registration when both modes
set; ARKG args check length, not point. **Medium:** inconsistent/invalid ceremony or crypto input.
**Source-confirmed restrictions/gaps; no-brainer: no. Python: P0.**

**BEFORE — `src/Ctap2/src/Extensions/PreviewSign/PreviewSignSigningParams.cs`, `KeyHandle/Tbs:52,57`:**

```csharp
public ReadOnlyMemory<byte> KeyHandle { get; init; }
```

```csharp
public ReadOnlyMemory<byte> Tbs { get; init; }
```

**AFTER:**

```text
choose consistent constructor/init/send-time validation and empty handle/TBS semantics
choose one registration/authentication mode; validate additionalArgs as selected COSE_Sign_Args map
validate points only for the selected ARKG profile, not every opaque algorithm argument
```

**Acceptance/dependencies:** empty/init-overridden values, both modes, null/empty/non-map arguments,
curve-specific point cases and selected-credential routing. Coordinate IMP-029/038/041 and WA-IMP-019
facade round-trip policy; no invented generic SDK validation API.

### CTAP-IMP-041 — Experimental signature DER and raw-message contract

**Context/impact:** converter reads two integers without root/sequence exhaustion or full positivity/bounds;
VerifySignature hashes raw message under fixed P-256/SHA-256, not arbitrary prehashed TBS.
**Medium risk, provisional:** downstream provider rejection/actual malformed acceptance needs evidence,
not an exploit claim. **Source-confirmed parser limits; no-brainer: no. Python: P0.**

**BEFORE — `src/Ctap2/src/Extensions/PreviewSign/PreviewSignDerivedKey.cs`, `ConvertDerToIeee:159–162`:**

```csharp
var reader = new AsnReader(derSignature.ToArray(), AsnEncodingRules.DER);
var sequence = reader.ReadSequence();
var r = sequence.ReadIntegerBytes();
var s = sequence.ReadIntegerBytes();
```

**AFTER:**

```text
select strict DER exhaustion/component admission and fixed supported algorithm contract
distinguish VerifyData raw-message input from prehashed TBS; reject unsupported dispatch explicitly
```

**Acceptance/dependencies:** valid independent vectors, extra sequence/root data, negative/zero/oversized
components and raw-message versus digest behavior through the actual provider. Coordinate IMP-029/039/040
and Core ARKG/provider qualification; no hardware or production qualification from malformed unit vectors.

### CTAP-IMP-042 — Optional immutable largeBlobKey export

**Context/impact:** an opt-in interop fixture writes a key as immutable hex text to a file.
**Medium confidentiality risk if enabled with real secrets; no export executed.** Current comments
already disclose unsafe persistence. **Source-confirmed channel; no-brainer: no** for remove/redesign
versus an explicitly authorized evidence lane. **Python: P0.**

**BEFORE — `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/AuditV2/Ctap2LargeBlobAuditReproTests.cs`,
`YESDK1621_SdkWriteIsReadablePerSpec:112–114`:**

```csharp
if (Environment.GetEnvironmentVariable("AUDIT_LARGE_BLOB_KEY_OUT") is { Length: > 0 } keyOut)
{
    await File.WriteAllTextAsync(keyOut, Convert.ToHexString(response.LargeBlobKey.Value.Span));
```

**AFTER:**

```text
remove immutable secret export OR select a bounded human-controlled evidence channel with explicit risk
never label file/environment cleanup as guaranteed erasure; keep ordinary tests non-exporting
```

**Acceptance/dependencies:** no accidental export by default; selected channel names destination/access,
retention, independent cleanup and immutable-copy limits. No new hardware/export authorization here.
Coordinate IMP-021/022/027; test comparison/temporary clearing remains IMP-021, not another export count.

### CTAP-IMP-043 — Encrypted GetInfo wrapper versus generic CBC helper

**Context/impact:** helper accepts IV-only/arbitrary blocks and any nonempty PPUAT, while standard fields
are IV16+ct16. CBC is unauthenticated: wrong-token output can look like bytes, not verified identity/state.
**Medium:** callers may misinterpret arbitrary plaintext as valid metadata.
**Source-confirmed; no-brainer: no** for strict wrapper versus generic helper. **Python: P0.**

**BEFORE — `src/Ctap2/src/Crypto/EncryptedMetadataDecryptor.cs`, `DecryptWithInfo:128–132`:**

```csharp
// Encrypted data format: [IV (16 bytes) | ciphertext]
// This generic helper accepts IV-only input; standard GetInfo fields also have 16 ciphertext bytes.
if (encryptedData.Length < 16)
{
    return null;
```

**AFTER:**

```text
strict GetInfo wrapper: enforce exact field/token profile, keeping plaintext opaque/unauthenticated
OR retain explicitly generic CBC helper and add a separately selected strict protocol boundary
clear untransferred failure output under IMP-035; do not invent credential identity interpretation
```

**Acceptance/dependencies:** IV-only, exact/long/non-block fields, token widths and wrong-token results;
generic longer-plaintext tests are not protocol vectors. Coordinate IMP-001/010/035 and DEC-003's
persistent-token/earliest-support evidence; two-session equality does not prove issuance or freshness.

### CTAP-IMP-044 — Optional RP-capable protocol demonstration

**Context/impact:** sample generates random hash/user and assertion returns first-only result, without
verifiable RP client data/cardinality. **Optional demo enhancement, no assigned defect severity** while
honestly presented as a protocol demo. **Source-confirmed; no-brainer: no. Python: P0** supplies no
local RP ceremony/result serializer equivalent.

**BEFORE — `src/Ctap2/examples/FidoTool/FidoExamples/MakeCredential.cs`, `CreateAsync:82–87`:**

```csharp
var clientDataHash = new byte[32];
RandomNumberGenerator.Fill(clientDataHash);

// Demo account handle; production uses the RP's stable handle, not a new random account each call.
var userId = new byte[32];
RandomNumberGenerator.Fill(userId);
```

**AFTER:**

```text
if selected: accept exact RP/account/client-data context and expose all selected assertion/result data
otherwise retain bounded protocol-demo claims; do not add a mandatory SDK RP/server feature
```

**Acceptance/dependencies:** selected enhancement can hand off exact original client data, stable account
handle, credential/signature/attestation and assertion count to an independent RP verifier. No real
ceremony authorized. Coordinate IMP-011/026/028, DEC-002 and WebAuthn ceremony ownership.

### CTAP-IMP-045 — FidoTool help, parser and truthful display

**Context/impact:** help omits set-pin/new-pin; unknown subcommands can return0, help can select a device
first and option value can be another flag. Unknown sensor kind UI labeling is a separate retained facet.
**Low:** misleading usage, success status and input handling. **Source-confirmed; no-brainer: yes**
for truthful help/parser errors, **no** for selected UI policy. **Python: P1/P2** show explicit command
arguments, not an inspected Click parser implementation.

**BEFORE — `src/Ctap2/examples/FidoTool/Program.cs`, `ParseOption:682–684`:**

```csharp
if (string.Equals(args[i], flag, StringComparison.OrdinalIgnoreCase))
{
    return args[i + 1];
```

**AFTER:**

```text
validate option presence/value and report unknown subcommands as errors
dispatch help before device selection; list actual set-pin/new-pin support
display unknown sensor kind truthfully rather than inventing Swipe support
```

**Acceptance/dependencies:** missing/next-flag value, unknown verb/subcommand, help with no device and
unknown-kind label exercise real control flow/exit codes. No mandate to add every absent CLI feature.
Coordinate IMP-015/046/047 and shared selector/input ownership; compilation is not device execution.

### CTAP-IMP-046 — Current selector and bio capability classification

**Context/impact:** current FidoTool delegates to shared index-based/cancellable selection; original
duplicate-label IndexOf and unconditional noninteractive retry mechanisms are **superseded**, not live
bugs. Transport-only eligibility still admits non-CTAP SmartCard candidates; auto-selection policy and
bioEnroll=false misclassification remain. **Medium:** wrong candidate/support outcome.
**Current remaining facets source-confirmed; no-brainer: no** for policy, yes for bio presence distinction.
**Python: P2** checks bioEnroll presence; P3 informs identity limits.

**BEFORE — `src/Ctap2/examples/FidoTool/Cli/Prompts/DeviceSelector.cs`, `Selector:24–25`:**

```csharp
private static readonly SharedSelector Selector = new(
    [ConnectionType.HidFido, ConnectionType.SmartCard], NonInteractiveSelection.PreferFirstTransport);
```

**BEFORE — `src/Ctap2/examples/FidoTool/FidoExamples/BioEnrollment.cs`, `IsSupported:378–379`:**

```csharp
var info = await session.GetInfoAsync(cancellationToken);
return info.Options.TryGetValue("bioEnroll", out var supported) && supported;
```

**AFTER:**

```text
retain shared index-based identity and cancellable/noninteractive prompt behavior
select CTAP qualification and explicit/noninteractive target policy; distinguish support from enrolled state
```

**Acceptance/dependencies:** identical labels select distinct views (current shared
`DeviceSelector.PromptAsync:224–234`); no noninteractive retry; SmartCard without FIDO AID and
bioEnroll absent/false/true receive truthful outcomes. Preserve preview-profile qualification.
Coordinate IMP-002/013/045/047/048; selected candidate is not verified destructive target continuity.

### CTAP-IMP-047 — Remaining error, cancellation and console lifetime

**Context/impact:** shared selector now propagates cancellation and uses cancellable prompts; old local
selector/debug/synchronous-prompt mechanisms are **superseded**. FidoExamples broad catches/admin probes
still turn cancellation/read errors into failure/unsupported; console Ctrl+C registration persists.
**Medium:** masked provenance and stale event callback/lifecycle behavior.
**Source-confirmed remaining facets; no-brainer: no** for result/event-owner contract. **Python: P2/P3**
show selected status handling, not universal cancellation or event-detachment precedent.

**BEFORE — `src/Cli.Shared/src/Cli/CommandHelper.cs`, `CreateConsoleCts:29–34`:**

```csharp
var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};
```

**AFTER:**

```text
select cancellable result/exception contract and preserve transport versus unsupported provenance
give Ctrl+C registration an explicit detach lifetime alongside CTS ownership
bio thrown cancellation: select independently authorized enrollment-cancel handling, not automatic rollback
```

**Acceptance/dependencies:** cancellation through actual prompts/discovery/token/sample loops and broad
helper catches, repeated console lifetimes and disposed CTS event delivery. Selected errors retain
status/cause; failed IsSupported read is not unsupported. Coordinate IMP-023/045/046/048, Core presence
finalization and raw cancellation qualification; no universal Ctrl+C completion deadline.

### CTAP-IMP-048 — Destructive reset target continuity

**Context/impact:** both reset flows select independently after reinsertion. Interactive flow retains old
transport; CLI chooses from later selection, but neither binds the confirmed key. Failed preflight defaults
and force bypass need policy. **High:** another key's credentials/PIN could be reset.
**Source-confirmed target-binding defect; no-brainer: yes** for preventing uncorrelated reset, **no** for
serial-less identity/force/recovery policy. **Python: P3**, with fingerprint/force limits explicitly retained.

**BEFORE — `src/Ctap2/examples/FidoTool/Cli/Menus/ResetMenu.cs`, `RunAsync:98–100`:**

```csharp
// Known target-continuity defect: independent selection can return a different physical key.
var newSelection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
if (newSelection is null)
```

**AFTER:**

```text
bind confirmation/preflight/reinsert/destructive action to the selected physical-target contract
reject uncertain correlation; revalidate current transport and product/reset requirements
choose force and failed-preflight behavior explicitly; preserve required touch/timing authorization
```

**Acceptance/dependencies:** simulated swap, serial-less lookalikes, changed transport, failed preflight,
force and cancellation never send reset to an unconfirmed key. Exact device/firmware reset windows need
DEC-003 qualification; no live reset here. Coordinate IMP-002/023/046/047 and shared identity evidence;
Python GetInfo fingerprint equality is not unique identity proof.

### CTAP-DEC-002 — Canonical native selected-user disclosure decision

**Original owner:** `ctap2-001:CTAP-DEC-002`, G-011, with IMP-026 representation context and
`CORE-WA-X008` occurrence. The [WebAuthn referral](#native-selected-user-disclosure--ctap-dec-002-referral)
is this same decision, not another WA-IMP or accepted privacy vulnerability. Actor tracing is
**source-reconciled**, not awaiting another packet: CTAP User passes through matcher/cached response;
SelectAsync returns the already-built object and the SDK supplies no RP upload/serializer.

**Qualification/contract decision, no assigned defect severity; no-brainer: no. Python: P0.** The
authenticator's no-identifying-fields-without-UV requirement and multiple-account platform picker's
only-id outgoing rule have different actors/conditions. Absence of a serializer establishes neither
browser conformance nor permission to retain every field at a future RP boundary.

**BEFORE — `src/Ctap2/src/Credentials/GetAssertionResponse.cs`, `Decode:197–199`:**

```csharp
case 4: // user
    user = PublicKeyCredentialUserEntity.Parse(reader);
    break;
```

**AFTER — choose one native-output model, consistent with the WebAuthn referral:**

```text
raw native DTO: retain modeled User as application picker data; application owns RP disclosure serializer
OR browser-equivalent output: separate internal picker data and enforce chosen outgoing field contract
in either model: apply selected authenticator UV/privacy response validation in CTAP
name each actor and account binding; cached selection is not a disclosure transformation
```

**Acceptance/dependencies:** maintainer records raw/application versus platform/browser intent, serializer
owner and exact permitted fields for UV/no-UV, single/multiple, allow/discoverable paths. Assert exact
selected DTO/serializer outputs and RP account binding after the selected change. Coordinate IMP-026/028
and WA result/picker XML; **no automatic DTO stripping**. User/descriptor prose remains resolved and
WA-IMP-010 recovery is a different dependency.

### CTAP-DEC-003 — Product, draft and physical qualification

**Qualification, no assigned defect severity; no-brainer: no. BEFORE/AFTER: N/A** because no source
patch can establish an unobserved product/profile result. **Python: P2/P3** identify selected application
checks only; P0 supplies no local preview/ARKG/PPUAT conformance oracle.

Retain exact SKU/firmware/prototype support; standard-versus-preview bio minima/vendor field definitions;
reset product-window gap **5.5.2/5.5.3**; earliest hmac-secret-mc/PPUAT/encrypted-field support;
ARKG vendor profile; preview beta0x7F/ECDH provenance; and unsigned-output introduction history.
Firmware constants, advertisements and sibling code do not close these questions. Sign-extension v4 is
the 2025-08-26 experimental editor draft, not CTAP v4 or an adopted W3C signing standard. ARKG draft-10's
stated expiry is 2026-08-31; its seed -65700 agreement and the pinned L3 BE/BS citation are resolved,
while actual kty/map/dkalg/curve/profile and Core mathematics/provider questions remain.

**Concrete verification procedure (future separately authorized lane):**

1. Select one question and pin its original publication/profile, device family/SKU, actual firmware,
   connection transport/OS/driver/loaded native producer and current PIN/UV/credential state. Distinguish
   advertisement, configured state, required permission and known firmware evidence.
2. Specify exact non-mutating probe or approved ceremony, expected wire/result/status, physical presence,
   reset clock and independent before/after state oracle. Do not introduce a mandatory host reset timer
   merely because a product reset window is unqualified.
3. Inspect complete setup: default PIN-normalizing helpers can mutate/consume retries; unsupported or
   skipped paths are not passes. Enrollment cancel, .NET cancellation, CTAPHID cancel and rollback differ.
4. Retain executed transcript/outcome and failures against exact source/artifact identity; independently
   clear owned secrets and restore authorized state. Separate managed/fake/compile evidence from native,
   hardware, crypto/provider and RP verification. Never claim universal support from a single key.

**Dependencies:** IMP-002/010/012–016/020–022/029/038–043/048 plus the narrow Core qualifications below.
Original Pass1 had no live ceremony/PIN/bio/blob/reset run; historical compile/probe failures and
corrections remain retained, not rerun or silently upgraded here.

### Narrow shared dependencies and Windows successor

Use the frozen [Core handoff](modules/core/pass-1/core-001/deferred-findings.md),
[candidate](modules/core/pass-1/core-001/triage/final/candidate-reconciliation.json),
[inherited](modules/core/pass-1/core-001/triage/final/inherited-reconciliation.json) and
[continuity](modules/core/pass-1/core-001/triage/final/reconciliation-4/deferred-finding-continuity.json)
maps. The [23-group boundary](modules/core/pass-1/core-001/boundaries/Ctap2/candidates.md),
[completed B4/B6 source return](modules/core/pass-1/core-001/boundaries/Ctap2/followup-B4-B6.md) and
[completed WebAuthn return](modules/core/pass-1/core-001/boundaries/Ctap2/followup-WebAuthn.md) settle
source-reading handoffs, not engineering or physical closure. Their old pending statuses are historical.
Do not import unrelated Core/PIV backlog or repeat resolved documentation as runtime bugs.

| Dependency | Canonical owner and retained facet | Closure procedure / code applicability |
| --- | --- | --- |
| Membership and complete workflow | Core A/B1 grouping/admission and **CORE-B5-C006**; CTAP X001/X002, IMP-019/036 and WA lifetime cards are occurrences | Decide whole-operation admission/reentrancy versus caller serialization, keeping known-member/process bounds and no fallback. BEFORE/AFTER for qualification **N/A**: controlled schedules must hold token/enumeration/blob/post-decode/terminal-callback continuations through actual final use, then verify disposal/claim/reopen. An exchange-only held test is insufficient; callbacks must not dispose/reenter their awaiting owner. |
| Independent reports/APDU copies | **CORE-B5-C005**, **CORE-B6-C002**; CTAP IMP-004/027/035 own their later buffers | Choose connection-to-protocol transfer/clearing and APDU/error/intermediate ownership; observe each actual returned array, alias and failure buffer independently. Windows native staging is not these arrays. Existing successful clone clearing is not end-to-end erasure. Coordinate original owner cards rather than proposing a second transport-copy fix here. |
| Framing/cancel/recovery | Core B5-C002/C003 and B6-C001/C003, CTAP X018 | Keep normalized CTAPHID versus CTAP command/status domains, at-most-one CANCEL and valid-terminal drain. BEFORE/AFTER **N/A** for physical claims: test actual keepalive/no-keepalive, raced success, missing terminal, malformed control/CID and partial framing then correlated reopen. Historical raced-success fix remains closed; dispose/reopen after uncertainty, no universal completion/reuse guarantee. |
| Firmware/APDU/SCP | Core B6-C003/C004/C005/C006/C007/C009/C010/C011/C012/C015, CTAP X017 | Selected source path is known: initial FIDO SELECT/plain GetInfo, detected gate, Configure/SCP/rebind, no protected CTAP probe. Major-zero config can precede ordinary ForceShort handling. Keep recovery latch/counter/setup failure, parameter clones, hidden copies and secure remaining-command/profile scopes separate. BEFORE/AFTER **N/A** for qualification: pin authorized SCP keys/profile and execute protected CTAP with actual selection survival, chaining/status/cleanup oracle; no plain SELECT recovery substitution. |
| ARKG math/provider | Core C018/C019, CTAP IMP-029/039/041 and X007 | CTAP labels/profile and Core P-256 math are independent owners. BEFORE/AFTER **N/A** for unavailable proof: bind Rust generator/comparator, expired draft-09/-10 mathematics/representation, provider and installed NativeShims source/artifact identities before independent vector/interop claims. Published source identifies no loaded producer. |
| Windows native/access and CCID | Core B4 qualification, additive [Windows successor](integrations/windows-upstream-20261113/deferred-findings-successor.md) | Old built-in synchronous Windows/early-release mechanism **B4-C001 superseded**; **B4-C007** new-owner source/managed prerequisite satisfied. Actual HID-driver races, unknown-release retention, access/ABI/JIT-AOT/deployed producer remain open. BEFORE/AFTER **N/A**: separately record OS/token/driver/architecture, actual open/submission/last-use/checked-close and claim retention/reopen; metadata and managed pipe tests are not HID feature/native closure. CCID/LP64 PCI and exact ISO/final SCP11/vendor sources remain qualified, not diagnosed transmit failures. |

Windows successor delivery also leaves **B1-C005 custom-provider disposal**, B5-C005 reports and
B5-C006 workflows open. FIDO built-in native owner now joins submission return/resource-last-use and
checked close; do not describe the removed synchronous report adapter as current baseline behavior.
Cancellation/terminal wake is not a latency deadline. The unexplained first-open failure, original
attended Windows **17-pass/1-fail** aggregate, unavailable Mac raw artifacts/loaded-image identity,
historical serial-9681620 cause and separate-process/touch/removal/platform lanes retain their evidence
limits; none is a newly counted CTAP defect or an executed result on this baseline. PIV physical/F9
deferrals are untouched.

### CORE-CTAP-X020 — Cleanup when terminal notification logging throws

This **distinct local/shared failure facet** was added by Core boundary review, not a 48th CTAP IMP.
Both backends await terminal resolution before clearing untransferred response. With a primary CTAP
failure, terminal callback failure can be logged; a throwing provider can escape before local clearing.
**Medium:** response secrets and primary exception provenance may be lost on that compound failure.
**Source-confirmed conditional ordering; not reproduced; no-brainer: no** until logger/exception policy
is selected. **Python: P0.** Shared notification/logging policy is Core C-owned; backend finally is CTAP-owned.

**BEFORE — `src/Ctap2/src/Backend/HidBackend.cs`, `SendCborAsync:114–117`:**

```csharp
await userPresenceNotification.ResolveAsync(outcome, primaryException).ConfigureAwait(false);
if (!responseTransferred && !response.IsEmpty)
{
    CryptographicOperations.ZeroMemory(MemoryMarshal.AsMemory(response).Span);
```

**AFTER:**

```text
make untransferred-view cleanup a finally around terminal resolution
select primary-versus-callback-versus-logger exception policy without skipping cleanup
keep transferred successful/decoded/report/APDU copies under their independent owners
```

**Acceptance/dependencies:** actual response-array sentinel with primary CTAP failure, terminal callback
failure and throwing logger, on both backends; assert selected primary/secondary reporting and clearing.
Coordinate IMP-004/027 and Core notification owner. Ordinary callback-failure tests with normal logging
do not establish this compound exit.

### CTAP2 coverage and exclusions

Every original IMP ID has a same-numbered card above except **017**, whose rejected ordering mechanism
is explicitly excluded. The following ledger makes decision/referral/partial dispositions explicit.
Card coverage is not an accepted bug count or implementation authorization.

| Original IDs / facets | Disposition in this draft |
| --- | --- |
| CTAP-IMP-001–016 | 16 same-numbered cards; each compound decision/evidence/cleanup facet retained |
| CTAP-IMP-017 | **Rejected runtime inference**: Ctap2Canonical writer sorts emitted keys; insertion order establishes no update-user/COSE wire defect. Framework evidence and original candidate preserved; corrected comments are documentation-resolved, no encoder fix proposed. |
| CTAP-IMP-018–048 | 31 same-numbered cards; IMP-046/047 explicitly exclude superseded local selector mechanisms while retaining capability/policy/helper/event facets |
| CTAP-DEC-001 | **Resolved into IMP-025** by pinned Guid source/little-endian probe; big-endian execution remains unperformed |
| CTAP-DEC-002 / G-011 / CORE-WA-X008 | One canonical native-user decision above, matching the existing WebAuthn referral; actor trace complete, contract still undecided |
| CTAP-DEC-003 | One qualification card retaining exact product/publication/profile/physical questions; no invented reset timer or production conformance |
| CTAP-XREF-003 / C-012 / B-04 / F-10 | Reader dependency → IMP-006 / CORE-CTAP-X003 and Core input owner, no separate Core bug count |
| CTAP-XREF-001 / WA-XREF-001 | **Documentation resolved** for User/descriptor prose. Does not close WA-IMP-010 recovery or DEC-002 disclosure intent |
| CTAP-XREF-002; WA-IMP-002/003/006/007/009/010/011/012/013/014–019 | Refer to their existing [WebAuthn section](#webauthn) cards, not duplicate CTAP defects: error/cancel, attestation/default AAGUID, token policy, recovery, PRF/protection/ownership, blob and preview facade scope |
| D-14 forced-change-status subclaim | **Withdrawn/rejected**: legacy0x05 PIN_INVALID was correct; permission0x09 differs. IMP-020/021/022 actual setup/restoration/oracle work stays open |
| ARKG seed marker / BE-BS citation | **Resolved evidence corrections**: draft-10 supplies -65700; pinned L3 Recommendation supplies BE/BS citation. IMP-029 profile differences remain; unsigned-output introduction history stays unclaimed |
| F-10 cloned CM-token leak | **Rejected**: constructor borrows token. IMP-018 admission, IMP-024 temporaries and IMP-006 input exposure remain distinct |
| Historical makeCredential up=false / HID raced-success | **Already corrected; excluded as current defects**. Preserve original evidence and current raw/contextual/cancellation limits |
| F project hash mismatch | **Resolved LF Git-blob versus CRLF checkout identity**; no implementation mutation |
| CTAP-DOC-001–018 | Pass1 documentation/independent reviews closed at handoff; re-review only selected post-change surfaces, not a new Pass1 backlog |

The boundary's **26 inherited source-run keys** are covered by the same IMP/DEC/XREF cards and WA
referrals above: **18 CTAP keys, six WA keys, one OpenPGP key and one old-audit key**.
`openpgp-001:PGP-CORE-001` remains the OpenPGP/Core input owner; it transfers no CTAP
profile rules. Original registry statuses and later completed source-return qualifications are preserved,
not rewritten into closure by this draft.

All **CORE-CTAP-X001–X023** are reconciled below as local occurrence groups, not 23 additive bugs.
Resolved boundary wording remains resolved; runtime/policy/qualification facets have these owners:

| Core boundary ID | Canonical card / narrow shared owner | Retained scope |
| --- | --- | --- |
| CORE-CTAP-X001 | Shared membership / Core A-B1 | Known member IDs/process; direct/disjoint/external coordination |
| CORE-CTAP-X002 | Shared workflow / B5-C006; IMP-019/036 | Multi-exchange and post-exchange work, callbacks and disposal |
| CORE-CTAP-X003 | IMP-006 / CTAP-XREF-003 | Reader units versus CTAP profile, OpenPGP separate |
| CORE-CTAP-X004 | IMP-027; IMP-004, Core B6-C002 | Successful/failure typed views and independent transport/APDU owners |
| CORE-CTAP-X005 | IMP-005 | Exact V1/V2/Z exceptional clearing sites |
| CORE-CTAP-X006 | IMP-007 | Strict PIN peer labels, provider import exception boundary |
| CORE-CTAP-X007 | IMP-029 / DEC-003; Core C018/C019 | Wire/profile versus math/provider/Rust evidence |
| CORE-CTAP-X008 | IMP-039, with IMP-041 dispatch | Derived algorithm propagation, independent of seed profile |
| CORE-CTAP-X009 | IMP-041 | Signature DER, not Core key DER |
| CORE-CTAP-X010 | IMP-030 / old audit #4; WA-IMP-017 | Get unsigned key8 and raw envelope, optional direct route |
| CORE-CTAP-X011 | IMP-028 | Structural/contextual response validation, not WA recovery |
| CORE-CTAP-X012 | IMP-037 | uint/cap/preserved-slice policy |
| CORE-CTAP-X013 | IMP-035 | Blob probe/compression/write temporaries |
| CORE-CTAP-X014 | IMP-035 / WA-IMP-013 | Salt/helper failure scope and later PRF copies |
| CORE-CTAP-X015 | IMP-043 / IMP-035 | Generic versus strict metadata and failure output |
| CORE-CTAP-X016 | IMP-004 | Builder-captured clones distinct from final sent arrays |
| CORE-CTAP-X017 | IMP-002 / shared B6 config-SCP qualification | Detected/sentinel/effective firmware and initial probe/ForceShort scope |
| CORE-CTAP-X018 | Shared B5/B6 framing/cancel/recovery | Windows old mechanism superseded; malformed/partial/physical facets retained |
| CORE-CTAP-X019 | Shared B5-C005 / IMP-004/027 | Independently returned reports, not new native staging cleanup |
| CORE-CTAP-X020 | Dedicated X020 card / Core notification | Primary + terminal callback + logger exceptional cleanup |
| CORE-CTAP-X021 | IMP-048; IMP-046 | Reset statement corrected; target binding still open |
| CORE-CTAP-X022 | IMP-020/021/022, DEC-003 and shared platform qualification | Actual assertions/setup/physical/native limits, not test-name proof |
| CORE-CTAP-X023 | IMP-025 / independent WA-IMP-007 | Guid convention versus default-value/equality |

**Section totals:** 47 original IMP coverage cards, two open DEC cards and one additional X020
boundary-facet card: **50 review cards**, not 50 confirmed defects. All three XREF labels and all
23 Core boundary groups are mapped; one rejected IMP and resolved/retracted/superseded subclaims are
explicitly excluded at their actual scope. Human review and implementation remain unstarted.

**Static author verification:** 55 literal BEFORE blocks matched the named ranges in 37 source paths
at the stated C# baseline; all 14 section links/anchors and fence balance were checked. At author handoff,
changes were limited to CTAP2 and its overview row; OATH, WebAuthn and Management
bodies were preserved. Both repository pins matched; Python was clean. This is static evidence,
not compilation, tests, crypto/provider execution or device qualification. No implementation, SDK test,
build, hardware operation, commit or push occurred in this author task.

## Security Domain

### Context and review map

The Security Domain stores the SCP keys used to protect SmartCard traffic for other applets. Its own
administrative commands can replace or delete those keys, certificates and CA restrictions. A secure
channel is not necessarily an administrative channel: SCP11b authenticates the device, not the host.
Reset deliberately sends bogus authentication attempts over the raw connection, outside the normal SCP
graph. Neither reset return nor a retained `IsAuthenticated` value proves factory restoration or live
management authority.

The [published module handoff](security-domain.md) leaves **all eight SD-IMP groups open**. The later
[Core boundary](modules/core/pass-1/core-001/boundary-SecurityDomain.md), its
[candidate map](modules/core/pass-1/core-001/boundaries/SecurityDomain/candidates.md) and frozen
reconciliations refine five inherited SD-DEP questions without turning them into five additional bugs.
Current source windows below were checked at `6ce355468047e8004fd51cf1425808f6e8242ed3`; Python windows
were read in the clean checkout at `4ca60f706af930459138d8dc0f0f953480e1c7a4`. Every `py:` citation in
this section uses that pin. All AFTER blocks remain **unimplemented, unapplied and uncompiled**.

| Card | Kind / severity and rationale | Certainty | No-brainer |
| --- | --- | --- | --- |
| [SD-IMP-001](#sd-imp-001--reset-completion-and-partial-raw-mutation) | Medium: incomplete destructive work can be reported as completed; stale graph/flags complicate recovery | Source-confirmed ordering; intended result/lifecycle unsettled | No |
| [SD-IMP-002](#sd-imp-002--reject-the-empty-normalized-delete-filter) | High potential impact: an unintended unrestricted destructive request; actual card outcome unknown | Empty filter source-confirmed | Yes for rejecting this documented-invalid combination |
| [SD-IMP-003](#sd-imp-003--allowlist-entry-and-counter-profile) | Medium: ambiguous restriction encoding or unsupported update profile; no authorization bypass demonstrated | Empty entry/no counter source-confirmed; final/vendor applicability unresolved | No for the group |
| [SD-IMP-004](#sd-imp-004--malformed-listings-and-future-valid-values) | Medium: silently incomplete metadata or incidental indexing failures can mislead provisioning | Source-confirmed parser paths; no new reproduction | No for DTO/profile policy |
| [SD-IMP-005](#sd-imp-005--reset-options-and-retained-reauthentication-inputs) | Medium: reset silently loses configuration/version and can fail SCP11 locally | Source-confirmed omission | No for reset/reuse policy |
| [SD-IMP-006](#sd-imp-006--wire-ranges-and-import-response-identity) | Medium: truncated target/version or accepted wrong response identity can misdirect administration | Source-confirmed admission/comparison | No for range/open-value policy |
| [SD-IMP-007](#sd-imp-007--tests-that-observe-the-intended-security-boundary) | Low test debt, Medium assurance risk: unrelated failures or public reads can satisfy security claims | Representative current assertions confirmed | No for oracle selection |
| [SD-IMP-008](#sd-imp-008--local-crypto-fixture-and-integration-cleanup) | Medium for real secret-derived storage; Low for public fixed test values; curve byte is a separate wire-quality risk | Representative ownership/cleanup gaps confirmed | No for group; settled local cleanup/curve initialization is mechanical |
| [SD-DEP-002](#sd-dep-002--parameter-containment-and-one-lifetime-owner) | Medium conditional lifetime risk: shared contained owners can be disposed while reset still needs them | Core source reconciliation plus current SD retention | No for API policy |
| [CORE-SD-X003](#core-sd-x003--the-hidden-response-owner) | Medium for sensitive selected objects/KCVs, not public metadata: caller cannot clear the hidden result owner | Current facade confirmed | No for result/API policy |
| [SD-DEP-001](#sd-dep-001--setup-history-host-authentication-and-authority) | Qualification, no assigned defect severity; bool/phase/permission are different facts | Setup flow confirmed; final/vendor phase and permissions unqualified | No |
| [SD-DEP-005](#sd-dep-005--selected-error-provenance-and-diagnostics) | Medium conditional privacy risk from opaque exceptions, not proven key logging | Catch/message confirmed; provider text unqualified | No for logging/error policy |
| [CORE-SD-X007](#core-sd-x007--empty-query-le-and-protected-chaining) | Qualification, no new applet defect severity; wire concerns retain Core owners | Caller reach confirmed; target edition/profile/oracle incomplete | No |
| [Device/producer qualification](#security-domain-device-and-producer-qualification) | Qualification, no assigned defect severity | Historical pre-reset failures and source-specific platform limits | No |

### SD-IMP-001 — Reset completion and partial raw mutation

Reset lists keys, skips duplicate SCP03 components, and makes at most 65 attempts per processed reference.
SCP03 uses INITIALIZE UPDATE with 00/00; a/c use EXTERNAL AUTHENTICATE, b INTERNAL AUTHENTICATE, and
other references PERFORM SECURITY OPERATION. A blocked/security-status response stops that reference.
Ordinary transmission errors are retried, exhaustion does not throw, and an empty listing skips work.
Reselection is not a factory-state check. This is Medium result/recovery risk, not proof that every
exhausted device remains partially reset.

**BEFORE — `src/SecurityDomain/src/SecurityDomainSession.cs`, `BlockKeyAsync:877–885`:**

```csharp
catch (Exception ex)
{
    _logger.LogWarning(ex, "Reset attempt {Attempt} for key {KeyRef} failed to transmit", attempt,
        keyReference);
    continue;
}

if (statusWord is SWConstants.AuthenticationMethodBlocked or SWConstants.SecurityStatusNotSatisfied)
```

**BEFORE — same path, `ResetAsync:795–803`:**

```csharp
Protocol?.Dispose();
Protocol = null;
_protocol = null;
_backend = null;
IsAuthenticated = false;
IsInitialized = false;

await InitializeSessionAsync(token => InitializeAsync(cancellationToken: token), cancellationToken)
    .ConfigureAwait(false);
```

Flags clear only after raw attempts and synchronous protocol disposal. Earlier cancellation, diagnostic
failure or disposal failure can leave old flags/graph after persistent mutation or plaintext SCP
termination. Reinitialization has no factory-style failure-disposal wrapper. Reset and the two-query
CA workflow also lack a whole-public-operation lease; Core's exchange guard does not serialize them.
These are **CORE-SD-X004 / SD-DEP-004** facets, not extra reset bug counts.

**Proposed AFTER — schematic alternatives, decision needed:**

```text
ADMIT_WHOLE_RESET according to the shared workflow contract
before the first raw attempt: invalidate normal secure-session admission/state
record per-reference stop status, exhausted limit, transport failure and skipped/empty listing
choose A: throw an incomplete-reset error with preserved cause/outcome
    or B: return a structured attempted/incomplete/verified result
choose independently: fresh-session-only completion, or explicitly supported reinitialization
on any failure: retire/clean the graph without masking the primary failure
claim restored state only after an agreed independent postcondition check
```

**Python:** `py:yubikit/securitydomain.py:185–221`, `SecurityDomainSession.reset`, uses the same 65-attempt
and instruction mapping, but sends through its protocol, raises other APDU errors and does not perform
C#'s reinitialization. It also has no post-state verification. Its stronger reset docstring is not
evidence of universal mixed-key restoration, nor a reason to copy its protocol route into the raw C# flow.

**Acceptance/dependencies:** meaningful fakes for empty listing, each instruction/skip, stop statuses,
65 nonterminal responses, all/mixed transmission failures, short response, cancellation after a mutation,
throwing diagnostics/disposal and failed reselection/reauthentication. Assert exact raw traffic, next-call
admission/flags, preserved primary error and fresh-session behavior. Coordinate **CORE-B5-C006** and
**CORE-B6-C003/C004**, and [SD-IMP-005](#sd-imp-005--reset-options-and-retained-reauthentication-inputs).
Real restoration remains [device qualification](#security-domain-device-and-producer-qualification).
Reopen both Reset contracts, README/contributor/test reset explanations and the SD AI-DOCS-GUIDE task.

### SD-IMP-002 — Reject the empty normalized DELETE filter

Zero KID/KVN means an omitted filter condition. SCP03 KIDs 01/02/03 become a KVN-only filter, so
`(01,00)`, `(02,00)` and `(03,00)` pass the initial check and then become empty request data.
Potential impact is High because this is key deletion with no selected filter; card behavior is still
unknown. No-brainer **yes** for refusing the already-documented invalid combination, not for changing
legitimate KID-only/KVN-only wildcard semantics or `deleteLast` policy.

**BEFORE — `src/SecurityDomain/src/SecurityDomainTlvEncoding.cs`, `NormalizeDeletePolicy:30–36`:**

```csharp
if (kid == 0 && kvn == 0)
    throw new ArgumentException("At least one of KID or KVN must be non-zero", nameof(keyReference));

if (kid is 0x01 or 0x02 or 0x03)
    kid = 0;

return (kid, kvn);
```

**Proposed AFTER — literal replacement for that window, unapplied/uncompiled:**

```csharp
if (kid is 0x01 or 0x02 or 0x03)
    kid = 0;

if (kid == 0 && kvn == 0)
    throw new ArgumentException("At least one of KID or KVN must be non-zero", nameof(keyReference));

return (kid, kvn);
```

**Python:** `py:yubikit/securitydomain.py:280–303`, `SecurityDomainSession.delete_key`, explicitly rejects
SCP03 KID with zero KVN and emits D0/D2 only for nonzero conditions. This is direct input/wire precedent,
not evidence of a real empty-filter device outcome.

**Acceptance:** through the public session, assert no DELETE transmission for all three normalized-empty
inputs; exact D2/KVN payload for nonzero SCP03 versions; unchanged generic KID-only, KVN-only and combined
filters and `deleteLast` P2. Keep factory KVN FF/first-import replacement, last-set restoration and fresh
authentication before deleting retained old sets qualified as vendor/profile tasks, not host safeguards.
Reopen both Delete contracts, rotation/deletion guidance and lifecycle test descriptions. No hardware
empty-filter experiment is required to establish the host rejection.

### SD-IMP-003 — Allowlist entry and counter profile

Store rejects an empty collection, but an individual empty string becomes `93 00`. Clear separately
emits empty tag 70 to **remove** the restriction; it is not deny-all. Invalid hex already throws
FormatException. Current writes emit no tag 92/counter. Medium reflects restriction/profile ambiguity,
not a demonstrated authentication bypass. Entry rules, serial representation and c support need decisions.

**BEFORE — `src/SecurityDomain/src/SecurityDomainSession.cs`, `StoreAllowListAsync:694–704`:**

```csharp
var buffer = new ArrayBufferWriter<byte>();
foreach (var serial in serials)
{
    using var serialTlv = new Tlv(TagSerial, Convert.FromHexString(serial));
    buffer.Write(serialTlv.AsSpan());
}

using var kidKvnTlv = new Tlv(TagKidKvn, keyReference.AsSpan());
using var controlRefTlv = new Tlv(TagControlReference, kidKvnTlv.AsSpan());
using var serialsListTlv = new Tlv(TagSerialsAllowList, buffer.WrittenSpan);
var serialsDataTl = TlvHelper.EncodeList([controlRefTlv, serialsListTlv]);
```

**Proposed AFTER — schematic, not an invented counter API:**

```text
validate each decoded serial against the accepted byte/integer representation before sending
keep empty collection rejected; keep explicit Clear as restriction removal
choose A: do not support c updates; retain/reject at the accepted profile boundary
    or B: introduce a reviewed counter contract only after target final/vendor rules are obtained
for B: encode tag 92 only where required and observe atomic list/counter update and exhaustion rules
```

**Python:** `py:yubikit/securitydomain.py:84–88,251–263`, `_int2asn1` / `store_allowlist`, accepts integers
and prefixes 00 when the high bit requires a positive ASN.1 encoding; an empty sequence writes empty
70. It has no counter input/tag 92 either. Do not transplant that different public representation or
empty-collection behavior into the C# Store/Clear split without a decision.

The retained **SCP11 1.3.0.13 Public Review, August 2023**, §§4.3/7.5.1/7.9, scopes matching to the
certificate directly signed by CA-KLOC (OCE or first intermediate). Draft §7.9 requires AUTHENTICATED,
and, if c is supported, a nonresetting per-CA counter and tag 92 for c only; missing/nonincreasing/exhausted
counter returns 6A80 and successful update is atomic. §4.3's revocation condition retains a nonempty
list. Final **1.4, March 2024** and exact YubiKey applicability remain uninspected, not settled by Python.

**Acceptance/dependencies:** settle final/vendor profile with **CORE-B6-C009/C010** before c support.
Assert exact serial order/leading-byte behavior and no transmission on rejected entries; separate Store
and Clear wire tests. Authorized profile tests need approved and unlisted CA-direct chains, intermediate
scope, clear behavior and, only if selected, counter success/replay/exhaustion/atomicity. Existing approved
chain/clear assertions are not unlisted-chain rejection evidence. Reopen allowlist XML, role/setup guide
and audit-repro/integration explanations; do not introduce an unqualified c administration recipe.

### SD-IMP-004 — Malformed listings and future-valid values

Key information requires a two-byte reference, then reads descriptor pairs and silently drops an odd
tail. CA listings pair TLVs without checking their tags, drop an odd final TLV and index short references.
Medium reflects misleading provisioning metadata or incidental exceptions, not a demonstrated card
failure. Reject structurally malformed/security-critical data while retaining future-valid identifiers;
raw-result/DTO representation still needs a decision.

**BEFORE — `src/SecurityDomain/src/SecurityDomainSession.cs`, `ListKeyInformationAsync:275–279`:**

```csharp
while (currentValue.Length >= 2)
{
    components.Add(new SecurityDomainKeyInfoComponent(currentValue[0], currentValue[1]));
    currentValue = currentValue[2..];
}
```

**BEFORE — same path, `GetCaIdentifiersAsync:384–391`:**

```csharp
while (caTlvObjects.Length >= 2)
{
    var caIdentifierTlv = caTlvObjects[0];
    var keyReferenceTlv = caTlvObjects[1];

    var keyReferenceData = keyReferenceTlv.Value.Span;
    var keyReference = new KeyReference(keyReferenceData[0], keyReferenceData[1]);
    caIdentifiers.Add(new CaIdentifier(keyReference, caIdentifierTlv.Value.ToArray()));
```

**Proposed AFTER — schematic alternatives:**

```text
require complete descriptor pairs and complete CA/reference groups
validate required tags and reference length against the selected profile
malformed structure -> consistent BadResponse provenance, no partial successful listing
unknown but valid descriptor/value -> retain it, do not reject merely because it is new
choose raw escape hatch: enriched result with complete raw listing, or explicit parallel raw API
retain KLOC-before-KLCC order and only the selected absent-group status handling
```

**Python:** `py:yubikit/securitydomain.py:122–129,136–165`, `get_key_information` also loses an odd tail
through `zip`; `get_supported_ca_identifiers` indexes pairs without these checks. `KeyRef` validates
two-byte length (`py:yubikit/core/smartcard/scp.py:145–160`). Shared weaknesses are not desired policy;
Python mappings also differ from C# ordered list/duplicate representation.

**Acceptance:** fake odd descriptor tail, truncated reference, odd CA TLV count, swapped/wrong tags,
malformed TLV length and valid unknown descriptors; assert chosen exception/result/raw preservation,
not just constructor guards. Keep empty listing and absent KLOC/KLCC behavior separately tested. Generic
GetData can fetch raw bytes, but that is not a complete listing result attached to these parsers.
Reopen both listing contracts, KeyInfo/CaIdentifier/component docs and parser tests; coordinate Core TLV
grammar/ownership without making every metadata byte sensitive.

### SD-IMP-005 — Reset options and retained reauthentication inputs

Create passes caller configuration/version; reset's two callbacks pass only cancellation. After a graph
rebuild `InitializeAsync` clears the explicit-version indicator and selects 5.3.0. This is a source-confirmed
local omission, even though Core later centralized readiness publication. Medium reflects changed framing
policy and a local SCP11 failure after destructive work, not a device rejection.

**BEFORE — `src/SecurityDomain/src/SecurityDomainSession.cs`, `CreateAsync:148–150`:**

```csharp
(session, token) => session.InitializeAsync(
    options?.ProtocolConfiguration, options?.FirmwareVersionOverride, token),
cancellationToken);
```

**BEFORE — same path, `ResetAsync:802–803` and `InitializeAsync:175–176` (separate windows):**

```csharp
await InitializeSessionAsync(token => InitializeAsync(cancellationToken: token), cancellationToken)
    .ConfigureAwait(false);
```

```csharp
_hasExplicitFirmwareVersion = firmwareVersion is not null;
var resolvedFirmwareVersion = firmwareVersion ?? FirmwareVersion.V5_3_0;
```

**Proposed AFTER — schematic, choose one reset contract:**

```text
A: retain accepted creation configuration and exact-version policy, not the mutable options object
   reapply both for every reset initialization; explicitly decide whether retained SCP inputs are reused
   fail/retire safely if those keys were removed; preserve setup versus card-error provenance
B: reset ends this session; do not silently reauthenticate or imply preservation
   caller awaits completion/disposal and opens a new session with explicit options
```

No-brainer **no** for the group: preserving version/configuration and reusing potentially deleted keys
are independent choices. SCP11 requires known effective 5.7.2+ in Core; a supplied override is policy,
not device detection. Losing it can cause wrapped NotSupported after SELECT but before a new handshake.

**Python:** `py:yubikit/securitydomain.py:103–116,185–221`, `__init__` uses 5.3.0, authentication is a
separate method, and reset does not recreate/configure the session. There is no equivalent override
retention contract to copy. **SD-DEP-003 / CORE-SD-X001** map here; **CORE-B5-C001 / CORE-B6-C015**
own shared effective-version/task policy, not the omitted applet arguments.

**Acceptance:** nondefault protocol configuration/explicit version, both reset callbacks, selected
SCP03/SCP11/no-SCP policy, removed retained keys, local-gate provenance, failed reinit cleanup and next
operation. The integration helper creates a *fresh* test session with state version/configuration after
disposing its reset session; that does not prove reset preserved options. Reopen factory/reset/Generate
XML, README/contributor firmware guidance, helper explanations and affected shared migration/version text.

### SD-IMP-006 — Wire ranges and import response identity

All three PutKey overloads cast integer `replaceKvn` to a byte; GET DATA takes the low two identifier
bytes; Generate sends caller KID without a local SCP11-family check. SCP03 KCV checking compares three
fixed-time three-byte values, but accepts any response version and extra bytes. Medium reflects wrong
administrative target/confirmation; no misprovisioned device is demonstrated.

**BEFORE — `src/SecurityDomain/src/SecurityDomainSession.cs`, `PutKeyAsync(StaticKeys):477–480`:**

```csharp
var command = new ApduCommand(ClaGlobalPlatform, InsPutKey, (byte)replaceKvn,
    (byte)(0x80 | keyReference.Kid),
    buffer.WrittenMemory);
var response = await TransmitAndGetResponseDataAsync(command, cancellationToken).ConfigureAwait(false);
```

**BEFORE — `src/SecurityDomain/src/SecurityDomainKeyMaterial.cs`, `ValidateKcv:65–66,76–78`
(separate windows):**

```csharp
if (response.Length < 10)
    throw new InvalidOperationException("Response too short for KCV validation");
```

```csharp
var actualKcvEnc = response.Slice(1, 3);
var actualKcvMac = response.Slice(4, 3);
var actualKcvDek = response.Slice(7, 3);
```

**Proposed AFTER — schematic, no range/profile decision silently selected:**

```text
choose byte/ushort request types or reject integers outside the accepted wire ranges before mutation
decide tighter vendor custom-KVN/factory/replacement restrictions separately from representable range
decide Generate's admitted KIDs against supported SCP11 profile, not catalog membership alone
bind SCP03 response version to requested KVN; retain fixed-time KCV comparisons
choose exact 10-byte response or explicitly parse/preserve justified future extensions
select malformed/mismatch exception provenance consistently with EC checksum handling
```

**Python:** `py:yubikit/securitydomain.py:306–325,327–380`, `generate_ec_key` forwards KID and exposes
more curve choices; `put_key` builds expected response as KVN followed by KCVs for StaticKeys, then
requires exact equality. `_int2asn1` is unrelated to KVN validation. Python's plaintext bytes comparison
is not a replacement for .NET fixed-time secret-derived comparisons. `get_data:118–120` delegates
integer header admission; this read does not establish every downstream range check.

**Acceptance/dependencies:** observable wire/no-send cases for negative/oversized int targets and
replacement versions at all overloads; distinct representable versus vendor-reserved versions; admitted
and rejected KIDs; correct KCVs with wrong version, truncation, extra tail and each mismatch. EC checksum
already compares the complete expected one-byte response, so do not duplicate that defect. Tie key
reference alias/header/filter consistency to **CORE-B6-C008**, and profile admission to C009. Reopen
Import/GetData/Generate/KCV contracts, rotation guidance and exact-wire/failure test descriptions.

### SD-IMP-007 — Tests that observe the intended security boundary

Broad exceptions can be satisfied by discovery, transport, setup or cancellation failure. The generated
key test's second session only lists public metadata without SCP11 parameters. Convenience listing
observes transmission, not disposal. Configuration tests that assert nonnull do not establish effective
formatting/options. These are Low test debt with Medium assurance risk where used as security evidence;
test names and corrected commentary do not close the engineering group.

**BEFORE — `src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/SecurityDomainSession_NegativeTests.cs`,
`Scp11b_WithWrongPublicKey_FailsAuthentication:59–65`:**

```csharp
await Assert.ThrowsAnyAsync<Exception>(async () =>
{
    await state.WithSecurityDomainSessionAsync(false,
        session => Task.CompletedTask,
        scpKeyParams: wrongKeyParams,
        cancellationToken: ct);
});
```

**BEFORE — `src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.UnitTests/SecurityDomainSessionTests.cs`,
`IYubiKeyExtensions_ListKeyInformationAsync_DisposesSession:431–434`:**

```csharp
Assert.NotNull(keyInfo);
// Verify the connection was used; this assertion does not observe session or connection disposal.
await connection.Received()
    .TransmitAndReceiveAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>());
```

**Proposed AFTER — schematic oracle choices, not uncompiled fake SDK methods:**

```text
negative auth: establish a positive control with the same authorized state/route first
then assert chosen wrapper, underlying verification failure and relevant status/phase
generated key: retain point/persistence checks; add a fresh SCP11b session using the trusted returned key
              and a protected exchange if authentication is the intended additional test
convenience factory: observe actual session/hidden-connection disposal and claim reuse separately
configuration: observe effective command formatting/gates, not only nonnull session
fixture: add independent host cryptogram/MAC and malformed/error/empty/chaining checks only for selected scope
```

**Python:** `py:yubikit/securitydomain.py:110–116,306–325`, `authenticate` and `generate_ec_key` are
separate operations. They confirm that generation alone is not an authentication oracle. No corresponding
Python test suite or C# disposal precedent was inspected. The C# fixture's four RFC 4493 CMAC vectors
verify only its primitive if executed; EXTERNAL AUTHENTICATE currently advances fixture state rather
than checking host cryptogram/MAC. This is not independent full SCP03 conformance.

**Acceptance/dependencies:** make wrong public/symmetric key, b command rejection (exact command and
status, not a broad Generate-then-Delete catch), c protected public listing, genuine administrative
authority, factory ownership and options distinct oracles. Coordinate **CORE-B6-C013**,
[host/profile qualification](#sd-dep-001--setup-history-host-authentication-and-authority) and
[SD-IMP-008](#sd-imp-008--local-crypto-fixture-and-integration-cleanup). No placeholder/skipped tests,
validation-only guards or hardware credit from compilation. Reopen test descriptions/README only when
actual assertions/fixture coverage change; keep native-backed certificate bundle tests at their real scope.

### SD-IMP-008 — Local crypto, fixture and integration cleanup

The pooled plaintext key copy is already zeroed in finally. Separate stack KCVs/AES output, allocated
encrypted values, private-import encrypted storage, fixture derived keys/MAC/wire captures and integration
owners need their own lifetimes. Public certificate/CA/reference bytes are not automatically secrets;
encrypted payload is not equivalent to plaintext exposure. Medium applies to real secret-derived storage,
Low to publicly fixed fixture keys. No native erasure or memory exploit is established.

**BEFORE — `src/SecurityDomain/src/SecurityDomainKeyMaterial.cs`, `CalculateKcv:104–107`:**

```csharp
Span<byte> encrypted = stackalloc byte[16];
aes.EncryptCbc(kcvInput, iv, encrypted, PaddingMode.None);

encrypted[..3].CopyTo(output);
```

**BEFORE — `src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.UnitTests/Scp03HandshakeFakeConnection.cs`,
`Dispose:104`:**

```csharp
public void Dispose() => _staticKeys.Dispose();
```

**BEFORE — `src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/SecurityDomainSession_Scp11Tests.cs`,
`GetOceCertificateChainAndPrivateKey:317–318`:**

```csharp
var privateKey = ECPrivateKey.CreateFromParameters(ecParams);
return (certChain, privateKey);
```

The PKCS12 helper exports private D then creates a copying wrapper without clearing the exported source.
The c helper repeats that pattern. Loaded certificates omitted from the returned chain, inline default
parameters, unscoped Scp11 owners/providers and failed fixture construction remain distinct owners.
Static 100-second CTS fields begin at class initialization, persist across tests and are undisposed;
some calls omit tokens. Local finally cleanup can reuse an already-canceled token. The Track base's
independent 60-second reset is useful only for tracked tests and still does not verify restoration.

**Proposed AFTER — schematic cleanup, dependent on the contained-owner contract:**

```text
for every local: classify sensitivity and identify exact owner/aliases before changing cleanup
clear local KCVs/AES secret-derived output in finally, including crypto/mismatch failures
clear/return only owned encrypted/wire storage according to accepted semantic policy
fixture Dispose: clear all owned derived/MAC/capture buffers; clear transient key copies on all exits
PKCS12 export: clear exported D after copying; dispose loaded objects on partial failure
transfer exactly the returned key/certificate identities to one parameter owner; dispose excluded certs
scope test CTS/parameters/providers/fakes per test; pass tokens to actual calls
independent bounded cleanup request; report primary and cleanup outcomes without masking either
```

**Separate mechanical wire-quality facet, CORE-SD-X002:** PutEcKeyAsync supplies an uninitialized
one-byte stack span as curve value. P-256 import is already locally required and Generate explicitly
emits 00. Source confirms missing explicit initialization, not an observed garbage byte.

**BEFORE — `src/SecurityDomain/src/SecurityDomainSession.cs`, `PutEcKeyAsync:653`:**

```csharp
using var paramsTlv = new Tlv(KeyTypeEccKeyParams, stackalloc byte[1]);
```

**Proposed AFTER — literal single-line replacement, unapplied/uncompiled:**

```csharp
using var paramsTlv = new Tlv(KeyTypeEccKeyParams, (ReadOnlySpan<byte>)[0x00]);
```

**Python:** `py:yubikit/securitydomain.py:61–77,327–380`, `Curve.SECP256R1=00` and `put_key` explicitly
encodes `Curve._from_key(sk)`. `_encrypt_cbc:91–97` and private integer/bytes encoding provide crypto
flow precedent only; immutable Python bytes/provider GC give no .NET deterministic-zeroing equivalence.
`py:yubikit/core/smartcard/scp.py:174–187,247–375` retains references/derived immutable buffers, not
the C# disposable containment contract. No Python fixture-cleanup precedent was inspected.

**Acceptance/dependencies:** coordinate [SD-DEP-002](#sd-dep-002--parameter-containment-and-one-lifetime-owner)
and **CORE-B6-C002/C006/C007**; observe actual owned buffers before release on success, encryption/parse/
assertion/callback failure and disposal, with positive controls. Do not read returned pool memory or
reintroduce withdrawn seams. Test source D versus cloned owner separately and ensure cleanup cannot
dispose a borrowed key early. Assert exact `F0 01 00` EC parameter bytes for both import variants.
Native/provider certificate/key erasure needs separate qualified execution; public cert disposal is
resource ownership, not secret wiping. Reopen helper/private-import security notes, fixture/test cleanup
descriptions and examples when their actual lifetimes change.

### SD-DEP-002 — Parameter containment and one lifetime owner

Security Domain retains the exact caller SCP parameter object for reset; it does not own/dispose it.
Core's frozen **CORE-B6-C007 / CORE-C-C020** reconciliation establishes that StaticKeys copies source
arrays, SCP03 parameters retain/dispose that same StaticKeys, and SCP11 parameters copy the collection
but retain/dispose its private-key wrapper and certificate objects. Record clones share contained owners.
The supplied SD public key is not disposed by parameters. This is Medium conditional lifetime risk if
one alias disposes contents while a session/reset still needs them; no new double-disposal failure is proven.

**BEFORE — `src/SecurityDomain/src/SecurityDomainSession.cs`, constructor `SecurityDomainSession:94–101`:**

```csharp
private SecurityDomainSession(
    ISmartCardConnection connection,
    ScpKeyParameters? scpKeyParams = null)
    : base(connection)
{
    _logger = Logger;
    _scpKeyParams = scpKeyParams;
}
```

**Proposed AFTER — schematic API alternatives; no-brainer no:**

```text
A: preserve caller-owned parameter contract; one owner encloses every session and reset
   give each shared contained key/certificate identity one disposal authority
B: redesign containment with explicit independent ownership/borrowed contents
   decide clone/disposal semantics in Core before migrating applet callers
in either case: clear original secret sources separately, never clear a borrowed private D alias
```

**Python:** `py:yubikit/core/smartcard/scp.py:174–187`, frozen dataclass parameter fields retain key and
certificate references, but lack C#'s IDisposable behavior. `py:yubikit/securitydomain.py:110–116,327–365`
uses parameters/keys for authentication/import, not C# lifetime-retained reset. It cannot settle wrapper
ownership. [SD-IMP-008](#sd-imp-008--local-crypto-fixture-and-integration-cleanup) owns local cleanup;
do not duplicate Core containment defects or Management's creation-only borrowing tests here.

**Acceptance:** choose Core owner policy, observe original source/copy, record-clone contents, repeated
session/reset use and early/late disposal under real ownership observers. Keep caller connection alive
through direct session disposal; hidden convenience connections have a different owner. Security Domain
does not retain/use UserPresencePrompt, unlike generic wording for adopted applets. Reopen parameter/key
XML with Core, SD setup/private-import guidance and owned integration scopes together; no implied deep
clone or automatic prompt adoption.

### CORE-SD-X003 — The hidden response owner

The final response array remains alive through the returned ReadOnlyMemory view, including after session
disposal. The facade hides ApduResponse's supported clear handle. Clearing an independent caller copy
does not reach that array, transport fragments or decrypted temporaries. Medium applies to KCV/generic
sensitive objects; ordinary key references, public points and certificates do not require secret zeroing.
Aliases: **SD-DEP-004 / SD-IMP-008**, shared copies **CORE-B6-C002**.

**BEFORE — `src/SecurityDomain/src/SecurityDomainSession.cs`, `TransmitAndGetResponseDataAsync:825–829`:**

```csharp
EnsureInitializedBackend();
var response = await _backend.SendAsync(command, cancellationToken: cancellationToken).ConfigureAwait(false);
// This alias keeps final response storage alive but hides its supported Clear owner. A caller
// copy has a separate lifetime; session disposal does not erase this result or earlier copies.
return response.Data;
```

**Proposed AFTER — schematic alternatives, decision needed:**

```text
A: return an explicit owned-response/result for generic sensitive GET DATA; caller disposes/clears it
B: copy into a chosen caller-owned destination; inspect status then clear final internal owner
internal parsed/KCV paths: inspect status, consume result, clear their own sensitive final response in finally
preserve required public results; solve earlier transport/fragment/decrypted owners in Core separately
```

**Python:** `py:yubikit/securitydomain.py:118–120`, `get_data` returns immutable bytes through its
protocol. That is byte-result precedent, not a supported clear-owner or pooled-loan contract.

**Acceptance:** actual final-owner/caller-result identity and positive-control cleanup tests, success
and failed parsing/KCV paths, result survival/disposal and retained input through the full awaited task.
Core Clear also clears status bytes: inspect status first. Never suggest writable casts of borrowed
read-only memory. Coordinate final/earlier copy owners, not an end-to-end erasure claim. Reopen GET DATA
and parsed-result XML, Core response/TLV reference, SD security notes and affected buffer observers.

### SD-DEP-001 — Setup history, host authentication and authority

Core publishes readiness after the full initialization callback and the inherited auth flag after SCP
setup. Those flags are not live security-level probes. SCP11b authenticates SD to OCE only; a/c use
OCE private key and CA-KLOC-signed host chain. Device certificate loading/CA identifier storage neither
provisions the host chain nor validates trust. **CORE-SD-X006 / CORE-B6-C009/C010** own these profile
distinctions; **CORE-SD-X005** adds the local exception boundary below.

**BEFORE — `src/SecurityDomain/src/SecurityDomainSession.cs`, `InitializeAsync:181–187`:**

```csharp
effectiveProtocol = (ISmartCardProtocol)await ConfigureProtocolAsync(
        protocol,
        resolvedFirmwareVersion,
        configuration,
        _scpKeyParams,
        cancellationToken)
    .ConfigureAwait(false);
```

**Proposed AFTER: N/A for qualification.** A new authority flag, privileged probe or broader rejection
policy would need an explicit API/profile decision; none is proposed as literal code. No-brainer no;
no assigned defect severity because no administrative bypass has been established.

**Python:** `py:yubikit/securitydomain.py:110–116,223–228,327–343` distinguishes b's lack of OCE
authentication but uses a processor-presence check for import. `py:yubikit/core/smartcard/scp.py:300–375`,
`ScpState.scp11_init`, sends host certificates, verifies receipt and returns state. Its comments cite
final 1.4 for command clauses and final 1.3 for KDF; these comments are **not inspected final sources**.

**Qualification procedure/acceptance:** obtain the selected final GP/YubiKey profile, then distinguish
receipt/setup, a/c's first correct-MAC command and actual per-command administration. The retained
August 2023 **1.3.0.13 Public Review §4.3** includes that first command in OCE authentication;
§4.6 forbids c PUT KEY/DELETE[keys]/SET STATUS and conditionally recommends against STORE DATA key
loading outside controlled environments. Keep description, SHALL and conditional SHOULD distinct.
Test b/c public metadata separately from authorized and rejected mutations, with exact phase/status and
positive controls on authorized devices. Final **1.4 March 2024**, final **1.3 November 2021** and draft
are different editions. Reopen role/auth/factory XML, Core SCP11 parameter/summary docs, allowlist tests
and migration actor distinctions only when evidence or the accepted contract changes.

### SD-DEP-005 — Selected error provenance and diagnostics

Preparation and SELECT precede SD's Configure catch. With supplied parameters it wraps only ApduException,
BadResponseException and NotSupportedException; cancellation is unchanged. Provider/missing-DEK
InvalidOperation failures escape that filter. StatusWord copies only an immediate ApduException, so
Core's nested CLA-not-supported translation can leave it null. These are source observations, not a
mandate to wrap everything. Opaque provider exception text can reach messages/logs: Medium conditional
privacy risk, no proof that a real key was logged.

**BEFORE — `src/SecurityDomain/src/SecurityDomainSession.cs`, `InitializeAsync:193–198`:**

```csharp
catch (Exception ex) when (_scpKeyParams is not null &&
    ex is ApduException or BadResponseException or NotSupportedException)
{
    // Wrap selected configuration failures only when SCP was requested. Preparation and
    // application selection occur before this catch; operation failures are outside it.
    throw new SecureChannelException(ex);
```

**BEFORE — `src/SecurityDomain/src/SecureChannelException.cs`, `GetMessage:73`:**

```csharp
return $"Failed to establish a secure channel (SCP) with the Security Domain: {innerException.Message}";
```

**Proposed AFTER — schematic alternatives, decision needed:**

```text
choose diagnostics policy with Core: opaque exception text/object retained only on an explicit trusted path
normal log/message path -> bounded operation/phase/status metadata, no payload/provider-text privacy promise
preserve original error for caller inspection; decide nested-status extraction and catch expansion separately
distinguish local version gate, card rejection, cryptographic verification and reset/cleanup failure
```

**Python:** `py:yubikit/securitydomain.py:110–120,209–220` forwards setup/operation errors and handles
selected reset statuses differently; it supplies no matching C# wrapper/redaction policy. `store_ca_issuer:270`
logs SKI metadata, not a model for secret logging. No provider/plugin capture was executed.

**Acceptance/dependencies:** **CORE-SD-X005 / CORE-B6-C014 / CORE-C-C008** retain global factory,
static logger/provider lifetime and opaque-error policy. Observe immediate/nested APDU status, pre-SELECT
versus Configure failures, cancellation, missing DEK/provider failure and synthetic sensitive markers
through selected log sinks without real secrets. The old inherited formatter-payload claim is
**superseded**: current formatters have no logs; ordinary PC/SC traces name headers/length, not payload.
That correction does not prove universal redaction or safe exception logging. Reopen both factories,
SecureChannelException, contributor diagnostics, migration exception prose/map scalar together; preserve
unrelated DOC-002 and do not change the already-accurate selected-wrap contract without a decision.

### CORE-SD-X007 — Empty query, Le and protected chaining

Normal key-info/recognition/CA GET DATA calls send no request payload and default Le=0. Core SCP03 still
pads/encrypts that empty field. Final **Amendment D SCP03 1.1.2 Public Release, March 2019, §6.2.6**
requires the counter increment but no encryption for absent command data when **C-DECRYPTION plus
C-MAC** are selected. This directly reaches **CORE-B6-C011**, not a separate SD encryption defect.
Short format accepts 0..255, not explicit 256; short/extended formats append zero Le. Large certificate
GET/STORE can chain. Protected response/error/61xx handling stays **CORE-B6-C001/C012**.

**BEFORE — `src/SecurityDomain/src/SecurityDomainSession.cs`, `GetDataAsync:223–231`:**

```csharp
var command = new ApduCommand
{
    Cla = 0,
    Ins = InsGetData,
    P1 = (byte)(dataObject >> 8),
    P2 = (byte)(dataObject & 0xFF),
    Data = requestData,
    Le = expectedResponseLength
};
```

**Proposed AFTER: N/A for applet qualification;** repair/edition selection belongs to Core's original
wire cards. No applet-only workaround or altered Le is proposed. No-brainer no, no new defect severity.

**Python:** `py:yubikit/core/smartcard/scp.py:204–216`, `ScpState.encrypt`, also pads empty bytes and
increments its counter. `unmac/decrypt:223–244` supplies algorithm flow but not an independent protected
chain/status oracle. Agreement does not override final 1.1.2. Code-cited **1.1.2.6 Public Review,
December 2019** has uninspected clauses; final **1.2 April 2020** is separate. Exact ISO/IEC
7816-4:2020 edition 4 / amendment 1:2023 target clauses remain unavailable.

**Procedure/acceptance:** Core selects exact supported editions/profile and an independent wire oracle;
then verify SD empty GET DATA, counter progression, positive/zero Le in configured formats and large
certificate command/response chains, including tamper/padding/status-only errors and 61xx continuation.
Final 1.1.2 §6.2.5 distinguishes 9000/62xx/63xx from unprotected error status-only replies and requires
R-MAC verification when selected; §6.2.7 omits encryption for absent response data. Keep those conditions
with the claims. The synthetic fixture's single protected 9000 reply proves none of the broader profile.
Normal SD post-setup methods do not reach **CORE-B6-C005**'s INS82 exemption; raw reset 82 bypasses
that processor. Reopen GET/STORE/XML, configured format guidance and certificate/wire tests after the
Core outcome. One logical STORE DATA can be several physical APDUs.

### Security Domain device and producer qualification

**BEFORE / proposed AFTER: N/A — evidence work, not an appropriate code change.** No assigned defect
severity and no-brainer no. The module Pass 1 performed no hardware mutation. Historical serial 125
returned **6A88** for both `80 50 FF 00` and `80 50 00 00` INITIALIZE UPDATE. EXTERNAL AUTHENTICATE
and RESET were never reached. Neither those failures nor a later metadata read establishes reset,
factory-key availability, successful rotation or SCP11 permission/counter behavior. Serial 9681620's
historical two-key failure remains unexplained; it is not a new SD reset observation.

**Python:** `py:yubikit/securitydomain.py:185–221`, `reset`, is only an attempted-blocking algorithm
precedent; no Python live qualification or installed-provider provenance was inspected.

**Future procedure/acceptance:** separately authorize a dedicated key, destructive scope, escrow and
restoration plan. Reinventory physical identity, firmware, USB CCID versus NFC reader and existing
key/certificate/CA state before any command. Resolve setup first without assuming factory keys or
bypassing discovery guards. For authorized reset/rotation/profile cases, retain exact nonsecret command
phase/status, independent fresh authentication/post-state checks, failure/cancellation outcome and
independent cleanup/readback. Distinguish factory versus mixed/custom state and reader/firmware outcomes;
a successful helper callback or skipped lane is not restoration evidence. No touch/PIN workflow is
provided by this applet, and no PIV touch/hotplug/F9 operation is imported or authorized here.

SmartCard native acceptance stays with **CORE-B1-C006/C009/C010/C012** and the narrowly applicable
**CORE-B4-C005/C006/C009** producer/loader/PCSC ABI-string/error scopes. Identify actual loaded library,
shim/package/build/header provenance, OS/architecture, native completion/drain/transaction-end/release
and physical claim outcome; source publication or managed fakes cannot prove them. Original Rust/
generator/comparator/capture gaps remain with their original shared owners when needed for that evidence,
not new SD tooling assignments.

The [Windows successor](integrations/windows-upstream-20261113/deferred-findings-successor.md) supersedes
old built-in HID report routing/staging (**CORE-B4-C001/C007**) at its accepted source/managed-test scope,
not SD's SmartCard/raw-reset route, workflow, independent APDU/SCP copies or native/platform closure.
Do not import Windows FIDO/OTP report-driver lanes into this SmartCard-only applet. PC/SC owner renames
do not close native ABI/loaded-producer/transaction/logging questions. macOS NativeShims 1.18.1 and
reported bounded PCSC results do not identify the locally loaded binary or prove later-source NFC,
removal/cancellation or AOT qualification. Reopen SD factory/raw-reset/result/error/task documentation
only for the actual changed route or newly bounded evidence.

### Security Domain coverage ledger and exclusions

Each original ID below has one disposition; overlaps are aliases, not additional accepted defects.
Frozen [candidate reconciliation](modules/core/pass-1/core-001/triage/final/candidate-reconciliation.json),
[inherited reconciliation](modules/core/pass-1/core-001/triage/final/inherited-reconciliation.json) and
[continuity](modules/core/pass-1/core-001/triage/final/reconciliation-4/deferred-finding-continuity.json)
retain the historical states/reopening conditions. Completed documentation is not engineering closure.

| Original module ID | Canonical remaining disposition |
| --- | --- |
| SD-IMP-001 | Reset completion card; raw partial mutation/stale state joins CORE-SD-X004, not a second reset count |
| SD-IMP-002 | Empty-normalized DELETE card; device empty-filter outcome remains unknown |
| SD-IMP-003 | Allowlist entry/counter/profile card; Store/Clear split and malformed-hex docs already corrected |
| SD-IMP-004 | Malformed/future-valid listing card; no silent tail loss endorsed |
| SD-IMP-005 | Reset options/reauthentication card; Core readiness centralization does not close omission |
| SD-IMP-006 | Range/KID/KCV identity card; complete EC checksum already present, excluded from duplicate defect |
| SD-IMP-007 | Behavioral security/disposal/configuration/fixture oracle card; comment correction alone excluded as closure |
| SD-IMP-008 | Local cleanup/fixture/integration card; explicit EC curve byte facet joins CORE-SD-X002 |
| SD-DEP-001 | Setup/host-authority qualification and SD-DEP-005 selected errors; Core owns base state/profile mechanics |
| SD-DEP-002 | Contained-owner card plus local SD-IMP-008 cleanup; CORE-B6-C007/CORE-C-C020 remain shared owners |
| SD-DEP-003 | SD-IMP-005; CORE-B5-C001/CORE-B6-C015 shared effective-version/override policy, no device-detection inference |
| SD-DEP-004 | SD-IMP-001/005 raw workflow, CORE-SD-X003 response, X007 wire and device/producer card; direct borrow/hidden own remain distinct |
| SD-DEP-005 | Diagnostics card; stale formatter-payload claim superseded, opaque provider text/global lifetime remain open |

| Original Core boundary ID | Reconciled remaining facet / resolved exclusion |
| --- | --- |
| CORE-SD-X001 | SD-IMP-005 / SD-DEP-003; immediate SCP11 version recipe is now documented, options/runtime not fixed |
| CORE-SD-X002 | SD-DEP-002 / SD-IMP-008; contained-owner wording corrected; temporary/test cleanup and curve initialization remain |
| CORE-SD-X003 | Hidden-response-owner card; documented lifetime limitation, no owned-result fix |
| CORE-SD-X004 | SD-IMP-001/005; destructive/partial-reset wording and fresh reopen advice corrected, lifecycle/result policy remains |
| CORE-SD-X005 | SD-DEP-005 / SD-DEP-001; selected catch/nested status/log provenance now documented; privacy policy/qualification open |
| CORE-SD-X006 | SD-DEP-001 / SD-IMP-003/007; first-MAC phase and c/b distinctions documented at draft scope, final/vendor and real oracles open |
| CORE-SD-X007 | Empty/Le/chaining qualification card; Core owns grammar/crypto/response status mechanics |
| CORE-SD-X008 | SD-IMP-007/008; precise fixture/test descriptions documented; assertion/resource/cleanup evidence still open |

**Shared Core applicability, all sixteen B6 caller dispositions:** C001 grammar/Le and C011/C012
empty/protected chaining → X007; C002 copies → X003/IMP008; C003 recovery and C004 mutation notification
→ IMP001 plus Core recovery/state owner; C006 partial handshake ownership → DEP002/IMP008 and selected
error provenance; C007 containment → DEP002; C008 cached KeyReference wire/header divergence → IMP006
shared dependency (use fresh constructors, not record `with` as a workaround); C009 admission/trust and
C010 editions/roles → IMP003/DEP001; C013 oracle → IMP007; C014 logging → DEP005; C015 task/reset/reselect
qualification → IMP005/device card. **C005** has no normal post-establishment SD arbitrary-INS82 path;
handshake initialization and raw reset are different routes. **C016 ATR hash** is not used by SD as a key
or comparison and remains only its transport identity owner's context. Those two are explicit nonlocal
exclusions, not claims that the Core findings are closed.

**Other scoped exclusions:** **audit-v2-20261022-20261031:consideration-5** is resolved for its exact
production certificate partial-cleanup scope. `GetCertificatesAsync` already disposes every loaded certificate on later
load/observer failure and transfers successful results to the caller; do not reopen that resolved exact
production bundle cleanup as SD-IMP-008. Integration/helper certificate owners and actual native-backed
test qualification remain open. Current factory-selected catch, metadata logging snippet, plaintext pooled
key finally, parameter scopes/rotation escrow and corrected test commentary are retained strengths, not
new edits. The rejected four-level testing-link claim and superseded draft chain-serial §7.5.4 locator
remain excluded; the correct draft locator is **§7.5.1**. No SD-IMP group was retracted or implemented.
OATH's fourteen IMP resolutions, resolved User/descriptor prose and retracted HSM retry-message subclaim
remain outside this applet's open counts. PIV physical/tool/cleanup qualifications retain their owners.

### Security Domain author verification and stopping point

Coverage: **eight SD-IMP groups, five SD-DEP questions, eight CORE-SD-X groups, fourteen review cards**,
including two qualification-only wire/device cards and one setup/authority qualification card. These
are coverage groups, not fourteen proven bugs. All selected BEFORE windows match current pinned source
after Markdown indentation normalization; paths/symbols/ranges, local links/anchors, fence pairing,
explicit original-ID coverage and allowed edit boundaries were checked statically. Python pin/clean
status matched. At author handoff, changes were limited to this section, its overview row and dispatch
label; the other applet bodies were preserved. No source fix, build, test,
provider/native/device execution, frozen-record edit, commit, push or subagent occurred.

## YubiOTP

### Context and review map

YubiOTP has two programmable slots. Keyboard configurations emit Yubico OTP, HOTP or static passwords;
challenge-response configurations return HMAC-SHA1 or an AES-encrypted Yubico ticket to a host verifier.
These are not OATH applet credentials. SmartCard and OTP HID are session transports, not a promise that
every command works on every interface. The convenience factory prefers SmartCard and does not retry
another transport after an open or initialization failure; USB challenge recipes use explicit OTP HID.

Programming serializes a 52-byte block: fixed/public data (16), UID (6), key (16), new access code (6),
fixed length and three flag bytes (4), reserved bytes (2), complemented little-endian CRC (2). A current
access code authenticates a write separately from the new code embedded at offsets 38–43. Omitting the
new code sends zeros, even when the current code authenticates successfully. Updates transmit complete
masked values, not the history of which fluent setters were called. Configuration read-back cannot
recover the original secrets or all flags; callers must retain their intended settings.

The [published handoff](yubiotp.md), [closeout](modules/yubiotp/pass-1/yubiotp-001/closeout.md) and
[deferred ledger](modules/yubiotp/pass-1/yubiotp-001/findings.md) retain all twelve engineering/verification
groups. The [Core boundary](modules/core/pass-1/core-001/boundary-YubiOtp.md) and its
[candidate definitions](modules/core/pass-1/core-001/boundaries/YubiOtp/candidates.md) add applicability,
not nine new defects. Frozen candidate/inherited reconciliation and continuity were checked for these
IDs; their capture-time pending fields are not implementation outcomes. The Windows successor narrows
old native mechanisms only as recorded under [shared dependencies](#yubiotp-shared-dependencies).

All source windows below were rechecked at the C# baseline in the introduction. Python references are
at **`4ca60f706af930459138d8dc0f0f953480e1c7a4`**, verified clean locally. They establish implementation
precedent only: in particular Python bytes/close/event semantics establish no .NET erasure, native drain
or callback lifetime. Every AFTER remains **proposed, unapplied and uncompiled**. Detailed original
vendor clauses, full test flows and deeper failure context can be loaded on request from the frozen
records; they are not prerequisites for understanding the bounded cards here.

| Review card | Kind | Severity and certainty | No-brainer |
| --- | --- | --- | --- |
| [OTP-IMP-001](#otp-imp-001--encode-the-requested-hotp-counter) | Wire defect | Medium: wrong starting counter; source/vendor discrepancy confirmed | Yes for divisor/range; firmware qualification separate |
| [OTP-IMP-002](#otp-imp-002--separate-scan-map-size-from-password-capacity) | Command contract | Medium: full vendor map rejected; source confirmed, reset/device contract incomplete | No for complete group |
| [OTP-IMP-003](#otp-imp-003--guard-each-secret-owner-at-acquisition) | Exceptional cleanup/lifecycle | Medium: unreturned secret copies survive failure; ordering confirmed | No for group; preflight ordering is mechanical |
| [CORE-OTP-X004](#core-otp-x004--retain-one-shot-output-through-teardown) | Outer result ownership | Medium: successful output stranded on teardown error; source confirmed, no induced native failure | No for exception/transfer policy |
| [OTP-IMP-004](#otp-imp-004--report-touch-with-the-right-certainty) | Notification policy | Low: misleading basis or missed notification; heuristic confirmed, device matrix incomplete | No |
| [OTP-IMP-005](#otp-imp-005--reject-malformed-status-and-decide-sequence-policy) | Response validation | Medium: apparent successful write/cache publication without validation; source confirmed | No for group; reject short fallback is mechanical |
| [OTP-IMP-006](#otp-imp-006--define-the-supported-configuration-matrix) | Firmware/capability policy | Medium: unsupported configurations admitted; gates confirmed, wider matrix unsettled | No |
| [OTP-IMP-007](#otp-imp-007--make-update-and-access-code-intent-explicit) | Provisioning/API policy | Medium: protection/settings can be dropped unintentionally; source confirmed, legacy transitions unqualified | No |
| [OTP-IMP-008](#otp-imp-008--define-ndef-input-and-disable-contracts) | Encoding/API decision | Low: invalid type or unexpected NFC payload; source confirmed, on-tap effects unqualified | No |
| [OTP-IMP-009](#otp-imp-009--translate-standalone-static-password-text) | Sample encoding defect | Medium: wrong password programmed; source and working sibling confirmed | Yes for translation; input/output policy separate |
| [OTP-IMP-010](#otp-imp-010--scope-a-coherent-otptool-contract) | CLI behavior decisions | Low generally; Medium for silent mutation/protection loss | No |
| [OTP-IMP-011](#otp-imp-011--strengthen-oracles-and-independent-cleanup) | Test/resource work | Medium for cleanup/state loss; assertion scopes source confirmed | No for group |
| [OTP-IMP-012](#otp-imp-012--qualify-challenge-semantics-and-real-outputs) | Device/verifier qualification and optional API policy | Qualification, no assigned defect severity; historical one-key results only | No |
| [OTP-DEP-005](#otp-dep-005--dispose-of-relocation-uncertainty-by-surface) | Surface/support decision | Low if legacy feature needed; source inventory resolved, API intent open | No |

### OTP-IMP-001 — Encode the requested HOTP counter

**Context/impact:** the device stores a two-byte initial moving factor (IMF), interpreted in steps of
16. The SDK instead accepts multiples of 65,536 and divides by 65,536. Requesting `0x30000` writes
`00 03` at UID offsets 4–5: the vendor interpretation is counter 48, not 196,608. Existing local
assertions reproduce the encoder and therefore cannot prove the requested counter was provisioned.
Medium reflects failed enrollment/counter synchronization, not a demonstrated authentication bypass.

**BEFORE — `src/YubiOtp/src/HotpSlotConfiguration.cs`, constructor, lines 55–62:**

```csharp
if (imf != 0 && (imf % 0x10000 != 0 || imf < 0))
{
    throw new ArgumentException(
        "Initial moving factor must be 0 or a positive multiple of 0x10000.",
        nameof(imf));
}

SetHmacKey(hmacKey, (ushort)(imf / 0x10000));
```

**Proposed AFTER — literal replacement of that window, unimplemented/uncompiled:**

```csharp
if (imf < 0 || imf > 0xFFFF0 || imf % 16 != 0)
{
    throw new ArgumentException(
        "Initial moving factor must be between 0 and 0xFFFF0, in steps of 16.",
        nameof(imf));
}

SetHmacKey(hmacKey, (ushort)(imf / 16));
```

**Python:** `py:yubikit/yubiotp.py:454–460`, `HotpSlotConfiguration.imf`, checks that range and stores
`struct.pack(">H", imf >> 4)`. Frozen vendor encoder/manual evidence agrees; no wider Python key-size
or HOTP default changes are proposed. **No-brainer: yes** for scaling/range, decision needed for the
legacy preset firmware contract under IMP-006.

**Acceptance/dependencies:** independent literal wire vectors for 0, 16, `0x30000` and `0xFFFF0`, then
authorized keyboard output checked against a host HOTP verifier with the known counter. Demonstrate
wrong-counter failure, not just argument exceptions or configured bits. Keep physical output under
IMP-012. Reopen HOTP/SetHmacKey XML, README task, IMF CLI explanations, related assertions and migration
when the encoder changes; until then retain the zero-only recommendation.

### OTP-IMP-002 — Separate scan-map size from password capacity

The 38-byte static-password capacity is `16 + 6 + 16`; it is not the vendor's 45-entry scan map. The
current command rejects that full map and has no feature gate. Medium reflects an unusable full-map
programming path and potentially unsupported admitted writes; no physical map mutation was performed.
The frozen vendor personalization evidence identifies firmware 3+ and 45 entries, but default reset
and exact device applicability still require an accepted command contract.

**BEFORE — `src/YubiOtp/src/YubiOtpSession.cs`, `SetScanMapAsync`, lines 290–295:**

```csharp
if (scanMap.Length != YubiOtpConstants.ScanCodesSize)
{
    throw new ArgumentException(
        $"Scan map must be exactly {YubiOtpConstants.ScanCodesSize} bytes, got {scanMap.Length}.",
        nameof(scanMap));
}
```

**Proposed AFTER — schematic options, no existing API implied:**

```text
define SCAN_MAP_SIZE independently as the accepted 45-entry command contract
retain static-password ScanCodesSize = 38
apply the accepted scan-map firmware/device gate before I/O
option A: full map only; represent default reset by a separate explicit operation
option B: full map plus a documented reset sentinel, after its wire semantics are verified
serialize the complete map/reset and current authentication code; do not truncate
```

**Python:** `py:yubikit/yubiotp.py:870–877`, `YubiOtpSession.set_scan_map`, forwards the caller's bytes
to `_write_config`; it does **not** validate 45 entries or establish reset behavior. It corroborates
command routing, not the missing size/gate contract. **No-brainer: no** for the complete group.

**Acceptance:** exact full-map command bytes, code append, independent CRC framing and no I/O for
unsupported devices; only after explicit authorization, full-map output and reset/read-back evidence
on a dedicated key. Scan-map changes affect keyboard behavior beyond one slot. IMP-006 owns gates;
IMP-012 owns physical output. Reopen SetScanMap XML/constants, README constraints and migration only
when size/reset/gate behavior or verified support changes.

### OTP-IMP-003 — Guard each secret owner at acquisition

Configurations privately copy inputs; Dispose clears those fields, not source buffers or each GetConfig
array. On normal WriteConfig completion both assembled arrays are cleared. Invalid new-code length
occurs after copying keys into an unreturned array; invalid current-code length occurs after GetConfig
but before the cleanup try. No device I/O on rejection does not erase those copies. Medium reflects
longer secret residence and unavailable caller cleanup, not proof of extraction or a permanent leak.

**BEFORE — `src/YubiOtp/src/SlotConfiguration.cs`, `GetConfig`, lines 154–167:**

```csharp
var config = new byte[YubiOtpConstants.ConfigSize];

_fixed.CopyTo(config.AsSpan(0, YubiOtpConstants.FixedSize));
_uid.CopyTo(config.AsSpan(YubiOtpConstants.FixedSize, YubiOtpConstants.UidSize));
_key.CopyTo(config.AsSpan(YubiOtpConstants.FixedSize + YubiOtpConstants.UidSize, YubiOtpConstants.KeySize));

if (!accCode.IsEmpty)
{
    if (accCode.Length != YubiOtpConstants.AccessCodeSize)
    {
        throw new ArgumentException(
            $"Access code must be exactly {YubiOtpConstants.AccessCodeSize} bytes, got {accCode.Length}.",
            nameof(accCode));
    }
```

**BEFORE — `src/YubiOtp/src/YubiOtpSession.cs`, `WriteConfigAsync`, lines 536–539:**

```csharp
var configBytes = config.GetConfig(accessCode.Span);
var data = BuildPayloadWithAccessCode(configBytes, currentAccessCode);

try
```

The same acquisition-before-protection class remains in standalone ChalResp/Hotp/Static commands.
Unified CLI HOTP's preserved unit **`4089aedab94d4e23`** is the separate caller anchor:
`src/Cli.Commands/src/Otp/OtpCommands.cs:455–469`, `OtpHotpCommand.ExecuteCommandAsync`, generates/prints
or parses the key before access-code parse/try. Unified static translation is correct, but its disposable
configuration at lines 535–546 precedes access-code parse and cleanup. These are existing X005/CLI
facets, not new bugs and not closures delivered by Windows integration.

**Proposed AFTER — schematic for the compound owner/lifecycle scope:**

```text
validate new/current code lengths before copying secret fields
enter a cleanup scope immediately for each acquired array or disposable configuration
guard GetConfig storage before constructing the appended frame
if encoding fails after a copy, clear the untransferred owned array
keep caller sources/configuration alive until the full public operation completes
clear every caller-owned successful calculation result after consumption, including output failure
decide disposed flag-setter admission and response-carrier contract explicitly
```

Preflight length ordering is mechanical; **no-brainer: no** for the group because disposal/API and
exception/diagnostic precedence need decisions. Immutable CLI strings and secret text intentionally
printed to the console remain uneraseable by buffer cleanup. CalculateCommand currently clears its
challenge, not its returned HMAC (`src/YubiOtp/examples/OtpTool/Commands/CalculateCommand.cs:38–62`).
The one-shot teardown gap is separately reviewable under X004 below.

**Python:** `py:yubikit/yubiotp.py:250–259,334–343`, `_build_config`/`SlotConfiguration.get_config`,
build immutable byte strings; `py:ykman/_cli/otp.py:633–675`, `chalresp`, parses/prints secrets and
programs. Useful wire/caller precedent only; no appropriate Python deterministic-erasure counterpart.

**Acceptance/dependencies:** actual retained-array observers for failed encoding and parse/output
failures, config field clearing, successful output clearing and no I/O on rejected preflight. Do not
substitute post-disposal rejection for erasure. Preserve ordinary callback-before-transfer cleanup;
throwing diagnostic cleanup, received reports and APDU/SCP copies stay shared owners, not promises
made by this local change. Reopen ownership/config/session XML, security examples, affected standalone
and unified CLI comments and migration upon allocation, disposal or transfer changes.

### CORE-OTP-X004 — Retain one-shot output through teardown

This is an **OTP-IMP-003 subfacet**, not a thirteenth IMP group. The backend transfers a valid HMAC to
the extension, but the extension's `await using` must dispose its hidden session before its task can
return. If teardown throws, the caller receives no buffer to clear and the extension has no failure
cleanup for that result. Medium reflects a source-confirmed lost sensitive-result owner; ordinary
successful teardown is unaffected and no native disposal failure was induced.

**BEFORE — `src/YubiOtp/src/IYubiKeyExtensions.cs`, one-shot `CalculateHmacSha1Async`, lines 94–98:**

```csharp
await using var session = await yubiKey
    .CreateYubiOtpSessionAsync(options, cancellationToken)
    .ConfigureAwait(false);
return await session.CalculateHmacSha1Async(slot, challenge, cancellationToken)
    .ConfigureAwait(false);
```

**Proposed AFTER — schematic alternatives; decision required before code:**

```text
retain the successful backend result as extension-owned through actual session teardown
option A: teardown failure faults the outer task; clear retained result before propagating
option B: expose output together with a separate teardown diagnostic through an approved new contract
transfer output ownership only at that selected outer success boundary
preserve the original calculation error when calculation and teardown both fail, per accepted policy
never treat an outer cancelled wait as completion or native resource release
```

**Python:** `py:yubikit/yubiotp.py:901–928`, `YubiOtpSession.calculate_hmac_sha1`, returns from an
already-created synchronous session. **N/A** for a comparable hidden async teardown producer boundary;
`close:756–765` is a separate deprecated call. Python cannot settle .NET error precedence.
**No-brainer: no** for policy, though retaining/clearing an untransferred owned result is required.

**Acceptance:** public one-shot fake journey with valid HMAC/CRC plus held and throwing disposal;
observe the actual result storage, outer task outcome and teardown completion. Add simultaneous
calculation/teardown failure and ordinary success controls. Preserve input-finally guidance and
borrowed/direct factory semantics. Reopen extension XML/body, one-shot README task and lifecycle tests
only after the accepted fix. Shared native checked-release work remains a separate dependency.

### OTP-IMP-004 — Report touch with the right certainty

Cached configured/touch bits describe keyboard activation, not challenge-response RequireTouch policy.
The SmartCard notification heuristic emits `PolicyRequires` from those bits, while unknown/non-triggered
slots remain silent. A misleading basis or missing prompt can confuse user interaction, but no security
bypass is shown: Low. HID's actual device-wait indication and terminal response validation are different
and must not be weakened merely to unify transports.

**BEFORE — `src/YubiOtp/src/YubiOtpSession.cs`, `CreateUserPresenceContext`, lines 608–618:**

```csharp
// Cached keyboard-trigger status drives the current SmartCard notification heuristic. It is not
// a read-back of challenge-response RequireTouch policy, despite the emitted PolicyRequires basis.
if (Protocol is not ISmartCardProtocol || !IsKnownTouchTriggered(slot))
{
    return null;
}

return new UserPresenceContext
{
    Basis = UserPresenceBasis.PolicyRequires,
    Application = UserPresenceApplications.YubiOtp,
```

**Proposed AFTER — schematic choices, not an assumed policy read-back API:**

```text
option A: retain silence without trustworthy challenge policy; remove unsupported certainty
option B: notify SmartCard challenges as PolicyMayRequire, with accepted unknown/non-touch behavior
option C: use explicitly supplied known provisioning policy, with defined stale-state invalidation
HID: request once only after device touch-wait; resolve after applet length/CRC validation
configuration/status calls remain silent
```

**Python:** `py:yubikit/yubiotp.py:609–614`, `ConfigState.is_touch_triggered`, explicitly excludes
challenge-response; `HmacSha1SlotConfiguration.require_touch:387–389` sets a separate flag. Its
SmartCard backend has no analogous async prompt-certainty contract. **No-brainer: no**; choose
certainty/cache refresh policy after the status/notification matrix is understood.

**Acceptance:** fake raw state versus effective override, both slots/modes, unknown/non-touch state,
HID touch wait and CRC failure, ordinary callback failure and post-admission cancellation. Only HID
touch-wait expiration resolves TimedOut; readiness/transport and all SmartCard timeouts resolve Failed.
A completed SmartCard command may still resolve Completed after post-admission cancellation; do not
force Cancelled merely because a token is later set. IMP-012 owns live evidence. Reopen ConfigState,
session/backend/prompt XML, README/CLAUDE, shared interaction and migration on policy changes.

### OTP-IMP-005 — Reject malformed status and decide sequence policy

A short SmartCard write triggers a second status exchange, but a still-short fallback returns successfully
through the validator and can replace cached status. The validator also bypasses sequence checks across
effective firmware `5.0.0 <= v < 5.5.0` and accepts zero after any predecessor. SmartCard calculation
accepts excess data and returns a prefix; HID checks CRC on expected data plus two bytes, then returns
a prefix. HID write status has a length check here, not that data CRC check. Medium reflects false
write success, malformed cached state or accepted unexpected response; CRC is not authenticity.

**BEFORE — `src/YubiOtp/src/Backend/SmartCardBackend.cs`, `ValidateProgrammingSequence`, lines 212–230:**

```csharp
if (statusBytes.Length < YubiOtpConstants.StatusBytesLength)
{
    return;
}

var newProgSeq = statusBytes[3];

// The current compatibility bypass covers all 5.0.0 <= version < 5.5.0.
if (_firmwareVersion >= Firmware500
    && _firmwareVersion < Firmware550)
{
    _lastProgSeq = newProgSeq;
    return;
}

// Accept the increment, or zero after any predecessor (not only a 255-to-0 rollover).
var expectedProgSeq = (byte)((_lastProgSeq + 1) & 0xFF);

if (newProgSeq != 0 && newProgSeq != expectedProgSeq)
```

**Proposed AFTER — short-status literal change; rest is decision-dependent:**

```csharp
if (statusBytes.Length < YubiOtpConstants.StatusBytesLength)
{
    throw new BadResponseException("OTP write status is shorter than six bytes.");
}
```

```text
choose the justified sequence exception interval, version provenance and zero/reset conditions
validate before publishing _lastProgSeq and session cached status
choose exact-length SmartCard data or a documented forward-compatible prefix policy
retain HID padded-report prefix CRC scope unless the framing contract changes
on rejection preserve error provenance, callback outcomes and untransferred-output cleanup
```

**Python:** `py:yubikit/yubiotp.py:685–705`, `_YubiOtpSmartCardBackend`, allows an increment, or
conditional zero after a positive predecessor with empty configured bits or status version below
5.4.3; data length must be exact. Python itself indexes short fallback without a defensive length
check and publishes its sequence early. It is narrower precedent, not a complete desired validator.
`_YubiOtpOtpBackend.send_and_receive:665–669` validates only the expected CRC prefix. **No-brainer: yes**
for rejecting a short fallback; **no** for the complete group and compatibility/profile policy.

**Acceptance/dependencies:** short primary/fallback responses, boundary versions 5.0/5.4.3/5.5 and
raw/effective disagreement, zero from zero/positive/255, unchanged/rejected writes, extra/short data
and independent bad-CRC vectors. Assert cache/sequence nonpublication after failure and correct next-call
policy. Shared Core custom-report admission, abort/reset/reuse and whole-workflow scope stay separate.
Reopen backend exceptions/comments, README/CLAUDE and meaningful backend assertions on validator changes.

### OTP-IMP-006 — Define the supported configuration matrix

Type/operation minima are not checks for every flag, password capacity, counter preset or physical
feature. Static password inherits base 2.0 admission despite legacy 2.1/16-character and 2.2/38-character
evidence; HOTP type 2.1 does not enforce the legacy 2.2 nonzero preset floor. Major-zero alpha/beta
admission is optimistic host policy. Unsupported writes may fail late or provision unintended behavior:
Medium, with wider modern-device restrictions still unsettled rather than invented here.

**BEFORE — `src/YubiOtp/src/SlotConfiguration.cs`, minimum/support gate, lines 60–67:**

```csharp
public virtual FirmwareVersion MinimumFirmwareVersion => new(2, 0, 0);

/// <summary>
/// Checks the configuration-type minimum, not all selected flags or length-dependent capabilities.
/// Alpha/beta firmware sentinels are treated as modern firmware (see <see cref="FirmwareVersion.IsAlphaOrBeta" />).
/// </summary>
public bool IsSupportedBy(FirmwareVersion version) =>
    version.IsAlphaOrBeta || version.IsAtLeast(MinimumFirmwareVersion);
```

**Proposed AFTER — schematic options:**

```text
define an edition-qualified operation/type/flag/length/preset/transport support table
option A: strict checks for all supported configurations before encoding/I/O
option B: explicit best-effort/expert path for unqualified development firmware, separate from support claims
retain raw status, detected applet version, metadata version and effective override as separate facts
do not infer NFC hardware, challenge parity or OTP FIPS mode from a generic firmware/SCP gate
```

**Python:** `py:yubikit/yubiotp.py:384–385,433–434,478–479,564–565`, configuration support methods,
use coarse 2.2 floors; `YubiOtpSession.__init__:723–746` probes Management only on NFC, unlike this
SDK's USB/NFC probe. Python's differing floors and normalization are evidence to reconcile, not
authority to bulk-copy a matrix. **No-brainer: no**; settle exact editions, development policy and gates.

**Acceptance:** meaningful wire/device behavior around each adopted boundary, selected flag aliases,
static lengths and corrected IMF; prove rejected requests perform no I/O and accepted requests encode
the intended flags. Keep HID first-report firmware, second-report cached status, NEO refresh and
post-discovery override distinct. Shared Feature/FirmwareVersion and Core polling policy remain canonical
dependencies. Reopen requirement/config/status XML, initialization guide, README/CLAUDE, firmware tests
and migration on matrix/default/provenance changes. IMP-004 owns prompt certainty; IMP-005 sequence rules.

### OTP-IMP-007 — Make update and access-code intent explicit

The SDK exposes both code roles correctly, but omission is not retention. Fresh updates enable base
SerialApiVisible/AllowUpdate and clear other masked settings; they do not preserve unspecified flags.
Standalone and unified programming callers often supply only current authentication, removing protection
where a new zero code is accepted. Legacy 4.3.2–4.3.5 code-change restrictions are not enforced here.
Medium reflects accidental protection/settings loss; deliberate full-mask replacement is not itself a bug.

**BEFORE — `src/YubiOtp/src/UpdateConfiguration.cs`, effective masks, lines 145–152:**

```csharp
protected override ExtendedFlag GetEffectiveExtFlags() =>
    _extFlags & ExtendedFlagMasks.UpdateMask;

protected override TicketFlag GetEffectiveTktFlags() =>
    _tktFlags & TicketFlagMasks.UpdateMask;

protected override ConfigFlag GetEffectiveCfgFlags() =>
    _cfgFlags & ConfigFlagMasks.UpdateMask;
```

**BEFORE — `src/YubiOtp/examples/OtpTool/Commands/ChalRespCommand.cs`, `RunAsync`, lines 79–81:**

```csharp
// Current code authenticates only. No new code is supplied, so this does not retain write
// protection. Production callers retain it by supplying accessCode in both code arguments.
await session.PutConfigurationAsync(slot, config, currentAccessCode: accessCode, cancellationToken: ct);
```

**Proposed AFTER — schematic alternatives, not invented retention/patch APIs:**

```text
option A: keep explicit full-mask replacement; require caller-owned complete intended settings
option B: introduce a separately designed intent-bearing API; do not pretend unreadable flags can be fetched
choose distinct retain/replace/remove access-code intent; omitted authentication is not that intent
for CLI retention, supply the known current code as both new and current code when selected
enforce the accepted 4.3.2 <= version < 4.3.6 transition restriction, including remove versus retain
reject unsupported configuration/update combinations and legacy flag transitions before I/O
```

**Python:** `py:yubikit/yubiotp.py:262–271,556–578`, update builders/defaults differ from .NET;
`update_configuration:834–848` rejects differing code values in that legacy interval.
`py:ykman/_cli/otp.py:670–675`, `chalresp`, supplies the same code in both roles. This is retention
precedent, not evidence that every device permits every transition. **No-brainer: no** for API/default
policy; explicit code retention at a caller that intends retention is mechanical after that intent is chosen.

**Acceptance:** complete literal update-mask vectors, unchanged/cleared settings according to accepted
intent, current/new/remove code transitions, boundary firmware and failures with no false status
publication. An authorized access-code transition probe remains unexecuted; never delete/reprogram an
unknown protected slot to prove a policy. Reopen put/update/base XML, README protection/settings tasks,
CLI provisioning/settings guidance and migration when defaults, masks, type checks or protection change.

### OTP-IMP-008 — Define NDEF input and disable contracts

The 56-byte block configures an existing compatible slot; it is not a complete arbitrary NFC record.
URI compression leaves 53 UTF-8 bytes, text's `02 en` header leaves 51. Null currently sends all zeros,
distinct from empty URI/text. Undefined enum values take the text branch but keep their invalid type byte.
UTF-8 replacement/control/URI policy and additional language/UTF-16/read-back/generation surfaces remain
decisions. Low reflects malformed or unexpected output; physical on-tap disable/restoration is unqualified.

**BEFORE — `src/YubiOtp/src/YubiOtpSession.cs`, `BuildNdefPayload`, lines 447–456:**

```csharp
payload[1] = (byte)ndefType;

if (ndefType == NdefType.Uri)
{
    BuildNdefUri(content, payload);
}
else
{
    BuildNdefText(content, payload);
}
```

**Proposed AFTER — schematic options:**

```text
reject undefined type values before constructing a typed payload
option A: preserve null = all-zero disable and empty = typed empty content
option B: design an explicit disable operation, preserving an unambiguous migration contract
decide strict Unicode/control/URI validation versus transparent well-defined UTF-8 encoding
retain longest ordinal case-sensitive URI prefix and byte budgets unless deliberately changed
separately scope language/UTF-16/read-back/generation APIs; do not claim them implemented
```

**Python:** `py:yubikit/yubiotp.py:274–292`, `_build_ndef_config`, maps null URI to a default URI and
null text to empty text; it uses a first matching prefix. That **differs** from this SDK's disable/longest
prefix behavior and also uses an else text branch. Do not import its null semantics silently.
**No-brainer: no** for the group; enum rejection is straightforward once null/type admission is specified.

**Acceptance:** literal URI/text/null/empty vectors, UTF-8 multibyte budgets, longest-prefix/case controls,
selected malformed-Unicode/type outcomes and code-appending errors. NFC read-back/on-tap evidence must
record slot configuration and whether legacy URI output appends OTP; USB write success is not that
evidence. IMP-006 owns NFC capability gates; IMP-012 physical qualification. Reopen NdefType/session XML,
README/CLAUDE, NdefCommand and migration when validation, disable, read-back or generation changes.

### OTP-IMP-009 — Translate standalone static-password text

OtpTool converts text to UTF-8 then passes those bytes to the raw HID scan-code constructor. For example,
`a` becomes `0x61`, not HID `0x04`. The parsed layout is ignored. Medium reflects programming a password
different from the one the user enrolled. The public string/layout constructor and unified CLI helper
already translate correctly; this is not a Core translator or all-CLI defect.

**BEFORE — `src/YubiOtp/examples/OtpTool/Commands/StaticCommand.cs`, `RunAsync`, lines 56–59:**

```csharp
string password = options.Password
                  ?? throw new ArgumentException(
                      "--password or --generate is required for static command.");
scanCodes = Encoding.UTF8.GetBytes(password);
```

**Proposed AFTER — schematic complete handoff, using an existing constructor, not a new helper API:**

```text
parse and validate the chosen KeyboardLayout before secret acquisition
explicitly choose immutable-string convenience or mutable translated-source policy
for the string option: use StaticPasswordSlotConfiguration(password, chosenLayout)
for generation: generate only chosen-layout-supported characters, then use the same translation route
protect configuration/source/code owners immediately; use selected protection intent from IMP-007
do not program any slot if translation fails
```

**Python:** `py:ykman/_cli/otp.py:555–588`, `static`, calls `encode(password, keyboard_layout)` before
the raw constructor and supplies the same code in both roles. The .NET working implementation is
`src/YubiOtp/src/StaticPasswordSlotConfiguration.cs:149–178`, `ToScanCodes`, validates every character
before allocating, then indexes the translator. **No-brainer: yes** for using that translation route;
mutable input/printed-secret policy and supported host layouts still require decisions.

**Acceptance:** public command-to-wire literal vectors for lowercase/uppercase and at least two supported
layouts, generated characters, unsupported characters and parse failures; retain the unified CLI N3
encoding resolution, do not relabel it unfixed. Physical output/dead-key/host-layout qualification is
separate and must use an authorized editor/input capture, never a shell. Core B4 C010 owns mapping/locale
and generator-description issues. Reopen sample limitation/layout comments/options, README static task
and migration on caller translation changes; IMP-003 owns exceptional cleanup.

### OTP-IMP-010 — Scope a coherent OtpTool contract

This group contains several independent intent questions, not one compatibility defect. Calculate help
suggests stdin but execution requires a hex argument; its USB calculation uses SmartCard-first defaults.
ChalResp `--totp` only toggles touch/default short mode, not base32 enrollment/time/truncation. Swap
ignores the parsed access code. JSON bypasses confirmation without Force and suppresses generated-secret
output that human mode prints. Parser exits and top-level cancellation/error/shutdown outcomes also need
an explicit contract. Low generally; Medium where noninteractive mutation or code omission changes protection.

**BEFORE — `src/YubiOtp/examples/OtpTool/Commands/CalculateCommand.cs`, `RunAsync`, lines 31–38:**

```csharp
byte[] challenge = OutputHelper.RequireHex(
    options.Challenge, "CHALLENGE (positional argument after slot)");

try
{
    await using var session = await DeviceHelper.CreateSessionAsync(ct);

    var response = await session.CalculateHmacSha1Async(slot, challenge, ct);
```

**BEFORE — `src/YubiOtp/examples/OtpTool/Commands/SwapCommand.cs`, `RunAsync`, lines 30–32:**

```csharp
if (!options.Force && !options.Json)
{
    if (!AnsiConsole.Confirm("Swap slot 1 and slot 2 configurations?", defaultValue: false))
```

**Proposed AFTER — schematic scoped choices:**

```text
choose either a deliberately small demonstrator or specified ykman-like command behavior
make challenge input/help agree: required hex OR explicit bounded stdin/prompt contract
select OTP HID for supported USB calculations; do not silently retry a failed selected open
implement specified TOTP/base32/time/truncation semantics OR rename/remove the misleading flag
reject irrelevant swap-code options OR define a supported command contract; do not invent wire code fields
separate JSON formatting from force/confirmation and enrollment-secret delivery policy
replace parser process exits with selected return/exit handling and define cancellation/error status
await actual operation/teardown completion before shutdown; guard parser/output owners under IMP-003
```

**Python:** `py:ykman/_cli/otp.py:619–675,699–746`, `chalresp`/`calculate`, distinguishes base32
provisioning from time challenge/code formatting, prompts for missing challenge and selects OTP USB versus
SmartCard NFC. It does not establish this tool's advertised stdin or JSON semantics. Standalone Program
currently returns zero on cancellation (`src/YubiOtp/examples/OtpTool/Program.cs:39–42`); this is observed
behavior, not an approved success-exit policy. **No-brainer: no**; select command scope before implementing.

**Acceptance:** deterministic command/input/output scenarios for each accepted choice, JSON without
Force, secret delivery/enrollment, rejected irrelevant options, cancellation and teardown failure.
Verify actual request/side effects and exit/output streams rather than only parser validation. Code
retention/settings stay IMP-007, static translation IMP-009, cleanup IMP-003, device capability IMP-012.
Reopen OtpTool README/help/options/affected comments and migration when parser, command, transport,
confirmation/output or shutdown changes. Do not claim a compiled sample was executed.

### OTP-IMP-011 — Strengthen oracles and independent cleanup

Configured-bit and length assertions do not prove keyboard text, HOTP digits/counter, AES ticket validity,
NFC output or hardware compatibility. CRC calculated by the same SDK helper is not an independent oracle.
Some tests program before cleanup try, swap both slots, or can omit disposal after an intervening assertion.
Protection cleanup is sequential: slot-two deletion failure prevents slot-one cleanup. Medium reflects
lost test-key state/resource lifetime and false confidence, not evidence that every existing test failed.

**BEFORE — `src/YubiOtp/tests/Yubico.YubiKit.YubiOtp.IntegrationTests/AuditV2/YubiOtpProtectSlot2Tests.cs`,
`YESDK1634_ProtectSlot2OnSlot1_BlocksSlot2Write`, lines 116–120:**

```csharp
finally
{
    await DeleteIfConfiguredAsync(session, Slot.Two);
    await DeleteIfConfiguredAsync(session, Slot.One);
}
```

**Proposed AFTER — schematic failure-preserving test policy:**

```text
authorize the full fixture's both-slot effects before execution
put each acquired owner/programming effect inside an immediate cleanup scope
attempt slot-one cleanup independently even if slot-two cleanup fails
record primary assertion/operation failure plus every cleanup outcome without masking the original
release held gates, join actual late operations and dispose any late-created session even on assertion failure
use independent CRC/HOTP/HMAC/AES oracles; keep wire, native and physical claims separate
```

**Python:** `py:yubikit/yubiotp.py:250–259,665–669` supplies wire/CRC usage but is not an independent
oracle merely by sharing a helper or algorithm. **N/A** for comparable .NET held-native fixture/drain
repair. No Python test suite was audited or run. **No-brainer: no** for group; independent cleanup scopes
are straightforward after primary-versus-cleanup reporting and authorized restoration policy are chosen.

**Acceptance:** deliberate assertion failure at held creation and between cleanup calls, observe actual
late-result disposal and retained arrays, preserve original failures, verify both cleanup attempts.
Do not create skipped placeholders or merely assert argument validation. Associate corrected IMF and
sequence assertions with IMP-001/005 rather than count another defect. Historical empty-slot cleanup
does not restore unreadable original secrets, NDEF/global settings or physical release. Reopen named
test explanations/fixtures and test/module guides when assertions, setup, cleanup or evidence changes.

### OTP-IMP-012 — Qualify challenge semantics and real outputs

**Qualification, no assigned defect severity.** HMAC host padding, transmitted bytes and the device's
hashed message are three different layers. Current short inputs use a pad byte different from the last
data byte; full 64-byte inputs are unchanged. In short mode the final marker can remove a trailing run,
with historical one-key evidence for a minimum one-byte message. Empty input therefore yielded HMAC
of one zero byte there, not empty HMAC. Fixed mode hashes all 64 transmitted bytes. The session does not
know the stored slot mode, and a short input and its explicit 64-byte padded form produce identical
requests. Do not "fix" this into an assumed generic empty/64-byte HMAC contract.

AES challenge-response returns a 16-byte variable ticket from a six-byte challenge, not deterministic
AES(challenge). The verifier needs known key/UID, decoded/decrypted ticket fields, independent CRC and
appropriate counter/replay validation. Response length or CRC alone does not prove authentication.

**BEFORE/AFTER code: N/A — device/verifier qualification has no appropriate runtime replacement.**
The existing source is context, not a requested fix: `YubiOtpSession.PadHmacChallenge`,
`src/YubiOtp/src/YubiOtpSession.cs:419–431`, and `CalculateYubicoOtpAsync:380–410`.
If maintainers later choose a mode-aware API, it needs an explicit slot-mode provenance/invalidations
design; no `GET_MODE` or equivalent API is assumed to exist. **No-brainer: no.**

**Python:** `py:yubikit/yubiotp.py:901–928`, `YubiOtpSession.calculate_hmac_sha1`, uses the same
last-byte-different padding and does not read slot mode. `py:ykman/_cli/otp.py:706–711`, `calculate`,
selects OTP USB/SmartCard NFC. Neither proves the device's marker minimum across firmware or .NET AES
verifier coverage. Frozen vendor/host-vector evidence remains edition-qualified.

**Proposed qualification procedure/acceptance, unexecuted here:**

1. Obtain explicit authorization for a dedicated key and whole fixture, including slot-one/protection,
   swap, scan-map and NFC effects if selected. Record key, actual applet/raw/effective/metadata firmware,
   OS, connection, mode/settings and preflight state. Never infer these from a skip or version override.
2. Execute host-known HMAC vectors for 1–63 bytes, both trailing pad choices and repeated input; separately
   test empty/63/64 boundaries and fixed 64-byte mode. Record transmitted payload and expected hashed
   message convention. Keep original failure/cancellation/touch/callback outcomes, not only successes.
3. For AES, independently decrypt the ticket, verify UID/challenge-related fields, CRC and counter/replay
   policy. Qualify USB HID, USB CCID and NFC separately; measure rejection as rejection, not success.
4. Use an authorized editor/input-capture lane for HOTP counter/digit and static layout output; inspect
   actual NFC URI/text/on-tap disable/read-back when authorized. Do not send keyboard output into a shell.
5. Independently verify cleanup and accessible metadata using a fresh session/tool, and distinguish new-
   session recovery from same-session reuse, actual drain/release from cancellation of a managed wait,
   and empty slots from complete restoration. Report unsupported/unexecuted dimensions explicitly.

Historical serial **103**, Linux USB HID/CCID, metadata **5.8.0.beta.0**, raw/effective OTP **0.0.1**:
all sixteen existing integration methods plus four focused probes passed at the recorded source scopes,
including twelve HMAC vectors and scoped HID AES/touch/cancel/natural timeout. USB CCID HMAC returned
**6985**, a classified rejection. Initial reader contention executed **zero** methods before a successful
retry; preserve that failure. Final slots were empty, not all device state restored. NFC read-back,
older firmware, keyboard IMF/output, access-code transitions and cross-platform hardware remain open.
Current Windows/Linux cross-layer managed seams are not hardware evidence. Serial **9681620**'s historical
two-key failure remains unexplained; unrelated protected PIV physical lanes remain untouched.

Reopen both calculation APIs/configurations, README/verifier enrollment examples, migration and KAT
commentary only when actual scope-bound evidence, padding/mode/capability or verifier contract changes.

### OTP-DEP-005 — Dispose of relocation uncertainty by surface

The original relocation **investigation is source-resolved**, not an accepted implementation backlog.
Management/Core supplies device-info reading through OTP; modern Management TLV configuration is not
the old four-byte mode/timeout/eject mutator. Current public snapshots still contain no typed
`QueryFipsMode`, `SetLegacyDeviceConfig` or `SetMode` API. Typed OTP FIPS-mode query and legacy mutation
are product support decisions, not proven missing Core internals. Low if a user needs that migration
surface; no assigned security defect follows from metadata being different from an OTP mode query.

**BEFORE — `docs/migration/v1-to-v2-gaps.md`, YubiOTP outstanding-verification entry, line 419:**

```markdown
1. **YubiOtp FIPS-mode query and legacy device-info/config commands** — unverified whether these relocated to `Management` or were dropped entirely.
```

**Proposed AFTER — literal bounded inventory wording, not an API implementation:**

```markdown
1. **YubiOTP legacy/FIPS surfaces** — device-info reading is available through Management/Core. No typed OTP FIPS-mode query or legacy mode/configuration mutator exists at the inspected v2 baseline. Modern Management TLV writes and raw expert commands are separate surfaces; legacy-device parity remains unqualified.
```

The frozen boundary inventories owning interfaces/snapshots; the three absent names were rechecked in
current public snapshots. This card's AFTER is unimplemented documentation too: no authority to update
the shared migration file follows from drafting it. **No-brainer: no** for future feature support;
accurate inventory wording is settled and should not remain "unverified relocation".

**Python:** `py:yubikit/management.py:695–753`, `ManagementSession.set_mode`, translates to modern
DeviceConfig for 5+ and otherwise sends `struct.pack("<BBH", ...)`. This is an actual legacy precedent,
not equivalent to the .NET TLV writer or evidence of an OTP FIPS-mode query. No FIPS query API is invented.

**Acceptance/dependencies:** choose leave-absent/expert-only versus a separately specified typed legacy
surface with its actual wire/firmware/transport contract. Management owns device-info/write enforcement
and legacy mutation policy; Core metadata FipsCapable/FipsApproved mapping stays its own owner. Reopen
active migration map/gaps/guide and relevant public XML only on accepted inventory/support changes;
historical point-in-time surveys remain unchanged. Keep NDEF generation/read-back and scan-map gaps local.

### YubiOTP shared dependencies

These are **narrow references to canonical `core-001` owners**, not additional YubiOTP bug cards. Original
IDs/facets and reopening conditions remain in the frozen
[candidate reconciliation](modules/core/pass-1/core-001/triage/final/candidate-reconciliation.json),
[inherited reconciliation](modules/core/pass-1/core-001/triage/final/inherited-reconciliation.json) and
[continuity](modules/core/pass-1/core-001/triage/final/reconciliation-4/deferred-finding-continuity.json).
The [Windows successor](integrations/windows-upstream-20261113/deferred-findings-successor.md) controls
later mechanism supersession; no broader Core/native audit was performed for this section.

| Canonical owner/dependency | Actual YubiOTP applicability and remaining acceptance |
| --- | --- |
| `CORE-B1-C005/C008`, `CORE-B5-C004/C006` — connection and workflow lifetime | Hidden factory owns its connection; direct factory borrows it; backends borrow protocol with no disposal hook. Grouping is process-local/overlapping-known-members, not all-process physical isolation. SmartCard config→status can span two exchange guards; sequence/cache/CRC/terminal callbacks outlive exchange drain. Maintain the same shared workflow owner already used by earlier applets. Any whole-operation policy must define overlap, callback/self-disposal and late-result lifetime before changing local callers. |
| `CORE-B5-C001/C007/C008` — OTP HID initialization, polling, abort/reuse and harness | First report initializes firmware/NEO refresh; second supplies raw cached status; override is later. Custom providers promise eight bytes but lack central defensive admission. NEO broad catch can swallow non-rejection failure after abort; local readiness/touch thresholds are checked between native calls, not hard native duration bounds. Classify first/second malformed reports, NEO rejection versus cancellation/transport/reset failure, retained failed-reset refusal and actual drain/reuse. Applet status/sequence/CRC remains IMP-005. |
| `CORE-B5-C005`, `CORE-B6-C002`, `CORE-C-C008` — independent response copies and diagnostic cleanup | HID incoming report arrays remain separate from accumulator/assembled prefix; SmartCard raw/APDU/fragment/decrypted owners are separate from returned Data prefix. Decide transfer/borrowed/clearable receive contracts before clearing custom-provider storage; observe actual owners. Ordinary backend terminal callback failure clears untransferred output, but a throwing logging provider can escape before subsequent zeroing. Preserve primary error/outcome and enforce chosen cleanup precedence; no blanket all-copy erasure claim. X004 owns only the later one-shot producer boundary. |
| `CORE-B4-C001/C007`, `CORE-B1-C005`, `CORE-B4-C008` — Windows successor/native qualification | Old built-in synchronous Windows/early registry-release and report-ID staging mechanisms are superseded. New rooted OTP owner drains feature calls before checked close and clears native staging after last use; source/managed buffer prerequisites passed in the integration's software scope. Independent protocol arrays remain B5 C005. Custom compatibility disposal failure, HID-driver races/unknown release, permission/topology, installed/loaded producer, JIT/AOT/architecture and broader harness qualification remain open. HidD feature API supplies no actual transfer count. Do not cite removed WindowsHidReportAccess as current source. |
| `CORE-B5-C007`, Core `Feature`/`FirmwareVersion` policy; `CORE-OTP-X006` applicability | Major-zero acceptance/override are host policies, not a universal feature certification. Retain first/second-report, Management/OTP and raw/effective distinctions. IMP-006 owns applet support and IMP-004 touch certainty; sibling SCP floors or metadata do not resolve them. |
| `CORE-B4-C010`, `CORE-C-C001–C004`, historical N3 | Core keyboard mapping/locale and credential-reader behavior remain their owners. Neither static caller uses ConsoleCredentialReader; encoded credential bytes are not scan-code translation. Unified static translation/redirected-reader historical fixes remain resolved; only standalone translation is IMP-009. Physical host/dead-key/output and immutable input/secret display policy remain qualified. |
| Management device-info/legacy policy; `CORE-A-C004` metadata mapping | Device-info relocation is resolved inventory under DEP-005/X008. FIPS-capability metadata is not OTP FIPS-mode state, and modern TLVs are not legacy mutation. Refer complete device-info/read/write enforcement to Management, do not duplicate its IMP groups. |

Dependency closure requires the canonical owner's accepted contract, meaningful failure/held-operation
and retained-owner oracles, independent review and the appropriate native/physical lane where needed.
Until then callers serialize and await complete public tasks, keep borrowed memory/configuration/prompt
services alive through actual completion and dispose session before caller-owned connection. A canceled
outer wait, successful fake-native cross-layer test or empty-slot cleanup is not resource release proof.

### YubiOTP complete coverage ledger and exclusions

Every original ID is covered below; repeated occurrences are aliases, not new defects. Cards inherit
the introduction's unreviewed/deferred state. Documentation reopening is conditional on the actual
behavior, policy or evidence change named in each card and original ledger, not permission for a broad rewrite.

| Original ID | Canonical disposition/card and retained facets |
| --- | --- |
| OTP-IMP-001 | IMP-001: corrected IMF range/scaling, independent wire/counter oracle; firmware under IMP-006, physical under IMP-012 |
| OTP-IMP-002 | IMP-002: 45-entry versus 38-byte object, reset/gate/device contract and wire/output evidence |
| OTP-IMP-003 | IMP-003 plus X004: preflight arrays, config lifecycle, caller/CLI parser/output cleanup, distinct outer teardown result; shared reports/APDU/logger owners remain dependencies |
| OTP-IMP-004 | IMP-004: keyboard-trigger versus challenge policy, stale/raw state and notification certainty; live matrix under IMP-012 |
| OTP-IMP-005 | IMP-005: short fallback, bypass interval/version source/zero rules, prefix/exact-length and CRC scope; Core framing not duplicated |
| OTP-IMP-006 | IMP-006: operation/type/flag/length/IMF/development matrix, NEO/raw/effective policy and physical support limits |
| OTP-IMP-007 | IMP-007: full masks/defaults, code retention/remove/change and 4.3.2–4.3.5 restrictions/type misuse; no assumed patch read-back |
| OTP-IMP-008 | IMP-008: undefined type/Unicode/control/URI policy, null/empty/disable, language/UTF-16/read-back/generation choices and NFC evidence |
| OTP-IMP-009 | IMP-009: standalone UTF-8/raw handoff and ignored layout only; correct public/unified translations excluded |
| OTP-IMP-010 | IMP-010: stdin/help, TOTP, swap-code, transport, JSON/confirmation/enrollment output, parser/shutdown; cleanup IMP-003 and protection IMP-007 |
| OTP-IMP-011 | IMP-011: weak or self-referential oracles, before-try programming, assertion-failure late owners, both-slot/independent cleanup and restoration limits |
| OTP-IMP-012 | IMP-012: real command/interface support, marker minimum/length/mode, AES verifier/ticket, optional mode-aware API and bounded physical outputs |
| OTP-DEP-001 | X001/shared B1/B5: selection/no fallback, hidden versus borrowed lifetime and failed initialization source trace complete; grouped physical/checked release/workflow qualification remains open |
| OTP-DEP-002 | X002/X003/X009/shared B5: size/framing/CRC/sequence/touch/reset/drain/reuse source trace complete; custom reports, received copies, polling/failure and physical qualification remain open |
| OTP-DEP-003 | X001/X003/X004/X005: admitted SmartCard cancellation and resolution-before-transfer traced; source/serialized/raw/result owners, logger/teardown failure and whole workflow remain open |
| OTP-DEP-004 | X002/X006 and IMP-004/006: firmware first/second report, Management/OTP and post-discovery override source-resolved; development/prompt/capability intent and device support remain open |
| OTP-DEP-005 | DEP-005/X008: relocation inventory resolved (device-info present, typed FIPS/legacy mutator absent); future typed support and legacy parity still decisions |
| CORE-OTP-X001 | Shared connection/workflow row; no applet-wide lease or backend disposal invented |
| CORE-OTP-X002 | Shared HID initialization row plus IMP-005/006/012; custom admission/NEO/polling/reset classifications retained |
| CORE-OTP-X003 | Shared receive/APDU/logging row plus IMP-003; old Windows staging superseded only, independent report/prefix owners remain open |
| CORE-OTP-X004 | Dedicated X004 card, subordinate to IMP-003, outer one-shot output/teardown exception policy |
| CORE-OTP-X005 | IMP-003/010, unified CLI HOTP anchor `4089aedab94d4e23` and static config-before-parse lifetime; correct encoding not reopened |
| CORE-OTP-X006 | IMP-004/005/006/012: raw/effective/capability/notification/sequence and historical fixture limits |
| CORE-OTP-X007 | IMP-009/010/012 plus Core keyboard/credential dependencies; standalone versus working public/unified paths explicit |
| CORE-OTP-X008 | DEP-005: resolved named-surface inventory; Management owns read/write/legacy policy, Core owns metadata mapping |
| CORE-OTP-X009 | IMP-011/012 plus shared harness: retained-array/callback observers, independent CRC, historical KAT/physical and failure cleanup scopes |

**Explicit exclusions/partial resolutions:** Pass-1 OTP-C001–018 documentation acceptance is not runtime
closure; all twelve IMP groups remain. Correct HMAC padding explanations, public string translation,
unified CLI N3 encoding/redirected-reader fix, ordinary callback-failure zeroing and backend nonownership
are not reopened as bugs. The reviewer request to rewrite already-correct merged-flag source wording
was withdrawn, not deferred engineering. DEP-005's relocation uncertainty is resolved by source inventory;
absence/support and physical parity are separate remaining questions. Windows B4 C001/C007 supersession
does not close B5 C005 or native qualification. OATH's fourteen IMP resolutions, resolved User/descriptor
prose and the retracted HSM retry-message mismatch remain excluded. No PIV tool/framing/cleanup/physical
or OpenPGP/HSM/profile/Rust/producer finding is imported or closed by this YubiOTP section.

### YubiOTP author verification

Coverage: **12 OTP-IMP groups, five OTP-DEP questions, nine CORE-OTP-X groups, 14 review cards**,
including one qualification-only challenge/device card and one named-surface support decision. Counts
are coverage groups, not fourteen proven bugs. Static verification checked selected BEFORE windows
against the current pinned bytes, source paths/symbols/ranges, local links/anchors, fence pairing,
explicit original-ID coverage and edit boundaries. Python pin/clean status matched. Earlier applet
bodies were preserved at author handoff; changes were limited to YubiOTP, its overview row and dispatch
label. No source fix, compilation/build, SDK test, hardware/native
or crypto execution, frozen-record edit, commit, push or subagent occurred. Review and implementation
remain unstarted for this section.

## YubiHSM Auth

### Context, evidence and review map

YubiHSM Auth is the applet on a **YubiKey**, not a connector client for the YubiHSM 2. It stores
authentication credentials and calculates S-ENC, S-MAC and S-RMAC. The application enrolls matching
HSM objects, supplies the actual HSM challenge and trusted HSM key, verifies peer material, completes
host authentication and manages protected commands. A successful CALCULATE or inherited
`IsAuthenticated` flag does not establish that external handshake.

This section covers the eight original engineering groups and the three inherited Core questions from
the [published handoff](yubihsm.md) and [frozen findings](modules/yubihsm/pass-1/yubihsm-001/findings.md).
The [dedicated Core boundary](modules/core/pass-1/core-001/boundary-YubiHsm.md), its
[candidate definitions](modules/core/pass-1/core-001/boundaries/YubiHsm/candidates.md) and
[inherited dispositions](modules/core/pass-1/core-001/boundaries/YubiHsm/inherited-dispositions.json)
preserve seven nonadditive boundary groups. The master-wide reconciliation and Windows successor
references above remain authoritative for their individual facets. Core's accepted prose corrections
are present on the current baseline; they do not close the deferred runtime or device questions.

All BEFORE windows below were inspected at C# **`6ce355468047e8004fd51cf1425808f6e8242ed3`**.
Paths are repository-relative. All AFTER blocks are **proposed, unapplied and uncompiled**;
`text` means a schematic choice, not an existing API. The single literal cleanup sketch uses existing
operations but has not been compiled. Review remains unstarted and no fixes are authorized.

| Review group | Necessary decision or bounded action |
| --- | --- |
| HSM-IMP-001 / 003 / 007 | Retry translation, malformed versus extensible LIST data, profile validation actors |
| HSM-IMP-002 / 004 / 005 / 006 | Failure-safe secret scopes, honest tool dispatch, diagnostic exposure, stronger test oracles/cleanup |
| HSM-IMP-008 / CORE-HSM-X005 | Identify actual 56-byte regions and select response/result contract before implementing |
| CORE-HSM-X001 / X003 / X004 | Reset terminal policy, complete workflow lifetime, effective-version provenance |
| CORE-HSM-X002 | Use supported final-owner clearing; route independent lower copies to existing Core owners |
| Qualification card | Recorded-device format, genuine HSM handshake, secure reset and physical/native/platform lanes |

#### Inspected Python precedent register

Local repository **`/home/rupicola/Code/y/yubikey-manager`** is clean at
**`4ca60f706af930459138d8dc0f0f953480e1c7a4`**. Only relevant HSM source and narrowly related version/CLI
windows were inspected. Every `py:` reference in this section uses that pin. Python code is vendor
implementation precedent, not a normative applet profile, .NET erasure design or runtime qualification.
Imported `cryptography` internals and external HSM libraries were not audited here.

| Python path and actual symbol | Observed precedent and limit |
| --- | --- |
| `py:yubikit/hsmauth.py:695–718`, `HsmAuthSession.get_challenge` | Sends password at its gate, returns ordinary APDU result/errors without typed retry translation |
| `py:yubikit/hsmauth.py:250–263`, `list_credentials` | Strict label decoding and enum construction; any nonzero touch byte becomes true, unlike C#'s unknown/null carrier |
| `py:yubikit/hsmauth.py:203–220,596–653`, `SessionKeys.parse`, `_calculate_session_keys`, `calculate_session_keys_symmetric` | Sends nonempty cryptogram and slices three keys; neither validates total length nor identifies the suffix |
| `py:yubikit/hsmauth.py:376–409,440–452,655–693`, asymmetric import/public-key/calculation methods | Typed P-256 key inputs, curve checks and encoded-point conversion; not proof that every HSM profile input is validated |
| `py:yubikit/hsmauth.py:157–172`, `_password_to_key` | SHA256, salt `Yubico`, 10,000 iterations, 32-byte split; corroborates constants, not an executed independent KAT |
| `py:yubikit/hsmauth.py:226–248`, constructor/reset; `py:yubikit/core/__init__.py:246–247`, `_OverrideVersion.patch` | SELECT then patched policy/SCP; reset sends RESET without C#'s re-SELECT. Exact `0.0.1` patch is not C#'s general major-zero policy |
| `py:ykman/_cli/hsmauth.py:165–194`, `_prompt_management_key`, `_prompt_credential_password`, `_prompt_symmetric_key` | Password prompts hide input; symmetric key prompt remains visible. Python strings/bytes provide no C# cleanup guarantee |

### HSM-IMP-001 — GET_CHALLENGE retry contract

**Medium; source-confirmed inconsistency, intended contract unsettled. No-brainer: no.**
GET_CHALLENGE can transmit a credential password but bypasses the typed `0x63Cx` retry translator used
by calculation and password changes. Callers using `RetriesRemaining` must instead inspect a general
APDU exception. This impedes consistent retry handling; it is not an SDK retry loop or evidence of
unobserved counter exhaustion. Credential exhaustion deletes that credential; management exhaustion
blocks management until destructive reset. Keep those consequences distinct.

**BEFORE — `src/YubiHsm/src/HsmAuthSession.cs:612–617`, `GetChallengeAsync`:**

```csharp
            var command = new ApduCommand { Ins = InsGetChallenge, Data = data };
            var response = await _backend.SendAsync(
                    command, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return response.Data;
```

**AFTER — decision-dependent alternatives:**

```text
A. Retain ordinary APDU errors; explicitly keep GET_CHALLENGE outside typed retry policy.
B. After command-specific status evidence, send with throwOnError:false and translate
   credential retry statuses through the existing retry checker; preserve other status/transport errors.
In either case: one authentication attempt per call; application chooses subsequent retries.
```

Python `get_challenge` above also leaves errors general; `_calculate_session_keys:616–626` translates
retry status. That supports the distinction, not a mandate to keep it. Dependency: HSM-IMP-007's
password-present/unsupported-gate policy and CORE-HSM-X006. Acceptance needs recording responses for
success, `63Cx`, unrelated failure and malformed data, with exact INS/SW/provenance. Separately obtain
authorized command-specific wrong-password/recovery observations without exhausting a counter. A
changed contract reopens interface/retry XML, README, migration mapping and retry-test explanations.

### HSM-IMP-002 — Own secrets from first acquisition through failure

**High; source-confirmed uncleared long-lived secret paths. No-brainer: yes for moving owned byte
acquisition under cleanup; broader input/product policy stays in HSM-IMP-005.**
In management rotation, the first parsed key survives if the second parse throws before `try`. Symmetric
enrollment similarly acquires K-ENC/K-MAC before later parsing, prompts and output. A wrong-width CLI
private-key array also takes an early return without clearing that array. Clearing the management key
does not clear a separately allocated private key. The scope defect leaves owned authentication material
in managed memory on ordinary invalid input or callback/output failure.

**BEFORE — `src/YubiHsm/examples/HsmAuthTool/HsmAuthExamples/ChangeManagementKey.cs:25–30`,
`RunAsync`:**

```csharp
        var currentKey = Convert.FromHexString(currentKeyHex);
        var newKey = Convert.FromHexString(newKeyHex);

        try
        {
            await session.PutManagementKeyAsync(currentKey, newKey, cancellationToken);
```

**AFTER — literal, unapplied/uncompiled replacement shape for that acquisition/send scope:**

```csharp
        byte[]? currentKey = null;
        byte[]? newKey = null;
        try
        {
            currentKey = Convert.FromHexString(currentKeyHex);
            newKey = Convert.FromHexString(newKeyHex);
            await session.PutManagementKeyAsync(currentKey, newKey, cancellationToken);
            // Keep existing result output inside this scope.
        }
        finally
        {
            if (currentKey is not null)
                CryptographicOperations.ZeroMemory(currentKey);
            if (newKey is not null)
                CryptographicOperations.ZeroMemory(newKey);
        }
```

Rechecked companion anchors: `AddSymmetricCredential.RunAsync:35–79` and
`Cli/Commands/CredentialsCommand.cs:200–259`, `AddAsymmetricAsync`. Some CLI prompts are already inside
`try`; preserve those scopes rather than rewriting the entire tool. The calculation example already
parses its challenge/cryptogram arrays inside its clearing scope. Standalone `SessionKeys.Parse` input
and any connector/key copies remain caller-owned; successful returned arrays are cleared on disposal.

Python `_parse_credential_password:125–135` and `_password_to_key:157–172` create immutable byte values;
they are not an erasure template. Dependencies: CORE-HSM-X002 for encoder/final/lower-copy boundaries,
HSM-IMP-006 for test-owned secret scopes. Acceptance captures actual owned arrays across second parse,
wrong width, selection abort, prompt/output/send failure and success; witnesses must observe those same
arrays cleared before release. Retain original errors and never clear borrowed caller inputs. Reopen
sample/SessionKeys ownership and security guidance after the selected scope changes.

### HSM-IMP-003 — LIST must distinguish malformed records from future metadata

**Medium; source-confirmed parsing behavior, policy decision needed. No-brainer: no for the whole group.**
LIST silently drops recognized records shorter than three bytes and replaces invalid UTF-8 label bytes.
Dropping a record can hide a stored credential; replacement decoding can lose its exact identity and
misassociate a later label lookup. Unknown algorithm bytes already survive enum casting; unknown touch
bytes become `null` and notify conservatively when the credential is found. Valid future metadata is not
the same as malformed framing. An absent credential currently yields no notification, so a skipped
malformed record must not be treated as proof of no touch requirement.

**BEFORE — `src/YubiHsm/src/HsmAuthSession.cs:274–278,286–290`, `ListCredentialsAsync`
(two literal windows):**

```csharp
            var value = tlv.Value.Span;
            if (value.Length < 3)
                continue;

            var algorithm = (HsmAuthAlgorithm)value[0];
```

```csharp
            var labelBytes = value[2..^1]; // Everything except algorithm, touch, and retries-remaining
            var retriesRemaining = value[^1];
            var label = Encoding.UTF8.GetString(labelBytes);

            credentials.Add(new HsmAuthCredential(label, algorithm, retriesRemaining, touchRequired));
```

**AFTER — schematic contract choice:**

```text
Reject malformed recognized record framing and invalid label encoding with a deliberate parse error.
Choose required minimum/label grammar from the verified applet profile, not the current guard alone.
Keep valid unknown algorithm/touch values inspectable; if a raw record/response carrier is needed,
select its public shape and ownership explicitly. Do not blindly re-emit unknown response fields.
On LIST failure the existing notification path may conservatively request touch, not certify absence.
```

Python `list_credentials:250–263` decodes strictly but rejects unknown algorithm values and maps all
nonzero touch bytes to true. Do not copy those latter choices over C#'s forward-compatible carriers.
Dependencies: CORE-HSM-X006/profile evidence; Core TLV framing remains Core-owned. Acceptance supplies
mixed valid/short/unknown records, invalid UTF-8, empty/valid label boundaries, unknown touch and failed
LIST fixtures; checks identity and prompt policy, not merely thrown validation exceptions. Reopen LIST,
credential/algorithm XML, raw-metadata ownership and policy/parser tests when grammar is selected.

### HSM-IMP-004 — Honest calculation dispatch and presence notification

**Medium; source-confirmed tool mismatch. No-brainer: yes for a symmetric-only chooser; no for adding
an asymmetric handshake or changing the interactive tool's notification contract.**
The chooser exposes every stored algorithm, then invokes symmetric CALCULATE with a 16-byte context.
Selecting an asymmetric credential therefore cannot run its required 130-byte EPK-OCE/EPK-SD flow.
The session menu supplies no presence prompt, leaving a touch-required calculation without the SDK
notification even though the device still enforces its policy. Lack of a prompt is not a touch bypass.

**BEFORE — `src/YubiHsm/examples/HsmAuthTool/HsmAuthExamples/CalculateSessionKeys.cs:26–29`,
`RunAsync`:**

```csharp
        var labels = credentials
            .OrderBy(c => c.Label, StringComparer.OrdinalIgnoreCase)
            .Select(c => c.Label)
            .ToList();
```

**AFTER — schematic alternatives:**

```text
A. Filter chooser to symmetric credentials; handle the resulting empty list before prompting.
B. Dispatch by actual algorithm; asymmetric branch requires a real profile-aware application
   adapter for trusted HSM static key, EPK-SD and cryptogram, not a fabricated connector SDK API.
Separately choose diagnostic silence or supply a caller-owned presence prompt at session creation.
Keep the prompt alive through the entire workflow and terminal resolution.
```

Companion baseline `Cli/Menus/SessionKeyMenu.cs:21–25` creates the session without prompt options.
Python has separate `calculate_session_keys_symmetric:630–653` and
`calculate_session_keys_asymmetric:655–693`; no inspected Python interactive calculation-menu equivalent
establishes a notification design. Dependencies: HSM-IMP-007/008 for any expanded handshake and
CORE-HSM-X003 for callback lifetime. Acceptance checks AES-only, EC-only, mixed and unknown-algorithm
lists, exact selected branch/context and prompt request/resolution on success/failure/cancellation.
Physical touch and external authentication require the qualification card. Reopen chooser/menu, example
guide and presence commentary without claiming that filtering implements asymmetric support.

### HSM-IMP-005 — Diagnostic exposure versus safe-default tool

**High; source-confirmed exposure, product intent unsettled. No-brainer: no.**
The sample prints long-lived enrollment keys and all three session keys, accepts some secrets in
visible immutable strings, and exposes CLI key/password options. Terminal capture, history and process
argument visibility can retain material beyond byte-buffer disposal. The existing diagnostic warning
is useful but does not make these surfaces safe for application adaptation. Suppressing generated-key
output without a protected matching-HSM enrollment/recovery path can also make provisioning unusable.

**BEFORE — `src/YubiHsm/examples/HsmAuthTool/HsmAuthExamples/CalculateSessionKeys.cs:90–94`,
`RunAsync`:**

```csharp
            // Diagnostic output exposes session secrets. Use disposable test material and keep key
            // output out of an application's terminal/logging path.
            OutputHelpers.WriteHex("S-ENC", keys.SEnc);
            OutputHelpers.WriteHex("S-MAC", keys.SMac);
            OutputHelpers.WriteHex("S-RMAC", keys.SRmac);
```

**AFTER — explicit product alternatives:**

```text
A. Keep a deliberately diagnostic tool: explicit opt-in key reveal, disposable-material warning,
   protected capture rules and clearly marked visible/argument inputs.
B. Safe-default tool: no secret output/arguments by default; acquire mutable secrets through a
   selected secure-input path; provision matching HSM objects and protected recovery storage
   before clearing generated enrollment keys. Diagnostics remain a separately gated mode.
Neither mode claims that hiding text zeroes immutable strings or process-history copies.
```

Python CLI `_prompt_management_key`/`_prompt_credential_password:165–187` hides passwords, while
`_prompt_symmetric_key:190–194` is visible. That is limited UX precedent, not a universal safe tool.
Dependencies: HSM-IMP-002's independent cleanup repair and the application's provisioning owner.
Acceptance enumerates secret input/output sinks, verifies safe-mode no-secret output on success and
failure, checks opt-in behavior and recovery-before-erasure. Preserve logger/callback error provenance
without logging secret values. Reopen Program/help, operation/menu comments, README evaluation warning,
credential/provisioning guidance and migration/sample references only after intent is accepted.

### HSM-IMP-006 — Meaningful oracles and independent test cleanup

**Medium; source-confirmed evidence/resource gaps, not proof the underlying crypto is wrong.
No-brainer: yes for retained CTS/input ownership and failure-safe teardown; oracle/profile selection
and destructive hardware scope need decisions.**
The symmetric calculation test checks three lengths, derived enrollment checks algorithm metadata, and
the PBKDF2 “KnownVector” recomputes through the same BCL primitive. Those are useful shape/parameter
checks, not independent crypto or HSM-authentication evidence. The asymmetric dummy flow supplies
several invalid inputs and asserts an exact general `ApduException` with only nonzero SW: a different
invalid field or unsupported instruction can satisfy it, while a retry subclass fails exact-type matching.
Both integration classes create timeout sources and return only their tokens; per-call password/key
properties allocate independent inputs that the tests do not own in clearing scopes. The helper resets
before the body but does not independently restore persistent state afterward.

**BEFORE — `src/YubiHsm/tests/Yubico.YubiKit.YubiHsm.IntegrationTests/HsmAuthSessionTests.cs:18–20`,
`NewToken`:**

```csharp
    // Each call creates an independent timeout so later operations start with their own deadline.
    private static CancellationToken NewToken(int timeoutSeconds = 30) =>
        new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)).Token;
```

**AFTER — schematic test ownership/oracle plan:**

```text
Retain/dispose each deadline source; retain and finally-clear every fixture-owned secret array.
Start the clearing scope at first acquisition and await admitted work before clearing/disposal.
Use independently sourced fixed PBKDF2 and key-derivation vectors, stating source and parameters.
For negative profile tests: establish a valid control, mutate one field, assert specific provenance.
Keep applet-only shape tests labeled as such; genuine connector authentication is another lane.
In authorized dedicated-device runs, perform independent final reset/readback even on body failure;
retain original failure separately if cleanup also fails. Never silently erase a user's credentials.
```

Companion anchors: `HsmAuthSessionTests.CalculateSessionKeysSymmetric_Returns48Bytes:199–209`,
`HsmAuthAsymmetricTests.CalculateSessionKeysAsymmetric_WithDummyCryptogram_ThrowsApduException:128–158`,
unit `Pbkdf2DerivationTests.DeriveKeys_KnownVector_MatchesExpected:25–37`, and
`TestExtensions/HsmAuthTestStateExtensions.WithHsmAuthSessionAsync:43–67`. Key rotation's copied source
and generated second-buffer failures need their own witnesses; a later clone's clearing is insufficient.

Python `_password_to_key:157–172` corroborates parameters but was not executed as a KAT here. Its
asymmetric methods illustrate actual HSM-key roles, not a reusable .NET hardware fixture. Dependencies:
HSM-IMP-007/008 and CORE-HSM-X001/003. Acceptance records body and cleanup independently, actual
array identities and CTS disposal, single-fault status attribution and independent equality of all three
keys. Touch-tagged metadata tests remain metadata evidence until attended CALCULATE is measured.
Reopen test explanations/helper, test guide and crypto/support claims on changed assertions, preserving
the original failed parser/cancellation probes alongside any successors.

### HSM-IMP-007 — Profile validation must name its actor

**Medium; source-confirmed partial validation and interoperability risk; no demonstrated peer-trust
bypass. No-brainer: no.**
C# checks 16/130-byte contexts and imported scalar width, but asymmetric CALCULATE forwards raw HSM
public-key/cryptogram bytes without a local curve/point/trust check. GET_PUBLIC_KEY checks width only;
GET_CHALLENGE returns an unchecked response shape. The application must supply the trusted **HSM**
static key, not the YubiKey credential's exported PK-OCE. The device and integration layer currently
perform different parts of validation; generic Core EC helpers are not called by these HSM methods.
Broadening local checks requires a verified applet grammar and a deliberate raw escape hatch, not an
invented guarantee that the existing API authenticates the peer.

**BEFORE — `src/YubiHsm/src/HsmAuthSession.cs:553–558`, `CalculateSessionKeysAsymmetricAsync`:**

```csharp
            data = TlvHelper.EncodeAndDisposeList(
                new Tlv(TagLabel, labelBytes),
                new Tlv(TagContext, context.Span),
                new Tlv(TagPublicKey, publicKey.Span),
                new Tlv(TagResponse, cardCryptogram.Span),
                new Tlv(TagCredentialPassword, credPwBytes));
```

**AFTER — schematic actor/shape choices:**

```text
A. Keep raw inputs: document exact borrowed byte roles and device/application validation responsibilities.
B. Add a typed validated layer: reject invalid scalar/curve/point and profile-defined cryptogram/context
   shapes before I/O; retain deliberate low-level access for integrations that need other valid variants.
Choose GET_CHALLENGE/GET_PUBLIC_KEY response grammar and error types without inferring algorithm
from total width alone. Select absent versus present-empty optional values explicitly.
Trusted HSM identity and handshake completion remain application responsibilities in both designs.
```

Python `put_credential_asymmetric:395–408`, `get_public_key:448–452` and
`calculate_session_keys_asymmetric:673–691` use typed P-256 inputs/point conversion. Python's truthy
optional fields in `_calculate_session_keys:606–610` omit empty data; C# symmetric
`HsmAuthSession.cs:440–441` sends tag 78 for present-empty memory. No precedent selects the correct
public C# admission policy. Password-present challenge behavior below its transmission gate also needs
an explicit choice; an ignored value is not authentication proof.

Dependencies: CORE-HSM-X006, HSM-IMP-001 and HSM-IMP-008, bounded normative GAP-SPEC-1. Acceptance
uses independently valid and single-invalid scalar/point/context/cryptogram/challenge/public-key vectors,
checks whether rejection occurs before I/O or at the intended device actor, and preserves trusted-HSM
versus PK-OCE roles. Reopen interface format/exception contracts, handshake guide, algorithm/credential
XML, byte-level tests and adapters on selected validation/raw-carrier changes.

### HSM-IMP-008 — Decode verified response variants, not arbitrary prefixes

**High; confirmed failure on the recorded development key; response layout and production applicability
unsettled. No-brainer: no.**
The [original investigation](modules/yubihsm/pass-1/yubihsm-001/cryptogram-investigation.md) records a
valid laboratory symmetric card cryptogram followed by a 56-byte response rejected by the strict
48-byte parser. The successful status path reaches Parse; this is not evidence of applet cryptogram
rejection. The documented cryptogram-taking flow fails after device processing, potentially preventing
an intended authenticated HSM workflow. All three keys and retry restoration for that call were **not
checked**, because parsing failed first. The raw 56 bytes were not retained, so neither the key prefix
nor the suffix's meaning is established.

**BEFORE — `src/YubiHsm/src/SessionKeys.cs:97–108`, `Parse`:**

```csharp
    public static SessionKeys Parse(ReadOnlySpan<byte> response)
    {
        if (response.Length != ExpectedResponseLength)
            throw new ArgumentException(
                $"CALCULATE response must be exactly {ExpectedResponseLength} bytes, got {response.Length}.",
                nameof(response));

        var sEnc = response[..KeyLength].ToArray();
        var sMac = response[KeyLength..(KeyLength * 2)].ToArray();
        var sRmac = response[(KeyLength * 2)..].ToArray();

        return new SessionKeys(sEnc, sMac, sRmac);
```

The session passes complete `response.Data.Span` at `HsmAuthSession.CalculateSessionKeysAsync:923`.
Its final response is cleared even on this parsing failure; a failing successful-resolution callback
also disposes produced keys. Neither fact clears earlier pipeline copies or identifies the missing tail.

**AFTER — alternatives only after actual layout/profile verification:**

```text
A. Session-layer decoder accepts only verified complete response variants, passes the verified
   48-byte key region to the existing strict Parse, and handles any required additional material explicitly.
B. Select a separately modeled owned calculation result if the host needs returned authentication
   material; define its meaning, validation, disposal and transfer before changing public signatures.
Reject unrelated lengths/malformed authentication data. Do not use length>=48 truncation, silently
widen standalone Parse, name the suffix by intuition, or claim omission is an authenticated workaround.
```

Pinned Python `SessionKeys.parse:210–220` slices without strict length checking; its calculation helper
sends nonempty supplied cryptograms. Frozen vendor history preserves 56-byte handling since 2023,
the removed `card_crypto` tail field and vendor C's suppression of an eight-byte symmetric cryptogram
plus its “host crypto” comment. Those request/parser choices disagree in important ways and do not
define our observed suffix or an introduction firmware. Do not silently replace that historical evidence
with the current Python slices.

Dependency: CORE-HSM-X005 and HSM-IMP-007. Acceptance first captures length and separate SW transiently,
compares all three actual candidate key regions and both independently derived host/card tail candidates
over distinct contexts, and checks retries before cleanup. Then accept supported variant grammar and
firmware evidence, add malformed/accepted/cryptogram-case regression oracles and actual owned-result
clearing. Genuine HSM/connector authentication remains a separate gate. Reopen symmetric XML,
SessionKeys parser contract, calculation helper, README exchange sketch, tool comments, tests and
shared migration/support statements. Frozen failed probe bytes remain unchanged.

### CORE-HSM-X001 — Reset and authorization state are not channel recovery

**Medium risk; source-confirmed retained state, no demonstrated secure-reset breakage. No-brainer: no.**
RESET reuses the protocol and then reselects without a fresh SCP handshake. Under SCP, SELECT is
unprotected while old host state and public flags remain. Inherited `IsAuthenticated` records prior
Core SCP setup, not credential authority, live channel health or external HSM authentication. A caller
may otherwise attempt protected work against uncertain state. The current comments already say this
path owns no PC/SC transaction; that historical prose defect is resolved, not a new fix here.

**BEFORE — `src/YubiHsm/src/HsmAuthSession.cs:516–525`, `ResetAsync`:**

```csharp
        var command = new ApduCommand { Ins = InsReset, P1 = ResetP1, P2 = ResetP2 };
        await _backend.SendAsync(command, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        // Reuse the protocol; this path owns no PC/SC transaction and starts no fresh SCP handshake.
        // Under SCP the re-SELECT passes through unprotected, retaining old host state and public flags;
        // it does not establish that the next protected operation will succeed.
        // Reselect after reset while retaining the resolved feature-version policy. Discarding the
        // returned version keeps caller overrides and later feature gates consistent within this session.
        _ = await _backend.ReadInitialStateAsync(cancellationToken).ConfigureAwait(false);
```

**AFTER — maintainer choices, not an implemented recovery API:**

```text
A. Terminal/reset-uncertain session: retire appropriate readiness/auth state and require dispose/reopen.
B. Explicit reinitialization: define retained option/key ownership, discard old secure graph, select,
   perform the required new handshake and publish readiness only after accepted completion.
C. Retain current behavior with its limitations; require caller reopen before relying on protected work.
Define outcomes for RESET failure, SELECT failure and cancellation separately; do not import OATH's
different terminal-reset contract or reinterpret a historical setup flag as live authorization.
```

Python `reset:245–248` sends RESET without C# reselect or a new handshake; it is no recovery proof.
Dependencies: HSM-DEP-001/002, shared CORE-B5-C001/C006 and CORE-B6-C011/C012/C015. Acceptance holds
RESET/reselect phases in a recording fixture and checks selected state/error/option policy; physical SCP
qualification must reach actual authenticated setup, RESET and the next protected operation. Preserve
serial 125's two `6A88` failures **before** EXTERNAL AUTHENTICATE/RESET; they establish no reset outcome.
Reopen reset/auth/factory XML, README/CLAUDE, reset tool/helper/tests and shared SCP/migration guidance.

### CORE-HSM-X002 — Supported final-owner clearing and independent copies

**Low maintainability risk for the legacy helper; no new final-response erasure defect.
No-brainer: yes for supported owner API substitution; no for a blanket lower-copy policy.**
The helper reaches the actual final response array, including status bytes. Core now exposes its
supported `ApduResponse.Clear()` owner API. Replacing write-through extraction is a mechanical
modernization, not proof of stronger end-to-end erasure. Retry/backend failure can throw before the
calculation helper enters its successful-response clearing `try`. Inline TLV construction failures,
encoder staging, transport results, fragments and decrypted arrays have independent lifetimes. Classify
error payloads by their actual meaning; public LIST metadata is not secret merely because it came back
from a key.

`SessionKeys.Parse:104–108` also creates its three owned arrays sequentially before publishing the
container. Include partial key-array creation in the allocation-failure scope: if later creation fails,
earlier copied arrays need an owner that can clear them. This is a source-level exceptional-path
question, not an executed allocation-failure result or a reason to weaken strict input framing.

**BEFORE — `src/YubiHsm/src/HsmAuthSession.cs:953–959`, `ZeroApduResponse`:**

```csharp
    internal static void ZeroApduResponse(ApduResponse response)
    {
        // This legacy helper reaches the final response's actual array, including status bytes.
        // Core now exposes Clear() for that owner. Earlier transport/fragment/decrypted copies remain
        // separate; retry/backend failures before the caller's try are outside this cleanup scope.
        if (MemoryMarshal.TryGetArray(response.RawData, out var rawData))
            CryptographicOperations.ZeroMemory(rawData.AsSpan());
```

**AFTER — schematic scope split:**

```text
Mechanical local substitution: invoke existing response.Clear() for this final owner.
Preserve Parse/callback failure cleanup and dispose produced keys if ownership transfer fails.
For local pre-try/partial TLV and retry-error owners: select cleanup scopes after semantic classification.
For partial SessionKeys construction: clear already-created arrays if ownership transfer cannot finish.
For independent formatter/transport/fragment/decrypted owners: link the canonical Core copy work;
do not claim that clearing final response or SessionKeys clears those arrays or borrowed caller inputs.
```

Python `SessionKeys.parse:210–220` slices immutable bytes and offers no disposal-owner equivalent.
Dependencies: HSM-IMP-002, HSM-DEP-003; Core C-C005/C006/C007/C020 and B6-C002/C006/C007 retain the
lower-owner work. Acceptance retains aliases to the **actual** final owner and produced key arrays,
checks success/Parse/callback-failure clearing, and supplies separate identity-based witnesses for any
new exceptional-owner repair, including partially constructed key arrays. Never inspect a pool array
after return. Reopen local owner/SessionKeys
tests and qualified TLV/APDU/security guidance only for the changed owner scope.

### CORE-HSM-X003 — Lease or explicitly serialize the complete public workflow

**Medium lifecycle risk; source-confirmed phase boundary, overlap policy unsettled. No-brainer: no.**
LIST lookup, CALCULATE, parsing, prompt callbacks and final clearing are not one guarded exchange.
A held callback or gap between exchanges can outlive disposal; exchange drain alone cannot protect all
borrowed inputs or later result handling. Existing guidance already asks callers to await and serialize
complete tasks. Prompt request failure prevents calculation/resolution; admitted requests resolve once
with token None, and a success-resolution failure disposes the keys before transfer. Preserve those
existing safeguards.

**BEFORE — `src/YubiHsm/src/HsmAuthSession.cs:425–426,447–453`,
`CalculateSessionKeysSymmetricAsync` (two literal windows):**

```csharp
        UserPresenceNotification userPresenceNotification = CreateUserPresenceNotification(
            await GetUserPresenceContextAsync(label, UserPresenceOperations.YubiHsmAuth.CalculateSessionKeysSymmetric, cancellationToken).ConfigureAwait(false));
```

```csharp
            var command = new ApduCommand { Ins = InsCalculate, Data = data };
            return await CalculateSessionKeysAsync(
                    command,
                    userPresenceNotification,
                    "CALCULATE symmetric session keys",
                    cancellationToken)
                .ConfigureAwait(false);
```

**AFTER — schematic admission choices:**

```text
A. Shared whole-operation admission spanning LIST, request, CALCULATE, Parse, resolution and finally;
   specify overlap, disposal and callback reentrancy behavior before introducing a lease mechanism.
B. Keep per-exchange protection and explicit caller serialization; document/review the full-task boundary.
Both retain caller-owned input/prompt lifetime through actual terminal completion, even when an outer
wait cancels. Resolve once, retain original failure and release all held work in failure-safe teardown.
```

Python synchronous `calculate_session_keys_symmetric:646–653` has no comparable asynchronous callback
or disposal lease. Canonical shared operation-lifetime owner is **CORE-B5-C006**, linked from the earlier
WebAuthn/Management/CTAP2/SD/OTP work; this is its HSM call-site application, not another shared bug.
HSM-DEP-002 and CORE-HSM-X001 retain reset/native distinctions. Acceptance holds LIST→CALCULATE,
request, resolution and final cleanup independently; attempts competing operation/disposal and verifies
the selected policy, exactly-once outcomes and actual result disposal. LIST cancellation propagates;
ordinary LIST errors notify conservatively. Measured `6982`/`Failed` followed by fresh-session recovery
remains a historical result, not proof of native interruption or natural touch timeout. Reopen interface,
creation/calculation/prompt tests and shared user-interaction/lifetime guidance on policy changes.

### CORE-HSM-X004 — Effective version is policy, not a physical reading

**Low user-decision risk in display provenance; source-confirmed policy, no inferred gate defect.
No-brainer: no for fallback/development policy changes; yes for an agreed truthful display label.**
The backend reads SELECT tag 79, accepts three bytes and falls back to 5.4.3 for missing/wrong-width
values. Malformed TLV framing can fail before fallback. Initialization applies caller override only
after SELECT; reset discards the new version and retains original effective policy. Metadata-based test
selection is another actor. The tool currently labels session policy “YubiHSM Auth version,” despite its
correct explanatory comment. Mistaking that display for raw applet or physical firmware can lead to
incorrect qualification or feature assumptions.

**BEFORE — `src/YubiHsm/examples/HsmAuthTool/Cli/Commands/InfoCommand.cs:30–32`, `RunAsync`:**

```csharp
        // This is session feature policy (SELECT version/fallback, or an override), not an independent
        // physical-firmware measurement. The display label does not change that provenance.
        AnsiConsole.MarkupLine($"YubiHSM Auth version: [green]{session.FirmwareVersion}[/]");
```

**AFTER — schematic separation:**

```text
Agree a display label that names effective session feature policy.
If exposing raw SELECT version/source is desired, select a separate carrier/provenance contract;
do not pretend the current FirmwareVersion property already preserves every source value.
Separately retain or change fallback, major-zero and override/reset semantics by maintainer decision.
Overrides never skip SELECT and metadata/test filtering never certifies actual operation support.
```

Source corroboration: `HsmAuthBackend.ReadInitialStateAsync:27–32`, parser `44–56` and
`HsmAuthSession.InitializeAsync:158–159`. Python constructor `226–238` applies
`_OverrideVersion.patch` after SELECT; `py:yubikit/core/__init__.py:246–247` patches exact `0.0.1`.
That is not C#'s general major-zero gate. Historical serial 103 SDK/direct Python **0.0.1**, ykman display
**5.8.0**, and Management metadata **5.8.0.beta.0** retain their distinct provenance.

Dependencies: HSM-DEP-001, Core A-C015/version policy and CORE-B5-C001, plus reset X001. Acceptance
checks absent/wrong-width/malformed/major-zero SELECT, override without SELECT bypass, reset retention
and truthful tool output; separately qualify production/older firmware. Reopen version/backend/factory
XML, feature tables, Info comments/help and metadata/gate tests. The existing obsolete
`FeatureGetChallengeNoPassword` alias explanation remains correct and is not a second gate defect.

### YubiHSM Auth response, handshake and physical qualification

**Qualification; no assigned defect severity. No-brainer: no—explicit device/HSM/reset authorization and
evidence lane selection are required. BEFORE code: N/A. AFTER code: N/A.** Code cannot establish an
unrun physical outcome. The frozen [hardware results](modules/yubihsm/pass-1/yubihsm-001/hardware/results.md)
and [cryptogram follow-up](modules/yubihsm/pass-1/yubihsm-001/cryptogram-investigation.md) provide the
starting procedures and original outcomes, not authorization for new execution.

The final YubiKey-only probes established omission-only three-key equality/retry restoration,
user/admin password transitions and attended touch with `Completed` plus independently matched keys.
The initial supplied-cryptogram parser failure and cancellation-expectation failure remain failures;
the later `6982`/`Failed` and fresh-session recovery result does not rewrite them. Final independent
applet readback found zero credentials, eight management retries and usable default management key
on serial 103, USB SmartCard/Linux. This establishes neither production applicability nor an HSM session.

1. **Recorded-device response layout:** under fresh, explicit destructive applet authorization, use
   transient owned raw response storage, exact request/tag presence and separate SW. Compare three key
   regions and both candidate host/card tails independently over distinct contexts. Observe post-call
   retries before cleanup; separately test a modified cryptogram without exhausting retries. Persist
   comparison booleans, offsets/lengths and provenance, not unprotected key bytes. Follow HSM-IMP-008's
   retained procedure; successful cleanup is distinct from successful body.
2. **Response/profile authority:** obtain bounded vendor/profile evidence for accepted response variants,
   scalar/point/challenge/cryptogram grammar and firmware applicability. Descriptive applet manuals and
   Python/C history are corroboration, not a located versioned normative clause. GAP-SPEC-1 remains
   precise missing evidence, not permission for an unlimited new search. Core crypto agreement cannot
   identify the HSM applet suffix.
3. **Genuine HSM/connector flow:** identify HSM firmware, connector/library revision, object enrollment
   and trusted static-key provenance. Use the real HSM challenge/EPK-SD and cryptogram, complete peer
   verification and host authentication, then demonstrate protected command and response verification.
   Check negative peer/cryptogram cases independently. Python HSM Auth calculates keys; its API alone
   supplies no end-to-end host authentication proof. Select result/receipt ownership before integration.
4. **Secure reset and task/physical behavior:** identify supported SCP setup and options, reach actual
   authentication, then separate RESET, SELECT, host-state transition, next protected operation and fresh
   recovery. Retain the two pre-authentication `6A88` outcomes. For cancellation/touch/removal, record
   actual token/request/device/terminal times and statuses; retain inputs/prompt/native owners until
   admitted work terminates. Do not equate an outer cancelled wait with native interruption or label
   `TimedOut` without device evidence. Cross-process access/release and physical power effects require
   independent observations, not managed disposal success.
5. **Supported-platform lanes:** qualify production/older firmware, NFC and other OS routes separately;
   identify package/native loaded producer and JIT versus Native AOT artifacts. Windows report-owner
   managed passes concern FIDO/OTP HID, not this SmartCard-only applet's native PC/SC or HSM profile.
   Core listener/loader/PCSC/producer questions keep their existing owners. No Linux applet pass closes
   Windows, macOS, AOT, hotplug, permission or native-release acceptance.

Acceptance records authorized scope, exact C#/Python/vendor/connector revisions, device/firmware/OS/
transport/state and actual executed outcomes with independent post-run reset/readback and secret
cleanup. A skip, metadata-only check, compilation or synthetic KDF match does not close a physical or
authenticated-handshake lane. Reopen only the support/profile/reset/presence claims affected by the
new evidence; retain historical failures and narrower successes unchanged.

### YubiHSM Auth shared dependencies and successor boundaries

These are narrow dependencies, not imported Core implementation cards or a new accepted defect count.
Original IDs/facets remain in the frozen reconciliation; link an existing canonical owner when it applies.

| Original dependency / shared owner | HSM consequence and remaining boundary |
| --- | --- |
| **HSM-DEP-001**; Core **CORE-A-C015**, **CORE-B5-C001** | SELECT/fallback/override/major-zero mechanism is traced and prose accepted. X004 owns local policy/display and X001 reset consequences. Old/production firmware, NFC and OS qualification remain open. Shared version semantics stay Core-owned. |
| **HSM-DEP-002**; **CORE-B5-C006**, **CORE-B6-C011/C012/C015** | Owned convenience versus borrowed direct connection and prompt, admitted uncancellable drain and successful result transfer are traced. X003 owns HSM full-workflow application; X001 reset; physical/native interruption, release and secure recovery remain unqualified. |
| **HSM-DEP-003**; **CORE-C-C005/C006/C007/C020**, **CORE-B6-C002/C006/C007** | X002/HSM-IMP-002 own local response/exception/input-copy actions. Final-owner erasure is functional; lower formatter/transport/fragment/decrypted/TLV staging copies retain their independent canonical owners and semantic cleanup decisions. No end-to-end erasure guarantee. |
| **CORE-B1-C006/C009**, **CORE-B4-C005/C006/C009** where SmartCard creation/native execution applies | PC/SC end-error reporting, post-open logging cleanup, loader/ABI/string/producer and shared error provenance remain Core questions. HSM factory trace or disposal outcome is not native release proof. Do not turn adjacent dependencies into new HSM defects. |
| Windows successor **CORE-B4-C001/C007**, **CORE-B5-C005/C006**, **CORE-B6-C015** | Old built-in HID report mechanisms are superseded and new managed staging prerequisites satisfied at their recorded scope. HSM is SmartCard-only: no direct HSM HID fix or physical closure follows. Independent copies, complete workflows, SCP/reselect and PC/SC/native/platform qualifications remain open. |

The eleven non-HSM routed provenance keys in the boundary's inherited register stay with their own
OATH, SD, OTP, OpenPGP and PIV owners. Shared-mechanism comparison closes none of their applet reset,
cache, key, workflow or physical questions. PIV protected tool/physical and OpenPGP original dependency
dispositions are untouched. Callback/log-provider exceptions retain their own failure/redaction
responsibilities; header-only APDU logs do not establish universal callback redaction.

### YubiHSM Auth coverage ledger and exclusions

| Original ID | Current disposition and canonical review location |
| --- | --- |
| **HSM-IMP-001** | Deferred retry-contract decision → HSM-IMP-001; CORE-HSM-X006 alias adds no bug |
| **HSM-IMP-002** | Deferred acquisition/exception/source-copy repair → HSM-IMP-002; X002 preserves final versus lower-owner distinction |
| **HSM-IMP-003** | Deferred malformed/unknown LIST policy → HSM-IMP-003; prompt consequence and X006 retained |
| **HSM-IMP-004** | Deferred chooser/algorithm/prompt scope → HSM-IMP-004; asymmetric/live flow not silently promised |
| **HSM-IMP-005** | Deferred diagnostic/safe-default input/output/provisioning decision → HSM-IMP-005 |
| **HSM-IMP-006** | Deferred stronger independent oracles, single-fault status attribution, input/CTS ownership and independent persistent cleanup → HSM-IMP-006 plus qualification |
| **HSM-IMP-007** | Deferred request/response/raw-carrier/validation-actor contract → HSM-IMP-007; optional empty/absent and challenge password-gate questions retained |
| **HSM-IMP-008** | Confirmed historical 56/48 failure, no fix → HSM-IMP-008; region/receipt grammar, firmware and genuine HSM authentication remain qualification |
| **HSM-DEP-001** | Source mechanism and prose answered, not runtime/physical closure → X004/X001 and shared version owner |
| **HSM-DEP-002** | Source ownership/drain mechanics and prose answered, not whole-workflow/native/physical closure → X003/X001 and qualification |
| **HSM-DEP-003** | Owner graph traced; final-owner clearing source-functional, independent copies/exception scopes remain → X002/HSM-IMP-002 and existing Core copy owners |
| **CORE-HSM-X001** | Reset/auth/transaction documentation corrected and accepted; runtime state/reset options and secure/physical qualification → X001 |
| **CORE-HSM-X002** | Ownership documentation corrected and accepted; legacy final-owner helper works, modernization/lower-copy and exceptional-owner facets → X002/HSM-IMP-002 |
| **CORE-HSM-X003** | Complete-task guidance accepted; whole-method overlap/disposal policy and native/presence evidence → X003 and qualification |
| **CORE-HSM-X004** | Version provenance/override/fallback prose accepted; effective display/policy and old/production/profile qualification → X004 |
| **CORE-HSM-X005** | Existing limitation prose retained; same HSM-IMP-008, not another mismatch → HSM-IMP-008/007 and qualification |
| **CORE-HSM-X006** | Source-accurate retry/parser/profile prose accepted; original HSM-IMP-001/003/007 remain deferred in their cards |
| **CORE-HSM-X007** | Diagnostic/oracle qualifications and narrowed exception-type prose accepted; HSM-IMP-002/004/005/006 retain engineering facets; retry-message subclaim explicitly retracted |

**Explicit exclusions:** the retracted **`core-001:CORE-HSM-X007#retry-message-observation`** remains
RETRACTED. Current `HsmAuthSession.cs:995–996` and
`HsmAuthRetryExceptionTests.cs:204` both use `Invalid credential password`; no mismatch fix or test
rewrite is proposed. Core's accepted reset/no-transaction, full-task, version/owner and retry exception
prose is not reopened merely because frozen candidates once requested it. `HsmAuthRetryException.cs:43–45`
already limits the catch claim to APDU-status failures. The rejected Pass-1 test-guide link claim remains
excluded: `../../../docs/TESTING.md` is correct from `src/YubiHsm/tests`. C001–C012 are accepted
documentation origins, not twelve additional unresolved engineering IDs. Existing ordinal comparison
versus all-property record equality and obsolete feature-alias explanations are intentional, not bugs
inferred from different Python choices.

Coverage: **eight HSM-IMP groups, three HSM-DEP questions, seven CORE-HSM-X groups, 13 review cards**
(eight original groups, four local boundary/policy/owner cards and one qualification-only card). Counts
are nonadditive coverage groups, not thirteen proven bugs. Static checks verify current-pin BEFORE
windows, paths/symbols/ranges, local links/anchors/fences, every original ID and scoped edit boundaries.
Python pin/clean status matched. At author handoff, other applet bodies were preserved; changes were
limited to YubiHSM Auth, its overview row and dispatch label. No fixes, compilation/build,
SDK tests, hardware/native/crypto execution, frozen edits, commits, pushes or subagents occurred.

## OpenPGP

### Context and review map

OpenPGP exposes card-level key, credential, certificate and private-key operations over USB CCID or
NFC SmartCard. It does not decrypt an OpenPGP file or validate an attestation trust chain for the caller.
PW1 is the User PIN: VERIFY P2=81 enables signing and P2=82 enables other operations, but both modes
use the same credential and retry counter. PW3 is the Admin PIN; the Reset Code is a separate credential.
SCP channel authentication grants none of those card access states.

The [published handoff](openpgp.md), [fourteen-group ledger](modules/openpgp/pass-1/openpgp-001/findings.md)
and [Core boundary](modules/core/pass-1/core-001/boundary-OpenPgp.md) are authoritative. The boundary's
[applet dispositions](modules/core/pass-1/core-001/boundaries/OpenPgp/applet-findings-dispositions.json)
close none of the fourteen engineering groups. Their numeric gaps are intentional. Its four inherited
Core questions have source answers, not implementation or platform closure. The original 21 PGP-DEP
labels map claims to documentation; they are not 21 more bugs. PGP-C022/PGP-REV-005 extends IMP-018.

C# BEFORE windows were re-read at `6ce355468047e8004fd51cf1425808f6e8242ed3`; Python HEAD is
`4ca60f706af930459138d8dc0f0f953480e1c7a4`, with clean status, in the repository named in the master
conventions. Every `py:` citation here uses that pin. The Python source is a comparator, not .NET
ownership equivalence or new hardware evidence. All AFTER blocks are **proposed, unapplied and
uncompiled**; `text` blocks are design alternatives, not existing APIs. Deeper source/probe history can
be loaded when a reviewer selects a card; no broad new audit was performed.

| Original group → review card | Severity and rationale | Certainty / no-brainer |
| --- | --- | --- |
| [PGP-IMP-001](#pgp-imp-001--user-identity-in-both-verify-modes) | High: a correct PIN can consume retries and be reported against the wrong credential | Current source and historical device controls / Yes for identity fix |
| [PGP-IMP-002](#pgp-imp-002--persistent-kdf-transitions) | High risk: transition changes credentials and can strand access | Current flow; wider transition contract unsettled / No |
| [PGP-IMP-003](#pgp-imp-003--kdf-aliases-and-independent-secret-copies) | High: credential-hash copies and mutable alias lifetime lack complete ownership | Source-confirmed; no exploit demonstrated / No for group |
| [PGP-IMP-005](#pgp-imp-005--reset-status-postcondition-and-channel-state) | High: apparent reset completion can conceal retained ordinary material | Unchecked statuses source-confirmed; live failure not reproduced / No for group |
| [PGP-IMP-007](#pgp-imp-007--x25519-decipher-framing) | Medium: modeled agreement cannot execute through this formatter | Source plus historical same-key comparison / Yes for wrapper |
| [PGP-IMP-009](#pgp-imp-009--partial-key-mutation-and-stale-cache) | High risk: destructive partial writes and stale algorithm state | Source-confirmed non-atomic flow / No |
| [PGP-IMP-010](#pgp-imp-010--capability-and-family-admission) | Medium: unsupported operations reach the device; support claims can mislead | Source gaps; old-family range unqualified / No |
| [PGP-IMP-011](#pgp-imp-011--roca-generation-and-test-applicability) | High: vulnerable on-device RSA generation is not guarded | Source/advisory; no key attack performed / No for policy group |
| [PGP-IMP-013](#pgp-imp-013--nested-att-control-reference-template) | Medium: ATT read/provisioning wire is malformed | Source plus public-only historical comparison / Yes for encoder |
| [PGP-IMP-014](#pgp-imp-014--neo-import-generation-and-deletion) | Medium: source-incompatible old-family provisioning | Wire mismatch confirmed; device tolerance unqualified / No for group |
| [PGP-IMP-015](#pgp-imp-015--tool-input-state-and-resource-contracts) | High risk for secret exposure/wrong-device mutation; Low for public certificate resource delay | Selected current sites confirmed / No for group |
| [PGP-IMP-016](#pgp-imp-016--meaningful-oracles-and-failure-cleanup) | Medium: weak tests can conceal regressions or leave altered state | Current representative oracles; other sites inherited / No for group |
| [PGP-IMP-018](#pgp-imp-018--open-values-credential-format-and-generation-time) | Medium for credential/parser defaults; Low for timestamp display | Current source; intended representation unsettled / No |
| [PGP-IMP-019](#pgp-imp-019--pin-status-provenance-and-retry-uncertainty) | Medium: non-PIN failures can provoke unsafe retry advice | Current mapping confirmed / No |
| [PGP-CORE-001](#pgp-core-001--reader-units-and-card-profile) | Medium support/input-policy risk, not demonstrated authentication bypass | Source answer complete; policy open / No |
| [PGP-CORE-003](#pgp-core-003--version-fallback-and-effective-profile) | Medium policy risk: unrelated failure can become compatibility version | Source-confirmed; development support separately qualified / No |
| [PGP-CORE-004](#pgp-core-004--whole-workflow-and-notification-result-ownership) | Medium lifetime/concurrency risk; Low public-certificate resource delay | Source-confirmed boundary; native causality unqualified / No for group |
| [Physical/chronology qualification](#openpgp-qualification-and-restoration) | Qualification, no assigned defect severity | Historical evidence only / No |

### PGP-IMP-001 — User identity in both VERIFY modes

Extended verification currently derives with Reset Code salt and reports Reset attempts on a 6982
fallback. With distinct salts, serial 103 rejected a correct extended User PIN, spent User retries 3→2
and reported zero rather than two. This is not evidence that Reset Code can authorize PW1.

**BEFORE — `src/OpenPgp/src/OpenPgpSession.Pin.cs`, `VerifyPinAsync`, lines 34–35:**

```csharp
var p2 = extended ? (byte)Pw.Reset : (byte)Pw.User;
var pw = extended ? Pw.Reset : Pw.User;
```

**Proposed AFTER — literal C#, identity-only change:**

```csharp
var p2 = extended ? (byte)0x82 : (byte)Pw.User;
var pw = Pw.User;
```

**Python:** `py:yubikit/openpgp.py:1205–1232`, `_verify`/`verify_pin`, derives PW.USER and adds mode
only to P2; retry lookup uses the same logical identity. No-brainer **yes** for this settled separation,
not for broader PIN error classification in IMP-019.

**Acceptance:** fake distinct User/Reset salts and counters, assert derived bytes and P2=81/82,
User retry reporting for both modes and unchanged Admin/Reset routes. Correct the existing wire test
that expects Reset attempts. Re-run the intended contract on an authorized dedicated device only in a
later lane; preserve the original failed assertion as evidence. Reopen DEP-001 PIN/Pw/KDF/retry XML,
README and migration. Dependencies: IMP-003 lifetime and IMP-019 status provenance.

### PGP-IMP-002 — Persistent KDF transitions

F9 provisioning is a persistent credential transition, not a harmless session option. Card 3.4.1 §4.3.2
assigns credential/configuration alignment to terminal software; YubiKey's transition installs initial
User/Admin hashes and invalidates the Reset Code. The historical F9 refusal after generating a key
and later success when provisioning first must both remain; neither establishes a universal firmware rule.

**BEFORE — `src/OpenPgp/src/OpenPgpSession.Pin.cs`, `ChangePwAsync`, lines 336–338:**

```csharp
var kdf = await GetOrLoadKdfAsync(cancellationToken).ConfigureAwait(false);
currentBytes = kdf.Process(pw, currentPin.Span);
newBytes = kdf.Process(pw, newPin.Span);
```

**Proposed AFTER — schematic alternatives, no selected transition API:**

```text
Option A: retain low-level F9 provisioning, with an explicit caller-managed transition procedure.
Option B: design a high-level vendor-profile transition, validating initial hashes and prerequisites.
For either: distinguish enable/change/disable from ordinary CHANGE PIN using the current KDF.
Read persistent configuration and authenticate expected credentials from a fresh session.
Never infer successful alignment from the just-installed cache or equal old/new PIN values.
```

**Python:** `py:yubikit/openpgp.py:1165–1192`, `get_kdf`/`set_kdf`, checks KDF capability and documents
initial credential/reset-code effects; `_change:1253–1273` derives both inputs with the current KDF.
It offers no automatic atomic credential transition. No-brainer **no**: choose generic versus vendor
scope and recovery before proposing a public helper.

**Acceptance:** independent S2K known-answer vectors, enable/change/disable with distinct old/new
credentials, same-session versus fresh-session F9 read and expected authentication, Reset Code invalidation,
state-dependent refusal and partial-failure recovery. Do not label the existing equal-old/new cached test
as that coverage. Dependencies: IMP-001/003/009/019 and qualified profile. Reopen DEP-002 Config/Pin/KDF,
configuration/recovery guide and Advanced test explanations.

### PGP-IMP-003 — KDF aliases and independent secret copies

GetKdf returns the cached instance; SetKdf retains its argument; disposal clears the current object's
arrays. A caller can therefore clear a still-used alias, while replacement/reset drops old references
without cleanup. Initial PIN hashes also survive in separate F9 encoding, parser-field and response
copies. Clearing the KDF or final transport response does not reach all of those owners.

**BEFORE — `src/OpenPgp/src/OpenPgpSession.Config.cs`, `SetKdfAsync`, lines 201–206:**

```csharp
await PutDataAsync(DataObject.Kdf, kdf.ToBytes(), cancellationToken)
    .ConfigureAwait(false);

// Retain the supplied instance: later PIN operations derive with this same object.
// Replacement currently drops the old reference without disposing it.
_kdf = kdf;
```

**BEFORE — `src/OpenPgp/src/Kdf.cs`, `KdfIterSaltedS2k.ToBytes`, line 326:**

```csharp
return TlvHelper.EncodeList([.. tlvs]).ToArray();
```

`Kdf.Parse:81–94` allocates dictionary fields; `ParseData:350–359` clones salts/hashes again.
`OpenPgpSession.GetOrLoadKdfAsync:390–392` loses the F9 response owner after obtaining Data;
`ResetAsync:71–72` drops the cached reference. These are distinct applet copies, not one buffer.

**BEFORE — `src/OpenPgp/src/OpenPgpSession.cs`, `GetOrLoadKdfAsync`, lines 394–398:**

```csharp
catch (ApduException)
{
    // Any F9 APDU error currently falls back to KdfNone, including errors beyond absence.
    _kdf = new KdfNone();
}
```

That fallback can cache raw-PIN handling after an unrelated F9 failure. Choose known-absence fallback
versus explicit compatibility behavior; neither an error nor an unrecognized KDF identifies a safely
disabled KDF. Keep the original status and avoid spending retries with an unjustified credential form.

**Proposed AFTER — schematic ownership alternatives:**

```text
Choose borrow, transfer, or session-owned clone for SetKdf; define GetKdf's matching lifetime.
Borrow: caller retains disposal responsibility; session never disposes a borrowed object.
Transfer/clone: session disposes exclusively owned replaced/reset/current values after last use.
Own each F9 encoding, parser field copy and response through success/failure; clear authentication data.
Invalidate/reload KDF after supported raw F9 writes; define unknown external/raw mutation recovery.
Classify F9 absence, unrelated APDU failure and unknown KDF separately before choosing raw-PIN handling.
Do not change caller-owned inputs or clear an alias still needed by an admitted public workflow.
```

**Python:** `py:yubikit/openpgp.py:1165–1192`, `get_kdf` rereads and `set_kdf` writes without retaining
the supplied object. Useful cache comparator; Python bytes provide no .NET disposal/erasure contract.
No-brainer **no** for ownership design; retaining and finally-clearing an exclusively owned encoding is
mechanical once ownership is selected.

**Acceptance/dependencies:** meaningful held-derivation/last-use cases, caller disposal, replacement,
reset and raw-write reload, throwing custom KDF and failed async connection teardown. Retain actual
encoding/parser/response arrays to establish independent-copy clearing on both success and failure.
Cover known F9 absence, unrelated status failure and unknown KDF with the selected next-VERIFY policy;
an unjustified raw-PIN fallback must not silently spend retries.
Core C-C005/C006 and B6 response/TLV/SCP producers stay their canonical owners; do not claim end-to-end
erasure from applet tests. CORE-PGP-X001/X003 and PGP-CORE-002 map here and to workflow below.
The **async-dispatch question alone is answered**: `ApplicationSession.DisposeSessionAsync:478–488`
calls derived disposal in finally after async teardown. Sync KDF clearing precedes base drain;
neither order leases the whole workflow. Reopen DEP-003 Kdf/Get/Set/Reset/PutData contracts and examples.

### PGP-IMP-005 — Reset status, postcondition and channel state

Reset spends invalid PIN attempts, ignores TERMINATE/ACTIVATE statuses, then logs completion after
selection/read. A refused reset could therefore be mistaken for ordinary-key/certificate erasure.
BlockPin stops only on 6983 and otherwise assumes exhausting the original count was sufficient.

**BEFORE — `src/OpenPgp/src/OpenPgpSession.Reset.cs`, `ResetAsync`, lines 49–55:**

```csharp
var terminateCmd = new ApduCommand(0x00, (int)Ins.Terminate, 0x00, 0x00);
await TransmitNoThrowAsync(terminateCmd, cancellationToken).ConfigureAwait(false);

// Step 5: ACTIVATE
_logger.LogDebug("Sending ACTIVATE");
var activateCmd = new ApduCommand(0x00, (int)Ins.Activate, 0x00, 0x00);
await TransmitNoThrowAsync(activateCmd, cancellationToken).ConfigureAwait(false);
```

**Proposed AFTER — schematic, choose reset lifecycle before implementation:**

```text
Classify blocking responses; stop on unrelated failure rather than declare exhausted attempts.
Check TERMINATE and ACTIVATE outcomes and retain the phase/status on failure.
Option A: make reset terminal; fresh creation and independent state readback follow.
Option B: support explicit reinitialization with defined cache/readiness/channel restoration.
Do not publish complete reset based only on SELECT/read success; define verifiable postconditions.
```

**Python:** `py:yubikit/openpgp.py:1110–1133`, `reset`, checks E6/44 through throwing send, but swallows
blocking APDU errors and verifies no complete postcondition. Its blanket erasure docstring is not ATT
authority. No-brainer **no** for the compound group; status checking is a settled local correction,
but accepted statuses, recovery and terminality require review.

**Acceptance:** fake refusal at blocking/E6/44/reselect/refresh, check no false completion, retry-safe
phase provenance and selected next-call behavior. Existing SCP reset retains the processor graph and
keys; `PcscProtocolScp.SelectAsync:98–106` sends plain SELECT, not a handshake. Effective version and
authentication/readiness remain until teardown; OpenPGP retains no reset SCP parameters. The helper's
separate plain reset followed by optional SCP body session is **not** an SCP-reset oracle.
Dependencies: IMP-003, B5-C006 and B6-C003/C015. Reopen DEP-004/005 reset/UIF/tool/helper guidance.
Successful ordinary reset erases ordinary material, **not ATT key/certificate or fixed ATT policy**.
Fixed ATT recovery requires irreversible private-key replacement; a saved certificate cannot restore
that private key or manufacturer trust. No such replacement is authorized for qualification.

### PGP-IMP-007 — X25519 decipher framing

The formatter bypasses the EC cipher DO wrapper for X25519. Historical SDK Alice-key import/public
point matching succeeded, then Decipher failed 6A80; the independent Python client used that same key
before reset/import and derived the exact RFC 7748 Alice/Bob shared secret. Import evidence alone is
not an agreement oracle. Medium reflects unavailable functionality, not demonstrated secret disclosure.

**BEFORE — `src/OpenPgp/src/OpenPgpSession.Crypto.cs`, `FormatDecryptPayload`, lines 259–261:**

```csharp
RsaAttributes => FormatRsaDecryptPayload(ciphertext),
EcAttributes { KeyType: KeyType.X25519 } => ciphertext.ToArray(),
EcAttributes => FormatEcDecryptPayload(ciphertext),
```

**Proposed AFTER — literal C#, use the existing common EC wrapper:**

```csharp
RsaAttributes => FormatRsaDecryptPayload(ciphertext),
EcAttributes => FormatEcDecryptPayload(ciphertext),
```

**Python:** `py:yubikit/openpgp.py:1736–1768`, `decrypt`, serializes X25519 as raw public bytes and wraps
every EC input in A6 {7F49 {86 point}}. For a 32-byte point the data prefix is `A6 25 7F 49 22 86 20`.
No-brainer **yes** for framing; separate input admission and profile choices remain with IMP-010/018.

**Acceptance:** exact 32-byte RFC point/wrapper/APDU vector, independent shared-secret equality, unchanged
NIST uncompressed-point and RSA `00 || ciphertext` paths, completion/failure cleanup of actual raw
response and returned caller-owned secret. Preserve X25519 import scalar reversal and key consistency
checks. This is raw decipher/agreement, not file/packet/content decryption. Reopen DEP-007 formats,
Decrypt XML/README and agreement tests; Core producer-copy qualification stays separate.

### PGP-IMP-009 — Partial key mutation and stale cache

Attributes can destroy/render unusable an existing key before generate/import succeeds. Delete writes
two different attribute values. Refresh can then fail after private-key mutation succeeded. Direct
config/raw writes leave `_appData` stale while crypto formats from cached attributes; no rollback exists.

**BEFORE — `src/OpenPgp/src/OpenPgpSession.Keys.cs`, `GenerateKeyAsync`, lines 42–45:**

```csharp
await SetAlgorithmAttributesAsync(keyRef, attributes, cancellationToken)
    .ConfigureAwait(false);

await GenerateKeyPairAsync(keyRef, cancellationToken).ConfigureAwait(false);
```

**Proposed AFTER — schematic alternatives:**

```text
Prevalidate selected slot/attributes/import before the first destructive write.
Choose immediate readback/cache update OR mark affected cached state invalid until fresh inspection.
Distinguish attribute-write, key-operation and refresh phases when reporting partial failure.
Define raw config/F9 invalidation; never promise transaction rollback the card does not supply.
Use the selected whole-workflow admission policy, not a lock around individual APDUs only.
```

**Python:** `py:yubikit/openpgp.py:1408–1429`, `set_algorithm_attributes`, checks supported requests;
`generate_rsa_key:1538–1556`/`put_key:1591–1605` still perform separate writes. `get_algorithm_attributes:1340–1347`
reads current app data; constructor `1014–1016` expressly warns its other cache can change. No atomicity
or rollback precedent. No-brainer **no**: select failure representation/cache policy, not speculative APIs.

**Acceptance:** fail/cancel at each persistent write and refresh, assert actual command prefix, cache
admission and fresh-session inspection without automatic destructive retry. Cover generate/import/delete,
direct attributes, raw writes and certificate occurrence selection. Dependencies: IMP-002/003/010/014,
B5-C006 and B6 recovery. Reopen DEP-009 key/config/raw contracts and mutation guidance.

### PGP-IMP-010 — Capability and family admission

The SDK's firmware constants do not supply complete card/slot capabilities. Cached/ATT UIF lacks its
subgate, retry changes lack family thresholds and NEO default-PIN effects, challenge lacks advertised
support/length enforcement, and algorithm discovery/workarounds have narrower/different behavior from
Python. Raw/config admission must be deliberately distinguished from the supported high-level path.

**BEFORE — `src/OpenPgp/src/OpenPgpSession.Config.cs`, `SetUifAsync`, lines 53–60:**

```csharp
ThrowIfDisposed();

EnsureSupports(FeatureUif);

_logger.LogInformation("Setting UIF for {Slot} to {Uif}", keyRef, uif);

await PutDataAsync(keyRef.UifDo(), uif.ToBytes(), cancellationToken)
    .ConfigureAwait(false);
```

**Proposed AFTER — schematic, support contract required:**

```text
For high-level operations, select family/effective-version/card/slot checks before mutation.
Check ATT/cached UIF, retry capability and side effects, challenge maximum and algorithm requests.
Choose old-device synthesized discovery OR explicit unsupported discovery; identify synthesized entries.
Bound malformed-FA padding workaround to accepted profiles; preserve valid repeated slot entries.
Keep raw access explicitly raw rather than implying high-level checks protect every APDU.
```

**Python:** `py:yubikit/openpgp.py:1048–1060`, `get_challenge`, checks capability/maximum;
`set_pin_attempts:1135–1162` gates NEO 1.0.7 versus other 4.3.1 and warns of PIN reset;
`set_uif:1443–1469` gates ATT/cached at 5.2.1; `get_algorithm_information:1349–1406` synthesizes old RSA
lists and repairs old 25519 entries. Its list operations are not a requirement to synthesize missing
entries or drop multiplicity in C#. No-brainer **no** for selected support/forward-compatibility policy.

**Acceptance:** meaningful wire/non-transmission cases for advertised support/bounds and valid repeated
entries; distinguish malformed FA from profile-specific workaround; qualify oldest claimed families,
slots and actual effects independently. SDK EC/certificate/attestation 5.2.0 checks, vendor commercial
5.2.3 wording and Python 5.2.0/5.2.1 gates retain different provenance. Dependencies: CORE-003, IMP-009/014/018,
shared framing/profile below. Reopen DEP-010 requirements/Feature/UIF/retries/challenge/discovery surfaces.

### PGP-IMP-011 — ROCA generation and test applicability

Shipping generation checks EC support but has no RSA ROCA guard before destructive attribute writes.
YSA-2017-01 identifies generated RSA on 4.2.6–4.3.4 as affected, with 4.3.5 corrected and externally
generated/imported keys a mitigation. Four integration methods instead return normally for
4.2.0–4.3.5 inclusive, including import cases: runner success there executes no body assertions.

**BEFORE — `src/OpenPgp/tests/Yubico.YubiKit.OpenPgp.IntegrationTests/OpenPgpSessionTests.cs`,
`GenerateRsaKey_2048_Succeeds`, lines 216–220:**

```csharp
if (state.FirmwareVersion >= new FirmwareVersion(4, 2, 0) &&
    state.FirmwareVersion <= new FirmwareVersion(4, 3, 5))
{
    return;
}
```

**Proposed AFTER — schematic policy choices:**

```text
Choose advisory-exact generation restriction OR explicitly justified conservative vendor range.
Apply the selected guard before any generation-related destructive attribute write.
Allow independently generated imports where the mitigation/profile applies; do not reuse generation gate.
Report unsupported applicability honestly, never normal return as a passed omitted body.
Exercise corrected 4.3.5 and unaffected/import cases with meaningful crypto verification.
```

**Python:** `py:yubikit/openpgp.py:1525–1556`, `generate_rsa_key`, rejects `[4.2.0,4.3.5)` before
attribute/generation writes; `put_key:1581–1605` has no ROCA-generation exclusion. That conservative
start differs from the advisory's exact affected range. No-brainer **no** for range/development override
policy; excluding corrected 4.3.5 and imported mitigation cases is plainly unsupported by the advisory.

**Acceptance:** guard boundaries, no attribute mutation on rejected generation, imported-key signature/
decipher verification and honest executed/unsupported reporting in all four original test locations
(Session generation/sign, KeyImport and Decrypt). Do not deliberately generate vulnerable keys for
coverage. Dependencies: effective physical/version policy and IMP-016. Reopen DEP-011 security rationale,
RSA generation/import requirements and exact test execution comments.

### PGP-IMP-013 — Nested ATT control reference template

ATT CRT encodes only the inner value instead of its tag/length/value. Historical read-existing ATT
failed 6A80; a public-only correct-CRT query matched the issuer SPKI without replacing private material.
Target Sig/Dec/Aut FB attestation is a separate operation and is not fixed by this encoder change.

**BEFORE — `src/OpenPgp/src/Crt.cs`, `BuildAttCrt`, lines 57–59:**

```csharp
using var inner = new Tlv(0x84, [0x81]);
using var outer = new Tlv(0xB6, inner.Value.Span);
return outer.AsSpan().ToArray();
```

**Proposed AFTER — literal C#, settled nested TLV correction:**

```csharp
using var inner = new Tlv(0x84, [0x81]);
using var outer = new Tlv(0xB6, inner.AsSpan());
return outer.AsSpan().ToArray();
```

**Python:** `py:yubikit/openpgp.py:360–367`, `CRT.ATT`, nests `Tlv(0x84, b"\x81")` under B6;
`get_public_key:1511–1518` uses that CRT. Expected bytes are `B6 03 84 01 81`, not `B6 01 81`.
No-brainer **yes** for encoding; not blanket authorization/support for ATT generate/import.

**Acceptance:** exact ATT read/generate/import request vectors and unchanged ordinary CRTs; use only
public read-existing comparison for a later physical confirmation. Preserve static CRT memory as
borrowed read-only data. Do not replace ATT private material merely to test serialization. Dependencies:
IMP-010 capability and independent public identity. Reopen DEP-013 Crt/KeyRef/key limits and wire tests.

### PGP-IMP-014 — NEO import, generation and deletion

Major 1–3 RSA import labels the template CRT 02 but includes modulus N; the Card distinguishes this
from CRT-with-modulus 03. Generation always writes supplied attributes; deletion always switches
attributes, while the vendor NEO path overwrites with an RSA2048 key. Source mismatch establishes no
particular NEO's tolerance or safe deletion result.

**BEFORE — `src/OpenPgp/src/PrivateKeyImport.cs`, `GetAttributes`, lines 103–105:**

```csharp
return RsaAttributes.Create(
    rsaSize,
    firmwareVersion.Major is > 0 and < 4 ? RsaImportFormat.Crt : RsaImportFormat.Standard);
```

`RsaTemplate:139–144` also supplies the modulus; `DeleteKeyAsync` writes RSA4096 then RSA2048.

**Proposed AFTER — schematic, old-family scope must be selected:**

```text
Encode CRT-with-modulus 03 when the selected NEO template includes N.
Choose supported NEO generation attributes/capability route rather than universal standard defaults.
For NEO deletion choose qualified vendor overwrite semantics OR explicit unsupported deletion.
Do not call overwrite proof of an empty slot; document persistent replacement and metadata/cert scope.
```

**Python:** `py:yubikit/openpgp.py:883–897`, `_get_key_attributes`, selects CRT_W_MOD for major 1–3;
`generate_rsa_key:1538–1553` respects attribute-change capability and uses that format;
`delete_key:1608–1627` imports a generated RSA2048 replacement on NEO. No-brainer **no** for the group:
format consistency is mechanical, destructive semantics and supported family range are decisions.

**Acceptance:** exact component tags/widths/format and request sequence, unchanged modern route,
admission before mutation, host verification/readback on a separately authorized dedicated old-family
key. No modern serial-103 pass qualifies NEO. Dependencies: IMP-009/010/011 and Core key-provider/copy
owners. Reopen DEP-014 old-family import/generation/delete guidance.

### PGP-IMP-015 — Tool input, state and resource contracts

OpenPgpTool declares an attestation PIN option but calls Attest on a fresh session without using it.
Its info display equates nonempty fingerprint storage with key presence—even zero-filled reset
fingerprints have length. Command-line PIN/PEM strings persist; no prompt is installed; noninteractive
selection has no deterministic serial safeguard. Certificate import/export/attest results lack disposal.

**BEFORE — `src/OpenPgp/examples/OpenPgpTool/Cli/Commands/KeysAttestCommand.cs`,
`ExecuteCommandAsync`, lines 38–40:**

```csharp
var keyRef = ParseKeyRef(settings.Key);

var cert = await session.AttestKeyAsync(keyRef);
```

**BEFORE — `src/OpenPgp/examples/OpenPgpTool/Cli/Commands/InfoCommand.cs`,
`ExecuteCommandAsync`, line 79:**

```csharp
var status = fingerprint.Length > 0 ? "[green]Present[/]" : "[grey]Empty[/]";
```

**Proposed AFTER — schematic tool decisions, not an invented credential/prompt API:**

```text
Require deterministic intended-device selection and command-specific destructive confirmation.
Acquire/verify the User PIN before FB; choose supported secret-input and presence-prompt integration.
Display key-information status, including unknown/absent, independently of fingerprint bytes.
Dispose each loaded/returned certificate after actual last use, including early returns/output failures.
Correct reset/help claims to preserve ATT material; do not claim output proves trust or complete reset.
```

**Python:** `py:ykman/_cli/openpgp.py:529–569`, `attest`, prompts/uses PIN, checks existing target
certificate, confirms overwrite, reads ATT touch policy and verifies ordinary User before FB.
`py:yubikit/openpgp.py:1472–1475`, `get_key_information`, reads actual status. Python strings/certificates
are not secure-buffer/disposable equivalents. No-brainer **no** for input/selection/prompt policy;
disposing exclusively owned public certificates and separating status from fingerprints are local fixes.

**Acceptance:** command dispatch actually honors credentials, zero owned encoded/private-export arrays
after last use, no unsupported secret-string erasure claim, exact status/output and deterministic
selection under multiple devices. Exercise canceled acquisition, output failure and early return with
resource cleanup. Include certificate import/export/attest sample sites, not only the shown command.
**SAMPLE-A004**'s distinct CLI certificate caller `3ccf06e816f277d2` in
`src/Cli.Commands/src/OpenPgp/OpenPgpCertificatesCommands.cs` remains: export `74` has no disposal;
import `123–150` loads a certificate before PIN acquisition with no enclosing owner scope. Same
deterministic-certificate class as PIV, **not a permanent leak/native failure assertion or duplicate bug**.
Dependencies: reader card, CORE-004 callback owner, IMP-016 and shared CLI selection/credential owner.
Reopen DEP-004/012/015 tool comments/help/sample limitations and actual shared CLI output/PIN/import anchor.

### PGP-IMP-016 — Meaningful oracles and failure cleanup

Tests named valid signature often assert only nonempty/length output. Attestation shape/overwrite tests
do not establish issuer trust or certified-key binding; reset checks only three counters. The helper
resets before a body but leaves its state afterward, and temporary secret/private/certificate results
need actual last-use scopes. Those are not repaired by assertion-accurate comments.

**BEFORE — `src/OpenPgp/tests/Yubico.YubiKit.OpenPgp.IntegrationTests/OpenPgpSessionTests.cs`,
`Sign_EcP256_ProducesValidSignature`, lines 255–260:**

```csharp
var signature = await session.SignAsync(message, HashAlgorithmName.SHA256);
Assert.True(signature.Length > 0);

// Check that public-key data is readable; this test has no host signature verification.
var pubKeyData = await session.GetPublicKeyAsync(KeyRef.Sig);
Assert.True(pubKeyData.Length > 0);
```

**Proposed AFTER — schematic useful behavior tests:**

```text
Verify generated RSA/ECDSA/Ed25519 signatures against independent host keys and original messages.
Compare X25519 agreement with an independent secret oracle; do not reuse public-import tests.
Separate attestation issuer signature, target SPKI binding and manufacturer-root trust assertions.
Control Once/Always policy and both PW1 modes before isolating Unverify's effect/status.
Use independent failure-safe restoration/readback; clear owned results and dispose certificates.
```

**Python:** `py:yubikit/openpgp.py:1715–1768`, `sign`/`decrypt`, supplies the algorithm-dependent result
shape, not a test oracle. No Python test execution or independent KDF implementation is claimed here.
No-brainer **no** for choosing meaningful controls/restoration; cleanup of uniquely owned fixtures is
mechanical only after ownership/last-use is established.

**Acceptance:** retain existing useful host-verifying RSA/P256/P384 imports, RSA plaintext/P256 agreement
and literal independent KDF vectors. SHA512 length and libgcrypt-named SDK-only derivation establish
narrower facts. `UnverifyPin_ClearsVerificationState:79–87` leaves policy uncontrolled: Always could
consume prior authorization before Unverify; later unspecified APDU failure is not causality or proof
of both modes cleared. Preserve four-slot versus old three-slot applicability, pre-admission/custom
provider versus native cancellation distinctions, early-return reporting (IMP-011), and failure cleanup.
Select actual behavioral tests, not validation-only or skipped placeholders. Reopen DEP-016 test/helper
comments and qualification claims after corresponding assertions/results change. Physical closure is below.

### PGP-IMP-018 — Open values, credential format and generation time

Unknown signature policy becomes Once; credential length bytes include format bits literally.
Required algorithm/OID parsing can prevent initialization of a valid newer card; optional unknown
entries can disappear. A raw GetData escape hatch on an already-created typed session does not solve
typed initialization failure. Generation times use signed int even though the wire is unsigned
four-octet seconds since 1970 UTC and zero means unspecified (PGP-C022/PGP-REV-005).

**BEFORE — `src/OpenPgp/src/PwStatus.cs`, `Parse`, lines 109–112:**

```csharp
var policyByte = encoded[0];
var policy = Enum.IsDefined((PinPolicy)policyByte)
    ? (PinPolicy)policyByte
    : PinPolicy.Once;
```

**BEFORE — `src/OpenPgp/src/DiscretionaryDataObjects.cs`, `ParseGenerationTimes`, lines 166–170:**

```csharp
var timestamp = (encoded[offset] << 24)
                | (encoded[offset + 1] << 16)
                | (encoded[offset + 2] << 8)
                | encoded[offset + 3];
entries[KeySlots[i]] = timestamp;
```

**Proposed AFTER — schematic model choices:**

```text
Preserve valid unknown response identifiers/raw bytes without silently assigning Once or a known curve.
Keep malformed encodings, required security fields and request validation strict.
Separate credential length from format bits and apply the selected card profile.
Choose unsigned seconds with explicit zero sentinel OR a lossless raw carrier plus typed interpretation.
Define high-bit/range/sentinel request behavior; never interpret unsigned high bits as pre-1970 dates.
Keep algorithm/import-format parsing distinct from slot-selecting factories.
```

**Python:** `py:yubikit/openpgp.py:344–357`, `PwStatus.parse`, has the same Once/literal-length fallback,
so supplies no justification for preserving it. `_parse_timestamps:588–593` and
`set_generation_time:1482–1492` use the four-byte integer field/unsigned pack; required algorithm
parsing `614–625` is also strict. No-brainer **no**: response extensibility, request strictness and public
carrier/range policy require one coherent decision.

**Acceptance:** valid unknown versus malformed ID/OID/TLV cases, required initialization/raw-session
diagnostics, lossless unknown policy/format, repeated algorithm entries, timestamps `0`, `7FFFFFFF`,
`80000000`, `FFFFFFFF` and exact setter bytes. Include AID invalid-BCD fallback and factory validation,
not a silent P-256 substitution. Core concrete catalog and legacy OpenPGP OID value octets already
have distinct owners; new provider vectors do not prove card support. The 20-byte fingerprint contract
does not become a newer packet profile merely because S2K cites RFC 9580. Dependencies: CORE-001/003,
IMP-010 and Core open-value/provider owners. Reopen DEP-018 model/raw/factory tests and all five
generation-time semantic surfaces; corrected documentation itself is not an extra unresolved bug.

### PGP-IMP-019 — PIN status provenance and retry uncertainty

Every non-OK VERIFY becomes OpenPgpInvalidPinException, including unrelated command/secure-messaging
failures. Only 6982 queries C4; unknown counts can arise without a failed query. CHANGE/reset use generic
APDU errors instead. Misclassifying these as wrong PIN can encourage retries that spend actual attempts.

**BEFORE — `src/OpenPgp/src/OpenPgpSession.Pin.cs`, `VerifyPwAsync`, lines 257–261:**

```csharp
if (!response.IsOK())
{
    var remaining = await ResolveRemainingRetriesAsync(pw, response, cancellationToken)
        .ConfigureAwait(false);
    int? retriesRemaining = remaining >= 0 ? remaining : null;
```

**Proposed AFTER — schematic alternatives:**

```text
Option A: classify known PIN statuses; preserve other failures as APDU/secure-message provenance.
Option B: retain broad typed mapping but explicitly represent uncertain cause and retry count.
Select consistent CHANGE/Reset Code/admin-recovery status policy and exact qualified family exceptions.
Retain original SW/phase; distinguish explicit 63Cx, blocked 6983 and ambiguous 6982/status lookup.
Never automatically retry an ambiguous authentication or mutation failure.
```

**Python:** `py:yubikit/openpgp.py:1205–1218`, `_verify`, classifies selected statuses and otherwise
rethrows; `_change:1253–1273` and `reset_pin:1308–1337` have operation/family-dependent mappings.
It does not settle Card §7.9's 6982 wrong/unverified PW versus SM ambiguity. No-brainer **no** for API
classification; IMP-001's logical retry identity is already settled separately.

**Acceptance:** exact known/unrelated/SM statuses, preserved SW, correct credential count, failed and
canceled fallback, unknown count without query, supported User recovery rather than permanent-block
wording, and no unsafe automatic retry. Dependencies: IMP-001/002, Core backend errors and SCP state.
Reopen DEP-019 exception/Pin/XML/recovery/migration and wire test explanations.

### PGP-CORE-001 — Reader units and card profile

The inherited source question is answered: reader presets count UTF-16 code units before UTF-8;
supplied tool strings bypass the reader. Neither path nor byte-taking session methods generally
enforces the card's format/encoded-byte limits. Deciding the supported input boundary remains applet
policy, not an import of CTAP's NFC/code-point rules.

**BEFORE — `src/OpenPgp/src/Credentials/OpenPgpCredentialOptions.cs`, `ForOpenPgpPin`, lines 33–39:**

```csharp
public static CredentialReaderOptions ForOpenPgpPin() => new()
{
    Prompt = "Enter OpenPGP PIN: ",
    ConfirmPrompt = "Confirm OpenPGP PIN: ",
    MinLength = 6,
    MaxLength = 127
};
```

**Proposed AFTER — schematic alternatives:**

```text
Choose reader-only presets with explicit caller card-profile checks OR shared profile-aware acquisition.
Apply the selected encoded/derived byte boundary consistently to supplied and interactive input.
Keep PW1/PW3/Reset Code identity, minimum, format and KDF-derived widths separate from text-reader units.
```

**Python:** `py:yubikit/openpgp.py:1194–1203`, `_process_pin`, checks length after derivation using
PW-specific minimum and cached status maximum. Its literal C4 limit interpretation also needs profile
qualification. No-brainer **no**. Acceptance: multibyte UTF-8 and surrogate cases, supplied/interactive
consistency, correct KDF versus raw byte admission and no transmission of rejected profile input.
Dependencies: IMP-018/015, canonical Core C-C001–C004 redirected-string, confirmation, unit/overflow
and queued/redirected-cancellation questions (shared with CTAP-IMP-006); keep OpenPGP as its own profile owner. Reopen PIN/options/input
examples under PGP-CORE-001 and DEP-001/018, without another Core backlog sweep.

### PGP-CORE-003 — Version fallback and effective profile

GET VERSION accepts at least three successful bytes and BCD-decodes the first three. Every non-OK,
short OK or caught APDU exception falls back to 1.0.0; transport/other exceptions propagate. That can
hide unrelated failure, but 1.0.0 is not an observed minimum-supported device. Physical metadata is
not consulted; the override-or-detected value configures framing/gates after actual SELECT/discovery.

**BEFORE — `src/OpenPgp/src/Backend/OpenPgpBackend.cs`, `GetVersionAsync`, lines 86–93:**

```csharp
catch (ApduException)
{
    // APDU exceptions share the compatibility fallback below. Transport and other exceptions propagate.
}

// Every non-OK response and every successful response shorter than three bytes also reaches this fallback.
// It is an effective compatibility value, not evidence that the device runs version 1.0.0.
return new FirmwareVersion(1, 0, 0);
```

**Proposed AFTER — schematic options, not fabricated fallback APIs:**

```text
Choose narrow known-legacy-status fallback OR explicit broad compatibility with diagnosable provenance.
Define short/malformed/invalid-BCD response handling independently of legacy absence.
Retain detected applet, configured effective, physical metadata and card-AID specification versions.
Select development/override framing and SCP applicability explicitly; reset must not silently lose policy.
```

**Python:** `py:yubikit/openpgp.py:1019–1028`, `_read_version`, falls back only for conditions-not-satisfied
and applies its version override to successful decoding. Different ordering/policy, not a mandate to
use metadata as truth. No-brainer **no**. Current Core development branch uses connection extended
capability/Yk43 ceiling before ForceShortApdus; stable overrides affect gates, NEO format, certificate
SELECT DATA and algorithm-info correction. Major-zero compatibility is optimistic host policy.

**Acceptance:** explicit fallback statuses, short/extra/invalid BCD, overridden stable/development
framing/ForceShort behavior, readiness/failure cleanup and reset-retained effective version. Qualify
raw applet 0.0.1 versus metadata 5.8.0.beta.0 and CLI application 5.8.0 separately. Generic SCP03 5.3.0/
SCP11 5.7.2 gates establish no earliest OpenPGP profile; do not borrow OATH's 5.6.3 floor.
Dependencies: canonical CORE-A-C015/B5-C001/B6 framing and physical lane. Reopen PGP-CORE-003,
DEP-010/021 firmware/factory/requirements after selected policy; no development-device defect asserted.

### PGP-CORE-004 — Whole-workflow and notification result ownership

Core serializes one exchange, not certificate occurrence selection plus read/write, key mutation plus
refresh, KDF derivation plus VERIFY, two-mode Unverify or reset. Parsing/cache updates and callbacks
are outside that guard. Sync KDF disposal can precede admitted workflow last use; async exchange drain
does not cover the next APDU or callback. Callers must currently await/serialize entire public tasks.

**BEFORE — `src/Core/src/Utilities/ExchangeGuard.cs`, `RunAsync`, lines 53–62:**

```csharp
Enter(cancellationToken);

try
{
    return await exchange(CancellationToken.None).ConfigureAwait(false);
}
finally
{
    Exit();
}
```

**Proposed AFTER — schematic workflow policy, Core remains canonical mechanism owner:**

```text
Choose documented caller sequencing OR a complete-public-workflow lease covering all actual last use.
Define overlap, callback reentrancy/self-disposal and terminal drain; do not add an accidental nested deadlock.
For Attest, own the produced certificate until Completed resolution succeeds; dispose on abandonment.
Retain crypto resolution-before-transfer/copy and clearing of abandoned owned response storage.
```

`AttestKeyAsync:174–181` produces a certificate inside the generic notification callback before its
fallback Completed resolution: a throwing terminal callback can abandon that disposable public result.
Sign/Authenticate explicitly resolve before transferring raw RSA/EdDSA storage or DER output; Decrypt
resolves before copying and clears raw storage in finally. Those successful caller-owned outputs still
require caller cleanup of actual backing memory, not a new ToArray clone.

**Python:** `py:yubikit/openpgp.py:1629–1680`, certificate select/read/write, and `attest_key:1699–1713`
also span separate synchronous commands; it has no corresponding .NET async prompt/lease mechanism.
No-brainer **no** for workflow policy; certificate owner-until-transfer cleanup is mechanical once the
chosen callback/result contract is settled. Severity is Medium for lifetime/cross-command interference,
Low for delayed public-certificate release; no permanent leak or trust validation is asserted.

**Acceptance:** held work between exchanges, concurrent call/disposal, throwing terminal callback after
certificate production, actual successful-output/abandoned-array cleanup and selected reentrancy behavior.
Admitted SmartCard passes None: outer cancellation/wait completion is not native completion, safe input
clear/retry or physical release. **SAMPLE-PGP-001**, units `8b2a95f01db9bf2c`/`710814862611f595`, also
remains: `GetUserPresenceContextAsync:363–368` logs before setting PolicyMayRequire; a logger throw
prevents advisory fallback/crypto dispatch. Its accepted `9d543310…` patch is queued/untransferred;
corrected prose is not a runtime fix. Choose nonthrowing advisory diagnostics or explicit propagation,
then assert canceled/read-failure/logging-failure outcomes without erasing primary provenance.
Dependencies: B5-C006, C-C008 notification/logging, C-C020 parameter/prompt containment and B6 response/recovery. Reopen PGP-CORE-002/004,
DEP-008/012 and interface/crypto/certificate/presence/resource guidance. Native timing qualification below.

### OpenPGP shared dependency and boundary ledger

Original Core boundary groups map nonadditively; accepted documentation is excluded from unresolved
runtime counts. The frozen [candidate reconciliation](modules/core/pass-1/core-001/triage/final/candidate-reconciliation.json),
[inherited reconciliation](modules/core/pass-1/core-001/triage/final/inherited-reconciliation.json) and
[continuity map](modules/core/pass-1/core-001/triage/final/reconciliation-4/deferred-finding-continuity.json)
retain their original states and exact reopening paths. Historical PENDING editor text does not override
the published handoff's revision-2 PASS/integration; separate queued sampling patches remain queued.

| Original boundary / inherited question | Canonical remaining card or exclusion |
| --- | --- |
| CORE-PGP-X001; PGP-CORE-002 | IMP-003 and CORE-004; factory borrow/hidden-own and derived async dispatch source questions answered, not missing-dispatch bugs; KDF aliases/last-use/SCP qualification open |
| CORE-PGP-X002; PGP-CORE-001 | IMP-001/002/018/019 and CORE-001; reader/Card source answer complete; input/profile policies stay open |
| CORE-PGP-X003 | IMP-003 applet F9 copies; Core C-C005/C006 TLV and B6-C002/C007 response producers are dependencies, not duplicate bugs; absent-Le/profile qualification remains |
| CORE-PGP-X004; PGP-CORE-004 | CORE-004 workflow/certificate callback/result ownership and SAMPLE-PGP-001 advisory logging; physical/native causality open |
| CORE-PGP-X005 | IMP-005; B6-C003 retained graph/plain reselect and B6-C015 SCP profile/task qualification; no SCP reset acceptance |
| CORE-PGP-X006; PGP-CORE-003 | CORE-003 version policy and IMP-010/014 qualification; broad fallback prose correction resolved, runtime fallback unchanged |
| CORE-PGP-X007 | IMP-007/010/011/013/014/018; incomplete PutKey representation-list prose resolved in integrated boundary; Core catalog/provider changes establish no extra slot/card support |
| CORE-PGP-X008 | IMP-015/016 plus SAMPLE-A004 distinct CLI caller; assertion-accurate prose resolved, resource/oracle/applicability and physical work open |

| Needed shared mechanism, canonical owner | OpenPGP applicability and closure boundary |
| --- | --- |
| CORE-C-C001–CORE-C-C004; [CTAP input card](#ctap-imp-006--pin-units-and-input-enforcement) | Redirected-string policy, retained confirmation ownership, reader units/overflow and queued/redirected cancellation; CORE-001 owns OpenPGP profile, not CTAP normalization; resolved confirmation bug stays excluded |
| CORE-C-C005/CORE-C-C006; CORE-B6-C002 | TLV, transport-result/ApduResponse, fragment and protected/plaintext SCP copies at PIN/F9/import/decipher; require retained actual-source-copy evidence, not final-buffer erasure |
| CORE-B5-C006; CORE-C-C008 | Whole workflow, callback/logging and prompt lifetime; link [existing shared work](#oath-shared-dependencies), do not duplicate other-applet policy |
| CORE-C-C020/CORE-B6-C007 | Session creation borrows SCP parameters/prompt; SCP03 parameters retain/dispose StaticKeys that clone source arrays; SCP11 retains/disposes private key/certificates while public key remains borrowed. Record clones share contained owners. Keep sources/parameters alive outside session lifetime and prove last-use/disposal separately; no applet alias closure from Core prose |
| CORE-A-C015/CORE-B5-C001; CORE-B6-C001/CORE-B6-C003/CORE-B6-C015 | Effective-version/framing/absent-Le, reselect/recovery and SCP profile distinctions. Card 3.4.1 VERIFY/CHANGE tables specify absent Le; exact ISO case/corrected-length clauses and device tolerance remain separate |
| Windows successor CORE-B1-C006/CORE-B1-C009, CORE-B4-C005/CORE-B4-C006/CORE-B4-C009 | Only adjacent PC/SC transaction/log-after-open/loader/ABI-string/error dependencies if selected OpenPGP closure needs them; renamed PC/SC owners are not repairs or native qualification |

The [Windows successor](integrations/windows-upstream-20261113/deferred-findings-successor.md)
supersedes old built-in Windows **HID** report mechanisms (B4-C001/C007), not OpenPGP's SmartCard route.
Its managed owner/software acceptance closes none of OpenPGP's copies, workflow, SCP, certificate,
PIN/key defects or physical lanes. Installed PC/SC producer, native completion/release, other OS and
JIT/AOT claims need source-bound evidence; no HID-driver backlog is imported here.

### OpenPGP documentation dependency and exclusion ledger

Each row preserves the original definition and exact path/reopening map in
[documentation-dependencies.json](modules/core/pass-1/core-001/boundaries/OpenPgp/documentation-dependencies.json).
These **21 labels have zero additional Core bug count**. A resolved prose facet retains its future
reopening trigger; executable tool strings and qualified evidence gaps are not silently excluded.

| Original label | Disposition / remaining owner and reopening scope |
| --- | --- |
| PGP-DEP-001 | IMP-001/019; PIN/Pw/KDF/retry/README/migration after identity/status changes |
| PGP-DEP-002 | IMP-002; persistent transition Config/Pin/KDF/Advanced/recovery contracts |
| PGP-DEP-003 | IMP-003; cache alias, serialization/parse, Get/Set/Reset/PutData/disposal ownership |
| PGP-DEP-004 | ATT reset/fixed-UIF prose resolved; IMP-005/015 executable reset overstatement and qualified ATT preservation remain |
| PGP-DEP-005 | IMP-005; reset phase/status/postcondition/helper/tool guidance |
| PGP-DEP-006 | Algorithm-dependent signing/Authenticate input/output prose resolved; reopen crypto/README for actual format/prehash changes, no extra engineering defect |
| PGP-DEP-007 | IMP-007; raw decipher/agreement framing/results and independent test oracle |
| PGP-DEP-008 | Transfer/clearing prose resolved; CORE-004 and Core producer-copy dependency reopen actual result/prompt/owner changes |
| PGP-DEP-009 | IMP-009; destructive mutation/config/raw/cache/refresh contracts |
| PGP-DEP-010 | IMP-010 and CORE-003; thresholds, capabilities, old-family discovery and override/framing |
| PGP-DEP-011 | IMP-011/016; ROCA rationale, generation/import distinction and honest executed-test range |
| PGP-DEP-012 | Attestation scope prose resolved; CORE-004 certificate abandonment and qualification of PW mode, overwrite, issuer/binding/root trust remain |
| PGP-DEP-013 | IMP-013; ATT CRT/KeyRef/read/provisioning limits, not target FB attestation |
| PGP-DEP-014 | IMP-014; NEO template/generation/delete and dedicated old-family evidence |
| PGP-DEP-015 | IMP-015; tool input/dispatch/status/prompt/selection/certificate/string/private-export ownership |
| PGP-DEP-016 | IMP-016; actual assertions, causal controls, fixture/result/final-state cleanup and applicability |
| PGP-DEP-017 | Task/API/package documentation completed and examples historically compiled; reopen relevant tasks for chosen deferred contracts, no missing-task bug or new compile credit |
| PGP-DEP-018 | IMP-018/CORE-001; model/raw/factory/format/fingerprint/AID/seed/OID distinctions; C022 timestamp extends same group |
| PGP-DEP-019 | IMP-019; typed-versus-APDU provenance, uncertain retries and recovery |
| PGP-DEP-020 | Shared Admin-PIN/SCP wording correction resolved; assigned shared claims reopen after behavior changes; chronology qualification below; DOC-002 parser remains original external owner |
| PGP-DEP-021 | CORE-003 and physical lane; raw applet/metadata/CLI/override provenance, not established development-device defect |

No PGP-IMP-004/006/008/012/017/020/021 engineering IDs are invented for documentation-only candidates.
PGP-REV-001–005 corrections and C022 timestamp prose are resolved documentation, not new engineering
groups. There is **no retracted OpenPGP engineering group** in this authority. Resolved OATH IMPs,
descriptor/User XREFs and retracted HSM retry-message claim stay excluded, while other applets' own
protected key/tool/reset/physical deferrals stay with those owners.

### OpenPGP qualification and restoration

**Qualification, no assigned defect severity; no-brainer: no. BEFORE/AFTER code: N/A**—the missing
work is identified device/profile/native/chronology evidence, not a code edit. The procedure below is
proposed and unexecuted here; no hardware, build/test or historical archive rewrite is authorized.

The [hardware results](modules/openpgp/pass-1/openpgp-001/hardware/results.md) preserve serial **103**,
Linux/Omarchy USB SmartCard, metadata **5.8.0.beta.0**, direct SDK/Python **0.0.1**, CLI application
**5.8.0** and card **3.4** as distinct observations. All 49 existing methods executed/passed there;
the initial 120-second run stopped after 24 reported passes and is **interrupted**, not a suite pass.
Supplemental history retains F9 setup refusal after key generation; reordered provisioning successful
controls followed by failed extended-PIN intent; wrong retry identity; first failed sign/decrypt touch
ceremonies; failed extended-only FB attestation; successful ordinary-mode attestation, same-key X25519
and public-only correct-CRT controls. Nine distinct .NET supplemental methods retain five successful
measured outcomes and four intended-contract failures, across 13 invocations. None becomes passed
because later documentation or a different control passed.

Cancellation scheduled 1500 ms after setup yielded request at 2074 ms and terminal at 14811 ms with
**6600/Failed**; fresh-connection authentication/UIF-Off/host-verified signature recovery succeeded
before reset. This is not native admission/interruption, timing causality or universal timeout proof.
Issuer signature and generated-key SPKI binding passed for ordinary-mode attestation; manufacturer-root
chain trust remains separately assessed. Extended-only failure is scoped evidence, not a blanket
exclusive-mode rule for every device. FB overwrites the target certificate before retrieval; a later
read/callback failure can leave that persistent overwrite even without returned output.

**Python precedent:** pinned `py:yubikit/openpgp.py:1736–1768` and `CRT.ATT:367` corroborate exact
agreement/CRT wire; `py:ykman/_cli/openpgp.py:543–561` corroborates ordinary PIN, overwrite confirmation
and ATT touch policy. These current source reads are not reruns of the separately installed historical
Python comparisons or proof of their binary identity. Python immutable response buffers have no
secure-erasure guarantee; no private/shared-secret bytes are logged to establish qualification.

**Proposed acceptance procedure:**

1. Obtain explicit authorization for each dedicated device, mutation and physical ceremony. Record exact
   SDK/source/artifact, installed Python revision/setup, OS/native PC/SC producer, transport, firmware
   observations, profile, override and initial public state before selecting tests. Existing historical
   evidence does not authorize another reset, key import or touch attempt.
2. Apply selected cards and meaningful unit/command behavior tests first in their later authorized lane.
   On device, separate raw/SCP creation, KDF enable/change/disable, partial reset/key mutation, controlled
   PW1 modes/UIF, host signature/agreement, issuer/binding and chain-trust oracles. Keep no-brainer wire
   correctness distinct from new minimum family/firmware claims.
3. Qualify NFC, older firmware/families, other OS, JIT versus Native AOT and actual native completion/
   release only where required for the accepted support claim. Explicitly retain blocked/unreached/
   skipped/failed dimensions; custom canceled tasks or managed seams establish no native interruption.
   Retained SCP graph/plain reselect requires its own reset/recovery oracle, not the plain-reset helper.
4. In failure-safe independent cleanup, inspect keys/certificates, counters, credential usability, KDF,
   algorithms/metadata and **all four UIFs** from fresh state. Historical final readback matched initial
   ordinary slots empty, retries **3/0/3**, KDF disabled, Once/counter zero, configurations and ATT
   certificate DER SHA256 `94afb703099edb4aaaee443c6e197c379f7f909d23f717f0868ef73057e4a16a`.
   ATT On survived reset and was separately restored to Off. Preserve ATT certificate identity and
   private material; the existing ATT private key was never replaced. Public backup cannot restore
   erased private key/manufacturer provenance. Record actual changed state and recovery failure, not
   a generic cleanup-success assertion.
5. For DEP-020 chronology, compare identified v1 source/release evidence against first/new-in-v2 wording
   and migration-map history; record the scoped conclusion without guessing or reopening unrelated
   DOC-002 parser work. Reopen only assigned shared history/usage surfaces after evidence acceptance.

### OpenPGP author verification and stopping point

Coverage is **18 review cards**, not 18 independent bugs: **14 original engineering groups**, three
remaining Core/profile/workflow cards and one physical/chronology qualification card. All four PGP-CORE
questions, eight CORE-PGP-X groups and 21 PGP-DEP labels are mapped. SAMPLE-PGP-001 logging and
SAMPLE-A004's independent OpenPGP CLI certificate anchor retain explicit owners/scopes. Shared Core
occurrences are dependencies, not additional findings; resolved prose and intentional gaps are excluded.

Static verification checks selected BEFORE lines against both current working source and the named
baseline, Python pin/path/symbol windows, local links/anchors, fences, full original-ID coverage and
the allowed edit boundaries. These checks provide no compilation, SDK-test, native/device or independent
human-review credit. At author handoff, other applet bodies were preserved; changes were limited to
OpenPGP, its overview row and dispatch label. No fixes, build/tests,
hardware/native/crypto execution, frozen-record edits, commits, pushes or subagents occurred.

## PIV

### Context, authority and review map

PIV keeps device private keys and certificates in separate numbered storage locations. The session
owns PIV workflows; Core supplies SmartCard exchanges, secure-channel machinery and connection
lifetime. The sample tool adds message framing, credential acquisition, certificate generation and
output handling. A successful raw private-key operation does not establish that the tool signed the
message the user supplied, and closing one managed session does not establish physical PIN isolation.

This section covers **ten original engineering/qualification groups**, split into **14 original-group
review cards**, plus a distinct SAMPLE-A004 resource card, one shared-workflow decision card and one
physical/platform qualification card (**17 cards total**).
Three PIVCORE questions and six Core boundary groups are nonadditive referrals. All remain unreviewed;
no AFTER below is implemented or compiled. Review the signing card first if a quick, confirmed defect
is wanted; decide reset, coherence, result ownership and interoperability intent before their repairs.

Authority is the published [PIV handoff](piv.md), frozen [findings](modules/piv/pass-1/piv-001/findings.md)
and [original definitions](modules/piv/pass-1/piv-001/investigation.md#separate-implementation-and-qualification-ledger),
then the [Core PIV boundary](modules/core/pass-1/core-001/boundary-Piv.md), its
[inherited outcomes](modules/core/pass-1/core-001/boundaries/Piv/inherited-outcomes.md), frozen
[candidate reconciliation](modules/core/pass-1/core-001/triage/final/candidate-reconciliation.json),
[inherited reconciliation](modules/core/pass-1/core-001/triage/final/inherited-reconciliation.json) and
[continuity](modules/core/pass-1/core-001/triage/final/reconciliation-4/deferred-finding-continuity.json).
The [PR745 integration](integrations/piv-pr745-20261105/integration.md) excludes already-repaired SDK
mechanisms. The later [Windows successor](integrations/windows-upstream-20261113/deferred-findings-successor.md#workflow-examples-and-protected-piv-scopes)
preserves response-only PIN-only tests, not an injected pool seam. The current baseline still uses
`ArrayPool<byte>.Shared` directly in `AuthenticationProtocol.AuthenticateAsync`; its `finally` clears
working slices before return. Independently owned response assertions remain valid. Reading retained
views after pool return is withdrawn, not an open defect or a replacement cleanup oracle.

All C# BEFORE windows were checked against **`6ce355468047e8004fd51cf1425808f6e8242ed3`**. Python identity
was freshly verified at **`4ca60f706af930459138d8dc0f0f953480e1c7a4`**, with empty `git status --short`, in
`/home/rupicola/Code/y/yubikey-manager`. Only the relevant PIV implementation windows below were
inspected. Python's immutable bytes, synchronous execution and third-party providers establish no
.NET zeroing, callback-drain or platform guarantees. The pinned NIST/RFC/vendor sources in the frozen
[source analysis](modules/piv/pass-1/piv-001/evidence/source-analysis.md) retain their original revision,
actor and profile conditions; this author made no new standards or hardware claim.

| Review group | Remaining work / canonical card |
| --- | --- |
| PIVIMP-001 | Bio reset preflight, product route and partial mutation |
| PIVIMP-002 | PIN-only transition/rotation coherence and legacy derived recovery |
| PIVIMP-003 | Firmware, product, FIPS, NFC and compatibility-policy qualification |
| PIVIMP-004 | Typed object generality and selected profile rules |
| PIVIMP-005 | Confirmed tool RSA/Ed25519 message framing; signing eligibility |
| PIVIMP-006 | Four cards: acquisition/source cleanup; plaintext/result ownership; certificate/menu tasks; callback/cancellation behavior |
| PIVIMP-007 | Two cards: crypto output admission and RSA provider lifetime; certificate parsing/decompression |
| PIVIMP-008 | Meaningful oracles, fixture floors, pending-task and independent restoration cleanup |
| PIVIMP-009 | ADMIN timestamp format and migration |
| PIVIMP-010 | Raw object-ID request range |

### Inspected Python PIV precedent

Every `py:` path is relative to the pinned repository above. These are actual source precedents,
including their limitations, not promises that the C# SDK already exposes equivalent APIs.

| Path / symbol / lines | What it establishes here |
| --- | --- |
| `py:yubikit/piv.py` · `PivSession.reset` · 736–777; `py:ykman/_cli/piv.py` · `reset` · 247–271 | SDK Bio-configured preflight precedes retry blocking; CLI separately checks `info.reset_blocked`. Neither is an atomic reset or F9-replacement authority. |
| `py:ykman/piv.py` · `pivman_set_mgm_key`, `pivman_change_pin`, `pivman_set_pin_attempts` · 314–414 | Manager-level coherence helpers supplement raw session commands. Writes remain sequential; derived rotation and best-effort protected cleanup are explicit. |
| `py:yubikit/piv.py` · `_do_check_key_support` · 600–629; `PivSession.get_serial` · 789–794 | Conservative ROCA exclusion begins at 4.2.0; explicit FIPS restrictions can supplement 4.4.x; serial uses 5.0.3. These differences require scoped policy/evidence, not blind gate copying. |
| `py:yubikit/piv.py` · `FascN`, `Chuid` · 424–543; `py:ykman/piv.py` · `generate_chuid`, `generate_ccc` · 501–532 | General CHUID field representation is separate from fixed provisioning defaults. CCC generation alone supplies no general CCC/KeyHistory parser precedent. |
| `py:yubikit/piv.py` · `_pad_message`, `_unpad_message` · 546–576; `PivSession.sign` · 1135–1160 | EC digest conversion, Curve25519 passthrough and RSA host padding are algorithm-specific. Python's X25519 passthrough is not evidence that X25519 can sign. Dummy RSA unpadding is provider precedent, not erasure proof. |
| `py:ykman/piv.py` · `check_key` · 449–498 | Independent RSA/ECDSA verification of a known message binds a public key to a private operation. This helper does not implement an Ed25519/X25519 oracle. |
| `py:ykman/piv.py` · `sign_certificate_builder`, `sign_csr_builder` · 692–776; `py:ykman/_cli/piv.py` · `generate_certificate_signing_request` · 1262–1283 | Certificate/CSR signing uses the actual TBS bytes; X25519 is excluded; CLI verifies PIN immediately before signing. This synchronous design supplies no .NET asynchronous callback lifetime contract. |
| `py:yubikit/piv.py` · `PivSession.get_certificate` · 1258–1276 | Unknown nonzero CertInfo is rejected rather than treated as ordinary DER. No decompressed-size ceiling is established by this inspected window. |
| `py:ykman/piv.py` · `PivmanData.__init__`, `get_bytes` · 209–214, 253–261 | Date tag 83 is read/written as fixed four-byte big-endian; the decode assigns `struct.unpack`'s tuple while encoding passes the property to `struct.pack`. Do not infer a verified Python timestamp round trip. |
| `py:yubikit/piv.py` · `PivSession.get_object`, `put_object` · 1214–1255 | Request IDs use `int2bytes`, not the C# branch/cast encoder. This window does not establish an accepted maximum ID; the C# range remains a decision. |

### PIVIMP-001 — Preflight reset before consuming retries

**High risk; source-confirmed missing preflight, product routing needs a decision. No-brainer: no.**
The SDK begins blocking PIN/PUK before checking whether this product can use applet reset. An eventual
reset error can leave credentials partly or fully blocked. A successful RESET can be followed by a
failed metadata refresh, so a failed task is not proof that reset did not apply. The impact is lost
access or incorrect recovery guidance, not destruction of F9 by normal PIV reset.

**BEFORE — `src/Piv/src/PivSession.cs` · `ResetAsync` · lines 189–196 (literal baseline).**

```csharp
        // TODO: Add Bio preflight before consuming retries; Bio MPE requires its device-reset route.

        // Step 1: Block PIN by verifying with empty PIN until blocked
        // Empty PIN encodes as 8 bytes of 0xFF per PIV spec
        await BlockPinAsync(cancellationToken).ConfigureAwait(false);

        // Step 2: Block PUK using RESET RETRY with empty credentials
        await BlockPukAsync(cancellationToken).ConfigureAwait(false);
```

**AFTER — decision-dependent schematic, unapplied and uncompiled.** Choose a PIV-local capability
preflight with a precise unsupported outcome, or a higher-level product router shared with Management
reset. A Bio metadata check alone must not be described as a complete product/reset-blocked policy.

```text
Before any blocking VERIFY/RESET RETRY:
  apply SELECTED_PRODUCT_PREFLIGHT; unavailable metadata is not arbitrary-error fallback
  unsupported applet reset -> direct caller to authorized PRODUCT_DEVICE_RESET, no retry mutation
Supported route -> retain block-PIN -> block-PUK -> RESET -> metadata-refresh order
Record command-applied versus refresh-failed outcome under SELECTED_ERROR_CONTRACT
```

Python: `PivSession.reset` and CLI `reset` in the register show these two separate preflight layers.
Dependencies: [MGMT-IMP-010](#mgmt-imp-010--combined-reset-product-and-lifecycle-contract), PIVIMP-002,
and the Core workflow referral below. Acceptance: fake the supported, unsupported, instruction-unavailable,
malformed and canceled preflight paths; assert no blocking APDU on refusal and exact mutation order on
success. Exercise post-RESET refresh failure without calling it rollback. Separately authorize Bio/product
qualification. Reopen `IPivSession.ResetAsync`, README Reset, CLAUDE reset gotcha and tool reset warnings
when route/outcome changes. **Normal reset retains F9; no replacement of attestation/private material.**

### PIVIMP-002 — Choose PIN-only coherence and recovery responsibility

**High risk; sequential writes confirmed, desired coherence is unsettled. No-brainer: no.** Protected
enable stores the already-active management key, blocks PUK and writes ADMIN in separate commands.
Disable restores the default key before deleting PRINTED then ADMIN. Raw PIN/key/retry changes leave
those recovery objects unchanged. A stale derived salt or stored key can break future recovery even
when a raw rotation succeeded. The settled active-key/witness/order fixes must remain intact.

**BEFORE — `src/Piv/src/PivSession.cs` · `EnablePinProtectedModeAsync` · lines 1091–1099 (literal baseline).**

```csharp
        await AuthenticateAsync(keyToProtect, cancellationToken).ConfigureAwait(false);
        await VerifyPinAsync(pin, cancellationToken).ConfigureAwait(false);
        await PinOnlyProtocol.StorePinProtectedManagementKeyAsync(_backend, keyToProtect, cancellationToken).ConfigureAwait(false);
        await AuthenticationProtocol.BlockPukAsync(_backend, cancellationToken).ConfigureAwait(false);

        var currentAdminRaw = await DataObjectProtocol.GetObjectAsync(_backend, DataObject.AdminData, cancellationToken)
            .ConfigureAwait(false);
        _ = AdminData.TryDecode(currentAdminRaw, out var currentAdmin);
        var updatedAdmin = AdminData.Create(pukBlocked: true, pinProtected: true, currentAdmin.Salt, currentAdmin.PinLastUpdated);
```

**AFTER — decision-dependent schematic, unapplied and uncompiled.** Option A keeps raw commands raw
and supplies explicit opt-in coherent workflows. Option B rejects or requires explicit coordination
when high-level rotation sees recovery metadata. Neither can promise an atomic card transaction.

```text
Inspect ADMIN plus authorized protected-object access; retain active-key escrow
Select RAW_CALLER_COORDINATED or OPT_IN_COHERENT_ROTATION
For legacy derived users: select PRESERVE_DERIVED or EXPLICIT_MIGRATION with recoverable old/new keys
Execute selected ordered writes; on each failure report what may already have applied
Reopen -> authenticate actual final key -> check selected ADMIN/PRINTED coherence
```

Python: manager `pivman_*` functions separate coherence from raw commands, but still permit partial
writes. Dependencies: PIVIMP-009 timestamp policy and `CORE-B5-C006` workflow lifetime. Acceptance:
inject failure/cancellation at every write boundary; verify reopen/recovery using the actual final
candidate and saved credentials, including mixed protected+stale-derived state. Preserve **6982-only**
candidate rejection/restoration, default-before-delete, and no traffic on unrelated errors. A no-candidate
`None` result must not invent authentication invalidation. Disable does not unblock PUK. Review PIN-only,
ChangePin/SetManagementKey/SetPinAttempts XML, README round trip and migration together; no forced legacy
derived erasure or undocumented recovery-object rewrite.

### PIVIMP-003 — Qualify firmware and product policy without guessing a new gate

**Qualification/policy; no assigned defect severity. No-brainer: no.** Feature predicates are not
product certification. C# serial/AES gates differ from Python serial and released-device AES origins;
4.4.x FIPS modeling does not express all newer FIPS/NFC/Bio constraints. Major-zero applet versions and
effective overrides must not be used to invent a physical firmware minimum or change reset defaults.

**BEFORE — `src/Piv/src/PivSession.cs` · `EnsureFipsKeyPolicySupported` · lines 990–992 (literal baseline).**

```csharp
        if (!FirmwareVersion.IsAlphaOrBeta && FirmwareVersion.IsAtLeast(4, 4, 0) && !FirmwareVersion.IsAtLeast(4, 5, 0)
            && (keyType == KeyType.Rsa1024 || pinPolicy == PinPolicy.Never))
            throw new NotSupportedException("RSA1024 and PIN policy Never are not supported on YubiKey 4 FIPS.");
```

`src/Piv/src/Features.cs` · `Serial`/`AesKey` lines 37/43 uses 5.0.0/5.4.0; `SupportsRsaGeneration`
lines 60–61 excludes 4.2.6–4.3.4. Python `get_serial` uses 5.0.3 and `_do_check_key_support` excludes
RSA generation from 4.2.0; the frozen vendor PIV advisory supports C#'s recorded 4.2.6 boundary.

**AFTER — code N/A until a policy is selected; qualification procedure, not implementation.**
Choose firmware-only compatibility plus device-enforced product errors, or explicit product-aware
preflight based on authoritative metadata. Keep detected FD, discovery/Management firmware and effective
override separately recorded. For each proposed gate change, identify the exact released/beta firmware,
product/FIPS mode, operation, algorithm/policy and USB/NFC route; compare accepted source revisions and
actual outcomes on separately authorized devices. Retain NEO subset, high-level decrypt's metadata
5.3 requirement, AES released 5.4.2 versus SDK 5.4.0, serial 5.0.0 versus Python 5.0.3 and the existing
ROCA interval until evidence supports a selected change. No unsafe-key generation is implied by this plan.

Dependencies: `CORE-PIV-X004`, Core native/profile referral below and PIVIMP-001 reset routing.
Acceptance: signed-off support matrix plus meaningful fake gates and scoped device results; unsupported
Core catalog identities (P-521, Brainpool, secp256k1, symmetric slot keys) stay refused before encoding.
Reopen Features/XML, backend/factory/default resolver, README Requirements/matrix, CLAUDE firmware and
fixture comments only for the selected change. A metadata capability bit or source comparison is not a
FIPS/Bio/NFC/SCP/Native AOT pass.

### PIVIMP-004 — Separate fixed provisioning models from general PIV objects

**Medium risk; parser scope confirmed, general representation needs a decision. No-brainer: no.**
The typed CHUID/CCC readers reject encodings outside fixed SDK provisioning shapes. KeyHistory
requires four ordered TLVs, including F3 for a null URL; creation checks URL byte length rather than
the full retired-key/count/URL profile. Consumers can misclassify a valid foreign object as invalid,
or emit a self-roundtripping object that does not meet their deployment profile. Raw access already
supports inspection; existing certificate/key independence must not be changed to solve parser scope.

**BEFORE — `src/Piv/src/DataObjects/KeyHistory.cs` · `Encode` · lines 103–109 (literal baseline).**

```csharp
        Tlv[] tlvs =
        [
            new Tlv(OnCardTag, [OnCardCertificates]),
            new Tlv(OffCardTag, [OffCardCertificates]),
            new Tlv(UrlTag, urlBytes),
            new Tlv(UnusedTag, ReadOnlySpan<byte>.Empty)
        ];
```

`CardholderUniqueId.TryDecode` in `src/Piv/src/DataObjects/CardholderUniqueId.cs` lines 139–165
requires five TLVs, fixed FASC-N/date and empty signature/LRC. `KeyHistory.TryDecode` lines 132–155
requires exactly four entries; `Create` lines 71–84 checks URL encoding size but not counts/profile.

**AFTER — decision-dependent schematic, unapplied and uncompiled.**

```text
A: retain explicitly named FIXED_PROVISIONING models plus raw inspection; add selected profile validator
B: introduce GENERAL_OBJECT representation preserving modeled fields/raw unknowns
   keep convenience provisioning factories separate
KeyHistory: implement selected conditional-F3/count/URL rules for the named profile
Malformed required fields stay strict; do not blindly re-emit unknown response fields in requests
```

Python: general `Chuid`/`FascN` versus `generate_chuid` is useful separation; `generate_ccc` is fixed
generation, and no applicable KeyHistory implementation was established. Dependencies: PIVIMP-010
request range and Core TLV owners, not a whole generic TLV rewrite. Acceptance: cross-implementation
vectors for nondefault CHUID, additional valid CCC layouts and conditional KeyHistory F3; malformed,
duplicate and profile-invalid fields need distinct outcomes. Preserve byte borrowing/owned decoding
and raw access. Review model XML, typed/raw IPivSession contracts, provisioning guide and migration map.

### PIVIMP-005 — Sign the original message with algorithm-specific framing

**High; source-confirmed and historically device-confirmed. No-brainer: yes for correcting framing
and excluding agreement-only X25519; padding/API selection still needs a decision.** The generic
tool hashes every input, then calls the raw operation. RSA receives a digest without EMSA encoding;
Ed25519 signs SHA256(message), not the message. The frozen actual-tool probes preserve two intended-
contract failures: RSA original-message verification failed despite helper success, and Ed25519
verified the hash. Direct SDK controls passed. This is PivTool behavior, not a reopened raw SDK bug.

**BEFORE — `src/Piv/examples/PivTool/PivExamples/Signing.cs` · `SignDataAsync` · lines 97–103 (literal baseline).**

```csharp
            // Sign using explicit algorithm from the already-fetched metadata.
            var keyType = metadata.Value.KeyType ?? throw new NotSupportedException(
                $"Unknown PIV algorithm 0x{metadata.Value.RawAlgorithmId:X2}.");
            var signature = await session.SignOrDecryptAsync(slot, keyType, hash, cancellationToken);

            stopwatch.Stop();
            return SigningResult.Succeeded(signature, stopwatch.ElapsedMilliseconds);
```

**AFTER — algorithm flow schematic, unapplied and uncompiled.** Choose explicit RSA padding input,
or a documented PKCS#1 v1.5-only tool contract; PSS must be an explicit supported choice, not a
silently invented overload. Choose how Ed25519 ignores/rejects a hash option.

```text
P256/P384 -> selected message hash -> raw PIV sign -> DER ECDSA signature
RSA -> selected EMSA-PKCS1-v1_5 or EMSA-PSS full-width representative -> raw PIV sign
Ed25519 -> original message bytes unchanged -> raw 64-byte signature
X25519 / unknown / nonsigning algorithm -> refuse before private operation; offer agreement task
Clear owned framing/hash intermediates; keep borrowed original message unmodified
```

Python: `PivSession.sign` calls `_pad_message`; RSA prepares a representative and Ed25519 passes
the message. The existing C# certificate RSA generator separately prepares DigestInfo/PKCS#1 v1.5;
it is not the defective generic helper and must not inherit a false missing-padding claim.
Dependencies: PIVIMP-006 cleanup/cancellation and PIVIMP-007 response admission. Acceptance:
independent host verification of **original messages** for RSA PKCS#1 v1.5 (and PSS if selected),
P-256/P-384 DER and Ed25519; wrong message/hash/signature must fail. Verify algorithm eligibility at
CLI/menu and helper boundaries, exact APDU payload framing, and failure cleanup. Do not use helper
`Success`, nonempty output or hash-verification as the intended-message oracle. Reopen Signing XML,
tool README, generic selectors and host verification workflow. Historical P05/P06 remain failed records.

### PIVIMP-006a — Protect acquisition and every independent source copy

**High confidentiality risk; source-confirmed unprotected scopes, no runtime leakage demonstrated.
No-brainer: yes for immediate cleanup scopes; production derivation/escrow policy needs a decision.**
PIN/management parsing precedes awaited discovery outside `finally`. A discovery exception or
cancellation bypasses clearing. Random/KDF sources copied into disposable owners remain independent
arrays; disposing the destination does not clear the source or console strings.

**BEFORE — `src/Piv/examples/PivTool/Cli/Commands/CliRunner.cs` · `HandlePinVerifyAsync` · lines 160–166 (literal baseline).**

```csharp
        var pinBytes = DeviceHelper.ParseCredentialBytes(DeviceHelper.GetFlag(flags, "pin"), "pin");
        if (pinBytes is null) return 1;

        var device = await DeviceHelper.GetDeviceAsync(serial, ct);
        if (device is null) { ZeroAndReturn(pinBytes); return 1; }

        try
```

**BEFORE — `src/Piv/examples/PivTool/Cli/Prompts/PinPrompt.cs` · `GenerateRandomManagementKey` · lines 249–256 (literal baseline).**

```csharp
        var key = new byte[keyLength];
        RandomNumberGenerator.Fill(key);

        // Show the hex so user can save it
        OutputHelpers.WriteInfo($"Generated key: {Convert.ToHexString(key)}");
        OutputHelpers.WriteWarning("Save this key securely! You will need it for future management operations.");

        return CreateFromSpan(key);
```

**AFTER — cleanup schematic, unapplied and uncompiled.**

```text
Acquire first owned credential -> enter its cleanup scope immediately
Within scope: parse later credentials, discover/select, read files, open session, await complete task
For each random/KDF source: fill/derive/copy inside try; finally clear that source
Return destination ownership only on success; dispose it on later failure before transfer
Select SECURE_ESCROW/EXPLICIT_EVALUATION_DISPLAY and legacy fixed-salt migration separately
```

Apply the same protection to `HandleMgmtKeyAuthAsync` lines 261–267 and sibling multi-credential/file
paths, `DeriveKeyFromPassphraseBytes` lines 288–299 and source handling in `CreateFromDefault` lines
305–308. Preserve original aliases **`core-001:unit:a579b6a5cf4242e4` / `8fc815177a95b970`** under
CORE-PIV-X006/PIVIMP-006, not two new bugs. Python `pivman_*` is workflow precedent, **not** applicable
mutable-source cleanup precedent; its manager may use immutable secrets. Generic reader/provider
facets retain Core C ownership. Digit-only menu PIN/PUK policy is not the universal UTF-8 or federal
profile, and printing a key is not secure escrow.

Acceptance: owned-array observations across discovery, parse, output, file-read and auth failure;
destination/source independence and successful transfer/cleanup. No post-return pool view assertions.
Keep credentials alive until actual task completion, not merely a canceled wrapper wait. Reopen PinPrompt,
CLI/menu resource scopes, tool credential guide and shared reader guidance only at the selected facets.

### PIVIMP-006b — Give plaintext and sample results one cleanup owner

**High confidentiality risk for plaintext; source-confirmed API obstacle. No-brainer: no for result
shape, yes for closing known local scopes.** SDK decryption returns mutable caller-owned memory;
the sample converts that into a read-only property without a clearing API. Signature results and
certificate/slot records also need explicit transfer/disposal responsibilities. A record clone can
alias the same underlying resource; do not invent independent ownership for each clone.

**BEFORE — `src/Piv/examples/PivTool/PivExamples/Results/DecryptionResult.cs` · `DecryptedData` · line 31 (literal baseline).**

```csharp
    public ReadOnlyMemory<byte> DecryptedData { get; init; }
```

`DecryptionResult.Succeeded` lines 46–47 takes read-only memory; `Decryption.DecryptDataAsync`
lines 77–81 transfers the SDK plaintext into that result. `SlotInfoResult.cs` contains certificate
objects; `SlotInfo.cs` lines 120–138 can have null metadata because the instruction is unsupported.
Its `HasKey => Metadata is not null` must not be displayed as proven empty occupancy on older firmware.

**AFTER — decision-dependent schematic, unapplied and uncompiled.**

```text
A: mutable plaintext carrier with caller-owned clearing and explicit alias rules
B: sealed disposable result/owner, no independently owning record clone
Producer owns output until transfer; on failed packaging/output clear or dispose owned resources
Consumer uses output then clears/disposes in finally, including output/file exceptions
Slot presence -> PRESENT / ABSENT / UNKNOWN_METADATA_UNAVAILABLE under selected carrier
```

Python: `PivSession.decrypt` returns immutable bytes; this is not a suitable .NET erasure contract.
`check_key` proves key binding for supported RSA/EC cases, not metadata-independent universal
occupancy. Dependencies: PIVIMP-006c certificate disposal, PIVIMP-007 provider/result admission and
Core response-copy owner. Acceptance: actual plaintext owner cleared after success and output failure,
no writable casts of read-only memory, no clearing caller inputs, no silently lost certificate owners;
unknown metadata is not false absence. Review all decrypt/sign/slot result consumers, JSON/menu output
and README ownership before selecting a breaking carrier.

### PIVIMP-006c — Complete certificate export, CSR authentication and resource tasks

**Medium; confirmed task/result mismatch and missing deterministic disposal. No-brainer: yes for
export-field/PIN prerequisites/local disposal; broader result redesign belongs to 006b.** The export
helper returns `Certificate`, but the menu checks `CsrPem`, so a valid export fails. The CSR menu calls
signing without acquiring/verifying PIN. Generated certificates can survive a failed store, and
read/export/self-sign/attestation/slot callers need disposal of each returned certificate. F9 retrieval
uses `AttestationCertificate`, not the slot-attestation alternate field.

**BEFORE — `src/Piv/examples/PivTool/Cli/Menus/CertificatesMenu.cs` · `ExportCertificateAsync` · lines 101–110 (literal baseline).**

```csharp
        var result = await PivExamples.Certificates.ExportCertificatePemAsync(session, slot, ct);

        if (!result.Success || result.CsrPem is null)
        {
            OutputHelpers.WriteError(result.ErrorMessage ?? "Export failed");
            return;
        }

        var filename = AnsiConsole.Ask<string>("Enter filename to save:");
        await File.WriteAllTextAsync(filename, result.CsrPem, ct);
```

**AFTER — task schematic, unapplied and uncompiled.**

```text
Export: take returned Certificate into using scope; success/null check; export PEM; write; dispose
CSR: acquire PIN into owner; verify selected signing policy; sign next; retain PIN/session/key until terminal
Self-sign: own new certificate before Store; transfer on success; dispose on any store/packaging failure
F9 read: consume AttestationCertificate; dispose returned object; never replace existing F9 material
```

Python: certificate/CSR builders sign actual TBS bytes; CLI CSR verifies PIN immediately before
signing. These precedents do not require management authentication for a read-only CSR or permit
using a host public-key extractor as the device private key. Dependencies: 006b result contract and
006d synchronous callback lifetime. Acceptance: valid PEM export through the **actual menu**,
file-write failure disposal, first-use PIN-gated CSR host verification, self-sign failed-store disposal,
and F9 result-field routing with independent object ownership. Apply disposal to all named callers,
not only a documentation example. Review certificate/menu/result XML and tool task guide; keep
certificate/key storage independent and enrollment trust separate from successful storage.

### PIVIMP-006d — Keep cancellation and synchronous signing callbacks truthful

**Medium risk; source-confirmed cancellation loss and retained intermediate copies. No-brainer: no
for synchronous bridge/lifecycle policy, yes for removing an unsupported touch-timeout diagnosis.**
The certificate generator blocks on device signing without the outer operation token. Its hash,
DigestInfo/padded input and copied signature have separate owners. Broad catches turn cancellation
into ordinary failure; generic signing/decryption explicitly call it touch timeout even though PIV
has no confirmed touch-timeout status. Callers can retry an uncertain mutation or dispose borrowed
objects while admitted work still needs them.

**BEFORE — `src/Piv/examples/PivTool/PivExamples/Certificates.cs` · `YubiKeyRsaSignatureGenerator.SignData` · lines 466–473 (literal baseline).**

```csharp
        // Pad to key size with PKCS#1 v1.5 padding
        int keySize = publicKey.KeySize / 8;
        byte[] padded = ApplyPkcs1Padding(digestInfo, keySize);

        // Sign using YubiKey (blocking call - this is sync API required by X509SignatureGenerator)
        var signature = session.SignOrDecryptAsync(slot, keyType, padded).GetAwaiter().GetResult();

        return signature.ToArray();
```

**BEFORE — `src/Piv/examples/PivTool/PivExamples/Signing.cs` · `SignDataAsync` · lines 105–108 (literal baseline).**

```csharp
        catch (OperationCanceledException)
        {
            return SigningResult.Failed("Touch timeout. Please touch the YubiKey when prompted.");
        }
```

**AFTER — decision-dependent schematic, unapplied and uncompiled.**

```text
A: retain required synchronous X509 bridge with explicit captured token and whole-task lifetime
B: select asynchronous TBS-sign/reassembly workflow after provider/API review
Own hash/DigestInfo/padded bytes and raw signature until copy/transfer; clear local owners in finally
Select PROPAGATE_CANCELLATION or DISTINCT_CANCELED_RESULT consistently through helpers/CLI
Never label token cancellation as touch timeout without a confirmed device status
```

Python builders are synchronous precedent only. C# ECDSA bridge has the same blocking/token issue
at `Certificates.cs` lines 551–565; it already uses DER, so no unnecessary signature-format conversion
is proposed. Dependencies: [whole-workflow referral](#pivcore-002--whole-workflow-lifetime-and-terminal-recovery),
006a–c and PIVIMP-005. Acceptance: cancellation before wire versus admitted success are separate
oracles; drain complete original tasks before clearing input or disposing session/key/prompt. Exercise
callback failure, certificate generation/store failure and error-output paths; preserve primary failure
and owned result cleanup. Physical touch-dependent cancellation remains deferred. Reopen actual callback,
touch/result XML and cancellation guidance only when the selected behavior changes.

### SAMPLE-A004 — Dispose the unified CLI attestation certificate

**Low resource-lifetime risk; source-only missing deterministic disposal, no permanent leak or native
failure proved. No-brainer: yes.** This is distinct caller evidence alongside PIVIMP-006, not a newly
counted PIV defect. Preserve **`core-001:unit:10653b759a2209f8`**; the OpenPGP caller
`3ccf06e816f277d2` remains under its existing module owner and is not rewritten here.

**BEFORE — `src/Cli.Commands/src/Piv/PivCommands.cs` · `PivKeysAttestCommand.ExecuteCommandAsync` · lines 552–553 (literal baseline).**

```csharp
        await using var session = await deviceContext.Device.CreatePivSessionAsync();
        var cert = await session.AttestKeyAsync(slot);
```

**AFTER — proposed literal local replacement, unapplied and uncompiled.**

```csharp
        await using var session = await deviceContext.Device.CreatePivSessionAsync();
        using var cert = await session.AttestKeyAsync(slot);
```

Python `check_key`/certificate builders establish cryptographic tasks but no applicable .NET
deterministic-disposal equivalent. Dependency: unified CLI/result owner, not session disposal; the
returned certificate has its own lifetime. Acceptance: normal header/PEM output and throwing output
paths dispose the returned object, while session cleanup retains primary-error policy. Review this
command's exact output scope and resource documentation independently; do not generalize to all CLI
commands, permanent native leakage, manufacturer-root trust or F9 replacement.

### PIVIMP-007a — Admit algorithm-shaped output before reporting completion

**Medium; malformed-success acceptance confirmed, provider erasure remains a bounded risk.
No-brainer: no for validation depth/provider strategy.** Crypto parsing checks 7C/82 tags, not
algorithm-specific widths or DER structure. A one-byte P-256 result or empty tagged output can be
returned as success and resolve user presence as Completed. RSA local unpadding also creates
`BigInteger` representations whose internal storage is not explicitly cleared; clearing original
arrays does not prove those copies erased. This is not a reopened mutable-output/response-Clear bug.

**BEFORE — `src/Piv/src/Cryptography/PrivateKeyOperations.cs` · `ParseCryptoResponse` · lines 429–437 (literal baseline).**

```csharp
        // Parse inner TLV (0x82 - Response data)
        using var inner = Tlv.Create(outer.Value.Span);
        if (inner.Tag != TagResponse)
        {
            throw new ApduException("Invalid crypto response - expected TAG 0x82");
        }

        // Copy the value before the Tlv objects are disposed. The caller owns this buffer until transfer.
        return inner.Value.ToArray();
```

**AFTER — decision-dependent schematic, unapplied and uncompiled.**

```text
Supply selected operation + key type to response admission before Completed/transfer
RSA -> exact modulus width; Ed25519 -> 64 bytes; agreement -> P256/X25519 32, P384 48
ECDSA -> selected strict DER/range admission; reject missing/empty/trailing malformed payloads
Malformed success -> Failed resolution; clear raw and untransferred parsed owner
Unpadding -> select reviewed provider/explicit-buffer alternative or document accepted BigInteger residual
```

Python `_unpad_message` uses dummy RSA/integer re-wrap; that does not resolve .NET provider-copy
erasure. `_pad_message` and `check_key` distinguish input framing/independent verification from output
shape. Dependencies: PIVIMP-005 framing, Core B6 response/staging and C provider owners. Acceptance:
fake algorithm-specific malformed responses through public methods, verify terminal pairing/cleanup,
and independent valid RSA/EC/Ed25519/agreement controls. Preserve the corrected **zero-prefixed valid
PKCS#1/OAEP block below a full-bit-width dummy modulus** rationale; do not revive the retracted
probability argument. Completed currently precedes local RSA unpadding; decide/document any change
without conflating a later padding exception with confirmed touch timeout. Reopen PrivateKeyOperations,
crypto XML/README and response-owner test explanations; no all-provider erasure claim.

### PIVIMP-007b — Bound certificate decompression and CertInfo admission

**Medium resource/interoperability risk; source-confirmed unbounded decompression and permissive
CertInfo handling. No-brainer: no for selected limits/encoding policy.** CertInfo with a first byte
other than 1 is treated as uncompressed, including unknown modes. GZip copies into an unbounded
MemoryStream, so card-supplied content can expand far beyond the compressed object. Certificates
are ordinarily public data; this is not automatically a secret-zeroing defect.

**BEFORE — `src/Piv/src/Certificates/CertificateProtocol.cs` · `GetCertificateAsync` · lines 83–90 (literal baseline).**

```csharp
        if (isCompressed)
        {
            using var input = new MemoryStream(certBytes);
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            await gzip.CopyToAsync(output, cancellationToken).ConfigureAwait(false);
            certBytes = output.ToArray();
        }
```

**AFTER — decision-dependent schematic, unapplied and uncompiled.**

```text
Parse required certificate fields under SELECTED_CERTINFO_LENGTH/MODE rules
Unknown unsupported mode -> selected explicit error/raw-inspection route, not DER fallback
Decompress with SELECTED_DECOMPRESSED_CEILING checked while copying, not after unbounded allocation
Load only accepted complete DER; dispose streams/certificate on failed transfer
```

Python `get_certificate` rejects unknown nonzero CertInfo; the inspected source supplies no size
ceiling to copy. Dependencies: PIVIMP-004 typed/raw scope and Core TLV parser contract. Acceptance:
known DER/GZip interoperability, missing/empty/unknown CertInfo, malformed GZip, high-expansion and
cancellation cases with bounded allocation behavior. **1856 is the SDK automatic-compression threshold,
not a universal standard storage or decompression ceiling.** Review CertificateProtocol, compression
enum/API guidance and raw-object alternative; do not broaden this into automatic certificate/key binding.

### PIVIMP-008 — Make tests survive their own failing assertions

**Medium evidence/resource risk; source-confirmed cleanup/oracle gaps. No-brainer: yes for full task
and owner cleanup; release-failure expectations and fixture/product floors need decisions.** Some
tests can throw before awaiting a started task, then dispose its session or leave persistent state.
Weak canonical oracles (generation/nonempty output/single-use policy) are not upgraded merely because
stronger supplemental probes passed. Decrypt eligibility must include metadata 5.3, and cleanup must
not mask the primary failure or claim an unconditional native release when release is unproven.

**BEFORE — `src/Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivConnectionConcurrencyTests.cs` · `ConcurrentSignAndObjectRead_OnOneSession_SecondOperationIsRefused` · lines 74–80 (literal baseline).**

```csharp
            var readTask = session.GetObjectAsync(DataObject.Retired1);
            // If refusal is not observed, this assertion can throw before readTask is awaited.
            // Complete pending-task cleanup on that failure remains a separate harness follow-up.
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await session.SignOrDecryptAsync(Slot.Authentication, KeyType.P256, digest));
            var readBack = await readTask;
            Assert.Equal(storedObject, readBack.ToArray());
```

**AFTER — harness/oracle schematic, unapplied and uncompiled.**

```text
Start pending task -> protect it immediately; release held seams in finally
On assertion failure: await original task terminal, clear returned owners, preserve primary failure
Independent bounded cleanup token/state readback; do not treat wrapper timeout as native completion
Oracle: host message verification / peer-secret equality / repeated policy enforcement / slot binding
Release: test selected checked-release/quarantine result, not unconditional successful reopen
```

Python `check_key` supplies an RSA/EC host-binding precedent; it is not an X25519/Ed25519 oracle.
Acceptance: failing-assertion paths actually drain and dispose; exact fixture floors and nonzero
executed counts; focused improvements to Always, X25519 agreement, imported Ed25519 verification,
ECDH peer/device equality, reset normal-slot/object coverage, key move/certificate independence,
attestation issuer/key binding, RSA PSS host verification and cross-key public-key association.
Preserve manufacturer-root trust as separate. Read complete cleanup in `PivMultiKeyContentionTests`,
`PivDiscoveryContentionTests`, `PivPinOnlyWorkflowTests`, decrypt/import/private-key/certificate tests
and physical hotplug tests when implementing, rather than rewriting the full suite here.

Dependencies: Tests.Shared authorization/fixtures (`CORE-A-C013`), Core B1/B5 release/drain, and the
physical lane below. Keep **86/89 canonical passed**, original skips plus later three two-key passes,
P01–P04 scoped supplemental passes and **P05/P06 failures** unchanged. Historical keys 103/125 are not
current-state proof. Reopen README Maintainer verification, CLAUDE Test Infrastructure, attributes,
test comments and shared test guide after actual oracle/cleanup changes. Response-only PIN-only
tests retain their accepted witnesses/clearing; withdrawn pool memory observations remain excluded.

### PIVIMP-009 — Decide the ADMIN timestamp format before migration

**Medium interoperability risk; source-confirmed format difference. No-brainer: no.** C# emits
trimmed little-endian Unix seconds and accepts up to eight date bytes. Python uses four-byte
big-endian. An SDK self-roundtrip cannot detect a foreign timestamp being misinterpreted or rewritten.
There is no reliable endian discriminator for arbitrary existing four-byte values; silently guessing
a legacy format could corrupt dates while preserving flags/salt.

**BEFORE — `src/Piv/src/DataObjects/AdminData.cs` · `EncodeUnixTimeTrimmed` · lines 247–249 (literal baseline).**

```csharp
        long unixSeconds = value.ToUnixTimeSeconds();
        Span<byte> buffer = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64LittleEndian(buffer, unchecked((ulong)unixSeconds));
```

**AFTER — decision-dependent schematic, unapplied and uncompiled.**

```text
A: preserve documented SDK legacy codec; require explicit cross-tool format choice
B: adopt fixed big-endian uint32 with explicit legacy import/migration, range/epoch policy
Preserve flags/salt/unknown-required-field policy; never auto-detect ambiguous four-byte dates
Use version/source-tagged migration inputs or caller-selected LEGACY_FORMAT, not guessed bytes
```

Python: `PivmanData` tag 83 uses `>I`; its tuple-versus-scalar decode detail also limits roundtrip
claims. Concrete host vector: Unix seconds `0x01020304` encode as C# `04 03 02 01`, Python
`01 02 03 04`. Dependencies: PIVIMP-002 legacy derived/coherence and migration owner. Acceptance:
independent vectors for zero, trimming, bounds/pre-epoch/overflow, both formats and explicitly selected
legacy input; determine actual v1 compatibility before committing migration. No new v1 inspection or
fix is claimed here. Review AdminData encode/decode XML, PIN-only tasks, README interoperability and
current migration fragments together; do not erase legacy objects to evade compatibility.

### PIVIMP-010 — Reject raw object IDs that would change on the wire

**Medium; source-confirmed request truncation. No-brainer: yes once the request range is accepted.**
Negative values take the one-byte branch and large values lose high bytes. For example `-1` becomes
`5C 01 FF`, while `0x01000000` becomes `5C 03 00 00 00`; GET/PUT can target an unintended object,
including writes/deletions. This is request validation, not a reason to discard unknown response data.

**BEFORE — `src/Piv/src/DataObjects/DataObjectProtocol.cs` · `EncodeObjectId` · lines 119–124 (literal baseline).**

```csharp
    private static byte[] EncodeObjectId(int objectId)
    {
        if (objectId <= byte.MaxValue)
        {
            return [TagObjectId, 0x01, (byte)objectId];
        }
```

**AFTER — proposed literal insertion at the start of that method, unapplied and uncompiled, conditional
on accepting `0..0xFFFFFF`.** Decide whether zero/reserved IDs need a narrower policy separately.

```csharp
        if (objectId < 0 || objectId > 0xFFFFFF)
        {
            throw new ArgumentOutOfRangeException(nameof(objectId), "Object ID must fit in 24 bits.");
        }
```

Python `get_object`/`put_object` uses `int2bytes`; the inspected source does not establish a matching
maximum. Dependencies: PIVIMP-004 raw/typed profile, not a Core open-identifier restriction. Acceptance:
behavior tests inspect emitted GET/PUT bytes for valid one/two/three-byte boundaries and assert **zero
transmission** for IDs that previously aliased through truncation, including deletion. Keep caller
payload untouched and unknown raw response encodings available. Reopen raw Get/Put XML, provisioning
guide and encoder tests; do not add validation-only tests divorced from wire behavior.

### PIVCORE-002 — Whole-workflow lifetime and terminal recovery

**High workflow-lifetime risk; source boundary confirmed, product intent unselected. No-brainer: no.**
Canonical shared owner is **`core-001:CORE-B5-C006`**, already linked in
[WebAuthn whole-ceremony lifetime](#webauthn-whole-ceremony-lifetime--linked-core-b5-c006).
PIVCORE-002/CORE-PIV-X002 adds actual PIV authentication, PIN-only/reset and metadata-plus-crypto
sites; it does not create another generic Core defect. PIV request/resolution callbacks lie outside
the exchange guard; they can outlive drain, but are not OTP's callback-inside-guard self-wait path.

**BEFORE — `src/Piv/src/PivSession.cs` · `ResetAsync` · lines 207–217 (literal baseline).**

```csharp
        // Reset management authentication state. This workflow retains its backend/protocol and performs
        // no terminal session disposal, applet reselection or fresh SCP handshake.
        SetManagementKeyAuthenticationState(false);

        // RESET changed the physical applet even if the metadata refresh below fails. Establish a
        // conservative post-reset type before querying: a >=5.7 version defaults to AES-192, and an
        // major-zero development version follows the SDK's modern-default compatibility convention.
        ManagementKeyType = GetConservativeDefaultManagementKeyType(_detectedFirmwareVersion);

        // Update management key type from metadata (firmware 5.3+)
        ManagementKeyType = await ResolveManagementKeyTypeAsync(_backend, cancellationToken).ConfigureAwait(false);
```

**AFTER — decision-dependent schematic, unapplied and uncompiled.** Choose explicit caller-serialized
whole tasks with limited exchange guarantees, or shared whole-operation admission/lifetime leases
that cover all applet sites and define nested calls/reentrancy. Select failure-state invalidation and
reset/SCP/reopen policy separately; a new lock or `IsAuthenticated` flag is not channel-restoration proof.

```text
SELECTED_WORKFLOW_ADMISSION covers authentication/PIN-only/reset/metadata+crypto until terminal
Keep borrowed credentials, keys, peers and prompt services alive through callbacks/local unpadding
Pre-admission cancellation -> no send; admitted traffic may succeed with None constituents
Dispose/reentrant callback -> selected refusal/drain policy, no assumed native interruption
On uncertain mutation -> terminal observation + authorized fresh recovery, not blind retry
```

Python reset/sign/coherence helpers are synchronous sequential workflows, not a shared async lease
precedent. Dependencies: Core B5, PIVIMP-001/002/006d/007a/008; local RSA unpadding follows parsed
Completed and remains outside the exchange. Acceptance: hold between real public workflow phases and
inside outside-guard callbacks, exercise overlap/disposal/cancellation under selected policy, release
held gates in `finally`, await original tasks and assert exact cleanup/terminal outcomes. Physical
touch cancellation/native interruption and recovery are **deferred**, never replaced by pre-wire callback
cancel tests. Reopen session/interface lifetime XML, PIV guide, tool callback tasks and shared user
interaction/raw-tier/migration guidance when the chosen contract changes.

### PIV physical, profile and restoration qualification

**Qualification; no assigned defect severity. No-brainer: no. BEFORE/AFTER code: N/A.** No suitable
source edit establishes device power, separate-process PIN isolation, native interruption, touch-cache
timing or recovery after removal. Strict Exclusive defaults and explicit Shared/fallback remain a
settled portable policy; requested UNPOWER, checked native release, observed power and observed PIN
state are four different facts. Direct session disposal closes no borrowed connection and sends no
PIN-clear command. Keep **PIVCORE-001**, **`audit-v2-20261022-20261031:#22` / YESDK-1630** and
CORE-PIV-X001/B1-C001 identities together without declaring universal physical isolation fixed.

Procedure for a separately authorized future lane:

1. Obtain explicit human coordination for the physical prerequisite printer, touch-signing, hotplug
   and supplemental touch-dependent cancellation/recovery. They remain **user-deferred**, not skipped
   passes. Identify current serial, actual firmware/product, OS, USB/NFC/reader, sharing mode, process
   boundaries and initial state; do not reuse historical key state as a fresh baseline.
2. Capture safe stable normal-slot/certificate/retry/management metadata and preserved F9 identity
   without extracting/replacing private keys or erasing preexisting protected content. Historical
   serials 103/125 ended with default PIN/PUK 3/3, AES192/default/Never management metadata, empty
   normal slots/certificates and distinct retained F9. Protected PRINTED was deliberately unread in
   final restoration: there is **no independently observed byte-absence claim** for it.
3. Observe setup, admitted operation, token expiry, original terminal result, callback completion,
   native release outcome and reopen/PIN state separately. Include strict/shared and separate-process
   cases only at their authorized scope. A canceled WaitAsync does not permit input cleanup or prove
   native cancellation; physical power-cycle evidence cannot be substituted by an UNPOWER request.
4. Preserve F9 keys/certificates, existing private material and saved credentials throughout bounded
   independent restoration/readback. Do not add an erase/reset step to make unexplained outcomes pass.
   Normal PIV reset retains F9; product-wide Bio reset has a separately selected scope/authorization.
5. Record exact compatibility/profile sources and actual executed outcomes for Bio, older/NEO,
   released AES/serial boundaries, modern FIPS, NFC, PIV SCP, other OSes and Native AOT when those lanes
   are assigned. Plain PIV passes do not qualify protected PIV. Serial 125's **FF and 00 INITIALIZE
   UPDATE `6A88`** remain failures before EXTERNAL AUTHENTICATE/RESET. Historical serial **9681620**
   two-key discovery failure and `fixes-wave-001:CORE-001` remain unexplained; do not erase those records.

Python reset/CLI checks and synchronous operations provide no physical-isolation or async-native
interruption proof. Dependencies: PIVIMP-003/008, B1 checked-release/sharing, B4 installed PC/SC
producer/header and B6 SCP/ISO/profile owners. Acceptance is source-bound actual evidence and independent
restoration within each authorized lane, not a broad all-PIV certification. Historical Linux USB
SmartCard 5.8.0.beta.0/5.8.0.alpha.2 and raw FD `00 00 01` retain their distinct provenance; attestation
issuer/key-binding passes are not manufacturer-root trust. Windows managed software acceptance and
unsealed pipe/JIT evidence are not HID-driver, PIV touch or PC/SC PIN-power qualification.

### PIV narrow shared dependencies and Windows successor

These rows retain canonical family/applet owners and only PIV-relevant closure conditions. The
[original dependency dispositions](modules/core/pass-1/core-001/boundaries/Piv/dependency-dispositions.json)
remain the exact path/reopening map; this table is not a transfer of the entire Core backlog.

| Original dependency / canonical owner | PIV occurrence and remaining closure condition |
| --- | --- |
| `CORE-B1-C001`; PIVCORE-001 / CORE-PIV-X001 | Sharing/native-resource/connection factory plus physical qualification above. Strict policy resolved; PIN/power/process outcomes remain qualified. |
| `CORE-B5-C006`; PIVCORE-002 / CORE-PIV-X002 | Whole-workflow card above; Core owns admission/drain mechanism, PIV owns mutation/authentication/reset/callback policy. Windows did not add a whole-operation lease. |
| `CORE-B6-C002`; `CORE-C-C005`, `CORE-C-C006`, `CORE-C-C007`; PIVCORE-003 / CORE-PIV-X003 | APDU response, TLV/result, transport/chained/SCP-decrypted/provider copies each retain their own owner. Actual PIV Clear/transfer is resolved; later-result clearing supplies no independent staging erasure. Review selected crypto/certificate parser/result changes at 007/006, not generic public metadata as secret. |
| `CORE-C-C013` / generic C reader-provider owner; CORE-PIV-X006 | PIV-local source/acquisition cleanup stays 006a; generic ConsoleCredentialReader/options, UTF-16 units, provider and independent-copy questions retain C. No universal federal PIN policy or production escrow inferred. |
| `CORE-B4-C006`, `B4-Q001`; adjacent `CORE-B4-C005`, `CORE-B4-C009` | Exact installed PC/SC producer/header/ABI/string/LP64 PCI, loader and error interpretation remain B4 source/native questions. New PC/SC owner names do not close them; PIV chooses SmartCard, not HID backends. |
| `CORE-B1-C006`, `CORE-B1-C009`; `CORE-B1-C010` / `CORE-B3-C006` | Native EndTransaction error/oracle, post-open logging cleanup and ambient-context policy are narrow connection dependencies, unchanged by extraction. Do not infer new PIV failure or accepted worker-context policy. |
| `CORE-B6-C015` / B6 exact SCP and ISO/YubiKey profile owner | SELECT-before-SCP, parameter contained-owner aliases, no plain-reselect recovery and reset retaining backend/protocol remain explicit. Exact SCP03 Public Release/Public Review, SCP11 Public Review/final/vendor and unavailable edition-specific ISO clauses remain qualified. |
| `CORE-A-C013` / Tests.Shared; `CORE-B2-C005` / harness owner | Actual fixture authorization/read ordering and failing-assertion drain remain primary-owner work. PIVIMP-008 owns its particular tests; no new device execution or broad Linux harness repair is credited. |
| `SAMPLE-A004` / unified CLI certificate owner | PIV command card above retains `10653b759a2209f8`; OpenPGP `3ccf06e816f277d2` is a separate caller. Missing disposal is source-only, not permanent native leakage. |
| `oath-pilot-and-fixes-wave-002:DOC-002` / shared migration owner | Original migration-YAML failure remains a separate inherited dependency; no parser run, fix or OATH re-review here. |

The Windows successor **supersedes old built-in Windows HID report access** at CORE-B4-C001 and
rescopes B4-C007 staging/ABI/platform questions. Those HID mechanisms are not PIV's SmartCard route.
Do not import CORE-B5-C005's received-HID-report backlog as a PIV defect, or infer that managed report
tests close PC/SC/native/profile or provider questions. Retain the successor's open/rescoped facets
only where this table names actual PIV dependencies.

### PIV coverage ledger, inherited referrals and exclusions

All original IDs below are coverage groups, never an asserted count of independent bugs. The six
boundary documentation facets were already accepted in Core PIV revision 2 (**13 paths**, integrated);
their old pending-editor text does not override that closeout. This section carries remaining
engineering, policy and qualification facets, not a new documentation rewrite.

| Original PIV ID | Active disposition / complete remaining scope |
| --- | --- |
| `piv-001:PIVIMP-001` | 001: Bio/product preflight, mutation-before-error, post-RESET refresh and reset route; physical/profile qualification retained. |
| `piv-001:PIVIMP-002` | 002: partial writes, raw rotation/retry/object coherence, legacy derived preservation; active-key/order/6982 fixes explicitly excluded below. |
| `piv-001:PIVIMP-003` | 003 and qualification: gates, detected/effective provenance, NEO/released/FIPS/NFC/Bio/ROCA/profile dimensions. |
| `piv-001:PIVIMP-004` | 004: fixed CHUID/CCC, KeyHistory conditional F3/count/URL and raw route; no automatic enrollment binding. |
| `piv-001:PIVIMP-005` | 005: confirmed/unfixed tool RSA original-message and Ed25519 SHA256(message), algorithm framing/eligibility and independent oracles. |
| `piv-001:PIVIMP-006` | 006a–d: pre-try/source cleanup, plaintext/signature/result/certificate/slot ownership, export/F9 field/CSR tasks, callback/cancellation/escrow/old-firmware unknown state. SAMPLE-A004 stays distinct caller evidence. |
| `piv-001:PIVIMP-007` | 007a–b: algorithm-shaped response admission before transfer, RSA BigInteger/provider residuals, CertInfo and decompression bounds; valid re-wrap rationale is already corrected. |
| `piv-001:PIVIMP-008` | 008 and qualification: fixture floors, all named weak oracle families, original-task drain, failure-primary preservation and independent cleanup. Historical successes do not rewrite canonical assertions. |
| `piv-001:PIVIMP-009` | 009: timestamp endianness/width/range, v1 compatibility and explicit legacy migration. |
| `piv-001:PIVIMP-010` | 010: negative/over-24-bit request aliases, selected range and no-transmission oracle; response compatibility unaffected. |
| `piv-001:PIVCORE-001` | Physical qualification / B1-C001, nonadditive #22 / YESDK-1630 provenance. Portable mitigation resolved; PIN-power/process proof remains open. |
| `piv-001:PIVCORE-002` | Whole-workflow card / B5-C006; complete mutation/callback/local-unpad task policy, physical cancellation/recovery deferred. |
| `piv-001:PIVCORE-003` | Narrow owner table / B6-C002 and C-C005–007: resolved actual PIV response/encoding owners, retained lower independent copies and borrowed/contained-object lifetime. |

| Original Core boundary ID | Remaining facet → canonical owner; documentation status |
| --- | --- |
| `core-001:CORE-PIV-X001` | PIVCORE-001 physical/process/shared-access policy; revision-2 documentation accepted, not physical isolation closure. |
| `core-001:CORE-PIV-X002` | PIVCORE-002 / B5-C006 workflow decision and physical cancellation; accepted explanatory correction does not add a lease. |
| `core-001:CORE-PIV-X003` | PIVIMP-007 / PIVCORE-003 validation, copy/provider qualification; actual owner/return terminology documentation accepted. |
| `core-001:CORE-PIV-X004` | PIVIMP-003 and B4/B6 source/profile qualification; detected/effective and actual Le/test rationale corrections accepted. |
| `core-001:CORE-PIV-X005` | PIVIMP-008 tests/release-oracle policy/physical lane; accepted test prose supplies no stronger executed assertion. |
| `core-001:CORE-PIV-X006` | PIVIMP-005 and 006 confirmed tool/resource/workflow scopes; accepted guidance and Windows pool rollback do not fix them. |

The [11 routed provenance keys](modules/core/pass-1/core-001/boundaries/Piv/inherited-outcomes.md)
reconcile as follows. The first four are the three PIV questions plus older issue provenance; the
other seven are dependency occurrences with their **existing applet owner**, not new PIV findings.

| Routed original key | Canonical owner / PIV-only relation |
| --- | --- |
| `piv-001:PIVCORE-001` | Qualification above / B1-C001. |
| `audit-v2-20261022-20261031:#22` / YESDK-1630 | Same portable mitigation and physical/process limits; older provenance preserved. |
| `piv-001:PIVCORE-002` | Whole-workflow card / B5-C006. |
| `piv-001:PIVCORE-003` | Narrow owner table / B6-C002 and C-C005–007. |
| `openpgp-001:PGP-CORE-002` | [OpenPGP](#openpgp) owns KDF/cache/SCP/reinitialization; PIV confirms shared borrowed/owned disposal only. |
| `openpgp-001:PGP-CORE-004` | [OpenPGP workflow](#pgp-core-004--whole-workflow-and-notification-result-ownership) owns 6600/Failed/recovery; PIV adds its distinct outside-guard callbacks, no native interruption claim. |
| `security-domain-001:SD-DEP-004` | [Security Domain](#security-domain) owns GetData/reset traces; PIV shares response/guard/Le mechanisms, not SD key/protocol reset semantics. |
| `yubihsm-001:HSM-DEP-002` | [YubiHSM Auth](#yubihsm-auth) owns CALCULATE/6982/Failed/fresh recovery; PIV shares borrowed prompt and admitted SmartCard drain. |
| `yubihsm-001:HSM-DEP-003` | YubiHSM Auth owns SessionKeys/receipt/cryptogram layout; PIV confirms separate response/TLV/result owners only. |
| `yubiotp-001:OTP-DEP-001` | [YubiOTP](#yubiotp) owns HID route; PIV's factory is SmartCard/no fallback with hidden-owned versus direct-borrowed connection. |
| `yubiotp-001:OTP-DEP-003` | YubiOTP owns status/CRC/request and inside-guard callback behavior; PIV keeps distinct SmartCard constituent cancellation/output transfer. |

**Resolved/retracted/superseded exclusions, not new work:**

- PR745/prior commits already resolved absent-data VERIFY count invention, instruction-specific
  metadata fallback, failed-management/disposal invalidation, SET 6982 versus unrelated-status state,
  detected-version defaults independent of override, active-key authentication before protected writes,
  default-before-delete order and stale-derived restoration/witness validation. Preserve exact fixes;
  partial coherence/workflow/profile questions above remain.
- Raw unknown algorithm metadata/extraction, strict wire subsets, mutable SDK outputs, actual owned
  response Clear and private encoding predecessor cleanup remain resolved. Raw Ed25519 original-message
  passthrough, short EC left-padding and oversized RSA rejection remain resolved; none fixes PivTool
  framing. SPKI metadata comparison is already strengthened. No pool seam/pre-return runtime oracle
  is reinstated; incoming tests observe independent responses, recovery candidates and witnesses.
- PIV-C001–C011 and PIV-REV-001–003 are completed documentation/correction groups, not additional
  engineering bugs. F9 result-shape work stays 006; zero-prefixed valid RSA re-wrap explanation is
  corrected. X25519 supplemental RFC/vendor secret equality and attestation issuer/key-binding passed
  historically, resolving those narrow missing-evidence facets, not Bio/SCP/native/physical/general
  oracle or manufacturer-trust qualification.
- All fourteen OATH IMP resolutions and the retracted HSM retry-message subclaim remain excluded.
  Windows old report-route supersession is limited to the successor's named mechanisms; no unrelated
  applet or whole-Core reopening follows. Historical failed signing assertions, old serial failures,
  F9/private material and intentionally unread PRINTED limits are preserved, not repaired by erasure.

**PIV author verification:** ten original PIVIMP IDs, three PIVCORE IDs, six Core boundary IDs and all
11 routed provenance keys are explicitly accounted for. The section contains 14 original-group review
cards, one distinct SAMPLE-A004 resource card, one shared-workflow decision card and one qualification
card (17 total). Static source/pin/path/link/anchor/fence
and edit-boundary checks were performed. At author handoff, changes were limited to PIV, its inventory
row and dispatch label; other applet bodies were preserved. No fix, compilation/build,
SDK test, hardware/native call, reset, commit, push, frozen-record edit or subagent was performed.
Human review and implementation remain unstarted; ready for a **separate final completeness review**,
not an all-applet completeness claim by this author.

## Final coverage reconciliation

### Card counts and applet review state

There are **176 local review cards** and **three additional displayed referrals**: WebAuthn
shows CTAP-DEC-002, and Management shows two Core metadata referrals. Thus **179 card displays** are
present, not 179 unique bugs. Shared-lifecycle, test, optional-support, policy and qualification cards
are included. Boundary IDs, inherited aliases and dependency rows do not add cards or accepted defects.

The following counts are checked against section headings and the authors' coverage ledgers. They do
not replace the original group inventories above. All states apply to every card in the row; no user
acceptance, implementation selection, compilation or new device execution is credited.

| Applet / review entry | Local review cards | Additional displayed referrals | Coverage ledger | User review | Implementation / proposed code |
| --- | ---: | ---: | --- | --- | --- |
| [OATH](#oath) | 8 | 0 | [Six DEC groups, inherited test and qualification; 14 IMP resolutions excluded](#oath-coverage-ledger-and-exclusions) | Unstarted | Deferred / uncompiled |
| [WebAuthn](#webauthn) | 23 | 1 CTAP decision | [21 WA groups, workflow and qualification; 12 boundary groups](#webauthn-coverage-ledger-and-exclusions) | Unstarted | Deferred / uncompiled |
| [Management](#management) | 19 | 2 Core metadata | [17 IMP groups, bound-factory test and qualification; three boundary groups](#management-coverage-ledger-and-exclusions) | Unstarted | Deferred / uncompiled |
| [CTAP2](#ctap2) | 50 | 0 | [47 IMP groups, two DEC groups and X020; 23 boundary groups](#ctap2-coverage-and-exclusions) | Unstarted | Deferred / uncompiled |
| [Security Domain](#security-domain) | 14 | 0 | [Eight IMP, five DEP and eight boundary groups](#security-domain-coverage-ledger-and-exclusions) | Unstarted | Deferred / uncompiled |
| [YubiOTP](#yubiotp) | 14 | 0 | [12 IMP, five DEP and nine boundary groups](#yubiotp-complete-coverage-ledger-and-exclusions) | Unstarted | Deferred / uncompiled |
| [YubiHSM Auth](#yubihsm-auth) | 13 | 0 | [Eight IMP, three DEP and seven boundary groups](#yubihsm-auth-coverage-ledger-and-exclusions) | Unstarted | Deferred / uncompiled |
| [OpenPGP](#openpgp) | 18 | 0 | [Four Core questions and eight boundary groups](#openpgp-shared-dependency-and-boundary-ledger); [14 IMP groups and 21 documentation labels](#openpgp-documentation-dependency-and-exclusion-ledger) | Unstarted | Deferred / uncompiled |
| [PIV](#piv) | 17 | 0 | [Ten IMP, three Core questions, six boundary groups and 11 routed keys](#piv-coverage-ledger-inherited-referrals-and-exclusions) | Unstarted | Deferred / uncompiled |
| **Total** | **176** | **3** | Nonadditive coverage, not a defect total | **All unstarted** | **All deferred / uncompiled** |

Management's contextual support note is not a third Core referral card. OpenPGP's numeric IMP gaps
are intentional documentation-only origins; PIVIMP-006 and 007 split into several cards. CTAP-IMP-017
is rejected and has no product card. OATH's fixed IMP groups are not reopened by their retained residual
policy or qualification facets.

“Local” counts the applet's review presentation, including caller-specific shared-workflow cards;
it does not transfer ownership of a Core mechanism or deduplicate overlapping facets across applets.

### Canonical shared referral index

This index routes **only dependencies already named by applet ledgers**. The canonical mechanism owner
remains the original Core family or shared owner, not whichever applet first describes it. Local cards
retain their applet-specific behavior, snippets and acceptance criteria. Refer to the rows below to
coordinate closure; do not use them as replacement cards or an expanded Core implementation backlog.

| Shared owner / original identities | Canonical applet occurrences and local closure context | Boundary to preserve |
| --- | --- | --- |
| Core A/B1 discovery, identity and claim owners; original `fixes-wave-001:CORE-001`; `CORE-B1-C005` | [OATH shared rows](#oath-shared-dependencies), [WebAuthn boundary rows](#webauthn-shared-dependencies-and-core-boundary-coverage), [Management shared rows](#management-shared-dependencies-and-core-boundary-coverage), [CTAP shared rows](#narrow-shared-dependencies-and-windows-successor), [PIV shared rows](#piv-narrow-shared-dependencies-and-windows-successor) | Known-member/process-local coordination is not physical all-process isolation. Historical two-key failure and custom-provider release remain qualified; no new discovery defect inferred. |
| `CORE-B5-C006` whole-workflow admission/drain; applicable B5-C004/C008 and B1 connection owners | [OATH workflow occurrence](#oath-shared-dependencies), [WebAuthn whole ceremony](#webauthn-whole-ceremony-lifetime--linked-core-b5-c006), [Management pagination](#management-shared-dependencies-and-core-boundary-coverage), [CTAP sequences](#narrow-shared-dependencies-and-windows-successor), [OTP lifetime](#yubiotp-shared-dependencies), [HSM X003](#core-hsm-x003--lease-or-explicitly-serialize-the-complete-public-workflow), [PGP CORE-004](#pgp-core-004--whole-workflow-and-notification-result-ownership), [PIVCORE-002](#pivcore-002--whole-workflow-lifetime-and-terminal-recovery); SD raw/reset occurrences stay [local](#security-domain-coverage-ledger-and-exclusions) | Exchange drain does not lease later parsing, additional exchanges or callbacks. Choose reentrancy/disposal/caller-serialization policy once at the mechanism owner; preserve distinct applet reset/authentication semantics. |
| `CORE-B5-C005`, `CORE-B6-C002/C006/C007`, `CORE-C-C005/C006/C007/C020` independent copies and contained owners | [OATH](#oath-shared-dependencies), [WebAuthn](#webauthn-shared-dependencies-and-core-boundary-coverage), [Management](#management-shared-dependencies-and-core-boundary-coverage), [CTAP](#narrow-shared-dependencies-and-windows-successor), [SD hidden owner](#core-sd-x003--the-hidden-response-owner) and [parameters](#sd-dep-002--parameter-containment-and-one-lifetime-owner), [OTP one-shot boundary](#core-otp-x004--retain-one-shot-output-through-teardown), [HSM X002](#core-hsm-x002--supported-final-owner-clearing-and-independent-copies), [PGP KDF aliases](#pgp-imp-003--kdf-aliases-and-independent-secret-copies), [PIV owner table](#piv-narrow-shared-dependencies-and-windows-successor) | Actual final-owner clearing can be resolved while earlier reports/APDU/TLV/SCP/provider copies remain separate. Choose transfer/borrowed/contained-owner contracts and prove actual last use. Public metadata is not automatically secret; no universal erasure claim. |
| Core credential/keyboard owners, `CORE-C-C001–C004`, `CORE-C-C013`, `CORE-B4-C010`; CTAP-XREF-003 | [OATH reader rows](#oath-shared-dependencies), [CTAP-IMP-006](#ctap-imp-006--pin-units-and-input-enforcement), [PGP-CORE-001](#pgp-core-001--reader-units-and-card-profile), [OTP mapping](#yubiotp-shared-dependencies), [PIV source acquisition](#pivimp-006a--protect-acquisition-and-every-independent-source-copy) and [generic reader referral](#piv-narrow-shared-dependencies-and-windows-successor) | UTF-16 reader units, code points, UTF-8 bytes and scan codes differ. Applet PIN/password profiles and immutable/provider source copies retain their own contracts. Resolved confirmation/redirected-input fixes remain excluded. |
| Core notification/logging owner, `CORE-C-C008` and applicable B6-C014; CTAP X020 / WA X004 | [CTAP X020](#core-ctap-x020--cleanup-when-terminal-notification-logging-throws), [WA notification referral](#webauthn-shared-dependencies-and-core-boundary-coverage), [SD diagnostics](#sd-dep-005--selected-error-provenance-and-diagnostics), [OTP response/logging](#yubiotp-shared-dependencies), [PGP workflow](#pgp-core-004--whole-workflow-and-notification-result-ownership), [HSM diagnostics](#yubihsm-auth-shared-dependencies-and-successor-boundaries) | Select primary/callback/logger exception precedence without skipping cleanup. Normal callback-failure clearing is not a throwing-logger oracle; header-only logs do not prove opaque-provider redaction. |
| Core B5-C002/C003, B6-C001/C003/C011/C012 framing, cancellation and recovery | [WA timeout](#wa-imp-003--timeout-input-and-success-race-policy), [Management wire rows](#management-shared-dependencies-and-core-boundary-coverage), [CTAP framing](#narrow-shared-dependencies-and-windows-successor), [SD wire card](#core-sd-x007--empty-query-le-and-protected-chaining), [OTP polling](#yubiotp-shared-dependencies), [HSM reset](#core-hsm-x001--reset-and-authorization-state-are-not-channel-recovery), [PGP reset](#pgp-imp-005--reset-status-postcondition-and-channel-state), [PIV workflow](#pivcore-002--whole-workflow-lifetime-and-terminal-recovery) | Logical cancellation is not native interruption, release or a hard deadline. Preserve status/framing domains and scoped historical fixes. Uncertain secure graphs require the selected fresh-reopen policy, not plain SELECT as automatic recovery. |
| Core A-C015/B5-C001 version policy, Core Feature/FirmwareVersion owners; B6 configuration owners | [OATH-DEC-007](#oath-dec-007--secure-channel-gate-provenance), [Management](#management-shared-dependencies-and-core-boundary-coverage), [CTAP-IMP-002](#ctap-imp-002--initialization-version-and-firmware-policy), SD-DEP-003 → [SD-IMP-005](#sd-imp-005--reset-options-and-retained-reauthentication-inputs), OTP-DEP-004 → [OTP shared rows](#yubiotp-shared-dependencies), HSM-DEP-001 → [HSM X004](#core-hsm-x004--effective-version-is-policy-not-a-physical-reading), [PGP-CORE-003](#pgp-core-003--version-fallback-and-effective-profile), [PIVIMP-003](#pivimp-003--qualify-firmware-and-product-policy-without-guessing-a-new-gate) | Detected, cached, fallback and effective version differ; major-zero/override are host policies. Metadata, device support, CTAP edition, SCP floor and present reachability are not interchangeable. |
| Core B6 SCP/profile/authority owners, especially `CORE-B6-C007/C009/C010/C015` | [OATH qualification](#oath-secure-reset-and-discovery-qualification), [Management SCP rows](#management-shared-dependencies-and-core-boundary-coverage), [CTAP shared rows](#narrow-shared-dependencies-and-windows-successor), [SD setup authority](#sd-dep-001--setup-history-host-authentication-and-authority), [HSM shared rows](#yubihsm-auth-shared-dependencies-and-successor-boundaries), [PGP shared rows](#openpgp-shared-dependency-and-boundary-ledger), [PIV profile](#piv-narrow-shared-dependencies-and-windows-successor) | Selection/configuration order, setup history, contained keys, host authority and live channel health remain separate. Pin exact SCP03/SCP11/ISO/vendor editions and authorized device configuration before conformance or secure-reset claims. |
| Core B4 native/producer/platform owners; B1-C006/C009 transaction/log-after-open; additive Windows B4-C001/C007 successor | [CTAP Windows limits](#narrow-shared-dependencies-and-windows-successor), [OTP Windows limits](#yubiotp-shared-dependencies), [Management](#management-shared-dependencies-and-core-boundary-coverage), [SD producer qualification](#security-domain-device-and-producer-qualification), [HSM](#yubihsm-auth-shared-dependencies-and-successor-boundaries), [PGP](#openpgp-shared-dependency-and-boundary-ledger), [PIV](#piv-narrow-shared-dependencies-and-windows-successor) | Superseded built-in Windows HID mechanisms are not current bugs. Managed owner/staging prerequisites do not prove driver release, loaded producer/ABI, JIT/AOT, PC/SC or other-OS/physical behavior. SmartCard-only applets do not acquire a new HID defect backlog. |
| Core A metadata owners `CORE-A-C003/C004/C005/C014/C016` and adjacent A-C006 | [MGMT-XREF-001](#mgmt-xref-001--core-fips-tag-decoding-referral), [MGMT-XREF-002](#mgmt-xref-002--core-flagged-form-factor-referral), [Management snapshot/filter rows](#management-shared-dependencies-and-core-boundary-coverage), OTP-DEP-005 → [OTP support decision](#otp-dep-005--dispose-of-relocation-uncertainty-by-surface) | Core owns decoder/carrier/filter policy; Management owns actual configuration/support use. Metadata FIPS capability does not establish OTP mode or product certification. The support note is context, not another bug. |
| Core C018/C019 provider/ARKG math and catalog owners | [CTAP profile/provider rows](#narrow-shared-dependencies-and-windows-successor), [WA experimental dependencies](#webauthn-shared-dependencies-and-core-boundary-coverage), [PGP boundary algorithms](#openpgp-shared-dependency-and-boundary-ledger), [PIV algorithm admission](#pivimp-007a--admit-algorithm-shaped-output-before-reporting-completion) | CTAP wire/draft labels, Core math/key DER, signature DER and applet slot admission have distinct owners. Pin actual vectors, Rust comparator and installed provider; catalog entries do not prove device support or production suitability. |
| Tests.Shared/Core harness owners, including `CORE-A-C013`, `CORE-B2-C005`, B5-C008; shared migration owner `oath-pilot-and-fixes-wave-002:DOC-002` | [OATH inherited tests](#core-oath-x005--inherited-test-resource-and-restoration-work), [Management factory test](#core-mgmt-x003--bound-factory-and-reset-oracles), [CTAP test rows](#ctap2-coverage-and-exclusions), [SD test ledger](#security-domain-coverage-ledger-and-exclusions), [OTP tests](#otp-imp-011--strengthen-oracles-and-independent-cleanup), [HSM tests](#hsm-imp-006--meaningful-oracles-and-independent-test-cleanup), [PGP tests](#pgp-imp-016--meaningful-oracles-and-failure-cleanup), [PIV tests](#pivimp-008--make-tests-survive-their-own-failing-assertions) | Local behavioral oracles and shared authorization/row selection/drain remain separate. Historical successes, skips or comment repairs are not stronger current assertions. DOC-002 remains external documentation hygiene: no parser run, edit or closure here. |

Cross-applet inherited source keys keep their **original applet owner**. In particular,
PGP-CORE-002 routes to [PGP-IMP-003](#pgp-imp-003--kdf-aliases-and-independent-secret-copies) and
[PGP-CORE-004](#pgp-core-004--whole-workflow-and-notification-result-ownership);
SD-DEP-004 routes through the [SD ledger](#security-domain-coverage-ledger-and-exclusions);
OTP-DEP-001/003 retain the [OTP ledger](#yubiotp-complete-coverage-ledger-and-exclusions);
HSM-DEP-002/003 retain the [HSM ledger](#yubihsm-auth-coverage-ledger-and-exclusions);
and PIVCORE-001/003 retain the [PIV qualification/owner ledger](#piv-coverage-ledger-inherited-referrals-and-exclusions).
The version and workflow rows above route the other recurring inherited keys. CTAP-DEC-002 remains
[one CTAP disclosure decision](#ctap-dec-002--canonical-native-selected-user-disclosure-decision), not
a second WebAuthn issue. SAMPLE-A004 in PIV and OpenPGP denotes distinct unified-CLI callers; preserve
their different source anchors rather than merging evidence.

Older routed aliases remain nonadditive: `audit-v2-20261022-20261031:multi-key-exclusions` refers to
the shared membership/physical bounds, `audit-v2-20261022-20261031:deviceinfo-FIPS-residual` to the
Management/Core metadata referrals, `audit-v2-20261022-20261031:#4` to CTAP-IMP-030 / WA-IMP-017,
and `audit-v2-20261022-20261031:#22` to PIVCORE-001. The OATH inherited IMP-004/005/010 aliases retain
their resolved production scopes and selected residual qualification; they do not add open IMP cards.

### Review workflow and acceptance

1. Choose the next applet in the tracker. Read its context/map and select a card; do not preload all
   other applets' snippets. Keep the original ID, severity rationale, certainty, no-brainer boundary,
   Python comparison and BEFORE/AFTER applicability together during review.
2. Confirm the intended contract and every remaining facet. Record **accepted**, **changes requested**,
   **decision pending**, or **rejected with scoped reason**, with reviewer, date and exact card/field.
   Partial acceptance must leave the other facets explicit. A referral is accepted at its canonical
   owner, not counted again at each occurrence. User acceptance authorizes no implementation by itself.
3. Load the named source window or frozen handoff only when needed to settle that card. If source has
   moved from the pinned C# baseline, record the new revision and changed mechanism; do not silently
   convert an old BEFORE excerpt into current evidence. Keep Python at its named pin or record a new
   comparison identity. Python precedent does not settle .NET ownership or normative/device behavior.
4. Check acceptance conditions against the selected alternative. Literal proposed C# is still uncompiled;
   schematic flow names are not APIs. Qualification cards may correctly mark code BEFORE/AFTER **N/A**,
   but must retain a concrete future procedure and evidence/cleanup boundary. Return missing fields or
   ambiguous source-ID routing for correction rather than assuming closure.
5. Record user-review disposition in the tracker only after the actual review. Stop after that applet.
   Do not mark another applet reviewed because it shares a dependency. The recorded independent review
   covers ledger-level consistency and a targeted semantic/source sample; it is neither full semantic
   certification, user acceptance nor an implementation review. Load remaining review groups lazily for
   the selected applet; their unsampled scope is not evidence of missing known finding IDs.

If implementation is later separately authorized, close a selected engineering facet only with accepted
intent, exact changes, meaningful behavior/original-owner tests, actual command outcomes and independent
code/documentation review. Physical/profile closure additionally needs authorized device identity,
firmware, transport, OS, state, executed outcomes and independent persistent/secret cleanup. An outer
canceled wait, successful managed fake, skipped test, proposed snippet or historical run closes none of
those broader claims. Use repository build/test skills in that future task; none were run for this master.

### Integration boundary and next owner

Integration reconciles the nine **existing ledgers**, the original inherited/boundary identity sets and
the named Windows-successor limits without re-auditing applet source or importing all Core candidates.
The companion [review inputs](pass-2-applet-master-coverage.md) and [independent bounded review](pass-2-applet-master-review.md)
distinguish static/ledger checks and targeted source evidence from unsampled semantic scope. Author-time
source/snippet receipts remain historical records, not full independent certification. Remaining
per-applet review groups are lazy human-review followups, not newly discovered backlog items or missing
known finding IDs; no all-nine-app deep review is required by this correction.

**Next owner: the parent coordinator requests bounded reviewer closure of R1's correction. Stop here.**
All user review remains unstarted, all implementation deferred, and all proposed code uncompiled.
