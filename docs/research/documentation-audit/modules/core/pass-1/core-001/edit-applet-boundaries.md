# Applet/Core boundary documentation edit return

**The five-module documentation batch is ready for independent review, uncommitted.** It applies the
source-supported documentation facets from Management, SecurityDomain, Oath, YubiOtp and YubiHsm.
It does not publish Core Pass 1, change SDK behavior, close engineering/profile/device findings, or
start another implementation/planning pass. The coordinator retains publication and final handoff.

Baseline: isolated worktree `/home/rupicola/Code/y/yubikit-core-docs-edit-applet-boundaries`, HEAD
`da66f5d01ba594993974ddd26723e8c300c0a454`; product source
`fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`. Initial worktree status was clean. Primary reports were
read-only inputs, including Management's completed B4/B6 follow-ups and HSM's final retry-message
retraction. Original reports, ledgers, failed attempts, frozen histories and other workers' work remain intact.

## Deliverables

| Record | Content |
| --- | --- |
| [Complete product diff](editing/applet-boundaries/product.diff) | All uncommitted changes in the five source modules, suitable for review/application |
| [Path decisions](editing/applet-boundaries/path-decisions.json) | All 233 eligible packet/disposition paths: 37 edit, 196 retain; original context/IDs, exact before/after SHA-256, inclusive changed ranges and newline facts |
| [Candidate decisions and handoff](editing/applet-boundaries/decisions-and-referrals.md) | All 33 local groups, retained engineering/policy/qualification facets and actual caller-to-Core routing |
| [Example inventory](editing/applet-boundaries/examples.json) | Exact complete bodies, path/range/hash/classification for 107 fences/XML code blocks; compilation explicitly not run |
| [Citation notes](editing/applet-boundaries/citations.md) | Reused original editions, actor/phase/strength/applicability and remaining final/vendor/ISO gaps |
| [Outside-scope packet paths](editing/applet-boundaries/outside-scope-paths.json) | Exact disposition paths excluded by module/extension ownership, unchanged with original reasons |
| [Input identities](editing/applet-boundaries/input-identities.json) | Exact primary report/companion/triage hashes consumed by this edit |
| [Static checks](editing/applet-boundaries/static-checks.json), [collector](editing/applet-boundaries/collect-static.py) | Reproducible read-only source comparisons, XML/fence/link checks and preservation limits |
| [Owned output identities](editing/applet-boundaries/owned-output-identities.json) | New report/record/diff hashes; excludes the identity manifest itself |

Every eligible file has an explicit current edit/retain decision, with its original substantive
boundary observation attached. Retain is not silent acceptance of runtime debt. The candidate handoff
separates documentation status from held implementation, policy, evidence and qualification work.

## Changes by module

| Module | Edited / retained paths | Documentation result |
| --- | ---: | --- |
| Management | 10 / 39 | Captured firmware/default/qualifier provenance; memory-backed timeout aliasing; fixture/live/discovery distinctions; reset/rediscovery and full-task/native limits; precise stale-guard test oracle; SCP setup versus lock authorization |
| SecurityDomain | 6 / 25 | SCP11 effective-version prerequisite and complete typed creation method; retained parameter/contained-object aliases; response-owner and Le/fragmentation semantics; raw reset/reinitialization and authentication-phase limits |
| Oath | 8 / 42 | Session state and borrowed Salt lifetime; full-workflow resource scopes; UTF-16 UI units; actual test oracles; SmartCard-capable selection and reset-fragment prerequisites. Terminal reset and all fourteen historical IMP resolutions remain intact |
| YubiOtp | 7 / 49 | First/second report and raw/effective firmware provenance; polling versus native duration; whole-task and response-copy scope; exceptional one-shot HMAC ownership; supported-layout conditions; named modern/legacy/FIPS API guidance |
| YubiHsm | 6 / 41 | Reset reuses protocol without handshake/transaction guarantee; historical authentication flags; SELECT/fallback/override provenance; final-response cleanup versus other copies; bounded retry-exception type description |

Useful existing conditions and rationale are retained: Management lock/reset-family limitations;
Security Domain destructive attempt limits, certificate trust/disposal and selected exception wrapping;
OATH seed/Base32/calculation cleanup and terminal reset; OTP HMAC/flag/transport qualifications; HSM
48/56-byte uncertainty, diagnostic sample warnings and external HSM responsibilities.

## Verification and example status

The static collector compares all 233 before-file hashes with the final boundary ledgers. For all
27 edited C# paths, every line other than a full `//` or `///` comment line remains exact (ignoring
only the final newline). Thus this diff preserves imports, directives, declarations, attributes,
defaults, assertions, runtime strings and executable bodies. Full changed contexts were inspected;
the changes are ordinary comments/XML, not text inside executable literals. Some originally missing
terminal newlines were added by the patch tool and are recorded per path. No import ordering changed.

Edited XML blocks parse as XML fragments. Module Markdown fences balance and local link file targets exist;
this check does not resolve anchors or external URLs and is not a Markdown renderer/compiler gate.
`git diff --check` is a separate final whitespace check. The collector stores exact checks rather than
substituting hash agreement for independent semantic acceptance.

All 29 excluded packet paths match their original raw ledger bytes. Nineteen have checkout-versus-Git
BOM/newline differences, compared only with explicit normalization. The first added excluded-path
check wrongly compared CRLF project bytes directly with the LF Git blob; the failed output and correction
are preserved in [static-attempt-1.txt](editing/applet-boundaries/static-attempt-1.txt). No project file
was edited. The successor collector checks raw ledger equality separately from normalized Git equality.

The example inventory includes 83 C#-context bodies and 24 non-C# command/layout/informational blocks.
Only two bodies changed: OATH's getting-started selection/imports and the new Security Domain SCP11b
creation method. The latter is a complete composable method with typed borrowed prerequisites and
owned parameter/session scopes. Other examples remain full, unchanged bodies with explicit fragment,
test-member, prompt-type, implementation-excerpt or application-adapter classification. HSM adapter
interfaces are application-defined, not SDK connector helpers. Non-C# layout blocks and implementation
excerpts are not consumer-program compilation claims; no historical example is promoted to current API
authority. All meaningful C# bodies are **VALID-NOT-CHECKED in this edit**; prior runs retain their own
source/output identities rather than receiving fresh compilation credit.

No SDK build/test/formatter, native/device/cryptographic/HSM-connector operation, index/ref/commit/push
or subagent was run. No temporary host was created. Static records live only in this worktree's owned
run directory. Independent full-unit/content/citation review, coordinator example compilation and any
selected runtime qualification remain outstanding. Remove edited overlaps from later clean sampling.

## Stop and deferred handoff

The [handoff](editing/applet-boundaries/decisions-and-referrals.md) preserves all original source-run
IDs and exact re-review triggers. New Windows concurrency work remains future, unready and unreviewed;
the completed pinned B4 reconciliation is not acceptance of that delta. PIV physical deferrals stand.
Historical serial 9681620's cause remains unexplained. Serial 125's two `6A88` results were before
EXTERNAL AUTHENTICATE and RESET and provide no live secure-reset evidence. HSM connector/peer/host
authentication qualification and HSM-IMP-008 remain open.

Next owner: the coordinator assigns fresh independent review, reconciles the remaining owned edit
batches, publishes Core Pass 1 and the deferred-finding handoff, then stops as requested. This return
authorizes no additional implementation, device action or Pass-2 project.
