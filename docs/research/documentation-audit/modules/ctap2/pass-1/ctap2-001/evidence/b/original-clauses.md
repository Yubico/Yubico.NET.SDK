# Investigator B — original reference excerpts

Access date: **2026-11-03**. These are focused transcriptions from original publications,
not claims of certification, device execution, or SDK conformance. Ellipses omit unrelated text.
Section numbers below belong to the specified revision, not to a later CTAP document.

## S21 — CTAP 2.1 errata

Title: *Client to Authenticator Protocol (CTAP)*. Publication masthead: **Proposed Standard,
June 21, 2022**. Exact revision:
https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-errata-20220621.html

§2: “As well as sections marked as non-normative, all authoring guidelines, diagrams, examples,
and notes in this specification are non-normative. Everything else in this specification is normative.”
MUST/SHOULD/MAY retain their RFC2119 strength. No reference was automatically upgraded to 2.3.

### S21-encoding — §6.5.1 / §6.5.5.5–6

Anchors: `#authnrClientPin-pin-composition`, `#settingNewPin`, `#changingExistingPin`.

Platform MUST baseline:

> Minimum PIN Length: 4 Unicode characters
>
> Maximum PIN Length: UTF-8 representation MUST NOT exceed 63 bytes
>
> PIN are in Unicode normalization form C.
>
> PIN MUST NOT end in a 0x00 byte

Authenticator MUST baseline:

> Minimum PIN Length: 4 code points.

The following note permits a greater authenticator minimum. §6.5.5.5 platform steps use
the advertised `minPINLength`, defaulting to four code points if absent. A platform
SHOULD display a too-short/too-long error; that is not the SDK's present byte-only check.

§6.5.5.5 request encoding, normative platform action:

> newPinEnc: the result of calling encrypt(shared secret, paddedPin) where paddedPin is newPin
> padded on the right with 0x00 bytes to make it 64 bytes long. (Since the maximum length of
> newPin is 63 bytes, there is always at least one byte of padding.)

§6.5.5.6:

> pinHashEnc: The result of calling encrypt(shared secret, LEFT(SHA-256(curPin), 16)).
>
> pinUvAuthParam: the result of calling authenticate(shared secret, newPinEnc || pinHashEnc).

Authenticator action in both flows drops trailing zero bytes and enforces current minimum /
additional constraints, returning PIN_POLICY_VIOLATION. This is **zero padding**, not PKCS#7.

### S21-negotiation — §6.5.5 / §6.5.5.4

Anchors: `#authnrClientPin-cmd-dfn`, `#gettingSharedSecret`.

Parameter definition: protocol is **Optional** in the general command table:

> PIN/UV protocol version chosen by the platform. This MUST be a value supported by the
> authenticator, as determined by the pinUvAuthProtocols field of the authenticatorGetInfo response.

> Platforms obtain a shared secret for each transaction. ... If there are subsequent
> authenticatorClientPIN transactions, a new sharedSecret is generated every time.

> The platform selects a mutually supported PIN/UV auth protocol by considering the list of
> protocols supported by the authenticator ... If there are multiple mutually supported protocols,
> and the platform has no preference, it SHOULD select the one listed first in pinUvAuthProtocols.

Actor: platform; MUST supported value, SHOULD first-listed only **if no platform preference**.
The authenticator returns INVALID_PARAMETER for unsupported selected protocol. Preferring V2
is therefore permitted policy; switching to subcommand 0x05 is not protocol negotiation.

### S21-key — §6.5.5 / §6.5.6 / §6.5.7

Anchors: `#authnrClientPin-cmd-dfn`, `#pinProto1`, `#pinProto2`.

Both directions' keyAgreement definition:

> This COSE_Key-encoded public key MUST contain the optional "alg" parameter and MUST NOT
> contain any other optional parameters.

§6.5.6 ECDH algorithm:

> Parse peerCoseKey as specified for getPublicKey, below, and produce a P-256 point, Y.
> If unsuccessful, or if the resulting point is not on the curve, return error.
>
> Let Z be the 32-byte, big-endian encoding of the x-coordinate of the shared point.
>
> Return SHA-256(Z)

getPublicKey parameters: `1 (kty) = 2 (EC2)`, `3 (alg) = -25 (although this is not the
algorithm actually used)`, `-1 (crv) = 1 (P-256)`, `-2/-3` each a 32-byte big-endian coordinate.
The COSE alg label **does not mean V1 uses HKDF**.

§6.5.6:

> pinUvAuthToken, a random, opaque byte string that MUST be either 16 or 32 bytes long.
>
> Return the AES-256-CBC encryption of demPlaintext using an all-zero IV. (No padding is
> performed as the size of demPlaintext is required to be a multiple of the AES block length.)
>
> Return the first 16 bytes of the result of computing HMAC-SHA-256 with the given key and message.

§6.5.7:

> This section provides a PIN/UV auth protocol that is intended to aid FIPS [CMVP]
> certification of authenticators.
>
> The length of the pinUvAuthToken for PIN/UV auth protocol two MUST be 32 bytes.
>
> Return HKDF-SHA-256(salt = 32 zero bytes, IKM = Z, L = 32, info = "CTAP2 HMAC key") ||
> HKDF-SHA-256(salt = 32 zero bytes, IKM = Z, L = 32, info = "CTAP2 AES key")

The following **note** states that two separate invocations cannot be replaced by one L=64 invocation.
Normative V2 encrypt steps discard first 32 key bytes, generate a random 16-byte IV,
AES-CBC encrypt without padding, and return `iv || ct`. Authenticate uses first 32 bytes
of a longer key and returns full HMAC-SHA-256.

### S21-scope — §6.5.5.7 / §6.5.5.7.1–3

Anchors: `#gettingPinUvAuthToken`, `#getPinToken`,
`#getPinUvAuthTokenUsingPinWithPermissions`, `#getPinUvAuthTokenUsingUvWithPermissions`.

Permission table (normative): `mc=0x01`, `ga=0x02`: RP ID Required;
`cm=0x04`: RP ID Optional, restricting credential-management operations to that RP when supplied;
`be=0x08`, `lbw=0x10`, `acfg=0x20`: RP ID Ignored.

> When a pinUvAuthToken is used with an operation that tests user presence, it is updated to
> remove all permissions except lbw.
>
> If authenticatorClientPIN's getPinToken subcommand is invoked, default permissions of mc and
> ga (value 0x03) are granted ... Other pinUvAuthToken permissions can only be acquired by
> providing the permissions parameter to ... (0x09) or ... (0x06).

Following **note**: legacy default-permission token acquires its permissions RP ID on first mc/ga use.
§6.5.5.7.1 forbids permissions/rpId parameters on legacy 0x05. A `noMcGaPermissionsWithClientPin`
true exception suppresses default mc/ga; legacy preview operations remain a separate compatibility case.

§6.5.5.7.2 MUST implementation condition: both `clientPin` and `pinUvAuthToken` true.
Authenticator unauthorized checks: cm requires credMgmt true; be requires bioEnroll present;
lbw requires largeBlobs true; acfg requires authnrCfg true; mc/ga forbidden when
noMcGaPermissionsWithClientPin true. Undefined bits ignored. Platform **SHOULD**, in a note,
request only necessary permissions (do not relabel the note as a MUST).

§6.5.5.7.3:

> This subCommand MUST be implemented if the authenticator returns both uv and pinUvAuthToken
> option IDs set to true in the authenticatorGetInfo response.

UV token permissions be/acfg have separate `uvBioEnroll` / `uvAcfg` requirements;
unconfigured built-in UV returns NOT_ALLOWED. The following note permits platform PIN fallback
on UV_BLOCKED or exhausted UV retries when clientPin is true; it is not automatic SDK behavior.
Built-in UV may establish UP; PIN issuance calls `beginUsingPinUvAuthToken(userIsPresent: false)`.

### S21-state — §6.5.2.1–2 / §6.5.5.6–7

Anchors: `#authnrClientPin-globalState-puat`, `#authnrClientPin-globalState-retries`.

> Authenticators MUST allow no more than 8 retries but MAY set a lower maximum.
>
> Once the pinRetries counter reaches 0, both ClientPin as well as built-in user verification
> are disabled and can only be enabled if authenticator is reset.
>
> Once the uvRetries counter reaches 0, built-in user verification MUST be disabled and can
> only be re-enabled if the authenticator is reset or the correct clientPIN is provided via ...
> getPinUvAuthTokenUsingPinWithPermissions or getPinToken subCommands.

Wrong-PIN processing returns PIN_BLOCKED at zero; otherwise three consecutive mismatches
return PIN_AUTH_BLOCKED “indicating that power cycling is needed for further operations”;
otherwise PIN_INVALID. Do not conflate temporary and exhausted lockout.

§6.5.5 response field `powerCycleState`:

> If the field is omitted, no information is given about whether a power cycle is needed or not.
>
> This field is only valid in response to a getRetries request ...

§6.5.5.3 UV retries response supplies uvRetries, not powerCycleState.

Successful PIN change and ordinary token issuance reset tokens for all supported protocols:

> (I.e. all existing pinUvAuthTokens are invalidated.)

§6.5.2.1 initial usage limits default at most USB 30s / NFC 19.8s; authenticator MAY use shorter
values. Max usage period SHOULD default at most ten minutes, MAY be shorter. Optional rolling
timer and user-present timeout exist. These are authenticator state, not byte-array lifetime or
an SDK timer guarantee. §6.5.8 notes possible replay/reordering within one token's scope.

### S21-uv-option — §5 / §6.1 / §6.2

> Note: clientPin is not a built-in user verification method.

The first statement is a **note** in terminology. Platform requirements in §6.1/§6.2:

> Platforms MUST NOT include the "uv" option key if the authenticator does not support built-in
> user verification.
>
> Platforms MUST NOT include both the "uv" option key and the pinUvAuthParam parameter in the same request.

§6.1 UV option default false; the platform-actions section is explicitly non-normative and even
contains a false-UV path. Do not claim that false necessarily fails or that omission proves UV=0.
An authenticated token can cause UV=1 regardless of an omitted uv option.

§6.6 original CTAP requirement is ten seconds from power-up **for authenticators without a display**.
That is not proof of a particular YubiKey's product window. Transport support and required user
interaction are separate conditions. Reset/dependent product-window adjudication belongs to A/F.

## S22 — persistent read-only permission, not a replacement for S21

Title: *Client to Authenticator Protocol (CTAP)*, **Proposed Standard, July 14, 2025**.
https://fidoalliance.org/specs/fido-v2.2-ps-20250714/fido-client-to-authenticator-protocol-v2.2-ps-20250714.html

§6.5.5.7 `#gettingPinUvAuthToken` / `#pcmr`:

> pcmr — Persistent Credential Management Read Only — 0x40 — Ignored
>
> This allows the persistentPinUvAuthToken to be used with the authenticatorCredentialManagement
> command. If this permission is present, the persistentPinUvAuthToken can only be used for
> Read Credential Management operations on Credentials.

§6.5.5.7.2–3 unauthorized condition:

> pcmr: perCredMgmtRO is false or absent, or any other pinUvAuthToken permission is requested.

§6.4 `#getinfo-percredmgmtro` states true enables requesting pcmr; false/absent does not.
§6.5.2.2 `#authnrClientPin-globalState-persistent-puat` assigns the persistent token a permission
set, separate from ordinary token timer/UP/UV state. §6.5.5.7.2–3 returns encrypted persistent
token when pcmr is requested, rather than issuing an ordinary scoped token.

§6.4 `#getinfo-encidentifier`:

> ... AES-128-CBC encryption of (128-bit device identifier) using HKDF-SHA-256(salt = 32 zero
> bytes, IKM = persistentPinUvAuthToken, L = 16, info = "encIdentifier").

Exact 2.2 HTML contained no `encCredStoreState`. Its protocol/vendor/revision provenance is an
explicit E/A follow-up dependency, not established by the 2.2 permission citation. Initial
original research plus one focused original follow-up completed; no speculative version upgrade.

## NET10 — framework lookup and limits

Context7 resolved `.NET` and queried `/websites/learn_microsoft_en-us_dotnet` for HKDF / ECDH.
ECDH Create(ECParameters) result describes importing the supplied key. HKDF retrieval twice returned
mostly unrelated PBKDF2 material; those results are **not** evidence for HKDF salt behavior.
Focused official page consulted:
https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.hkdf.derivekey?view=net-10.0

> Performs the key derivation HKDF Expand and Extract functions.

The Span overload has explicit `ReadOnlySpan<byte> salt`; the array overload documents omitted salt
defaulting to hash-output-length bytes. The page alone does not explicitly settle an **empty Span**
salt's internal representation. .NET v10.0.0 original HKDF source consulted:
https://raw.githubusercontent.com/dotnet/runtime/v10.0.0/src/libraries/System.Security.Cryptography/src/System/Security/Cryptography/HKDF.cs
The Span overload forwards salt to DeriveKeyCore; the platform partial implementation was not followed.

No empty-salt defect is asserted: HMAC-SHA-256 key padding makes an empty salt and 32 zero bytes
equivalent at the HMAC block-key level. This is cryptographic reasoning, not an executed .NET vector.
Likewise no exact cross-platform ECDH import exception type is asserted; import occurs outside the
SDK's derivation catch. Framework exception/point-validation characterization remains a bounded gap.
