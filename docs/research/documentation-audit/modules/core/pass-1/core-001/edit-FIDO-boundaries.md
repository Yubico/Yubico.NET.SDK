# Core Pass 1: FIDO boundary documentation edit

**Applied source-supported documentation corrections in 14 product paths, uncommitted.**
Work is isolated in `/home/rupicola/Code/y/yubikit-core-docs-edit-fido-boundaries`, at editing base
`da66f5d01ba594993974ddd26723e8c300c0a454`, using product source
`fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`. PRIMARY boundary/family/triage records were read-only.
No original investigation, coverage, triage, inherited state or frozen evidence was edited.

## Applied corrections

- Bound connection exclusion to overlapping known member interface IDs in this process, without
  changing one-session-per-connection or one-selected-transport/no-failover behavior.
- Put serialized, fully awaited whole-workflow/ceremony lifetime beside session/client ownership.
  Per-exchange drain does not join probing, token acquisition, continuation or extension output.
- Distinguished prompt ownership, CTAP terminal status, callback completion, native completion,
  typed decoding and RP acceptance; qualified the conditional throwing-logger cleanup gap.
- Clarified detected versus effective firmware, major-zero policy, early probes versus APDU options
  and SCP rebind/setup versus device/profile qualification.
- Qualified the Core-reader adapter's UTF-16 limits versus prompt code-point/UTF-8-byte/profile duties.
- Clarified HID/APDU response views and independent report/SCP/decoded storage lifetimes.
- Corrected the stale reset-target comment. Both flows reselect; only the menu reuses the old transport.
- Narrowed fake-connection intent, transport-routing and re-mint test commentary to actual assertions.

No executable tokens, imports, declarations, defaults, attributes, assertions or runtime-help strings
were changed. All 47 fenced example/command/diagram bodies in the affected Markdown files are identical
to the base. Complete methods/types, composable demos, supplied-context fragments, alternatives,
schematic declarations and historical migration fragments have separate honest classifications.
No fake helper or unscoped replacement example was introduced.

## Complete return packet

All links below are beneath `editing/FIDO-boundaries/`:

| Artifact | Contents |
| --- | --- |
| [product.patch](editing/FIDO-boundaries/product.patch) | Complete uncommitted product diff against the editing base |
| [path-decisions.json](editing/FIDO-boundaries/path-decisions.json) | All 310 boundary-packet paths, eligibility, local-ID associations, edit/retain/hold decisions, original path-specific observations, Git/base/pin/current hashes and changed ranges |
| [candidate-decisions.json](editing/FIDO-boundaries/candidate-decisions.json) | All 23 CTAP + 12 WebAuthn local concerns: selected documentation disposition and separate implementation/policy/unrun holds with exact original definitions/aliases/reopening text |
| [examples.json](editing/FIDO-boundaries/examples.json) | All 47 exact complete fence bodies, classifications, old/new positions, hashes, unchanged-body checks and no compilation/execution claims |
| [citations-and-handoff.md](editing/FIDO-boundaries/citations-and-handoff.md) | Source/affected-unit support, exact citation revision/status/section/actor/strength/applicability and precise external-owner referrals |
| [input-identities.json](editing/FIDO-boundaries/input-identities.json) | Read-only PRIMARY packet/follow-up/triage/source-register identities |
| [verification.json](editing/FIDO-boundaries/verification.json) | Static scope, comment-erased C# preservation, XML and fence checks; all unrun gates explicit |
| [link-check.json](editing/FIDO-boundaries/link-check.json) | Modified Markdown link file-target checks; target existence is not anchor validation |
| [record.py](editing/FIDO-boundaries/record.py) | Small static receipt generator; runs no SDK/compiler/native/device code |

Static checks passed for the final product diff: all changed paths belong to the exact boundary
disposition population and permitted `.cs`/`.md` module scope; comment-erased C# code is unchanged;
no new XML syntax error or missing linked file target was introduced; fence bodies are unchanged.
These checks do not establish independent semantic acceptance, compiler/API/example binding, runtime
cleanup, native release, standards conformance or physical interoperability.

## Preserved decisions and stopping boundary

WA-XREF-001 / CTAP-XREF-001 stay documentation-resolved. WA-IMP-010 descriptor recovery remains a
source-confirmed engineering gap. CTAP-DEC-002 remains a maintainer/coordinator choice of native output
actor and eventual RP serializer contract, with no field stripping authorized. No accepted-ID allocation,
runtime fix, test assertion change or new policy was made.

B4/B6 and WebAuthn pinned follow-ups are source-reconciled **with qualifications**; their old pending
source-return language is historical. Future Windows concurrency input stays UNREADY / UNREVIEWED.
Native producer, ISO/final-GP/vendor/profile/provider/physical qualifications and PIV physical deferrals
remain with their exact owners. All accurate candidate-associated paths are explicitly retained, not
silently treated as repaired or independently re-audited.

No build, SDK test, formatter, cryptographic experiment, native/device call, Git index/ref mutation,
commit, push or subagent ran. Independent review, coordinator validation and publication remain pending.
This editor stops at the complete uncommitted return; the coordinator finishes Core Pass 1, publishes
its reviewed/validated result, delivers deferred findings, then stops.
