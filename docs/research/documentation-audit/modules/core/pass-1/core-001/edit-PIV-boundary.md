# PIV boundary documentation edit return

**PIV documentation edits are prepared, uncommitted, as a separate patch from the completed
OpenPGP batch** in `/home/rupicola/Code/y/yubikit-core-docs-edit-pgp-piv`. They apply the released
source-supported facets of CORE-PIV-X001–X006. Independent full-unit/citation review and the
coordinator's existing final validation/publication gates remain pending.

Product comparison uses pinned Git source `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`; this worktree
stays at `da66f5d01ba594993974ddd26723e8c300c0a454`. Primary final boundary records were read-only
inputs. Current primary Core product bytes were not used as the baseline after integration began.

## Patch and records

All new PIV editing records are in [editing/PIV-boundary](editing/PIV-boundary/).

| Record | Purpose |
| --- | --- |
| [Complete product patch](editing/PIV-boundary/product.patch) | Entire uncommitted PIV product documentation patch; independent of OpenPGP, apply from repository root |
| [Changed files](editing/PIV-boundary/changed-files.json) | Thirteen paths, exact before/after SHA-256, changed ranges, EOF-newline state and candidate associations |
| [Path decisions](editing/PIV-boundary/path-decisions.json) | All 121 candidate/disposition paths, eligibility, explicit edit/retain reasons and pinned/local identities |
| [Decisions](editing/PIV-boundary/decisions.md) | X001–X006 applied facets, retained accurate text, engineering/policy/qualification holds and stopping point |
| [Outside-owner referrals](editing/PIV-boundary/outside-owner-handoffs.json) | All 31 external dependencies with exact path, original primary owner, local candidates and precise reason/trigger |
| [Example inventory](editing/PIV-boundary/example-inventory.json) | Nineteen Markdown bodies and 23 XML fragments, exact before/after ranges/hashes, resource context and no fresh execution credit |
| [Citation notes](editing/PIV-boundary/citation-notes.md) | Exact retained authority/status/section/actor/strength/application and bounded reuse/qualification |
| [Preserved provenance](editing/PIV-boundary/preserved-provenance.json) | Eleven routed keys and PIVIMP-001–010 unchanged identities/status scopes |
| [Input identities](editing/PIV-boundary/input-identities.json) | Consumed primary final-record hashes; originals/frozen archives untouched |
| [Static verification](editing/PIV-boundary/static-verification.json) / [links](editing/PIV-boundary/local-links.json) | Comment-only preservation, XML, exact examples, local link targets and patch whitespace; no SDK execution |

## Applied corrections and remaining gates

The patch names direct versus hidden connection ownership, factory binding/readiness and SCP/version
provenance; separates card PIN state, management authentication and protocol authentication; explains
complete-public-task input/key/prompt lifetimes beyond per-exchange drain; and states admitted-traffic
cancellation and retained-protocol reset limits. Response/TLV/fragment/provider owners remain distinct.

Local test/internal corrections remove the physical >=5.8 inference from major-zero fixtures, describe
actual attestation Le, use raw X25519-result terminology, distinguish Data-only assertions from full
response Clear, and narrow reset/concurrency/hotplug comments to their actual oracles. Runtime-help
strings, method names, assertions, imports/directives, signatures/defaults/attributes, configuration and
API snapshots are unchanged. No implementation or policy decision was made.

PIVIMP-005 remains confirmed/unfixed for the historical tool RSA/Ed25519 framing failure; other original
PIVIMP states and exact earlier resolved scopes remain intact. Serial 125's 6A88 results were before
EXTERNAL AUTHENTICATE/RESET, not secure-reset evidence. F9/private/attestation material and protected
PRINTED restoration scope were untouched. Physical PIN/power, cross-process, touch, hotplug and
touch-dependent cancellation/recovery qualification remains deferred. The user's unready Windows
concurrency work requires its own future pin/source/evidence handoff.

Fresh static checks passed: thirteen PIV documentation files, 121 path decisions, 31 referrals, all
nineteen Markdown and 23 XML example bodies unchanged, 124 XML blocks well formed, twenty local targets
resolving and `git diff --check` clean. The check also verified that the previously delivered OpenPGP
product patch and all seventeen product after-hashes are unchanged. EOF newlines added by the comment
edit tool are recorded separately from preserved executable lines.

No builds, SDK tests, formatter, cryptographic/provider/native/device operations, Git index/ref/commit/
push, subagents, primary writes or other-worktree writes ran. Static verification supplies neither
compiler binding nor independent semantic/citation acceptance. The 513-unit investigation remains the
completed investigator's evidence, not 513 new editor investigation credits.

Next: coordinator review/integration through the existing Core Pass-1 gates, publication and deferred
finding handoff, then stop. This return starts no broader planning, tooling project, implementation or
physical lane.
