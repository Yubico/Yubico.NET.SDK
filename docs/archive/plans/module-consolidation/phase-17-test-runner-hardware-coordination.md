# Phase 17: Test Runner And Hardware Coordination

This artifact records the active coordination policy for xUnit v3 filters and FIDO2/WebAuthn hardware checks.

## xUnit v3 Focused Filters

- Always invoke tests through `dotnet toolchain.cs test`.
- xUnit v3/Microsoft.Testing.Platform projects are detected with `<UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>`.
- VSTest-style filters are translated by `toolchain.cs` to MTP-native flags.
- Positive filters are preflighted with `--list-tests`; if a selected xUnit v3 project has no matching tests, that project is marked `no matching tests`.
- If every selected xUnit v3 project preflighted by the positive filter has no matching tests, `toolchain.cs` fails clearly with `No tests matched the specified filter`.
- In mixed unit+integration selections, non-matching projects do not execute any hardware test bodies when another selected project has matching tests.
- Exclusion filters such as `Category!=RequiresUserPresence` still apply to the real run.

## Lane Classification

| Lane | Examples | Agent-runnable | Required filter/coordination |
|------|----------|----------------|------------------------------|
| Read-only FIDO2/WebAuthn smoke | FIDO2 `GetInfo`, WebAuthn client construction/unit tests | Yes | `dotnet toolchain.cs test --project <Module>` or `--smoke` for integration |
| User Presence | `MakeCredential`, `GetAssertion`, `previewSign` registration/authentication | No by default | `[Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]`; run only after human approval and presence |
| User Verification / PIN | PIN-token, UV-preferred/required, biometric enrollment, PIN normalization | No by default | Human-approved test PIN/device state; may also require `RequiresUserPresence` |
| Reset/destructive | FIDO2 reset, delete-all credential cleanup beyond scoped cleanup | No | Human-approved destructive run only |
| Insert/remove/touch timing | Reset power-cycle window, insertion/removal tests | No | Human-coordinated timing only |

## Agent-Runnable Defaults

```bash
# Focused unit tests; toolchain handles xUnit v2/v3 differences.
dotnet toolchain.cs -- test --project Fido2 --filter "Method~ExtensionBuilder"

# Mixed unit+integration selection; focused unit tests run and non-matching integration tests do not execute hardware bodies.
dotnet toolchain.cs -- test --integration --project Fido2 --filter "Method~ExtensionBuilder"

# Integration smoke; excludes Slow and RequiresUserPresence.
dotnet toolchain.cs -- test --integration --project Fido2 --smoke
dotnet toolchain.cs -- test --integration --project WebAuthn --smoke

# Explicit equivalent when not using --smoke.
dotnet toolchain.cs -- test --integration --project WebAuthn --filter "Category!=RequiresUserPresence"
```

## Human-Coordinated Commands

These commands are examples only. They require a human physically present, a selected device, and explicit approval immediately before execution.

```bash
dotnet toolchain.cs -- test --integration --project Fido2 --filter "Category=RequiresUserPresence"
dotnet toolchain.cs -- test --integration --project WebAuthn --filter "Category=RequiresUserPresence"
```

Before running human-coordinated checks, record:

- target module and exact filter
- target serial or allow-list selection evidence
- whether the run may mutate FIDO2 credentials or PIN state
- whether reset, insert/remove, or destructive cleanup is involved
- human approval and whether the human will touch/verify at prompts

## Explicit Phase 17 Skips

- No UP/UV integration tests run by the agent.
- No FIDO2 reset tests run by the agent.
- No PIN retry-limit or blocked-PIN tests run by the agent.
- No insert/remove timing tests run by the agent.
