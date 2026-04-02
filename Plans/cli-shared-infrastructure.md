# CLI Shared Infrastructure Extraction — Plan (#12)

**Status:** Review complete, ready for implementation  
**Branch:** `yubikit-applets` (future work)  
**Created:** 2026-04-02  

---

## Problem

All 5 CLIs (ManagementTool, OathTool, FidoTool, OpenPgpTool, HsmAuthTool) share ~2600 LOC of duplicated patterns. No shared project exists — each CLI copy-pastes device selection, output formatting, argument parsing, and lifecycle management.

## Proposed Solution

New project: `Yubico.YubiKit.Cli.Shared`

```
Yubico.YubiKit.Cli.Shared/
├── src/
│   ├── Device/
│   │   ├── DeviceSelection.cs          (shared record)
│   │   ├── DeviceSelectorBase.cs       (abstract base, ~200 LOC)
│   │   ├── FormFactorFormatter.cs
│   │   └── ConnectionTypeFormatter.cs
│   ├── Output/
│   │   ├── OutputHelpers.cs            (Spectre.Console variant)
│   │   ├── PlainTextOutputHelpers.cs   (pipe-friendly alternative)
│   │   ├── ConfirmationPrompts.cs
│   │   └── PinPrompt.cs
│   ├── Cli/
│   │   ├── ArgumentParser.cs
│   │   ├── CommandHelper.cs            (YubiKeyManager lifecycle + CTS)
│   │   └── InteractiveMenuBuilder.cs
│   └── Yubico.YubiKit.Cli.Shared.csproj
```

---

## Shared Patterns Found (5/5 CLIs)

### 1. Device Selection (~1000 LOC duplicated)
- `DeviceSelection` record — identical across all 5
- `FindDevicesWithRetryAsync` — same retry loop
- `PromptForDeviceSelectionAsync` — identical interactive prompt
- `FormatFormFactor` / `FormatConnectionType` — identical switch statements
- **Variation:** Connection type filtering differs per CLI (SmartCard-only vs HidFido+SmartCard)

### 2. Output Helpers (~850 LOC duplicated)
- `WriteSuccess`, `WriteError`, `WriteWarning`, `WriteInfo` — identical
- `WriteKeyValue`, `WriteHex`, `WriteBoolValue` — identical
- `ConfirmDangerous`, `ConfirmDestructive` — identical
- `CreateTable` — 4/5 CLIs identical
- **Variation:** OathTool uses plain text (no Spectre.Console)

### 3. Argument Parser (~100 LOC, 3 CLIs)
- `HasFlag`, `GetArgValue`, `GetPositionalArgs` — identical logic

### 4. YubiKeyManager Lifecycle (~50 LOC, 5 CLIs)
- `StartMonitoring()` + `ShutdownAsync()` wrapper
- CancellationTokenSource + Console.CancelKeyPress boilerplate

---

## Extraction Phases

### Phase 1: Foundation (2-3 hours, Risk: Very Low)
1. `DeviceSelection` record
2. `ArgumentParser` (HasFlag, GetArgValue, GetPositionalArgs)
3. `FormFactorFormatter` + `ConnectionTypeFormatter`
4. `ConfirmationPrompts` (ConfirmDangerous, ConfirmDestructive)

**Impact:** ~250 LOC saved, zero risk

### Phase 2: UI/Output Layer (3-4 hours, Risk: Low)
5. `OutputHelpers` (core methods — WriteSuccess/Error/Warning/Info/KeyValue/Hex)
6. `PinPrompt` helpers
7. `CommandHelper` (YubiKeyManager lifecycle + CTS setup)

**Impact:** ~350 LOC saved, minimal risk

### Phase 3: Device Selection (4-6 hours, Risk: Medium)
8. `DeviceSelectorBase` abstract class
9. 5 CLI-specific subclasses (filtering by connection type)

**Impact:** ~1000 LOC saved, requires testing

### Phase 4: Optional
10. `InteractiveMenuBuilder` (builder pattern for Spectre.Console menus)
11. Generic `SessionHelper<TSession>` (device+session creation pattern)

---

## Inconsistencies to Normalize First

| Issue | CLIs Affected | Fix |
|-------|--------------|-----|
| OpenPgpTool uses `v` instead of `✓` for success | OpenPgpTool | Use U+2713 |
| OathTool lacks CancellationToken in entry points | OathTool | Add CTS pattern |
| Non-interactive auto-select varies (some prompt, some auto) | All | Virtual property on base |
| Exception handling: some continue, some exit | All | Standardize per-mode |
| Menu item styling: emoji vs plain text | 3 CLIs | Choose consistent style |

---

## Estimates

| Metric | Value |
|--------|-------|
| Total duplicated LOC | ~2,600 |
| Expected savings | ~2,200 (84%) |
| Total effort | 9-13 hours (phased) |
| Risk | Low (Phase 1-2), Medium (Phase 3) |

---

## Decision Log

- 2026-04-02: DevTeam review completed. All patterns documented. Deferred to future sprint.
