# CLAUDE.md - OATH Tests

This file provides guidance for the OATH module test infrastructure.

## Required Reading

**CRITICAL:** Read [`docs/TESTING.md`](../../../docs/TESTING.md) for test runner requirements. Key rule: **ALWAYS use `dotnet toolchain.cs test` - NEVER use `dotnet test` directly.**

## Test Projects

- `Yubico.YubiKit.Oath.UnitTests` - Unit tests for OATH module
- `Yubico.YubiKit.Oath.IntegrationTests` - Integration tests requiring YubiKey hardware

## Reset setup

Successful OATH RESET reselects and refreshes the same session, including re-establishing SCP if configured.
`WithOathSessionAsync(resetBeforeUse: true)` still performs test setup on a separate owned connection and
disposes it before opening the body session. This is a fixture choice, not a requirement of `ResetAsync`.
`resetBeforeUse: false` skips setup, not the normal session/connection lifetime ownership.
Offline lifecycle/cleanup cases live in the integration project without `RequiresHardware`; select them
with `Category!=RequiresHardware&Category!=RequiresUserPresence`. Do not count unexecuted live SCP tests as evidence.
