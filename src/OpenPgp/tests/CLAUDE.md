# CLAUDE.md - OpenPGP Tests

This file provides guidance for the OpenPGP module test infrastructure.

## Required Reading

**CRITICAL:** Read [`docs/TESTING.md`](../../../docs/TESTING.md) for test runner requirements. Key rule: **ALWAYS use `dotnet toolchain.cs test` - NEVER use `dotnet test` directly.**

## Test Projects

- `Yubico.YubiKit.OpenPgp.UnitTests` - Unit tests for OpenPGP module
- `Yubico.YubiKit.OpenPgp.IntegrationTests` - Integration tests requiring YubiKey hardware

## Interpret the assertions

`WithOpenPgpSessionAsync` optionally resets in a separate plain session, disposes it, then creates the action's
session with any SCP parameters. It awaits the action before synchronous session disposal and leaves post-action
state in place. This setup does not exercise resetting an SCP session or restoring its channel options.
Start a device case from known state; do not add post-failure card mutations in teardown.
`Reset_WithScp03_ReauthenticatesUsingOriginalKeysOrEndsSession` covers a separate software fixture:
it checks a fresh challenge, nondefault keys, authenticated follow-up and terminal refresh failure.
It does not establish physical behavior.

Keep comments within each test's actual oracle:

- Wire initialization checks command membership; dedicated key-generation tests check their asserted operation order.
- KDF reads verify independent caller ownership; write failure checks erasure of the retained encoded payload.
  Those assertions do not establish native transport cleanup or a multi-exchange workflow drain.
- Signature-helper tests retain actual response arrays for transfer/zeroing assertions. The decrypt completion-callback
  failure test checks exception identity and outcome, not raw-array erasure. Neither establishes cleanup of the full
  transport/chaining/SCP producer graph.
- The PIN status-lookup cancellation fake cancels its own task, independently of the token it receives. Its exception
  propagation is not evidence that an admitted native SmartCard exchange can be interrupted.
- SHA-512 KDF coverage checks length. Literal known-answer comparisons and same-logic SHA-256 calculations have
  different independence; libgcrypt-named tests execute SDK derivation, not libgcrypt.
- Host signature/plaintext/shared-secret equality is stronger than nonempty/length checks. Public-point
  import checks remain separate from the RFC 8032 signature and RFC 7748 agreement cases. The attestation
  SPKI comparison checks target binding; a separate signature check uses the card's reported ATT issuer.
  Neither establishes manufacturer trust.
- Unverify's signing-failure case leaves signature policy uncontrolled and checks no other-operation state. Reset's
  three retry-counter assertions establish no complete erasure postcondition.

Firmware early returns and provider skips remain exclusions, not executed body evidence. Running a test suite does
not by itself qualify another transport, device family, provider or operating system.
