# Investigator C — focused primary-source evidence

> **Interpretation erratum (2026-11-04):** the ARKG summary below overstates disagreement on outer
> algorithm -65700. Original draft-10 §§5.1/5.2/8.3 explicitly define it. Its kty=-65537/direct-map
> profile still differs from SDK emission. See ../../review-c.md RC-R1 and ../../correction-c.md;
> original excerpts/interpretation remain preserved rather than silently substituted.

Access date: **2026-11-03**. These are focused quotations, not whole specifications or a
conformance assessment. Section numbers belong to the explicitly named revision. Ellipses omit
unrelated text, not conditions. Source-observed SDK behavior is recorded separately in
`../../investigation-c.md`.

## S20 — CTAP2.0 descriptor and next-assertion exception

**Client to Authenticator Protocol (CTAP), Proposed Standard, 30 January 2019.**
URL: https://fidoalliance.org/specs/fido-v2.0-ps-20190130/fido-client-to-authenticator-protocol-v2.0-ps-20190130.html

§5.2, authenticatorGetAssertion response table, authenticator actor:

> credential (0x01) … Optional … PublicKeyCredentialDescriptor structure containing the credential
> identifier whose private key was used to generate the assertion. May be omitted if the allowList
> has exactly one Credential.

§5.3, authenticatorGetNextAssertion, client/platform and authenticator actors:

> The client calls this method when the authenticatorGetAssertion response contains the
> numberOfCredentials member and the number of credentials exceeds 1.

> If timer since the last call to authenticatorGetAssertion/authenticatorGetNextAssertion is greater
> than 30 seconds, discard the current authenticatorGetAssertion state and return
> CTAP2_ERR_NOT_ALLOWED. This step is optional if transport is done over NFC.

## S21 — CTAP2.1 errata, not an unqualified latest specification

**Client to Authenticator Protocol (CTAP), CTAP2.1 Proposed Standard with 21 June 2022 errata.**
The fetched page's publication heading says **Proposed Standard, June 21, 2022**; its URL retains
the original 15 June 2021 publication directory.
URL: https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-errata-20220621.html

§6.1, authenticatorMakeCredential input/options table, platform actor:

> The array is ordered from most preferred to least preferred and MUST NOT include duplicate entries.

> Platforms MAY send the "up" option key to CTAP2.1 authenticators, and its value MUST be true if
> present. The value false will cause a CTAP2_ERR_INVALID_OPTION response regardless of authenticator
> version.

> Platforms MUST NOT include the "uv" option key if the authenticator does not support built-in
> user verification. Platforms MUST NOT include both the "uv" option key and the pinUvAuthParam
> parameter in the same request.

§6.1, user input definition:

> Note that while an empty account identifier is valid, it has known interoperability hurdles in
> practice and platforms are RECOMMENDED to avoid sending them.

> The user name and display name are OPTIONAL for privacy reasons for single-factor scenarios where
> only user presence is required.

§6.2, authenticatorGetAssertion input/options table:

> A platform MUST NOT send an empty allowList—if it would be empty it MUST be omitted.

> Platforms MUST NOT include both "uv" and pinUvAuthParam parameters in same request.

§6.2.2 response table, authenticator actor:

> credential (0x01) … Required … PublicKeyCredentialDescriptor structure containing the credential
> identifier whose private key was used to generate the assertion.

> User identifiable information (name, DisplayName, icon) MUST NOT be returned if user verification
> is not done by the authenticator.

> FIDO Devices - server-side credentials: … this parameter is OPTIONAL … Authenticators MAY store
> user information inside the credential ID.

> FIDO Devices - discoverable credentials: … at least user "id" is mandatory.

> numberOfCredentials … Optional; defaults to one. This member is required when more than one
> credential is found for an RP, and the authenticator does not have a display or the UV & UP flags
> are false. Omitted when returned for the authenticatorGetNextAssertion method.

> userSelected … Optional; defaults to false. MUST NOT be present in response to a request where
> an allowList was given, where numberOfCredentials is greater than one, nor in response to an
> authenticatorGetNextAssertion request.

§6.3, next assertion:

> It takes no arguments.

> If the credentialCounter is equal to or greater than numberOfCredentials, return
> CTAP2_ERR_NOT_ALLOWED.

> If timer since the last call to authenticatorGetAssertion/authenticatorGetNextAssertion is greater
> than 30 seconds, discard the current authenticatorGetAssertion state and return
> CTAP2_ERR_NOT_ALLOWED. This step is OPTIONAL if transport is done over NFC.

> Call authenticatorGetNextAssertion numberOfCredentials minus 1 times.

§5, stateful-command guidance:

> The authenticator MAY maintain state based on the assumption that each stateful command is
> exclusively preceded by either another instance of the same command, or by the corresponding
> state initializing command, and no more than 30 seconds will elapse between such commands.

> Here, “exclusively preceded” means that no other authenticator operation occurs in between. An
> authenticator MAY assume this globally, even when the transport-specific binding provides for
> independent streams of platform commands.

§8.1, message encoding, encoder actor:

> The keys in every map MUST be sorted lowest value to highest. The sorting rules are:
> If the major types are different, the one with the lower value in numerical order sorts earlier.
> If two keys have different lengths, the shorter one sorts earlier;
> If two keys have the same length, the one with the lower value in (byte-wise) lexical order sorts earlier.

## S23 — CTAP2.3 fields and unchanged relevant conditions

**Client to Authenticator Protocol (CTAP), CTAP2.3 Proposed Standard, 26 February 2026.**
URL: https://fidoalliance.org/specs/fido-v2.3-ps-20260226/fido-client-to-authenticator-protocol-v2.3-ps-20260226.html

§6.1, clientDataHash input:

> Hash of the ClientData contextual binding specified by host. See [WebAuthn].

§6.2.2 response table also marks credential (0x01) **Required**, and repeats the identifying-field
UV prohibition, optional server-side user entity and mandatory discoverable user ID quoted in S21.
§6.1/§6.2 repeat the mutually exclusive uv/pinUvAuthParam rule and empty allow-list omission rule.
These repetitions corroborate those exact clauses; they do not establish whole-version SDK conformance.

§6.1 response table, makeCredential:

> unsignedExtensionOutputs (0x06) … A map, keyed by extension identifiers, to unsigned outputs of
> extensions, if any. Authenticators SHOULD omit this field if no processed extensions define
> unsigned outputs. Clients MUST treat an empty map the same as an omitted field.

§6.2.2 response table, getAssertion:

> unsignedExtensionOutputs (0x08) … A map, keyed by extension identifiers, to unsigned outputs of
> extensions, if any. Authenticators SHOULD omit this field if no processed extensions define
> unsigned outputs. Clients MUST treat an empty map the same as an omitted field.

This investigation did not verify the first revision introducing these fields. Do not turn the
source's “CTAP2.2 / WebAuthn L3” shorthand into an independently verified introduction claim.

## W2 — WebAuthn L2 is a different layer

**Web Authentication: An API for accessing Public Key Credentials — Level 2, W3C Recommendation,
8 April 2021.**
URL: https://www.w3.org/TR/2021/REC-webauthn-2-20210408/

§5.4.3, PublicKeyCredentialUserEntity, RP actor:

> A user handle is an opaque byte sequence with a maximum size of 64 bytes, and is not meant to be
> displayed to the user.

> To ensure secure operation, authentication and authorization decisions MUST be made on the basis
> of this id member, not the displayName nor name members.

> The user handle MUST NOT contain personally identifying information about the user, such as a
> username or e-mail address … The user handle MUST NOT be empty, though it MAY be null.

The CTAP S21/S23 empty-ID allowance and optional display inputs must not be replaced silently with
WebAuthn dictionary requirements. The SDK's constructors and response parser are their own narrower
current API, described in the investigation.

§6.5.1, attested credential data:

> The credential public key encoded in COSE_Key format … using the CTAP2 canonical CBOR encoding
> form. The COSE_Key-encoded credential public key MUST contain the "alg" parameter and MUST NOT
> contain any other OPTIONAL parameters.

> The encoded credential public key MUST also contain any additional REQUIRED parameters stipulated
> by the relevant key type specification, i.e., REQUIRED for the key type "kty" and algorithm "alg".

§6.1.1, signature counter, RP actor:

> A Relying Party stores the signature counter of the most recent authenticatorGetAssertion operation.

> Detecting a signature counter mismatch does not indicate whether the current operation was performed
> by a cloned authenticator or the original authenticator. Relying Parties should address this situation
> appropriately relative to their individual situations, i.e., their risk tolerance.

§8.7, None Attestation Statement Format:

> The none attestation statement format is used to replace any authenticator-provided attestation
> statement when a WebAuthn Relying Party indicates it does not wish to receive attestation information.

The surrounding L2 attestation-type definitions distinguish **Self** from **None** (“no attestation
information is available”). §7.1/§7.2 contain the RP registration/assertion verification procedures;
SDK parsing or VerifyRpIdHash alone is not either complete procedure.

## IANA — identifiers, not device availability

**IANA CBOR Object Signing and Encryption (COSE) registry**, fetched registry last-updated date
**2026-08-25**. URL: https://www.iana.org/assignments/cose/cose.xhtml

COSE Algorithms table:

> ESP256 | -9 | ECDSA using P-256 curve and SHA-256 … [RFC9864, Section 2.1]

> ES256K | -47 | ECDSA using secp256k1 curve and SHA-256 … [RFC8812][RFC9053]

> EdDSA | -8 | EdDSA … [RFC9053][RFC9864] | Deprecated

COSE Elliptic Curves table includes **Ed25519 (6)** and **Ed448 (7)**. Thus the registry's -8 is
not inherently Ed25519-only. W2's WebAuthn credential-key rule is narrower and specifies Ed25519
for -8; keep the actor/layer qualification rather than changing wire identifiers.

## ARKG-10 — an expired work-in-progress revision, not the SDK's wire profile

**Asynchronous Remote Key Generation (ARKG), draft-bradleylundberg-cfrg-arkg-10, Internet-Draft.**
URL: https://www.ietf.org/archive/id/draft-bradleylundberg-cfrg-arkg-10.html

Status passage:

> Internet-Drafts are working documents … It is inappropriate to use Internet-Drafts as reference
> material or to cite them other than as "work in progress."

> This Internet-Draft will expire on 31 August 2026.

§5.1, COSE key type: ARKG public seed; Table 1 and accompanying CDDL:

> An ARKG public seed is represented as a COSE_Key structure [RFC9052] with kty value TBD
> (placeholder value -65537).

> pkbl | -1 | COSE_Key | Required | BL key of ARKG public seed
> pkkem | -2 | COSE_Key | Required | KEM key of ARKG public seed

The CDDL uses **-1: { … }** and **-2: { … }**, direct nested maps, not byte strings containing
CBOR maps. §5.2 identifies **ESP256-split-ARKG** with placeholder **-65539**. This supports the
BL/KEM label mapping and signing-operation distinction, not the SDK's outer kty=2/alg=-65700
profile or its byte-string wrapping. No claim that this draft became a standard or was current
on access date; this fixed revision's stated expiry precedes access.

## .NET CBOR library evidence and unsuccessful retrieval limits

Context7 resolved `/dotnet/docs`; the focused CborWriter/Ctap2Canonical query returned an unrelated
OrderedDictionary example. It is **not** evidence about CBOR behavior. Direct primary Microsoft
documentation was therefore fetched:

https://learn.microsoft.com/en-us/dotnet/api/system.formats.cbor.cborconformancemode?view=net-10.0

> Ctap2Canonical … Ensures that the CBOR data is canonical, as specified by the CTAP v2.0 standard,
> section 6. … Maps (major type 5) must contain keys sorted by encoding.

The WriteEndMap page was also fetched but supplied no useful focused sorting explanation. No
runtime experiment/build/test was performed. The corrected **canonical ordering rule** follows
S21, independently of whether a particular writer reorders insertion order.

Two direct shell extraction attempts were unsuccessful: local BeautifulSoup was unavailable;
stdlib HTTP retrieval received **403 Forbidden**. No dependency was installed and no shell fetch
cache was written. Exa's original-URL extraction supplied the quoted passages. Research was bounded
to initial reads plus focused follow-up for the unresolved descriptor/options/ARKG/library claims.
Unverified vendor profile, publication-introduction and live-device questions remain open.
