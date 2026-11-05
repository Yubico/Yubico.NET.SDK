# Installed-source review for the public-state helper

Reviewed 2026-11-05 using Python `/usr/bin/python`, installed distribution
`yubikey-manager==5.9.2`. `installed-manifest.json` preserves SHA-256 hashes and API
signatures; adjacent files retain complete installed source rather than assuming a release
tag or an upstream commit matches the local package. The helper rechecks these hashes at
runtime. Source inspection imported modules and read source; it opened no device.

## Consequential installed paths

| Snapshot | Reviewed behavior |
| --- | --- |
| `yubikit.piv.py:697–794` | Session SELECT/GET VERSION, initial management metadata, reset sequence and serial gate. Constructor falls back to TDES when metadata is locally unsupported; the helper records actual metadata separately and never authenticates using that cached fallback. |
| `yubikit.piv.py:1069–1133,1466–1474` | Public PIN/PUK/management/slot/Bio metadata API shape, gates, TLV tags and enum interpretation. Bio maps `6A88`/`6D00` to unsupported. |
| `yubikit.piv.py:1214–1281` | Public object and certificate GET; certificate format/decompression/parsing failures propagate. |
| `yubikit.piv.py:736–787` | Reset rejects enrolled biometrics, blocks PIN then PUK and sends RESET. It has no rollback after partially blocking retries. Its unavailable-metadata management-type fallback is TDES. |
| `yubikit.piv.py:796–861` | `authenticate(management_key)` consumes the session's current management-key type; deprecated explicit type is checked against that cached type. Cleanup must use post-reset metadata authority. |
| `yubikit.piv.py:976–993` | `get_pin_attempts()` can issue empty VERIFY and return a cached count after success. Baseline uses metadata alone. |
| `yubikit.management.py:294–418` | DeviceInfo public shape; beta qualifier replaces effective version at lines 391–395. Both reported qualifier and effective firmware are captured. |
| `yubikit.core.py:238–261` | Global private version override patches development sentinel `0.0.1`; `require_version` otherwise compares directly. The helper leaves this state untouched. |
| `ykman.piv.py:209–273` | Public ADMIN (`5FFF00`) flags and derived-key salt; protected object (`5FC109`) contains key tag `89`. Baseline reads only public ADMIN and emits a boolean allowlist. |
| `ykman.piv.py:417–446` | Certificate listing suppresses APDU failures and omits empty slots; the helper reads each slot/certificate individually to retain absence/failure evidence. |
| `ykman.device.py:307–345` | Composite serial discovery opens cards; helper uses reader descriptors instead to leave serial 125 unopened. |
| `ykman.pcsc.py:77–102,105–162,261–284` | Public connection adapter, descriptor enumeration and convenience opener's agent termination recovery. Direct adapter construction opens the selected reader without that recovery path. |
| `ykman._cli.piv.py:244–286` | CLI checks DeviceInfo reset-blocked mask before reset. Its literal printed default values do not establish the management algorithm on beta hardware. |

Metadata fallback is narrowly tied to the installed source: GET METADATA `00 F7 00 ref`,
PIN/PUK tags `05` default and `06` retries; management tags `01` algorithm, `02` policy,
`05` default; asymmetric tags `01` algorithm, `02` policy, `03` origin, `04` public key.
The capture retains raw public metadata responses and status words alongside decoded
results. The source's management algorithm default of TDES applies only when its algorithm
tag is absent; the helper records the raw response so this fallback remains auditable.

## Current documentation lookup

Context7 library resolution selected `/yubico/yubikey-manager`. Two bounded queries covered
the PivSession metadata/read/reset API and attestation/reset/default-management behavior.
Returned documentation source:
<https://github.com/yubico/yubikey-manager/blob/main/_autodocs/api-reference/yubikit.piv.md>.

Relevant returned API passages on 2026-11-05:

> `PinMetadata(default_value, total_attempts, attempts_remaining)`;
> `ManagementKeyMetadata(key_type, default_value, touch_policy)`;
> `SlotMetadata(key_type, pin_policy, touch_policy, generated, public_key_encoded)`.

> `reset()` blocks PIN and PUK with invalid attempts, then sends `INS_RESET`, restoring
> defaults. Refuses when biometrics are configured.

The installed snapshots establish local API details; Context7 supplies documentation
corroboration rather than an asserted installed-source revision. The bounded follow-up
did not establish a beta-specific factory management algorithm. Runtime metadata remains
the authority; the coordinator's observed AES192 supersedes the earlier AES256 hypothesis.

## Attestation preservation passage

Original vendor page read 2026-11-05:
<https://developers.yubico.com/PIV/Introduction/PIV_attestation.html>, “Implementation”:

> The YubiKey comes with a pre-loaded attestation certificate signed by a Yubico PIV CA.
> This can be overwritten by loading a new key and certificate to slot f9. After the
> Yubico key is overwritten it can not be brought back. The attestation key and certificate
> will not be cleared out by a reset of the device.

The same page explains that attestation creates a certificate for a key generated on the
device, and that preview devices use a preview CA. Preserve the device's existing F9
identity without treating a certificate hash as an independent proof of private-key
possession or factory provenance. The coordinator's later oracle can verify a generated
slot's attestation signature against that captured F9 public key.

## Static preparation checks

Python's `ast.parse` accepted the helper source without executing it. SHA-256 comparison
confirmed that all nine snapshots match both their manifest entries and installed files.
The helper source hash at handoff is
`41aa7200cd912a5f7a1438ad693fff83ad36b32873540b8a70d1b578f976600a`.
Hardware behavior remains unexecuted in this worker's assignment.
