# CLAUDE.md - YubiHSM Tests

This file provides guidance for the YubiHSM module test infrastructure.

## Required Reading

**CRITICAL:** Read [`docs/TESTING.md`](../../../docs/TESTING.md) for test runner requirements. Key rule: **ALWAYS use `dotnet build.cs test` - NEVER use `dotnet test` directly.**

## Test Projects

- `Yubico.YubiKit.YubiHsm.UnitTests` - Unit tests for YubiHSM module
- `Yubico.YubiKit.YubiHsm.IntegrationTests` - Integration tests requiring YubiHSM hardware
