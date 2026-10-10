# 23 Short SCP response crashes the host process (YESDK-1631)

| | |
| --- | --- |
| Audit severity | MED |
| Our verdict | Confirmed |
| Our severity | MED |
| Root cause | SDK only |
| Fix group | B (small API decision: exception type and recovery behaviour) |
| Evidence | unit test (child process, run today), static (code and spec), Python read and run. No hardware needed. |
| Since the audit | Bug unchanged: `ScpState.cs` is identical at `df1ec06d`. `ScpProcessor.cs` changed. It now latches "recovery required" when a protected exchange fails after the Secure Channel Protocol (SCP) state has advanced (`ScpProcessor.cs` lines 144-148 at `df1ec06d`). The `Unmac` call moved from line 121 to line 128. The fix must be based on `df1ec06d`. |

Abbreviations used in this file: API is the application programming interface. CMAC is the cipher-based message authentication code. NFC is near-field communication. R-MAC is the response message authentication code. SCP is the Secure Channel Protocol. SDK is the software development kit. SIGABRT is the Unix abort signal. SW is the status word.

## What the audit says

The audit says that a response with a short data field makes `Unmac` compute a negative `stackalloc` size. The process then aborts before the MAC check runs.

> "The negative allocation size causes a fatal stack overflow on the tested runtime. Execution never reaches the MAC comparison."

## What is right

- The arithmetic is as described. In the .NET software development kit (SDK), `Unmac` computes `msgLength = data.Length - 8 + 2` with no lower bound ([ScpState.cs:192](../../../src/Core/src/Protocols/SmartCard/Scp/ScpState.cs#L192)). It passes that value to `stackalloc` for sizes up to 512 ([ScpState.cs:198-200](../../../src/Core/src/Protocols/SmartCard/Scp/ScpState.cs#L198-L200)). A data field of 1 to 5 bytes gives a negative size.
- The caller sends every non-empty data field to `Unmac`, whatever the status word (SW) ([ScpProcessor.cs:121-123](../../../src/Core/src/Protocols/SmartCard/Scp/ScpProcessor.cs#L121-L123)).
- The crash reproduces. The child process for lengths 1 to 5 exits with code 134 (SIGABRT, the abort signal) and prints `Stack overflow.` This run used .NET 10.0.12 on macOS arm64. Repro: [YESDK1631_ShortResponseDataBelowRmac_DoesNotTerminateProcess](../../../src/Core/tests/Yubico.YubiKit.Core.UnitTests/AuditV2/CoreScpAuditReproTests.cs#L33-L45). Result today: failed as expected.
- The catch-all cannot help. The process ends before the `catch (Exception e)` at [ScpState.cs:223-226](../../../src/Core/src/Protocols/SmartCard/Scp/ScpState.cs#L223-L226) runs.
- Lengths 6 and 7 do not crash. They throw `NotSupportedException` ("Cryptography provider does not support AESCMAC"), because the same catch-all relabels an `ArgumentOutOfRangeException` from `data[..^8]`. Repro: [YESDK1631_SixOrSevenByteResponse_ThrowsBadResponse](../../../src/Core/tests/Yubico.YubiKit.Core.UnitTests/AuditV2/CoreScpAuditReproTests.cs#L63-L68). Result today: failed as expected. The actual type was `NotSupportedException`.
- The Python reference does not crash on the same input. See the Python section below.

## What is wrong or imprecise

- The audit's wording "crashes" is right only for the 1-to-5 byte lengths. The audit does not mention lengths 6 and 7. Those inputs do not crash. They raise a misleading exception type, which is a second defect in the same code path.
- The repro's failure message names exit code 134 (SIGABRT). That code is specific to Unix, so the message does not describe other platforms correctly.
- The parent repro only checks the child's exit code. If the child runs no test (for example, after a filter mismatch), the parent can still pass. The repro should also assert that the child reports exactly one passed test. This gap is in our repro, not in the SDK.

## Why it matters

- **Who can trigger it:** a party that can change bytes between the host and the card. This is the attacker that SCP is designed to resist, for example a malicious reader or a near-field communication (NFC) relay.
- **Precondition:** an SCP03 or SCP11 session is open, and the attacker controls the response to one protected command. The response must carry a data field of 1 to 7 bytes.
- **Effect:** for 1 to 5 bytes, the host process aborts (SIGABRT). No managed exception is raised, and no `finally` blocks or cleanup run. For 6 and 7 bytes, the caller gets a `NotSupportedException` with a misleading message. Code that catches `BadResponseException` misses it.

## Specification

> "The R-MAC is made of the first 8 bytes (in S8 mode) or full 16 bytes (in S16 mode) of the CMAC computed on the message made of the MAC chaining value, the response data field (if present) and the status bytes."

Source: GlobalPlatform Card Specification v2.3 Amendment D, Secure Channel Protocol '03', public review v1.1.2.6, section 6.2.5 ([PDF](https://globalplatform.org/wp-content/uploads/2019/12/GPC_2.3_D_SCP03_v1.1.2.6.pdf)). CMAC is the cipher-based message authentication code. R-MAC is the response MAC, the authentication code on a card response.

> "No R-MAC shall be generated and no protection shall be applied to a response that includes an error status word: in this case only the status word shall be returned in the response."

Source: same section, same document. Quotes checked against the PDF text.

> "As R-MAC uses a different session key than C-MAC, the same MAC chaining value can be used for the response and the next command."

Source: same section, same document. This sentence is what allows a malformed response to be refused without implying a broken command chain.

| Clause | SCP03 Amd D v1.0 (public release) | SCP03 Amd D v1.1.2.6 (public review) | Notes |
| --- | --- | --- | --- |
| 6.2.5, which responses have no R-MAC | "...a response when status bytes SW1 and SW2 indicate a system error" | "...a response that includes an error status word" | Later text treats every status word except `9000` and the warning words as an error. |
| 6.2.5, R-MAC length | "The R-MAC is made of the first 8 bytes of the CMAC..." | "...first 8 bytes (in S8 mode) or full 16 bytes (in S16 mode)..." | v1.0 has one R-MAC length, 8 bytes. v1.1.2.6 adds S16 mode (16 bytes). |

The SDK's own contract for a malformed card response is `BadResponseException`. Its class body says: "The data contained in a YubiKey response was invalid" ([Exceptions.cs:19](../../../src/Core/src/Exceptions.cs#L19)).

## Canonical Python reference

**Python is correct here.** Python does not check the length explicitly. A short data field makes the tag comparison fail, and the code raises `BadResponseError("Wrong MAC")`. Our check: we called the Python `unmac` with 0 to 9 byte inputs, and every call raised `BadResponseError: Wrong MAC`. We also checked that `constant_time.bytes_eq` returns `False` for inputs of different lengths (`cryptography` 50.0.2).

[yubikit/core/smartcard/scp.py, lines 223-230](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/core/smartcard/scp.py#L223-L230) at `4ca60f7`:

```python
def unmac(self, data: bytes, sw: int) -> bytes:
    msg, mac = data[:-8], data[-8:]
    rmac = _calculate_mac(...)[1]
    if not constant_time.bytes_eq(mac, rmac):
        raise BadResponseError("Wrong MAC")
    return msg
```

Python has no recovery latch. `ScpProcessor.send_apdu` in [yubikit/core/smartcard/__init__.py, lines 292-320](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/core/smartcard/__init__.py#L292-L320) has no exception handling. The SCP state is reused after an error. The recovery latch is an SDK design choice.

## Sibling SDKs (context only)

- Android computes the same `data.length - 8 + 2` ([ScpState.java#L172](https://github.com/Yubico/yubikit-android/blob/f46268563437ac52910001222a77741d229b9b99/core/src/main/java/com/yubico/yubikit/core/smartcard/scp/ScpState.java#L172)). Not run.
- Swift uses `data.prefix(data.count - 8)` without a check ([SCPState.swift#L76-L77](https://github.com/Yubico/yubikit-swift/blob/8cd5583a489a2f7ab7a1c7669518e68b26b6e475/YubiKit/YubiKit/SCP/SCPState.swift#L76-L77)). Not run.
- The legacy .NET v1 SDK rejects responses shorter than 8 bytes before any arithmetic ([ChannelMac.cs#L96-L98](https://github.com/Yubico/Yubico.NET.SDK/blob/941874e91a77616f5d7063f2952a0291c7e7c8f8/Yubico.YubiKey/src/Yubico/YubiKey/Scp/Helpers/ChannelMac.cs#L96-L98)).

## Reproduction

- Unit: [CoreScpAuditReproTests.YESDK1631_ShortResponseDataBelowRmac_DoesNotTerminateProcess](../../../src/Core/tests/Yubico.YubiKit.Core.UnitTests/AuditV2/CoreScpAuditReproTests.cs#L33-L45). Runs the 1-to-5 byte body in a child process and asserts exit code 0. Result today: failed. The child exited with 134 and printed `Stack overflow.`
- Unit: [CoreScpAuditReproTests.YESDK1631_SixOrSevenByteResponse_ThrowsBadResponse](../../../src/Core/tests/Yubico.YubiKit.Core.UnitTests/AuditV2/CoreScpAuditReproTests.cs#L63-L68), cases 6 and 7. Asserts `BadResponseException`. Result today: failed. The actual type was `NotSupportedException`.
- Unit: [CoreScpAuditReproTests.YESDK1631_ChildBody_OneToFiveByteResponse_ThrowsBadResponse](../../../src/Core/tests/Yubico.YubiKit.Core.UnitTests/AuditV2/CoreScpAuditReproTests.cs#L47-L57). Skipped in normal runs. It only runs in the child process.
- Hardware: not applicable.

## Proposed fix

- **Recommendation.**
  1. In `ScpState.Unmac` ([ScpState.cs:190](../../../src/Core/src/Protocols/SmartCard/Scp/ScpState.cs#L190)), reject a data field shorter than the 8-byte R-MAC before any arithmetic:

     ```csharp
     if (data.Length < RmacLength)
         throw new BadResponseException("Response too short to contain an R-MAC");
     ```

     `RmacLength` is a new constant with value 8. `ScpProcessor` already has a private `MacLength` constant.
  2. Narrow the catch-all at [ScpState.cs:223-226](../../../src/Core/src/Protocols/SmartCard/Scp/ScpState.cs#L223-L226) so that it wraps only CMAC provider failures. Other exceptions keep their type.
  3. Keep the check inside `Unmac`. That way the existing catch in `ScpProcessor` reports the failure to the recovery owner.
- **Options considered.**
  - **A (recommended): `BadResponseException` thrown in `Unmac`.** It is the type that `Unmac` already uses for "Wrong MAC" ([ScpState.cs:217](../../../src/Core/src/Protocols/SmartCard/Scp/ScpState.cs#L217)), and the padding check uses it too ([ScpState.cs:139](../../../src/Core/src/Protocols/SmartCard/Scp/ScpState.cs#L139)). No new type is needed.
  - **B: a new secure-channel exception type.** The existing `SecureChannelException` lives in the SecurityDomain namespace ([SecureChannelException.cs:17](../../../src/SecurityDomain/src/SecureChannelException.cs#L17)). Core cannot use it without a layering change. A new Core type adds public surface.
  - **C: skip `Unmac` for error status words**, as section 6.2.5 of the GlobalPlatform SCP03 specification describes. This changes behaviour for every response that has data and an error status word. It is not needed to fix the crash. See the open question below.
- **Recovery-required decision.** Latch, which is the automatic result of option A. `State.Mac` runs before the command is sent ([ScpProcessor.cs:108](../../../src/Core/src/Protocols/SmartCard/Scp/ScpProcessor.cs#L108)), so the card has already processed the command. `Mac` advances the command chain ([ScpState.cs:173-176](../../../src/Core/src/Protocols/SmartCard/Scp/ScpState.cs#L173-L176)). `Unmac` reads that chain but does not advance it ([ScpState.cs:205-215](../../../src/Core/src/Protocols/SmartCard/Scp/ScpState.cs#L205-L215)). GlobalPlatform allows the same chaining value to serve the response and the next command (section 6.2.5). Refusing reuse is therefore conservative. The host cannot authenticate the response or establish what happened during the exchange. A malformed response does not by itself prove that the command MAC chains are out of step. The design text at `df1ec06d` requires the same for response-MAC failures: "After protected state advances, first transport, response-MAC or intermediate-fragment failures also latch refusal on the same protocol, retaining the original exception without replay." ([02-architecture.md, lines 76-78](https://github.com/Yubico/Yubico.NET.SDK/blob/df1ec06d1a03c18c0b697ad424a40e4b0f9b46d1/docs/plans/yubikit-async-boundaries/02-architecture.md#L76-L78))
- **API impact.** None. The change is internal. A malformed response now raises `BadResponseException` instead of ending the process or raising `NotSupportedException`. After such a failure, the protocol refuses further exchanges. That refusal already exists at `df1ec06d`.
- **Proving test.** Follow the pattern of `InvalidProtectedResponseMac_RefusesNextExchangeWithoutReplay` ([PcscProtocolScpTests.cs#L448-L472](https://github.com/Yubico/Yubico.NET.SDK/blob/df1ec06d1a03c18c0b697ad424a40e4b0f9b46d1/src/Core/tests/Yubico.YubiKit.Core.UnitTests/Protocols/SmartCard/Scp/PcscProtocolScpTests.cs#L448-L472)) at `df1ec06d`. Send a 1-to-7-byte protected response. Expect `BadResponseException`. The next exchange must refuse with the same inner exception, and only one command may be transmitted. Keep the child-process repro, and add the "exactly one passed test" assertion from the review.
- **Depends on / interacts with.** The latch exists only at `df1ec06d`, so the fix must start from that base. N2 changes the same SCP files (initialization slicing). Use the same exception type there.
- **Open questions for the maintainer.**
  1. A card should not send data with an error status word (GlobalPlatform SCP03, section 6.2.5). Keep checking that data with the R-MAC (recommended, and what this fix does), or skip the check for error status words?

## Check it yourself

- `dotnet toolchain.cs -- test --project Core.UnitTests --filter "FullyQualifiedName~YESDK1631"`, from the worktree root.
- Read `src/Core/src/Protocols/SmartCard/Scp/ScpState.cs` lines 190-236.
- Read Python `yubikit/core/smartcard/scp.py` lines 223-230 at `4ca60f7`.
- Read GlobalPlatform SCP03 Amd D, public review v1.1.2.6, section 6.2.5 (PDF linked above).
