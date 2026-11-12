# Astra PIV boundary sample verdicts

Read all 40 complete pinned units in `read-boundary-Piv.md`, the current cross-key liveness helper,
and the preserved CORE-PIV-X006 / PIVIMP-006 cleanup disposition. No hardware, PIN, reset, signing,
touch or cryptographic probe was executed. Final mapping and clean-credit reconciliation remain pending.

| Unit suffix | Verdict and scope |
| --- | --- |
| `7ef8a2e75575c5d2` | Supported: AES selectors and default 3DES mapping are the exact parser behavior; no strict rejection guarantee. |
| `3eb8053339c6e5be` | Supported: returned owner is caller-disposed; custom reader may return null. Initial selection behavior is separate from credential-reader cancellation. |
| `1990c8b8a253ce31` | Supported within current source: CSR generator borrows the session, scopes public host keys, returns PEM/error and converts caught exceptions. Its existing synchronous generator/token limitations remain explicit elsewhere. |
| `93c8f7b10e5bd567` | Supported: AES's single acquired key copy is immediately protected by finally; manual EDE path remains separate. No provider-internal erasure claim. |
| `009632cb50b26e3d` | Supported within inherited parser/profile limits: empty/missing certificate returns null, optional decompression and certificate construction are shown. Bounded-decompression/CertInfo questions remain PIVIMP-007. |
| `6eec962fc55a985c` | Supported: inner CHUID content is encoded without the outer53 wrapper; no full federal-profile claim. |
| `81297035b8793da4` | Supported: known metadata type must also pass actual PIV encoding; operation subsets remain separate. |
| `6576de010de5767d` | Supported: slot/key/policy/version and management-auth checks precede the import protocol call. |
| `a579b6a5cf4242e4` | **Known deferred qualification, zero clean credit:** PIN allocation precedes awaited discovery outside try. Already owned by CORE-PIV-X006 / PIVIMP-006, not a new issue or runtime fix. |
| `8fc815177a95b970` | **Known deferred qualification, zero clean credit:** management-key bytes likewise precede discovery outside the clearing scope; same preserved finding. |
| `b3919addb32e317d` | Supported: exact output fields and optional metadata serialization; no implicit certificate disposal or validation. |
| `8f112e59fc6debcb` | Supported: three leading version bytes after minimum-length validation. |
| `df0399d60d88c3f0` | Supported: asymmetric category check plus concrete PIV encoding. |
| `0aa0e8aee63a59f5` | Supported: symmetric category check plus concrete PIV encoding. |
| `d741c3bac5834946` | Supported as SDK policy: final4.4.x restriction, excluding alpha/beta, is not inferred from the generic key catalog or a new device probe. |
| `e0d8e2e0ada81ba1` | Supported: Core dependency, AOT opt-in and internal-constructor unit-test access. |
| `89c5d1ecb6fd0e8d` | **Edited overlap, zero clean credit:** the pinned example's post-PIN setup precedes try; accepted PIV revision2 moved that setup into the protected scope. Read history is preserved, not counted as unchanged clean content. |
| `65b137a50ae4091c` | Supported with explicit profile/interoperability/trust qualifications and protected F9 material; no new attestation trust established. |
| `70fb463e165b9ad9` | Supported within the existing audited CLI scope: task/options/output vocabulary retains key-type, validity/exit-status and partial-result qualifications. PIVIMP-005 remains confirmed/unfixed. |
| `f066105117ef736a` | Supported: null-certificate assertion after requested reset; not physical reset qualification. |
| `ee5b81e0432fe552` | Supported: comment explicitly narrows misleading historical test name to generation/point width, not agreement. |
| `f6b6a42a64636d03` | Supported: default flag and retry counts asserted on metadata. |
| `d428dd99f2122ebc` | Supported: algorithm/origin and full public SPKI equality are asserted. |
| `fce5dda5582cebf9` | Supported with existing cleanup limits: helper establishes pending keygen before and after the responsive operation in both directions. It drains keygen but explicitly leaves responsive timeout drainage as separate work. |
| `eb98b78558a307d8` | Supported as setup intent and successful call sequence; no independent cross-process PIN-state proof. |
| `13db8df479930852` | Supported: raw RSA padded input and independent host SHA-1 verification; weak-hash use is explicitly algorithm-coverage-only. |
| `ce8af4db9c07f5ad` | Supported: SHA-384 PKCS1 input and independent host verification. |
| `febe7cd8e74ac70c` | Supported: SHA-512 PKCS1 input and independent host verification. |
| `d36b5c72a45f3b4c` | Supported: metadata change and host ECDSA verification check successful overwrite. |
| `79aade9dc99cb132` | Supported: metadata change, manually formed SHA-256 PKCS1 representative and host verification check the reverse overwrite. |
| `6d100fecb62d4abb` | Supported as a read-only metadata inspection definition, not a test of touch or current physical state. |
| `92d416947d877101` | Supported: fake checks failed instruction is last, no subsequent GET DATA, and captured secret buffers cleared. |
| `af3aaba139d19db7` | Supported: unknown algorithm/raw bytes retained, no silent factory default, authentication rejected before extra transmission. |
| `1b220d760b58a657` | Supported: the two scripted unavailable-metadata statuses produce the5.7 AES192 fallback. |
| `0cf32693862d0e51` | Supported: unsupported-instruction mapping is distinguished from6A80 on repeated fake calls. |
| `a7d45b5ed78ea182` | Supported: overridden old firmware rejects after the three setup transmissions; no new operation request. |
| `e4f10ec180cc9e83` | Supported: enumerated RSA/non-RSA metadata helper results. |
| `e0620adee4adaf59` | Supported: Curve25519 representations reject ECDSA extraction with the asserted diagnostic. |
| `48aebb8b6acbc6ac` | Supported: one metadata query plus notification assertions; one-byte fake crypto response is not a valid-signature oracle. |
| `31264273bb8cc2f5` | Supported: unit-runner/project references and package declarations; compilation/execution gates remain separate. |

All IDs have prefix `core-001:unit:`. No new documentation correction is assigned from this packet.
The two known implementation-qualified units and edited overlap require clean-quota replacements.
