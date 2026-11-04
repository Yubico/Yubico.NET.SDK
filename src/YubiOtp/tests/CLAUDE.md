# CLAUDE.md - YubiOTP Tests

This file provides guidance for the YubiOTP module test infrastructure.

## Required Reading

**CRITICAL:** Read [`docs/TESTING.md`](../../../docs/TESTING.md) for test runner requirements. Key rule: **ALWAYS use `dotnet toolchain.cs test` - NEVER use `dotnet test` directly.**

## Test Projects

- `Yubico.YubiKit.YubiOtp.UnitTests` - Unit tests for YubiOTP module
- `Yubico.YubiKit.YubiOtp.IntegrationTests` - Integration tests requiring YubiKey hardware

## Effects and evidence

Use dedicated authorized test keys. Programming overwrites secrets that cannot be read back for restoration.
Most programming tests use slot 2 and later delete it; swap touches both slots and the protection tests
replace slot 1 too. Existing cleanup is not uniformly in finally, and a second swap only attempts to restore
placement. Inspect each complete test and fixture before execution. New tests should use finally cleanup
and prefer slot 2 where their purpose allows it.

Read assertions, not only test names: configured status does not establish keyboard text or HOTP digit count;
response length does not establish HMAC correctness; NDEF writes do not establish NFC read-back. The known-answer
HMAC tests compare exact host-computed responses when executed. Fake transport tests establish request/response
handling, not hardware command support. Post-disposal GetConfig rejection is not an erasure assertion;
SlotConfigurationEncapsulationTests separately inspect the actual private arrays.
