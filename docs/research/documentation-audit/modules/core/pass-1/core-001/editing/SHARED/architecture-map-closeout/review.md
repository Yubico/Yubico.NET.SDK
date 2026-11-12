# Independent focused review — architecture evidence-map closeout

**PASS for the exact authorized PF02 map/prose patch.** No mandatory correction remains in this delta.

Accepted patch SHA-256:
`27a8d85cda33ec95f3e309a8d1d5665ec9f3cd6403d3a75acdf501aebae31c33`.

| Accepted path | After SHA-256 |
| --- | --- |
| `docs/architecture/sdk-architecture-map.yml` | `5e1f2107e05ec7c9e13de1b922d9ea08f8cd0adebe0e34443f93aeaf44005b21` |
| `docs/architecture/sdk-architecture-diagrams.md` | `71c78c9d5dcfbc921873320552654d9449952d4b135573a25262683692061359` |

This packet is separate from the three-guide sampling-final-guide patch and its acceptance. The explicit
coordinator authorization was read at PRIMARY
`triage/final/reconciliation-3/coordinator-adjudication.md:24–32`: narrowly add real CTAP diagram-box
evidence entries and qualify the nearby established SCP/lifecycle prose, retaining useful anchors,
Mermaid bodies and rendered images. This is documentation evidence metadata, not permission for SDK
configuration, runtime/API, test or device changes. Only this review and its identity record were written.

## Five actual diagram-box keys — PASS

The complete actual map and diagram teaching context were read, not just checker schema/output.
Map contract `:24–28` distinguishes real type boxes from conceptual labels. The L2 CTAP box
(`sdk-architecture-diagrams.md:118–123,175–179`) and L3b participants/sequence (`:251–286`) name the
actual request helper and response decoders. Exactly these keys were added:

| Map diagram | Added type | Actual declaration |
| --- | --- | --- |
| L2-layered-stack | `Ctap2SessionRequestEncoding` | `src/Ctap2/src/Cbor/Ctap2SessionRequestEncoding.cs:30–37` — internal static class / BuildMakeCredentialRequest |
| L2-layered-stack | `MakeCredentialResponse` | `src/Ctap2/src/Credentials/MakeCredentialResponse.cs:49,135,151` — public sealed class / Decode overloads |
| L2-layered-stack | `GetAssertionResponse` | `src/Ctap2/src/Credentials/GetAssertionResponse.cs:49,152,164` — public sealed class / Decode overloads |
| L3b-fido2-ctap-sequence | `Ctap2SessionRequestEncoding` | Same actual request-helper declaration |
| L3b-fido2-ctap-sequence | `MakeCredentialResponse` | Same actual response declaration |

The helper actually composes through `CtapRequestBuilder` at `Ctap2SessionRequestEncoding.cs:46–79,82–113`.
The actual session caller at `Ctap2Session.cs:271–299,333–361` builds these requests, exchanges through
the backend and invokes the named response Decode methods. Existing `CtapRequestBuilder` and
`CtapResponseParser` entries remain useful supporting evidence and were not substituted for or removed
by the new direct box anchors. Removing exactly the five added mapping lines recovers the complete
before map bytes; no other map key/value, format version or project-graph entry changes.

## Bounded teaching prose — PASS

- **SCP, diagram guide lines 235–241:** `ScpProcessor.cs:58–63,79–87,121–131,133–166` separates
  secure wrapping from command-data encryption, bypasses wrapping when useScp is false and implements
  an empty-response success/warning exemption by INS 0x82, not handshake phase.
  `PcscProtocolScp.cs:63–109` requests secure ordinary traffic and plain SELECT.
  `ScpInitializer.cs:31–32,138–149` selects **0x33** and sends EXTERNAL AUTHENTICATE with
  useScp=true/encrypt=false. The note now describes these source conditions instead of encrypting/MACing
  every APDU. It calls out the local instruction-only exemption rather than endorsing it as a new policy.
- The selected 0x33 names agree with retained **SCP03 1.1.2 Public Release, March 2019**,
  §7.1.2.3/Table 7-6 (`scp03-1.1.2.txt:1490–1504`). Other levels are explicitly possible; selected R-MAC
  and C-DECRYPTION/C-MAC applicability are not generalized. The accepted protocol revision-2 review was
  read as a scoped dependency, not used to turn review/final/vendor/ISO gaps into conformance. The
  reviewed raw-SCP guide link retains wrapping, response, recovery and profile qualifications.
- **Lifecycle, lines 338–348:** the note now describes attempted listener cleanup and subsequent stages
  conditional on preceding calls returning. This matches the already-checked complete
  `HotplugSignals`, `YubiKeyMonitor` and `DeviceChangeFeed` implementation flow: logging can interrupt
  signals teardown before cancellation/join/retirement/feed completion; reaching completion allows healthy
  uncanceled tails to end normally but does not erase existing terminal/overflow outcomes. The detailed
  lifecycle guide is linked. No blanket cleanup, total-duration or native-release proof is added.

Diagrams remain schematic main paths, not exhaustive fault/cleanup or whole-protocol attestations.

## Fresh static checks and preservation

- Both before snapshots match current PRIMARY accepted bytes and recorded hashes; after snapshots
  match actual product files. Reconstructing both unified diffs gives the exact accepted patch.
- All **80** prior artifacts match their preservation hashes, including the separate three-guide editor
  packet. Other **25** predecessor guide paths remain byte-identical to this packet's baseline. The
  map's original bytes and all prior review/patch identities remain preserved history.
- All **8** fences remain byte-identical: **7 Mermaid** and **1 command fence**. Independent extraction
  verifies complete text, language, coordinates and SHA-256 against every before/after receipt.
- All **15 image/manifest files** match their recorded hashes: 7 SVG, 7 PNG and the image manifest.
  No renderer, npm, image regeneration or schema migration ran.
- The existing map checker was read in full and confirmed read-only before independent execution:
  `bash scripts/architecture/check-architecture-map.sh` exits **0**, reporting
  `architecture map OK: 6 diagram entries, all evidence + project edges resolve; coverage matches 6 diagram heading level(s).`
  This validates bounded paths/declarations/project edges/heading coverage, not semantic protocol
  correctness or rendered image freshness. Source/prose decisions above were checked separately.
- All **10** packet source-evidence entries match actual complete source/captured review bytes.
  `git diff --check` and read-only focused reverse-application check each exit **0**; the index is unchanged.

The exact authorization, path, source and artifact identities are bound by
[review-identities.json](review-identities.json). This is bounded metadata/prose acceptance, not clean
sampling credit, whole-architecture re-audit, SDK compilation/testing, rendering, cryptographic/provider,
native/device or full standards/profile qualification. No product/prior-record/primary write, operational
configuration change, SDK build/test/formatter, Git mutation or subagent occurred.

Keep the two packet verdicts and predecessor histories separate. After the coordinator's existing final
reconciliation/validation gates, finish Core Pass 1 publication and the precise deferred-owner handoff,
then stop. Original SCP/provider/native/Windows/PIV deferrals remain intact.
