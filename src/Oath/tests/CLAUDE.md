# CLAUDE.md - OATH Tests

This file provides guidance for the OATH module test infrastructure.

## Required Reading

**CRITICAL:** Read [`docs/TESTING.md`](../../../docs/TESTING.md) for test runner requirements. Key rule: **ALWAYS use `dotnet toolchain.cs test` - NEVER use `dotnet test` directly.**

## Test Projects

- `Yubico.YubiKit.Oath.UnitTests` - Unit tests for OATH module
- `Yubico.YubiKit.Oath.IntegrationTests` - Integration tests requiring YubiKey hardware

## Terminal reset setup

Successful OATH RESET ends plain and SCP sessions. `WithOathSessionAsync(resetBeforeUse: true)` completes
reset/disposal on a separate owned connection before opening the body session on a fresh connection.
Never reset and pass that ended session to the body, or expect a post-reset SELECT on its connection.
`resetBeforeUse: false` skips setup, not the normal session/connection lifetime ownership.
Offline lifecycle/cleanup cases live in the integration project without `RequiresHardware`; select them
with `Category!=RequiresHardware&Category!=RequiresUserPresence`. Do not count unexecuted live SCP tests as evidence.
