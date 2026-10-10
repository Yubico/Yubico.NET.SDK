# 17 Private-key intermediate copies (YESDK-1625)

| | |
| --- | --- |
| Audit severity | MED |
| Our verdict | Partial. Most SDK copies were fixed at `65e36386`. The one that remains is a copy the .NET runtime makes on macOS. |
| Our severity | LOW. Local memory disclosure is required. The path is reachable only through the public `ECPrivateKey.CreateFromValue()` factory. |
| Root cause | .NET runtime |
| Fix group | A (no-brainer): file upstream and document. Any SDK change to the derivation belongs to #27 (group C). |
| Evidence | Static (runtime source at v10.0.10). Existing unit tests (12, passing). No hardware. |
| Since the audit | Unchanged at current yubikit (`df1ec06d`). The four SDK files involved are not in the diff from `fdfcd6fd`. The runtime file at v10.0.10 is identical to v10.0.0. The runtime is outside this repository. |

## What the audit says

The audit says the SDK leaves private-key copies in memory after use. The one case it says remains is the runtime path behind `CreateFromValue()`:

> "`ECPrivateKey.CreateFromValue()` leaves one complete 32-byte scalar in memory on macOS/.NET 10.0.10, even after clearing the caller's input and final wrapper."

Abbreviations: EC is elliptic curve. RSA is Rivest–Shamir–Adleman. PKCS#8 is the Public-Key Cryptography Standards #8 private-key format. D is the EC private scalar. The scalar is the private number itself. Q is the EC public point.

## What is right

- Most SDK copies are already cleared. The audit says so, and the code agrees:
  - The RSA normalizer clears arrays it replaces ([RSAParametersExtensions.cs#L92-L101](../../../src/Core/src/Cryptography/RSAParametersExtensions.cs#L92-L101)). The RSA factory clears its decoder arrays ([RSAPrivateKey.cs#L125-L130](../../../src/Core/src/Cryptography/RSAPrivateKey.cs#L125-L130)).
  - The EC PKCS#8 factory clears the decoded D after the wrapper copies it ([ECPrivateKey.cs#L120-L131](../../../src/Core/src/Cryptography/ECPrivateKey.cs#L120-L131)).
  - `CreateFromValue()` clears both SDK-made copies of D and disposes the temporary `ECDsa` ([ECPrivateKey.cs#L226-L246](../../../src/Core/src/Cryptography/ECPrivateKey.cs#L226-L246)). The unit test `ECPrivateKey_CreateFromValue_ZeroesAllTemporaryPrivateValues` checks this ([PrivateKeyDecodingZeroingTests.cs#L271-L294](../../../src/Core/tests/Yubico.YubiKit.Core.UnitTests/Cryptography/PrivateKeyDecodingZeroingTests.cs#L271-L294)).
- The residual is in the runtime. At v10.0.10, the D-only import path works like this:
  1. The SDK builds `ECParameters` with D and no public point, then calls `ECDsa.Create(parameters)` ([ECPrivateKey.cs#L226-L232](../../../src/Core/src/Cryptography/ECPrivateKey.cs#L226-L232)).
  2. The runtime import sees no Q and calls `ExtractPublicKeyFromPrivateKey(ref parameters)` ([EccSecurityTransforms.cs#L205-L208](https://github.com/dotnet/runtime/blob/v10.0.10/src/libraries/Common/src/System/Security/Cryptography/EccSecurityTransforms.cs#L205-L208)).
  3. The macOS helper exports the key and reads it back into `ecParameters`. That allocates a new D array ([EccSecurityTransforms.macOS.cs#L57-L58](https://github.com/dotnet/runtime/blob/v10.0.10/src/libraries/Common/src/System/Security/Cryptography/EccSecurityTransforms.macOS.cs#L57-L58), [EccKeyFormatHelper.cs#L177](https://github.com/dotnet/runtime/blob/v10.0.10/src/libraries/System.Security.Cryptography/src/System/Security/Cryptography/EccKeyFormatHelper.cs#L177)).
  4. The import uses the new array. The runtime then drops its reference without clearing it ([EccSecurityTransforms.cs#L210-L232](https://github.com/dotnet/runtime/blob/v10.0.10/src/libraries/Common/src/System/Security/Cryptography/EccSecurityTransforms.cs#L210-L232)).
- The PIV and Security Domain imports do not reach this path. A search of `src/Piv/src` and `src/SecurityDomain/src` finds only public-key `CreateFromValue()` calls ([PivKeyProtocol.cs#L497](../../../src/Piv/src/Keys/PivKeyProtocol.cs#L497), [SecurityDomainSession.cs#L616](../../../src/SecurityDomain/src/SecurityDomainSession.cs#L616)). PIV import encodes the caller's D directly ([PivKeyProtocol.cs#L279-L293](../../../src/Piv/src/Keys/PivKeyProtocol.cs#L279-L293)).

## What is wrong or imprecise

- The audit's workaround does not help. It says to use `ECPrivateKey.CreateFromParameters()` for PIV and Security Domain import. Those paths never call `CreateFromValue()`, so there is nothing to replace. For a caller that has only D, `CreateFromParameters()` needs a public point the caller does not have. A wrong point is the problem described in #27.
- The verification report says the runtime "overwrites the ref ECParameters holding the original private D without clearing it". That is true, but in the SDK path the overwritten array is the SDK's own input. The SDK clears it ([ECPrivateKey.cs#L246](../../../src/Core/src/Cryptography/ECPrivateKey.cs#L246)). The copy that stays in memory is the one the runtime allocates ([EccKeyFormatHelper.cs#L177](https://github.com/dotnet/runtime/blob/v10.0.10/src/libraries/System.Security.Cryptography/src/System/Security/Cryptography/EccKeyFormatHelper.cs#L177)).
- A deterministic unit test cannot observe the runtime's copy. The existing zeroing tests cover only arrays the SDK owns.
- In the source we read, the residual is specific to macOS (`EccSecurityTransforms.macOS.cs`). We did not check other platforms' import code for the same pattern, so do not generalize.

## Why it matters

A process that can read this process's memory, or a crash dump, could recover an EC private scalar imported through this factory, including supported P-256, P-384, or P-521 inputs ([ECPrivateKey.cs#L213-L232](../../../src/Core/src/Cryptography/ECPrivateKey.cs#L213-L232)). That scalar is the full private key for the pair. The import must have happened on macOS, through the public `CreateFromValue()` factory, with a D-only input. Nothing here is reachable remotely. PIV and Security Domain imports are not affected.

## Specification

No protocol specification governs host memory handling. The governing rules are SDK rules:

> "A new disposable private-key object that owns its decoded key material."

XML doc on `AsnPrivateKeyDecoder.CreatePrivateKey` ([AsnPrivateKeyDecoder.cs#L37-L39](../../../src/Core/src/Cryptography/AsnPrivateKeyDecoder.cs#L37-L39)).

> "ALWAYS zero sensitive data: `CryptographicOperations.ZeroMemory()`"

Root [CLAUDE.md#L58-L59](../../../CLAUDE.md#L58-L59), Security section.

## Canonical Python reference

Python has no equivalent. Python integers and bytes are immutable, and the cryptography objects expose no clear operation, so the question does not arise in the same way. yubikit reads the EC private value into a Python integer and encodes it ([yubikit/piv.py#L1364-L1366](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/piv.py#L1364-L1366)).

## Sibling SDKs (context only)

Not checked for this residual.

## Reproduction

- Unit: [PrivateKeyDecodingZeroingTests](../../../src/Core/tests/Yubico.YubiKit.Core.UnitTests/Cryptography/PrivateKeyDecodingZeroingTests.cs) (12 facts). They assert that the SDK's own temporary arrays are zero after `CreateFromValue()`. **Recorded result: all 12 pass.** No test can observe the runtime's copy.
- Hardware: not applicable.

## Proposed fix

- Recommendation (group A):
  1. Do not change the PIV or Security Domain import code. It is already clean.
  2. File a focused upstream issue at dotnet/runtime. Cite `ExtractPublicKeyFromPrivateKey` in `EccSecurityTransforms.macOS.cs` and `FromECPrivateKey` in `EccKeyFormatHelper.cs`. Include a macOS repro that calls `ECDsa.Create` with a D-only `ECParameters` and inspects the heap. We searched dotnet/runtime issues for `EccSecurityTransforms` and `ExtractPublicKeyFromPrivateKey` and found no issue for this residual. The nearest hits were unrelated.
  3. Add an XML remark to `ECPrivateKey.CreateFromValue()`. It should say that the runtime may keep an uncleared copy of the scalar on macOS. Do not present `CreateFromParameters()` as a workaround for D-only input.
- Options considered:
  - (a) Upstream issue plus documentation (recommended). No code risk.
  - (b) Replace the D-only import with a managed or vetted derivation. This is the #27 decision. Calling `ECDsa.Create` with D only does not help.
  - (c) Do nothing. Not recommended while the public factory exists.
- API impact: none.
- Proving test: none that is deterministic. For the upstream issue, a macOS heap-inspection repro.
- Depends on / interacts with: #27 (do not trust a caller-supplied Q). Option (b) belongs to #27.
- Open questions: Who files the upstream issue? Does the maintainer want to wait for a runtime fix before 2.0?

## Check it yourself

- Runtime file at the version the audit used: [EccSecurityTransforms.macOS.cs at v10.0.10](https://github.com/dotnet/runtime/blob/v10.0.10/src/libraries/Common/src/System/Security/Cryptography/EccSecurityTransforms.macOS.cs#L52-L61). It is identical at [v10.0.0](https://github.com/dotnet/runtime/blob/v10.0.0/src/libraries/Common/src/System/Security/Cryptography/EccSecurityTransforms.macOS.cs).
- Import path: [EccSecurityTransforms.cs#L184-L232](https://github.com/dotnet/runtime/blob/v10.0.10/src/libraries/Common/src/System/Security/Cryptography/EccSecurityTransforms.cs#L184-L232) and [EccKeyFormatHelper.cs#L115-L181](https://github.com/dotnet/runtime/blob/v10.0.10/src/libraries/System.Security.Cryptography/src/System/Security/Cryptography/EccKeyFormatHelper.cs#L115-L181).
- SDK tests: `dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~PrivateKeyDecodingZeroingTests&Category!=RequiresHardware&Category!=RequiresUserPresence"`
