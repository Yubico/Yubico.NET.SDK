# Independent narrow review — Mac restore property routing

## Decision

**The three-property routing remedy is sound as a source plan. One small provenance correction is
required before retry authorization: bind the new process-environment inputs into the actual build
receipt. The current proposed launcher does not do that.**

This finding does not reopen the accepted v5 fixture or v4 consumers proofs, or require any SDK,
product, package, feed, credential or verifier-policy change. It is not a prediction that corrected
restore or compilation succeeds. Reviewer: independent Sol narrow review, 2026-11-11.

Only this review file was written. No build, capture, test, native/device operation, product/config/auth
change, index/ref change, commit or delegation occurred. No raw NuGet.Config or credential store was
read, copied or queried in this review; configuration/auth discussion uses only the redacted packet.

## Exact reviewed identities

Paths are relative to this integration's `validation/consumer-proof/` directory.

| File | SHA-256 |
| --- | --- |
| `mac-restore-blocker/receipt.json` | `6dea02b4bd9f7c7e0e830dcfb8322b33f5cc07d9009bb0c59298dcb3705b10cf` |
| `mac-restore-blocker/proposed-lane.sh` | `f2b645b8694c1aedcec8c7d779ffb8fd939fb288ee23e6de28ee4d6cdf73211a` |
| `mac-restore-blocker/handoff.md` | `eeed5f8086e230f56656ad6e6041bae74b1e71bc6ed1762db35272067df42a7b` |
| `mac-restore-blocker/report.json` | `aa03a288a11e72145d90ecb3fe146b84c0519b1be0475d89ebbaa6f74ab48259` |
| `mac-restore-blocker/credential-presence-supplement.json` | `4cb809fbaf9961aa1e4e65fb75786f7d766cdf871333d22cb5007acd3d269375` |
| Accepted `correction-v5/receipt.json` | `174bef5e532a95386a0e573bef9692b68e1e90824186b930779868f72eaabc39` |

Independently checked every receipt-listed identity and all **30 original/preserved snapshot pairs**;
all sizes and hashes match. The accepted v5 receipt chain still passes without modification.
`bash -n mac-restore-blocker/proposed-lane.sh` passed; the launcher was not executed.

## Confirmed diagnosis and routing scope

The original `consumer-mac-comparison-build-first` receipt is failed/exit 1, complete-retained and
quiescent. Its retained build step shows **NU1301 / HTTP 401**, six warnings and one restore error.
There is no raw compiler-policy directory for that attempt. This is a genuine restore-stage build
failure before Mac Csc/capture, not another compiler-proof collector failure or a native/API fault.

Independently re-read the actual failed child dgspec/assets and source declarations:

- Central NativeShims version: **1.18.1**.
- Actual child `version` and `versionOverride`: **`[1.18.1-async.8, )`**.
- Actual child restore output: **`verification/MacOSHidBoundaryComparison/obj/local/`**.
- Intended integration-variant assets: absent.
- Resolved NativeShims library records: none. The 401 was on the package-ID index; no particular
  version download was established. Stable 1.18.1 is not excluded by the prerelease lower-bound range.

The installed selected `NuGet.targets:1215-1220` converts ProjectReference paths into a string property
and constructs new restore items. Its recursive path walk at lines 1298-1318 passes restore-input
properties rather than the original wrapper item's AdditionalProperties. Together with the actual
default/local restore metadata, this supports the observed missing derivative restore profile.

The proposed values **exactly match** the sealed wrapper AdditionalProperties:

```text
BoundaryCorePath=/home/rupicola/Code/y/yubikit-core-docs-integration/src/Core/src/bin/Release/net10.0/Yubico.YubiKit.Core.dll
BoundaryNativeVersion=1.18.1
BoundaryComparisonVariant=audit-integration-20261111
```

The Core path is the canonical existing regular current-checkout output, not an old temporary-tree
path. Names/case and shell quoting are correct. These are command-local environment assignments for
the child Bash lane and descendants, not parent-shell exports or machine/global configuration changes.
They are visible to the invocation's other process/project evaluations, but are the narrowly named
Boundary properties consumed by this comparison profile, not global framework/analyzer/version-policy
switches. The wrapper's original explicit properties continue to agree.

Context7 `/dotnet/msbuild` evaluation source confirms environment properties are loaded initially,
before conditional project defaults. Thus the intended nonempty values suppress these historical
defaults in fresh restore evaluation. Environment properties are not immutable command-line global
properties; actual generated restore and Csc evidence must still corroborate the intended profile.
No product default, version policy, feed mapping or authentication input is changed by this plan.

## R1 — actual build receipt does not bind the new environment

`proposed-lane.sh:10-13` supplies the Boundary values, then invokes the unchanged v5 lane. The v5
lane's build logger command remains the same canonical `dotnet toolchain.cs ...` command.

`validation/run-command-v2.py:180-181` correctly inherits the complete child environment for execution,
but its durable invocation/receipt at lines 196-200 records only:

```text
TMPDIR, TMP, TEMP, PYTHONDONTWRITEBYTECODE
```

The three Boundary properties are absent. The actual old failed receipt confirms that four-key
schema. The proposed launcher does not create an attempt-specific routing receipt or pass any
`--retain-text` input to the logger, and the unchanged v5 lane offers no such forwarding argument.
The static diagnosis receipt pins this proposed script, but is not linked into the future actual
build's retained receipt; the v5 capture-tool inventory likewise does not add this nested launcher or
diagnosis receipt. Consequently the changed environment would be used without the requested durable
build-receipt binding to that actual suffix/build invocation.

**Small bounded remedy:** use an additive caller/lane revision that records only these three nonsecret
effective environment values, active root, actual attempt/build label and launcher/lane identities in
an attempt-specific immutable routing descriptor before the SDK command. Retain that descriptor
through the existing build logger's **`--retain-text <relative-descriptor-path>`** support. Keep the
canonical toolchain command unchanged. The accepted build-receipt/capture code already retains and
hash-binds such extra text through its ordinary retained-record chain, so no rewrite of the sealed
v5 verifier, old lane, logger or accepted receipts is necessary. Do not dump the full environment.

Re-review only that small source/receipt linkage before retry. Merely adding the values to an
unconnected explanatory report, or relying on later Csc policy alone, does not repair actual caller
provenance. No implementation was made by this reviewer.

## Cache/authentication facts and acceptance conditions

Independently hashed the canonical cached 1.18.1 archive in place:

- Bytes: **84,067,166**.
- SHA-256: **`243958f7e87d96e22b33b1d9b469287ea9d6dd3461cd904a292d3342d27d5155`**.
- Its signed-archive SHA-512 matches the stored archive hash; the distinct installed content SHA-512
  matches all **21** accepted consumers SDK catalog bindings. No hash-domain equality was invented.

This cache evidence supports reuse of the intended exact canonical package. It does not prove a
future restore avoids remote metadata/audit requests or establish private-feed authorization.
The redacted packet shows both existing source mappings and absent recorded source-specific
credentials, consistent with the observed 401. It does not prove absence of all external providers,
or the validity/scope of any CLI account. No token, provider, keyring, identity or login was queried.

Before accepting a retry's compilation/capture, require the **new actual child dgspec** to show
`[1.18.1, )` and `obj/audit-integration-20261111/`, and actual assets/policies/Csc/direct-Core copies to
agree with the original prepared source contract and unchanged v5 guards. Preserve that new evidence.
The old lane automatically advances from successful build to capture; a logged capture PASS alone
must not substitute for this explicit restore-profile acceptance check. If needed, stage the caller
at build/inspect/capture or add a narrow metadata gate, not a new restore interpreter.

The intended fresh external invocation is:

```bash
cd /home/rupicola/Code/y/yubikit-core-docs-integration
P=docs/research/documentation-audit/integrations/core-upstream-20261111/validation/consumer-proof
bash "$P/mac-restore-blocker/proposed-lane.sh" restore-aligned-first
```

**Do not execute the currently pinned proposal until R1's receipt linkage is added and reviewed.**
At review time that suffix's four static/build/capture/live labels, raw attempt and versioned evidence
destination are unused. Use a newly reserved suffix if this changes; never overwrite/retry the old
`first` records or metadata. The 30 failed-record snapshots remain intact.

If the correctly routed retry still fails private-feed authorization, stop and retain the new failure
for the operator/auth owner. No credential lookup/export/refresh, source disable/ignore-failed-sources,
package substitution, config edit or public-source bypass is authorized by this review. A warm cache
and source-routing fix are not a promise of successful restore/build.

Accepted fixture-v5 four-project/live, consumers-v4 22-project/live and earlier 30-body/examples
evidence remain outside this narrow re-review. No whole-Core audit, Windows work, AOT linking,
Mac/native runtime or device qualification was started or claimed.
