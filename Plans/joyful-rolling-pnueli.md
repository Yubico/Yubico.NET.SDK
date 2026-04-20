# YkTool Unified CLI -- Execution Guide

This is the authoritative plan for building the `yk` unified CLI tool. The scaffold is complete. What remains is 7 sequential DevTeam iterations to port each applet's commands.

---

## Scaffold Summary (Phase 1 -- COMPLETE)

### What Was Built

| File | Purpose |
|------|---------|
| `src/Cli.Commands/src/Yubico.YubiKit.Cli.Commands.csproj` | Shared commands library. Empty shell. DevTeam populates it. References all 7 applet assemblies + `Cli.Shared`. |
| `src/Cli/YkTool/Yubico.YubiKit.Cli.YkTool.csproj` | Monolith executable. Outputs `yk` binary. References `Cli.Commands` + `Cli.Shared` + `Core` + `Management`. |
| `src/Cli/YkTool/Program.cs` | `CommandApp` wiring. 7 branches stubbed (`management`, `fido`, `oath`, `openpgp`, `piv`, `hsm-auth`, `otp`). Each has an `info` stub command. |
| `src/Cli/YkTool/Infrastructure/ExitCode.cs` | Constants: 0=Success, 1=GenericError, 3=DeviceNotFound, 4=AuthenticationFailed, 5=UserCancelled, 7=FeatureUnsupported. |
| `src/Cli/YkTool/Infrastructure/GlobalSettings.cs` | `CommandSettings` subclass: `--serial`/`-s`, `--transport`, `-i`/`--interactive`. Every command inherits this. |
| `src/Cli/YkTool/Infrastructure/YkDeviceContext.cs` | Enriched context: `IYubiKey Device`, `DeviceSelection Selection`, `DeviceInfo? Info`, `string DisplayBanner`. |
| `src/Cli/YkTool/Infrastructure/YkDeviceSelector.cs` | `DeviceSelectorBase` subclass. Takes `ConnectionType[]` from each command's `AppletTransports`. |
| `src/Cli/YkTool/Infrastructure/YkCommandInterceptor.cs` | `ICommandInterceptor`. Currently no-op. Registered in `CommandApp`. |
| `src/Cli/YkTool/Infrastructure/YkCommandBase.cs` | Abstract base. Sealed `ExecuteAsync` handles: start monitoring, select device, enrich via `GetDeviceInfoAsync`, call `ExecuteCommandAsync`, shutdown. |
| `src/Cli/YkTool/Commands/Stubs/*.cs` | 7 stub info commands (one per applet). Each extends `YkCommandBase<GlobalSettings>`. Replaced during porting. |

### Architecture

```
User runs: yk --serial 12345 oath accounts list

CommandApp (Program.cs)
  -> YkCommandInterceptor.Intercept() [no-op currently]
  -> OathAccountsListCommand.ExecuteAsync() [sealed in YkCommandBase]
       -> YubiKeyManager.StartMonitoring()
       -> YkDeviceSelector.SelectDeviceAsync() [filtered by AppletTransports]
       -> device.GetDeviceInfoAsync() [ManagementSession enrichment]
       -> ExecuteCommandAsync(context, settings, YkDeviceContext) [your code]
       -> YubiKeyManager.ShutdownAsync()
```

---

## The Command Pattern

Every command follows this exact structure. No exceptions.

### Step 1: Settings class in Cli.Commands

File: `src/Cli.Commands/src/<Applet>/<CommandName>Settings.cs`
Namespace: `Yubico.YubiKit.Cli.Commands.<Applet>`

```csharp
using System.ComponentModel;
using Spectre.Console.Cli;
using Yubico.YubiKit.Cli.YkTool.Infrastructure;

namespace Yubico.YubiKit.Cli.Commands.Oath;

public sealed class AccountsListSettings : GlobalSettings
{
    [CommandOption("--period <PERIOD>")]
    [Description("Only show TOTP accounts with this period (in seconds).")]
    public int? Period { get; set; }
}
```

Rules:
- ALWAYS extend `GlobalSettings` (not `CommandSettings`). This ensures `--serial`, `--transport`, `-i` are available.
- Settings class lives in `Cli.Commands`, not in `YkTool`. Both the monolith and the individual tool reference it.
- Use `{ get; set; }` for optional parameters, `{ get; init; } = ""` for required arguments.
- Mark arguments with `[CommandArgument(position, "<NAME>")]`, options with `[CommandOption("--name <VALUE>")]`.

### Step 2: Command class in Cli.Commands

File: `src/Cli.Commands/src/<Applet>/<CommandName>Command.cs`
Namespace: `Yubico.YubiKit.Cli.Commands.<Applet>`

```csharp
using Spectre.Console.Cli;
using Yubico.YubiKit.Cli.Shared.Output;
using Yubico.YubiKit.Cli.YkTool.Infrastructure;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Cli.Commands.Oath;

public sealed class OathAccountsListCommand : YkCommandBase<AccountsListSettings>
{
    protected override ConnectionType[] AppletTransports =>
        [ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context,
        AccountsListSettings settings,
        YkDeviceContext deviceContext)
    {
        // Open applet session
        await using var session = await deviceContext.Device.CreateOathSessionAsync();

        // Business logic here -- port from the individual tool's command
        var accounts = await session.ListAccountsAsync();

        foreach (var account in accounts)
        {
            OutputHelpers.WriteKeyValue(account.Name, account.Issuer ?? "");
        }

        return ExitCode.Success;
    }
}
```

Rules:
- Extend `YkCommandBase<TSettings>` where `TSettings` is your settings class.
- Set `AppletTransports` to the connection types this applet needs. Reference the individual tool's `DeviceSelector` for the correct values.
- DO NOT do device selection or monitoring -- `YkCommandBase` handles that.
- DO NOT catch broad exceptions at the command level -- `YkCommandBase` handles that.
- Use `deviceContext.Device` to open your applet session.
- Use `deviceContext.Info` for feature gating (firmware version, capabilities).
- Return `ExitCode` constants, never raw integers.
- Use `OutputHelpers` from `Cli.Shared` for all output (never raw `Console.WriteLine`).

### Step 3: Wire in Program.cs

File: `src/Cli/YkTool/Program.cs`

```csharp
config.AddBranch("oath", oath =>
{
    oath.SetDescription("TOTP/HOTP one-time password credential management.");

    oath.AddCommand<OathInfoCommand>("info")
        .WithDescription("Display OATH application status.");

    oath.AddBranch("accounts", accounts =>
    {
        accounts.SetDescription("Manage OATH accounts.");
        accounts.AddCommand<OathAccountsListCommand>("list")
            .WithDescription("List all stored accounts.");
        accounts.AddCommand<OathAccountsAddCommand>("add")
            .WithDescription("Add a new OATH account.");
        accounts.AddCommand<OathAccountsDeleteCommand>("delete")
            .WithDescription("Remove an account.");
    });

    oath.AddBranch("access", access =>
    {
        access.SetDescription("Manage OATH password.");
        access.AddCommand<OathAccessChangePasswordCommand>("change-password")
            .WithDescription("Change or remove the OATH password.");
    });
});
```

Rules:
- Remove the stub command when replacing it with the real one.
- Mirror the individual tool's command hierarchy. Look at its `Program.cs` or dispatch logic.
- Keep descriptions concise (one line, no period for short phrases).

### Step 4: Refactor the individual tool

The individual tool (`src/<Module>/examples/<Tool>/`) should be refactored to import commands from `Cli.Commands` instead of having its own implementations. This is a secondary goal -- do it if straightforward, skip if complex.

What this means in practice:
- The tool's base command class (e.g., `OpenPgpCommand<T>`) stays as-is (it has its own device selection lifecycle).
- The business logic inside each command can be extracted to shared helpers in `Cli.Commands` if feasible.
- The tool's `Program.cs` stays as its own entry point.
- Do NOT break the individual tool. If refactoring is risky, just port the logic to `Cli.Commands` and leave the individual tool untouched.

---

## DevTeam Iteration Protocol

### How to Invoke

Kick off each iteration with the `/DevTeam Ship` skill:

```
/DevTeam Ship Port the <Applet> applet commands into the unified yk CLI.
Read the full plan at Plans/joyful-rolling-pnueli.md and the progress file at Plans/yk-cli-progress.md before writing any code.
Follow the DevTeam Iteration Protocol exactly.
Source: src/<Module>/examples/<Tool>/
Target: src/Cli.Commands/src/<Applet>/ and src/Cli/YkTool/Program.cs
After completion, update Plans/yk-cli-progress.md with checked boxes.
```

Each iteration ports one applet. The agent receives this plan and executes these steps in order.

### Before Writing Code

1. Read this plan (you're doing that now).
2. Read the individual tool's source to understand its full command surface:
   - `src/<Module>/examples/<Tool>/Program.cs` -- dispatch/routing
   - `src/<Module>/examples/<Tool>/Commands/*.cs` -- business logic
   - `src/<Module>/examples/<Tool>/Cli/Commands/*.cs` -- for OpenPgpTool (Spectre pattern)
3. Read the prior ported applet's commands in `src/Cli.Commands/src/` as canonical reference for the pattern.
4. Read `src/Cli/YkTool/Program.cs` to see current wiring.

### Writing Code

5. Create `src/Cli.Commands/src/<Applet>/` directory.
6. For each command in the individual tool:
   a. Create a settings class (if the command has arguments/options beyond `GlobalSettings`).
   b. Create a command class extending `YkCommandBase<TSettings>`.
   c. Port the business logic from the individual tool's command, adapting:
      - Device selection removed (handled by base)
      - `selection.Device` becomes `deviceContext.Device`
      - Exit codes use `ExitCode` constants
      - Output uses `OutputHelpers` from `Cli.Shared`
7. Wire all commands in `Program.cs`, replacing the stub.
8. Remove the stub file from `Commands/Stubs/`.

### After Writing Code

9. Build: `dotnet toolchain.cs build` -- must compile with zero warnings.
10. Verify `yk --help` shows the applet with all subcommands.
11. Verify `yk <applet> --help` shows correct descriptions.
12. Update `Plans/yk-cli-progress.md` -- check off completed items.

---

## The 7 Iterations

### Iteration 1: Management

**Source:** `src/Management/examples/ManagementTool/`
**Commands to port:** `info`, `config`
**Transports:** `[SmartCard, HidFido, HidOtp]` (all transports)
**Notes:**
- `InfoCommand` uses `DeviceInfoQuery.GetDeviceInfoAsync(session, ct)` -- port this logic, but leverage `deviceContext.Info` which already has the enriched data.
- `ConfigCommand` uses `session.SetDeviceConfigAsync()`.
- Check `ManagementTool/Program.cs` for the full dispatch map.
- The stub `ManagementInfoStub.cs` already shows a partial implementation -- expand it into the real command.
- Interactive menu: check if `ManagementTool` has an `InteractiveMenuBuilder` setup; port that too.

### Iteration 2: OpenPGP

**Source:** `src/OpenPgp/examples/OpenPgpTool/`
**Commands to port:** `info`, `reset`, `access/*` (5 commands), `keys/*` (4 commands), `certificates/*` (3 commands)
**Transports:** `[SmartCard]`
**Notes:**
- Already on Spectre.Console.Cli -- closest to the target pattern.
- `OpenPgpCommand<T>` base is analogous to `YkCommandBase<T>`. The ported commands just swap the base class and session creation.
- Settings classes already exist as inner classes (`InfoCommand.Settings`). Move them to `Cli.Commands` as standalone files.
- Helper methods (`ParseKeyRef`, `FormatKeyRef`, `GetPin`, `ConfirmAction`) from `OpenPgpCommand<T>` -- move to a shared helper in `Cli.Commands/src/OpenPgp/OpenPgpHelpers.cs`.
- OpenPgpTool has its own `OutputHelpers` in `Cli/Output/` -- these may duplicate `Cli.Shared.OutputHelpers`. Prefer `Cli.Shared` versions.

### Iteration 3: OATH

**Source:** `src/Oath/examples/OathTool/`
**Commands to port:** `info`, `reset`, `access` (rename to `access change-password`), `accounts` (list, add, calculate, delete, rename)
**Transports:** `[SmartCard]`
**Notes:**
- Manual dispatch (`DispatchAsync` method) -- parse the command tree from the source.
- `accounts` is the OATH-specific term (not `credentials`). Keep it.
- `access change` should become `access change-password` for clarity.
- OATH accounts have `calculate` (compute current OTP code) -- this is a key command.

### Iteration 4: HsmAuth

**Source:** `src/YubiHsm/examples/HsmAuthTool/`
**Commands to port:** `info`, `reset`, `access/*`, `credentials/*`
**Transports:** `[SmartCard]`
**Notes:**
- Manual dispatch. Read the switch tree in `Program.cs` or main dispatch.
- Uses `credentials` terminology.
- Relatively small command surface.

### Iteration 5: OTP

**Source:** `src/YubiOtp/examples/OtpTool/`
**Commands to port:** `info`, `swap`, `delete`, `chalresp`, `hotp`, `static`, `yubiotp`, `calculate`, `ndef`, `settings`
**Transports:** `[SmartCard, HidOtp]` (verify from OtpTool's DeviceSelector)
**Notes:**
- Custom `CliOptions.Parse(args)` -- must read to understand all arguments.
- 10 commands -- largest surface by count.
- Each command file is already well-isolated in `Commands/`.

### Iteration 6: PIV

**Source:** `src/Piv/examples/PivTool/`
**Commands to port:** Full PIV surface (many operations are currently interactive-menu-only)
**Transports:** `[SmartCard]`
**Notes:**
- Most commands are behind the interactive menu, not CLI arguments yet.
- Must read the interactive menu to understand the full feature set.
- Build CLI surface that matches `ykman piv` commands: `info`, `reset`, `access/*` (change-pin, change-puk, change-management-key), `keys/*` (generate, import, export, attest), `certificates/*` (generate, import, export, delete), `objects/*`.
- Largest effort of the 7 iterations.

### Iteration 7: FIDO

**Source:** `src/Fido2/examples/FidoTool/`
**Commands to port:** `info`, `reset`, `access/*` (set-pin, change-pin, verify), `credentials/*` (list, delete), `fingerprints/*` (list, add, rename, delete)
**Transports:** `[HidFido, SmartCard]` (FIDO prefers HID)
**Notes:**
- Manual dispatch with `HasFlag`/`GetOption` parsing.
- User presence required for many operations -- mark these with clear warnings in descriptions.
- E2E testing requires human physical touch on YubiKey gold contact. All automated testing happens before this iteration.

---

## Naming Conventions

| Item | Convention | Example |
|------|-----------|---------|
| Settings file | `<Verb><Noun>Settings.cs` | `AccountsListSettings.cs` |
| Command file | `<Applet><Verb><Noun>Command.cs` | `OathAccountsListCommand.cs` |
| Settings namespace | `Yubico.YubiKit.Cli.Commands.<Applet>` | `Yubico.YubiKit.Cli.Commands.Oath` |
| Applet directory | `src/Cli.Commands/src/<Applet>/` | `src/Cli.Commands/src/Oath/` |
| Helper file | `<Applet>Helpers.cs` | `OpenPgpHelpers.cs` |

---

## Transports Reference

| Applet | `AppletTransports` value |
|--------|-------------------------|
| Management | `[SmartCard, HidFido, HidOtp]` |
| OpenPGP | `[SmartCard]` |
| OATH | `[SmartCard]` |
| HsmAuth | `[SmartCard]` |
| OTP | `[SmartCard, HidOtp]` |
| PIV | `[SmartCard]` |
| FIDO | `[HidFido, SmartCard]` |

Verify against each individual tool's `DeviceSelector.SupportedConnectionTypes` before using.

---

## Verification Checklist (Per Iteration)

After each iteration, confirm:

- [ ] All commands from the individual tool are ported to `src/Cli.Commands/src/<Applet>/`
- [ ] All commands are wired in `src/Cli/YkTool/Program.cs`
- [ ] The corresponding stub file is deleted from `Commands/Stubs/`
- [ ] `dotnet toolchain.cs build` compiles with zero warnings
- [ ] `yk --help` lists the applet
- [ ] `yk <applet> --help` lists all subcommands with descriptions
- [ ] `Plans/yk-cli-progress.md` is updated with checked items
- [ ] Feature parity documented: any commands in the individual tool that were NOT ported, and why

---

## Final Testing Sequence (After All 7 Iterations)

Run in this order. Never run two applets simultaneously (hardware constraint).

**Phase A -- Automated (no human required):**
1. Management: `yk management info`
2. OpenPGP: `yk openpgp info`
3. OATH: `yk oath info`, `yk oath accounts list`
4. HsmAuth: `yk hsm-auth info`, `yk hsm-auth credentials list`
5. OTP: `yk otp info`
6. PIV: `yk piv info`

**Phase B -- Human-required (run last):**
7. FIDO: `yk fido info`, `yk fido credentials list` (requires PIN + physical touch)

---

## Key Files Reference

```
src/Cli.Commands/src/                          <- Shared commands (DevTeam populates)
src/Cli/YkTool/Program.cs                      <- CommandApp wiring
src/Cli/YkTool/Infrastructure/YkCommandBase.cs <- Abstract base class
src/Cli/YkTool/Infrastructure/GlobalSettings.cs <- Global CLI flags
src/Cli/YkTool/Infrastructure/ExitCode.cs      <- Exit code constants
src/Cli/YkTool/Infrastructure/YkDeviceContext.cs <- Enriched device context
src/Cli/YkTool/Infrastructure/YkDeviceSelector.cs <- Transport-aware selector
Plans/yk-cli-progress.md                       <- Progress tracker (update after each iteration)
```
