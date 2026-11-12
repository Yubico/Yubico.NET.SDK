# Boundary source register and bounded research

## Source authority

The audit source is `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`; current checkout
files are compared by raw inventory hashes and normalized pinned Git content in
`file-identities.json`. Whole-file and exact selected character-range identities
remain in `unit-dispositions.json`. Reading supplies semantic conclusions; hashing
alone supplies preservation evidence.

The investigator read current root/Core/YubiOtp/nested test instructions, Core and
YubiOtp guides, workflow/style/SDK house style, execution plan, dispatch/inventory,
inherited registry/routing and completed A/B1/B2/B3/B4/B5/B6/C reports. Core-family
findings retain their original owners. Narrow Management, CLI and Core callers are
read-only context rather than newly accepted whole-module coverage.

## Independently read original comparator sources

| Source and exact revision/status | Sections read | Actor, strength and applicability |
| --- | --- | --- |
| Yubico yubikey-manager, `4ca60f706af930459138d8dc0f0f953480e1c7a4`, project 5.9.3-dev.0 | Complete `yubikit/core/otp.py:1–275`; complete `yubikit/yubiotp.py:1–928` | Pinned sibling host implementation, descriptive source policy, not normative device specification. Byte framing, length admission, NEO catch, sequence rules, backend prefix/exact-length differences, version selection and padding were compared directly. Python immutable bytes and connection close ownership do not define C# lifetime. |
| Yubico yubikey-personalization, `db0c0d641d47ee52e43af94dcee603d76186b6d3`, vendor implementation/header | `ykcore/ykcore.c:490–752`; `ykcore/ykdef.h:45–274` | Host implementation and named wire constants, no RFC-style SDK MUST/SHOULD obligation. Describes feature flags, 64-byte frame layout, packetization, force-update/abort, variable ticket fields, configuration/status/map fields. Header alone does not establish C ABI padding or universal current-firmware support. |

The existing shared Python manifest/context identifies repository/full revision,
remote freshness check, installed-release distinction and bounded history. This
worker uses that pin without a new checkout, package import, CLI, crypto or test run.
`source-identities.json` independently compares the two local Python files with
`git show` at that pin and verifies the already captured vendor bytes against B5's
immutable-URL capture hashes. The retained B5 originals were accessed on November 11,
2026 UTC. This boundary read them on the November 12 campaign session without
refetching those vendor originals or inventing a replacement retrieval timestamp.

Original immutable URLs:

- `https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/core/otp.py`
- `https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/yubiotp.py`
- `https://raw.githubusercontent.com/Yubico/yubikey-personalization/db0c0d641d47ee52e43af94dcee603d76186b6d3/ykcore/ykcore.c`
- `https://raw.githubusercontent.com/Yubico/yubikey-personalization/db0c0d641d47ee52e43af94dcee603d76186b6d3/ykcore/ykdef.h`

Consequential comparisons:

1. Python `_receive:189–196` checks exact eight-byte input before indexing. C# Core
   trusts custom typed providers, while built-in routes enforce their own invariant.
2. Python NEO catch `146–151` permits CommandRejectedError only. C# catches broader
   failures after successful reset; source difference supplies no physical failure result.
3. Python data backend `665–669` validates expected CRC prefix like C#; SmartCard
   `701–705` requires exact data length while C# accepts a prefix. Both are implementation
   policies; no unavailable normative host requirement is invented.
4. Python SmartCard sequence `685–699` inspects returned raw version with narrower
   zero/status conditions and 5.0.0–<5.4.3 interval. C# uses effective configured
   version and 5.0.0–<5.5.0 bypass plus any zero. Preserve OTP-IMP-005 rather than
   adopting sibling policy without a product decision.
5. Python initialization `716–747` probes Management on NFC, with its own NEO condition;
   C# probes on USB and NFC and keys greater-version selection on raw OTP major three.
   C# override then applies separately. Neither version field is a hardware attestation.
6. Python NDEF null `274–292` supplies a default URI or empty text; C# sends all-zero
   disable. Keep the existing applet null/empty intent and target-device qualification.
7. Vendor frame writer sends all-zero middle packets selectively, C# sends all ten;
   that optimization difference is not missing-payload evidence. Vendor 0x8F and
   Python 0xFF reset bytes are invalid report-sequence controls, not applet factory reset.
8. Vendor SCAN_MAP contains the 45-entry mapping and SHIFT flag. Static-password
   capacity 38 is a separate configuration layout, preserving OTP-IMP-002 scope.

## Framework API evidence

Context7 resolved `/dotnet/docs` and was queried during this boundary investigation
for Task.WaitAsync cancellation and original-task/buffer lifetime. Its response cites:

- `https://github.com/dotnet/docs/blob/main/docs/standard/asynchronous-programming-patterns/cancel-non-cancelable-async-operations.md`
- `https://github.com/dotnet/docs/blob/main/docs/standard/asynchronous-programming-patterns/snippets/coalescing-cancellation-tokens-from-timeouts/csharp/Program.cs`

**Revision/status:** current Context7 documentation result, mutable main, no immutable
revision supplied. **Sections:** cancel-only-the-wait, safe handling of abandoned work,
WaitOnlyTimeoutDemo. **Actor/strength:** .NET consumer guidance and illustrative example,
not an OTP/native cancellation requirement. **Applicability:** an outer timed/canceled
wait can finish while the original operation continues; retain/observe the original
task and keep its owned resources alive. B5's independently pinned dotnet/runtime
v10.0.0 Task source corroborates its own exact implementation scope. No installed
runtime identity or new .NET execution is claimed here.

A second Context7 query checked await-using disposal/return ordering. The focused
follow-up read Microsoft's original **using statement** language reference at
`https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/using`.
Its metadata identifies dotnet/docs revision
`399ea4a18a9312379c5b3306bbffcc753491c628`, January 16, 2026 content date. The exact
raw Markdown was retained as `using-reference.original.md`; access/status/hash/bytes
are in `using-reference-capture.json`. Read sections describe await using, scope-end
disposal, reverse declaration order, and return lowered through try/finally before
the method returns. **Actor/strength:** Microsoft C# consumer/reference guidance,
descriptive language semantics rather than a device normative requirement. The
X004 conclusion combines that guidance with the actual extension and session
DisposeAsync source; it remains a source-derived exceptional-path candidate,
not an executed compiler-lowering or native-failure experiment. Context7's WCF
exception-masking illustration is contextual guidance, not an OTP API requirement.

## Inherited source and historical evidence, kept separate

- B2/B3/B4 source registers own exact Linux/Apple/Microsoft/NativeShims authority,
  ABI, release and deployed-binary qualifications. This worker independently reads
  actual C# routing/owner/facade bodies for YubiOTP applicability; it does not claim a
  new independent OS specification review or native run. Future Windows work remains
  the user's separate ready handoff, outside the pin.
- B6 owns APDU/SCP original standards: retained SCP03 1.1.2 Public Release versus
  code-cited Public Review, retained SCP11 Public Review versus unavailable final,
  ISO 7816 exact-clause access and native/crypto gaps. No Core generic SCP floor is
  promoted into YubiOTP applet support. Caller parameters remain borrowed through
  creation; session does not silently dispose those objects.
- The frozen YubiOtp run's `hardware/results.md` and external `yubiotp.md` retain
  actual prior evidence: serial 103/Linux, metadata 5.8.0.beta.0/raw 0.0.1, twelve
  HMAC vectors, HID AES ticket verification, successful touch and scoped cancellation/
  timeout outcomes, CCID HMAC 6985 rejection, empty-slot cleanup. These are historical
  run results, not current Core-native acceptance or executions by this worker.
- Legacy Manual 3.3, personalization v1.20.0 and manager 5.8.0 references already
  qualified in the frozen applet run remain earlier authority. This boundary does
  not claim to have re-reviewed their PDFs or upgraded legacy rules to every 5.x key.

## Research bounds and precise remaining gaps

The initial pass read the pinned C#/Python/vendor implementation context and Core
family source registers. One focused follow-up checked actual public snapshots,
Management's reader/modern TLV mutator, both static-password CLI routes and prior
hardware outcomes. No additional open-ended source search is planned.

Remaining qualifications have precise evidence owners:

1. **Earliest firmware/NEO rationale:** vendor evidence for the broad C# NEO catch,
   exact sequence-bypass interval and per-flag/password/map matrix is absent. B5/app
   maintainers must choose behavior; a modern major-zero fixture is insufficient.
2. **Custom input/response ownership:** API policy must define malformed-provider
   exception and borrow/transfer/clear contracts before universal cleanup is promised.
3. **Legacy/FIPS surface equivalence:** current named typed surfaces are now inventoried;
   old FIPS-mode query equivalence and legacy four-byte mutation are absent from this
   current typed API. Device support/raw recipes require separate design and verification.
4. **Physical/platform matrix:** older firmware, NFC read-back, keyboard output/host
   layouts and new Windows source/runtime qualification remain unexecuted. PIV physical
   deferrals and the unexplained historical serial 9681620 failure remain untouched.
5. **Independent acceptance:** fresh original-citation/content review, edits, sampling,
   meaningful consumer compilation and selected runtime tests remain later assignments.
