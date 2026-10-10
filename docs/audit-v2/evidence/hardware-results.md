# Phase 2 hardware results (no touch), run by orchestrator 2026-09-25
Devices: YubiKey 5C NFC fw 5.7.4 (5.7.4 test key), YubiKey 5 NFC Enhanced PIN fw 5.8.0-alpha.2 (5.8.0 test key). Both allow-listed.

## GetInfo (python-fido2 via ykman 5.9.2 bundled python)
- 5.7.4: maxMsgSize=1536, extensions=[credProtect, hmac-secret, largeBlobKey, credBlob, minPinLength], largeBlobs=true, rk=true, maxCredCountInList=8, maxSerializedLargeBlobArray=4096
- 5.8.0: maxMsgSize=1536, extensions add hmac-secret-mc, thirdPartyPayment, sign (previewSign)
- Neither advertises the direct CTAP "largeBlob" extension (§12.4) -> #3/#4 not reachable on current YubiKeys.
- maxFragmentLength on YubiKeys = 1536-64 = 1472 >= SDK's 1024 -> #12 is non-conformant but does not fail on current YubiKeys; fails on an authenticator that omits maxMsgSize (default 960) or advertises < 1088.

## #22 YESDK-1630 PIV PIN state across processes — CONFIRMED on both devices
Script: docs/audit-v2/evidence/scripts/Yesdk1630CrossProcess/app.cs (each mode is a separate OS process).
1. setup (reset PIV, P-256 in 9A, PIN=ONCE TOUCH=NEVER)
2. Sequential processes: sign (no PIN) -> 6982 rejected; verify process (verify PIN, dispose, exit); new sign process -> STILL REJECTED (6982). I.e. when no other process holds a handle, the PIN state is lost after the victim process exits (card powered down/reset by macOS PC/SC stack when last handle closes).
3. Concurrent attacker: attacker process opens PIV session first (no PIN) and waits; victim process verifies PIN, disposes, exits; attacker then signs WITHOUT PIN -> SUCCESS, signature verifies against slot 9A public key. Reproduced on 5.7.4 and 5.8.0.
4. Same as 3 but victim sets CoreCompatSwitches.OpenSmartCardHandlesExclusively=true: victim fails to connect (sharing violation, attacker holds shared handle); attacker's later sign -> rejected 6982. Exclusive mode prevents the confusion but turns it into a connect failure (DoS) for the victim.
5. In-process: integration test YESDK1630_PinOnceStateSurvivesConnectionDisposal (three sequential SDK sessions in one process; test inverted to assert spec-correct behaviour) -> FAILS today: "Unverified handle produced a signature without PIN (verifies against slot key: True)". Also shows PIN state survives a fresh SELECT of the PIV AID (consistent with NIST SP 800-73-5 Pt2 §2.4.2: reset only when a *different* application is selected).
6. Poll variant (prebuilt binary, 5.7.4): attacker polls sign every 1s; victim verifies and keeps session open 15s -> attacker SIGNED ~1s after victim's verify, while the victim session was still open. Reset-on-dispose alone would not prevent this.
7. Fast sequential with prebuilt binary (~0.6s total for verify+sign processes) and with 5s gap: NOT reproduced (6982). Mechanism for process-exit clearing unknown (in-process dispose of handle+context does NOT clear). macOS only.
Conclusion: requires a concurrently connected local process (or same-process code); on macOS sequential processes are not affected; other platforms untested.

## #24 YESDK-1632 Ed25519 — CONFIRMED on hardware
YESDK1632_Ed25519SignsUnmodifiedShortAndLongMessages: signature for 10-byte message fails OpenSSL PureEd25519 verification against slot public key ("Signature Verification Failure").

## #28 YESDK-1636 OATH period=0 — CONFIRMED on hardware
YESDK1636_RawPutZeroPeriodDoesNotAbortNormalBatch (fixed test bug: type byte 0x11=HOTP -> 0x21=TOTP/SHA1): CalculateAllAsync throws System.DivideByZeroException at OathSession.CalculateCodeAsync; valid credential in same batch never returned. Cleanup via raw DELETE succeeded.

## #10 YESDK-1618 makeCredential up=false — CONFIRMED on hardware
YESDK1618_MakeCredentialFalseUserPresenceReturnsInvalidOption PASSED: device returns CTAP2_ERR_INVALID_OPTION (0x2C) before requesting touch. Impact = request is always rejected (functional), not a UP bypass.

## Unit repro run (consolidated worktree), filter FullyQualifiedName~AuditV2 & no hardware
Cli.Commands 4 (3 failed, 1 skipped child-body) | Core 23 (20 failed, 2 passed controls, 1 skipped) | Fido2 15 (14 failed, 1 passed control) | Management 1/1 failed | Oath 7 (6 failed, 1 passed = #26g not reproduced) | OpenPgp 4/4 failed | Piv 4/4 failed | SecurityDomain 1/1 failed | WebAuthn 21 (20 failed, 1 passed control) | YubiOtp 3/3 failed. Total 83 tests: 76 failing repros, 5 passing controls/counterexamples, 2 skipped child-process bodies.

## Pending user-present (Phase 4)
#1 PRF round trip (YESDK1609_PrfRoundTripProducesStable32ByteResult), #7 (YESDK1615_PreferredResidentKeyCanBeDiscoveredWithoutAllowList), #11 (FidoCredBlobTests.CredBlob_StoreAndRetrieve_ReturnsStoredData, needs strengthening), #13 (YESDK1621_SdkWriteIsReadablePerSpec, YESDK1621_ReadsPythonFido2WrittenBlob + C-interop.py; python-fido2 with cryptography is available at <ykman 5.9.2 bundled python>), #5 on hardware (optional).
Verified that installed python-fido2 blob.py _lb_pack/_compress are identical in algorithm to agent C's C# fixture (raw DEFLATE wbits=-15, AAD "blob"||u64le(origSize), map {1:ct||tag,2:nonce,3:origSize}).

# Phase 4 user-present results (maintainer present, 2026-09-25; tests bound to the 5.7.4 test key unless stated)
- #11 credBlob: FidoCredBlobTests.CredBlob_StoreAndRetrieve_ReturnsStoredData, tightened to assert unconditionally -> PASSED. YubiKey 5.7.4 tolerates getAssertion "credBlob": h'' and returns the blob (matches the audit's "a YubiKey tolerates it"). The request is still non-conformant.
- #2 required UV: new YESDK1610_RequiredUvRegistrationWithPinSucceeds -> FAILS today with WebAuthnClientError "Invalid option" (CTAP2_ERR_INVALID_OPTION) within 1 s, before touch. Independent check with python-fido2 on BOTH devices: makeCredential with options {"uv": true} + pinUvAuthParam -> INVALID_OPTION 0x2c. So WebAuthn registration with UserVerification=Required and a PIN fails on every PIN-only YubiKey: a functional break on the default path, not just non-conformance.
- #7 rk=preferred: YESDK1615 (changed to UV=Preferred to isolate it from #2) -> FAILS today: registration with residentKey=Preferred on rk-capable 5.7.4, then getAssertion without allowList returns [] (credential not discoverable).
- #1 PRF: YESDK1609 (UV=Preferred) -> FAILS at registration: ClientExtensionResults.Prf.Enabled == null. The authenticator ignored the non-spec "prf" CTAP extension and returned no hmac-secret output, although GetInfo advertises hmac-secret. The assertion stage was not reached.
- #13 largeBlob: YESDK1621_SdkWriteIsReadablePerSpec -> FAILS: SetBlobAsync throws InvalidOperationException "next CBOR data item is of major type '5'" while reading the existing array. python-fido2 read_blob_array() shows the 5.7.4 array already holds 1 spec-conformant map entry (written earlier by another client). So the SDK cannot write large blobs on a device that holds spec-format data. On 5.8.0 test key (5.8.0) the array holds 2 entries that python-fido2 sees as raw `bytes`, i.e. SDK private-format entries left by earlier SDK runs, which no spec client can decrypt.
- Not run: #5 on hardware (the unit repro suffices); python->SDK read interop (superseded by the observation above); FIDO reset (not needed).
