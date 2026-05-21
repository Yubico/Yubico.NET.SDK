# Merge Plan: `worktree-security-remediation` into `yubikit-applets`

## Context

We need to merge the security remediation branch (27 commits) into our current working branch (13 unique commits). The remediation branch adds ConfigureAwait(false), IDisposable on SCP types with buffer zeroing, a ChainedApduTransmitter range bug fix, and PIN/password API changes from `string` to `ReadOnlyMemory<byte>`. Our branch has the unified `yk` CLI with all 7 applets, CTAP exit codes, and various bug fixes. No .csproj conflicts exist.

## Merge Strategy

**Command:** `git merge worktree-security-remediation --no-ff`

### Expected Git Conflicts (5 files)

| File | Resolution |
|------|-----------|
| `Plans/handoff.md` | Take theirs (`--theirs`) |
| `src/Fido2/examples/FidoTool/FidoExamples/PinManagement.cs` | Take theirs (ReadOnlyMemory API) |
| `src/Fido2/tests/.../FidoTestData.cs` | Manual: combine KnownTestPinString ref + PinUtf8 field |
| `src/OpenPgp/src/IOpenPgpSession.cs` | Manual: remediation API + our ResetPinAsync docs |
| `src/OpenPgp/src/OpenPgpSession.Pin.cs` | Manual: our admin PIN flow fix + remediation's ReadOnlyMemory API |

### Auto-Merged Security Fixes (no conflicts, just verify)

These auto-merge cleanly and bring the security improvements we want:
- `src/Core/src/SmartCard/ChainedApduTransmitter.cs` - range bug fix (`offset..ShortApduMaxChunk` -> `offset..(offset + ShortApduMaxChunk)`)
- `src/Core/src/SmartCard/Scp/ScpProcessor.cs` - IDisposable + buffer zeroing
- `src/Core/src/SmartCard/Scp/ScpState.cs` - IDisposable + AES key zeroing
- `src/Core/src/SmartCard/Scp/ScpState.Scp03.cs` - Console.WriteLine removal + sessionKeys.Dispose()
- `src/Core/src/SmartCard/Scp/ScpInitializer.cs` - auth failure cleanup
- ConfigureAwait(false) additions across many files

### Post-Merge Compilation Fixes

The remediation branch changed PIN/password APIs from `string` to `ReadOnlyMemory<byte>`. Our CLI code (which auto-merges clean) will fail to compile because it calls the old string-based APIs.

**Files needing adaptation:**
1. `src/Cli/YkTool/Commands/Fido/FidoCommands.cs` (~12 call sites) - convert string PINs to `Encoding.UTF8.GetBytes(pin)`
2. `src/Cli/YkTool/Commands/OpenPgp/OpenPgpAccessCommands.cs` (~6 call sites) - same pattern
3. `src/Fido2/tests/.../TestExtensions/FidoTestStateExtensions.cs` - use existing `KnownTestPin` byte array instead of string

**Pattern for CLI fixes:**
```csharp
// Before
await clientPin.SetPinAsync(pin, cancellationToken);

// After
var pinBytes = Encoding.UTF8.GetBytes(pin);
try
{
    await clientPin.SetPinAsync(pinBytes, cancellationToken).ConfigureAwait(false);
}
finally
{
    CryptographicOperations.ZeroMemory(pinBytes);
}
```

## Execution Steps

1. `git merge worktree-security-remediation --no-ff`
2. Resolve 5 git conflicts per table above
3. Build - identify compilation errors
4. Fix all string-to-ReadOnlyMemory API mismatches in CLI and test code
5. Build again - verify clean
6. Run unit tests: `dotnet toolchain.cs test`
7. Commit the merge

## Verification

- [ ] `dotnet build Yubico.YubiKit.sln` compiles clean
- [ ] `dotnet toolchain.cs test` passes
- [ ] ChainedApduTransmitter has correct range: `data[offset..(offset + ShortApduMaxChunk)]`
- [ ] ScpProcessor implements IDisposable
- [ ] ScpState implements IDisposable with key zeroing
- [ ] No Console.WriteLine in SCP code
- [ ] CLI code uses ReadOnlyMemory<byte> for PIN/password APIs
- [ ] ConfigureAwait(false) present on async calls
