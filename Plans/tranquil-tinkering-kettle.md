# Plan: Refactor ApduCommand to readonly record struct

## Context

`ApduCommand` is currently a `sealed class` that clones the caller's buffer into an internal `byte[]` and implements `IDisposable`/`ZeroData()` to zero that clone. After analysis, this design is unnecessary:

- Commands are never queued or stored across scopes; the pipeline is a straight call chain
- The clone-and-dispose approach actively *breaks* queuing (disposes at scope exit, before dequeue)
- `using var` on a class zeroes the internal clone — but with passthrough (`ReadOnlyMemory<byte>`), all struct copies reference the same underlying buffer, so the caller zeroing their source zeroes everything
- `ApduResponse` is already `readonly record struct` — making `ApduCommand` match gives the pair consistent semantics

**Goal:** Passthrough design — `ApduCommand` stores `ReadOnlyMemory<byte>` directly with no clone, no `IDisposable`, no `ZeroData()`. Callers own their buffer and zero it themselves.

**Note on CLAUDE.md rule:** The rule "never put `ReadOnlyMemory<byte>` in a struct" targets the struct-owns-a-clone pattern (multiple copies, each a different reference, can't zero them all). Passthrough is different — all copies reference the same caller-owned memory; zeroing the source zeroes all views. This is safe.

---

## New ApduCommand Design

```csharp
public readonly record struct ApduCommand
{
    public ApduCommand(int cla, int ins, int p1, int p2, ReadOnlyMemory<byte> data = default, int le = 0)
    {
        Cla = ByteUtils.ValidateByte(cla, nameof(cla));
        Ins = ByteUtils.ValidateByte(ins, nameof(ins));
        P1  = ByteUtils.ValidateByte(p1,  nameof(p1));
        P2  = ByteUtils.ValidateByte(p2,  nameof(p2));
        Data = data;
        Le   = le;
    }

    public byte Cla  { get; init; }
    public byte Ins  { get; init; }
    public byte P1   { get; init; }
    public byte P2   { get; init; }
    public int  Le   { get; init; }
    public ReadOnlyMemory<byte> Data { get; init; }

    public override string ToString() =>
        $"CLA: 0x{Cla:X2} INS: 0x{Ins:X2} P1: 0x{P1:X2} P2: 0x{P2:X2} Le: {Le} Data: {Data.Length} bytes";
}
```

- `record struct` auto-generates parameterless constructor → object-initializer syntax (`new ApduCommand { Ins = 0xA4 }`) continues to work
- No `IDisposable`, no `ZeroData()`, no backing `byte[]`
- Consistent with `ApduResponse` (`readonly record struct`, same file's neighbour)

---

## Files to Modify

### 1. `src/Core/src/SmartCard/ApduCommand.cs` — **Full rewrite**
- Change type declaration to `public readonly record struct ApduCommand`
- Remove `_dataBytes` field, `ZeroData()`, `Dispose()`, `IDisposable`
- Remove `using System.Security.Cryptography` import (no longer needed)
- Replace `Data` clone init with passthrough `{ get; init; }`
- Remove private `byte` constructor (validation chain no longer needed — constructor takes `int` directly)
- Update XML docs to reflect passthrough + caller-owns semantics

### 2. `src/Core/src/SmartCard/Scp/ScpProcessor.cs` — **Remove ZeroData calls**
- Lines 64-65: `ApduCommand? scpCommand = null` / `ApduCommand? finalCommand = null` — keep as nullable struct (works fine)
- Line 150: `scpCommand?.ZeroData();` → **remove** (source arrays `scpCommandData`/`finalCommandData` are already zeroed 2 lines below)
- Line 151: `finalCommand?.ZeroData();` → **remove** (same reason)

### 3. `src/Core/src/SmartCard/ChainedApduTransmitter.cs` — **Remove `using`**
- Line 37: `using var chainedCommand = new ApduCommand(...)` → `var chainedCommand = new ApduCommand(...)`
- Line 47: `using var finalCommand = new ApduCommand(...)` → `var finalCommand = new ApduCommand(...)`
- (Struct chunks are not sensitive here — they're slices of the original command's data, which the original caller owns)

### 4. All other `using var cmd = new ApduCommand(...)` sites — **Strip `using`**

These files have `using var` wrapping non-sensitive or caller-managed data. Remove `using`; callers already zero their source buffers in their own `try/finally` blocks:

| File | Lines (approx) |
|---|---|
| `src/Piv/src/PivSession.Authentication.cs` | ~158, ~535, ~657 |
| `src/Piv/src/PivSession.cs` | ~266 |
| `src/Piv/src/PivSession.KeyPairs.cs` | ~100, ~190 |
| `src/Piv/src/PivSession.Crypto.cs` | ~187, ~406 |
| `src/Piv/src/PivSession.Metadata.cs` | ~142, ~275, ~311 |
| `src/SecurityDomain/src/SecurityDomainSession.cs` | ~448, ~626, ~685 |
| `src/OpenPgp/src/OpenPgpSession.Crypto.cs` | ~36, ~54, ~73 |
| `src/OpenPgp/src/OpenPgpSession.Pin.cs` | ~140, ~206, ~281 |
| `src/YubiHsm/src/HsmAuthSession.cs` | ~482, ~638, ~687, ~747, ~789 |
| `src/Oath/src/OathSession.cs` | ~523, ~595 |
| `src/Core/src/SmartCard/Scp/ScpInitializer.cs` | ~97 |

### 5. `src/Core/tests/.../Fakes/FakeApduProcessor.cs` — **No change needed**
`List<ApduCommand>` works correctly with value-type structs. Copies stored in list are fine — test assertions read `.Cla`, `.Ins`, etc. which are all value fields.

### 6. `src/Core/src/SmartCard/ChainedResponseReceiver.cs` — **No change needed**
`private readonly ApduCommand _getMoreDataApdu` is an embedded struct field — fine.

---

## What Does NOT Change

- All call sites that use `new ApduCommand(int, int, int, int, ...)` — constructor signature identical
- All call sites that use `new ApduCommand { Ins = X, Data = y }` — object initializer still works (record struct has parameterless ctor)
- All method signatures that accept `ApduCommand` — pass-by-value was already the intent; struct makes it explicit
- `FakeApduProcessor`, `NSubstitute` matchers — struct works fine

---

## Verification

```bash
# 1. Build — must compile with zero errors
dotnet build.cs build

# 2. Unit tests — all green
dotnet build.cs test

# 3. Confirm no ZeroData/Dispose references remain on ApduCommand
grep -rn "\.ZeroData()\|apduCommand.*Dispose\|using var.*ApduCommand\|using var.*= new ApduCommand" src/
# Expected: zero matches

# 4. Confirm struct declaration
grep -n "readonly record struct ApduCommand" src/Core/src/SmartCard/ApduCommand.cs
# Expected: one match on line ~22
```
