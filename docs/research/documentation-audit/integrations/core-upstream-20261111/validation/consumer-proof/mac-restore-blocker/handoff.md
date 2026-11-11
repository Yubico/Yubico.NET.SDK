# Mac restore blocker — read-only diagnosis

## Root cause and exact failure

The first actual Mac-comparison build failed during restore, **before Mac Csc or capture**. The retained
step reports NU1301 / HTTP **401 Unauthorized** from the configured GitHub package-id index. Its actual
build log contains six authentication warnings and one restore error; none is hidden or counted as a
passing compilation. Logger exit 1 is complete, retained and quiescent.

There is a separate, source-proven **derivative restore-routing mismatch**:

| Input | Actual value |
| --- | --- |
| Central `Directory.Packages.props` | Yubico.NativeShims **1.18.1** |
| Prepared wrapper AdditionalProperties | BoundaryNativeVersion **1.18.1**, integration variant, current Core path |
| Child project's historical conditional default | **1.18.1-async.8** |
| Actual generated restore dependency | VersionOverride **`[1.18.1-async.8, )`** |
| Actual restore output | **`obj/local/`** |
| Intended integration-variant restore assets | **Absent** |

Both the child's dgspec and failed assets record the historical default, despite retaining the central
1.18.1 pin. The VersionOverride takes precedence. No concrete NativeShims library/version was resolved:
the failed request was for the package-ID index, not proof of a particular version download.
Stable 1.18.1 is newer than the prerelease lower bound; the report does not falsely say it is excluded
by that range. The unintended minimum is not cached, and the actual restore queried the remote index.

Selected SDK 10.0.404 `NuGet.targets:1215-1220` converts ProjectReference paths into a string property
and constructs fresh restore items. Its recursive path walk at 1298-1318 passes its restore input
properties, not the original wrapper item's AdditionalProperties. The observed local variant/default
override confirms those three properties did not reach this restore evaluation. Normal build-reference
metadata is not sufficient to establish identical child **restore** property evaluation.

This is not a shipping SDK/API/native failure. The immediate network failure is real authentication
failure, but asking for credentials for the unintended historical restore profile is not the first fix.

## Canonical cache and credentials — no secrets retrieved

`report.json` and `credential-presence-supplement.json` contain redacted inspection only:

- Cached NativeShims directories are exactly **1.16.1** and **1.18.1**. The unintended prerelease minimum
  has no cache directory. No lower-version substitution is proposed.
- Cached **1.18.1** has nuspec ID/version matching the intended canonical package. Its installed content
  hash matches **21 accepted SDK catalog bindings**. The package source metadata records the official
  `https://api.nuget.org/v3/index.json` source.
- Its **84,067,166-byte** nupkg exists. Archive SHA-256 is
  `243958f7e87d96e22b33b1d9b469287ea9d6dd3461cd904a292d3342d27d5155`;
  streamed signed-archive SHA-512 matches its stored archive hash. Catalog/unsigned content SHA-512 is
  separately matched, not incorrectly equated to the signed archive hash.
- Both official nuget.org and `Yubico_GH` are already configured/mapped for NativeShims. No source was
  added, changed, disabled or bypassed. Cached source metadata is recorded provenance, not a new
  independent network verification of public-server availability/hash.
- Neither actual restore config contains a `Yubico_GH` credential group, including inherited groups
  without a same-file source declaration. `NuGetPackageSourceCredentials_Yubico_GH` is absent.
  GH_TOKEN, GITHUB_TOKEN and NUGET_PLUGIN_PATHS are absent in this tool environment. Only presence
  booleans are reported; no values are logged.
- This configuration is **consistent with an unauthenticated GitHub request receiving 401**. It does
  not establish whether an external credential provider/keyring could supply credentials, nor the
  validity/scopes of any existing GitHub CLI account. No gh token/status, keyring/provider credential,
  auth refresh/login, identity switch, secret export or NuGet.Config copy was performed.

Earlier successful cache-backed SDK/tools/native builds do not prove GitHub feed authentication worked.
NuGet credentials are source-name-specific; GitHub CLI authentication is not automatically NuGet feed
authentication. Context7 authenticated-feed documentation confirms the environment/config/provider
lookup order. No protected-feed authorization is inferred or bypassed.

## Preserved evidence

`snapshots/` contains byte-exact copies of the original v2 logger streams/RESULT/step logs, failed child
restore metadata, traversal restore metadata, project/wrapper/central declarations, selected NuGet
targets and nonsecret canonical cached package metadata. Raw 401 output remains retained. Config files
and credentials are never copied. The large cached archive is hashed in place, not placed in the packet.

No Mac compiler/capture output or new DLL was produced by this failed attempt. No completed earlier
proof is recompiled, relabelled or repaired. Product source, SDK/package configuration, index and all
accepted v4/v5 artifacts remain unchanged.

## Minimal recommended remedy — not executed

First align the **derivative caller's restore and build properties** with the already approved baseline.
`proposed-lane.sh` supplies the exact same three nonsecret Boundary properties through the child process
environment as well as the unchanged wrapper metadata. MSBuild loads environment variables as initial
properties before conditional project defaults; every fresh restore evaluation therefore sees the
intended version/variant/Core path. Context7 `/dotnet/msbuild` evaluation source confirms this ordering.

This changes neither the shipping project nor its historical default. It neither lowers the package
version nor changes a feed or credential. The original source wrapper, copy guards and v5 verifier
remain intact. The proposal requires coordinator/source review and a newly assigned SDK attempt;
this owner did not execute it.

After approval, the coordinator can use a **fresh** suffix:

```bash
cd /home/rupicola/Code/y/yubikit-core-docs-integration
P=docs/research/documentation-audit/integrations/core-upstream-20261111/validation/consumer-proof
bash "$P/mac-restore-blocker/proposed-lane.sh" restore-aligned-first
```

Before interpreting compilation/capture, inspect the new child dgspec: it must record native override
`[1.18.1, )` and output `obj/audit-integration-20261111/`, not local/async. Do not retry the old `first`
label or overwrite its failed metadata. Actual success is not predicted: configured private-source
metadata/audit requests may still need authentication even with a canonical warm cache.

**User input:** no credential is needed to prepare/review the property-routing correction. If a correctly
evaluated retry still requires the private feed, an operator/auth owner must explicitly authorize an
appropriate existing credential for an ephemeral source-specific NuGet invocation (with required
package-read permission), or approve an independently verified exact-package public-source plan.
Do not retrieve/swap credentials, install anything, refresh login, change config or suppress restore
errors without that assignment. No authentication remedy has been applied here.

This packet supplies diagnosis and a source-bound proposal only. Mac compilation, capture/live,
native/platform behavior and the full Core audit remain unqualified by it. Windows concurrency work
is unrelated and untouched.
