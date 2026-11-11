# Packet C original-source review register

**Checkpoint provenance:** the pending-reading list below is historical. The completed
source reading and bounded final qualifications are in [final-source-register.md](final-source-register.md).

**Access date:** November 11, 2026. Exact original URLs, downloaded-byte SHA-256 values
and fetch outcomes are in [fetch-manifest.json](fetch-manifest.json) and
[focused-fetch-manifest.json](focused-fetch-manifest.json). Original bytes are retained
beside these manifests. A fetched document is available evidence; only the passages below
received substantive inspection in this checkpoint. Independent review remains outstanding.

## Consequential passages inspected

| Source and exact revision/status | Passage and actor | Strength and application to this source |
| --- | --- | --- |
| RFC 8410, August 2018, Standards Track | §3, retained lines 178–214: the four Ed/X algorithm identifiers and their parameters; §4 lines 216–245: SPKI BIT STRING; §7 lines 349–386: private wrapping | AlgorithmIdentifier parameters **MUST** be absent for these OIDs. This supports the Core absent-parameters rule, shared key/signature OIDs where applicable, octet-aligned public bytes, and nested OCTET STRING private encoding. It supplies no device/profile/provider capability guarantee. |
| RFC 7748, January 2016, IRTF Informational | §4.1 lines 180–222: fields, orders, cofactors and birational forms; §5 lines 406–435: scalar decode | Both 25519 forms use field `2^255−19`; subgroup order starts at `2^252` plus a positive smaller term, hence 253-bit order. Scalar masking belongs to X25519 decoding at the operation boundary. Imported/exported 32-byte storage need not be rewritten. These mathematical/algorithm definitions are distinct from a PKCS#8 storage or applet-support claim. |
| RFC 8032, January 2017, IRTF Informational | §4 and §5.1, lines 432–500 | Pure/Hash variants and Ed25519 parameters distinguish intrinsic SHA-512 use from a separately selected hash field, and distinguish Edwards construction from ordinary ECDSA. The inspected table is parameter evidence, not an independent full EdDSA implementation review. |
| RFC 8017, November 2016, PKCS #1 v2.2, Informational | §3.1–3.2, lines 446–551 | Valid RSA keys have positive components, a modulus made from distinct odd primes, exponent/range conditions, and inverse/CRT relations. Core checks a subset of algebraic relations. This source supports saying exactly which relations are checked; it does not support advertising full RSA validation. |
| RFC 8017, same revision | §9.1.1, lines 2323–2377 | EMSA-PSS requires `emLen >= hLen + sLen + 2`; hash, MGF and salt are separate parameters. SHA-512 plus a 64-byte salt needs 130 bytes, explaining the catalog's RSA-1024 exclusion. This establishes encoded-message capacity, not provider support or protocol key policy. |
| RFC 5915, June 2010, Informational | §3–§5, lines 100–207 | EC private scalar OCTET STRING has subgroup-order-derived fixed width. The ASN.1 fields are optional in syntax, but conforming implementations **MUST** include parameters and **SHOULD** include publicKey, with the stated public-key-distribution exception. Receivers **SHOULD** handle BER and DER. The unencrypted structure itself provides no confidentiality. Core's deliberate inner-parameter omission needs profile qualification; BCL import success alone does not erase the RFC's prose requirement. |
| RFC 5480, March 2009, Standards Track | §2.1.1 lines 192–242; §2.2 lines 349–374 | PKIX `id-ecPublicKey` parameters **MUST** be present and named-curve; implicit/explicit parameter alternatives are forbidden in this profile. Uncompressed public points **MUST** be supported, compressed points **MAY** be supported, and hybrid points are forbidden. This supports Core's documented uncompressed subset without pretending compressed-point rejection is universally required. |
| RFC 4648, October 2006, Standards Track | §3.2–3.5 lines 178–272; §6 lines 420–500; §10 lines 650–685 | Encoders **MUST** pad unless a referring specification says otherwise, and canonical encoders set pad bits to zero. Decoder rejection of nonzero pad bits is **MAY**. Base32 alphabet, 40-bit quanta and terminal counts match the inspected codec and literal vectors. Core separately documents lowercase, ASCII-space and omitted-padding acceptance; that local policy is not presented as a universal RFC decoder requirement. |
| RFC 9053, August 2022, Informational | §2.1 lines 224–296 | COSE ES256/384/512 select hash/signature combinations. Pairing SHA-256/P-256 etc. is suggested, while this document's supported ECDSA curve set is P-256/P-384/P-521 and future extensions may add others. COSE signatures concatenate fixed-width R/S; key `kty`, optional `alg` and `key_ops` have operation-specific checks. Core structural ECDSA applicability on secp256k1/Brainpool does not establish that a given key/`-7` COSE combination is valid. The secp256k1 COSE operation has its own registered identifier. |
| RFC 9459, September 2023, Standards Track | §5–§5.2 lines 206–275 | COSE AES-CBC uses a 16-octet IV, **MUST** use the cited CMS padding, **MUST** use companion authentication/integrity, and has key/algorithm/operation checks. The CBC IDs were registered Deprecated to avoid accidental unauthenticated use. These are COSE encoder/decryptor requirements, not a requirement to add padding to SCP's different wire profile. |
| IANA COSE XML snapshot, `<updated>2026-08-25</updated>` | Complete selected algorithm records and key-type/curve records printed from retained XML | Numeric mappings inspected: ES256/384/512, EdDSA, Ed25519 `-19`, ECDH-ES+HKDF-256, RSA/PSS/CBC variants; curve 1–8 and Brainpool 256/258/259; kty 1–4. Zero is reserved rather than a registered Unknown algorithm/key/curve. A row is authoritative registration metadata, not recommendation of every use or SDK/device capability. The registry names and controller/reference fields are retained in the original. |
| RFC 9864, October 2025, Standards Track, updates 7518/8037/9053 | §4.2.2–§4.4 lines 456–569 | Registry updates deprecate polymorphic ECDSA/EdDSA IDs. Deprecated means a preferred replacement **SHOULD** be used in new deployments unless documented operational/regulatory constraints prevent migration; Prohibited has **MUST NOT** strength. These are different statuses. This audit preserves pinned existing applet profiles and proposes no automatic ID migration. |
| `draft-bradleylundberg-cfrg-arkg-09`, July 7, 2025, Internet-Draft, intended Informational, expired January 8, 2026 | Header/status lines 5–66; §4.1 lines 1021–1045 | Draft-09 is work in progress, not a final RFC. ARKG-P256 selects secp256r1, SHA-256 and the named RFC 9380 suite; IKM inputs **SHOULD** have at least 256 bits entropy. These inspected parameters do not establish that all Core derivation steps conform or that its Rust-derived vectors are independent normative oracles. |
| OpenSSL tag `openssl-3.0.0`, `doc/man3/BN_bn2bin.pod` | Complete 124-line manual | BN_bin2bn allocates when `ret` is NULL and returns NULL on error; BN_bn2binpad returns written length or -1 for a short buffer. This supports checking actual ARKG wrapper outcomes. It is not evidence that the packaged NativeShims forwards every behavior unchanged or that every target uses this OpenSSL version. |
| .NET runtime tag `v10.0.0`, `System.Console/Console.cs` | Lines 156–185 and 722–728 | `KeyAvailable` explicitly rejects redirected input; `ReadLine` calls `In.ReadLine` and returns a string without cancellation input. This supports the inspected Core fallback/cancellation qualification. No current installed runtime identity is inferred from this reference tag. |
| .NET runtime tag `v10.0.0`, `UTF8Encoding.cs` | Lines 40–109 | `Encoding.UTF8` default construction and default fallback select U+FFFD replacement; the strict constructor selects exception fallback. Core's default UTF-16-char collection therefore differs from CLI Rune admission. No Unicode-policy change is inferred from BCL permissiveness. |
| .NET runtime tag `v10.0.0`, `ECDsa.cs` | Lines 1041–1042, with nearby overload documentation | The overload used by PrivateKeyConsistency requests IEEE P1363 fixed-field concatenation. This is signature-format evidence only; it is distinct from DER SPKI key format and COSE profile admission. No BCrypt/SecKey/OpenSSL platform implementation was executed. |

## Framework orientation and bounded unresolved work

Context7 was used with `/dotnet/docs` for Memory/IMemoryOwner lifetime, Console/UTF-8
orientation, logging factory/redaction, and C# 13+ ref-struct use in async methods. It
returned the official memory ownership rules and the C# rule that spans cannot cross
suspension, rather than a ban on all async-method locals. Logging redaction requires
explicit classification/registration; a bare `ILoggerFactory` is not a redaction layer.
OpenSSL Context7 orientation emphasized reading individual return-value contracts;
the exact pinned BN manual above provides the consequential contract.

These are orientation results, not replacements for the SDK implementation or actual
installed-runtime identities. This checkpoint performed no .NET installation/version
probe and makes no assumption that historical SDK 10.0.403 output or newer 10.0.404
source context identifies the runtime/provider executed on a later validation host.

The following originals were fetched and identity-recorded but still need substantive
passage review: RFC 5208/5958 container/version/attribute details, RFC 5639 curve and
OID tables, RFC 9380 expansion/reduction steps, and the OpenSSL EC_POINT/EVP_MAC
manuals. RFC 8017 AlgorithmIdentifier appendix details, RFC 8032 key-encoding/seed
clauses, NIST/SEC original curve tables and the full ARKG draft derivation remain
open. No full mathematical catalog verification is claimed from the portions above.

Exact .NET platform-provider sources for the current macOS scalar-copy limitation,
BCrypt parameter requirements and CoreFoundation/SecKey lifetime behavior remain a
bounded provider-source task. Native shim internals and loader/export availability
belong to B4's exact package/runtime context. No generic OpenSSL or BCL description
settles those actual packaged calls. GlobalPlatform final wire specifications remain
B6-owned; C made no renewed download attempt for that already recorded source gap.

Fetches and retained URLs are separated from semantic reading in their manifests.
The independent reviewer should reread the cited consequential clauses from originals,
especially RFC 5915's prose requirements and RFC 9864's status/actor distinction.
