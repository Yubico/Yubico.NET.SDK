# Consequential source checks for the C edit batch

This is the editor's source/citation check, not fresh independent review or runtime qualification.
The completed investigation, `investigation/C/triage-clarifications.md`, full-family map, inherited
dispositions and B6 ownership/overlap refinements remain the authority chain. Their original bytes
were not edited. Existing retained sources and Python context were reused; no new web research ran.

| Source and exact passage checked | Actor, strength, and use in this batch |
| --- | --- |
| ITU-T X.690 (02/2021), ISO/IEC 8825-1:2021; retained `x690-202102.txt:492–597`, derivative of the original PDF identified by the final source register | §§8.1.2.2/8.1.2.4 describe sender low-tag and continuation-octet encoding; §8.1.3.5 forbids FF, and Note 2 permits redundant long-form length octets. §8.1.3.6 defines indefinite form separately. Keep Core's two-octet/definite-only bounded helper distinct from general BER. Correct regression/guard descriptions without changing parser behavior. Earlier pending-confidence text is historical, not a current uninspected-source gap. |
| RFC 5915, *Elliptic Curve Private Key Structure*, June 2010, Informational; §3, retained lines 96–149 | The ASN.1 schema marks inner parameters OPTIONAL, but the prose requires implementations conforming to this document to always include parameters (MUST). PublicKey is SHOULD with its stated external-distribution condition. The encoder currently omits inner parameters because the outer AlgorithmIdentifier has the curve OID. Document that difference; do not silently change the target profile or claim universal importer behavior. The same section specifies order-based private octet width, separately from scalar range. |
| RFC 5958, *Asymmetric Key Packages*, August 2010, Standards Track; §2, retained lines 131–257 | Schema has v1(0)/v2(1), optional attributes and a versioned publicKey field. Prose associates publicKey presence with v2. A recognized version number is distinct from validating all remaining bytes or a mathematical private key. Core's attribute-free/version-0 typed subset and early version exception are documented without calling the all-zero fixture valid. |
| .NET runtime tag v10.0.0, retained `AsnWriter.cs:79–99,185–209` | Encode returns an independent copy; Reset clears the written internal prefix. Production private encoders omit Reset. Existing INTEGER/nested encoding observers cannot establish writer-buffer erasure. This source conclusion is settled; actual installed runtime/provider/exceptional-path qualification stays separate. |
| .NET runtime tag v10.0.0, retained `EccSecurityTransforms.cs:184–232,242–269`, `.macOS.cs:52–79`, `ECDsaSecurityTransforms.cs:74–102` | ImportParameters calls absent-Q extraction only when a coordinate is missing. Extraction replaces the ECParameters value via ReadEncryptedPkcs8; supplying both Q coordinates skips that route. ImportKey returns CryptoPool staging in finally; signing uses imported SecKey. Narrow the SDK warning to this checked D-only route, without claiming native erasure or identifying the installed runtime. |
| .NET runtime tag v10.0.0, retained `UTF8Encoding.cs` constructors/fallback setup | Default replacement fallback is separate from application-selected strict encodings. Core's default options use Encoding.UTF8, while policy counts UTF-16 code units before conversion. The CLI Rune/raw-byte paths are dependency context, not the same mechanism or a newly adopted Unicode policy. |
| ARKG draft-bradleylundberg-cfrg-arkg-09 title/status and retained final source register | July 7, 2025 Internet-Draft, intended Informational, expiry January 8, 2026: work in progress, not a final standard. Keep its deliberate derivation target and domain-separation wording. The generator's immutable Rust revision/source remains missing; selected A/B/C expectations do not prove injectivity, independent reproducibility, constant time or full conformance. |
| Retained IANA COSE snapshot and final C source register, plus actual current enums/catalog | Correct the Key Types registry name and local zero sentinels. Retain numeric IDs and protocol profile choices. No new registry deprecation recommendation or applet upgrade is introduced, and generic structural applicability plus an alg mapping is not a complete profile pairing. |
| Final C SEC/RFC source qualifications and current named-curve test method | The retrieved SEC 2 v2.0 copy retains Draft headers. Brainpool P256/P384/P512 are RFC 5639 §§3.4, 3.6, 3.7. Correct the test's source scope and conditional host-provider assertions without claiming a fresh OpenSSL run or inspected NIST-final table text. |

The original PDF/source identities remain in `investigation/C/sources/final-source-register.md` and
its immutable download manifests. This batch adds no stronger citation requiring a new broad search.
The explanatory OpenSSL 3.0.0 manuals remain distinct from the actual NativeShims binary/export/provider
profile. C019 has no D facet: allocation/conversion outcome checks and native erasure are held, even
where the same files receive separate C011/C012/C018 documentation edits.

## Example review and exact bounds

`examples.json` captures opening/closing line coordinates, body character boundaries and SHA-256
for both base and current XML code bodies. Classifications distinguish complete example classes,
complete methods, scoped resource fragments, startup assignments and excluded historical RsaFormat
illustrations. All compiler/execution states are `NOT-RUN`.

- `DialogPrompt` uses an actual application-supplied async delegate rather than an undefined dialog
  helper. It forwards context/token, distinguishes decline from cancellation, clears the transferred
  byte array in finally, and returns a separate exactly sized owned buffer only on success. Its
  acquisition callback must be nonblocking and honor the prompt contract. Empty-source policy is
  explicitly limited to this nonempty-secret example.
- `ReadAndVerifyPinAsync` borrows `Yubico.YubiKit.Piv.IPivSession`; the actual source signature at
  `src/Piv/src/PivSession.cs:307` and `IPivSession.cs:114` takes ReadOnlyMemory and a token. The
  complete credential using scope includes the awaited use. Session disposal stays with its caller.
  This verifies the method name and ownership boundary only, not a PIV hardware/profile conclusion.
- `InspectRsaKey` uses a concrete disposable `RSAPrivateKey` owner. `IPrivateKey` itself does not
  promise IDisposable; its alias and the checked cast are not independent owners. Encoded input is
  borrowed and remains the caller's clearing responsibility.
- Provider fragments use actual BCL types, including `Aes`, and complete using braces. Startup
  assignments select defaults rather than pretend to implement hardware providers with fake helpers.
  Imports/context for a later compile harness are System.Security.Cryptography and the actual Core
  cryptography namespace. They demonstrate lifetime/configuration, not an algorithm exercise.
- The retained UserPresenceIntent fragment has a complete intent using scope and borrows an existing
  WebAuthnClient, PublicKeyCredentialRequestOptions and token. The method signature was checked in
  `src/WebAuthn/src/Client/WebAuthnClient.Authentication.cs:65–68`; client lifetime stays caller-owned.
  No new boundary-wide notification adoption or authorization claim is made.
- The unchanged native C sketch in `CmacPrimitivesOpenSsl.cs:24–37` is an OpenSSL API illustration,
  not C# SDK code or proof of the packaged shim's configuration. RsaFormat's 22 retained XML bodies
  are commented-out historical context, excluded from active example compilation.
