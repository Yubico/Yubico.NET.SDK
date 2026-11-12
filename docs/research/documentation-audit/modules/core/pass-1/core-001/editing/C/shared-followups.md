# C shared and boundary recommendations

These are recommendations for the later assigned owners, not edits or independent boundary acceptance.
Exact candidate/path decisions and source identities are in `path-decisions.json`. Preserve original
run-qualified inherited identities; `CORE-C-C*` and `FPT-*` remain local/proposal IDs.

| Surface / owner | Recommendation and retained limit |
| --- | --- |
| `docs/usage/user-interaction.md` / SHARED | C001–004/C008/C017: keep synchronous reader separate from asynchronous SDK callback. Describe pooled interactive UTF-16 input, redirected managed strings, encoded-byte/code-point adapter requirements, empty allocation and hex policy differences, and waiting-key-only polling. Returned credential ownership and a borrowed open session must enclose the complete awaited use. Applet PIN/Unicode rules require actual boundary acceptance. Callback exception payloads can reach providers. |
| `docs/usage/tlv-processing.md` / SHARED | C005–007: name Tlv/list/returned encoding owners, borrowed views, and independent source/result/intermediate clearing. The existing nested sensitive example has complete cleanup and should remain. Static TlvHelper itself is not a buffer owner. Prefer disposable DecodeList for sensitive sequences; a clearable returned search copy does not erase abandoned copies, and clearing a writable copy cannot erase a read-only original. Preserve definite-only, bounded-tag scope without endorsing unchecked single-item remainder/tag admission. |
| `docs/LOGGING.md`, Core README/CLAUDE / SHARED | C008/FPT-090 plus B6 C014: configure before first use; caller owns supplied factories; existing static logger instances survive replacement. UseTemporary is process-global and not concurrency-safe scope isolation. There is no central redactor. Ordinary APDU trace templates contain headers/lengths, whereas SELECT logs supplied identifier bytes; callback/other exception provenance is independent. Do not describe every APDU body as logged or infer global redaction/safe payload policy from header-only templates. Intent is display text: keep secrets out because application callback exception text can carry it to providers. |
| `docs/CRYPTO-APIS.md`, Core README/CLAUDE / SHARED | C009–016: catalog metadata is not key material, capability, provider acceptance, profile conformance, a recommendation or a default. Keep mathematical field/order bits distinct from scalar/seed/coordinate/public encoding widths. SPKI encodes a key, not an ECDSA signature; DER certificate ECDSA, BCL P1363 and COSE concatenation are different contexts. A registered COSE mapping and broad structural applicability do not validate every curve/alg pairing. Retain existing applet algorithm choices. |
| `docs/CRYPTO-APIS.md`, `docs/MEMORY-MANAGEMENT.md` / SHARED | C010–013: distinguish caller arrays, wrapper-owned mutable aliases, exported encodings, SDK temporaries, writer storage and provider/native copies. Disposing a provider does not erase every other owner. Keep macOS supplied-Q/absent-Q distinction qualified to inspected .NET v10.0.0, and AsnWriter Encode copy versus Reset written-prefix clearing separate from actual native/provider qualification. Include selected RSA relations versus full mathematical validation and DP-width gate. Hold stronger uniform factory/exact-DER admission pending the selected unrun experiment. |
| `docs/MEMORY-MANAGEMENT.md`, root `CLAUDE.md` / SHARED | C017: spans may be used in async segments that do not cross suspension under the selected language version. Prefer actual borrowed ApduCommand or an explicitly nonsecret illustration. Keep disposable reference owners for privately cloned secrets, but correct the rationale: copying an array-bearing struct copies references, so zeroing shared storage reaches aliases; shared mutation and ambiguous disposal/lifetime ownership are the hazard. Do not offer writable casts as cleanup guidance. |
| `docs/migration/v1-to-v2*.md` and map YAML / SHARED | C012/C017: internal crypto creators are active but not public extension points; public factories do not cover direct runtime calls. Remove stale shared-Base32 absence claim while preserving unrelated codec gaps and OATH nonempty/profile policy. No configuration/YAML edit was made here. |
| `docs/architecture/webauthn-extension-processing.md` / SHARED and WebAuthn owner | C011/C018/C019: preserve deliberate expired draft-09 target and structural derivation evidence without full conformance/constant-time/native cleanup claims. Context is domain separation, not an injective key guarantee. Original Rust/gen_kat immutable provenance is missing; A checks point+handle, B/C selected points. Keep actual WebAuthn actor/serializer/result decisions at the boundary. |
| SCP parameter/source paths / B6, applet examples / their owners | C020/FPT-102 overlaps B6 C007/C009 only for referred facets. StaticKeys clones source arrays; Scp03KeyParameters retains/disposes the supplied object. SCP11 retains key wrappers/certificate objects, copies only the certificate collection, disposes retained private key/certificates, and leaves the public wrapper intact. A record with-clone shares these owners. Keep lifetime through every initialization/reset use. Optional OCE reference admission and a/b/c requirements require the actual initializer and qualified profile, not record shape. No B6 source was edited here. |
| Core test guidance / SHARED | Preserve finite assertion scopes, synthetic/public fixture meaning, pool-identity assumptions and exact observer aliases. Private array observers intentionally retain references only to inspect clearing after ownership exits; they must not mutate live values. Do not infer native erasure or fresh passing execution. |

## Explicit held qualifications

- C003/C004/C006/C007/C009/C010/C013/C019 E facets: no input, parser, failure-allocation,
  native conversion or provider experiment ran. Source-ready local qualifications are not evidence of
  stronger uniform acceptance, responsiveness, all-path erasure or deployment behavior.
- NIST SP 800-186 final table text remains unavailable after the retained bounded attempts. The SEC 2
  v2.0 copy retains Draft headers; mathematical metadata supplies no FIPS/NIST approval statement.
- Original immutable Rust/gen_kat provenance remains missing. Do not substitute current sibling SDK
  agreement for that oracle. Pinned Python is implementation/history context, not a normative authority.
- Exact NativeShims binary/export/producer profile and installed runtime/provider/native erasure remain
  qualified. SafeHandle/API record shape establishes no uniform factory/provider conformance.
- Actual applet boundaries remain required for caller-specific constraints, authorization, SCP phases,
  option propagation, certificate trust, and reset behavior. The user-ready Windows CCID/HID delta is
  still unready; PIV physical touch/hotplug and touch-dependent cancellation remain deferred.

Finish Core Pass 1 publication and the deferred-finding handoff, then stop. These recommendations
authorize neither Pass-2 planning nor implementation.
