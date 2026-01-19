# CLAUDE.md - FIDO2 Tests

This file provides guidance for the FIDO2 module test infrastructure.

## Required Reading

**CRITICAL:** Read [`docs/TESTING.md`](../../../docs/TESTING.md) for test runner requirements. Key rule: **ALWAYS use `dotnet build.cs test` - NEVER use `dotnet test` directly.**

## Test Projects

- `Yubico.YubiKit.Fido2.UnitTests` - Unit tests for FIDO2 module
- `Yubico.YubiKit.Fido2.IntegrationTests` - Integration tests requiring YubiKey hardware
