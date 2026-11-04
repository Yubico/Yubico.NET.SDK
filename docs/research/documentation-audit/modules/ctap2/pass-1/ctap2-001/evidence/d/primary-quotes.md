# D primary evidence — 2026-11-03

Access date for every URL below: **2026-11-03**. These are focused excerpts, not a replacement
for independent review of originals. Whitespace has been normalized; spelling is preserved.
No authenticator command was issued. Source baseline: `5b09a9b6942ded3913b5c9340c3e126617e61a82`.

## S1 — exact standard and strength

Title: **Client to Authenticator Protocol (CTAP)**, CTAP **2.1 Proposed Standard with
2022-06-21 errata**, at
https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-errata-20220621.html

Publication heading: “Proposed Standard, June 21, 2022”. Status: “This document was published
by the FIDO Alliance as a Proposed Standard Specification.” The URL retains the original
2021-06-15 publication directory; do not mistake that for the errata document's heading date.

§2: “As well as sections marked as non-normative, all authoring guidelines, diagrams, examples,
and notes in this specification are non-normative. Everything else in this specification is
normative.” RFC2119 keywords apply. Consequently, a normative algorithm's uncapitalized
instruction is not merely optional, but text specifically labeled **Note** is not a MUST.
This investigation did not substitute a later 2.2/2.3 revision for these clauses.

## S2 — stateful enumeration (§6 Authenticator API; #stateful-commands)

“The authenticator MAY maintain state based on the assumption that each stateful command is
exclusively preceded by either another instance of the same command, or by the corresponding
state initializing command, and no more than 30 seconds will elapse between such commands.”
“If this pattern is violated then the authenticator MAY fail any stateful command with the
error CTAP2_ERR_NOT_ALLOWED.” Exclusively preceded means no intervening authenticator operation;
this assumption can be global even across transport streams.

“An authenticator MUST discard the state for a stateful command command if the pinUvAuthToken
that authenticated the state initializing command expires since the stateful commands do not
themselves always verify a pinUvAuthToken.” State **SHOULD NOT** survive power cycles.
Actor: authenticator. This permits constraints; it does not promise every device keeps state
for 30 seconds or that SDK transport admission locks an entire multi-command enumeration.

## S3 — GetInfo support versus state (§6.4, #authenticatorGetInfo)

`bioEnroll`: present/true = command supported and at least one enrollment provisioned;
present/false = command supported and none provisioned; absent = unsupported.
`userVerificationMgmtPreview` has the analogous provisioned/not-provisioned distinction for
prototype command 0x40. `ep`: present/true = capable/enabled, present/false = capable/disabled,
absent = unsupported. `authnrCfg`, `credMgmt`, `setMinPINLength`: present/true = supported;
false or absent = unsupported. `uvBioEnroll` and `uvAcfg` separately advertise permission
acquisition using built-in UV. Actor: authenticator response and interpreting platform.

## S4 — biometric wire and workflow (§6.7, #authenticatorBioEnrollment)

Subcommands: `enrollBegin=0x01`, `enrollCaptureNextSample=0x02`,
`cancelCurrentEnrollment=0x03`, `enumerateEnrollments=0x04`, `setFriendlyName=0x05`,
`removeEnrollment=0x06`, `getFingerprintSensorInfo=0x07`.
Input envelope `getModality=0x06` is Boolean and **MUST** be true when used.
Timeout belongs in `subCommandParams`, key `timeoutMilliseconds=0x03`, unsigned integer.

§6.7.3: platform sensor-info request includes modality 0x01 **and** subCommand 0x07.
Response contains fingerprintKind (2), maxCaptureSamplesRequiredForEnroll (3), and
maxTemplateFriendlyName (8). It does not define a max-template-count field at key 4.

§6.7.4: platform obtains a token with `be` permission. Begin authentication is
`authenticate(pinUvAuthToken, fingerprint(0x01) || enrollBegin(0x01) || subCommandParams)`.
Next authentication is
`authenticate(pinUvAuthToken, fingerprint(0x01) || enrollCaptureNextSample(0x02) || subCommandParams)`.
Begin response contains templateId (4), lastEnrollSampleStatus (5), remainingSamples (6).
Next response lists lastEnrollSampleStatus (5) and remainingSamples (6), **not** templateId.
Platform uses the template identifier it received from **begin**. Continue until zero,
unrecoverable error, or platform cancellation. Authenticator cancels any unfinished enrollment
when beginning a new one. These are normative algorithm instructions, not quoted MUST keywords.

§6.7.5: platform sends modality 1 and cancelCurrentEnrollment 3, with no authorization fields
specified. Authenticator “cancels current ongoing enrollment, if any, and returns CTAP2_OK.”
This is a CTAP command distinct from cancellation of a .NET wait or CTAPHID_CANCEL.

§6.7.6: authentication is `fingerprint(0x01) || enumerateEnrollments(0x04)`.
“If there are no enrollments existing on authenticator, it returns CTAP2_ERR_INVALID_OPTION.”
Success returns `templateInfos(0x07)`, an array for **all** available enrollments. Each nested
TemplateInfo has required byte-string templateId **key 1**, optional string friendly name **key 2**.

§6.7.7–8: rename/remove authentication includes modality || subcommand || parameter map.
Friendly-name maximum size **SHOULD** be the lesser of 64 bytes and maxTemplateFriendlyName;
the authenticator checks its advertised maximum and can return INVALID_LENGTH. Removing an
unknown template returns INVALID_OPTION. Identifiers/name/status are not biometric sensor samples.

Feedback table: `0x0B=FP_EXISTS` (fingerprint already exists), `0x0C=unused`,
`0x0D=NO_USER_ACTIVITY` (did not touch/swipe), `0x0E=NO_USER_PRESENCE_TRANSITION`
(did not lift finger). `0x00–0x0A` are good/high/low/left/right/fast/slow/poor/skewed/short/merge.
Note about not expiring the token upon completion is labeled **Note**; do not turn its
“SHOULD NOT expire” wording into an unconditional lifetime guarantee.

## S5 — credential management (§6.8, #authenticatorCredentialManagement)

Standard command 0x0A; subcommands 1–7 match current CredManagementSubCommand constants.
§6.8.2–3: platform obtains `cm` token and **MUST NOT** supply permissions RP ID for
metadata or RP enumeration. Authenticator checks `cm` and no associated RP ID.
§6.8.4–6: credential enumeration/delete/update permit an unbound token or RP binding matching
the requested RP/credential. Actor checking token scope: authenticator, not this SDK helper.

§6.8.2 response's maximum remaining count: “Note that this number is an estimate as actual
space consumed to create a credential depends on various conditions such as which algorithm
is picked, user entity information etc.” This is explanatory non-normative **Note**, not a
fixed-capacity or reservation guarantee.

§6.8.3–4 begin replies provide totals; platform performs total-1 next requests when total >1.
Next request lists only the subCommand; replies omit total. Both are explicitly stateful.
No credentials at begin returns NO_CREDENTIALS. The spec does not prescribe this SDK's nullable
total fallback or silently returning one entry if a begin response lacks its total.

§6.8.5: unknown credential returns NO_CREDENTIALS; otherwise “Delete the credential and return
CTAP2_OK.” **Note**: platforms **SHOULD** also delete associated large blobs; non-normative
note is not automatic device cleanup. §6.8.4 similarly notes platform garbage collection.

§6.8.6: “If the supplied user parameter’s id field is not the same as the matching credential’s
id field then return CTAP1_ERR_INVALID_PARAMETER.” Replace name/displayName; absent or empty
fields are removed. No matching credential = NO_CREDENTIALS; insufficient storage = KEY_STORE_FULL.
Actor: authenticator. This does not rename the RP's account or replace the user handle.

§6.8.7: authenticator **MAY** truncate stored display RP identifiers; if storing, **MUST** keep
at least 32 bytes. Truncation applies only to returned RP entity; authenticator **MUST NOT** use
truncated identifiers for comparisons. Use returned rpIDHash for enumeration rather than hashing
possibly truncated display text.

## S6 — config (§6.11, #authenticatorConfig)

Opening **Note** contains “Platforms MUST NOT invoke this command unless the authnrCfg option
ID is present and true”; preserve that guidance while retaining §2's non-normative-note distinction.
Normative GetInfo option definitions separately identify support.
Defined subcommands: enterprise=1, toggle=2, minPIN=3, vendorPrototype=**0xFF**.
“Authenticators MAY implement none, some, or all currently defined authenticatorConfig subcommands.”
No response map is defined; subsequent GetInfo reads are used to ascertain results.

Platform authentication transcript:
`32×0xff || 0x0d || uint8(subCommand) || subCommandParams`.
Authenticator checks `acfg` when protected by UV or alwaysUv is true. Normative algorithm also
permits skipping UV for toggleAlwaysUv when unprotected and alwaysUv is true, and for other
configuration when unprotected and alwaysUv is disabled. The SDK helper always computes and
sends authentication; “all config operations require a token” is not an unconditional protocol rule.

§6.11.1 enable is idempotent: already enabled -> no action, OK. `ep=false` still means capable.
§6.11.2: if disabling alwaysUv is supported, disable it; otherwise return OPERATION_DENIED.
Comment about supporting disable unless certification forbids it is explicitly a **Note**.
Enable can disable makeCredUvNotRqd; disable restores that option's default. No universal guarantee
that any operation succeeds or that ordinary metadata queries become verified ceremonies.

§6.11.4: newMinPINLength is in **Unicode code points**, applicable to ClientPIN, not any other
PIN functionality. All three parameter fields are optional. Minimum cannot decrease; reset restores
preconfigured minimum. `minPinLengthRPIDs` **MUST NOT** be used without minPinLength extension
support. Absent new length uses current minimum. Nonempty RP lists replace mutable allowed-list
entries, combined with an immutable preconfigured list if present. Empty/absent list does not
establish list clearing. `forceChangePin` or current PIN shorter than new minimum sets forcePINChange.
If no PIN and force=true -> PIN_NOT_SET. PIN_POLICY_VIOLATION blocks token acquisition until changePIN
succeeds; this is not erasure of the current PIN.

## S7 — prototype distinction (§6.12–13)

Prototype 0x40/0x41 are superseded, optional compatibility commands for FIDO_2_1_PRE.
Platforms **MUST NOT** use prototype bio when `bioEnroll` is present, or prototype credential
management when `credMgmt` is present. Prototype feature detection includes FIDO_2_1_PRE and
corresponding preview option. These commands do not require permissions and can use getPinToken.
The current CredentialManagement helper only sends standard 0x0A. No original prototype-specific
wire definition was obtained; do not assume its wire shape excuses standard-command defects.

## S8 — canonical keys (§8 Message Encoding)

“The keys in every map MUST be sorted lowest value to highest.” Same major type: shorter
keys sort earlier; same-length keys sort by byte-wise lexical order. Thus text `id` (2),
`name` (4), `displayName` (11) sort in that order, **not alphabetically**.
Actor: message encoder. Current update-user encoder and several model fixtures use the reverse
ordering for displayName. No compiler or test execution was used to confirm exception timing.

## V1 — Yubico firmware/model evidence, not SDK enforcement

https://docs.yubico.com/hardware/yubikey/yk-tech-manual/yk5-firmware-5.7.html
§CTAP 2.1 Features: “Yubico supported CTAP 2.1 features even before that standard was approved,
for example, credential management introduced in **5.2.1** ... and uvRetries introduced in 5.5.0.”
This proves an early feature introduction, not that the standard 0x0A helper interoperates on 5.2.1.

https://docs.yubico.com/hardware/yubikey/yk-tech-manual/yk5-apps-fido.html
§FIDO2 Extensions Available per Firmware Version table: credential management in 5.2.x–5.8.x;
alwaysUV, minimum PIN length, ForceChangePIN, biometric enrollment/built-in UV in 5.5.x–5.8.x,
not 5.4.x/earlier in those rows; enterprise attestation in 5.6.x–5.8.x.
§CTAP Specifics says CTAP2.1 on 5.7.x+, CTAP2.3 on 5.8.x+.
§Always Require User Verification table says YubiKey 5 FIPS Series cannot disable alwaysUV;
Bio defaults enabled and can toggle. Product table does not manufacture a sensor on a non-Bio key.
§FIDO2 PINs and Fingerprint Templates identifies biometric fingerprint templates **on YubiKey Bio**.
§Enterprise Attestation says custom-configured keys and platform/RP support matter; FIDO reset
disables enterprise attestation and it can be re-enabled.

Limit: current manual has internally inconsistent chronology (e.g., 5.5.x feature table versus
5.7.4 introduction prose) and mixes product series. Initial official search + one full-page
follow-up exhausted the allotted vendor research. Exact minimum standard-command firmware and
alleged YubiKey-specific bio fields/subcommands remain **missing proof**. Do not replace source
feature constants or establish qualification from these coarse rows. No CMVP certificate was
read; the manual's FIPS claims do not certify SDK behavior.

## L1 — framework evidence and limit

Context7 resolve returned `/dotnet/docs` and Microsoft API-library IDs; query for
System.Formats.Cbor Ctap2Canonical ordering returned no matches. Focused official follow-up:
https://learn.microsoft.com/en-us/dotnet/api/system.formats.cbor.cborconformancemode?view=net-10.0
documents Ctap2Canonical: “Maps (major type 5) must contain keys sorted by encoding,” definite
lengths, shortest integers, and no tags. Page displayed System.Formats.Cbor v11 RC package metadata
despite the net-10.0 view. This supports conformance-mode intent, not a local version-specific
runtime experiment. Exact byte ordering is independently grounded in S8.
