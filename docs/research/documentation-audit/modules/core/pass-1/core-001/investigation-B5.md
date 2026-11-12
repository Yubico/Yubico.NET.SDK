# B5 session/framing investigation

**Outcome:** complete source/assertion reading and disposition of **43 primary files / 8,830 lines /
893 primary semantic units**, plus the packet's actual **39 assigned shared units**. Nine local candidate
groups are ready for coordinator triage and fresh independent review. This is a **B6/boundary handoff**,
not whole-Core closeout, accepted findings, an editing authorization or platform qualification.

Source remains `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`. Initial metadata HEAD was
`01a114d4e251e0f52b92f73393fbe1dca5d5c24e`; the coordinator advanced records to
`c7ff868c6f1b89e2d72eaba526bf98d9c28adb05` during investigation, followed by C's records-only checkpoint
`bb94f0884f63f260156227957d4cc112d984e300` at final ledger verification. All 43 primary paths and 16 additional
shared paths match the source pin after explicit UTF-8 BOM/CRLF normalization; every selected raw file
and character-range hash matches inventory. Exact raw/pinned Git/LF hashes are retained. The metadata
advance changed no B5 product baseline.

## Record map

| Record | Purpose |
| --- | --- |
| [Unit dispositions](investigation/B5/unit-dispositions.json) | All 932 exact original IDs/ranges/hashes, read context, clean/candidate/exclusion and pending review/validation |
| [File identities](investigation/B5/file-identities.json) | All 59 primary/shared source paths, raw/Git/LF hashes and precise read scope |
| [File dispositions](investigation/B5/file-dispositions.json) | All 43 complete primary files, distinct implementation/assertion/helper observations and unit IDs |
| [Authored semantic notes](investigation/B5/semantic-notes.json) | Complete-file context used by the mechanical join; hashes supply identity, not semantic reading |
| [Candidates](investigation/B5/candidates.md) | CORE-B5-C001–C009, documentation/runtime/decision/qualification distinctions and precise reopening conditions |
| [Candidate index](investigation/B5/candidate-index.json) | Exact affected original unit IDs and every candidate-associated unchanged path awaiting editor/reviewer disposition |
| [Handoff and verification](investigation/B5/handoff-and-verification.md) | Creation/disposal/resource trace, complete public async lifetime table, nine actual factory seeds, uncompiled typed examples and bounded unrun checks |
| [Inherited dispositions](investigation/B5/inherited-dispositions.json) | All 15 assigned source-run keys in five nonadditive groups; original registry states/conclusions preserved |
| [Source register](investigation/B5/sources/register.md) | Original CTAP editions/sections/actors/strength, pinned vendor OTP bytes and framework API evidence/gaps |
| [Source captures](investigation/B5/sources/captures.json) / [follow-up](investigation/B5/sources/captures-followup.json) | Exact URL/access/status/hash/bytes, including immutable vendor pin verification |
| [Reused identities](investigation/B5/sources/reuse-identities.json) | Existing B1 Task/.NET and shared Python/registry/source authority hashes, no duplicate vendor checkout |
| [Coverage](investigation/B5/coverage-summary.json) / [record verification](investigation/B5/preservation-verification.json) | Reconciled counts and identities; zero new SDK/native/device executions |
| [Separate static verification receipt](investigation/B5/verification-receipt.json) | Separate checker verifies IDs, raw/range/Git identities, source captures, candidate paths, inherited keys and authored-record links/fences; not independent content review |
| [Clean nominees](investigation/B5/clean-nominees.json) | Thirteen substantive method/test units for later independent sampling; not completed sampling |

Final 932-unit counts: **716 read-clean, 212 read-candidate, four read-exclusion, zero unread**. Primary is 694
clean/199 candidate, with no exclusions. Shared is 22 clean/13 candidate/four exclusions. Exclusions were
read completely: dated raw-access decisions, the earlier architecture internalization proposal and
August2026 AOT preamble retain historical status; the forward-compat raw CTAP model migration row
requires its own Ctap2 response/model boundary disposition rather than false B5 framing acceptance.
Related helper/context units can remain clean while their containing file has a localized candidate;
the coordinator must remove later edited overlaps from the clean denominator.

B5 read root AGENTS/CLAUDE, Core README/CLAUDE and nested test instructions, applicable PublicApi and
all nine module instructions for narrow caller reads, campaign workflow/style, approved plan/planning
review, inventory/dispatch/source ownership, reverse inherited routing/registry, and completed A/B1/B2/B3/C
reports. Actual current eight applet factory/initializer paths plus indirect WebAuthn creation were read
narrowly, without accepting or dispositioning their full packets. B6 APDU/SCP response/guard callers and
C's presence implementation were read only to trace B5 contracts; their original unit owners remain.

Only `investigation/B5/*` and this report were written by B5. No product edits, changed assertions,
central inventory/coverage/status/source-register mutation, formatter, build/test/native/device command,
index/ref write, commit, push or subagent ran. Python performed source capture/static record joins/hash
checks; Git performed read-only identity/source/history inspection. Other workers' checkout work and
original/frozen artifacts were left untouched. All source-only qualifications below remain unexecuted.

## Current supported journey

Prefer an applet session for command/profile semantics. A device extension selects exactly one exposed
transport, opens it, awaits the direct factory, then transfers hidden connection ownership. The direct
factory borrows its already-open connection and validates any preferred type as an assertion. It binds
after constructor success but before wire work. `PrepareProtocolAsync` registers protocol disposal,
rejects SCP on HID before I/O, and eagerly initializes applet FIDO channels. Applet selection/version
discovery precede Configure/SCP. Only full callback completion publishes readiness; OpenPGP cache and
PIV management-type discovery are examples of work after protocol configuration. Raw FIDO/OTP create
without wire I/O and initialize lazily on the first exchange.

Protocol auth and applet auth are distinct. A successful SCP wrapper makes inherited IsAuthenticated
true but confers no PIN/UV/management authorization; SCP11b authenticates the device, not host management
access. FirmwareVersion is effective configured policy, not a universal hardware fact. Raw HID currently
keeps inherited 0.0.0 even after its protocol discovers firmware; Security Domain can assume 5.3.0; applet
raw 0.0.1 and Management 5.8 metadata retain separate provenance. Major-zero acceptance is optimistic SDK
compatibility policy. Overrides do not skip required probes, nor uniformly control every applet gate.

Session disposal immediately clears readiness/auth and refuses future guarded session calls. It drains
the protocol's admitted logical exchange, then detaches and disposes only an owned connection. Repeated
sync/async callers share one outcome. That drain is not a lease for an entire multi-exchange applet
workflow, post-exchange parsing/cache changes or detached callback work. Keep complete public workflows
awaited and dispose non-reentrantly. A borrowed connection's detached slot permits sequential binding;
its actual protocol/device recovery still depends on the exchange and platform outcome.

Raw sessions add framing/guarding, not applet semantics. RawCtap sends ordinary CTAPHID request/response
commands; CANCEL is a special no-response control command. Core's caller-token CANCEL happens only after
a keepalive, then it drains the original response; no further report can mean an indefinitely pending
operation. Native cancellation, terminal wake, callback failure and device CANCEL are distinct. RawOtp
builds outbound CRC and returns command-specific inbound bytes for the caller/app backend to validate.
One attempted frame followed by failure gets one abort; a failed abort/completion reset latches refusal.
RawSmartCard uses caller APDU/AID, adds no command-specific scaffolding and returns an ApduResponse
owner. Inspect status before Clear because Clear also invalidates status/data aliases.

## Candidate priorities

| Local group | Documentation outcome / separate implementation or decision |
| --- | --- |
| C001 | Qualify raw readiness/firmware, assumed/overridden/detected sources, post-disposal queries and retained service/parameter exceptions. Decide raw HID version propagation separately. |
| C002 | Explain between-read CANCEL/original terminal response and request-response command grammar; a vendor-command 2D fake is not a normative cancel acknowledgement. Encoded CBOR 90 late-success normalization is a source-visible decision/runtime candidate. |
| C003 | Scope control/INIT validation and failure recovery: reserved assigned CIDs accepted, malformed control lengths tolerated, FIDO partial failure releases guard without recovery latch. No blanket malformed/reuse guarantee. |
| C004 | Scope physical claim assertion to managed known-member opens/platform owners; document actual input borrow/copy stage and result ownership, including outer WaitAsync versus original task. |
| C005 | Separate cleared outgoing/accumulation arrays from uncleared sensitive incoming report arrays. Define report ownership and retain actual-array zeroing oracles; public metadata is not automatically secret. |
| C006 | State per-exchange versus whole-public-workflow lifetime and non-reentrant sync/async disposal. OTP prompt self-disposal can self-wait; selected policy remains maintainer work. |
| C007 | Explain current OTP status/control/frame vocabulary and local poll deadlines, broad NEO refresh catch and busy-loop cadence. Vendor/Python policies are comparators, not required timing. |
| C008 | Narrow test explanations to actual assertions; held lazy INIT is not held application continuation, same-helper CRC is not independent KAT, and several failed assertions skip fake release/drain. Test harness/stronger oracles require separate scope. |
| C009 | Correct extension-only public creation wording, current event vocabulary and historical commentary; factory reflection grammar and unnamed Rust/Python sources have exact limits. |

Every candidate has precise unchanged paths and a reopening trigger in its record. Source-confirmed
observations, desired behavior, unresolved policy and platform qualification remain separate. No accepted
`CORE-001`, `CORE-DOC-*` or `CORE-IMP-*` ID was allocated.

## Inherited and cross-packet preservation

The original registry stays at 100 provenance rows/70 nonadditive groups. B5's 15 exact keys are listed in
its inherited JSON; the separate 21 PGP documentation labels are not new Core findings. Consideration 3's
FIDO local Configure/awaited Prepare INIT resolution is confirmed for that exact source scope. OATH
IMP-010 terminal-reset mechanics remain resolved-source with a secure physical gap; DEC-007's 5.6.3 policy
has C's pinned source/history but no new earliest-hardware explanation. SD-DEP-003 retains omitted
reset config/version despite centralized readiness. The other PIV/PGP/HSM/OTP dependencies receive the
current borrowed/owned, exchange/prompt and version trace, not automatic full applet closure.

A-C003 timeout memory alias/equality/hash stability, A-C017 metadata-cache ProductId qualification and
A-C021 successful serial-less cache policy remain A's findings. ProductId is USB product identity, not
an OS process ID. No new stable/fresh metadata assertion is inferred from native owner release. C owns
credential/presence type/implementation/logging contracts; B5 checks actual notification passdown and
lower-layer request versus applet status/CRC terminal classification. Shared version cache/key-size/COSE
catalog work stays with C and applet profiles, not transport framing.

B1 portable PC/SC, B2 Linux and B3 macOS evidence retain their separate admitted/terminal/drained/released
states; B4 pinned Windows/support source remains active. A future Windows CCID/HID delta is inspected
only after the user explicitly supplies readiness, then affected B5 admission/disposal/raw-tier prose
must be reopened. No unfinished branch/worktree was inspected. Historical serial 9681620 Linux HID failure
remains unexplained; fake Windows/portable links and modern Linux owners do not establish its cause.
Historical retained 4,442 integration units plus 197 overlapping resilience executions/native-only Linux
evidence are inherited, not fresh B5 executions or physical proof. PIV physical deferrals remain in force.

## Next boundary

[Handoff and verification](investigation/B5/handoff-and-verification.md) supplies B6 the exact prepare→
select/probe→configure/auth→ready and failure-dispose ownership boundary. B6 still owns full APDU/SCP
wire/crypto/profile/error-response cleanup review. All nine boundary owners have distinct actual factory
seeds and inherited reopening paths; none is silently accepted as a whole applet audit here.

Coordinator triage, fresh editors/independent reviewers, stratified sampling, meaningful consumer
compilation and selected managed/native/device qualification remain later gates. The finite unrun
checks and complete typed **UNCOMPILED** examples are ready for those assignments. B5 requests no new
execution, physical action or phase transition.
