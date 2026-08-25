<!-- Copyright 2026 Yubico AB

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License. -->

# FIDO2 forward-compatibility audit and handoff

**Date:** 2026-08-25
**Branch:** `bugfix/fido2-credmgmt-unsupported-cose-key`
**Open PR:** <https://github.com/Yubico/Yubico.NET.SDK/pull/585>
**Status:** one fix shipped to that PR; 12 further defects identified, none built.

This document exists so the remaining work can be picked up cold. It records
what was fixed, what was found, what was decided and why, and what was
deliberately *not* done. The design-rationale sections exist to stop decisions
being relitigated.

---

## 1. Origin

Reported by Jonas Markström: `Fido2Session.EnumerateCredentialsForRelyingParty`
throws and aborts enumeration of *all* credentials for a relying party when any
one credential's public key uses a COSE algorithm the SDK does not model.

Two corrections to the original report, both confirmed by reading the code:

- The exception is `System.NotSupportedException` ("The requested algorithm is
  not supported"), **not** `Ctap2DataException`.
- The message quoted in the report — `Ctap2CborUnexpectedKey`, *"An unexpected
  key was encountered in a CBOR map; expected to find {0} (name '{1}')"* — is
  thrown from exactly one place in the SDK: `Fido2/RelyingParty.cs:128`. That is
  **not** on the reported code path. See defect D1 in section 5.

The report quoted the unformatted resx template (`{0}`, `{1}`), which suggests
source analysis rather than a captured stack trace. **A stack trace was
requested and had not arrived when this document was written.** Whoever picks
this up should chase it — it determines whether the user's actual problem is D1
rather than the COSE issue.

---

## 2. What shipped (PR #585)

Two commits on `bugfix/fido2-credmgmt-unsupported-cose-key`:

- `89effa2b` — `fix(fido2): tolerate unmodeled COSE key types when enumerating credentials`
- `94093fd9` — `docs(fido2): generalize wording for unmodeled COSE key types`

**Change:** added `CoseUnsupportedPublicKey` (sealed, internal constructor,
carries the original encoding plus the reported key type and algorithm) and an
internal `CoseKey.CreateOrUnsupported`. Applied at all three data-bearing
`CoseKey.Create` call sites: `CredentialUserInfo.cs`,
`Commands/CredentialManagementData.cs`, and `AuthenticatorData.cs`.
`Commands/ClientPinResponse.cs` was deliberately left on strict `Create`.

**Behavior change shipped:** `AuthenticatorData.CredentialPublicKey` now returns
the sentinel instead of `null` for an unmodeled key. Code detecting such a
credential via a null check must test for `CoseUnsupportedPublicKey` instead.
`null` on that property now means unambiguously "no attested credential data".

**Known incomplete:** see D7/D8 in section 5. PR #585 as it stands does not
fully deliver its own guarantee.

---

## 3. Hardware findings — the reported scenario did NOT reproduce

Run against three connected devices. **This section exists so nobody repeats
the experiment.**

| Serial | Firmware | Extensions | credMgmt |
|---|---|---|---|
| 25555459 | 5.4.3 | credProtect, hmac-secret | NotSupported |
| 103 | 5.8.0 | ..., **previewSign** | True |
| 125 | 5.8.0 | ..., **`sign`** | True |

**Note serials 103 and 125: same firmware version, different extension name.**
`previewSign` has already been partly renamed to `sign` in the field. This is
why the documentation shipped in PR #585 describes the *mechanism* rather than
naming the extension. Do not reintroduce extension names into permanent API
documentation.

What the hardware confirmed:

- A previewSign-generated key **is** an unmodeled COSE key:
  `kty = -65537, alg = -65700`. After the fix it decodes to
  `CoseUnsupportedPublicKey` with byte-exact raw preservation. The sentinel is
  therefore validated against real authenticator bytes, not just synthetic CBOR.

What the hardware **disproved**:

- `EnumerateCredentialsForRelyingParty` returned the credential cleanly, with
  `CoseEcPublicKey kty=2 alg=-7`. The ARKG key rides inside the *extension
  output*; it is **not** stored as its own discoverable credential.
- The other route is closed too: requesting the unmodeled algorithm as the
  credential's own algorithm (`alg = -65539`, `rk = true`) is **rejected by the
  YubiKey** with `Fido2Exception`.
- `AddPreviewSignGenerateKeyExtension` has no discoverability flag — its
  `PreviewSignOptions` are user-presence/user-verification policy only.

**Conclusion:** on firmware 5.8.0 there is no route via the public SDK API to a
discoverable credential whose own public key is an unmodeled COSE type. The code
defect is real and worth fixing regardless, but it is probably not what the
reporter hit. D1 is the stronger candidate.

**Safety note for anyone re-running this:** `FidoSessionIntegrationTestBase`'s
constructor (`Yubico.YubiKey/tests/integration/Yubico/YubiKey/Fido2/FidoIntegrationTestBase.cs`)
**deletes every discoverable credential on the device** and asserts the PIN is
the integration-test PIN. Do not point it at a personal key. The probes used for
the results above deliberately did not inherit it, restricted writes to a serial
allowlist, and cleaned up after themselves. They were not committed.

---

## 4. Audit scope and method

139 `.cs` files under `Yubico.YubiKey/src/Yubico/YubiKey/Fido2/` were enumerated
and every decoding file read. `Yubico.Core` was searched — **no FIDO2 response
decoding exists there**; all CBOR parsing uses `System.Formats.Cbor` from
`Yubico.YubiKey`.

**A defect** is code that decodes data *received from a YubiKey* and hard-fails
on something it does not recognize, when the data it *does* recognize would have
been sufficient for the caller.

**Not defects** (explicitly considered and rejected): validation of
caller-supplied input; genuinely malformed CBOR; missing *required* fields; a
recognized discriminator paired with a structurally wrong payload; and `default`
arms over `CtapStatus`, which correctly funnel into a `Fido2Exception` carrying
the raw code.

**Exemplary patterns already in the codebase** — use these as templates:

- `Fido2/AuthenticatorOptions.cs` — `default: return OptionValue.Unknown`.
- `Fido2/Cbor/CborMap.cs` — `default` arms fall back to `RawCborValue`.
- `Fido2/UserEntity.cs` and `Fido2/CredentialId.cs` — `CborMap<string>`, unknown
  keys silently ignored.
- `Fido2/Commands/CredentialManagementData.cs` — `RawData` + `UnknownFields`
  (PR #508).
- `Fido2/CredentialUserInfo.cs` — `TryGetCredentialManagementField` (PR #508).
- `Fido2/AuthenticatorData.cs` — `EncodedCredentialPublicKey` (PR #468).

**Not audited:** CTAPHID transport framing in
`Yubico.YubiKey/src/Yubico/YubiKey/Pipelines/`. Explicitly out of scope by
decision. An unrecognized CTAPHID command byte has not been assessed.

---

## 5. Defects

The **Confirmed** column records how the finding was checked. `direct` means the
file and line were read and the defect confirmed by hand. Every line reference
below was verified against the tree at commit `94093fd9`; re-check them if the
files have moved since.

| ID | Confirmed | Location | Defect | Severity |
|---|---|---|---|---|
| D1 | direct | `Fido2/RelyingParty.cs:124` | `default: throw` on any relying-party map key but `id`/`name`. Kills `EnumerateRelyingParties()`. **Only site emitting the message in the bug report.** | HIGH |
| D2 | direct | `Fido2/Commands/ClientPinResponse.cs:97` | `default: throw` on any clientPIN response key but `0x01`–`0x05`. Breaks **every** PIN/UV path, and therefore MakeCredential, GetAssertions, and all credential management | HIGH |
| D3 | direct | `Fido2/MakeCredentialData.cs:188` | `Statement as PackedAttestationStatement ?? throw`. Any non-`packed` `fmt` breaks `MakeCredential` | HIGH\* |
| D4 | direct | `Fido2/AttestationStatement.cs:136` | `ContainsOnlyKeys` rejects a `packed` attestation statement carrying any extra key, degrading it to `UnknownAttestationStatement`, which then trips D3 | HIGH |
| D5 | direct | `Fido2/AttestationStatement.cs:181,214,236` | `ContainsExactKeys` does the same for `fido-u2f`, `apple`, and `none`. Silent loss of the typed properties rather than a throw | LOW–MED |
| D6 | direct | `Fido2/Fido2Session.Pin.cs:1422` | Three separate bugs in one line — see section 6 | HIGH |
| D7 | direct | `Fido2/Cose/CoseEcPublicKey.cs:211-220` | The decode constructor assigns through the public property setters, which run `ValidateCurve`/`ValidateLength` and throw `ArgumentException`. **This escapes `CreateOrUnsupported`, whose catch is `NotSupportedException`-only** — a hole in the PR #585 fix | MED |
| D8 | direct | `Fido2/Cose/CoseEdDsaPublicKey.cs:128-129` | Same pattern for non-Ed25519 OKP curves. `Ed448` is already a `CoseEcCurve` member but is rejected | MED |
| D9 | direct | `Fido2/Commands/ClientPinResponse.cs:78` | Still uses strict `CoseKey.Create` for the `keyAgreement` field | MED |
| D10 | direct | `Fido2/AuthenticatorData.cs:408` | `GetCredProtectExtension()` rejects credProtect values outside 1–3 | MED |
| D11 | direct | `Fido2/AuthenticatorInfo.cs:460,520` | `AsDictionary<bool>()` is all-or-nothing; a single non-boolean `options` value throws `InvalidCastException` from the **session constructor** | MED |
| D12 | direct | `Fido2/Cbor/CborMap.cs:162-189` | `ReadArray<T>` rejects the whole array when one element does not convert (throw at line 189); 13 call sites | LOW |
| D13 | direct | `Fido2/Commands/GetKeyAgreementResponse.cs:53` | Misleading `Ctap2MissingRequiredField` for a field that is present but not an EC key | LOW |
| D14 | direct | `Fido2/PinProtocols/PinUvAuthProtocolBase.cs:177` | Duplicate of D13 one layer down; line 184 also hard-codes P-256 regardless of what the device reported | LOW |

\* **D3's severity is unconfirmed.** Nobody has established whether YubiKey
firmware ever returns a non-`packed` `fmt`. `MakeCredentialParameters` has no
attestation-format preference input, so the format is entirely the
authenticator's choice today. The presence of
`AuthenticatorInfo.AttestationFormats` (getInfo key `0x16`) and four modeled
format classes is circumstantial evidence that non-packed is expected. **A
firmware owner must confirm before this is scheduled.**

---

## 6. D6 in detail

```csharp
// Fido2Session.Pin.cs:1422 — called unconditionally from the constructor (Fido2Session.cs:273)
var protocol = AuthenticatorInfo.PinUvAuthProtocols?[0] ?? PinUvAuthProtocol.ProtocolOne;
```

Three separate defects:

1. **It only inspects index 0.** CTAP 2.1 defines `pinUvAuthProtocols` as a list
   *in order of decreasing authenticator preference*. A device advertising
   `[3, 2, 1]` means "I prefer 3, but I also support 2 and 1." The SDK throws and
   refuses to construct a session against a device that supports both protocols
   the SDK implements. Unambiguously a bug.
2. **`?[0]` does not guard against an empty list.** The null-conditional operator
   guards null only. A non-null empty list yields `ArgumentOutOfRangeException`
   rather than the intended `NotSupportedException`. Only reachable on a device
   that does not conform to CTAP, which requires the array to be non-empty when
   present.
3. **It throws from the constructor**, which is disproportionate *and* defeats
   the SDK's own escape hatch. `AuthProtocol` is consumed only by PIN/UV paths;
   reading `AuthenticatorInfo`, calling `Reset`, and a non-UV `GetAssertion`
   never need it. Worse, `SetAuthProtocol(PinUvAuthProtocolBase)` exists and is
   documented as *"Overrides the default PIN/UV Auth protocol"* — but it is an
   **instance method**. If the constructor throws you never obtain an instance,
   so the designed override is unreachable in exactly the scenario it exists for.

**Chosen remedy** (see section 7 for the reasoning): walk the list for the first
implemented protocol; handle the empty case; and when the device advertises only
unmodeled protocols, assign an **internal**
`UnsupportedPinUvAuthProtocol : PinUvAuthProtocolBase` sentinel that reports the
advertised protocol number and throws a precise `NotSupportedException` from each
of its eight abstract members. The session then constructs, non-PIN work
proceeds, `SetAuthProtocol` becomes reachable, and `AuthProtocol` stays
non-nullable.

---

## 7. Decisions already made — do not relitigate without new information

**Sentinel over nullable.** For `CredentialUserInfo.CredentialPublicKey`,
nullable was rejected. The property is non-nullable and CTAP mandates field
`0x08` for `enumerateCredentials`, so nullable is both the wrong model and a
source-breaking annotation change that would force null checks on 100% of
callers to guard a case that fires for a fraction of a percent of credentials.
The sentinel costs nothing unless you meet an unmodeled key, and it degrades
better: `is CoseEcPublicKey` skips gracefully, `.Algorithm`/`.Type` return the
real reported values instead of a `NullReferenceException`, a hard cast fails
with a message naming the actual type, and consumers building with nullable
reference types disabled would get no warning at all under the nullable design.
The same reasoning drives the D6 remedy.

**`CreateOrUnsupported` catches only `NotSupportedException`** — to be widened to
`ArgumentException` for D7/D8, and nothing else. `Ctap2DataException` must keep
propagating: malformed CBOR, a missing key type or algorithm, and a modeled
algorithm paired with the wrong key type are corrupt data, not future values.

**Public `CoseKey.Create` stays strict.** Making it return the sentinel would
silently change a documented public throw contract. `Create` is strict;
`CreateOrUnsupported` is lenient.

**No `TryCreate`.** With a sentinel the method cannot fail, so the boolean would
duplicate `key is CoseUnsupportedPublicKey`; none of the three call sites branch
on it; and every `Try*` method in this codebase pairs `false` with a null or
default out-parameter, several of them annotated `[MaybeNullWhen(false)]`, which
an always-populated out-parameter would invert.

**Decode-side tolerance must never become encode-side fabrication.** Preserved
unknown fields must **not** be re-emitted from any `CborEncode()`.
`RelyingParty.CborEncode()` is fed to the authenticator from
`MakeCredentialParameters`; re-emitting authenticator-originated keys during
registration is wrong, and CTAP2 canonical ordering makes it fiddly besides.

**`whats-new.md` is release-time only.** Every edit to it comes from a release
commit. Do not touch it in a feature or bugfix PR; put behavior-change notes in
the PR body for the release manager to pick up.

**Hard constraint set by the owner: no breaking changes, and nothing becomes
nullable.** This is what blocks D3, and what forces remedy (a) over remedy (b)
for D7.

---

## 8. Proposed remaining work

Three stacked PRs. **None of this has been built.**

```text
develop
 └─ bugfix/fido2-credmgmt-unsupported-cose-key   → PR #585 (open) + amendment
     └─ bugfix/fido2-unknown-map-keys             → D1, D2, D4, D5, D9
         └─ bugfix/fido2-pinuvauth-protocol       → D6
```

### Stack 1 — amend PR #585 (D7, D8)

Widen `CreateOrUnsupported`'s catch to absorb `ArgumentException` and return the
sentinel.

Two remedies were considered; **only (a) is permitted under the
no-breaking-changes constraint**:

- **(a) widen the catch.** Confines the change to the tolerant path;
  `CoseEcPublicKey`, `CoseEdDsaPublicKey`, and public `Create` are untouched.
  **Chosen.**
- (b) stop the decode constructors running their validators. This changes decode
  behavior for every caller including direct `Create` users, and would permit a
  `CoseEcPublicKey` to exist with an out-of-range `Curve`. **Rejected.**

Consequence: `CoseUnsupportedPublicKey`'s XML documentation must widen from
"algorithm this SDK does not model" to "cannot be represented by a modeled type",
because you can now receive it for a modeled algorithm on an unmodeled curve.

Criteria: an unmodeled curve, an unmodeled coordinate length, and a non-Ed25519
OKP curve each yield the sentinel with raw bytes preserved; enumeration returns
all credentials when one is affected; **anti:** public `Create` still throws for
all three; **anti:** `CoseEcPublicKey.cs` and `CoseEdDsaPublicKey.cs` are
unmodified; **anti:** null input still throws `ArgumentNullException` rather than
being absorbed into a sentinel.

### Stack 2 — unknown map keys (D1, D2, D4, D5, D9)

- **D1** — rewrite `RelyingParty`'s decode constructor onto `CborMap<string>`,
  matching `UserEntity` and `CredentialId`, and add
  `TryGetUnknownField(string, out ReadOnlyMemory<byte>)` mirroring
  `CredentialUserInfo.TryGetCredentialManagementField`.
  **`RelyingParty` has no unit tests today** — a new `RelyingPartyTests.cs` is
  required, as is `EnumerateRpsResponseTests.cs`.
  Note that `RelyingParty` is decoded at exactly one site, in
  `Commands/CredentialManagementData.cs`, so a single fix covers
  `EnumerateRpsBeginResponse`, `EnumerateRpsGetNextResponse`, and
  `CredentialManagementResponse`.
- **D2** — have the `default:` arm collect into an unknown-fields dictionary, and
  add `RawData` and `UnknownFields` to the public `ClientPinData`, exactly as
  `CredentialManagementData` does.
- **D4/D5** — drop `ContainsOnlyKeys`/`ContainsExactKeys` and keep only the
  `Contains(...)` required-field checks. Then **delete both helpers from
  `CborMap`**, along with their unit test in `CborMapTests.cs` — they are used
  nowhere else, so this removes the hazard from the toolkit permanently.
  `CborMap<TKey>` is `internal`, so this is not a public API change.
- **D9** — swap the `keyAgreement` decode in `ClientPinResponse` to
  `CreateOrUnsupported`.

**Anti-criteria:** `RelyingParty.CborEncode()` on a decoded relying party that
carried unknown fields must emit **only** `id` and `name`; and `CborEncode()` for
`new RelyingParty("x")` must remain byte-identical to its current output.

### Stack 3 — PIN/UV protocol selection (D6)

As described in section 6. The key criterion, and the point of the exercise: with
a device advertising only unmodeled protocols, `SetAuthProtocol(myProtocol)` must
succeed and subsequent PIN operations must use it. **Anti:** `AuthProtocol`
remains non-nullable; the sentinel type is `internal`; and `Dispose` on a session
holding the sentinel does not throw.

### Not scheduled

| ID | Disposition |
|---|---|
| D3 | **Blocked twice.** The fix makes three public properties nullable, which violates the constraint, and reachability needs a firmware owner |
| D10, D11, D12, D13, D14 | All non-breaking and buildable. Deferred only on reviewer-load grounds. Pull them forward freely |

---

## 9. Open items

1. **Jonas's stack trace** — outstanding. It determines whether the real fault is
   D1 rather than the COSE issue. Chase it before promising that PR #585 solves
   his problem.
2. **D3 reachability** — needs a firmware owner to confirm whether a non-`packed`
   attestation format is ever returned.
3. **D6 tail case** — the sentinel approach was chosen, but "device advertises
   only unmodeled protocols" is product-visible behavior; confirm before building.
4. **CTAPHID transport** (`Pipelines/`) has never been assessed for this class of
   defect.
5. **Version** — PR #585 adds a public type and changes shipped behavior. A minor
   bump (1.18.0) was recommended over a patch release; not yet ratified.

---

## 10. Reproducing the audit

Sweep for the two throwing patterns:

```bash
grep -rn "Ctap2CborUnexpectedKey\|CborUnexpectedMapTag" --include='*.cs' Yubico.YubiKey/src/
grep -rn "ContainsExactKeys\|ContainsOnlyKeys" --include='*.cs' Yubico.YubiKey/src/
```

Both were exhaustive at the time of writing: two `default: throw` sites (D1 and
D2) and four `Contains*Keys` sites, all four in `AttestationStatement.cs`.

---

## Glossary

- **ARKG** — Asynchronous Remote Key Generation.
- **CBOR** — Concise Binary Object Representation (RFC 8949).
- **COSE** — CBOR Object Signing and Encryption (RFC 8152).
- **CTAP / CTAP2** — Client to Authenticator Protocol.
- **CTAPHID** — CTAP over the USB Human Interface Device transport.
- **OKP** — Octet Key Pair, the COSE key type used for Edwards curves.
- **PIN/UV** — Personal Identification Number / User Verification.
- **RP** — Relying Party.
- **SDK** — Software Development Kit.
