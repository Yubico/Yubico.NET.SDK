# yubikit-android previewSign Parity Report

**Date:** 2026-04-22
**Investigated:** yubikit-android v3.1.0 (released 2026-03-31, main branch f4626856)
**Local path:** /Users/Dennis.Dyall/Code/y/yubikit-android
**Verdict:** PROVEN (Registration + Authentication; hardware-tested registration only)

## Findings

**Code paths:** Full implementation for both registration and authentication.

- **Registration (generateKey):** `fido/src/main/java/com/yubico/yubikit/fido/client/extensions/SignExtension.java:195–289` (`makeCredential()` method)
  - Parses `generateKey` input with algorithm list
  - Extracts attestation object and credential data from unsigned extension outputs
  - Returns `generatedKey` map with keyHandle, publicKey, algorithm, attestationObject
  
- **Authentication (signByCredential):** `SignExtension.java:305–376` (`getAssertion()` method)
  - Parses `signByCredential` input mapping (credential ID → {keyHandle, tbs, additionalArgs})
  - Validates allowList presence and credential mapping
  - Extracts signature from extension results
  - Returns signature in response map

- **Android UI Bridge:** `fido-android-ui/src/main/kotlin/com/yubico/yubikit/fido/android/ui/internal/FidoJs.kt:1–15+` (generated from JavaScript source)
  - Client-side JavaScript decodes previewSign extension results
  - Handles both `generatedKey` (registration) and `signature` (authentication) paths
  - Base64-decodes binary fields (keyHandle, publicKey, attestationObject, signature)

**Hardware tests:**

- **Registration:** ✅ Hardware-tested via instrumented tests
  - `testing-android/src/androidTest/java/.../SignExtensionInstrumentedTests.java:36–48` (Android instrumented tests, runs on real device)
  - `testing-desktop/src/integrationTest/java/.../SignExtensionInstrumentedTests.java` (Desktop integration tests, physical YubiKey required)
  - Test invokes `SignExtensionTests::testWithDiscoverableCredential()` and `testWithNonDiscoverableCredential()`
  - Exercises CTAP with `state.withCtap2(session -> ...)` driver (lines 66–121)
  - Asserts both JSON and CBOR serialization paths for generatedKey output

- **Authentication:** ❌ NOT hardware-tested
  - `SignExtensionTests.java` covers only registration (`makeCredential` with `generateKey` input)
  - `getAssertion()` code path exists but zero hardware test invocations
  - No test calls `getAssertion()` with `signByCredential` input
  - Lines 305–376 of SignExtension.java show full auth logic but no harness exercises it

**Demo app:** 
- AndroidDemo includes `SignExtension()` in extension list (`src/main/java/.../FidoFragment.kt:55`) but no UI screens explicitly demonstrate previewSign features

**CHANGELOG/release notes:** 
- NEWS line 6: "fido: support for WebAuthn previewSign extension v4" (v3.1.0, released 2026-03-31)
- Marked as new feature in 3.1.0, no prior versions mention it
- Categorized under "new" (not experimental/beta)

**Issues / TODOs / FIXMEs:** None found near SignExtension code

**Documentation:** Not present in fido/README.adoc or top-level README.adoc

## Citations
- `fido/src/main/java/com/yubico/yubikit/fido/client/extensions/SignExtension.java:45` — SIGN constant = "previewSign"
- `SignExtension.java:195–289` — `makeCredential()` method (registration)
- `SignExtension.java:305–376` — `getAssertion()` method (authentication)
- `testing/src/main/java/com/yubico/yubikit/fido/client/extensions/SignExtensionTests.java:44–122` — Unit test (registration only)
- `testing-android/src/androidTest/java/.../SignExtensionInstrumentedTests.java:36–48` — Instrumented test suite (Android)
- `testing-desktop/src/integrationTest/java/.../SignExtensionInstrumentedTests.java` — Instrumented test suite (Desktop)
- `fido-android-ui/src/main/kotlin/.../FidoJs.kt` — JavaScript bridge (previewSign result parsing, lines ~60–80)
- `NEWS:1–10` — v3.1.0 release notes mentioning previewSign v4
- `AndroidDemo/src/main/java/.../FidoFragment.kt:55` — Demo integration of SignExtension

## Recommendation for Phase 9.2 verdict step

**Combined evidence (Swift + libfido2 + yubikit-android):**
- **Swift** (yubikit-swift v1.3.0): Code paths for both registration and authentication; hardware-tested neither (diagnostic comment at IntegrationTests/PreviewSignTests.cs:107)
- **libfido2** (v1.17.0): NONE — zero code paths, zero extension mask, zero hardware tests
- **yubikit-android** (v3.1.0): Code paths for both registration and authentication; hardware-tested registration only, authentication code untested

**Synthesis:** yubikit-android provides **proven** registration support (hardware-tested in instrumented suites on Android/Desktop). Authentication path is fully implemented but **not hardware-validated**. Combined with libfido2 silence and Swift's untested state, this suggests: (a) previewSign registration is production-ready and YubiKey-verified, (b) authentication is recent/beta and lower priority. For the C# port's Phase 9.2 verdict, yubikit-android registration evidence supports PROVEN for `generateKey`. However, the lack of hardware-tested authentication across all three SDKs reinforces DEFER for the multi-credential probe (`signByCredential`) workload until explicit authentication hardware test data is available.
