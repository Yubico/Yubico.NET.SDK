# CLAUDE.md - Management Tests

This file provides guidance for the Management module test infrastructure.

## Required Reading

**CRITICAL:** Read [`docs/TESTING.md`](../../../docs/TESTING.md) for test runner requirements. Key rule: **ALWAYS use `dotnet build.cs test` - NEVER use `dotnet test` directly.**

## Test Projects

- `Yubico.YubiKit.Management.UnitTests` - Unit tests for Management module
- `Yubico.YubiKit.Management.IntegrationTests` - Integration tests requiring YubiKey hardware
