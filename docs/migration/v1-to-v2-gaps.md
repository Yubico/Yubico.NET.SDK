# V1 to V2 Feature/Behavior Gap Analysis (Handoff)

## Metadata

- **Generated**: 2026-07-21
- **Generated from working branch**: `yubico/fork-investigation-v2-readiness` @ `7bb8f7893aadc5c89836dffa4a2b021ca1354599`
- **v1 baseline**: `develop` @ `7a186deb97ef1484812e5da76a54843b911b4475`
- **v2 baseline**: `yubikit` @ `f807251434090393071e51b006aa3c319ce528aa` (matched `origin/yubikit` at analysis time)
- **Method**: 9 parallel read-only code-exploration agents diffed public API surface and observable behavior between the two branches, module by module. Library/package API surface only — CLI tooling and sample apps were explicitly out of scope.

## ⚠️ Caveat for whoever picks this up next

This was diffed against the `yubikit` **integration branch tip only, at the commit above**. It does **not** account for unmerged feature branches that may already close some of these gaps, including but not limited to:
`yubikit-piv`, `yubikit-oath`, `yubikit-openpgp`, `yubikit-hsmauth`, `yubikit-yubiotp`, `yubikit-transaction`, `yubikit-smartcard-improvements`, `yubikit-piv-example`, `yubikit-fido2-cli`, `yubikit-consolidation`, `yubikit-performance`, `yubikit-webauthn-puat-retry`, `yubikit-ctap-status-22-alignment`, `yubikit-ctaphid-sequence-wrap`, `yubikit-device-info-more-data-count`, `yubikit-apdu-always-append-le`, `yubikit-piv-9-byte-aid`.

**Any remediation work based on this report must re-verify each finding against the current `yubikit` tip (and any branches merged into it since) before planning or fixing it.** Treat this as a point-in-time snapshot, not current truth.

---

## Top-line: biggest risks to "v2 is better in almost every way"

Ranked roughly by blast radius. Full detail for each is in the module sections below.

| # | Gap | Module | Severity |
|---|-----|--------|----------|
| 1 | No .NET Framework/netstandard support — v2 is net10.0-only, v1 supported net472/netstandard2.0/2.1 | Cross-cutting | Blocker (for that consumer segment) |
| 2 | U2F protocol entirely removed — no register/authenticate, no `U2fSession` equivalent | FIDO2 | Major/Blocker for U2F-only consumers |
| 3 | PIV PIN-only mode gone — `PivSession.Pinonly.cs`/PIN-derived management key has no v2 equivalent | PIV | Major |
| 4 | `KeyCollector` delegate pattern removed everywhere — each applet (Piv/Oath/Fido2/YubiHsm) handles PIN/PUK/touch with direct parameters. A shared async SDK-to-application prompt primitive, `ICredentialPrompt`, now exists in Core but is adopted only by WebAuthn so far; the application-initiated `ISecureCredentialReader` terminal helper serves a different role | Cross-cutting | Major |
| 5 | Legacy pre-5.0 firmware mode switching removed from public API (`SetLegacyDeviceConfiguration`) — YubiKey NEO/4 users can't reconfigure interfaces at all | Management | Major |
| 6 | Pluggable crypto primitives gone (`IAesGcmPrimitives`/`IEcdhPrimitives`/`ICmacPrimitives` extension points) | Core | Major |
| 7 | `TlvReader`/`TlvWriter` typed sequential API gone, replaced by a much thinner `Tlv`/`TlvHelper` | Core | Major |
| 8 | `Base16`/`Base32`/`Bcd`/`ModHex` general-purpose codecs gone | Core | Major |
| 9 | Exception hierarchy shrank (10→8 types) — `TlvException`, `SecureChannelException`, `KeyboardConnectionException` have no v2 equivalent; SecurityDomain/YubiOtp/OpenPgp/Management/YubiHsm/Oath have zero dedicated exception types | Cross-cutting | Major |
| 10 | PIV typed data objects gone (CHUID/CCC/AdminData/KeyHistory) — replaced by raw get/put-object with no parsed fields | PIV | Major |
| 11 | OATH loses `IsPasswordProtected` signal and auto-retry-on-lock behavior; exception on wrong password is now generic, not `SecurityException` | OATH | Major |
| 12 | YubiOTP loses: string+keyboard-layout static passwords, Yubico-OTP-algorithm challenge-response, touch-notify callback, NDEF read-back, and silently hashes/pads wrong-length HMAC keys instead of throwing | YubiOTP | Major |
| 13 | YubiHSM Auth loses interactive retry (`Try*`) and touch-notify callback; possible mislabeled retry-counter field (`Counter` vs "retries remaining") needs hardware verification | YubiHsm | Major/needs-check |
| 14 | Logging is silent by default — v1 auto-configured console logging at Error level; v2 defaults to `NullLoggerFactory` until explicitly configured | Core | Minor (but easy to miss) |
| 15 | No meta-package — v1 was 1 package, v2 is 9 with nothing bundling "all applets" | Cross-cutting | Minor |

---

## Module: Core / Discovery / Transport / Crypto Utilities

v1 locations: `Yubico.Core/src/Yubico/Core/**`, `Yubico.YubiKey/src/Yubico/YubiKey/{Pipelines,InterIndustry,DeviceExtensions,Utilities,Cryptography}/**`
v2 location: `src/Core/src/**`

- **Feature/API**: `IAesGcmPrimitives`/`IEcdhPrimitives`/`ICmacPrimitives` pluggable crypto interfaces + `CryptographyProviders.*Creator` settable factories
  **v2 status**: Missing (Degraded) — evidence shows the `Func<...>Creator` properties literally commented out in `src/Core/src/Cryptography/CryptographyProviders.cs`; AES-CMAC now hardcoded internal to SCP code.
  **User impact**: Apps that swapped in custom/hardware-backed AES-GCM, ECDH, or CMAC implementations (e.g. HSM-backed SCP key material, FIPS validation) have no extension point in v2.
  **Severity**: Major | **Confidence**: High

- **Feature/API**: `TlvReader`/`TlvWriter`/`TlvObject`/`TlvObjects` sequential typed TLV parsing API
  **v2 status**: Missing (Degraded) — v2's `Tlv`/`TlvHelper`/`DisposableTlvList` only offer single-object tag/value containers and static decode/encode helpers, no typed sequential reader or fluent writer.
  **User impact**: Consumers parsing custom TLV-encoded vendor data must hand-roll parsing against raw `Memory<byte>`.
  **Severity**: Major | **Confidence**: High

- **Feature/API**: `DisposableTlvDictionary`
  **v2 status**: Missing — entire class body is commented out in `src/Core/src/Utilities/DisposableTlvDictionary.cs`; dead code left in tree.
  **User impact**: None today (unreferenced), but signals incomplete/abandoned type.
  **Severity**: Minor | **Confidence**: High

- **Feature/API**: `Base16`/`Base32`/`Bcd`/`ModHex`/`ITextEncoding` standalone codec utilities
  **v2 status**: Missing — no general-purpose equivalents; Base32-like logic only reappears inline in `src/Oath/src/CredentialData.cs`, ModHex only inside private `HidCodeTranslator`.
  **User impact**: Apps encoding/decoding OTP public IDs, modhex serials, or BCD card data have no supported replacement.
  **Severity**: Major | **Confidence**: High

- **Feature/API**: `Log`/`Logger` static façade with automatic `appsettings.json` discovery and default console-at-Error fallback
  **v2 status**: Behavior-changed (Degraded default) — `YubiKitLogging.LoggerFactory` defaults to `NullLoggerFactory.Instance` (silent) until explicit `Configure(...)`; no JSON auto-discovery; `CreateLogger<T>()` is internal.
  **User impact**: Apps upgrading without an explicit `YubiKitLogging.Configure(...)` call silently lose all SDK diagnostics they got "for free" in v1.
  **Severity**: Minor | **Confidence**: High

- **Feature/API**: `TlvException` (`InvalidOperationException` subclass for TLV parse errors)
  **v2 status**: Missing — generic `ArgumentException`/`InvalidOperationException` thrown instead.
  **User impact**: Code catching `TlvException` specifically won't compile/catch correctly.
  **Severity**: Minor | **Confidence**: Medium

**Verified parity/improvement, no gap**: device discovery/hot-plug (`YubiKeyManager.WatchAsync` for `await foreach`; richer than v1's event-based `YubiKeyDeviceListener` and with no third-party dependency); USB HID/CCID/NFC transports; Windows/macOS/Linux platform interop; APDU chaining (`ApduException` gains `FromResponse`/`FromStatusWord` factories); crypto key-handling types (RSA/EC/Curve25519, ASN.1, HKDF) fully ported plus new COSE/ARKG types; `SCardException`/`PlatformApiException` parity.

---

## Module: PIV

v1 location: `Yubico.YubiKey/src/Yubico/YubiKey/Piv/**`
v2 location: `src/Piv/src/**`

- **Feature/API**: PIN-only mode (PIN-protected / PIN-derived management key) — `GetPinOnlyMode()`, `TryRecoverPinOnlyMode()`, `SetPinOnlyMode()` (`PivSession.Pinonly.cs`, `PivPinOnlyMode.cs`, ~1700 lines)
  **v2 status**: Missing — no trace anywhere in `src/Piv`.
  **User impact**: Integrations relying on PIV PIN-only mode (common in smart-card minidriver / Windows CAPI / macOS setups) have no migration path.
  **Severity**: Major | **Confidence**: High

- **Feature/API**: `KeyCollector` delegate pattern driving interactive PIN/PUK/management-key collection with retry/cancel semantics, used across nearly every PIV operation
  **v2 status**: Missing — callers must pass PIN/PUK/management-key bytes directly with no built-in retry-loop callback.
  **User impact**: Apps must hand-roll retry loops around each async call using `InvalidPinException.RetriesRemaining`.
  **Severity**: Major | **Confidence**: High

- **Feature/API**: Typed PIV data object model — `CardholderUniqueId` (CHUID), `CardCapabilityContainer` (CCC), `AdminData`, `KeyHistory`, `PinProtectedData`, base `PivDataObject` with `Encode()`/`Decode()`/`TryDecode()`
  **v2 status**: Missing/Degraded — v2's `PivDataObject.cs` is only a `static class` of tag constants; `PivDataObjectProtocol.cs` exposes raw `GetObjectAsync`/`PutObjectAsync` with no parsed field access.
  **User impact**: Consumers must hand-parse/build TLV-encoded CHUID/CCC/admin-data themselves.
  **Severity**: Major | **Confidence**: High

- **Feature/API**: MSROOTS support (`WriteMsroots`, `WriteMsrootsStream`, `ReadMsroots`, `ReadMsrootsStream`, `DeleteMsroots`) for Windows minidriver root-cert bundle storage
  **v2 status**: Missing — no trace in `src/Piv`.
  **User impact**: Integrations pushing/pulling the Windows minidriver MSROOTS object have no v2 equivalent.
  **Severity**: Minor (niche/Windows-minidriver specific) | **Confidence**: High

- **Feature/API**: PIV data object tag coverage — `IrisImages`, `BiometricGroupTemplate`, `SecureMessageSigner`, `PairingCodeReferenceData`
  **v2 status**: Missing named constants — raw numeric tags still usable via `GetObjectAsync`/`PutObjectAsync`, just no discoverable named constant.
  **Severity**: Cosmetic | **Confidence**: High

- **Feature/API**: `ReplaceAttestationKeyAndCertificate(...)`/`GetAttestationCertificate()` convenience methods for the F9 attestation key/cert
  **v2 status**: Present-but-renamed — reachable via generic `GetCertificateAsync(PivSlot.Attestation)`/`ImportKeyAsync(PivSlot.Attestation, ...)`, just without dedicated named methods/docs.
  **Severity**: Cosmetic | **Confidence**: Medium

- **Feature/API**: Certificate compression default behavior
  **v2 status**: Behavior-changed — v1 only compresses when `compress: true` is explicitly passed; v2's `StoreCertificateAsync` auto-compresses any cert > 1856 bytes even when `compress: false` is passed. Format-compatible (both use GZip), arguably an improvement, but differs from the literal v1 contract.
  **Severity**: Minor | **Confidence**: High

- **Feature/API**: Non-throwing `Try*` family — `TryVerifyPin`, `TryChangePin`, `TryChangePuk`, `TryResetPin`, `TryAuthenticateManagementKey`, `TryChangeManagementKey`, `TryReadObject<T>` with `out int? retriesRemaining`
  **v2 status**: Behavior-changed — only throwing variants exist; retry info surfaced via `InvalidPinException.RetriesRemaining` or `GetPinMetadataAsync`/`GetPukMetadataAsync`. Deliberate async/exception-first style shift, not a capability loss.
  **Severity**: Minor | **Confidence**: Medium

**Verified improvement, no gap**: key generation/import (all algorithms incl. Ed25519/X25519), all 20 retired slots, Move/Delete key, AttestKey, GetMetadata, management-key ops (incl. touch policy, AES/3DES), PIN/PUK verify/change/unblock/retry-count, biometric UV + temporary PIN — v2 additionally adds touch-notification callbacks and algorithm-auto-detecting `SignOrDecryptAsync`.

---

## Module: FIDO2 / U2F / WebAuthn

v1 locations: `Yubico.YubiKey/src/Yubico/YubiKey/Fido2/**`, `.../U2f/Commands/**`
v2 locations: `src/Fido2/src/**`, `src/WebAuthn/src/**`

- **Feature/API**: U2F/CTAP1 register+authenticate (raw U2F protocol) — `U2fSession`, `RegisterCommand`, `AuthenticateCommand`
  **v2 status**: Missing — no `U2f` folder or `U2fSession` anywhere in `yubikit`. `"fido-u2f"` only exists as a CTAP2 *attestation format string*, not the legacy wire protocol.
  **User impact**: Consumers relying on `U2fSession.Register()`/`Authenticate()` (legacy U2F-only relying parties, non-CTAP2 browsers/servers) have no migration path — the entire U2F application surface is gone.
  **Severity**: Major (Blocker for U2F-only consumers) | **Confidence**: High

- **Feature/API**: `KeyCollector` delegate for PIN/touch/UV prompts on `Fido2Session`
  **v2 status**: Present-but-renamed/Behavior-changed — `Fido2Session` exposes explicit async methods (`SetPinAsync`, `ChangePinAsync`, `GetPinUvAuthTokenUsingPinAsync/UsingUvAsync`) with no callback. At the WebAuthn layer the closest analog is `ICredentialPrompt` (`Yubico.YubiKit.Core.Credentials`), an optional async SDK-to-application prompt supplied to `WebAuthnClient`: the SDK calls it when a ceremony needs a PIN and owns a bounded retry loop with a fresh, zeroed secret for each attempt. The synchronous `ISecureCredentialReader` is instead an application-initiated terminal input helper and does not replace this callback.
  WebAuthn ceremonies are plain awaitable methods; there is no progress stream and no interaction callback. Abandonment is via the cancellation token.
  Touch remains a gap: WebAuthn has no dedicated in-flight touch signal, so UI can only prompt speculatively while a ceremony may be waiting for user presence.
  No 1:1 analog; arguably more explicit/testable, but requires a rewrite.
  **Severity**: Minor (migration friction, not capability loss) | **Confidence**: High

- **Feature/API**: COSE named constants `ES512` (-36) and `ECDHwHKDF256` (-25)
  **v2 status**: Degraded (cosmetic) — `CoseAlgorithm.IsKnown`/`ToString()` doesn't recognize these; still functionally usable via `CoseAlgorithm.Other(int)`.
  **Severity**: Cosmetic | **Confidence**: Medium

**Verified improvement, no gap**: full CTAP2 GetInfo/AuthenticatorInfo parity; PIN protocols 1&2; credential management; bio enrollment; config (enterprise attestation, always-UV, min PIN length); large blob; credProtect; hmac-secret; reset; HID + SmartCard/NFC/CCID transports; attestation format coverage (packed/fido-u2f/none/apple/android-key/android-safetynet/tpm, plus forward-compat `Other(string)`).

---

## Module: OATH

v1 location: `Yubico.YubiKey/src/Yubico/YubiKey/Oath/**`
v2 location: `src/Oath/src/**`

- **Feature/API**: `IsPasswordProtected` persistent flag (device has a password configured, independent of current session unlock state)
  **v2 status**: Missing — v2's only related member, `IsLocked`, becomes `false` after a successful `ValidateAsync`, so a consumer can no longer distinguish "no password" from "password already unlocked this session."
  **User impact**: Breaks "remove password" vs "device isn't protected" UI, and key-change flows that decide whether to prompt for the current password.
  **Severity**: Major | **Confidence**: High

- **Feature/API**: Automatic `KeyCollector`-driven authentication retry (auto-prompt + transparent retry on `AuthenticationRequired`)
  **v2 status**: Missing/Behavior-changed — all operations let a locked-device error surface as a raw `ApduException`; caller must know to call `ValidateAsync` first.
  **User impact**: Consumers must reimplement "catch auth-required, prompt, validate, retry" logic themselves.
  **Severity**: Major | **Confidence**: High

- **Feature/API**: Descriptive `SecurityException` + status-word-to-message mapping on wrong/locked password
  **v2 status**: Behavior-changed/Degraded — v2 throws generic `ArgumentException`/`InvalidOperationException`/`BadResponseException` with no dedicated exception type or OATH-specific status-word mapping.
  **User impact**: Callers catching `SecurityException` for "wrong password" handling get nothing meaningful in v2.
  **Severity**: Major | **Confidence**: Medium

- **Feature/API**: Firmware feature gating for touch-required and SHA-512 credentials on Put (clear client-side exception before sending APDU)
  **v2 status**: Missing — `PutCredentialAsync` sends unconditionally with no `IsSupported`/`EnsureSupports` check (unlike rename/SCP03 paths, which do check).
  **User impact**: Older YubiKeys get an opaque device-level APDU error instead of a clear client-side exception.
  **Severity**: Minor | **Confidence**: High

- **Feature/API**: Client-side credential name length validation (`MaximumNameLength = 64`, pre-flight exception)
  **v2 status**: Missing — over-length names sent straight to the device, fail with a raw device status word.
  **Severity**: Minor | **Confidence**: High

- **Feature/API**: Convenience "add/remove/calculate by parts" overloads (build a `Credential` from issuer/account/type/period without contacting the device)
  **v2 status**: Missing/renamed-with-friction — v2's public `Credential` constructor requires the exact wire-format `Id`; the ID-computing helper is `internal`. Consumers typically must call `ListCredentialsAsync` first.
  **Severity**: Minor | **Confidence**: Medium

- **Feature/API**: `AddCredential` returning the created `Credential`
  **v2 status**: Missing — `PutCredentialAsync` returns `Task`, not `Task<Credential>`; caller must re-list to get a usable object.
  **Severity**: Minor | **Confidence**: High

- **Feature/API**: "Full" (untruncated) one-call response format for calculate operations (`ResponseFormat.Full`/`Truncated`)
  **v2 status**: Missing-but-equivalent-exists — raw HMAC still obtainable via `CalculateAsync` (P2=0x00), just without the one-call formatted convenience.
  **Severity**: Cosmetic | **Confidence**: High

**Verified improvement, no gap**: CredentialType/HashAlgorithm/Period parity plus v2 lifts the 15/30/60-second period restriction; `otpauth://` URI parsing present (renamed, expanded); PBKDF2 derivation matches (1000 rounds/SHA1/16-byte) plus v2 uses `ReadOnlyMemory<byte>` password for explicit zeroing; Reset command parity; calculate-all HOTP/touch handling equivalent (cleaner `Code?` nullable expression).

---

## Module: YubiOTP

v1 location: `Yubico.YubiKey/src/Yubico/YubiKey/Otp/**`
v2 location: `src/YubiOtp/src/**`

- **Feature/API**: Static password from human-readable string with keyboard-layout translation (`SetPassword(ReadOnlyMemory<char>)`, `WithKeyboard(KeyboardLayout)`, `GeneratePassword(...)`)
  **v2 status**: Missing from the YubiOtp public surface — v2's `StaticPasswordSlotConfiguration` only takes raw scan-code bytes. `HidCodeTranslator`/`KeyboardLayout` still exist under Core but are wired only into the `OtpTool` CLI, not the public session API.
  **Severity**: Major | **Confidence**: High

- **Feature/API**: Yubico-OTP-algorithm challenge-response (config + calculate) and TOTP-style challenge convenience (`UseYubiOtp()`, `UseTotp()`, `WithPeriod()`, `GetCode()`)
  **v2 status**: Missing — v2's `YubiOtpSession` only exposes `CalculateHmacSha1Async`; no Yubico-OTP-algorithm challenge-response path at all.
  **Severity**: Major | **Confidence**: High

- **Feature/API**: Touch-required notification callback during challenge-response (`UseTouchNotifier(Action)`)
  **v2 status**: Missing — no touch-prompt hook on `CalculateHmacSha1Async`.
  **Severity**: Minor | **Confidence**: Medium

- **Feature/API**: Reading back a programmed NDEF tag over NFC (`ReadNdefTag()`)
  **v2 status**: Missing — v2 only exposes write/configure (`SetNdefConfigurationAsync`), no read-back/select-NDEF-file API.
  **Severity**: Minor | **Confidence**: High

- **Feature/API**: NDEF URI well-known-prefix compression and text language-code/UTF-16 configuration
  **v2 status**: Degraded — `SetNdefConfigurationAsync` has no prefix-compression, language-code, or UTF-16 parameters, risking overflow of the ~54-byte NDEF payload for long URIs that fit in v1.
  **Severity**: Minor | **Confidence**: Medium

- **Feature/API**: "Use device serial number as Yubico OTP public ID" convenience (`UseSerialNumberAsPublicId`)
  **v2 status**: Missing — no serial-derived helper anywhere.
  **Severity**: Minor | **Confidence**: High

- **Feature/API**: In-builder cryptographically random key/private-ID/IMF generation helpers (`GenerateKey`, `GeneratePrivateId`)
  **v2 status**: Missing as session/config API — callers must bring their own CSPRNG.
  **Severity**: Minor | **Confidence**: Medium

- **Feature/API**: Exact-size key validation for HMAC-SHA1/Yubico OTP keys (throws on wrong length)
  **v2 status**: Behavior-changed — v2 silently SHA-1-hashes keys >20 bytes and zero-pads keys <20 bytes instead of throwing.
  **User impact**: A caller passing a wrong-length key gets silently different key material programmed onto the device instead of an immediate error — can go undetected until a production challenge-response mismatch.
  **Severity**: Major | **Confidence**: High

- **Feature/API**: FIPS-mode query (`QueryFipsModeCommand`)
  **v2 status**: Missing from YubiOtp session — not verified whether it moved to Management.
  **Severity**: Minor | **Confidence**: Medium (unverified relocation)

- **Feature/API**: Get-device-info/legacy-config commands issued via the OTP application (`GetDeviceInfoCommand`, `SetLegacyDeviceConfigCommand`)
  **v2 status**: Degraded/Missing at YubiOtp-session level — not verified against `src/Management`.
  **Severity**: Minor | **Confidence**: Low (unverified relocation)

**Verified improvement, no gap**: slot configuration flags, config-state, swap/delete/update operations all present with clearer typed enums (`ConfigFlag`/`TicketFlag`/`ExtendedFlag`), plus v2 adds SmartCard transport in addition to HID.

---

## Module: SecurityDomain / SCP

v1 locations: `Yubico.YubiKey/src/Yubico/YubiKey/Scp/**`, `.../Scp03/**`
v2 location: `src/SecurityDomain/src/**`

Overall: **strong parity**. SCP03 and SCP11(a/b/c) authentication, static key management, EC key put/generate, certificate store operations, CA identifiers, allow-lists, GetData/StoreData, factory reset, and the "SCP session composed into an app session" pattern are all present and closely match v1 behavior.

- **Feature/API**: `Scp03KeyParameters.FromStaticKeys(StaticKeys)` convenience factory
  **v2 status**: Missing — only `Default` remains; no `FromStaticKeys`.
  **Severity**: Cosmetic | **Confidence**: High

- **Feature/API**: `StaticKeys.AreKeysSame(StaticKeys?)` equality helper
  **v2 status**: Missing — v2's `StaticKeys` has no equality/comparison method.
  **Severity**: Cosmetic | **Confidence**: High

- **Feature/API**: Public introspection of SCP key parameters used to establish a connection (`IScpYubiKeyConnection.KeyParameters`)
  **v2 status**: Missing — `PivSession` stores `_scpKeyParams` as a private field with no public accessor; `PcscProtocolScp` exposes only `GetDataEncryptor()`.
  **User impact**: Can't ask an already-open SCP-secured session "which SCP key/KID/KVN authenticated this?" for diagnostics/audit logging.
  **Severity**: Minor | **Confidence**: Medium (spot-checked PivSession only)

- **Feature/API**: Synchronous API surface (all v1 SCP methods are blocking)
  **v2 status**: Behavior-changed — v2 is fully async (`*Async(..., CancellationToken)`). Mechanical migration cost, not a functional loss.
  **Severity**: Minor | **Confidence**: High

No genuine SCP03/SCP11 protocol, key-management, or certificate-chain regressions found.

---

## Module: YubiHSM Auth

v1 location: `Yubico.YubiKey/src/Yubico/YubiKey/YubiHsmAuth/**`
v2 location: `src/YubiHsm/src/**`

Scope confirmed identical on both branches: YubiHSM Auth *applet* operations only (credential put/delete/list, management-key ops, session-key derivation) — not full HSM connector/object management.

- **Feature/API**: Interactive `KeyCollector` + auto-retrying `Try*` overloads (`TryAddCredential`, `TryDeleteCredential`, `TryChangeManagementKey`, `TryGetAes128SessionKeys`, `TryGetEccP256SessionKeys`)
  **v2 status**: Missing — every operation in v2 is single-attempt with no retry loop.
  **Severity**: Major | **Confidence**: High

- **Feature/API**: Touch-required notification callback before/during physical touch wait
  **v2 status**: Missing — no in-flight "please touch now" signal; touch requirement only discoverable after the fact via `ListCredentialsAsync`.
  **User impact**: Operation appears to hang with no UI cue.
  **Severity**: Major | **Confidence**: High

- **Feature/API**: Structured retry-count reporting (`out int? retriesRemaining` / typed `RetriesRemaining` property)
  **v2 status**: Behavior-changed/Degraded — retry count embedded only in the exception's string message; callers must derive it from `SW` via `SWConstants.ExtractRetryCount`.
  **Severity**: Minor | **Confidence**: Medium

- **Feature/API**: `ListCredentials` trailing-byte semantics — v1 documents it as "retries remaining before deletion"; v2's equivalent field (`Counter`) is documented as "number of times this credential has been used" — **opposite meaning**, unconfirmed by any test in either branch.
  **v2 status**: Behavior-changed (possible mislabel) — **needs hardware verification**.
  **User impact**: If v2's doc is wrong, a "N attempts remaining" UI built on `Counter` could badly misreport imminent credential-deletion risk.
  **Severity**: Major | **Confidence**: Medium

- **Feature/API**: Explicit live application-version query (`GetApplicationVersion()`)
  **v2 status**: Missing as public method — version is cached from SELECT response, refreshed only on `ResetAsync`; functionally equivalent for normal use via `FirmwareVersion`, but no on-demand re-check.
  **Severity**: Cosmetic | **Confidence**: Medium

- **Feature/API**: Strongly-typed reusable credential value objects (`Credential`, `CredentialWithSecrets`, `Aes128CredentialWithSecrets`, `EccP256CredentialWithSecret`)
  **v2 status**: Missing — replaced by raw parameter lists on `PutCredentialSymmetricAsync`/`PutCredentialAsymmetricAsync`; no pre-constructible/reusable credential model.
  **Severity**: Cosmetic | **Confidence**: High

- **Feature/API**: Zeroable credential passwords — v1 accepted `ReadOnlyMemory<byte> credentialPassword` (`YubiHsmAuthSession.Symmetric.cs`, `Aes128CredentialWithSecrets.cs`).
  **v2 status**: **Regressed, now fixed (2026-08-31).** v2 originally shipped these as `string`, which callers cannot wipe — the only module in the SDK still doing so after the `75353fd1` Fido2/OpenPgp/Oath sweep, which missed `src/YubiHsm/` because it landed seven days earlier. Nine members now take UTF-8 `ReadOnlyMemory<byte>`, restoring v1 parity. Padding and PBKDF2 behavior are unchanged. The parameters are named plainly (`credentialPassword` and friends), with the UTF-8 and ownership contracts documented on each `<param>`.
  **Severity**: Major (while it lasted) | **Confidence**: High

**Verified improvement, no gap**: `ChangeCredentialPasswordAsync`/`ChangeCredentialPasswordAdminAsync` (fw 5.8.0+), `GenerateCredentialAsymmetricAsync` (on-device EC keygen), `PutCredentialDerivedAsync` (PBKDF2-derived symmetric credential), `SessionKeys` as `IDisposable` with zeroization — all new capabilities beyond v1.

---

## Module: Management / Device Info

v1 location: `Yubico.YubiKey/src/Yubico/YubiKey/Management/**` + `IYubiKeyDevice.cs`
v2 location: `src/Management/src/**`

- **Feature/API**: Legacy pre-firmware-5 mode switching (`SetLegacyDeviceConfiguration`, interface byte-mapping for OTP/CCID/FIDO U2F combos)
  **v2 status**: Missing from public surface — the underlying `SetModeAsync(byte[])` exists in backend classes but `IManagementBackend` is `internal`, unreachable from `ManagementSession` consumers; no interface-code translation table anywhere in v2.
  **User impact**: YubiKey NEO / YubiKey 4 (pre-5.0 firmware) users cannot reconfigure enabled USB interfaces, legacy challenge-response timeout, touch-eject flag, or auto-eject timeout at all.
  **Severity**: Major | **Confidence**: High

- **Feature/API**: `SetTemporaryTouchThreshold(int)` (manufacturing/test-only capacitive touch threshold override)
  **v2 status**: Missing — no corresponding TLV tag (0x85) or method anywhere in v2 Management.
  **Severity**: Minor | **Confidence**: High

- **Feature/API**: Granular one-purpose config setters (`SetEnabledUsbCapabilities`, `SetEnabledNfcCapabilities`, `SetChallengeResponseTimeout`, `SetAutoEjectTimeout`, `SetDeviceFlags`, `SetIsNfcRestricted`, `LockConfiguration`/`UnlockConfiguration`)
  **v2 status**: Present-but-consolidated — collapsed into a single `SetDeviceConfigAsync(DeviceConfig, ...)` built via `DeviceConfig.Builder`. Functional coverage intact; more verbose per-call-site migration, and reboot semantics must now be explicit.
  **Severity**: Minor | **Confidence**: High

- **Feature/API**: Per-field firmware-version gating on device-config writes (e.g. NFC-restricted requires ≥5.7.0)
  **v2 status**: Degraded — `SetDeviceConfigAsync` only gates on one blanket 5.0.0 feature check, no field-specific gates.
  **User impact**: Setting `NfcRestricted` on firmware 5.0.0–5.6.x could hit an on-device protocol failure instead of the clear typed `NotSupportedException` v1 raised pre-flight.
  **Severity**: Minor | **Confidence**: Medium

- **Feature/API**: `ChallengeResponseTimeout` type — `byte` in v1 vs `ReadOnlyMemory<byte>` in v2's read-side `DeviceInfo` (write side correctly uses `byte`)
  **v2 status**: Behavior-changed (API shape) — unnecessary ergonomic regression for a single-byte value with no secrecy/zeroing rationale.
  **Severity**: Cosmetic | **Confidence**: Medium

**Verified improvement, no gap**: full device-info read parity (firmware version, form factor, serial, USB/NFC capability bitmasks, FIPS capable/approved, reset-blocked, PIN complexity, part number, NFC-restricted, auto-eject/challenge-response timeouts, device flags, FPS/STM versions) across SmartCard/FIDO HID/OTP HID transports, including multi-page reads.

---

## Cross-cutting Developer Experience

- **Target framework**: v1 targeted `netstandard2.0;netstandard2.1;net472`; v2 is `net10.0`-only across all 9 packages (confirmed via `Directory.Build.props` and every module `.csproj`).
  **User impact**: Any consumer on .NET Framework, older .NET (Core 3.1/5/6/8), or netstandard2.0 library authors cannot adopt v2 until their host app is on .NET 10.
  **Severity**: Blocker (for that segment) | **Confidence**: High

- **Exception hierarchy**: v1 has 10 dedicated exception types (`ApduException`, `TlvException`, `SCardException`, `PlatformApiException`, `Ctap2DataException`, `Fido2Exception`, `KeyboardConnectionException`, `MalformedYubiKeyResponseException`, `SecureChannelException` ×2). v2 has 8 (`BadResponseException`, `PlatformInteropException`, `SCardException`, `PlatformApiException`, `ApduException`, `CtapException`, `InvalidPinException`, `WebAuthnClientError`). No v2 equivalent for `TlvException`, `KeyboardConnectionException`, or either `SecureChannelException`; `Fido2Exception`/`Ctap2DataException`'s two-level hierarchy collapses into one flat `CtapException`. SecurityDomain, YubiOtp, OpenPgp, Management, YubiHsm, and Oath have **zero** dedicated exception types.
  **User impact**: Consumers catching specific v1 exception types lose that precision and fall back to broad/generic catching.
  **Severity**: Major | **Confidence**: High

- **Synchronous API availability**: v1 is fully synchronous; v2 is 100% async with zero sync facades anywhere (`.Result`/`.Wait()`/`GetAwaiter().GetResult()` grep across all of `src/*/src` returns nothing).
  **User impact**: Simple synchronous console-app/script consumers must adopt async/await throughout, including `await using` for disposal — a non-trivial rewrite for straightforward use cases v1 didn't require.
  **Severity**: Major (understandable architecturally, but a real DX cost) | **Confidence**: High

- **`KeyCollector` delegate pattern**: removed entirely. v1 had one callback shape shared across Piv/Fido2/Oath/U2f/YubiHsmAuth (31 files reference it). v2 has zero matches for `KeyCollector`/`IAsyncKeyCollector`/`PinCollector`/`IPinProvider`/`IUserVerifier` — most applets take credentials as direct parameters.
  Partially addressed: `ICredentialPrompt` (`Yubico.YubiKit.Core.Credentials`) is a shared async, context-carrying, cancellable SDK-to-application prompt primitive intended as the one reusable callback shape. It is currently consumed only by `WebAuthnClient`; other applets remain direct-parameter and would adopt the same interface if they grow interactive needs. `ISecureCredentialReader` remains a separate synchronous, application-initiated terminal helper.
  **User impact**: A caller cannot yet wire one prompt across all applets. Prompting patterns still differ per applet outside WebAuthn.
  **Severity**: Major | **Confidence**: High

- **Logging**: not a regression — both v1 and v2 use a global static logger factory (no per-instance DI). v2 additionally adds `UseTemporary(ILoggerFactory)` for test isolation. (Separate from the "silent by default" finding under Core above, which is a real behavior change in default output, not architecture.)
  **Severity**: N/A (parity/improvement) | **Confidence**: High

- **NuGet package count**: v1 = 2 packages (`Yubico.Core`, `Yubico.YubiKey`). v2 = 9 packages (Core, Management, Piv, Fido2, WebAuthn, Oath, YubiOtp, OpenPgp, SecurityDomain, YubiHsm), confirmed no meta-package referencing all 9.
  **User impact**: Consumers wanting "all applet support" (v1's default) must now explicitly add/version-manage up to 9 references; footprint-conscious single-applet consumers benefit.
  **Severity**: Minor | **Confidence**: High

- **XML doc coverage**: comparable overall density, via a different pattern (docs on interfaces + `<inheritdoc/>` on implementations vs. v1's docs-on-concrete-class). Legitimate convention, not a real discoverability regression. One concrete gap: `BadResponseException` has zero XML docs, and `CS1591` is globally suppressed in `Directory.Build.props`, so such gaps produce no build warning and can silently persist.
  **Severity**: Minor/Cosmetic | **Confidence**: Medium (2 modules spot-checked)

---

## Verified strong/full parity summary (no action needed)

- Device discovery/hot-plug, transports (HID/CCID/NFC), platform interop (Windows/macOS/Linux)
- PIV key management, algorithms, slots, metadata, attestation (v2 adds touch callbacks + algorithm auto-detection)
- FIDO2 CTAP2 surface (GetInfo, PIN protocols, bio enrollment, credential mgmt, config, largeBlob, extensions)
- SCP03/SCP11 core protocol, key management, cert store
- Management device-info read / device-config write (firmware ≥5.0 path)
- OATH credential types/algorithms, PBKDF2, calculate-all semantics
- YubiHSM Auth's new capabilities (password change, on-device EC keygen, derived credentials, zeroizing `SessionKeys`)

## Items needing hardware/live verification before acting

1. **YubiHsm `Counter` field** — possible flip from "retries remaining" (v1) to "usage counter" (v2); needs a real hardware check against near-zero-retry credentials.
2. **YubiOtp FIPS-mode query and legacy device-info/config commands** — unverified whether these relocated to `Management` or were dropped entirely.
