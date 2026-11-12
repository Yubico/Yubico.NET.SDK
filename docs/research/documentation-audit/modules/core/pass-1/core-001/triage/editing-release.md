# Core documentation editing release

## Coordinator decision

The eight completed family investigations and the full-family proposal map supply sufficient source
evidence to begin bounded Core documentation editing. **Accept the D (documentation-ready) facets
for fresh editing and independent review**, subject to the exact evidence and conditions in each
proposal. This release does not accept all 102 proposal groups as bugs or resolve their other facets.

Preserve every local/inherited identity and its source-run provenance. Editing reports map actual
changes back to those identities; final consolidated documentation/engineering IDs may be allocated
after the remaining boundary results are reconciled, without renumbering historical inputs.

## Scope and limits

- First editors own disjoint primary Core source/test documentation paths from packets A and C.
  They work in isolated home-backed worktrees. Shared guides, architecture and applet documents remain
  with their later explicitly assigned owners; current investigators continue reading the unchanged
  primary checkout source.
- Correct source-supported names, units, formats, ownership, lifetimes, prerequisites, exceptions,
  current mechanisms and assertion descriptions. Preserve useful rationale and complete examples.
- Runtime bodies, declarations, imports, directives, public API snapshots, existing assertions,
  executable help/output, project behavior and native ABI declarations remain unchanged.
- M/P facets remain policy/implementation handoffs. E facets block only the stronger statements they
  actually require; retain a truthful source-supported qualification instead of implying an experiment
  ran. Q gaps remain explicit. B facets await the named boundary when a caller-specific conclusion is
  required; they do not delay independently supported Core wording.
- Apply the B1 and C source-authority clarifications and exact revision/status/actor distinctions.
  Native producer, physical release, cross-process state, profile conformance and historical failures
  retain their recorded evidence limits.
- Every candidate-associated file receives an explicit edit/retain/hold decision. A retained path is
  not silently omitted. Record exact before/after hashes and complete example-body classifications.
- Fresh independent content/citation reviewers check full affected units before integration. Late
  boundary or Windows input reopens only its mapped dependencies; it receives a separate revision and
  review rather than silently changing the audit baseline.

## Additional Core C# ownership

The source-supported editing release now includes the remaining primary Core C# families:

- B1–B4: transport/lifetime editor in `/home/rupicola/Code/y/yubikit-core-docs-edit-transports`,
  owning only packet-primary `.cs` files beneath `src/Core/`, `editing/transports/` and
  `edit-transports.md`.
- B5–B6: session/protocol editor in `/home/rupicola/Code/y/yubikit-core-docs-edit-protocols`,
  owning only packet-primary `.cs` files beneath `src/Core/`, `editing/protocols/` and
  `edit-protocols.md`.

Both start at `da66f5d…` and follow all preservation, evidence and review conditions above. Shared
narratives and external/app-specific files require their own editing ownership.

## Shared narrative ownership

The SHARED editor owns active Markdown documentation in `/home/rupicola/Code/y/yubikit-core-docs-edit-guides`:
`src/Core/**/*.md` and shared-ledger Markdown paths in root `README.md`/`CLAUDE.md`, direct `docs/*.md`,
`docs/usage/` and `docs/architecture/`. Changes are limited to source-supported audit corrections.
Historical research/frozen records, applet module documents, C# files, configuration and assets remain
outside this ownership. The editor records its work in `editing/SHARED/` and `edit-SHARED.md`.

## Completed boundary editing ownership

The completed boundary reports also release source-supported documentation facets for two disjoint
applet editing assignments. Each editor owns only `.cs` comments/XML and `.md` documentation in its
modules that appear in the corresponding boundary candidate/disposition sets:

- Ctap2/WebAuthn: `/home/rupicola/Code/y/yubikit-core-docs-edit-fido-boundaries`, with records in
  `editing/FIDO-boundaries/` and `edit-FIDO-boundaries.md`.
- Management/SecurityDomain/Oath/YubiOtp/YubiHsm:
  `/home/rupicola/Code/y/yubikit-core-docs-edit-applet-boundaries`, with records in
  `editing/applet-boundaries/` and `edit-applet-boundaries.md`.

These editors use the latest completed reports in the primary checkout as read-only inputs. All
runtime, policy, unrun-evidence and qualification facets retain the limits above. OpenPGP/PIV and
external shared code require separate ownership after their applicable returns.

OpenPGP's completed boundary now releases its source-supported documentation facets to
`/home/rupicola/Code/y/yubikit-core-docs-edit-pgp-piv`, restricted to the packet's `.cs`/`.md` paths
under `src/OpenPgp/`, with records in `editing/PGP-boundary/` and `edit-PGP-boundary.md`. PIV remains
unreleased until its source return; the worktree name grants no additional file ownership.

PIV has subsequently returned its completed source packet. The same editor now also owns its exact
boundary `.cs`/`.md` paths under `src/Piv/`, with separate records in `editing/PIV-boundary/` and
`edit-PIV-boundary.md`. OpenPGP and PIV batches retain independent review identities and all physical
deferrals. No runtime or assertion change is authorized.

## External shared commentary ownership

The external editor in `/home/rupicola/Code/y/yubikit-core-docs-edit-external` owns active `.cs`/`.md`
paths in `inventory/shared-sections.json` outside Core, the nine applet modules and the existing SHARED
guide owner's root/docs scope. This includes eligible CLI, shared-test, verification and benchmark
commentary. C# changes remain comments/XML only. Historical research/frozen records, executable code,
assertions, runtime output, configuration and script files stay outside this assignment. Records belong
in `editing/external/` and `edit-external.md`; uncovered findings receive explicit referrals.

The external scope subsequently includes the 26 deduplicated `.cs`/`.md` verification/benchmark paths
owned by primary B2/B3/B4 packets, which were absent from the shared-section ledger. The same editor
owns these source-supported commentary corrections. Current inventory-listed script/project/config
files may receive documented D-facet corrections only in non-directive comments/XML comments; all
runtime values, code, imports, shebangs/directives, docstrings and output remain preserved. Historical
records and non-comment data are excluded. Successor records preserve the initial external batch.

## Stopping point

The user's clarification is authoritative: **finish and publish Core Pass 1, provide the deferred
finding handoff, then stop.** This cycle includes the agreed nine applet/Core boundary reviews. It
does not start a separate Pass-2 planning or implementation project. Existing qualified validation
tools should be reused; further tooling work is limited to a demonstrated blocker for the audit.
