# PF02 bounded architecture evidence-map closeout

This is the coordinator-authorized diagram-evidence metadata/prose correction, not a new architecture
project or runtime/config/API change. Only `docs/architecture/sdk-architecture-map.yml` and bounded prose
in `docs/architecture/sdk-architecture-diagrams.md` change. All original map bytes are retained in the
complete before snapshot; the map edit consists solely of five needed symbol entries. Existing builder/
parser anchors remain. No Mermaid body, rendered image, image manifest, or renderer changes.

Both before-files were checked against primary current bytes. The separately completed three-guide
sampling patch and all prior accepted/editor/review/sampling records remain byte-exact. Fresh focused
independent review is required; this return does not accept its own content or supply compilation,
rendering, protocol, platform or device qualification.

## Exact source checks and corrections

### PF02 — real CTAP diagram-box declarations

The full 175-line map and 456-line current diagram guide were read. The map contract at lines 24–28
says real type boxes SHOULD have symbol entries; conceptual boxes stay schematic. Actual declarations:

| Diagram | Added key | Declaring source |
| --- | --- | --- |
| L2-layered-stack | Ctap2SessionRequestEncoding | src/Ctap2/src/Cbor/Ctap2SessionRequestEncoding.cs, internal static class |
| L2-layered-stack | MakeCredentialResponse | src/Ctap2/src/Credentials/MakeCredentialResponse.cs, public sealed class |
| L2-layered-stack | GetAssertionResponse | src/Ctap2/src/Credentials/GetAssertionResponse.cs, public sealed class |
| L3b-fido2-ctap-sequence | Ctap2SessionRequestEncoding | src/Ctap2/src/Cbor/Ctap2SessionRequestEncoding.cs |
| L3b-fido2-ctap-sequence | MakeCredentialResponse | src/Ctap2/src/Credentials/MakeCredentialResponse.cs |

The request helper's BuildMakeCredentialRequest and response Decode declarations agree with the shown
request/credential paths. CtapRequestBuilder/CtapResponseParser remain useful supporting anchors and
are preserved, not substituted for the actual boxes. The schema, project graph, other diagram entries
and existing symbol values remain unchanged.

### L3 teaching — actual protected traffic, not every APDU

Complete ScpProcessor, PcscProtocolScp and ScpInitializer paths were read. ScpProcessor passes through
when useScp=false; command encryption is separately controlled by encrypt. PcscProtocolScp's ordinary
transmit requests secure wrapping, while SelectAsync passes false. SCP03 setup selects security level
0x33 and sends EXTERNAL AUTHENTICATE with useScp=true/encrypt=false. The empty-response exception is
currently keyed to INS 0x82, not proof of handshake phase. These are source facts, not an authorized
new exemption or general conformance promise.

The accepted B5/B6 revision-2 independent review was read, including R3's selected-R-MAC and
C-DECRYPTION/C-MAC applicability and retained instruction-only exemption/profile holds. The teaching
note now distinguishes ordinary protected command traffic, selected SCP03 level, MAC-only setup,
plain SELECT and the current response exemption, and links the reviewed Core raw-SCP guide for the
remaining wrapping/recovery/profile limits. No unseen final/vendor clause is asserted.

### L4 teaching — same accepted A003 conditions

The discovery paragraph now describes listener cleanup attempts and later disposal stages conditional
on preceding calls returning. A logger failure during listener cleanup can prevent cancellation/join,
retirement or feed completion. Healthy uncanceled tails can end normally when completion is reached;
existing fault/overflow errors remain observable. The detailed current lifecycle guide is linked.
Diagrams remain schematic rather than claims of exhaustive cleanup or fault-path behavior.

## Transfer and static verification

`product.patch` is the focused before-to-after map/prose diff. `before/*`, `after/*`, current path hashes,
exact declaration/source/review identities, complete unchanged fence bodies, image hashes and prior
record preservation receipts accompany it. The existing map checker was inspected as read-only before
execution; no SDK toolchain, npm or renderer is invoked. Its result checks paths/declarations/project
edges and heading coverage, not semantic protocol correctness or image regeneration.

No original editor/review record or other product path is overwritten. No C#, operational config,
runtime/API, primary checkout, test assertion, diagram body or image asset is changed. No SDK build/
test/formatter/compiler/native/crypto/device operation, Git index/ref/commit/push or subagent runs.
Original deferred findings, SCP edition/final/vendor qualifications and Windows/PIV physical deferrals
remain. Finish focused review, Core Pass-1 publication and the deferred handoff, then stop.
