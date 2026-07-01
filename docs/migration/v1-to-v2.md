# YubiKey SDK v1 to YubiKit v2 Migration Guide

This document reflects the v2 SDK state on branch `yubikit` as of commit `e348013685d92a6a665cd0b8bd7e8b05850fddd5`. Later pull requests targeting `yubikit` update this guide incrementally.

This is an initial migration snapshot. It records high-confidence migration areas and marks low-level or behavior-sensitive cases for manual review.

## Package and Namespace Split

The v2 SDK uses `Yubico.YubiKit.*` packages and namespaces instead of the v1 `Yubico.YubiKey.*` and `Yubico.Core` shape.

High-level package guidance:

- Core device, transport, connection, APDU, logging, and platform infrastructure move to `Yubico.YubiKit.Core`.
- Management interfaces move to `Yubico.YubiKit.Management`.
- Application features are split by applet: `Yubico.YubiKit.Piv`, `Yubico.YubiKit.Fido2`, `Yubico.YubiKit.Oath`, `Yubico.YubiKit.YubiOtp`, `Yubico.YubiKit.OpenPgp`, `Yubico.YubiKit.SecurityDomain`, and `Yubico.YubiKit.YubiHsm`.

Treat package and namespace changes as assisted migrations until the specific v1 type or member mapping is present in `v1-to-v2-map.yml`.

## Device Discovery and Connections

V2 separates device discovery, device identity, connection factories, and protocol handling more explicitly than v1. Migration work usually starts by replacing direct v1 device enumeration or static discovery calls with the v2 device repository and connection abstractions in `Yubico.YubiKit.Core`.

Review code that assumes:

- One global device discovery entry point.
- A fixed transport for all operations.
- A device object that directly owns all applet operations.
- Synchronous connection setup for operations that are async in v2.

## Session Lifecycle

V2 application sessions are applet-specific and commonly own connection/protocol state. Prefer the v2 session factory or constructor pattern documented by each applet package rather than carrying v1 session setup forward mechanically.

Migration review is required for code that:

- Constructs sessions directly from v1 device objects.
- Relies on synchronous disposal where v2 uses async cleanup.
- Reuses one connection across multiple applet sessions.
- Depends on implicit transport selection.

## Application Sections

### PIV

Use `Yubico.YubiKit.Piv` for PIV operations. Review authentication, PIN/PUK handling, key import/generation, certificate management, and APDU-level customization manually because lifecycle and security-sensitive buffer handling can differ between v1 and v2.

### FIDO2

Use `Yubico.YubiKit.Fido2` for FIDO2/WebAuthn operations. Review transport selection, PIN/UV flows, credential management, and authenticator state assumptions manually.

### OATH

Use `Yubico.YubiKit.Oath` for TOTP/HOTP credential management and code calculation. Review credential naming, secret handling, password flows, and time-source assumptions manually.

### YubiOTP

Use `Yubico.YubiKit.YubiOtp` for Yubico OTP configuration and slot operations. Review slot numbering, configuration flags, and write/update behavior manually.

### OpenPGP

Use `Yubico.YubiKit.OpenPgp` for OpenPGP card operations. Review key slots, PIN policy, management key behavior, and command-level assumptions manually.

### Security Domain

Use `Yubico.YubiKit.SecurityDomain` for SCP03 and security domain key management. Treat all secure channel, cryptographic key, and diversification migrations as manual until a specific high-confidence mapping exists.

### YubiHSM

Use `Yubico.YubiKit.YubiHsm` for YubiHSM 2 workflows. Review connector/session creation, authentication, object identifiers, capabilities, and command behavior manually.

## Manual Low-Level Command Cases

Manual migration is required for code that builds APDUs, parses raw responses, relies on status-word behavior, preserves unknown protocol fields, or sends vendor-specific commands directly. These cases should be migrated against v2 command/session abstractions where possible. If direct command access remains necessary, verify byte-level behavior against both v1 and v2 sources.

## Automation Note

Migration documentation is maintained by automation on the `yubikit` branch. Pull requests targeting `yubikit` receive migration-impact preview comments. Pushes to `yubikit` open documentation update pull requests and advance `docs/migration/.state.yml` only after migration artifacts are updated.

Weekly scheduled reconciliation requires a wrapper workflow on the repository default branch, because GitHub only runs scheduled workflows from the default branch. The wrapper checks out `yubikit`, runs the same reconciliation logic, and opens documentation pull requests back into `yubikit`.

## How This Document Grows

This guide is intended to mature over the v2 development cycle rather than be written in one final pass.

1. Pull requests targeting `yubikit` get preview comments that identify migration impact without editing files.
2. Merges into `yubikit` trigger documentation update pull requests for the newly analyzed commit range.
3. Weekly reconciliation catches missed or stale migration guidance.
4. Monthly synthesis reorganizes accumulated notes into clearer release-ready sections.

The human review responsibility is intentionally narrow: review the generated migration documentation pull requests for truthfulness, confidence level, and usefulness. The automation should preserve uncertainty as manual-review items instead of inventing mappings.

## Release Readiness Tracker

This initial snapshot is not release-complete. Future automated updates should improve these areas:

- Core/device discovery: package, namespace, physical device, and transport model guidance.
- Session lifecycle: direct v1 constructors to v2 async factories and async disposal.
- Applet recipes: before/after examples for common PIV, FIDO2, OATH, YubiOTP, OpenPGP, Security Domain, and YubiHSM tasks.
- Manual migration cases: raw APDU, custom command classes, exception behavior, and security-sensitive credential flows.
- Tooling foundation: structured map entries precise enough to support a future scanner or analyzer.
