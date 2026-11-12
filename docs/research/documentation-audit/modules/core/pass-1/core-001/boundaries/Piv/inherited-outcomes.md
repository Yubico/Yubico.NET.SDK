# Inherited findings and nonadditive outcomes

The authoritative registry remains unchanged. This packet preserves all **11 routed provenance
keys in three mechanism groups**, plus all ten original PIVIMP engineering/qualification groups.
The source-run spelling `piv-001:PIVCORE-001..003` is authoritative; dispatch's PIV-CORE-001..003
refers to those identities and allocates no new IDs. `inherited-dispositions.json` retains the
original registry rows and supplies the PIV-scoped successor recommendation.

| Routed key | PIV caller outcome | Remaining owner / limit |
| --- | --- | --- |
| `piv-001:PIVCORE-001` | Strict Exclusive defaults and explicit Shared/fallback are confirmed in source and policy assertions; borrowed PIV disposal closes no connection; native release requests UNPOWER | B1-C001 / X001; physical PIN state, separate processes, OS/device/power evidence remain unresolved/deferred |
| `audit-v2-20261022-20261031:#22` / YESDK-1630 | Same portable mitigation as PIVCORE-001, retaining separate historical provenance | No unconditional physical isolation or compatibility acceptance inferred |
| `piv-001:PIVCORE-002` | Authentication, PIN-only transition/recovery/reset, metadata+crypto and callbacks cross APDU exchange boundaries; no whole-operation lease | B5-C006 / X002; disposal/overlap policy and physical cancellation/recovery remain separate |
| `piv-001:PIVCORE-003` | Actual crypto/raw GET/Bio/auth response Clear, scoped TLVs and output transfer checked; direct connection borrowed | B6-C002/C-C005–C007 / X003; lower staging/decrypted/fragment/provider copies remain independent owners |
| `openpgp-001:PGP-CORE-002` | Shared base disposal dispatch and borrowed/owned connection mechanics confirmed at PIV callers | OpenPGP owner decides KDF cache/SCP/reinitialization specifics; no duplicate assignment or OpenPGP closure |
| `openpgp-001:PGP-CORE-004` | Same per-exchange/public-workflow and prompt ownership distinction applies to PIV | OpenPGP retains its 6600/Failed and recovery scope; PIV adds no native-interruption inference |
| `security-domain-001:SD-DEP-004` | PIV reaches same session/guard/response/Le mechanisms, including empty and large commands | Security Domain owner retains GetData/reset-specific trace; B6 retains actual wire/profile questions |
| `yubihsm-001:HSM-DEP-002` | PIV confirms borrowed prompt/connection and admitted SmartCard drain; callback resolution belongs to producer | HSM owner retains CALCULATE and historical 6982/Failed/fresh recovery; no HSM operation was re-audited here |
| `yubihsm-001:HSM-DEP-003` | PIV confirms response copied owner and distinct TLV/result copies through actual parser | HSM SessionKeys/receipt layout remain applet-owned; supplied-cryptogram mismatch is no generic Core defect |
| `yubiotp-001:OTP-DEP-001` | PIV factory uses shared no-fallback helper, binds before selection, owns only hidden connection | YubiOTP's Linux/native HID routing remains its boundary; PIV chooses SmartCard only |
| `yubiotp-001:OTP-DEP-003` | PIV uses guarded noncancellable constituent SmartCard traffic and resolves sensitive output before transfer | OTP status/CRC/request ownership remains distinct; do not transfer OTP self-wait/cancel conclusions to PIV |

## Original PIV engineering groups

| Original ID | Source rebaseline / retained outcome | Exact active reopening surface |
| --- | --- | --- |
| `piv-001:PIVIMP-001` | Bio reset preflight TODO remains. Retry blocking begins before RESET; metadata refresh may fail afterward. Normal reset retains F9 | PivSession.Reset/BlockPin/BlockPuk; IPivSession.Reset; README Reset; CLAUDE reset gotcha; Reset helper/menu/test comments |
| `piv-001:PIVIMP-002` | Partial PIN-only writes and raw rotation/object coherence remain applet decisions. Supplied-active-key auth, default-before-delete and 6982-only restoration scopes remain fixed | PivSession enable/recover/disable; IPivSession PIN-only/credential changes; README round trip; migration; workflow test descriptions |
| `piv-001:PIVIMP-003` | Actual gate/profile differences preserved: major-zero effective policy; raw version independent of metadata; AES/serial/ROCA/FIPS product dimensions remain qualified | Features, backend/factory/default resolver; README Requirements/matrix; CLAUDE firmware; current migration and fixture version comments |
| `piv-001:PIVIMP-004` | Fixed CHUID/CCC/KeyHistory shapes and broader profile/count/URI decisions remain unchanged | DataObjects models, typed/raw interface XML, README provisioning section, model-vector explanations and migration map |
| `piv-001:PIVIMP-005` | **Confirmed/unfixed.** Tool hashes every algorithm: RSA omits EMSA; Ed25519 signs hash. Historical actual tool RSA failed original-message verification and Ed25519 verified SHA256(message); direct SDK controls passed | Signing.SignDataAsync; CLI/menu eligibility; tool README signing section/XML; independent original-message regression oracles. Applet/tool finding only |
| `piv-001:PIVIMP-006` | Sample result/credential/certificate/callback/cancellation defects remain. Core reader mechanisms now mapped at PinPrompt, without recounting them | All X006 file dispositions, tool README, helper/result XML, CLI/menu credential finally scopes and certificate generators |
| `piv-001:PIVIMP-007` | Tags without algorithm-specific output widths, BigInteger/provider residuals and certificate decompression/CertInfo admission remain. Valid zero-prefixed RSA re-wrap rationale is already corrected | PrivateKeyOperations parser/unpad; CertificateProtocol; interface crypto XML, README formats/cleanup, response-owner test explanations |
| `piv-001:PIVIMP-008` | Existing suite oracle/fixture/cleanup debt remains. Strong historical supplemental checks supplement rather than rewrite weaker canonical methods | X005 exact test/method scopes, Maintainer verification, Test Infrastructure and shared fixture instructions via primary owner |
| `piv-001:PIVIMP-009` | C# trimmed little-endian date versus Python fixed four-byte big-endian remains source-confirmed; self-roundtrip misses compatibility | AdminData date codec/XML, typed object/pin-only tasks, README interoperability and migration; desired format requires separate decision |
| `piv-001:PIVIMP-010` | Negative/over-24-bit arbitrary object IDs still truncate into request encoding; response forward compatibility is a different concern | DataObjectProtocol.EncodeObjectId; IPivSession.Get/PutObject; README provisioning/raw examples; future fail-fast tests |

Earlier fixes remain resolved for their exact source scopes: absent-data VERIFY never invents three;
metadata fallback is instruction-specific; failed management attempts/disposal hide authentication;
SET 6982 differs from unrelated status; detected version drives reset defaults independently of
overrides; PIN-only validates supplied active key and restores prior candidate only after 6982;
unknown algorithms retain raw metadata; explicit wire subsets stay strict; SDK outputs are mutable;
actual response owners and private encoding predecessors clear; raw Ed25519 passes original bytes;
short EC digests left-pad; oversized RSA input rejects. Passing names or new owner terminology do
not expand these into all-provider, native, physical or profile closure.

## Historical evidence kept historical

The PIV handoff records 86/89 canonical methods passed, with three physical-lane methods deferred;
the supplemental touch-dependent cancellation/recovery case is also deferred. P01–P04 passed their
recorded X25519, repeated-policy, key/certificate-independence and attestation signature/key-binding
oracles. P05/P06 retain intended-contract failures and direct SDK controls. That is inherited Linux
USB SmartCard evidence on development firmware 5.8.0.beta.0/alpha.2, not this packet's executions.

Historical serial 103/125 default PIN/PUK 3/3, management metadata, empty normal slots/certificates
and preserved F9 identities remain dated readbacks. Protected PRINTED content was deliberately
unread during final independent restoration; there is no independent observed byte-absence claim.
No current key state was inspected. Existing attestation/private material was untouched.

Serial 125's FF and 00 INITIALIZE UPDATE failures remain 6A88 before EXTERNAL AUTHENTICATE/RESET.
`fixes-wave-001:CORE-001` and historical serial 9681620's two-key discovery failure remain
unexplained, with no PIV runtime result added. `oath-pilot-and-fixes-wave-002:DOC-002` remains the
separate inherited migration-YAML failure. The 21 PGP-DEP labels retain original source-run
identity and add zero Core bugs. Windows CCID/HID concurrency readiness remains a future user
handoff, outside pinned B4; no unfinished checkout was inspected.
