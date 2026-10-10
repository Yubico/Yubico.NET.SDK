# G — applet audit verification at fdfcd6fd

### #25 Invalid OATH periods abort batch code calculation (YESDK-1633) — audit severity MED
- Verdict: CONFIRMED (same root cause as #28) | My severity: medium.
- Location at HEAD: `src/Oath/src/Credential.cs:135-153`, `src/Oath/src/OathSession.cs:520-580` (especially 561, 568-570 and 450); line 450 is individual calculation, not the batch's initial timestamp division.
- Spec / contract: “More specifically, T = (Current Unix time - T0) / X, where the default floor function is used in the computation.” — RFC 6238 §4.2; “Performs CALCULATE for all available credentials, returns name + response for TOTP and just name for HOTP and credentials requiring touch.” — [YKOATH protocol, CALCULATE ALL](https://developers.yubico.com/OATH/YKOATH_Protocol.html#_calculate_all_instruction). Zero is not a defined time step. Google [Key URI Format, Period](https://github.com/google/google-authenticator/wiki/Key-Uri-Format) actually says “The `period` parameter defines a period that a TOTP code will be valid for, in seconds. The default value is 30.” **It does not literally say “positive integer”;** that requested attribution is incorrect.
- Reference implementations: `yubikey-manager/yubikit/oath.py:200-209,491-529,243-251` also parses `0/` as zero and recalculates, then Python raises `ZeroDivisionError`. The reference is vulnerable too; it is not proof of a fix.
- Evidence: a `0/demo` name is parsed as period zero; a truncated response for the nonstandard period calls `CalculateCodeAsync`, which divides by that period before the next result is reached.
- Repro: `src/Oath/tests/Yubico.YubiKit.Oath.UnitTests/AuditV2/OathAuditReproTests.cs:YESDK1636_MalformedPeriodDoesNotAbortNormalBatchEntry` — FAILED as expected, `DivideByZeroException: Attempted to divide by zero`; response contains a normal entry after `0/demo`.
- Remediation: validate positive periods at URI import / creation and at the untrusted response boundary (`CredentialData`, `Credential.ParseCredentialId`, `OathSession.CalculateAllAsync`); skip/report a malformed entry while preserving valid entries, rather than recalculating it. API impact: none if existing `ArgumentException`/malformed-response convention is retained. Proving test: batch test above. Depends on: #28. Open decisions: whether expose malformed credentials with `null` code or omit them; recommend omit with diagnostics, so no invalid `Credential` escapes.

### #25 OpenPGP KDF count truncation (YESDK-1633) — audit severity MED
- Verdict: CONFIRMED | My severity: medium (malicious/invalid card-supplied KDF object, no PIN entropy in derived value).
- Location at HEAD: `src/OpenPgp/src/Kdf.cs:319-329` casts unsigned 32-bit count to signed `int`; `:214-257` processes only that many bytes. Both #25 truncation and #26 short-count issue meet here.
- Spec / contract: “The input is truncated to the octet count, except if the octet count is less than the initial size of the salt plus passphrase. That is, at least one copy of the full salt plus passphrase will be provided as input to each hash context regardless of the octet count.” — [RFC 9580 §3.7.1.3](https://www.rfc-editor.org/rfc/rfc9580.html#section-3.7.1.3). [OpenPGP card 3.4.1 §4.3.2](https://gnupg.org/ftp/specs/OpenPGP-smart-card-application-3.4.pdf): “The KDF-DO has the following format and shall be evaluated by the terminal software, if present”; its tag `83 04` holds the “Iteration count (long integer)”. The card treats it as opaque.
- Reference implementations: `yubikey-manager/yubikit/openpgp.py:756-765,790-807` reads unsigned with `bytes2int` (so no .NET signed wrap), but also hashes only `iteration_count` bytes even if less than salt + PIN; it too departs from RFC 9580 for short counts.
- Evidence: 0x80000000 becomes -2147483648, and `Math.DivRem`/loop hash no positive amount (or a short negative slice) rather than rejecting it; counts 8/9 hash just salt / salt plus one PIN byte.
- Repro: `src/OpenPgp/tests/Yubico.YubiKit.OpenPgp.UnitTests/AuditV2/OpenPgpAuditReproTests.cs:YESDK1633_UnsignedKdfCountDoesNotWrapNegative` — FAILED as expected, `Assert.Throws: No exception was thrown`; `YESDK1633_ShortKdfCountStillHashesCompletePin` (8 and 9) — both FAILED, digests differ from SHA256(salt || entire PIN).
- Remediation: decode 83 as unsigned with checked range / resource-bound policy; reject unprocessable values rather than signed-wrap. In `DoProcess`, process `Math.Max(iterationCount, data.Length)` bytes. API impact: potentially breaking rejection of absurd card-supplied values; document bound. Proving tests: both above. Depends on: #26 KDF sub-item. Open decisions: max accepted count (memory/time denial-of-service versus compatibility); recommend bounded checked count following supported device ranges.

### #26a OpenPGP import target mismatch (YESDK-1634) — audit severity MED
- Verdict: CONFIRMED | My severity: medium.
- Location at HEAD: `src/OpenPgp/src/IOpenPgpSession.cs:212-225`, `OpenPgpSession.Keys.cs:48-92`, `PrivateKeyTemplate.cs:35-45,97-108` (no material drift).
- Spec / contract: “The DOs in the Extended Header list start with a Control Reference Template (CRT) of the referenced key. Digital signature: B6 00 (Tag, Length) Confidentiality: B8 00 Authentication: A4 00” — [OpenPGP card 3.4.1 §4.4.3.12 Private Key Template](https://gnupg.org/ftp/specs/OpenPGP-smart-card-application-3.4.pdf). SDK interface promises “The target key slot” for `keyRef`.
- Reference implementations: `yubikey-manager/yubikit/openpgp.py:1581-1605` constructs `_get_key_template(private_key, key_ref, ...)` from the **same requested** reference used for algorithm attributes.
- Evidence: attributes go to SIG (`C1`), but `template.ToBytes()` sends AUT CRT `A4 00` inside `4D` when a caller requests SIG with an AUT template.
- Repro: `src/OpenPgp/tests/Yubico.YubiKit.OpenPgp.UnitTests/AuditV2/OpenPgpAuditReproTests.cs:YESDK1634_ImportDoesNotTargetTemplateSlotInsteadOfRequestedSlot` — FAILED as expected, expected `B6 00`, actual `A4 00` in recorded import command.
- Remediation: reject a mismatch before any attribute mutation, or encode CRT from `keyRef`. Recommend reject (single source of truth after removing redundant target from the template in a later breaking release). API impact: behavior change for mismatched arguments; eventually breaking removal of duplicate template ref. Proving test: recorded commands test, with no attribute command on rejected input. Depends on: none. Open decisions: reject or override template CRT; recommend reject to prevent accidental cross-slot operations.

### #26b OpenPGP attestation replaces cardholder certificate (YESDK-1634) — audit severity MED
- Verdict: BY-DESIGN (missing SDK warning, not an SDK-specific overwrite) | My severity: medium documentation footgun.
- Location at HEAD: `src/OpenPgp/src/IOpenPgpSession.cs:241-254`, `OpenPgpSession.Keys.cs:142-175`.
- Spec / contract: “When an attestation statement is generated, it is placed in the OpenPGP cardholder certificate slot for the attested key. ... Any data currently stored in the cardholder certificate slot is overwritten.” — [Yubico OpenPGP Attestation, Implementation](https://developers.yubico.com/PGP/Attestation.html). Same page, Generate Attestation: “Upon successful completion (SW 0x9000), the Attestation Statement is written to the corresponding Cardholder Certificate slot.”
- Reference implementations: `yubikey-manager/yubikit/openpgp.py:1699-1713` explicitly documents the slot write, sends `GET_ATTESTATION`, and reads back `get_certificate`.
- Evidence: a single `GET_ATTESTATION` APDU triggers **device** overwrite before SDK `GetCertificateAsync`; restoring a previous cert is not done and may require admin authorization. No hardware test written: cannot meaningfully simulate a real card's certificate mutation with a recorded response, and running on a populated key could destroy user data.
- Remediation: add explicit destructive side-effect warning to `IOpenPgpSession.AttestKeyAsync` and usage docs. API impact: none. Proving test: documentation review or carefully provisioned dedicated-device test only. Depends on: none. Open decisions: opt-in preflight/backup feature vs documentation; recommend documentation first; avoid promising atomic restore.

### #26c OATH credential device identity ignored (YESDK-1634) — audit severity MED
- Verdict: CONFIRMED | My severity: medium.
- Location at HEAD: `src/Oath/src/Credential.cs:23-27,169-188`, `OathSession.cs:333-343,346-377,385-405,437-472`.
- Spec / contract: “Credentials are uniquely identified by the combination of DeviceId and Id.” — SDK `Credential` XML contract, `Credential.cs:23-27`.
- Reference implementations: `yubikey-manager/yubikit/oath.py:534-544` checks `credential.device_id != self.device_id` for `calculate_code`, but `:428-489` rename/delete accept raw credential IDs and cannot check a foreign credential handle; Python's partial check is not uniform.
- Evidence: the .NET delete, rename, calculate, and calculate-code methods transmit the supplied `Id` on the current session, never compare its `DeviceId` with `OathSession.DeviceId`.
- Repro: `src/Oath/tests/Yubico.YubiKit.Oath.UnitTests/AuditV2/OathAuditReproTests.cs:YESDK1634_ForeignCredentialCannotTargetLocalDevice` — four variants FAILED as expected (`Assert.Throws: No exception was thrown`); recorder had a same-named credential and returned success.
- Remediation: compare `credential.DeviceId` to session `DeviceId` before constructing/transmitting any command, throw `ArgumentException`. API impact: rejects previously accepted cross-device handles. Proving test: four operation variants and zero commands after initial SELECT. Depends on: none. Open decisions: exception type; recommend `ArgumentException` consistently.

### #26d OTP update security flags (YESDK-1634) — audit severity MED
- Verdict: CONFIRMED-WITH-CORRECTION | My severity: medium for AllowUpdate; low for ProtectSlot2.
- Location at HEAD: `src/YubiOtp/src/SlotConfiguration.cs:48,65-72,119-126,155-158`, `ExtendedFlag.cs:37-46`, `TicketFlag.cs:38-46`, `UpdateConfiguration.cs:133-140`.
- Spec / contract: `UpdateConfiguration.cs:19-29` says “Only flags within the defined update masks are written to the device.” The SDK's own `SlotConfiguration.cs:65-70` promises “Enables or disables the AllowUpdate flag, permitting future update operations on this slot.” The local mask **incorrectly excludes** AllowUpdate; ProtectSlot2 is not updateable in the canonical implementation.
- Reference implementations: `yubikey-manager/yubikit/yubiotp.py:167-186` explicitly **includes** `EXTFLAG.ALLOW_UPDATE` in update mask; `:556-578` raises `ValueError` on `protect_slot2` for update configuration. Thus the two setters need **different** treatment, not both transmitted.
- Evidence: `UpdateConfiguration.AllowUpdate(true)` and `.AllowUpdate(false)` both emit byte 45 = 4; `.ProtectSlot2()` silently clears bit 0x40 in serialized ticket flags.
- Repro: `src/YubiOtp/tests/Yubico.YubiKit.YubiOtp.UnitTests/AuditV2/YubiOtpAuditReproTests.cs:YESDK1634_UpdateDoesNotSilentlyDiscardAllowUpdate` — FAILED, expected different bytes, both 4; `YESDK1634_UpdateRejectsUnsupportedProtectSlot2` — FAILED, no exception.
- Remediation: add `ExtendedFlag.AllowUpdate` to update mask; reject `ProtectSlot2` on `UpdateConfiguration` rather than transmitting it. API impact: behavior change (unsupported call throws). Proving tests: both above. Depends on: none. Open decisions: hide inherited method in update builder vs validate in base; recommend explicit update-builder rejection and document it.

### #26e Security Domain empty allowlist (YESDK-1634) — audit severity MED
- Verdict: CONFIRMED-WITH-CORRECTION (cannot represent deny-all with an empty list on device) | My severity: high for callers using empty as deny-all.
- Location at HEAD: `src/SecurityDomain/src/SecurityDomainSession.cs:741-788`.
- Spec / contract: “If an allowlist with one or more Certificate Serial Number entries exists in the SD for the CA-KLOC’s public key, the SD also verifies that the certificate is contained in an allowlist. Else the SD accepts all certificates signed by the CA-KLOC.” Also: “special care shall be taken never to empty/remove the allowlist ... because no restrictions apply ... once an allowlist is removed.” — [GlobalPlatform Card Specification Amendment F SCP11 v1.3, §3.3 authentication](https://globalplatform.org/wp-content/uploads/2023/08/GPC_2.3_F_SCP11_v1.3.0.13_PublicRvw.pdf). SDK `StoreAllowListAsync` XML `:745-747` likewise warns no-list permits any CA-signed certificate.
- Reference implementations: `yubikey-manager/yubikit/securitydomain.py:251-263` also serializes an empty tag `70` for an empty `serials` sequence, with no guard.
- Evidence: `StoreAllowListAsync(key, [])` serializes zero-length tag `70`; `ClearAllowListAsync` explicitly calls the same method with `[]`.
- Repro: `src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.UnitTests/AuditV2/SecurityDomainAuditReproTests.cs:YESDK1634_EmptyAllowListDoesNotIssueRestrictionRemovingStoreData` — FAILED as expected, no `ArgumentException`, STORE DATA sent. The fake cannot establish hardware authorization outcome; the spec does.
- Remediation: reject empty lists in `StoreAllowListAsync` before I/O; retain explicit `ClearAllowListAsync` with its own explicit wire implementation; document absence means unrestricted and deny-all requires disabling/removing CA trust/key rather than storing zero entries. API impact: previously accepted empty input now throws. Proving test: rejection + no STORE DATA, and separate explicit clear test. Depends on: none. Open decisions: intended deny-all policy; recommend explicit key/CA removal instead of an illusory empty allowlist.

### #26f Management builder reuse (YESDK-1634) — audit severity MED
- Verdict: CONFIRMED | My severity: medium.
- Location at HEAD: `src/Management/src/DeviceConfig.cs:148-161,202-215`, `ManagementSession.cs:173`.
- Spec / contract: `DeviceConfig.Builder.Build()` rejects zero USB capabilities (`DeviceConfig.cs:202-205`); `DeviceConfig.EnabledCapabilities` is exposed as `IReadOnlyDictionary` (`:42`), suggesting stable snapshot semantics, though not an explicit immutability guarantee.
- Reference implementations: management device-config serialization in `yubikey-manager/yubikit/management.py` operates on a concrete configuration object, not this .NET mutable-builder/read-only-view pattern; no equivalent builder reuse policy to compare.
- Evidence: `new ReadOnlyDictionary` wraps the original `_enabledCapabilities`, so `.WithCapabilities(Usb, 0)` mutates a previously built configuration after its validation. `GetBytes` then reads it without revalidation.
- Repro: `src/Management/tests/Yubico.YubiKit.Management.UnitTests/AuditV2/ManagementAuditReproTests.cs:YESDK1634_BuiltConfigurationIsSnapshotOfBuilder` — FAILED, expected USB 32, actual 0.
- Remediation: copy dictionary in `Build`, then wrap copy; optionally validate at serialization for direct `DeviceConfig` initializers. API impact: none for builder users; additional validation of directly constructed config could reject existing callers. Proving test: snapshot test plus direct-object validation if chosen. Depends on: none. Open decisions: builder-only or session-boundary validation; recommend both for policy invariants.

### #26g OATH authentication retry zero seed (YESDK-1634) — audit severity MED
- Verdict: NOT-REPRODUCED at HEAD (claim conflates password and derived key) | My severity: none for alleged all-zero retry.
- Location at HEAD: `src/Oath/src/OathSession.cs:757-825`.
- Spec / contract: `OathSession.cs:782-805` promises “Validates an already-derived access key and unconditionally zeroes it once the attempt completes”; `IOathSession` retry contract uses a `passwordProvider` to supply a borrowed password, not a key to send twice.
- Reference implementations: `yubikey-manager/yubikit/oath.py:230-231` derives a PBKDF2 key from a passphrase; `:243-251` use the key for challenge HMAC; no corresponding .NET `AuthenticateAndRetryAsync` retry helper exists there.
- Evidence: retry calls `passwordProvider` once, `DeriveKey(password)` creates a new array, `AuthenticateWithDerivedKeyAsync` zeroes that **derived** array after `ValidateAsync`; operation retries after success, not a second `ValidateAsync` on a zeroed key. No path zeroes `password` in this method. Test `src/Oath/tests/Yubico.YubiKit.Oath.UnitTests/AuditV2/OathAuditReproTests.cs:YESDK1634_RetryDoesNotZeroCallerPassword` PASSED (same password bytes after wrong-key response, only one VALIDATE command). It pins the opposite of the audit claim; no intentionally failing test was added for an unsubstantiated issue.
- Remediation: none for this finding. API impact: none. Proving test: above. Depends on: none. Open decisions: none.

### #26h YubiOTP HMAC challenge length ambiguity (YESDK-1634) — audit severity MED
- Verdict: CONFIRMED-WITH-CORRECTION (encoding collision real, but cannot promise distinct output for mixed modes without knowing slot configuration) | My severity: low/medium.
- Location at HEAD: `src/YubiOtp/src/YubiOtpSession.cs:351-390,426-444`.
- Spec / contract: [Yubico OTP challenge-response manual](https://docs.yubico.com/yesdk/users-manual/application-otp/challenge-response.html) says “The size of the challenge sent to the YubiKey with `UseChallenge()` must align with the slot's configuration. ... If the slot has been configured with `UseSmallChallenge()`, a challenge smaller than 64 bytes is acceptable.” [Programming manual](https://docs.yubico.com/yesdk/users-manual/application-otp/how-to-program-a-challenge-response-credential.html): “An HMAC-SHA1 challenge is 64 bytes by default. The YubiKey also supports a short challenge mode ... less than 64 bytes.” `IYubiOtpSession.cs:211-216` instead accepts all sizes up to 64 without a mode parameter.
- Reference implementations: `yubikey-manager/yubikit/yubiotp.py:901-925` also left-justifies with `b"\1" if challenge.endswith(b"\0") else b"\0"`; canonical padding **does** differ from last challenge byte for a trailing zero, but `0x41` and `0x41 + 63 zeros` still serialize identically. Slot configuration is not queryable at this call site; identity of **device HMAC output** depends on mode/firmware, not just wire identity.
- Evidence: `PadHmacChallenge([0x41])` and `PadHmacChallenge([0x41, 0...])` both produce 64 bytes `41 00...00`. No mode metadata is available at calculation time.
- Repro: `src/YubiOtp/tests/Yubico.YubiKit.YubiOtp.UnitTests/AuditV2/YubiOtpAuditReproTests.cs:YESDK1634_DistinctChallengeLengthsDoNotHaveSameRequestBytes` — FAILED, collections equal. This proves wire collision, not device-side digest equality.
- Remediation: expose/require explicit challenge mode at invocation or document restriction that callers must use a uniform input width matching the configured slot; reject ambiguous lengths for short mode if strict injectivity is required. API impact: additive mode overload and potentially breaking validation for existing method. Proving test: record two request payloads per mode and assert documented rejection. Depends on: none. Open decisions: how to handle existing mode-less API; recommend document its inability to guarantee distinct variable/fixed messages and add an explicit mode API for strict clients.

### #26i OpenPGP KDF short count excludes PIN (YESDK-1634) — audit severity MED
- Verdict: CONFIRMED | My severity: medium.
- Location at HEAD: `src/OpenPgp/src/Kdf.cs:214-257,319-329`.
- Spec / contract: RFC 9580 §3.7.1.3 exact quote above: “at least one copy of the full salt plus passphrase will be provided ... regardless of the octet count.” [OpenPGP card 3.4.1 §4.3.2](https://gnupg.org/ftp/specs/OpenPGP-smart-card-application-3.4.pdf) KDF-DO tag 83 holds count and terminal evaluates it.
- Reference implementations: `yubikey-manager/yubikit/openpgp.py:756-765,804-807` makes the same short-count mistake (`digest.update(data[:trailing_bytes])`).
- Evidence: count 8 SHA256 hashes 8-byte salt only; 9 hashes salt and first PIN byte. Passphrase is not incorporated in full.
- Repro: `src/OpenPgp/tests/Yubico.YubiKit.OpenPgp.UnitTests/AuditV2/OpenPgpAuditReproTests.cs:YESDK1633_ShortKdfCountStillHashesCompletePin` (counts 8 and 9, tagged both IDs) — FAILED as expected, digests differ from SHA256(salt || PIN).
- Remediation: hash one full salt+PIN block minimum, even when `IterationCount` is smaller. API impact: derived output changes for invalid/small counts; important PIN migration risk on cards provisioned with them. Proving test: both short-count vectors. Depends on: #25 count truncation (unsigned conversion and bounds). Open decisions: honor RFC or preserve legacy hashes for installed cards; recommend RFC behavior with explicit migration warning for affected cards.

## Changed
- `src/Oath/tests/Yubico.YubiKit.Oath.UnitTests/AuditV2/OathAuditReproTests.cs:1` — URI, batch, cross-device, and retry tests.
- `src/Oath/tests/Yubico.YubiKit.Oath.IntegrationTests/AuditV2/OathAuditReproTests.cs:1` — raw PUT hardware repro; **built, not run**; no touch, but changes OATH state and cleans up both named credentials in `finally`.
- `src/OpenPgp/tests/Yubico.YubiKit.OpenPgp.UnitTests/AuditV2/OpenPgpAuditReproTests.cs:1` — import APDU and KDF vectors.
- `src/YubiOtp/tests/Yubico.YubiKit.YubiOtp.UnitTests/AuditV2/YubiOtpAuditReproTests.cs:1` — update flags and challenge collision.
- `src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.UnitTests/AuditV2/SecurityDomainAuditReproTests.cs:1` — empty allowlist wire repro.
- `src/Management/tests/Yubico.YubiKit.Management.UnitTests/AuditV2/ManagementAuditReproTests.cs:1` — immutable snapshot regression test.

## Verification
From `<repo root>`:

```
$ dotnet toolchain.cs -- build --project Oath
3 projects built; each 0 warnings, 0 errors.
$ dotnet toolchain.cs -- build --project OpenPgp
3 projects built; each 0 warnings, 0 errors.
$ dotnet toolchain.cs -- build --project YubiOtp
3 projects built; each 0 warnings, 0 errors.
$ dotnet toolchain.cs -- build --project Management
3 projects built; each 0 warnings, 0 errors after import correction.
$ dotnet toolchain.cs -- build --project SecurityDomain
3 projects built; each 0 warnings, 0 errors.
$ dotnet toolchain.cs -- test --project Oath --filter "FullyQualifiedName~AuditV2&Category!=RequiresHardware&Category!=RequiresUserPresence"
total: 7; failed: 6; succeeded: 1; skipped: 0 (last run).
$ dotnet toolchain.cs -- test --project OpenPgp --filter "FullyQualifiedName~AuditV2&Category!=RequiresHardware&Category!=RequiresUserPresence"
total: 4; failed: 4; succeeded: 0; skipped: 0.
$ dotnet toolchain.cs -- test --project YubiOtp --filter "FullyQualifiedName~AuditV2&Category!=RequiresHardware&Category!=RequiresUserPresence"
total: 3; failed: 3; succeeded: 0; skipped: 0.
$ dotnet toolchain.cs -- test --project SecurityDomain --filter "FullyQualifiedName~AuditV2&Category!=RequiresHardware&Category!=RequiresUserPresence"
total: 1; failed: 1; succeeded: 0; skipped: 0 (last run).
$ dotnet toolchain.cs -- test --project Management --filter "FullyQualifiedName~AuditV2&Category!=RequiresHardware&Category!=RequiresUserPresence"
total: 1; failed: 1; succeeded: 0; skipped: 0.
```

**Hardware command, NOT RUN:** `dotnet toolchain.cs -- test --integration --project Oath --filter "FullyQualifiedName~YESDK1636_RawPutZeroPeriodDoesNotAbortNormalBatch"`. No human touch required. Uses only allowed devices selected by `[WithYubiKey]` harness; raw PUT changes OATH credentials and deletes test IDs in `finally`. Do not run alongside another device user.

## Tests
All six test files were written before any production modification (there were no production modifications); observed intended failures above. Escape hatch not used. No tests were written for #26b because a fake cannot reproduce device-side mutation; the governing device documentation is decisive. #26g has a passing counterexample rather than a fabricated failing test.

## Uncertain
- The real device might refuse raw PUT of `0/` on some firmware; hardware test is built but never run. It preflights both test credential IDs to avoid overwriting preexisting credentials.
- OpenPGP oversized-count handling needs a documented resource limit and a compatibility decision for previously provisioned short-count KDFs.
- For OTP short-versus-fixed challenge inputs, recorded bytes collide; actual device digest equality is configuration dependent.

Abbreviations: OATH = Initiative for Open Authentication (OTP credential applet); OTP = one-time password; TOTP = time-based one-time password; HOTP = HMAC-based one-time password; HMAC = hash-based message authentication code; URI = uniform resource identifier; API = application programming interface; SDK = software development kit; KDF = key derivation function; S2K = string-to-key; PIN = personal identification number; APDU = application protocol data unit; CRT = control reference template; DO = data object; SCP = secure channel protocol; CA = certificate authority; USB = universal serial bus; HMAC-SHA1 = HMAC using secure hash algorithm 1; RFC = request for comments; XML = extensible markup language; TLV = tag-length-value; SHA256 = secure hash algorithm 256-bit; SIG = signature key slot; AUT = authentication key slot; .NET = Microsoft .NET development platform.
