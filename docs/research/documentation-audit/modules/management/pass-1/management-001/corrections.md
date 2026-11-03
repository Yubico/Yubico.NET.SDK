# Required review corrections

## RA-01 — prompt retention

2026-11-03: independent A review found the newly added direct-factory XML incorrectly inherited the
generic options description that a user-presence prompt is retained. Astra checked Management's sole
constructor and factory: neither forwards `UserPresencePrompt`, and the one-argument base constructor
supplies null. The reviewer finding is accepted.

Corrected only the worktree A XML to say Management does not retain, invoke or dispose that prompt;
corrected the matching edit-report mapping. Original review preserves the rejected claim and its
source/hash evidence. No constructor/runtime change. C editor notified to reconcile related prose with
this narrow module behavior. [Correction re-review](review-a-followup.md) subsequently passed: the other
13 changed-file hashes and 18 evidence dependencies were unchanged, and the sole file delta was RA-01.

## C module guide corrections

[Original module review](review-c-module.md) required C-MOD-R1..R5. The
[editor's correction record](correction-c-module.md) preserves verification and exact changes:
caller-owned SCP inputs, command credential/SCP distinction, conditional reboot, top-level versus
command-specific help, and unit-plus-integration selection. [Follow-up](review-c-module-followup.md)
passed all five; exactly four Markdown hashes changed, with thirteen other reviewed files unchanged.
All seventeen final reviewed module files are integrated; patch retained as `patches/edit-c-module.patch`.

## Shared-reference and compiler-discovered corrections

[Original shared review](review-c-shared.md) required CSH-R1..R3: actual session shape, default-SCP input
ownership in complete examples, and fakeable SCP initialization versus device interoperability.
[Correction record](correction-c-shared.md) preserves editor source checks.

Preliminary compiler output then found a real nonexistent `DeviceCapabilities.SecurityDomain` value in
the complete filtered shared example. [Compilation correction](correction-c-compilation.md) replaces it
with actual SmartCard/firmware criteria and accurate SCP03 task/prerequisites, preserving real reset,
list, assertions and disposal. A neighboring asynchronous callback now makes a real awaited metadata
read, rather than adding a meaningless await. [Shared correction re-review](review-c-shared-followup.md)
passed: two corrected hashes and fifteen unchanged files verified. All seventeen shared files integrated
at those exact hashes. Final compilation remains pending.

The xUnit/SkippableFact CS1701 reference diagnostics are a separate standalone-compiler context issue,
not evidence that the SDK runtime or every test example is defective. Diagnosis and final disposition
must be retained before claiming successful final example validation.
