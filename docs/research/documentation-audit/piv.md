# PIV documentation handoff

Pass1 is underway as [piv-001](modules/piv/pass-1/piv-001/plan.md), based on
`727fe43afc430c5d8364d868a6436de50626e476` on `yubikit-docs-audit`.
PR745 merged into `yubikit` atb1362127 before this cycle; [integration evidence](integrations/piv-pr745-20261105/integration.md)
records preservation, reviewed conflict resolutions and validation. Both parent histories and remote
baseline were verified before starting this whole-module audit.

See [current status](modules/piv/pass-1/piv-001/status.md). Fresh Sol6.1 sessions investigate, edit and
independently review; Astra coordinates, samples and validates. Approved affirmative task-oriented
style, full module coverage, original specification/firmware provenance, complete examples and scoped
hardware outcomes follow the established campaign workflow. Runtime fixes remain deferred to Pass2;
Core dependencies are retained for the final Core cycle. User authorizes dedicated-key test/configuration
work; physical touch and hotplug timing remain coordinated.

## Prepublication checkpoint

Content, execution gates and [fresh final evidence review](modules/piv/pass-1/piv-001/review-closeout.md)
are complete. The final review passed with no required corrections; archive freeze and verified
publication remain. Frozen run status/closeout files preserve their reviewed prepublication checkpoint;
this external handoff records subsequent publication without rewriting those identities.
Whole-module scope reconciles 123 paths, 23 public types, 370 API entries and 42 operations. Independent
reviews accepted **59 authored paths**, with **50 C# paths** preserving executable tokens/directives/
imports and all 20 API snapshots unchanged. Expanded clean sampling passed **64/385 regions**.

Clean repository gates passed **4,124 unit tests**, including 289 PIV, five existing skips and zero
compiler/analyzer warnings or failures. **45 typed example groups / 47 selected bodies** and the full
**36-source PivTool** compiled with actual Csc/project-graph evidence and final live checks.

**86/89 canonical integration methods passed.** The user deferred three physical-lane methods and
the supplemental touch-dependent cancellation/recovery case. Six unattended supplements produced
four passes and two preserved intended-contract failures in actual PivTool signing, confirming
**PIVIMP-005**. Direct SDK controls passed; no runtime fix was made. Both dedicated keys' final captured
public states match their separate baselines, with existing F9 identities preserved and factory
authentication independently verified. The original test allowlist is restored byte-for-byte.

[Findings](modules/piv/pass-1/piv-001/findings.md) retain ten engineering/qualification groups and
three Core questions. [Closeout](modules/piv/pass-1/piv-001/closeout.md),
[hardware results](modules/piv/pass-1/piv-001/hardware/results.md) and
[example validation](modules/piv/pass-1/piv-001/snippet-validation.md) preserve exact scopes and limits.
All source, tool, example and hardware failures remain attached to their original inputs and results.
