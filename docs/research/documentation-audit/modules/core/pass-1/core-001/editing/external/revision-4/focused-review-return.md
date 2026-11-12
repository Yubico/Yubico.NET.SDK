# External review correction — R1/R2

## Outcome

R1 and R2 were verified against the named implementation/passages and corrected within the bounded
assignment. The final uncommitted successor has **37 product files**: the reviewed 35-path set plus
two previously retained files. The README is an intentional R1 update within the reviewed set, not a
third new path. All **164 path decisions** remain represented. Focused independent R1/R2 re-review
is still required; this editor return does not declare the full batch accepted.

Worktree: `/home/rupicola/Code/y/yubikit-core-docs-edit-external`. Base/unchanged HEAD:
`da66f5d01ba594993974ddd26723e8c300c0a454`. Source pin:
`fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`.

Full patch: `final-successor-source.patch`.
SHA-256: `4738181238db7103d65e47addac0a96c8a936e8c4a464a991885abd8ea788d68`.

Only three product documentation paths and new `editing/external/revision-4/*` records were written.
The accepted three toolchain comment corrections remain raw-byte identical. Every initial/revision-2/
revision-3 patch and receipt, plus `revision-3/review.md` and `review-identities.json`, remains unchanged.
The prior README raw hash remains preserved as history and is explicitly superseded for R1; no claim
is made that all earlier 19/32/35 current product hashes remain equal after this authorized correction.

## R1 — finish the existing authorization facet

Identity: **CORE-A-C013 / FPT-013**, inherited **management-001:MGMT-XREF-003**. No new finding family,
canonical ID, authorization setting or runtime policy was introduced.

| Product path | Verification and bounded correction |
| --- | --- |
| `src/Tests.Shared/README.md` | The overview and Known Limitations contradicted its corrected Safety section. Both now distinguish empty configured serials exiting unless unknown serials are enabled, filtered rejected/failed-identification views, and `SkipException` at empty-fixture row binding. Verified against infrastructure203–206,216–260,263–290 and state315–334 in the reviewed source. |
| `src/Tests.Shared/Infrastructure/AllowList.cs` | Read the complete constructor/predicate. The exit condition is `_allowedSerials.Count == 0 && !AllowUnknownSerials`; `IsDeviceAllowed` returns Boolean. Class/constructor XML now states those facts and the separate harness filtering/binding/pre-authorization identification behavior. The formerly generic retained path is explicitly attached to A-C013 with original shared units and new source anchors. |
| `src/Tests.Shared/Infrastructure/AppSettingsAllowListProvider.cs` | Read constants, GetList and AllowUnknownSerials. The XML JSON body now uses actual `AllowUnknownSerials=false`, not unread `EnableHardFail=true`; nearby prose names both consumed keys and the serial-less authorization condition. The code tag identifies JSON. This formerly generic retained path is explicitly attached to the existing A-C013 facet. |

The provider's executable `OnInvalidInputErrorMessage` raw-string example still contains
`EnableHardFail=true`. It is preserved in full as required, with an explicit existing-identity non-D
output referral in `remaining-referrals.json`. No actual configuration, constructor, predicate, helper,
guard, test assertion, import/directive or runtime/error/help string was changed.

## R2 — authoritative successor classifications

`example-classification-overlay.json` is keyed by exact **path/version/body SHA-256**. It retains each
immutable original entry and its replacement classification. `example-inventory.json` materializes the
corrected 247-body successor view, rather than binding the final return only to the old misclassified
inventory. It reuses the already existing XML-body extraction expression, not a new parser or harness.

| Body / exact original hash | Successor classification and provenance |
| --- | --- |
| Provider XML, base/prior-current: `f44d4bb4843f4f6f73c6aa90a40ed69d8bbedaf1da0a26b1fd1276ef05dee888` | JSON configuration illustration, never a composable C# example or C# compilation participant. The old unread-setting body remains historical. Current R1 JSON replaces only the prior-current body with hash `d5539c5c9af0a16cb63930653f4a11a2979f287af69b3cb1899032d286d30b4e`; its keys/false value were statically checked, not loaded as configuration. |
| TestCategories XML shell recipes, base/current: `5a25bdc655b19f6637001274cb1de027ee0aca3bff6f1e77eb4797cb5419a4d7` | Shell command fragments embedded in XML, not C#. Preserve literal `&amp;` in the extracted raw body/hash. A separate derivative expands only those references to shell `&`, hash `5c293e080aac266553cc3e0920b1a11d6eb758448ba41340e09b27b1147e67a2`. The derivative is labeled, not substituted for raw provenance. No command ran. Associations: A C019/FPT-019 and B4 C008/FPT-055. |
| CLAUDE AllowList current: `4a89a27ab23bc7e2123eb74ffd192133757490d18bb829dbf1d0767862041126` | Abbreviated implementation sketch, not a complete replacement. Current CLAUDE32–33 says this explicitly; actual property/logging/complete implementation remains in source. The base sketch (`96d97d285b01486d97a7ce0e752b83f616ca98a7e926cb3631594edd31094813`) receives the same sketch category. No product body expansion or compilation was performed. |
| Superseded IYubiKeyFilter base: `ae523949c5098862ac6b754577eee04dd0731485e5a39cd027f9c08dbb6e93a5` | Non-current invalid API example for the audited pin, with nonexistent YubiKeyTheory/YubiKeyTestDevice. It is not a usable fragment whose context supplies missing types. The existing corrected current WithYubiKey/YubiKeyTestState example remains unchanged and separately classified. No compiler failure is claimed to have been executed. |

There are **seven hash-keyed classification corrections** across those base/current entries, including
one replaced current JSON body. All original bodies remain available in the overlay and frozen earlier
inventories. No new C# consumer body, compiler harness, shell execution or configuration load was added.

## Records for focused re-review

All names below are relative to this revision-4 directory.

| Record | Purpose |
| --- | --- |
| `final-successor-source.patch` | Complete 37-file successor, conventional a/b prefixes |
| `R1-comment-delta.patch` | Exact three-path change from the reviewed revision-3 source; README before-image verified against its frozen raw hash |
| `path-decisions.json`, `candidate-decisions.json`, `allowlist.json` | Current 164-path identities/decisions, actual A-C013 attachments, original inherited IDs and unchanged ownership scope |
| `example-classification-overlay.json`, `example-inventory.json` | Exact R2 corrections, original/current body text/hashes, raw/rendered distinctions and final example view |
| `remaining-referrals.json` | Existing holds plus the protected provider runtime-example inconsistency; no behavior change |
| `history-preservation.json` | Byte-exact earlier outputs/reviews, unchanged other 34 reviewed product paths and accepted toolchain comments, intentional README supersession |
| `static-preservation.json`, `final-checks.json`, `output-identities.json` | Fresh bounded static checks, counts, full patch identity and final owned record hashes |
| `record.py` | Bounded reproduction without running frozen assemblers, SDK/test code or new parsers |

## Preservation and remaining limits

The two changed C# files retain exact non-full-line-comment lines, including raw-string error text,
imports, directives, declarations, expressions and branches. XML is well formed. README preservation
is checked by reversing only the two exact R1 passage replacements and matching the reviewed raw hash.
All other 161 path rows retain their prior current hashes; historical output/review hashes are checked
separately. Markdown fences, exact write scope, unchanged HEAD and `git diff --check` are checked.

No build, SDK/test run, formatter, cryptographic/provider/native/device execution, Git index/ref change,
commit or subagent occurred. Other worktrees were read-only. No validation tooling/profile was modified;
the coordinator still reconciles accepted source/input identity after independent acceptance and the
current pilot. These static checks are not Roslyn binding, compiled examples or independent acceptance.

All earlier policy/implementation/experiment, runtime/data, producer/profile/physical and historical
qualification holds retain their original identities and reasons. Windows concurrency is still
UNREADY/UNREVIEWED, PIV physical deferrals stand, serial 9681620's cause remains unexplained, and serial 125's
setup 6A88 before RESET supplies no protected-reset result. Re-review only these bounded R1/R2 changes
and successor identities, then complete Core Pass 1 publication and the deferred handoff and stop.
