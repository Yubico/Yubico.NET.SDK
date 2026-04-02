# CLAUDE.md - YubiOTP Tests

This file provides guidance for the YubiOTP module test infrastructure.

## Required Reading

**CRITICAL:** Read [`docs/TESTING.md`](../../../docs/TESTING.md) for test runner requirements. Key rule: **ALWAYS use `dotnet build.cs test` - NEVER use `dotnet test` directly.**

## Test Projects

- `Yubico.YubiKit.YubiOtp.UnitTests` - Unit tests for YubiOTP module
- `Yubico.YubiKit.YubiOtp.IntegrationTests` - Integration tests requiring YubiKey hardware
