# Small hardware-free wording-critical experiment shortlist

**All four proposals are unexecuted.** This ledger creates no host, probe implementation, test,
compiler job, native call or device operation. Astra may later select a smaller subset after independent
source review. Use the existing validation foundation and an explicit later execution scope; preserve
the original inputs, actual outputs, failures and source/output identities. Hardware-free does not
automatically mean provider-free or permission to compile here.

The purpose is to settle a consequential ambiguity in consumer wording, rather than increase coverage
or repeatedly demonstrate behavior that a completed source report already establishes. A measured
current outcome also leaves the intended policy, original-source authority and deployment cells separate.

## Priorities

| Priority / ID | Exact question and smallest useful observation | Wording decision it informs | Inputs, limits and stop condition |
| --- | --- | --- | --- |
| **1 — FPX-01: returned metadata alias** | Decode synthetic timeout/firmware TLVs; snapshot bytes and content hash before and after input-TLV disposal. Include independent caller-memory mutation and scalar-field controls. Observe the actual returned timeout backing storage, without clearing a clone as a substitute. | `CachedDeviceInfo`, DeviceInfo equality/hash and Management result examples need a precise borrowed-storage caveat. The observation can settle whether an example remains valid across input disposal; the scalar/copy/explicit-borrow policy still needs a decision. | A report C003 / A-E01, C C005/C006, Management follow-up dependency. Use only public synthetic metadata. One disposal vector and one mutation control suffice; no device or crypto provider is needed. The source already establishes the alias mechanism; this is a compact independent consumer demonstration before stronger snapshot wording, not a prerequisite for acknowledging the source concern. |
| **2 — FPX-02: selected ABI and constant carriers** | In one isolated managed layout/constant consumer, compare size/offset/carrier declarations against a small independently transcribed manifest of the *already retained pinned headers*. Include IOReturn reserved gaps/Invalid, Win32 BROKEN_PIPE and backend unsupported-feature/raw values, CFNumberType versus SInt32 value storage, Windows PULONG/BOOLEAN/BOOL/LPGUID shapes, and managed PCI fields. Label architecture-specific expectations explicitly. | Replace universal “signatures match” or symbolic-name assurances with exact carrier/backend/architecture conditions. Compile-time consumer layout helps expose assumptions before publishing native-contract tables. | B3 C001/C009; B4 C003/C006/C009 and source registers. This is a limited carrier table, not a new whole-header audit. The conditional published Linux LP64 two-unsigned-long PCI is 16 bytes versus managed 8; Windows two-DWORD PCI is 8, Apple headers/producer remain separate. Managed layout or a host-size check proves neither foreign calling convention nor installed native producer correspondence. No native loader/import executes; generated stubs and native ABI harnesses require a different later assignment. |
| **3 — FPX-03: independent SCP/APDU transcript** | Keep a tiny table of independent expected bytes and state transitions for (a) API Le zero versus absent Le, (b) empty protected command: counter advance with no command-data encryption under inspected SCP03 release rules, (c) genuine setup versus later INS82 empty-success exemption, and (d) one response split inside ciphertext/R-MAC plus a status-only 6A82 control. Run only the selected cells after the supported profile and observation seam are agreed. | “Transparent chaining,” “authenticated response,” empty-command and setup-phase wording depends on exact bytes/phase, not whether the current fake accepts itself. A compact transcript can separate a format example from a supported-profile/conformance assertion. | B6 C001/C004/C005/C011/C012 and its unrun probe plan; B5 readiness distinction. Expectations must come from independently reviewed **SCP03 1.1.2 Public Release**, not the current synthetic card or Python agreement. SCP03 1.1.2.6 review clauses, final SCP11 1.4 and exact YubiKey profile remain qualified. Start with pure byte-format cells; managed mock/provider use needs explicit later scope, and any actual provider/native route is separately assigned. No hardware exploit, actual card error/recovery or interoperability conclusion follows. |
| **4 — FPX-04: ASN.1 single-key/profile boundaries** | Use retained synthetic or published public fixture bytes to inspect emitted ECPrivateKey inner parameters and explicit wrapping, then add one top-level tail, one nested tail and one optional-attribute case to the selected managed decoder paths. Record factory-specific acceptance and exact exception type. Keep supplied-Q/D-only provider derivation outside this parser probe. | Examples and XML should distinguish exact AlgorithmIdentifier consumption, supported container subset and RFC 5915 target profile from complete DER or mathematical validation. A finite table may settle the consumer-facing subset before promising one uniform factory contract. | C C009/C010/C016, latest C clarifications and final source register. Decide the supported target profile first; a runtime acceptance result cannot override RFC prose. Choose paths that require no signing, derivation or native import. If construction invokes an actual platform provider, stop that cell and refer it to a separately approved provider lane. This is lower priority than direct source-backed documentation corrections. |

## Source-supported corrections that need no new experiment merely for coverage

- Qualifier `uint.MaxValue`, current feed/ownership vocabulary, current fake API/type names, explicit
  per-exchange scope, retained confirmation entry and configure-before-first-use logger behavior can
  proceed to fresh editing/review from their completed evidence.
- C's latest clarification settles the inspected X.690 passages, the pinned .NET v10.0.0 supplied-Q
  route distinction and AsnWriter Encode/Reset behavior. Repeating a test is unnecessary to correct
  the old confidence wording. Installed runtime/provider erasure remains a different qualification.
- B6's copied response status/data owner, visible private copies, always-appended Le and inspected
  synthetic-card behavior are source facts. The shortlist is required only before a stronger example,
  phase, wire/profile or end-to-end guarantee is selected. Existing test comments can describe their
  actual assertions now, subject to independent review.
- B4's exact current synchronous Windows route, access split, published static-AOT target intent and
  conditional PCI forwarding path can be qualified from the completed report. No new test is needed
  simply to remove a universal elevation, async-responsiveness or installed-producer assertion.
- B1's NIST-PIV issue requires the missing original-PDF review, not a fake test as a replacement for
  authority. Original PC/SC PDF acquisition is already recorded separately. Physical PIN/power-off and
  cross-process isolation are outside this hardware-free shortlist and remain user-deferred where applicable.

## Separate later engineering/decision probes, deliberately outside the priority set

Keep the reports' proposals for logger-after-open cleanup, listener startup/unregister faults,
ExecutionContext sentinels, native worker/report-array cleanup, queued/redirected credential cancellation,
OTP cadence, KeyReference/ATR equality and minimal libudev deployment. They may become meaningful
selected Pass-2 or qualification work after policy and scope decisions. They are not a blanket prerequisite
for source-accurate documentation. Full workflow/callback ownership still needs the actual boundary
returns; additional Core fake tests alone cannot close those call sites.
