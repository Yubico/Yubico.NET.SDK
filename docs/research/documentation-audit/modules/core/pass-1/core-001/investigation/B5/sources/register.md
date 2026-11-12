# B5 original-source register

Original bytes, URL, UTC access, byte counts and SHA-256 are in `captures.json` and
`captures-followup.json`. Text files are derived HTMLParser extraction; the retained HTML is authority
for tables and section numbering. Retrieval clock reported November 11, 2026 UTC while the session's
calendar date was November 12; preserve that actual timestamp, not an invented later access time.
No retrieved source was executed. Research had one initial pass and one focused follow-up; remaining
source/intent gaps below are bounded handoffs, not an open-ended fetch queue.

Wire command, status and control-byte values below use hexadecimal notation (for example CBOR 10 means
0x10). Byte lengths, offsets, line coordinates, time intervals and version components use decimal.

## CTAPHID originals and exact applicability

| Original | Edition/publication status | Inspected sections | Actor and strength |
| --- | --- | --- | --- |
| [CTAP 2.0](https://fidoalliance.org/specs/fido-v2.0-ps-20190130/fido-client-to-authenticator-protocol-v2.0-ps-20190130.html) | Client to Authenticator Protocol; Proposed Standard, January 30, 2019 | §8.1 framing/channels/arbitration and §8.1.9.1.5 cancellation; exact cancel passage independently read | Host/client sends request; authenticator allocates CID and emits response. MUST/SHALL clauses apply to specified roles, not universal SDK connection scheduling. |
| [CTAP 2.1 errata](https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-errata-20220621.html) | Client to Authenticator Protocol; Proposed Standard, June 21, 2022, errata edition of June 15, 2021 | Edition header and §11.2.9.1.5 exact cancel passage independently read | Same CBOR-active-CID cancel actor distinction; do not copy §8.1 numbering from 2.0. |
| [CTAP 2.3](https://fidoalliance.org/specs/fido-v2.3-ps-20260226/fido-client-to-authenticator-protocol-v2.3-ps-20260226.html) | Client to Authenticator Protocol; Proposed Standard, February 26, 2026 | Complete §11.2.1–11.2.9.3 read, including all packet fields, channel allocation/version/capability/error/control tables; narrow §11.3.3 AID | Current selected authority for source comment citing 2.3. It does not upgrade all SDK applet commands to 2.3 conformance. |

Consequential original passages, with 2.3 derived text coordinates for reproducible lookup:

* **§11.2.3** (13515–13532): CID is 32 bits, authenticator allocated/vendor algorithm; 0 reserved,
  FFFFFFFF broadcast. **§11.2.4** (13534–13612): initialization report fields are CID (4 bytes), CMD (1), length (2), and data;
  continuation fields are CID (4), SEQ (1), and data. Init bit 7 is set, continuation clear; sequence 0..127; full report always sent;
  unused bytes SHOULD be zero. For 64-byte reports, 57 + 128 × 59 = 7609. Fixed 64-byte reports are this SDK's supported connection
  contract; the standard allows descriptor-defined packet sizes rather than universal 64-byte reports for every HID.
* **§11.2.5.1–.3** (13625–13668): request, processing, response atomicity; another channel busy gets
  busy error; host MUST assume another process can transact between transactions and former state can
  be dropped. Same-active-CID INIT can abort/resynchronize, if possible. Cancellation after full request
  is separate. This does not justify the shorthand “a foreign init packet always aborts it”: same CID,
  actual INIT command and different-channel busy are distinct cases. SDK managed exclusion is stricter
  than simply relying on multi-client channel arbitration.
* **§11.2.9.1.1/.2** (13823–13870): MSG 03 encapsulates CTAP1/U2F; CBOR 10 encapsulates a CTAP command byte
  plus CBOR, response begins CTAP status. HID command 10/on-wire 90 is not a CTAP operation code. Keepalive
  may precede CBOR response. MSG/U2F response semantics remain owning protocol work, not Ctap2 parsing.
* **§11.2.9.1.3** (13872–13940): nonce (8 bytes), assigned CID (4), protocol version at byte 12, vendor-defined major/
  minor/build at bytes 13–15 and capabilities at byte 16. Host SHALL allocate using broadcast and SHALL accept responses
  longer than 17 bytes for extensions. It compares nonce before storing CID. CTAPHID protocol version 2 is
  distinct from CTAP edition 2.0/2.1/2.3 and device firmware 5.x. Vendor-defined device version does not
  automatically equal Management's parsed qualifier/effective firmware.
* **§11.2.9.1.5** (13961–13973), identical relevant clauses independently read at 2.0 text lines 4879–4891 and
  2.1 text lines 12656–12668: authenticator MUST cancel outstanding cancelable work; canceled original request
  returns KEEPALIVE_CANCEL. Authenticator MUST NOT reply to CANCEL itself. Client MAY send during CBOR;
 2D MUST be that request response, not HID ERROR. No active CBOR/nonactive CID SHALL ignore it. The
  standard's absence of an instruction to discard a raced success is not a normative host-MUST-return
  success requirement; returning real CBOR success is this SDK's explicit nonrollback policy.
* **§11.2.9.1.6/.7** (13975–14029): HID ERROR 3F, BCNT 1 with error byte; KEEPALIVE 3B, BCNT 1 with processing 1
  or UPneeded 2. Keepalive does not complete transaction; authenticator SHOULD emit at least every 100 ms
  and status change. That emission cadence is an authenticator requirement, not a native-read deadline or
  .NET cancellation guarantee. Keepalive prose says MSG while CBOR section explicitly permits it; this
  source wording is retained, not generalized into vendor-command cancellation permission.
* **§11.2.9.3** (14079–14084): implemented vendor commands MUST use 40..7F before on-wire init mask.
  Core's encoded C0/C2/C3 values normalize to 40/42/43. Yubico command semantics remain vendor/app-level.
* **§11.3.3** (14108 onward): FIDO RID A000000647/PIX2F0001 supports ApplicationIds.Fido/FidoU2f same
  AID. Other AID values have local exact-byte tests and current factory callers, not nine independently
  verified standards in this packet. ISO APDU/SCP framing is B6; no broad ISO conformance disposition.

## Yubico OTP original implementation authority

`yubikey-personalization` remote master resolved with read-only `git ls-remote` to
`db0c0d641d47ee52e43af94dcee603d76186b6d3`. Initial master captures and focused immutable-URL follow-up
have identical hashes. This is pinned **vendor implementation/header**, not a revisioned normative OTP
standard or deployed-device proof. No clone/install/native library was needed.

* `ykdef.h:56–79` defines wait 20/pending 40/write 80, dummy 8F, payload (64 bytes), command, ushort CRC, filler (3 bytes);
  `status_st:253–265` has firmware (3 bytes), programming sequence (1), touch-level (2). Packed header interpretation
  must be read with actual serializer/report code, not a C struct's assumed host padding/endianness.
* `ykcore.c:672–740` copies a padded 64-byte payload, calculates CRC over payload, writes little-endian via its swap
  helper, forms a report with seven data bytes plus sequence/write and polls device write flag before sending. It skips middle all-zero
  reports, unlike C#'s ten always-sent reports. This optimization is not a requirement or a missing-byte
  correctness defect by itself. `743–752` dummy 8F invalid-sequence force-update/abort is separate from
  destructive applet factory RESET.
* `ykcore.c:572–665` reads seven-byte chunks, pending flag and terminal sequence 0; known expected data length
  gets CRC over expected+2 and rounded report capacity. C# Core returns raw assembled data and applet
  validates a command-specific prefix; do not invent universal inbound length-at-64/CRC-at-65 fields.
* `ykcore.c:490–570` uses exponential backoff and its own touch/wait policy. Its timeout comments do not
  bound the C# path's native GET duration. `229–256` programming-sequence change/deletion interpretation
  is vendor evidence, not cryptographic response authentication.

Shared Python source is the existing clean local pin
`4ca60f706af930459138d8dc0f0f953480e1c7a4` (5.9.3-dev.0), identified by
`../../../sources/python/manifest.json` and context.md. B5 read complete `yubikit/core/otp.py` from
`git show`, without importing package or changing checkout. `reuse-identities.json` records its blob,
raw SHA-256 and exact path. It confirms CRC FFFF/8408/F0B8, a little-endian outbound 64-byte payload plus slot/CRC/filler,
reports with seven data bytes plus control, and sequence/deletion logic. Its NEO catch is CommandRejectedError only; C# catches
broader failures. Python reset uses FF while C/vendor header uses 8F; both are invalid sequence writes,
not evidence that one resets persistent applet state. Python busy/touch polling/rejection and immutable
bytes ownership differ; matching bytes does not settle C# cancellation, secret-memory or latency policy.
No duplicate vendor SDK repository was fetched. C's pinned OATH floor/history is reused without renewed
SCP spec hunting; B6 retains that wire source family.

## Framework/API evidence

Context7 `/dotnet/docs` was resolved and queried for three topics: Task.WaitAsync wait versus operation,
ValueTask/DisposeAsync/SemaphoreSlim disposal, ExecutionContext/AsyncLocal/ConfigureAwait. Current docs
orient API meaning; exact consequential implementation is pinned to dotnet/runtime **v10.0.0** below.
This is not an assertion that the installed runtime has those exact bytes or a .NET execution result.

* Reuse B1 `Task-v10.0.0.cs:2832–2940` (hash in reuse-identities): WaitAsync returns original task for
  completed/infinite cases, otherwise creates CancellationPromise. Token/timer completes only promise;
  Cleanup removes continuation, not cancellation of source. Keep original task and its buffer owner until
  actual terminal. Observe later exceptions/results after an abandoned wait.
* `ValueTask-v10.0.0.cs:29–53`: await directly once; for caching, repeated awaiting/continuations or
  combinators convert once with AsTask and reuse Task. Multiple independent DisposeAsync calls can share
  SDK DisposalGate's Task; this grants no universal permission to consume one ValueTask repeatedly.
* `SemaphoreSlim-v10.0.0.cs:925–964`: Dispose is not thread-safe against other members and clears
  wait structures. Test cleanup must first release/drain actual waiters, then dispose primitives. No
  suggestion to add a session semaphore or infer fairness/operation lifetime from this BCL contract.
* `ExecutionContext-v10.0.0.cs:52–110`: Capture reads current context unless flow suppressed;
  SuppressFlow creates suppressed shallow state. Context7's executioncontext-synchronizationcontext and
  consuming-TAP articles explain ConfigureAwait(false) changes continuation scheduling, not ambient
  ExecutionContext. FIDO AsyncLocal self-exchange detector belongs to protocol flow; native workers'
  explicit SuppressFlow is a different ownership boundary (B1/B2/B3).

## Bounded unresolved sources and future evidence

ConnectionSessionGuard's unnamed Rust/Python Session comparison has no exact original repository/full
pin. The identified Python manager source does not establish that claim. One focused local-pin search
did not locate the named ownership mechanism; keep C009 provenance gap and C# source reasoning separate.
The FIDO test's python-fido2 `_do_call` comparison is likewise an unpinned sibling remark, not an
independently verified compatibility requirement here. CTAP original and current C# late-success policy
are enough to state the actual contract without upgrading that remark to authority.

B4 remains pinned Windows/support source work, and later user Windows CCID/HID work is pending an explicit
ready delta. B2/B3 native release states and B1 PC/SC states are reused with their own exact source/runtime
qualification, never promoted to universal interface guarantees. Historical Linux serial 9681620 failure
remains unexplained; fake Windows link 2/portable SCard framing cannot establish that historical cause.
No physical checks, touch, device metadata reads, PIN, reset or native probes ran in B5.
