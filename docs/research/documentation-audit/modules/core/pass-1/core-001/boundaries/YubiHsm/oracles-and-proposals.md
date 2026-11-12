# Actual test oracles and unexecuted proposals

Every assigned test file, local fixture and recording/prompt helper was read in full. No tests,
cryptographic primitives, native libraries or devices were executed. Existing historical results retain
their original source/run context, separate from this source inspection.

## Existing oracles

| Source / actual assertion | Established by the source-defined oracle | Limit |
| --- | --- | --- |
| `HsmAuthSessionByteLevelTests` PUT/symmetric CALCULATE/DELETE | Exact selected header and ordered TLV bytes, padded password and returned S-ENC fixture | Recorder copies command bytes and returns queued data without implementing applet crypto; no SCP or 56-byte response variant. |
| Context length tests | 15/17 and 129/131 reject with `context` ParamName and unchanged command count | This is meaningful before-I/O behavior, not on-curve/key/cryptogram validation. |
| GET_CHALLENGE 5.6.0/5.7.1 | Label-only on 5.6.0 even when password supplied; password TLV on 5.7.1 | Forced policy under a fake, not old-firmware execution; no response point or retry behavior. |
| Password-change byte tests | INS 0B, selected P1 and specific tag positions | User test does not assert all password bytes; admin test checks only initial label/management tags, not every header/value. |
| Reset override test | Version 5.8 remains after re-SELECT 5.4.3; password feature query true | No SCP initialization/protected reset/next-operation assertion, native transaction or persistent reset-state proof. |
| Direct factory probe failure | Unqueued SELECT failure leaves recorder disposal count 0 | Source proves caller connection borrowing. It does not assert actual native release or subsequent factory reuse. |
| `HsmAuthRetryExceptionTests` | Type inheritance plus nine operation-specific 63Cx count/status checks; asymmetric credential message matches the helper | Most inputs are non-cryptographic sequences. Synthetic 63C9 tests nibble extraction beyond documented eight initial retries. GET_CHALLENGE deliberately absent. |
| `RetryExtractionTests` |0/1/5/8/15 and unrelated SW classification | Encoded-status helper only, not device retries, blocking/deletion or successful restoration. |
| `CredentialPasswordTests` | Padded 16-byte copies; original mutation leaves copy intact; UTF-8 byte widths and public ParamName paths | Tests construct UTF-8; implementation accepts raw bytes without UTF-8 validation. Helper creates a borrowed recorder without an external connection-disposal scope. |
| `LabelValidationTests` | UTF-8 byte width versus character count; maximum 64-byte checks | Same encoder-derived comparison, no malformed UTF-16/label wire roundtrip/trust assertion. |
| `HsmAuthCredentialTests` | Ordinal-ignore-case order and selected record equality/retry difference | Equality source includes all properties; selected assertions do not independently vary each field. |
| `Pbkdf2DerivationTests` | Same BCL primitive with independently stated constants; nonzero/distinct split; repeatability | Method name KnownVector is stronger than its same-primitive oracle. Existing comments already state this accurately. No standalone external KAT is asserted. |
| `SessionKeysTests.Parse` | Full byte content of three artificial 16-byte regions; rejects selected lengths other than 48 | 56 is absent from explicit InlineData. Source strict 48-byte check covers it; the test does not validate applet response layout. |
| `SessionKeysTests.Dispose` | Reflection captures all three actual owned arrays, verifies nonzero, disposal zeroing and property refusal | Good owned-array witness, without transport/decrypt/callback allocation or connector-copy coverage. |
| `SessionKeysTests.ZeroApduResponse` | Actual final RawData allzero after helper | It neither checks constructor-source bytes unchanged nor traverses lower copies. Current Core response tests own that source/copy boundary. |
| `TouchNotificationTests` policy cases | Required/optional/unknown/missing policy, no-prompt LIST omission, count before CALCULATE, identical context, single request/resolution and token None | Fake queued response accepts arbitrary asymmetric points. Policy/list metadata is not observed physical touch. |
| Prompt cancellation test | Prompt cancels caller source before guarded CALCULATE, command count remains SELECT+LIST and outcomeCancelled | No admitted/native cancellation; token source is disposed but unlinked to runner. |
| Request callback failure | Same message, no CALCULATE and no resolution | No retained-byte cleanup witness. |
| Successful resolution failure | Exact exception object propagates | Source locally disposes produced keys, but this test never captures those arrays. |
| Operation+resolution failure | Original general ApduException/SW 6982 and one resolution | No broad log-redaction or timeout-causality oracle. |
| Integration symmetric roundtrip | PUT/LIST label+algorithm/DELETE/empty | Applet persistence, not HSM object enrollment. |
| Derived integration | LIST algorithm AES128 | No independent derived key equality. |
| Management rotation integration | New key authorizes later PUT/LIST | No final restoration in helper; default reset-before next run is not post-run cleanup proof. |
| Symmetric calculation integration | Three returned lengths of 16 bytes with random context and omitted cryptogram | No independent key values or live HSM. |
| Asymmetric import/generate/public key | Metadata algorithm; generated response length 65 and prefix 04 in one test | Import verifies metadata only; prefix+length is not an on-curve/private-public consistency proof. |
| GET_CHALLENGE asymmetric integration | Nonempty response | No point shape/valid HSM agreement. |
| Dummy asymmetric integration | Exact general ApduException and nonzero SW using PK-OCE instead of trusted HSM static key, locally generated EPK-SD and dummy cryptogram | Several independently invalid inputs; cannot isolate cryptogram rejection. Retry subclass fails exact type assertion. |
| Both touch-tagged integration cases | Stored `TouchRequired` metadata | Physical CALCULATE/touch is a separate historical probe, not these cases. |
| Password-change integration | New-password omitted-cryptogram calculation returns 16-byte S-ENC | No old-password rejection or full three-key equality in this shipping test. |
| Test helper | Plain reset session disposed before optional-SCP body session; connection encloses both | Body mutations persist; `NewToken` creates undisposed CTS instances and selected password buffers lack explicit clearing. |

The recorder has no native owner and `BeginTransaction` is a no-op. Its queued result arrays and recorded
command clones are test-owned copies. An arbitrary synthetic APDU sequence being accepted by it supplies no
reader or applet grammar guarantee. No source-defined test result here is described as freshly passed.

## Historical probes retained separately

The frozen YubiHSM handoff records 13 integration methods and four final scoped probes, Linux/USB/serial 103.
The independent AES-block CMAC oracle checked RFC 4493's four vectors before device binding; omitted-
cryptogram calculation matched all three keys. The first valid synthetic supplied-cryptogram request
reached strict Parse with 56 bytes, failed, and skipped its three-key/retry-restoration assertions. Its
raw 56-byte payload was not retained. The successful successor omitted cryptograms and observed retries 8→7→8.
Password user/admin lanes, human touch and fresh-session recovery remain distinct outcomes.

Cancellation was requested around 1 second; the recorded terminal outcome arrived around 14 seconds as
6982/Failed. The original expected-OCE/6985 probe failed and skipped recovery. The successor explicitly
measured 6982/Failed and independently recovered via a fresh owned session before reset. Preserve both.
Neither result identifies native cancellation admission or proves natural touch-timeout causality.

## Later validation lanes — proposals only

1. **Managed regression:** coordinator may select the actual full classes in HSM.UnitTests, plus Core
   session/ExchangeGuard/APDU-response/TLV/PCSC-lifetime classes, using current test skill/toolchain.
   Require nonzero execution counts and real warning totals. Do not treat all Core tests as no-native:
   some listener/interop tests access OS services. No execution is authorized by this proposal.
2. **Held lifetime proof:** hold LIST completion, request callback, CALCULATE response and terminal
   callback separately. Observe guard release, second operation admission, disposal completion, response
   owner clear, produced key clear and original method terminal. Release gates/drain in finally. This
   establishes the present boundary; selecting desired whole-method policy is separate work.
3. **Response cleanup:** use distinct sentinel arrays for transport result, APDU clone, fragment owners,
   decrypted temporary and returned keys. Observe the actual identities rather than clearing test copies.
   Inject parser, callback and continuation failure independently. Classify data before requiring erasure.
4. **SCP HSM profile:** use independently checked protected wire oracles for empty LIST/retries/RESET,
   long-label asymmetric CALCULATE fragmentation and protected 61xx retrieval. Current Core fake/card
   conventions differ from inspected final SCP03 empty/status rules; matching that fake is insufficient.
5. **56 response:** reuse the frozen proposal only under coordinator authorization. Capture status/length
   and actual region comparisons, then decide response/result grammar. A passing raw YubiKey comparison
   remains distinct from production-firmware support and a live enrolled HSM/connector handshake.
6. **Platform/device matrix:** identify exact OS/resource-manager/shim/device/firmware/transport and
   parameters; separate caller request, native terminal, drain, release and fresh recovery. NFC/older
   firmware/other operating systems remain unrun. Wait for user-ready Windows delta; keep PIV physical deferrals.

Route any later scratch/output to
`/home/rupicola/Code/y/.core-docs-audit-runtime/core-001/BOUNDARY-YubiHsm` and propagate TMPDIR/TMP/TEMP.
This boundary created no device/connector task and asks for no immediate physical action.

## Complete typed proposal — UNCOMPILED

This composable method uses existing SDK APIs. The application supplies a selected SmartCard-capable
device, a caller-owned prompt and optional caller-owned SCP parameters. Those references remain alive
through the full awaited method and are disposed by their application owner afterward. Parameters must
match provisioned device state; no default/static keys are assumed. The method owns its connection and
session, and lists public credential metadata rather than performing HSM authentication.

```csharp
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.YubiHsm;

static async Task<IReadOnlyList<HsmAuthCredential>> ReadHsmCredentialsAsync(
    IYubiKey device,
    IUserPresencePrompt? prompt,
    ScpKeyParameters? callerOwnedScp,
    CancellationToken cancellationToken)
{
    await using ISmartCardConnection connection =
        await device.ConnectAsync<ISmartCardConnection>(cancellationToken);
    await using HsmAuthSession session = await HsmAuthSession.CreateAsync(
        connection,
        new SessionCreationOptions
        {
            UserPresencePrompt = prompt,
            ScpKeyParameters = callerOwnedScp
        },
        cancellationToken);

    return await session.ListCredentialsAsync(cancellationToken);
}
```

The session is disposed before its caller-created connection. The returned DTOs escape independently.
LIST requests no touch; the prompt parameter shows borrowed service ownership rather than asserting a
notification will occur. This method has no invented connector API. Compiler shape remains
`VALID-NOT-CHECKED`; the existing README's application adapter sketches retain their explicit typed
contracts and previously recorded historical compilation only.
