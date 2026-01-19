# CLAUDE.md - PIV Tests

This file provides guidance for the PIV module test infrastructure.

## Required Reading

**CRITICAL:** Read [`docs/TESTING.md`](../../../docs/TESTING.md) for test runner requirements. Key rule: **ALWAYS use `dotnet build.cs test` - NEVER use `dotnet test` directly.**

## Test Projects

- `Yubico.YubiKit.Piv.UnitTests` - Unit tests for PIV module
- `Yubico.YubiKit.Piv.IntegrationTests` - Integration tests requiring YubiKey hardware
