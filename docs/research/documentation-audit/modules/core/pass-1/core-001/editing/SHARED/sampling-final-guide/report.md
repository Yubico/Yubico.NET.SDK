# Bounded final-guide sampling correction

This documentation-only return propagates SAMPLE-A003's already established lifecycle conditions into
selected units `core-001:unit:0b5f8163f489c670` (`docs/usage/device-discovery.md`) and
`core-001:unit:c2de2d6f26065229` (`src/Core/README.md`), and corrects SAMPLE-TESTING001, selected unit
`core-001:unit:287e6410db16f0e7` (`docs/TESTING.md`). These are actual sample misses, not a new runtime
finding, testing policy or broader audit. Coordinator-owned edited exclusions, expansion and quota
replacement remain separate.

All three exact before-files matched primary current accepted bytes before editing. The old blanket
claims were still present; there is no already-corrected overlap to dismiss. Only these three product
Markdown paths change. All prior accepted/editor/review/patch and sampling artifacts remain byte-exact.

## Checked evidence and corrections

### A003 propagation: cleanup path and subscription outcomes

Complete monitor/hosted-guide units and their surrounding startup/disposal/thread-safety guidance were
read. The same source order established in `sampling-correction/source-evidence.json` governs these
two surfaces: HotplugSignals attempts cleanup with unguarded logging; YubiKeyMonitor invokes signal
disposal before cancellation/join/retirement/feed completion; DeviceChangeFeed preserves an existing
channel error while later completed-feed subscriptions end empty.

The hosted adapter's fence is unchanged. Its prose now describes requesting/awaiting disposal,
conditional ordered completion, healthy uncanceled tails versus terminal/overflow errors, and the
application's responsibility for consumer errors/queued work. Core's readme uses the same conditions.
First-publication failure/cancellation awaits constructed-monitor disposal, whose own failure can
replace the earlier error. No unconditional original-error/cleanup guarantee or new drain API appears.
Same-unit listener-startup and thread-safety wording receives the matching bounded qualification.

### SAMPLE-TESTING001: runner-specific manual filter and central version detection

Context7 was consulted for current xUnit v3/Microsoft.Testing.Platform CLI guidance, followed by the
official xUnit MTP page's CLI mapping. That page distinguishes native xUnit CLI options from MTP
options; its method mapping is `-method` versus `--filter-method`. Newer query-filter facilities and
multi-value/repeated-switch semantics are not imported into this checkout's pinned contract.

Actual checkout evidence:

- `scripts/toolchain/Discovery.cs:29–36` checks the project opt-in string, with
  `Conventions.cs` defining `UseMicrosoftTestingPlatformRunner`.
- `scripts/toolchain/TestTargets.cs:124–155,195–242` builds MTP `dotnet run ... --` arguments and
  translates method terms to `--filter-method`; the other branch passes VSTest `--filter`.
- Core unit tests declare `UseMicrosoftTestingPlatformRunner=true`, `OutputType=Exe` and `xunit.v3`;
  Core integration tests reference `xunit` and `Microsoft.NET.Test.Sdk` without that opt-in.
- `Directory.Packages.props` enables central package management and pins `xunit` 2.9.3 and `xunit.v3`
  3.0.0. PackageReference names identify the framework dependency; inline project Version attributes
  are not the version source here, and a version number alone does not choose CLI mode.

The manual table now explicitly compares this repository's v2/VSTest and v3/MTP modes and gives
`--filter-method "*TestName*"` for the MTP row. The canonical toolchain recommendation stays unchanged.
No package upgrade, config/project edit, runner execution, assertion or example-body change occurs.

## Transfer artifacts

`product.patch` is the focused three-document before-to-after diff. Complete `before/*` and `after/*`
files, exact hashes/original IDs, source evidence, unchanged complete example bodies and static receipts
are alongside this report. Source captures identify moving official documentation separately from the
checkout's centrally pinned packages and source; they supply no executed-run qualification.

`preserved-record-identities.json` binds all prior SHARED artifacts. Verification compares the other
23 existing product paths, checks every fence body in these three files unchanged, and checks focused
patch application, whitespace and new missing local file targets. Anchors/external URLs are outside
that mechanical link check. Independent focused acceptance remains pending.

No primary-checkout/source/config/product script, central sampling record, original artifact or other
worker file is edited. No build/test/formatter/compiler/native/crypto/device operation, Git index/ref/
commit/push or subagent runs. Existing deferred findings and physical/Windows qualifications remain.
Complete focused review, Core Pass-1 publication and the deferred handoff, then stop.
