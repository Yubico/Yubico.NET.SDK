# Source checks for serial-103 cleanup

The cleanup helper binds to successful baseline SHA-256
`3f160854e948cb01f3e535efe0e59cdf5df5eb67b94cdeb7658b0de9d1223718`, public helper SHA-256
`41aa7200cd912a5f7a1438ad693fff83ad36b32873540b8a70d1b578f976600a`, and installed-source
manifest SHA-256 `ebd4aa11daec37265b8c2b027d31bdfdd871d850b7b7b637a4a5ffb07fa96f5d`.
Their bytes stay unchanged. Runtime `vendor-state.provenance()` checks installed source
against all nine original snapshots; the cleanup receipt records its own source hash and
the resulting Python/source identity.

## Installed reset and development version

Re-read installed `/usr/lib/python3.14/site-packages/yubikit/piv.py:736–910` on 2026-11-05.
Complete source remains in frozen `yubikit.piv.py`, SHA-256
`f1055e88dca0c8b0ff107a3f1724cd9e03447a95565ecbfde7517f8114ba47fd`.

- Lines 744–750 reject configured biometrics, allowing `NotSupportedError` to continue.
  The bounded helper instead requires the actual captured non-Bio `6A88` response.
- Lines 752–760 call `get_pin_attempts()`, then `verify_pin("")` until blocked. Lines
  976–993 show metadata fallback to empty VERIFY and a cached count after success. The
  helper uses raw public GET METADATA counters, finite loops and exact decrement checks.
- Lines 762–773 use PUK metadata, falling back to an initial counter of one, then call
  `_change_reference(INS_RESET_RETRY, PIN_P2, "", "")` until blocked. Public
  `unblock_pin("", "")` at 1026–1044 delegates to that same call and propagates
  `InvalidPinError`; the helper uses that public entry point and actual metadata counts.
- Lines 775–785 send RESET and refresh cached defaults. The helper sends the same public
  protocol RESET instruction after blocking, then closes the session and obtains a fresh
  independent public capture instead of using stale retry/type caches.
- Constructor lines 711–720 uses reported applet version and falls back to TDES when
  metadata is locally unsupported. Actual baseline `000001` triggers that path even
  though Management metadata identifies `5.8.0.beta.0` and raw F7 `9B` reports algorithm
  `0A` (AES192). Raw F7 parsing follows installed 1079–1109 and 1466–1474.

## Public default authentication

Installed `authenticate()` lines 805–861 reads the public `management_key_type` property
at line 829, then performs witness/challenge mutual authentication with a constant-time
comparison. Passing the deprecated explicit type at lines 819–825 still checks the cached
property and would reject actual AES192 when the constructor fell back to TDES.
The local subclass overrides only that public property with freshly validated raw
management metadata. Its inherited authentication implementation remains installed vendor
code. The helper changes neither `_version`, `_management_key_type` nor global override.

Installed public `DEFAULT_MANAGEMENT_KEY` at 287–292 contains 24 published bytes.
`MANAGEMENT_KEY_TYPE.AES192.key_len` returns 24 (153–164). The helper requires both widths
and actual returned algorithm to agree. Factory PIN verification uses public
`verify_pin()` after successful RESET and a fresh exact-default readback. PUK metadata's
default flag and retry counts supply its post-reset evidence; there is no separate PUK
VERIFY command in this flow.

Context7 query on 2026-11-05 used `/yubico/yubikey-manager` for reset, InvalidPinError,
public authenticate and management_key_type. Returned documentation source:
<https://github.com/yubico/yubikey-manager/blob/main/_autodocs/api-reference/yubikit.piv.md>.
It corroborates the public method/property shapes, reset's deliberate blocking sequence,
the biometric reset refusal and management-key type checks. Installed source establishes
the exact local behavior, especially the sentinel/cache interaction.

## Attestation and partial reset

The original vendor preservation passage remains in frozen `review-notes.md`, sourced
from <https://developers.yubico.com/PIV/Introduction/PIV_attestation.html>:

> The attestation key and certificate will not be cleared out by a reset of the device.

The same passage says overwriting the factory key is irreversible. Cleanup therefore
leaves F9's private key, certificate and policy untouched and compares all captured public
F9 fields before/after. Reset failure after deliberate blocking has no rollback in the
installed implementation; the receipt and journal preserve this partial-state boundary.
An APDU without a returned status leaves its physical completion uncertain.

The coordinator's baseline raw F9 metadata contains origin `02`, algorithm `07`, PIN/touch
policy `02 01`, and its public key matches its certificate SPKI. The helper pins these
observations without inferring original factory provenance or private-key possession.

## Preparation checks

Static `ast.parse` accepted the final cleanup source without executing it. SHA-256 checks
confirmed the exact baseline, frozen public helper and frozen manifest identities, plus
all nine installed-source/snapshot pairs. The prepared cleanup-helper SHA-256 is
`8e5aba11c7cb391b454aac9d1b4c478c69f0735671c701793f91d4f7f4bc6a7f`.
No device access, reset, authentication, build, test, formatter or commit ran in this
preparation worker. Actual execution results belong in the coordinator's new receipt and
three public readbacks.
