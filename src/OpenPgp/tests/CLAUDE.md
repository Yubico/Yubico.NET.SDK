# CLAUDE.md - OpenPGP Tests

This file provides guidance for the OpenPGP module test infrastructure.

## Required Reading

**CRITICAL:** Read [`docs/TESTING.md`](../../../docs/TESTING.md) for test runner requirements. Key rule: **ALWAYS use `dotnet build.cs test` - NEVER use `dotnet test` directly.**

## Test Projects

- `Yubico.YubiKit.OpenPgp.UnitTests` - Unit tests for OpenPGP module
- `Yubico.YubiKit.OpenPgp.IntegrationTests` - Integration tests requiring YubiKey hardware
