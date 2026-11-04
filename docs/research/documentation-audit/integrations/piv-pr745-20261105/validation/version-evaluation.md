# Why the capture's version equality failed

The recorded build distinguishes the evaluated assembly/project version (`Version=1.0.0`) from the later
catalog property (`YubiKitVersion=2.0.0-alpha.2`). The original capture assumed they were equal; the
versioned adapter records them separately and checks actual project/reference versions instead.

The root `Directory.Build.props:33` assigns `<Version>$(YubiKitVersion)</Version>` without a project-name
condition. `Directory.Packages.props:6` defines `YubiKitVersion`. The installed SDK's import sequence
explains why this assignment supplies no final equality invariant:

1. `Microsoft.Common.props:34` imports `Directory.Build.props`.
2. Its later line 127 imports `NuGet.props`.
3. `NuGet.props:35` imports `Directory.Packages.props`, where the catalog property is defined.
4. `Microsoft.NET.DefaultAssemblyInfo.targets:14–18` fills an empty `Version` from `VersionPrefix`, whose
   default is `1.0.0`.

MSBuild expands a property reference when evaluating its assignment; later defining the referenced
property leaves that earlier assignment unchanged. If `YubiKitVersion` is empty at step 1, the root
assignment leaves `Version` empty, and the later SDK default fills it with `1.0.0`. This is consistent
with the actual captured host and all three SDK restore/reference/generated-assembly records. It is an
evaluation-order issue, rather than a project-name exception for the custom host. This task changes
the evidence check only; SDK build/version/packaging policy remains outside this correction.

Focused installed-source identity, read on 2026-11-05 without invoking MSBuild:

SDK root: `/home/rupicola/.local/share/mise/dotnet-root/sdk/10.0.403`.

| SDK-relative source | SHA-256 |
| --- | --- |
| `Current/Microsoft.Common.props` | `87a2707df41f5abbbd8ad5678715c169ba40ee7543fef4a98d7852ff17e94e48` |
| `NuGet.props` | `0bd56dea541dd607af6287264e4f5aa3dd223e42d2ffcac9a5230d41bbac3d7e` |
| `Sdks/Microsoft.NET.Sdk/targets/Microsoft.NET.DefaultAssemblyInfo.targets` | `1fde10e25a61a9f66d6e12616bb79c2eb878657bccfcff2a74d9155c345a0440` |

The relevant default block is:

```xml
<PropertyGroup Condition=" '$(Version)' == '' ">
  <VersionPrefix Condition=" '$(VersionPrefix)' == '' ">1.0.0</VersionPrefix>
  <Version Condition=" '$(VersionSuffix)' != '' ">$(VersionPrefix)-$(VersionSuffix)</Version>
  <Version Condition=" '$(Version)' == '' ">$(VersionPrefix)</Version>
</PropertyGroup>
```

Actual version provenance is retained independently in `capture-v2-receipt.json`: declared project paths,
restore entries, compiler reference paths and SHA-256, metadata-derived FusionName versions, and generated
assembly attributes. Root props/package files and the actual host policy remain in the compiler capture.
