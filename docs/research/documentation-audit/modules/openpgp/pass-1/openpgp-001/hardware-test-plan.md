# OpenPGP hardware evidence plan

**Coordinator execution only; prepared 2026-11-04 against fe180282.** The investigator executed no
hardware. The user authorizes testing, reset, PIN/configuration changes, and ad hoc SDK/Python/ykman
probes on the connected dedicated key. Apply the established allowed-serial boundary and verify
identity/state; routine PIN/configuration work needs no additional approval gate. Coordinate physical
touch, insert/remove and timing with the user. Serialize device and toolchain access.

**Initial execution steer (historical pause):** mutation had not started. The installed direct Python session reports `0.0.1`,
while ykman CLI reports application `5.8.0`, card `3.4`, and device metadata identifies a `5.8` beta
device. Vendor helpers reject ATT/DEC/SIG certificate reads at their firmware gate; this establishes
no certificate absence. AUT reads absent. Other baseline metadata reports ATT key Imported, ordinary
keys None, all UIF Off, retries 3/0/3 and KDF None. The separate probe owner is completing the narrow
public ATT certificate baseline using framing supported by actual device metadata and installed vendor
source. That checkpoint required waiting for its result before mutation. Keep observed versions and effective version
policy separate; PGP-DEP-021 records the deferred Core boundary question without presuming an SDK bug.

**Current execution checkpoint:** `hardware/vendor-baseline-v2.json` completes that baseline with
explicit certificate and all four UIF DO reads. ATT certificate length is 793 bytes; DER SHA-256 is
`94afb703099edb4aaaee443c6e197c379f7f909d23f717f0868ef73057e4a16a`. Ordinary certificates are absent;
all actual UIF values are Off. Framing uses fresh `5.8.0.beta.0` metadata, direct session version stays
0.0.1, and no global override is applied. The coordinator has started the 48 non-KDF methods including
Slow cases. The KDF case can run separately under existing authorization. The supplemental attestation
probe uses extended User verification; retain that mode/outcome separately from ordinary-mode tests,
without assuming either mode is exclusively required from source examples alone.

## Baseline, irreversible effects, and recovery

1. Establish actual serial, model, physical firmware, OpenPGP application version, OS, USB/NFC transport,
   installed ykman version/revision and SDK revision. Pin serial for every independent script. Keep other
   keys outside selection and close competing SmartCard clients. Read state before mutations.
2. Record AID, card version, app firmware, advertised capabilities, supported algorithms, PW status,
   raw KDF configuration, signature counter, key information/attributes/public keys, fingerprints/times,
   Sig/Dec/Aut certificates and UIF. Export/hash the **public** ATT certificate and record ATT metadata
   and UIF. Establish the factory key's presence without replacing it. Record absence, APDU failure,
   gate rejection, and decode failure distinctly. Keep credentials and derived secrets out of logs.
3. Every reached test helper below resets OpenPGP **before** its body. This irreversibly removes ordinary
   slot private keys and related state, restores credentials/defaults, and may first consume remaining
   PIN retries. Saved public certificates cannot recover deleted device private keys. Disposal releases
   resources; the helper performs **no post-test reset**. Tests can leave keys, PINs, KDF, retry counts,
   touch settings and certificates for the next operation.
4. ATT key/certificate survive ordinary reset; fixed ATT UIF can survive too. Replacing the factory ATT
   private key loses its manufacturer attestation trust permanently. **Never overwrite the factory ATT
   private key merely for coverage.** Test ATT CRT through no-hardware wire evidence and a read-only
   public-key query if useful. Keep ATT UIF reversible (`On`/`Off`) for touch probes. Probe fixed UIF
   on a disposable ordinary slot only, with explicit replacement/reset recovery and readback.
5. After failed setup, cancellation or partial mutation, await terminal completion/drain, release the
   session/connection, inspect with a fresh session and perform bounded recovery before the next case.
   SDK Reset currently ignores E6/44 status; success text alone establishes no reset postcondition.
   Preserve raw failure and later successful recovery separately. Readback/recovery are coordinator work.

The current key's baseline is a checkpoint, not a permanent assumption. Recheck after another authorized
phase changes it. Build/runner preparation belongs to the independent tooling owner.

## Exact execution filters

`evidence/hardware-methods.json` contains **all 49 methods**, their full source bodies/attributes, exact
line locations, firmware filter, `FullyQualifiedName=...` and complete toolchain command per method.
All rows select `ConnectionType.SmartCard`; this is a device-interface predicate, so record the actual
operation transport separately.

```bash
dotnet toolchain.cs -- test --integration --project OpenPgp --filter "FullyQualifiedName=Yubico.YubiKit.OpenPgp.IntegrationTests.OpenPgpSessionTests.Reset_RestoresDefaultState"
```

Grouped execution excluding the isolated KDF transition:

```bash
dotnet toolchain.cs -- test --integration --project OpenPgp --filter "FullyQualifiedName!~SetKdf_IterSaltedS2k_ThenVerifyPin_Succeeds"
```

This command includes the two Slow methods. Smoke excludes them and is incomplete evidence. No method
has a `RequiresUserPresence` trait; **zero UP traits establishes no touch qualification**. Setup reset
normally sets ordinary UIF Off; ATT policy needs its own check. Use individual filters for diagnosis
and KDF isolation. Keep ad hoc queries separate from tests and active touch waits.

## All 49 body effects and actual assertions

**Common setup S:** when the method reaches its helper, open shared connection, construct reset
session, call `ResetAsync`, dispose reset session, construct fresh test session, invoke body.
`resetBeforeUse: true` appears in **all 49**. **Common cleanup C:** dispose test session and connection
through helper scopes, with no automatic post-reset. “Restore” below means explicit body work, often
outside a `finally`. Thus even read-only bodies have destructive setup.

Names below complete `Yubico.YubiKit.OpenPgp.IntegrationTests.<class>.<method>`. Firmware is the
attribute's minimum; “—” means no declared minimum, not universal support. **ER** means a normal return
before S for **4.2.0 through 4.3.5 inclusive**. Record those successes as **body unexecuted**.

### OpenPgpSessionTests — 28 methods

| Method | Min FW | Body effects beyond S; actual assertions; state after C |
|---|---|---|
| Reset_RestoresDefaultState | — | Read C4; assert retries 3/0/3 only. Keys, certificates, PIN values, KDF and UIF remain unprobed. |
| GetApplicationRelatedData_ReturnsValidData | — | Read 6E; assert nonnull AID, discretionary data and three algorithm objects. Reset state remains. |
| VerifyPin_DefaultPin_Succeeds | — | Verify User/81; success only. Establishes transient signature authorization. |
| VerifyAdmin_DefaultPin_Succeeds | — | Verify Admin/83; success only. Establishes transient admin authorization. |
| VerifyPin_WrongPin_ThrowsWithRemainingAttempts | — | One wrong User attempt; assert typed exception, retries 2 and SW 63C2 or 6982. Leaves User retries 2. |
| ChangePin_AndRestore_Succeeds | — | Change User to 654321, verify, restore default, verify. Explicit restore is reached only on success. |
| ChangeAdmin_AndRestore_Succeeds | — | Change Admin to 87654321, verify, restore default, verify. Explicit restore is reached only on success. |
| GetPinStatus_DefaultState_ReturnsExpected | — | Read C4; assert retries 3/0/3. No credential probes. |
| GetAlgorithmAttributes_DefaultState_ReturnsRsa2048 | — | Three 6E-based reads; assert RSA type and NLen 2048 for Sig/Dec/Aut. |
| GenerateEcKey_P256_Succeeds | 5.2.0 | Admin; mutate Sig attributes and generate P256; assert EC/P256 attributes. Leaves Sig key. |
| GenerateEcKey_P256_PublicKeyReadable | 5.2.0 | Admin; generate Sig P256; assert nonempty public-key buffer only. Leaves Sig key. |
| GenerateRsaKey_2048_Succeeds | —; ER | Admin; generate Sig RSA2048; assert attributes and nonempty public key. Leaves Sig; no host verification. |
| Sign_EcP256_ProducesValidSignature | 5.2.0 | Admin; generate Sig P256; User/81; sign. Assert nonempty signature/public-key buffers. Comment claims verification, but no host verify occurs. Leaves Sig and incremented counter. |
| Sign_Rsa2048_ProducesSignature | —; ER | Admin; generate Sig RSA2048; User/81; sign. Assert nonempty, 256-byte signature. Leaves Sig/counter; no host verify. |
| Authenticate_EcP256_ProducesSignature | 5.2.0 | Admin; generate Aut P256; User/82; authenticate. Assert nonempty output. Leaves Aut; no host verify. |
| Certificate_PutGetDelete_Succeeds | 5.2.0 | Admin; generate Sig; store unrelated host self-signed certificate, assert bytes, delete and assert null. Leaves Sig and empty certificate slot; no key binding assertion. |
| AttestKey_ReturnsValidCertificate | 5.2.0 | Admin; generate Sig; User/81; attest and write Sig certificate. Assert nonnull/nonempty certificate. Leaves Sig/statement; no chain/key-binding assertion. |
| AttestKey_RequiresUserPin_AndReplacesCardholderCertificate | 5.2.0 | Admin; generate Sig; store host certificate. FB without User must throw ApduException and retain bytes. User/81 then FB succeeds; assert returned/stored bytes and replacement. Leaves statement; no chain verification. |
| GetUif_DefaultState_ReturnsOff | 4.2.0 | Read Sig UIF; assert Off only. |
| SetUif_On_ThenReadBack | 4.2.0 | Admin; set Sig On/read On. Leaves Sig UIF On; no crypto or touch occurs. |
| GetKeyInformation_AfterGenerate_ShowsGenerated | 5.2.0 | Admin; generate Sig P256; assert Sig present/status Generated. Leaves Sig. |
| GetFingerprints_AfterReset_ReturnsFourEntries | — | Read fingerprints; require Sig/Dec/Aut/Att and 20-byte values. Zero values satisfy assertions; historical three-slot devices can fail body. |
| GetGenerationTimes_AfterReset_ReturnsFourEntries | — | Read dates; require four slots. Values/defaults/date validity are unasserted. |
| GetSignatureCounter_DefaultState_ReturnsZero | — | Read 7A; assert counter zero. |
| GetSupportedAlgorithms_ReturnsSupportedAlgorithms | 5.2.0 | Read FA; assert nonempty and Sig/Dec/Aut references. Supported choices, multiplicity and workarounds remain unverified. |
| DeleteKey_AfterGenerate_Succeeds | 5.2.0 | Admin; generate Sig P256; assert Generated; attribute-switch delete; assert None. Leaves RSA2048 attributes/no Sig key; other metadata unasserted. |
| GetKdf_DefaultState_ReturnsKdfNone | — | Lazy/cached F9 lookup; assert KdfNone. No PIN transition. |
| GetChallenge_ReturnsRandomBytes | — | Two 16-byte requests; assert lengths and inequality. This is not an entropy/certification test. |

### OpenPgpPinManagementTests — 5 methods

| Method | Min FW | Body effects/assertions; state after C |
|---|---|---|
| ResetPin_ViaResetCode_RestoresAccess | — | Admin; set Reset Code; three wrong User attempts; assert zero retries; reset to 654321 with code; verify new User. Leaves new PIN and Reset Code. |
| UnverifyPin_ClearsVerificationState | 5.6.0 | Admin; generate Sig; User/81; sign successfully; unverify 81+82; expect ApduException on second sign. Leaves Sig/counter; extended state unprobed. |
| SetSignaturePinPolicy_ChangesPolicy | — | Assert initial Once; Admin; set/read Always; restore/read Once. Byte roundtrip, not a two-signature authorization experiment. |
| GenerateKey_WithoutAdminAuth_Fails | 5.2.0 | Attempt P256 generation without Admin; expect ApduException. Failure may occur at attribute write before generation. |
| ResetPin_ViaAdminPin_RestoresAccess | — | Three wrong User attempts; assert zero; Admin; reset User to 654321; verify. Leaves new User. The complexity comment needs profile qualification. |

### OpenPgpDecryptTests — 2 methods

| Method | Min FW | Body effects/assertions; state after C |
|---|---|---|
| Decrypt_RsaPkcs1_RecoverPlaintext | —; ER | Admin; import host RSA2048 to Dec; host PKCS1 encrypt; User/82/card decrypt; assert plaintext equality. Leaves imported Dec; meaningful RSA crypto evidence. |
| Decrypt_EcdhP256_ProducesSharedSecret | 5.2.0 | Admin; import host P256 to Dec; submit ephemeral 04/X/Y point; User/82; compare card result with host raw agreement. Leaves imported Dec. |

### OpenPgpKeyImportTests — 6 methods

| Method | Min FW | Body effects/assertions; state after C |
|---|---|---|
| ImportRsaKey_2048_ThenSign_VerifiesWithTheHostPublicKey | —; ER | Admin; import Sig RSA; assert Imported/Standard attributes; User/81; sign SHA256; host RSA PKCS1 VerifyData. Leaves Sig/counter. |
| ImportEcKey_P256_ThenSign_VerifiesWithTheHostPublicKey | 5.2.0 | Admin; import Sig P256; assert Imported/P256; User/81; sign SHA256; host ECDSA DER verification. Leaves Sig/counter. |
| ImportEcKey_P384_ThenSign_VerifiesWithTheHostPublicKey | 5.2.0 | Admin; import Sig P384; assert Imported; User/81; sign SHA384; host ECDSA DER verification. Leaves Sig/counter. |
| ImportEcKey_Ed25519_CardReportsTheTestVectorPublicKey | 5.2.0 | Admin; import RFC8032 seed to Sig; assert Imported/Ed25519; parse 7F49/86 and compare RFC public key. No signing. Leaves Sig. |
| ImportEcKey_X25519_ToDec_CardReportsTheTestVectorPublicKey | 5.2.0 | Admin; import RFC7748 scalar to Dec; assert Imported/X25519; parse 7F49/86 and compare Alice public key. No agreement. Leaves Dec. |
| ImportEcKey_P256_ToAuth_ThenAuthenticate_VerifiesWithTheHostPublicKey | 5.2.0 | Admin; import Aut P256; assert Imported; User/82; authenticate SHA256; host ECDSA DER verification. Leaves Aut. |

### OpenPgpMultiCurveTests — 3 methods

| Method | Min FW | Body effects/assertions; state after C |
|---|---|---|
| GenerateEcKey_P384_ThenSign_Succeeds | 5.2.0 | Admin; generate Sig P384; assert attributes/nonempty public key; User/81; sign SHA384/nonempty output. No host verify. Leaves Sig/counter. |
| GenerateEcKey_BrainpoolP256R1_Succeeds | 5.2.0 | Admin; generate Sig BrainpoolP256; assert attributes/nonempty public key/Generated status. Leaves Sig. |
| SetPinAttempts_CustomValues_ReflectedInStatus | — | Admin; set Reset Code; set retries 5/3/5; assert those C4 counts. Leaves custom retries/code. NEO side effect unprobed. |

### OpenPgpAdvancedTests — 5 methods

| Method | Min FW | Body effects/assertions; state after C |
|---|---|---|
| GenerateEcKey_X25519_Succeeds | 5.2.0 | Admin; generate Dec X25519; assert attributes/nonempty public key. No agreement. Leaves Dec. |
| SetKdf_IterSaltedS2k_ThenVerifyPin_Succeeds | 5.2.0 | **Isolate.** Admin; distinct random salts and initial default-PIN hashes; set F9; ChangeUser(default,default); ChangeAdmin(default,default); read cached KDF/type/SHA256/count; VerifyUser/81. Both operands of each change are derived, despite raw-old comment. Leaves KDF/credential state even on failure; only local hash arrays explicitly cleared in finally. |
| GenerateRsaKey_3072_Succeeds | 5.2.0; Slow | Admin; generate Sig RSA3072; assert attributes/nonempty public key. Slow generation; leaves Sig. |
| GenerateRsaKey_4096_Succeeds | 5.2.0; Slow | Admin; generate Sig RSA4096; assert attributes/nonempty public key. Slow generation; leaves Sig. |
| GenerateEcKey_Ed25519_ThenSign_ProducesSignature | 5.2.0 | Admin; generate Sig Ed25519; assert attributes; User/81; sign raw message; assert 64 bytes. No host verify. Leaves Sig/counter. |

## Isolated, high-value independent probes

Retain statuses, actual body execution counts, errors and recovery. Scripts need finite timeouts,
operation tokens and finally-scoped secret clearing. Use explicit known disposable test keys.
Independent host crypto should use another implementation from the SDK formatter where practical.

1. **User identity/counters (C001).** From verified reset, install valid S2K with distinct User/Reset/Admin
   salts and correct initial hashes. Verify User/81 and Admin using logical default PINs. Attempt
   User/82 once in a fresh SDK session; capture SW and before/after C4. Compare actual User versus Reset
   retries with the exception count. Compare with pinned Python/raw APDU using User-derived bytes and
   P2=82. Avoid repeated guesses. Reset/read back before the next case. Equal salts hide this defect.
2. **KDF transition (C002/C003).** Run the existing KDF method alone after fresh reset. Separately enable
   F9 with correct initial hashes and verify logical User/Admin defaults **before extra Change calls**.
   Then change to nondefault credentials and verify 81/82 in same/fresh sessions. Probe Reset Code
   invalidation separately. Disable with KdfNone; verify actual default PINs/raw format in a fresh
   session. Read F9/C4 independently at each boundary; same-object GetKdf proves cache behavior only.
   Record the exact failing APDU and bounded reset recovery. Test alias/disposal lifetime with fake
   backends rather than spending hardware PIN retries.
3. **RSA/ECDSA generation-signing (C006/C016).** Decode generated Sig public keys and verify signatures
   with host RSA/ECDSA; verify rejection for a changed message. Record raw RSA size versus DER ECDSA.
   Test Always with two signs after one verification, then Once separately, observing C4 and counter.
   Existing import tests already provide host verification for RSA/P256/P384/Aut.
4. **Ed25519 (C006).** Import RFC8032 test-1 seed, sign the empty message and compare the RFC signature
   or independently verify it. If needed, compare two hashAlgorithm arguments for the same input to
   establish the ignored parameter. A 64-byte result alone is insufficient.
5. **X25519 agreement (C007).** Import RFC7748 Alice scalar to Dec and validate the public key. Submit
   Bob's raw 32-byte public key through SDK Decrypt and record status. Compare with correctly wrapped
   `A6 {7F49 {86 key}}` through an authorized Python/raw probe and the RFC shared secret. Current SDK
   code sends unwrapped X25519 bytes. Reverse only the private-import profile conversion, keeping
   public input encoding explicit. Clear shared secrets and keep them out of logs.
6. **Mutation/cache (C009).** Prefer fake-wire regressions for between-command failures. On hardware,
   use a disposable Sig key and record attributes/status before and after a change plus explicit 6E
   refresh. Key, certificate, fingerprint and date are separate state. Avoid synthetic communication
   failure that strands an unobserved command; cancellation success is not rollback evidence.
7. **Attestation (C012).** Preserve factory ATT material and establish UIF. Generate Sig P256; store an
   unrelated certificate; FB before User verification should fail and preserve its bytes. User/81 then
   FB should overwrite it. Compare returned public key with the generated key and independently verify
   statement signature/chain against recorded ATT certificate/trust anchor where available. Record
   trust validation separately from parsing. Probe imported-target rejection on an ordinary slot.
   Use read-only ATT CRT/public-key queries only; avoid factory private-key replacement.
8. **Touch/prompt/cancellation.** Coordinate human availability. Set a disposable Sig key On; capture
   Requested(Sig/Sign/PolicyRequires), actual touch, terminal Completed and host verification. Probe
   Aut/Dec/ATT with reversible On where useful; attestation notification scope is Att. Cached policy
   may complete without new touch; capture PolicyMayRequire and observed behavior. Request cancellation
   at an explicit stage, await terminal result/drain, record Cancelled versus APDU Failed as observed,
   then verify fresh-session recovery. Pre-admission cancellation and an in-flight SmartCard command
   are different experiments. Use TouchTimedOut only after a verified status mapping supports it.

## Final independent reset and state readback

After all tests/probes, perform one final reset and open a fresh session. Retain independent raw/typed
readbacks for **each dimension**, rather than quoting “reset complete”:

- Identity, AID, card/app version and actual USB/NFC transport, with version provenance preserved.
- Logical default User/Admin PIN verification, retries 3/0/3, and Reset Code unset/inactive. Verify
  retry defaults after any custom-policy probe instead of inheriting the last test's assumptions.
- KDF disabled through raw F9/read interpretation; signature policy and length limits; counter zero;
  ordinary algorithm defaults; ordinary keys None and public-key absence with recorded SW.
- Sig/Dec/Aut certificates empty/absent, distinguishing selection from read failures. Fingerprints and
  dates at observed defaults. Read cardholder name/login/language/URL/private-use fields where applicable;
  mark untouched or unreadable dimensions explicitly unverified.
- Ordinary UIF Off. ATT key/certificate identity matches initial baseline; ATT UIF/metadata recorded as
  separate persisted exceptions. Restore reversible ATT UIF changes without deleting its key. If the
  original ATT policy was fixed, preserve that baseline expectation and coordinate required touch;
  avoid “recovery” by overwriting factory material.
- Session/connection disposal and a fresh read-only query where needed to establish resource recovery.

Classify every case as attempted, setup-failed, body-executed, passed, failed, skipped, early-return or
not-run. Qualification covers only the recorded key, firmware, OS and transport. Passing JIT tests
establish no Native AOT runtime, other-platform/NFC, old-firmware, FIPS-profile or full-conformance claim.
