# CLAUDE.md - OATH Tests

This file provides guidance for the OATH module test infrastructure.

## Required Reading

**CRITICAL:** Read [`docs/TESTING.md`](../../../docs/TESTING.md) for test runner requirements. Key rule: **ALWAYS use `dotnet build.cs test` - NEVER use `dotnet test` directly.**

## Test Projects

- `Yubico.YubiKit.Oath.UnitTests` - Unit tests for OATH module
- `Yubico.YubiKit.Oath.IntegrationTests` - Integration tests requiring YubiKey hardware
