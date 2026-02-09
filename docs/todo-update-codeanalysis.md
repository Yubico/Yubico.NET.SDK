# TODO: Upgrade Microsoft.CodeAnalysis.NetAnalyzers to 10.0.102

## Current State
- Pinned at `9.0.0` in `Directory.Packages.props`
- Latest available: `10.0.102`

## Why Deferred
Upgrading introduces 189 new analyzer errors, primarily:
- **CA2000** - Dispose objects before losing scope (stricter detection in Scp11 code)
- **CA1724** - Type name conflicts with namespace (`DependencyInjection` class vs `Microsoft.Extensions.DependencyInjection` namespace)

## Scope
Files with known violations:
- `Yubico.YubiKit.Core/src/SmartCard/Scp/ScpState.Scp11.cs` - Multiple CA2000 (Tlv, ECDiffieHellman disposal)
- `Yubico.YubiKit.Core/src/DependencyInjection.cs` - CA1724 naming conflict

## Action
1. Upgrade `Microsoft.CodeAnalysis.NetAnalyzers` to `10.0.102` in `Directory.Packages.props`
2. Fix CA2000 violations (add `using` declarations or explicit disposal)
3. Fix CA1724 by renaming the extension type or suppressing with justification
4. Build with zero errors/warnings before committing
