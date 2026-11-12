# Revision 2 — verified citation and applicability corrections

This addendum supersedes only the R1–R4 passages identified in `../review.md`. The original editor
`../citations.md`, original diff, reports, source investigations and review records remain byte-exact
historical inputs. Their imprecise combined request/response locators are not repeated as current authority.
All other original citation qualifications remain in force. No new source retrieval or execution occurred.

## R1 — exact GlobalPlatform locators and variant roles

**Verified:** retained SCP11 **1.3.0.13 Public Review**, August 2023:

| Passage | Exact locator / printed page | Scope |
| --- | --- | --- |
| Identifier/parameter coding | §7.1.1/Table 7-2, p. 45 | Tag 90 parameters; this section is not handshake processing |
| MUTUAL AUTHENTICATE | §7.6, pp. 55–58 | a/c command; request data and processing in §7.6.2.3 |
| a/c response data | §7.6.3.1/Table 7-18, p. 58 | SD SHALL return tag 5F49 then receipt 86; a returns ephemeral SD key, c returns static SD key |
| INTERNAL AUTHENTICATE | §7.7, pp. 60–62 | b command; request data in §7.7.2.3 incorporates §7.6.2.3 processing with explicit b key-agreement modifications |
| b response data | §7.7.3.1/Table 7-22, p. 62 | SD SHALL return ephemeral SD key 5F49 then receipt 86 |

The common parser comments now cite both response sections; the b-only malformed-tag test cites only
§7.7.3.1/Table 7-22. No a/c test coverage is inferred from that fixture. The constructor-admission versus
fixed processing profile, trust selection and first correct-MAC a/c APDU qualifications are unchanged.

**Verified:** retained SCP03 **1.1.2 Public Release**, March 2019:

- §7.1.1.6/Table 7-3, p. 34: response SHALL contain 10-byte diversification data, 3-byte key information,
  8-byte card challenge and 8-byte cryptogram, plus a conditional 3-byte sequence counter. The counter is
  present for pseudo-random challenge generation. Thus 29 bytes is the implementation's minimum prefix,
  not an exact universal response size. Source remarks and the short-response regression comment now agree.
- §6.2.3, p. 24: SCP03 mandates a MAC on EXTERNAL AUTHENTICATE and explicitly sets its MAC chaining value
  to 16 zero bytes. This is the initial-chain locator; §6.2.4 describes subsequent command-MAC generation.

Retained text checked: SCP11 lines 1993–2016, 2417–2609, 2647–2743; SCP03 lines 939–961 and
1397–1435. Source byte identities are unchanged and recorded in `receipt.json`.

## R2 — null firmware reaches processor admission, not a support branch

`PcscProtocol.InitializeScpAsync` passes freshly built base processors to `ScpInitializer.InitializeScpAsync`.
Without Configure, the response receiver's firmware is null. `ScpInitializer.cs:57–59` rejects that receiver
with ArgumentException before its try/switch and `Supports` checks. Both unknown-firmware tests assert an
arbitrary exception and conditionally exclude firmware-threshold text only for NotSupportedException.
They neither require the actual ArgumentException type nor prove support-branch/handshake entry.

The comments are corrected without changing identifiers, assertions or null-firmware policy. This is a
code-path assertion-scope correction, not a normative GP requirement. `ScpExtensionsTests.cs` moves from
retain to edit in successor path/candidate decisions; parameter-lifetime and stronger-oracle work stay held.

## R3 — selected security levels are part of the claims

SCP03 1.1.2 Public Release §6.2.5, pp. 26 and 28, explicitly permits R-MAC to be optional and switched
on/off. **When R-MAC is selected**, the card's 9000/62xx/63xx responses carry that protection, except
EXTERNAL AUTHENTICATE. Error statuses return only status, without protection. Core's SCP03 initializer
selects **0x33**, which includes C-MAC, C-DECRYPTION, R-MAC and R-ENCRYPTION; see its constant and
EXTERNAL AUTHENTICATE P1, and the original §7.1.2.3/Table 7-6, p. 35. This makes the selected rule
applicable to this path rather than every SCP03 security level.

§6.2.6, p. 29, expressly applies **when both C-DECRYPTION and C-MAC are required**. Under that condition,
the off-card entity SHALL advance the counter for protected commands and SHALL apply no encryption to
an absent command data field. The source comments now retain this condition. Current padding of empty
input remains an implementation/profile discrepancy, not an endorsed synchronization requirement.

The instruction-only INS82 exemption, counter mutation/notification gap, synthetic signed errors and
chaining/response-copy concerns remain unchanged. No phase gate, crypto behavior or device claim is fixed.
Retained text checked: SCP03 lines 1023–1030, 1131–1166, 1217–1224 and 1490–1504. SCP11's separately
selected security levels remain governed by the inspected review §5.3.2, not blanket applet authorization.

## R4 — agreement input and KDF output have different widths

`Scp11X963Kdf.GetSharedSecret` checks 32 bytes for each P-256 agreement and concatenates them into
**64 bytes**. `DeriveSessionKeys` passes that input to X9.63 and requests `5 * 16 = 80` output bytes,
split into receipt key, S-ENC, S-MAC, S-RMAC and DEK. The corrected comment states both stages. This
follows actual length checks, allocation and output slices; no ECDH, digest or KDF computation ran.

## Evidence limits retained

No finding contradicted the checked code or retained passages. The review's corrections are accepted for
these exact documentation scopes, not as implementation approval. Final SCP11 1.4/vendor-profile clauses,
code-cited SCP03 1.1.2.6 Public Review clauses, original NIST/capture provenance and runtime/native/device
qualification gaps remain explicit. Original candidate/proposal identities and all M/P/E/Q/B holds survive.
