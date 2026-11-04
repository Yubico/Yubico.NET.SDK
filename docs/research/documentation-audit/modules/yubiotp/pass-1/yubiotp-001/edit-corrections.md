# Bounded required-review corrections

2026-11-04 · GPT-6.1 Sol editor · OTP-REV-001–003 from review.md.

Only the three required consumer corrections below were applied. No optional edits, runtime/help-string/
assertion/API-snapshot/configuration/import changes, builds, formatting, tests, hardware operations,
delegation or commits. The original edit-report.md and coordinator/evidence records were not modified.
Independent focused re-review and coordinator compilation/Roslyn gates remain pending.

## Before/after SHA-256

Before hashes are the original editor/reviewer handoff bytes. A read-only reverse-substitution check
reconstructed those exact hashes from the final files, proving these corrections contain no additional
unrecorded edits relative to that handoff.

| Path | Before SHA-256 | After SHA-256 | After bytes |
|---|---|---|---:|
| `src/YubiOtp/src/ExtendedFlag.cs` | `9c70b120a8c2ec18a5986db3be95a9a54553282e0dcf2d2289dc096a272907a9` | `6ac9449167e736fe03b5475eafdf1364cd49ce1319b7a4607271578e5ca9f7c1` | 1943 |
| `src/YubiOtp/examples/OtpTool/Commands/YubiOtpCommand.cs` | `2e91394f0ce0f6e53e394e4a32f2a1e30a93f00d1a917c824fb8eb2958115db5` | `e129bcfbfc3ff12f64eb5f0d4c5fe688b8bd63078f4f26d50d9dba542ff7c45d` | 6080 |
| `src/YubiOtp/README.md` | `20f56a448dae2cbba9ef8deef5c732861c83a1711739d63238331c97744fb0ec` | `5ba4557b704e023e231e09208f584cdf8bbf1dc8753f8d437d8558ef2ff32eba` | 22516 |

## Exact changed blocks

### OTP-REV-001 — correct serial-exclusion attribution

`src/YubiOtp/src/ExtendedFlag.cs`, before:

```text
/// On legacy devices serial visibility merges across both slots. Manual 3.3 documents button visibility
/// for 2.x and USB serial unavailability on 3.0/3.1; these are legacy applicability notes, not universal
```

After:

```text
/// On legacy devices serial visibility merges across both slots. The personalization v1.20.0 manual documents
/// button visibility for 2.x and USB serial unavailability on 3.0/3.1; these are legacy applicability notes, not universal
```

The legacy/non-universal qualification and following flag-enforcement/update-mask explanation are unchanged.
Per coordinator adjudication, evidence/sources.md lines 23–24 describe correctly attributed device-global
merging, not these serial exclusions. No source-record factual correction was made or is requested here.

### OTP-REV-002 — correct this command's cleanup description

`src/YubiOtp/examples/OtpTool/Commands/YubiOtpCommand.cs`, before:

```text
/// Parsed secret strings are immutable; arrays allocated before later parsing failures are not all
/// protected by the command's finally cleanup. Use the SDK guide's ownership pattern for application code.
```

After:

```text
/// Parsed secret strings are immutable and parser diagnostics can echo input. The command's finally
/// clears its acquired AES-key, private-ID and access-code arrays, not those strings or captured output.
```

Verified the complete command: private-ID/AES acquisition and later parsing occur inside try, and finally
clears the acquired AES-key/private-ID/access-code arrays. Retained the local generated-output and current/
new-access-code cautions. The immutable/parser/output limits remain, without assigning other commands'
pre-try allocation defects to this one.

### OTP-REV-003 — enroll the generated HMAC key before erasure

`src/YubiOtp/README.md`, immediately following the existing HMAC key-length/mode paragraph, added:

```text
This application-dependent provisioning fragment generates a new key. Securely enroll that same key with
your verifier before it is cleared; a random key known only to this process is not a deployed verifier.
```

Inside the existing try, before session creation, added exactly this comment:

```text
    // Securely enroll hmacKey with your verifier here as part of your application's provisioning.
```

The full final application-dependent fragment is now README lines 146–162, including fences. Preserve
this original body for the first compilation, not a pre-correction extraction:

```csharp
byte[] hmacKey = RandomNumberGenerator.GetBytes(20);
try
{
    // Securely enroll hmacKey with your verifier here as part of your application's provisioning.
    await using var session = await device.CreateYubiOtpSessionAsync(
        new SessionCreationOptions { PreferredConnectionType = ConnectionType.HidOtp });
    using var config = new HmacSha1SlotConfiguration(hmacKey);
    config.RequireTouch();
    config.UseShortChallenge();
    await session.PutConfigurationAsync(Slot.Two, config);
}
finally
{
    CryptographicOperations.ZeroMemory(hmacKey);
}
```

Prerequisites remain README's imports, typed IYubiKey device with no open connection, authorized slot-2
replacement and OTP HID support. The application supplies secure verifier enrollment of this same exact
20-byte key. Enrollment is within try so failure still clears the key. Configuration/session scopes,
explicit HID selection, touch requirement and finally are unchanged. This adds no verifier runtime API,
key printing or executable statement. All subsequent README line ranges shift by four; the original
edit-report inventory is historical and intentionally unchanged. Coordinator owns snippet-preparer coordination.

## Verification and hardware-dependent assessment

Read-only Python checks passed: all three reconstructed before hashes match; changes in both C# files
are exclusively XML comments; all their XML comment blocks parse. No compilation or execution claimed.

Read hardware/results.md. Its recorded status matrix (keyboard true, both HMAC variants false), scoped
USB CCID 6985 rejection, HID known answers/AES tickets, cancellation/timeout and fresh-session recovery
align with current qualified prose. Metadata 5.8 beta versus raw/effective 0.0.1 does not require relabeling
the raw version as production firmware. No necessary dependent consumer correction was identified within
this bounded pass. No run-specific hardware narrative was inserted into public documentation.

The results do not establish universal firmware/transport behavior, NFC on-tap output, same-session recovery,
all-path native ownership, nonzero IMF correctness, full scan-map support, or code retention/change/removal.
Those qualifications and deferred implementation findings remain. Independent reviewer follow-up should
assess the hardware record separately; this editor did not execute those operations or change their evidence.
