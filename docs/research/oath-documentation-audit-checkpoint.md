# OATH documentation audit checkpoint

This checkpoint records the completed OATH follow-up through `76a8ae52` and the remaining work. It is an audit
handoff, not a claim of complete firmware or secure-channel validation.

## Scope

The campaign is documentation-first. The user approved the OATH implementation fixes as an explicit exception.
Meaningful behavior or API changes discovered elsewhere are deferred to a separate implementation pass, followed
by re-review of the affected documentation. Cross-application reset/reopen consistency is specifically deferred.

## Completed work

- Initial documentation corrections and reviewed protocol/CLI fixes: `7c4153f8`, `618adbec`, `c79e4c1e`, integrated
  through `c90dced1`. Linux PC/SC setup guidance: `244df5b1`.
- Shared Base32 decoding, failure cleanup, and credential request validation: `8503fe2d`, integrated in `eba72a81`.
- Terminal OATH reset, timestamp bounds, authentication-error provenance, and affected caller/setup migrations:
  `d74cdd44`, integrated in `57e0fa4d`.
- Documentation-impact review and migration/comparison corrections: `76a8ae52`.

Successful OATH RESET now ends the session, both plain and SCP. Callers close/reopen the connection and create a
fresh session. Caller-supplied connections remain caller-owned. This is an intentional difference from the pinned
Python SDK, which retains SCP parameters and re-establishes the channel on the same session and connection.

## Verification

- Independent implementation reviews and correction re-reviews passed. A separate documentation-impact review
  found three stale claims; all were corrected and re-reviewed.
- Combined full solution build and unit suite: **3,998 passed**, including **436 OATH tests**; five existing skips;
  zero warnings. Documentation, architecture, and applicable complexity checks passed.
- Combined non-user-presence OATH integration suite on serial **125**, firmware **5.8.0.alpha.2** (SDK reports
  `5.8.0`): **32 passed**—19 hardware and 13 offline cases—with seven skips and zero warnings.
- The skips were three absent older-firmware rows, three serial-103-only rows, and the SCP initialization case
  described below. The user-presence case was excluded, not counted as skipped.
- Final cleanup and separate postflight opens confirmed password protection disabled, no OATH accounts, and
  released connections. Earlier wave-one protocol tests also passed on firmware 5.4.3 in a separate run.

## Deferred: live OATH SCP03 initialization and reset verification

The live SCP test **was attempted**. On serial 125, INITIALIZE UPDATE returned `6A88` before EXTERNAL AUTHENTICATE
or OATH RESET. The reset test therefore skipped at initialization; it did not exercise reset over a live secure
channel. This does not establish that OATH SCP is generally broken.

A separate bounded probe selected OATH and issued INITIALIZE UPDATE on fresh connections:

| Command header | Result | Response data length |
| --- | --- | --- |
| SELECT OATH `00 A4 04 00` | `9000` | 15 |
| INITIALIZE UPDATE `80 50 FF 00` | `6A88` | 0 |
| SELECT OATH `00 A4 04 00` | `9000` | 15 |
| INITIALIZE UPDATE `80 50 00 00` | `6A88` | 0 |

The diagnostic test collected both observations and failed its acceptance assertions. It was not a passing
integration test. No external authentication, reset, key writes, or additional key-version probes were performed.

Source comparison with Python YubiKey Manager revision `4ca60f706af930459138d8dc0f0f953480e1c7a4` confirms that both
implementations send `P2=00`; their default key versions differ (`FF` in C#, `00` in Python). Changing that version
did not resolve the observed rejection. No default was changed, and neither the installed key inventory nor the
exact cause was established.

Offline SCP03 tests exercise Core secure messaging and terminal disposal, including zeroing of actual derived-key
storage. They do not substitute for live SCP verification; there is no new SCP11-specific verification.

**Follow-up:** investigate the initialization failure in a separate SDK/device debugging pass. Once a channel can
be established, rerun the live terminal-reset case and review any resulting SCP, firmware, and reset documentation
claims. Do not infer missing credentials, unsupported firmware, or a protocol defect solely from this status.

## Other recorded follow-ups

- **CORE-001:** multi-key discovery can exclude serial 9681620 after an abandoned HID identity read; it works alone.
  The ownership guard was not bypassed. Exact cause and any fix are deferred.
- Broader session policy after failed device-HMAC verification, raw access-key scope, provisioning-URI extensions,
  wider HOTP counter APIs, padding provenance, detailed HOTP counter/marker behavior, and earliest firmware rationale
  remain bounded questions. Current documentation must preserve the distinctions already established.
- **DOC-002:** the migration map has a pre-existing full-file YAML parse error at line 101, column 621 of
  `docs/migration/v1-to-v2-map.yml` at this checkpoint. It reproduces before the OATH entry edits; the changed OATH
  exception entry parses in isolation. Record it for a documentation-only hygiene pass; full-map YAML validity is
  not part of the successful checks above.

Detailed local handoffs, patches, review records, and raw test logs are retained in the audit workspace. This
repository checkpoint preserves the results and deferred decisions without publishing those external artifacts.
