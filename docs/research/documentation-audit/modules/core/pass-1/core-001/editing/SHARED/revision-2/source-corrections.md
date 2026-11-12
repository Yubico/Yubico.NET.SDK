# SHARED revision 2: focused source corrections

All six review issues are supported by the checked source; no contradiction was found. This is the
editor's correction return for focused independent review, not review acceptance or compilation.
Product base remains `da66f5d01ba594993974ddd26723e8c300c0a454`; runtime source pin remains
`fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`. Only the seven correction-required Markdown paths change
relative to the reviewed subject. The other nineteen content-reviewed changed paths remain byte-identical.

The original `edit-SHARED.md`, all initial editor/reviewer records, and original product patch
`3267ab5d547b281f19c28b3b709ef509a7cd54ee82439b6386b33a842030b937` remain unchanged. The new
`product.patch` is the full base-to-current successor, not a patch to replace the preserved original.
`corrections.patch` separately shows only the reviewed-subject-to-current delta.

## R1 — Actual constructor, units, and public connection shape

Checked complete relevant declarations/bodies:

- `PcscProtocol.cs:44–70,162–195`: constructor takes `ISmartCardConnection`, optional remaining INS
  memory, then optional logger. `MaxApduSize` starts at `SmartCardMaxApduSizes.Neo`; configuring 3.5.0
  takes the below-4.0 return without replacing that ceiling.
- `SmartCardMaxApduSizes.cs:17–34`: NEO complete-command ceiling is **1390**, independently of the
  **255-byte short data chunk**. Neither is the stale 254-byte assertion.
- `ISmartCardConnection.cs:19–72` and `IConnection.cs:26–29`: public Type is available; IsConnected is
  absent. Fake-specific producer state does not add that property to the typed interface.

Root `CLAUDE.md` now contains a complete Core-unit-test-context class using the actual internal fake,
`new PcscProtocol(connection)`, scoped connection/protocol disposal and numerical 1390 expectation.
The protocol borrows the fake connection; it is disposed first. The example remains current, not
historically excluded. `docs/TESTING.md` now has a complete illustrative assertion helper borrowing an
`IYubiKey` fake and owning its returned `ISmartCardConnection`. It checks actual `connection.Type`,
without an invented liveness property or a fake construction API. Both remain uncompiled.

Mapping: CORE-B6-C001/FPT-067, CORE-C-C017/FPT-099, CORE-B4-C010/FPT-057.

## R2 — Sorted incremental acquisition before native open

`DeviceConnectionRegistry.cs:94–124,193–245` sorts/deduplicates IDs, awaits each acquisition, and
rolls back earlier claims in reverse order on failure. Waiting for an earlier discovery lease can
precede checking a later live holder; cancellation can win during that wait. The contributor guide
now describes this actual sequence, before-native-open refusal and rollback instead of whole-set
atomic preflight or invocation-time failure. It keeps native-release proof route-specific and retains
the Windows/custom compatibility limitation. No admission or release policy changes.

Mapping: CORE-B1-C012/FPT-033; related CORE-B1-C005/FPT-026 and CORE-B4-C001/FPT-048.

## R3 — PIN peer profile requirements versus implemented checks

`PinUvAuthHelpers.cs:58–117` checks X/Y byte-array presence and 32-byte lengths, forces P-256 import,
and relies on provider point acceptance. It omits peer kty/crv/alg and optional-label restrictions;
its generated platform dictionary does not validate the peer. `src/Core/CLAUDE.md` states those
implemented checks and the validation gap, while retaining the distinction from open catalog metadata.

Original retained CTAP **2.1 Proposed Standard, 15 June 2021, errata edition 21 June 2022**, §6.5.6:
`investigation/B5/sources/ctap21.txt:8227–8230` directs ecdh to parse peerCoseKey as getPublicKey and
reject unsuccessful/off-curve results; `8272–8292` defines kty=2, alg=-25, crv=1 and 32-byte X/Y.
§6.5.7 at `8321–8325` supplies protocol-two inheritance. These are protocol implementation requirements,
not evidence of SDK enforcement. No validation fix, new intended forward-compatibility policy, or
authenticator token-reuse claim is introduced.

Mapping: CORE-C-C015/FPT-097; boundary CORE-CTAP-X006 and inherited ctap2-001:CTAP-IMP-007 remain separate.

## R4 — Late discovery work and borrowed logging-factory lifetime

`FindYubiKeys.cs:43,230–271` retains a static logger and observes abandoned enumeration asynchronously,
including later completion/failure logging. `YubiKitLogging.cs:38–39,52–66` creates loggers from the
current factory, then temporary disposal restores only the factory reference. It does not replace
already captured loggers. One-shot retirement and bounded wait completion are not a late-work join.

The discovery table now promises no caller-owned monitor/start-stop lifetime, not absence of background
cleanup. The temporary logging helper borrows an `ILoggerFactory` and never disposes it. Nearby text
requires the application-owned factory/providers to outlive late completion and later uses of captured
static loggers. Process-global/non-isolating restrictions remain. No drain API, timeout, or framework
is invented, and helper completion is not treated as an ownership boundary for that factory.

Mapping: CORE-A-C008/FPT-008, CORE-B1-C002/FPT-023, CORE-C-C008/FPT-090, CORE-B6-C014/FPT-080.

## R5 — Original CBOR byte controls the late-success exception

`RawCtapHidSession.cs:64–74` forwards the caller command unchanged.
`FidoHidProtocol.cs:290–310,384–410` checks late success against the original command exactly equal
to unencoded CBOR **0x10**, with nonempty response beginning **0x00**. Terminal correlation separately
masks the initialization bit; encoded **0x90** can correlate but miss that success classification and
surface cancellation. Both ownership and raw-tier guides now preserve this exact condition and call
the encoded path a current normalization gap, not intended alternate grammar.

The retained CTAP **2.3 Proposed Standard, 26 February 2026**, §11.2.9.1.5 assigns cancellation of
outstanding cancelable CBOR work and the original canceled-request response to the authenticator,
with no response to CANCEL itself. This protocol duty supplies no broader SDK raced-success rule.
The existing no-response CANCEL/original-response drain and indefinite-silence qualifications remain.

Mapping: CORE-B5-C002/FPT-059. No normalization/runtime change or new wire experiment.

## R6 — Complete method and mixed-history eligibility

- Reviewed body **65**, `docs/MEMORY-MANAGEMENT.md`: all complete bytes remain unchanged, SHA-256
  `9642d27320b61b9da546aee4260d55f32829d95c80f5e3677ae7b2c6357d3119`. It is now classified as a
  complete compilable-unchecked method. It borrows an open raw session, owns/clears its request and
  response, and still requires a valid selected applet/command; illustrative bytes are not device support.
- Reviewed body **118**, `docs/architecture/raw-access-tiers.md`: all complete mixed body bytes remain
  unchanged, SHA-256 `048e4a43422e260a9df70e5e85b7e1576cb644c625850265b5811e1ca4e311d5`.
  The whole body is retained as mixed historical/current material. Only Before is historical/excluded;
  After remains a current context-dependent compilation-eligible fragment borrowing the caller's
  connection/AID/token and owning its raw session. Exact sub-body text, boundaries and hashes accompany
  the complete body rather than replacing it with matching lines.

Mapping: CORE-C-C017/FPT-099, CORE-B6-C015/FPT-081, CORE-B5-C009/FPT-066.

## Verification and unchanged holds

Revision receipts freshly check all current complete bodies/coordinates/hashes, all original record
identities, the nineteen unchanged content-reviewed paths, the seven correction paths, current local
file targets/fences, full successor patch and focused delta. These checks do not compile examples,
bind APIs/crefs, execute tests, or provide native/platform/device qualification.

All original policy, implementation, experiment and qualification holds remain. C019 has no D release;
metadata alias/serial cache, native producer/release, cross-process PIN state, provider erasure,
SCP edition/final/vendor gaps and FPX unrun status are unchanged. Future Windows delta and PIV physical
deferrals remain. No runtime/config/product script, other checkout, original editor/review record,
Git index/ref/commit/push, build/test/formatter/compiler/native/crypto/device operation or subagent
is part of this correction. Finish focused review, Core Pass-1 publication and deferred handoff, then stop.
