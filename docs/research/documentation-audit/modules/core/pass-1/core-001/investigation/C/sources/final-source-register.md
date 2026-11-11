# Final Packet C source qualifications

This register completes the static source work recorded in
[review-register.md](review-register.md). The earlier register remains checkpoint
provenance; its pending-reading list is superseded here. Independent review,
compilation and runtime/native/device qualification remain separate gates.

Original download manifests retain exact URLs, byte hashes, failures and access
dates. Completion fetches additionally record UTC request timestamps. PDF text files
are `pdftotext -layout` reading derivatives of the retained original PDFs, not alternate
editions. RFC section names are the stable citation coordinates; form-feed page breaks
can make Python `splitlines()` numbering differ from LF-only tool line numbers.

## Completed original passage checks

| Original and status | Exact reading and applicable actor/strength | Conclusion and bound |
| --- | --- | --- |
| ITU-T X.690 (02/2021), ISO/IEC 8825-1:2021, Recommendation/International Standard | §8.1.2.1–8.1.2.4: BER identifier sender uses low-tag form for 0–30 and follows high-tag continuation octets; §8.1.3.3–8.1.3.6: definite/indefinite lengths, forbidden FF and sender option to use redundant long-form length octets; §8.3: minimal signed two's-complement INTEGER | Supports current long-length rejection and accepted redundant zero-length octets. C's two-byte tag parser/constructor is a bounded smart-card helper, not a general BER codec. RSA INTEGER bytes must not be reinterpreted as unsigned merely by trimming zeros. Core's chosen definite-only behavior is distinct from general BER's constructed indefinite form. |
| SEC 2, retrieved January 27, 2010 v2.0 PDF from `secg.org/sec2-v2.pdf` | §2.4.1/2.4.2/2.5.1/2.6.1 parameter sextuples and Appendix A.2.1 named-curve OIDs | P-256/P-384/P-521 and secp256k1 widths/orders/OIDs/generator points match the inspected catalog/vector literals. The original PDF retains **“SEC 2 (Draft) Ver. 2.0” page headers**. Identify the exact retrieved copy; do not silently label it a final/current NIST approval document. Its recommendations describe parameter selection, not SDK/device/provider availability. |
| RFC 5639, March 2010, Independent Submission Informational | §3.4, §3.6, §3.7: Brainpool r1 field/order/generator tables; §4.1: complete OID arc; §4.2: X.509 representation actor | Brainpool P256/P384/P512 field and order widths are 256/384/512, coordinates/scalars 32/48/64 bytes; raw uncompressed points use two coordinates plus 04. OIDs end 7/11/13. The existing test comment's §3.4–3.6 range misses P512's §3.7 and should be corrected. RFC5480 governs the cited X.509 AlgorithmIdentifier context; these data are not an applet capability claim. |
| NIST SP 800-186 final publication metadata, February 2023 | Official final page read through webfetch: final history 2023-02-03, planning note 2023-04-17 identifies a §3.2.2.1 issue | Exact final PDF download returned 403; one focused official-page follow-up exposed metadata, while direct retention returned 403. No draft was substituted. Current mathematical rows have independent SEC/RFC evidence; NIST-final table text, updates and approval claims remain a precise bounded external gap. |
| RFC 7517, May 2015, Standards Track | Appendix A.2 original RSA/EC JWK example | Eight RSA fixture byte values were compared by base64url-to-hex source-text conversion. This checks published fixture identity, not primality, modular relations, provider acceptance or historical extraction procedure. See `../vector-provenance.json`. |
| RFC 6979, August 2013, Informational | Appendix A.2.5/A.2.6 published P-256/P-384 private/public keys | Six EC literal values match original hex text. Known matching pairs are independent inputs; this audit ran no ECDSA operation or arithmetic relation experiment. The tests' separate fresh perturbations still depend on their exact admission path. |
| RFC 4493, June 2006, Informational | §4 Example 1: key `2b7e...f3c`, empty message, MAC `bb1d...6746` | Current default-CMAC test's input/output literals match the published example. Source comparison is distinct from executing the test, proving constant time, or validating every CMAC/profile use. |
| RFC 5208, May 2008, Informational; RFC 5958, August 2010, Standards Track, obsoletes 5208 | RFC5208 §5: PrivateKeyInfo version 0, algorithm, private OCTET STRING and optional attributes; RFC5958 §2: v1(0)/v2(1), attributes and publicKey/version relationship | Typed Core decoders implement a narrower attribute-free/version-0 subset. A syntactically valid version value does not validate all following bytes or an all-zero mathematical key. Public/private association checks remain independent. Broad malformed-input XML should name actual AsnContentException/algorithm/version errors accurately. |
| RFC 8017, November 2016, Informational | Appendix A.1/A.1.1/A.1.2, A.2.3/A.2.4, plus previously inspected §3 and §9.1.1 | RSA key OID requires NULL; absent input is Core compatibility tolerance. Two-prime versus multiprime syntax, positive components and CRT fields are separate from selected relation checks. PSS identifies an operation with hash/MGF/salt/trailer parameters; trailer field number 1 represents byte BC. Concrete RSA signature OIDs match the catalog; source editorial wording elsewhere does not turn PSS into encryption. |
| RFC 3279, April 2002, Standards Track; RFC 5758, January 2010, Standards Track | RFC3279 §2.2.3: ECDSA/SHA-1 OID, absent parameters, DER signature SEQUENCE; RFC5758 §3.2: SHA-2 ECDSA OIDs and absent parameters for certificate/CRL issuers/clients | OIDs match `Oids`; DER signature encoding in this certificate profile differs from BCL P1363 default and RFC9053's COSE concatenation. None of those signature formats is an SPKI key format. A key OID alone does not choose signing or ECDH. |
| RFC 3565, July 2003, Standards Track; RFC 3370, August 2002, Standards Track | RFC3565 §4.1/ASN.1 module: AES CBC OIDs .2/.22/.42 with required 16-byte IV in CMS; RFC3370 §5.1: Triple-DES-CBC OID 1.2.840.113549.3.7 and required 8-byte IV | The catalog contains operation OID metadata, not complete CMS encoders. Triple-DES stored key bits/parity and mathematical/security strength remain different units. Historical identifiers are retained without recommending new deployment. |
| RFC 8032, January 2017, IRTF Informational; RFC7748/RFC8410 as in checkpoint register | RFC8032 §5.1.5: 32-octet seed, SHA-512/pruning and compressed public y/sign encoding; RFC8410 §10.1/10.2/10.3: exact public/certificate/private fixtures | Confirms 32-byte seed versus operation scalar and distinct Edwards/Montgomery public encodings. Test vectors from certificate dumps and private examples establish representation bytes, not standalone .NET Ed/X primitive support. |
| ARKG draft-bradleylundberg-cfrg-arkg-09, July 7, 2025, expired Internet-Draft, intended Informational | §2.3 full derivation procedure; §3.1 blinding; §3.2 HMAC adaptation; §3.3 ECDH sub-KEM; §4.1 ARKG-P256 | Compared all corresponding SDK helpers: context length/framing, domain tags, HKDF inputs, 16-byte HMAC truncation, tag/ephemeral-point handle and public blinding structure. §3.2 explicitly permits omitting ctx_sub when the ECDH sub-KEM ignores ctx. Work in progress is the deliberate reference target, not a final standard or authorization to switch drafts. Secret cleanup/native errors and missing generator provenance remain separate concerns. |
| RFC 9380, August 2023, IRTF Informational | §5.2 hash_to_field, §5.3.1 expand_message_xmd, §8.2 P-256 suite | Inspected step ordering, 64-byte Z_pad, length/DST encodings, b0/b1/XOR sequence, two blocks for 48-byte output, reduction modulo subgroup N and suite L=48. Wide-reduction decomposition is structurally equivalent to reducing the 48-byte big-endian integer. No crypto experiment or full side-channel/conformance proof ran. |

The checkpoint IANA/RFC9053/RFC9459/RFC9864 distinctions remain current: registration
is not capability, generic ECDSA applicability is not a valid universal COSE key/alg
pairing, and Deprecated's qualified SHOULD is different from Prohibited's MUST NOT.
No protocol/app applet algorithm or draft profile was automatically upgraded.

## Actual framework/native call scope

Context7 was consulted again for AsnWriter cleanup and platform cryptography. Its
AsnWriter answer did not supply the requested API evidence, so the exact runtime source
was read instead. Platform tables are orientation; source calls and artifact identities
govern consequential qualifications.

| Pinned explanatory source | Actual inspected call path | Result |
| --- | --- | --- |
| .NET runtime `v10.0.0` AsnWriter | `Encode()` copies written bytes; `Reset()` clears the written internal prefix | Production private encoders clear explicit temporary arrays but omit writer Reset. Observer tests do not establish internal writer-buffer erasure. Some fixture writers correctly Reset in finally. |
| .NET runtime `v10.0.0` EccSecurityTransforms, `.macOS`, ECDsaSecurityTransforms | ECPrivateKey D-only `ECDsa.Create(parameters)` → ImportParameters → absent-Q extraction → ReadEncryptedPkcs8 replaces `ECParameters`; later ImportKey copies into CryptoPool staging and returns it in finally | The D-only path introduces a runtime-owned managed D copy whose cleanup is not exposed to Core. Supplying Q skips that extraction in the checked source, and signing uses the imported SecKey. The SDK's supplied-Q warning is broader than this evidence. Reference code is not actual installed-runtime or SecKey native-memory attestation. |
| .NET runtime `v10.0.0` ECDsaCng.ImportExport | `ECDsa.Create(parameters)` on Windows provider paths delegates named/explicit blobs to CNG helpers; named friendly name and platform support are checked | C's primary code has no direct BCrypt/CoreFoundation imports. These are indirect BCL provider routes. Do not copy direct native ABI assumptions or universal CNG validation guarantees into SDK XML; the supplied-Q consistency verifier remains an independent SDK check. |
| .NET runtime `v10.0.0` ECDsaOpenSsl | Parameter import delegates ECOpenSsl; sign/verify converts DER/native representation to/from P1363 | Runtime/OS curve support differs. BCL's native library choice is separate from ARKG's explicit NativeShims route. No actual provider execution or all-curve support claim is made. |
| OpenSSL tag `openssl-3.0.0` BN conversion, EC_POINT, EVP_MAC manuals | Actual C-packet NativeShims exports wrap BN/point operations and the retained alternate CMAC helper | Allocation functions return NULL on failure; conversions have written-count/error outcomes. `EC_POINT_clear_free` explicitly destroys sensitive point contents, while SDK uses ordinary point free. EVP_MAC APIs use 1/0 outcomes. Package forwarding, actual availability and native release/erasure remain B4's exact artifact/runtime qualification. Active CMAC default is managed AES-128, not this retained alternate. |

The modern .NET reference sources and OpenSSL manuals explain concrete boundaries;
they do not identify the installed SDK/runtime or every indirect native dependency.
Historical SDK 10.0.403 output and a newer 10.0.404 host remain different evidence.
No version probe, compiler, host experiment, native loader or device ran here.

The checked package declarations select **Yubico.NativeShims 1.18.1** and
**Microsoft.Extensions.Logging 10.0.2**. `Libraries.NativeShims` names the runtime import
`Yubico.NativeShims`. These are source dependency identities, not evidence that a package
binary loaded successfully. The narrow active `AesCmac` helper context confirms direct
`Aes.Create()` rather than the public `AesCreator` hook, with separate key/subkey/block/
result clearing scopes; B6 retains its full protocol/implementation disposition.

## Closed research bounds and remaining decisions

- **NIST final:** initial final-PDF request and one focused official-page follow-up are
  complete. The raw final table remains unavailable. Metadata text was read through
  webfetch; direct-download 403 failures are retained without substituting a draft.
- **Rust oracle:** initial existing-reference inspection and one focused exact-name
  source search found historical SDK PR 468, but no immutable original
  `cnh-authenticator-rs-extension/.../arkg.rs` or `/tmp/gen_kat` revision. Obtain the
  original from its maintainer before calling it independently reproducible.
- **Native/provider artifacts:** bounded reference-source inspection is complete.
  Actual runtime revision, BCrypt/SecKey/OpenSSL behavior and NativeShims availability
  are qualification gaps for B4/validation, not unread Packet C semantic units.
- **SCP:** C supplied narrow containment/provider-test lifetime evidence. B6 owns wire
  profiles, OCE-reference/certificate requirements and the known GlobalPlatform-source
  gap. No final-spec download was retried by C.
- **Policy:** Unicode/empty-input enforcement, single-key/optional-attribute validation,
  key validation depth and shared disposable-record cloning need maintainer decisions.
  The investigator proposes no future breaking API or intended-contract endorsement of
  present cleanup/admission defects.

The shared pinned Python manifest/context remains available unchanged to A/B6. Its OATH
floor introduction identifies implementation policy, while earliest physical support and
hardware rationale remain unresolved. Sibling agreement is implementation evidence,
not an official normative actor or conformance finding.
