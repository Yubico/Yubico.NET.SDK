# Architectural Review: CLI Monolith Merger Plan

**Reviewer**: Architect Agent
**Plan Under Review**: `Plans/joyful-rolling-pnueli.md`
**Date**: 2026-04-08

---

## Overall Rating: 6.5 / 10

The plan identifies the right problem (fragmentation across 7 tools with 4 different parsing approaches), proposes a reasonable target (unified CLI with Spectre.Console.Cli), and the audit section is excellent. But it has significant gaps in execution strategy, makes one premature decision that will cost you later, and underestimates the "example CLI" constraint that should shape every choice.

---

## Question-by-Question Assessment

### 1. Is Spectre.Console.Cli the right framework?

**Verdict: Yes, with reservations.**

Spectre.Console.Cli is the correct choice for this codebase. The evidence is strong:

- OpenPgpTool already uses it successfully, proving it works with YubiKey session lifecycle patterns
- The `OpenPgpCommand<TSettings>` base class pattern (device selection, session creation, error handling) translates directly to every applet
- Spectre.Console (non-CLI) is already a dependency across all tools for `AnsiConsole`, `SelectionPrompt`, markup rendering
- Auto-generated help at every tree depth eliminates the hand-written help strings that are already stale in FidoTool and ManagementTool

**The reservations**: Spectre.Console.Cli has a known limitation with async commands and global options propagation in branch nodes. The `CommandApp` model forces you to thread global settings through `CommandSettings` inheritance or use an `ITypeRegistrar`/DI approach. The plan says "GlobalSettings.cs" but does not address how `--serial` and `--transport` actually propagate to leaf commands. This is not trivial in Spectre.Console.Cli's architecture.

**Alternative considered**: `System.CommandLine` (now stable in .NET 10 era) has better middleware/pipeline support and native global option binding. But switching would abandon the working OpenPgpTool reference implementation and introduce a second framework dependency. Not worth it.

**What I'd change**: Add a section specifying the exact Spectre.Console.Cli pattern for global option propagation. The cleanest model is a `GlobalSettings` base class that all command settings inherit from, combined with a custom `ITypeRegistrar` that injects the parsed global values.

### 2. Global --serial / --transport flag placement

**Verdict: Before the applet, which is what the plan proposes. But the implementation mechanism is underspecified.**

```
yk --serial 12345678 fido info     (correct)
yk fido --serial 12345678 info     (wrong -- confusing)
```

The placement is right. The mechanism is the problem. In Spectre.Console.Cli, global options that appear before the first branch command require one of two approaches:

**Option A: Interceptor pattern** (recommended)
```csharp
app.Configure(config =>
{
    config.SetInterceptor(new GlobalOptionsInterceptor());
    // branches...
});
```
The interceptor parses `--serial` and `--transport` before command dispatch, stores them in a shared context. This is the cleanest because branch commands do not need to know about global options in their Settings classes.

**Option B: Settings inheritance**
Every `CommandSettings` subclass inherits from `GlobalSettings` with `--serial` and `--transport` properties. This pollutes every settings class and creates coupling.

The plan needs to specify which approach and how the selected device flows from global option parsing into the `OpenPgpCommand<TSettings>` base class (or its unified equivalent). Right now the base class calls `DeviceSelector.SelectDeviceAsync()` with no serial filter -- that wire-up is entirely missing.

### 3. Dropping interactive menus

**Verdict: Wrong call. Keep them, but make them the fallback, not the primary interface.**

The plan states: "Drop entirely in the monolith -- interactive menus made sense for standalone tools with no --help."

This misreads what the interactive menus actually provide. I looked at the FidoTool, PivTool, and ManagementTool implementations. The interactive menus serve three functions that `--help` does not replace:

1. **Guided discovery for beginners.** A user who types `yk` with no arguments and gets a wall of help text is worse off than one who gets a selection prompt. Hardware security key operations are high-stakes (wrong command can lock your device). Guided navigation prevents errors.

2. **Multi-step workflows.** The PivTool interactive menu lets users do "generate key, then import cert, then set PIN policy" in a single session without re-selecting the device each time. CLI mode requires three separate invocations, each with its own device selection overhead.

3. **These are example/reference CLIs.** The interactive mode is arguably the better teaching tool. Someone exploring the SDK for the first time will learn more from navigating a menu tree than from reading `--help` output.

**What I'd do instead**: Make the monolith CLI default to help when invoked with no arguments (`yk` shows help), but add `yk interactive` or `yk -i` as an explicit interactive mode that launches a unified menu across all applets. The `InteractiveMenuBuilder` in Cli.Shared is well-designed and already handles the loop/exit/error pattern. This costs almost nothing to preserve.

### 4. Phasing strategy

**Verdict: The order is mostly right but the rationale is wrong, and one sequencing risk is missed.**

The plan proposes: OpenPGP (move) -> FIDO -> OATH -> HsmAuth -> Management -> OTP -> PIV

**What's right:**
- OpenPGP first is correct -- it's already on Spectre.Console.Cli, so it validates the scaffold with minimal porting work
- PIV last is correct -- it's the most feature-rich and has the most interactive-mode dependency

**What's wrong:**
- The stated rationale is "in order of CLI completeness." This is backwards. You should port the **simplest CLI-only tools first** to validate the architecture, then tackle the complex interactive+CLI tools. The right order considering complexity:

  1. OpenPGP (move, validates scaffold)
  2. OATH (CLI-only, simple, validates porting from manual dispatch)
  3. HsmAuth (CLI + interactive, medium complexity)
  4. OTP (custom parser, validates porting from non-standard parsing)
  5. Management (all transports, validates transport abstraction)
  6. FIDO (HID + SmartCard, user presence, fingerprint enrollment -- highest protocol complexity)
  7. PIV (interactive-heavy, most commands, last)

**The missed sequencing risk:** FIDO requires HID transport. Every other applet works over SmartCard. When you port FIDO, you'll be forced to make the unified `DeviceSelectorBase` handle HID device selection, which means the unified base command class must support transport-specific connection types. If you discover an architectural problem here after porting 3 tools, you may need to refactor all of them. Port FIDO **before** the simpler SmartCard-only tools to flush out the transport abstraction early.

Actually, reconsidering: this argues for FIDO being second (after OpenPGP), not sixth. Port the hardest transport case early to validate the architecture, then do the simple ones.

**Revised order:**
1. OpenPGP (validates scaffold)
2. FIDO (validates multi-transport, user presence, HID)
3. OATH (simple CLI-only port)
4. OTP (custom parser port)
5. HsmAuth (medium complexity)
6. Management (all transports, but read-only so lower risk)
7. PIV (everything, last)

### 5. "Example CLIs" vs "shipping product CLI"

**Verdict: This is the most important architectural question and the plan does not address it adequately.**

The plan says "these are example/reference implementations" but then proposes a monolith that looks, walks, and quacks like a shipping product CLI (unified binary, `yk` command, deprecation of individual tools).

This tension matters because it drives every subsequent decision:

**If these are examples**, then:
- Each tool should remain in its module's `examples/` directory (where developers find them)
- The monolith should be *optional*, living alongside the per-tool examples
- Interactive mode is more valuable (teaching tool)
- The per-tool CLIs should NOT be deprecated -- they're documentation
- Code organization should prioritize readability over DRY

**If this is becoming a product CLI**, then:
- The monolith belongs in `src/Cli/YkTool/`
- Individual tools should be deprecated
- You need versioning, shell completions, error taxonomy, update checking
- You need CI/CD for the binary itself

The plan conflates these. My recommendation: **build the monolith as an additional example that happens to compose all applets, without deprecating the individual tools.** The per-module examples remain in `src/<Module>/examples/` as self-contained reference implementations. The monolith lives in `src/Cli/YkTool/` as a composition example. Both co-exist.

This also eliminates Phase 4 entirely (deprecation), which is the riskiest phase because it removes working code that developers rely on for reference.

### 6. What's architecturally missing from the plan?

Six significant gaps:

**A. Error taxonomy (mentioned but not designed).**
The plan notes "no structured error taxonomy" but does not propose one. Every applet throws different exception types. The monolith needs a unified error boundary. Minimum:

| Exit Code | Meaning |
|-----------|---------|
| 0 | Success |
| 1 | General error |
| 2 | Usage error (bad arguments) |
| 3 | Device not found |
| 4 | Authentication failed (wrong PIN/password) |
| 5 | Operation cancelled by user |
| 6 | Device communication error |
| 7 | Feature not supported (firmware too old) |

Without this, every command returns 0 or 1, and scripting is impossible.

**B. Shell completion.**
Spectre.Console.Cli does not provide shell completion out of the box. For a tool that manages hardware security keys, tab-completion of `yk fido credentials <TAB>` is high value. This should be a Phase 3 deliverable, not an afterthought.

**C. Output format flag (--format json|text|table).**
Every serious CLI supports machine-readable output. The `info` commands especially should support `--format json` for scripting. The `OutputHelpers` in Cli.Shared are all Spectre.Console markup -- there's no structured output path.

**D. PIN/credential prompting in non-interactive mode.**
FidoTool accepts `--pin` on the command line (insecure, visible in process list). The plan should specify whether PINs are accepted via stdin, environment variable, or command-line flag, and document the security implications. The `PinPrompt.cs` in Cli.Shared suggests interactive prompting exists, but the non-interactive path is undefined.

**E. Testing strategy for the monolith.**
The plan says "All existing per-tool tests pass unchanged (business logic untouched)." This is true for the business logic layer but false for the CLI layer itself. Who tests that `yk fido info` actually dispatches correctly? Who tests that `--serial` filtering works? The plan needs a test strategy for the CLI routing/dispatch layer, even if it's "we test this manually."

**F. Build integration.**
How does the monolith CLI build? Is it added to the solution? Is it a `dotnet tool`? Does `dotnet build.cs build` include it? None of this is specified.

### 7. Naming and structural concerns

**Location `src/Cli/YkTool/`:** Acceptable, but inconsistent with the existing pattern where examples live under their module. If this is a composition example, consider `src/Cli/examples/YkTool/` to parallel the other tools.

**Name `yk`:** Good choice -- matches `ykman` convention, short, memorable. But consider `yubikit` instead to avoid confusion with `ykman` (the official Python CLI). Users may expect `yk` to be `ykman`. As a .NET SDK example, a distinct name prevents confusion.

**Project name `Yubico.YubiKit.Cli`:** This collides conceptually with `Yubico.YubiKit.Cli.Shared`. Consider `Yubico.YubiKit.Cli.Tool` or `Yubico.YubiKit.Cli.YkTool`.

---

## Top 3 Changes Before Implementation

### 1. Port FIDO second (not sixth) to validate multi-transport architecture early

FIDO is the only applet requiring HID transport. If you port 5 SmartCard-only tools first, you'll build a unified base command class that assumes SmartCard. When FIDO arrives, you'll discover the abstraction is wrong and refactor everything. Port FIDO immediately after OpenPGP to force the transport abstraction to be correct from day one.

### 2. Do not drop interactive menus -- make them an explicit mode

Replace "drop menus entirely" with "add `yk interactive [applet]` command that launches the existing menu infrastructure." This preserves the teaching/discovery value, costs near-zero effort (the `InteractiveMenuBuilder` already exists), and avoids alienating users who depend on guided navigation for high-stakes operations like PIV key management or FIDO reset.

### 3. Specify the global options propagation mechanism before writing any code

The `--serial` and `--transport` flags are the architectural linchpin. Every command needs them. The plan must specify exactly how they flow through Spectre.Console.Cli's type system into the device selection layer. Write a spike (proof of concept) that demonstrates:
- `yk --serial 12345 fido info` selects the right device
- `yk --transport smartcard fido info` forces SmartCard transport
- The base command class receives the parsed global options without settings inheritance pollution

If this spike reveals that Spectre.Console.Cli cannot cleanly support this pattern, you need to know before porting 7 tools, not after.

---

## Summary Assessment

| Aspect | Rating | Notes |
|--------|--------|-------|
| Problem identification | 9/10 | Excellent audit of inconsistencies across 7 tools |
| Target architecture | 7/10 | Right direction, but underspecified on key mechanisms |
| Framework choice | 8/10 | Spectre.Console.Cli is correct given existing usage |
| Migration strategy | 5/10 | Wrong sequencing, missing risk mitigation |
| Completeness | 4/10 | Six significant architectural gaps |
| "Example" alignment | 5/10 | Plan treats examples as a product without acknowledging the tension |

The plan is a solid starting point that needs refinement before implementation begins. The audit section is genuinely excellent. The migration strategy needs the three changes above plus the six gap-fills before any code is written.
