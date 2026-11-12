# Verification proposal and assertion scopes

**Execution:** zero SDK builds/tests, native/crypto/device operations or formatters in this packet.
Python/Git performed record/source reads and hash joins only. No test success is claimed. Product source
remains pinned, later campaign metadata commits notwithstanding. The static checker checks identity and
record consistency, rather than independently reviewing technical content.

## Actual assertions consumed

| Existing source, read with implementation/fake context | Oracle supported by source | Limit |
| --- | --- | --- |
| IYubiKeyExtensionsTransportTests, complete selected methods and all three fake connect probes | Chosen IFidoHid/ISmartCard type, unsupported/nonconcrete requests, no second SmartCard after held HID, original connect exception | Probe throws before protocol initialization; no physical grouping or working applet |
| Ctap2SessionTests, entire667-line file | Owned async disposal/idempotence/shared failure, readiness refusal, borrowed failure's sync Dispose count, SmartCard policy and intent contexts, callback error precedence/no-send, silent generic/up:false/Selection behavior | Borrowed probe lacks async-disposal assertion; versions-only GetInfo omits required AAGUID; fake presence not a gesture/RP verification |
| Ctap2BackendLifecycleTests, entire214-line file including request/response helpers | Status-stripped borrowed views, same CTAPHID0x10, no backend INIT, CTAP status outcome mapping, actual response storage clears on ordinary success-resolution failure | No throwing logging-provider case; Data clearing check is separate from all raw SW/intermediate copies |
| Ctap2SessionHidCrossLayerTests, complete methods and FramedFidoBridge | SCP-before-I/O, idle borrowed Mac reuse, pending exchange terminal wake without borrowed release, exactly one CANCEL after complete multi-report request, held original continuation before outcome and correlated GetInfo reuse | Fake bridge below production Mac owner; no macOS native/driver qualification or full-workflow disposal lease |
| Ctap2SessionLinuxCrossLayerTests, entire254-line file including NativeFido | Real public typed Linux slot/device/native owner composition; held creation invocation returns pending, borrowed reuse, cancel/drain/claim refusal and owned shutdown through true quiescence/reopen | Fake report/reactor APIs and synthetic versions-only replies; no libc/hardware/AOT qualification |
| CredentialManagementWireTests selected methods and complete wire helpers | Request fields, first reported total/order, readonly collection, captured clone views zero on successful result disposal, continuation cancellation/decode propagation and unchanged borrowed responses | Continuation tests observe original responses, not inaccessible abandoned clones; no global adjacency lock or real V2 MAC (fakeV2/tag16) |
| CredentialManagementModelsTests.StoredCredentialInfo_Dispose_ZeroesOwnedViewsAndKeepsPublicFieldsUsable | Exact raw/key aliases clear; repeated disposal; getters reject; independently decoded fields usable; source unchanged | Private clone owner only; failed clone/transport ownership separate |
| CoseArkgP256SeedKeyTests selected encode/decode methods | Synthetic coordinates/wrapped-map identifiers and round-trip fields | No valid point, vendor/draft conformity or native crypto qualification |
| PreviewSignGeneratedKeyTests selected KAT/context/IKM methods | Exact one derived public point, nonempty handle, borrowed device handle bytes, copied context, finite input comparisons | No accessor algorithm propagation or all-context uniqueness proof; native test existence supplies no execution here |
| UserPresenceProbeTests selected methods/recording prompt | One requested/resolved context, Completed or Cancelled, None resolution token, later nonempty GetInfo versions | Human-coordinated existence only; timer signals cancellation and supplies no completion deadline |
| Ctap2SessionSimple/SmartCard/Transport integration selected methods and helper normalization | Concrete direct SmartCard open, fresh info fields; default helper can set/change/verify PIN; explicit SmartCard creation request; nonempty assertion bytes | Broad SELECT wrapper may skip; attributes filter candidates and do not alone pin ordinary convenience sessions |

The assigned applet-specific import/preamble/DTO ranges were read for boundary applicability. Their
earlier whole-module parser/algorithm/schema findings remain applet-owned; this packet never upgrades
their existing oracle descriptions to a fresh whole-file conformance review. Selected complete integration
operations were read with actual setup/cleanup. They retain human-coordination and best-effort restoration
bounds; even a GetInfo callback can follow PIN-normalizing setup. Exact file/selected-unit scope is explicit
in the ledgers, rather than an assertion that all 39,568 file lines were a new whole-CTAP audit.

## Meaningful next checks, all unexecuted

Coordinator-assigned runs use current domain-build/domain-test skills and `dotnet toolchain.cs`; each
checkout has one serialized execution owner. Before running, set TMPDIR/TMP/TEMP and explicit output
under `/home/rupicola/Code/y/.core-docs-audit-runtime/core-001/BOUNDARY-Ctap2`. Preserve failed outputs and
actual nonzero executed counts. No command in this proposal is authorization to run it here.

1. **Existing managed boundary lanes:** select full class names for Ctap2SessionTests,
   Ctap2BackendLifecycleTests, IYubiKeyExtensionsTransportTests and both cross-layer suites in the Ctap2
   unit project. Retain their fake-native versus runtime distinctions. Review failure-path gate release
   before expanding stress loops; a watchdog kills a harness, not proof of cooperative SDK drain.
2. **Whole workflow versus exchange:** use a fake ICtap2Session barrier between credential begin/next,
   or GetInfo/key-agreement/token exchanges. Start an unrelated command or dispose between exchanges;
   record exact admission, resulting next-call refusal and cleanup. Separately hold terminal callback or
   post-exchange decoder work to observe whether session disposal waits. This tests real boundaries
   without inventing a public lease API or claiming intended semantics before maintainer selection.
3. **PIN profile:** keep valid published P-256 point coordinates and vary kty/alg/crv/missing/extra labels
   independently, so a point-import failure cannot mask label admission. Verify exact exception stage and
   protocol request transcript after selected fixes. Unicode vectors should distinguish four ASCII characters, the BMP
   multibyte 63-byte boundary, supplementary scalars, decomposed NFC equivalents, final NUL and malformed
   UTF-8; test chosen rejection/normalization policy rather than silently changing existing PIN bytes.
4. **Cleanup actual owners:** use existing response seam/retained Memory aliases to observe actual
   accumulation/report/APDU/typed arrays on callback/decode failure. Include a throwing resolution callback
   combined with a throwing logging provider for X020, restoring global logger state in finally. Core
   family ownership decides whether to add a narrow observer seam; allocating/clearing a separate test
   copy supplies no oracle for production storage.
5. **Independent defects:** give builder-retained copies, V1 full-MAC stack cleanup, decrypt failed output,
   post-ECDH dictionary allocation, blob probe plaintext, compression backing storage, saltData and PRF
   range temporaries their own chosen owner/observer paths. A single “all secrets cleared” assertion is
   unsupported. Provider fault injection and native erasure need separately bounded capabilities.
6. **Firmware:** script GetInfo 5.7.x, 5.8.0, missing and 0.0.1 against explicit USB/NFC connection classification
   and independent override values. Assert gate-before-override and effective session snapshot; check later
   GetInfo leaves it unchanged. Have B6 add true short/extended wire vectors for major-zero configuration
   instead of asserting ForceShortApdus only from option construction.
7. **Representation:** after the chosen profile work, assert both ARKG accessors' derived algorithm,
   generic unknown COSE preservation, strict peer labels and signature-DER boundaries separately.
   Keep malformed/trailing DER provider behavior, AAGUID default equality and big-endian conversion as
   distinct oracles. No new crypto computation is needed for pure label/encoding tests unless separately
   assigned.
8. **Physical/platform:** retain separate Linux/macOS/Windows, USB HID/USB SmartCard/NFC and plain/SCP
   cells. Read-only direct GetInfo/current version/reopen precedes authorized UP/UV/cancel/reset flows.
   Bind OS/runtime/native producer/device/firmware, admitted command, cancellation request, terminal result,
   actual drain/release and independent state recovery separately. PIV physical deferrals stay in force.

No proposed test uses a nonexistent SDK helper or a skipped placeholder. Successful existing metadata or
nonempty-signature tests are retained as such; they never replace actual cryptographic/RP/profile oracles.

## Clean sampling nominees

The exact IDs and hashes are emitted in `clean-nominees.json`. Eligible clean units should be sampled
only after edited overlaps are removed. These nominations supplement the coordinator's stratified 10% /
minimum 3 campaign rule; they are not completed sampling or independent review:

- Default SCP transport choice test plus SelectionProbeYubiKey: resolves transport before any native I/O.
- Ordinary backend success borrowed-view tests: status strip, underlying unchanged bytes and no backend INIT.
- SmartCard request callback failure method with complete recording prompt: no target transmit/resolution.
- Intent-scope method with full notification Create context: same context/operation and scope capture.
- CTAP README Getting started section: complete same-session example, second one-shot read distinction
  and construction-time firmware snapshot versus fresh GetInfo.
- Ctap2CredentialOptions preset XML/body: exact units and final awaited owner disposal.
- Stored-credential disposal assertion: actual captured aliases, unchanged source and independent public data.
- Distinct credential-parameter order assertion: real encoded array, with missing-field failure branch.

Where a candidate associates a file as an unchanged dependency, a localized unaffected unit may still be
eligible. Candidate association is not a finding that every line is defective; later edited overlap controls
the final denominator. Namespace/import/record boilerplate should not substitute for these method contracts.

## Windows delta and reopening map

Continue the fixed-pin review until the user announces readiness. At that handoff, the coordinator pins
the delivered commit and source/evidence delta. Compare Windows HID report/compatibility adapter,
connection slot/registry and portable PC/SC admission/teardown paths; then revisit CTAP creation,
keepalive-driven CANCEL, callback/terminal timing, owned versus borrowed disposal and actual caller-thread
responsiveness. Reopen Ctap2Session/ICtap2Session/factory XML, both guides, cross-layer test commentary,
TestStateExtensions Windows diagnostic and Core raw tiers/connection lifetime claims. Match managed
mechanism tests and a separately observed Windows native/device lane before strengthening claims.

B4/B6 and BOUNDARY-WebAuthn's exact mandatory questions are recorded in candidates.md. Their answers,
independent content/citation review and hardware qualification remain separate from this complete static
investigation. No other worker's unfinished checkout was inspected.
