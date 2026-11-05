# PIV annotation-example correction review

**PASS — accept the corrected complete example and all 57 current post-format product identities in
`evidence/review-accepted-v2.json`.** No further content correction or optional rewrite is requested.
This resolves a compiled-example documentation defect under PIV-C009/C010, not a new runtime finding.

Independent reviewer: GPT-6.1 Sol, November 5, 2026. Baseline:
`727fe43afc430c5d8364d868a6436de50626e476`. The corrected `src/Piv/CLAUDE.md` SHA-256 is
`f97b64c75b941c90e339f9b9b9e6b51a269f4bafeaae3d58e3a5bc2d22578d65`.

## Complete-example meaning and actual host

Read the complete test-infrastructure example and surrounding maintainer guidance, `PinMetadata`
declaration, concrete `GetPinMetadataAsync` gate, the real annotation host and its project references,
and the retained failed compiler output. `PinMetadata` is a readonly record struct with integer
`TotalRetries` and `RetriesRemaining`; a non-null assertion on that value cannot establish useful
behavior. The retained xUnit2002 error in `validation/examples-v2-annotations-build.log` identifies
that exact documentation assertion in the extracted `Examples.cs`, not an SDK runtime failure.

The replacement `Assert.InRange(metadata.RetriesRemaining, 0, metadata.TotalRetries)` checks a useful
coherence condition: remaining attempts lie between zero and the reported configured limit,
inclusive. It accommodates blocked and non-default/configured retry states without assuming the
factory value three. It establishes this relationship for the observed metadata, not independent
proof of the configured limit, credential validity or device-wide conformance.

The example's `[WithYubiKey(Capability = DeviceCapabilities.Piv, MinFirmware = "5.3.0")]` matches
`Features.Metadata` and the session's enforced metadata prerequisite. The actual Tests.Shared
attribute exposes both named properties. `[SkippableTheory]`, the `YubiKeyTestState` input and async
session disposal remain coherent in the genuine annotation context. Reading metadata requires no
credential or physical touch, and this example introduces no persistent mutation.

The actual v2 annotation project references PIV and Tests.Shared and uses the repository's `xunit`
package (2.9.3), SkippableFact and real `Xunit` imports. The installed xunit.assert 2.9.3 API XML for
`Assert.InRange<T>(actual, low, high)` explicitly defines inclusive low/high bounds. Context7 also
confirmed the xUnit2002 rule's cause for null assertions on value types. This API/context review
supports the correction's meaning; it is not a claim that the corrected extracted host has compiled.

## Exact change and acceptance continuity

Read-only checks reverse exactly these two lines in the current complete CLAUDE file:

1. Remove `MinFirmware = "5.3.0"` from the example's attribute.
2. Restore `Assert.NotNull(metadata)` in place of `Assert.InRange(...)`.

The reversed bytes reproduce the preceding accepted complete-file hash exactly. All other content
in that file is unchanged. Current source-order fence #6 has SHA-256
`4464f14bda4ed114d1cbbb0c3994bc0af74fab338095a7eb6ee9ba5a756d213e` and remains a composable example
requiring the genuine xUnit/Tests.Shared annotation host. No block was added or removed.

Independently checked the other 56 product paths against `reviewed-files.json` final hashes: every
actual current file matches. Checked all 48 `format-transform.json` before/after mappings against the
original accepted manifest, recorded whitespace-only/import preservation, and preceding final hashes.
Only CLAUDE differs from that prior final checkpoint. The new JSON enumerates all 57 current hashes
and scopes. No shipping C# body or executable test assertion changed in this Markdown-only correction.
Prior API/token-preservation and hardware-impact acceptance remain unaffected.

## Validation boundary

The original annotation build failure stays preserved. The old v2 extracted annotation host still
contains the former assertion at this review checkpoint; **fresh extraction and affected-example
compilation remain pending with the coordinator**. The supplied 44-group consumer compilation and
second-capture PASS do not validate this changed annotation body. Supplied repository unit results
(4,124 cases, including 289 PIV, five skips, zero warnings) precede this Markdown edit and remain
separate historical code-gate evidence. Documentation QA and the current affected example need their
own final outcomes; this correction supplies no reason to infer a new shipping-code regression.

Existing hardware impact remains accepted: four supplemental PASS, two expected desired-contract
FAIL confirming PIVIMP-005, and P07 deferred; physical touch/hotplug/cancellation qualification remains
unchanged. This reviewer authored only this record and `evidence/review-accepted-v2.json`, preserving
all earlier review records. No product edits, builds, tests, hardware, formatting, agents or commits
were run. The user's approved task/actor/behavior-first style is preserved without prose churn.
