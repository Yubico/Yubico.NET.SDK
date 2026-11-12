# Independent SHARED content and citation review

**MANDATORY CORRECTIONS — not PASS.** Six bounded corrections remain. These are factual,
resource-lifetime and example-disposition corrections, not requests for style churn, runtime repair,
new tests, validation policy or an additional applet audit.

Reviewed base `da66f5d01ba594993974ddd26723e8c300c0a454`, product source
`fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`, and product patch SHA-256
`3267ab5d547b281f19c28b3b709ef509a7cd54ee82439b6386b33a842030b937`.
Coordinates below refer to the unchanged reviewed guide bytes. Exact reviewed/accepted-for-content
hashes and the seven blocked product paths are in [review-identities.json](review-identities.json).
Only these two reviewer files were written. Product files and editor/coordinator records were read only.

## Mandatory corrections

### R1 — Repair retained current-API examples, not just their cleanup syntax

- **Location:** root `CLAUDE.md:500–512`, `Configure_FirmwareBelow400_UsesNeoMaxSize`;
  example-body record 13 (opening line 502); also `docs/TESTING.md:552–565`, body 95.
- **Candidates:** `CORE-B6-C001/FPT-067`, `CORE-C-C017/FPT-099`, with the complete-example
  accuracy facet of `CORE-B4-C010/FPT-057`.
- **Evidence:** pinned `src/Core/src/Protocols/SmartCard/Apdu/PcscProtocol.cs:44–54,58,162–195`
  takes the connection first, an optional remaining-instruction memory second, and the optional logger
  third. `new PcscProtocol(logger, connection)` is not the current constructor. The same positive
  example asserts 254, but `SmartCardMaxApduSizes.cs:17–34` defines the complete-command NEO ceiling
  as 1390, separately from the 255-byte short data chunk. Configure below 4.0 retains that initial ceiling.
  In the testing guide, `ConnectAsync<ISmartCardConnection>` gives the connection an interface type
  with no `IsConnected` property (`Transports/SmartCard/ISmartCardConnection.cs:19–72` and
  `Abstractions/IConnection.cs:26–29`). The later `Assert.True(connection.IsConnected)` is not made
  API-correct by labeling the producer an application-supplied fake.
- **Required:** keep the useful configuration test example, use the actual constructor and the
  correct quantity/expectation, and provide scoped fake/borrowed-connection context. Do not silently
  classify a current “GOOD” example as historical merely to avoid correcting it. Reconcile its body
  receipt; actual compilation remains a later gate. No executable SDK test is to be changed here.
  Correct the typed connection assertion/context in the testing guide too, preserving its failure-safe
  disposal and explicitly illustrative nature rather than inventing a Core liveness property.

### R2 — Remove the remaining atomic/immediate grouped-acquisition guarantee

- **Location:** `src/Core/CLAUDE.md:911`, Concurrency Model, “Discovery/connection ownership is atomic”
  and the following “throws … immediately” sentence.
- **Candidate:** `CORE-B1-C012/FPT-033`, also the route-specific release bounds in
  `CORE-B1-C005/FPT-026` and `CORE-B4-C001/FPT-048`.
- **Evidence:** `DeviceConnectionRegistry.cs:94–124,193–241` acquires sorted member claims one at a
  time, awaiting an active discovery lease before inspecting a later member. It rolls back prior
  acquisitions on failure. An earlier discovery-owned member can delay encountering a later live
  connection, and cancellation can win first. The corrected
  `docs/architecture/connection-ownership-and-contention.md:39–54` already explains this accurately.
- **Required:** align the contributor statement with incremental acquisition, before-native-open
  refusal and rollback; do not promise whole-set atomic preflight or invocation-time failure.
  Keep checked native release route-specific rather than implying the Windows/custom compatibility
  wrappers prove physical teardown. This is a prose correction, not authorization for stronger admission.

### R3 — Distinguish CTAP PIN-peer requirements from actual validation

- **Location:** `src/Core/CLAUDE.md:116–119`, specifically “PIN/UV profile checks remain strict.”
- **Candidates:** `CORE-C-C015/FPT-097`, boundary `CORE-CTAP-X006`, inherited
  `ctap2-001:CTAP-IMP-007`; the completed FIDO review's R1 is an additional dependency, not a new bug.
- **Evidence:** `src/Ctap2/src/Pin/PinUvAuthHelpers.cs:58–106` checks byte-array coordinates and
  32-byte lengths, forces P-256 import and delegates point acceptance to the provider. It does not
  validate peer kty/crv/alg labels or optional-label restrictions. Correct platform-generated labels
  at 109–117 do not validate the peer. Retained CTAP 2.1 Proposed Standard, 15 June 2021,
  21 June 2022 errata edition, §6.5.6 `ecdh`/`getPublicKey` and §6.5.7 inheritance
  (`investigation/B5/sources/ctap21.txt:8227–8230,8272–8292,8321–8325`) supplies profile requirements,
  not proof that the SDK enforces them.
- **Required:** preserve the open-catalog versus protocol-profile distinction, but disclose the
  implemented coordinate/provider checks and omitted required peer-label checks locally. This
  declarative sentence is not merely prescriptive contributor advice. Do not call the omission intended
  forward compatibility or implement validation. No shared-guide token-reuse claim was found that
  upgrades the FIDO review's fake SDK-reuse oracle to authenticator acceptance.

### R4 — Keep discovery's late work and logging-provider ownership consistent

- **Locations:** `docs/usage/device-discovery.md:45` (“no background cleanup”);
  `docs/LOGGING.md:170–186`, complete `DiscoverWithTemporaryLoggingAsync` example (body 53).
- **Candidates:** `CORE-A-C008/FPT-008`, `CORE-B1-C002/FPT-023`,
  `CORE-C-C008/FPT-090` and `CORE-B6-C014/FPT-080`.
- **Evidence:** one-shot exit retires admission, not already-dispatched enumeration/native work.
  `FindYubiKeys.cs:233–270` explicitly observes and logs abandoned enumeration later;
  `FindYubiKeys.cs:43` retains a static logger. `YubiKitLogging.cs:38–39,52–56` creates loggers from
  the current factory and restores only the factory reference, not those retained loggers.
  The guide's own lines 133–138 and logging lines 28–31 correctly preserve these distinctions.
- **Required:** describe the short-command benefit as no caller-owned monitor/start-stop lifetime,
  not absence of background cleanup. In the temporary-logging method, do not create/dispose the
  factory in a helper whose sole join is the bounded Find task. Borrow an application-owned factory
  kept alive through all participating SDK work (including late completion and later uses of captured
  loggers), or provide equally explicit safe surrounding ownership. Restoring the global factory
  cannot make a locally disposed provider safe. Retain the process-global/non-isolating warning.
  No new drain API, timeout or framework is requested.

### R5 — Preserve the raw command-byte condition on late CBOR success

- **Locations:** `docs/architecture/connection-ownership-and-contention.md:193–198` and the
  corresponding raw FIDO cancellation/command context in
  `docs/architecture/raw-access-tiers.md:93–110,321–327`.
- **Candidate:** `CORE-B5-C002/FPT-059`.
- **Evidence:** `RawCtapHidSession.cs:64–74` forwards the caller's byte unchanged.
  `FidoHidProtocol.cs:302–310,384–387` recognizes raced success only when the original command
  equals `CtapConstants.CtapHidCbor` (`0x10`) and the first result byte is zero. Terminal-command
  correlation masks the initialization bit (`FidoHidProtocol.cs:392–410`), so an encoded `0x90`
  request can correlate but fail that success classification and surface cancellation. The exact
  original candidate records this at `investigation/B5/candidates.md:46–77`.
- **Required:** qualify the local late-success statement by unencoded CBOR `0x10`, distinguish it
  from the accepted encoded-byte path's current classification gap, and retain the no-response
  CANCEL/original-response drain rule. Do not describe this as a new intended grammar or silently
  fix normalization. CTAP 2.3 Proposed Standard, 26 February 2026, §11.2.9.1.5 assigns the canceled
  request and CANCEL response duties to the authenticator; it does not supply this SDK result policy.

### R6 — Correct full-body versus mixed-history example dispositions

- **Locations:** `editing/SHARED/example-bodies.json`, body 65
  (`docs/MEMORY-MANAGEMENT.md:179–207`) and body 118
  (`docs/architecture/raw-access-tiers.md:352–360`); owning capture/receipt records on regeneration.
- **Candidates:** `CORE-C-C017/FPT-099`, `CORE-B6-C015/FPT-081`, `CORE-B5-C009/FPT-066`.
- **Evidence:** body 65 is a complete typed `static async Task SendAsync` with its raw session
  parameter, fully qualified SDK API names, owned request and finally scopes. The prose expressly calls
  it a complete method; it is not an omitted-context API fragment merely because its command bytes
  are illustrative. Body 118 contains both obsolete **Before** and current supported **After** code,
  but its reason excludes the entire body from current compilation.
- **Required:** classify the complete method as complete/compilable-unchecked while keeping its
  selected-applet/illustrative-command precondition. Identify the mixed before/after body's actual
  scope and keep the current After portion eligible for appropriate context/compilation checks;
  only the obsolete Before portion is historical. Keep all original complete bodies and their hashes,
  not just matching lines. No compiler execution is claimed or requested of this reviewer.

## Content and citation boundaries preserved

The 26 changed Markdown documents, affected surrounding flows, all 193 captured bodies, all 103 local
candidate mappings / 102 proposal groups, candidate-associated retains and the 635 outside-owner
referral rows were considered. The mechanical map preserves original IDs, facets and evidence-unit
references; an edited path does not close its held policy/implementation/qualification facets.
The seven retains do not acquire a new whole-file semantic certification merely by being listed.
No missing candidate-owned narrative path was identified beyond the corrections above.

The following source-backed corrections need no further preference-driven rewrite:

- Find versus owned monitoring, subscription replay/projection, metadata freshness/serial latch,
  scalar versus timeout-memory alias, serial-less metadata caching and metadata PID validity are
  distinguished. Complementary-partial grouping and serial-to-slot risk remain explicit. No first
  complete/serial-bearing scan or expiry-driven convergence is promised. Null topology resolution is
  attributed to the implemented SDK route, not universal OS API impossibility.
- Borrowed and hidden-owned connections, per-exchange versus complete workflow scopes, outer wait
  versus original task, native completion/reference/callback drain/checked release, and custom/Windows
  compatibility limitations remain distinct, subject to R2/R4/R5. Parameter source clones, retained
  key/certificate owners and shallow record clones are described without granting trust or applet authority.
- The completed PIV, OpenPGP and YubiHSM source returns agree with the shared presence descriptions:
  PIV parse/Completed before host RSA unpadding; OpenPGP ATT/UIF and 6600/Failed; HSM LIST advisory
  lookup and complete CALCULATE lifetime. These narrow checks do not accept those whole applets.
  The new PIV credential method borrows an already-open session and scopes its returned credential
  through VerifyPinAsync; its ASCII-digit preset is identified as local input policy.
- Response/TLV/request/intermediate/writer/provider/caller owners remain separate. The nested sensitive
  TLV cleanup example is preserved. AES-CBC key/IV copies precede the cleanup try; no allocation failure
  or all-copy/provider/native erasure is asserted. Static logging, SELECT bytes and exception provenance
  remain qualified, subject to the temporary-provider example in R4.
- RFC 5915 (June 2010, Informational) §3, retained lines 103–149, really distinguishes OPTIONAL schema
  from the conforming implementation's MUST include parameters. Inspected .NET v10.0.0 AsnWriter
  lines 79–99/185–209 separates retained writer storage from Encode's copy; the supplied-Q macOS
  route remains separate from absent-Q extraction. ARKG remains deliberately expired draft-09,
  not a final standard, injectivity proof or recovered immutable generator.
- Retained SCP11 1.3.0.13 **Public Review**, August 2023, §4.3/4.4 (derivative 1085–1120), separates
  receipt/setup from a/c's first correct-MAC post-establishment command and b's SD-only authentication.
  Retained SCP03 1.1.2 **Public Release**, March 2019, is not code-cited 1.1.2.6 **Public Review**.
  The inspected §6.2.6 absent-data rule applies with C-DECRYPTION plus C-MAC; R-MAC is optional/selected,
  not universal. The shared guides do not borrow the erroneous initial SCP11 response/request table
  numbers, universal exact SCP03 size, or conflated agreement/KDF width claims being corrected elsewhere.
  Final SCP11/vendor/ISO and empty/protected-error/continuation qualification gaps remain open.
- Apple retained IOReturn definitions distinguish NotPermitted 0xE00002E2 from NotOpen 0xE00002CD.
  udev(7) systemd 262 OWNER/GROUP/MODE and Ubuntu noble amd64 file lists support the scoped account
  permissions and libudev loader-name caveat. NativeShims published source/header correspondence is
  not deployed-producer/ABI/transmit proof. Historical AOT/rig evidence is not current all-path execution.
  Root/module instruction edits introduce no unrelated operational policy.

## Static identity checks and remaining gates

Independent read-only verification found all 33 base/source-pin/current path hashes equal to the
editor receipt, 26 edit / 7 retain decisions, all 193 full body texts/line ranges/SHA-256 identities
matching current Markdown, and the product patch byte-identical to the actual product diff.
`git diff --check` exited 0. No new fence/link breakage was identified; the editor's local-target
check explicitly excludes anchors/external URLs and retains pre-existing missing targets.
The semantic example exceptions are R1/R6, not hash mismatches.

Nineteen changed product-path hashes have no other mandatory content correction identified and are
marked `content-reviewed` in the identity record; seven are `correction-required`. This is not whole-batch
acceptance, compiler/analyzer/API binding, accepted finding allocation, or acceptance of unedited
runtime behavior. Editor manifests stay historical until their owner regenerates them for corrected bytes.

Compiler/example binding, final exact-source sampling/coordinator validation and publication remain
pending. FPX-01–04 and other held experiments remain **UNRUN**. C019 has no D release; contained-owner
referrals and original history remain preserved. Provider/native erasure, producer/ABI, physical
release and cross-process PIN-state dimensions are not closed. The future Windows concurrency delta
is **UNREADY / UNREVIEWED**; PIV physical touch/hotplug/touch-dependent cancellation remains **DEFERRED**.
Serial 9681620 remains unexplained; serial 125's two 6A88 setup failures preceded EXTERNAL AUTHENTICATE
and RESET. No new device, native, cryptographic/provider, build, test, formatter, compiler, Git index/ref,
commit/push or subagent action occurred.

After the narrow corrections and existing review/validation gates, finish and publish Core Pass 1,
deliver the precise deferred-owner handoff, then stop. This review starts no Pass-2 project.
