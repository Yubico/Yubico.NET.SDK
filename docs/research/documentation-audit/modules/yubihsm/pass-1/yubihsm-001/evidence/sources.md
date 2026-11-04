# Original-source evidence — investigator

Accessed 2026-11-04. Focused passages below were read from originals, following the initial search and
one focused follow-up. Vendor manuals describe product behavior; they are not a versioned normative
applet specification. A Python implementation corroborates wire choices; agreement establishes neither
normative conformance nor hardware qualification. No source was silently upgraded to a newer standard.

## S1 — YubiKey Technical Manual, “YubiHSM Auth Specifics”

URL: https://docs.yubico.com/hardware/yubikey/yk-tech-manual/yk5-apps-yubihsm-auth.html
Status: living vendor technical manual, copyright 2026; page exposes no numbered document revision.
Actor/layer: YubiKey applet, YubiHSM 2, host libraries, provisioning operator. Strength: descriptive
product requirements and explicit operational guidance, not RFC-style MUST/SHOULD clauses.

Opening conditions:
> YubiHSM Auth is supported by YubiKey firmware version 5.4.3 and above.
> 5.4.3 or later firmware requires using ykman or Yubico Authenticator to enable it.
> 5.7.x or later firmware is enabled (on) by default.

`#yubihsm-2-secure-channel`:
> The identical long-lived keyset is protected in the YubiHSM 2 and in the YubiKey YubiHSM Auth application.
> The YubiHSM Auth session-specific keys are output from the YubiKey to the calling library, which uses
> the session keys to encrypt and authenticate commands and responses during a single session. The
> session keys are discarded afterwards.

`#architecture-overview`:
> The identical long-lived credentials (key sets) are protected in both the YubiKey YubiHSM Auth
> application and in the YubiHSM 2. The YubiHSM-Shell software tool can be used for generating the key
> sets in the YubiHSM 2, and the YubiHSM-Auth software tool can be used for importing the same key sets
> to the YubiKey YubiHSM Auth application.

`#yubihsm-auth-flowchart`, steps 3–4, 10–11:
> The libyubihsm library generates a host challenge, and opens a session to the YubiHSM 2 device.
> The YubiHSM 2 device generates an HSM challenge, and generates the session keys based on the HSM
> challenge, the host challenge, and the static key set in the YubiHSM 2 device.
> The YubiHSM Shell acknowledges the protocol handshake to libyubihsm.
> The libyubihsm sends the host response to the YubiHSM 2 device. The session keys can now be used
> for secure channel communication between YubiHSM-Shell/libyubihsm in the host and the YubiHSM device.

`#using-yubihsm-auth-with-yubihsm-shell` identifies separate inputs:
> authkey is the identifier of the authentication key in the YubiHSM 2
> label is the label of the YubiHSM-Auth credentials stored in the YubiKey
> password is the password that protects the YubiHSM-Auth credentials stored in the YubiKey.

Conditions: this passage explains symmetric AES authentication. Its “each credential” language and
access-code generalizations predate/omit asymmetric detail; use it for the scoped symmetric enrollment
and host flow, rather than extrapolating every command's authentication or asymmetric receipt format.

## S2 — “YubiHSM Auth overview”, vendor .NET SDK user manual

URL: https://docs.yubico.com/yesdk/users-manual/application-yubihsm-auth/yubihsm-auth-overview.html
Status: living **v1** SDK manual (Edit link targets `develop`), no numbered revision shown. Actor: applet.
Strength: descriptive vendor product behavior; do not inherit its v1 method names or immutable-password
claim into v2. Sections “Management key” and “Credential”:

> There is a limit of 8 attempts to authenticate with the management key before the management key is
> blocked. Once it is blocked, the application itself must be reset before authentication can be
> attempted again. Supplying the correct management key before the key is blocked will reset the retry
> counter to 8.

> Each credential has a 16-byte password which is required when calculating session keys. There is a
> limit of 8 retries, after which the credential will be permanently deleted. Supplying the correct
> password before the credential is deleted will reset the retry counter to 8.

> Optionally, the credential may be configured to also require touch when calculating session keys.

Same page also says “Once a credential is added, it cannot be changed or modified in any way.” That
blanket statement conflicts with newer password-change support; reject it as authority for 5.8 behavior.
Retry recovery is supported by the explicit passage, while live success-reset observation remains the
coordinator's proposed YubiKey-only probe. No new hardware evidence is claimed here.

## S3 — “Interacting with a YubiHSM 2”, vendor .NET SDK user manual

URL: https://docs.yubico.com/yesdk/users-manual/application-yubihsm-auth/interacting-yubihsm-2.html
Status: living v1 user manual, no revision shown. Actor: host integrator. Strength: recommendation and
descriptive secure-channel prerequisite, under “YubiHSM SDK” and “YubiHSM 2 secure channel”:

> Once credentials are added to the YubiHSM Auth application, use the YubiHSM SDK (and bundled tools)
> to establish a secure session with a YubiHSM 2 device and perform operations on it.

> In order to successfully create a secure channel, the long-lived keys in the YubiHSM Auth credential
> and YubiHSM 2 device must be identical.

This supports directing readers to external connector/HSM integration. It supplies no v2 connector API.

## S4 — YubiKey Manager `yubikit/hsmauth.py`

Pinned file: https://github.com/Yubico/yubikey-manager/blob/296cb587ba15f57672356d8445393a96b41803fb/yubikit/hsmauth.py
Retrieved main raw file, then resolved latest path commit through GitHub API: the above commit is
2025-09-23, “HSMAUTH: add change credential password”. Status: vendor implementation, **corroborating**.
Actor: host-side encoder. Relevant original passages:

```python
INS_CHANGE_CREDENTIAL_PASSWORD = 0x0B
INITIAL_RETRY_COUNTER = 8
# _password_to_key:
key = PBKDF2HMAC(
    algorithm=hashes.SHA256(), length=32, salt=b"Yubico", iterations=10000,
    backend=default_backend(),
).derive(pw_bytes)
key_enc, key_mac = key[:16], key[16:]
```

`_change_credential_password` gates at `(5, 8, 0)`, sends instruction `0x0B`, P1 `1` for management
authentication and `0` for credential authentication. `generate_credential_asymmetric` gates at
`(5, 6, 0)` and passes `b""` as the private-key value. `calculate_session_keys_asymmetric` describes:

```python
:param context: The context (EPK.OCE + EPK.SD).
:param public_key: The YubiHSM device's public key.
```

`get_challenge`:
```python
require_version(self.version, (5, 6, 0))
data: bytes = Tlv(TAG_LABEL, _parse_label(label))
if credential_password is not None and (
    self.version >= (5, 7, 1) or self.version[0] == 0
):
    data += Tlv(TAG_CREDENTIAL_PASSWORD, _parse_credential_password(credential_password))
return self.protocol.send_apdu(0, INS_GET_CHALLENGE, 0, 0, data)
```
Its docstring calls the password “needed for asymmetric credentials”. This supports showing an
authenticated asymmetric challenge call on 5.7.1+, not promising every omission works. Neither Python's
generic GET_CHALLENGE error nor its password parsing is an authority for the desired C# error policy.

## S5 — Python YubiHSM `yubihsm/core.py`, focused KDF/handshake implementation

Pinned: https://github.com/Yubico/python-yubihsm/blob/055f85fcff0a1d3c0ec779dc28cb21e2762cef3c/yubihsm/core.py
Retrieved main raw file; latest path commit resolved with GitHub API: 2025-06-19. Status: corroborating
vendor implementation, no normative-standard status. Actor: host session negotiator.

```python
KEY_ENC = 0x04
KEY_MAC = 0x06
KEY_RMAC = 0x07
CARD_CRYPTOGRAM = 0x00
HOST_CRYPTOGRAM = 0x01
def _derive(key: bytes, t: int, context: bytes, L: int = 0x80) -> bytes:
    # this only supports aes128
    if L != 0x80 and L != 0x40:
        raise ValueError("L must be 0x40 or 0x80")
    i = b"\0" * 11 + struct.pack("!BBHB", t, 0, L, 1) + context
    c = cmac.CMAC(algorithms.AES(key), backend=default_backend())
    c.update(i)
    return c.finalize()[: L // 8]
```
`SymmetricAuth.create_session` derives S-ENC with K-ENC/type04, S-MAC with K-MAC/type06, S-RMAC with
K-MAC/type07. `authenticate` derives card cryptogram from S-MAC/type00/L=0x40, compares it constant-time,
then sends a host cryptogram. This is a useful independent probe oracle, explicitly short of an actual
connector/HSM handshake or a conformance claim.

`AsymmetricAuth.init_session` concatenates EPK-OCE then received EPK-HSM; `create_session` obtains the
device's static public key separately, derives four 16-byte keys and verifies a full CMAC receipt before
returning three session keys. This corroborates the wrong-key/multiple-invalid-input limitation of the
local dummy-cryptogram integration test.

## S6 — Current .NET documentation

Context7 resolve `.NET` → `/dotnet/docs`, then query for static PBKDF2 and zero-memory: returned
`docs/fundamentals/syslib-diagnostics/syslib0060.md`, recommending one-shot static `Pbkdf2` and showing
`byte[] derivedKey = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, hashAlgorithm, 64);`.
This is library guidance, not an applet derivation specification.

Original: https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.cryptographicoperations.zeromemory?view=net-10.0
Title: “CryptographicOperations.ZeroMemory(Span<Byte>) Method”; .NET10 reference, page metadata updated
2026-07-01, source revision `29d4de1d2ba94856f503b249d2d2201c0b7aeefb`.
Definition: “Fills the provided buffer with zeros.” Parameter: “The buffer to fill with zeros.”
Actor: managed caller/runtime, strength: API contract. Apply only to the supplied storage; source arrays,
copies, immutable strings, console history and framework internals require separate reasoning.

## Failures and bounded gaps

Original fetches returned HTTP404 for:
- https://developers.yubico.com/yubihsm-auth/Protocol.html (initial plausible applet protocol URL)
- https://developers.yubico.com/ykhsmauth/Protocol.html (focused alternate)
- https://developers.yubico.com/YubiHSM2/Concepts/Secure_Channel.html
- https://docs.yubico.com/hardware/yubihsm-2/hsm-2-user-guide/hsm2-sdk-tools-libraries.html (vendor-manual link target)

Initial search plus focused follow-up located original manuals and vendor code, but no versioned applet
normative command specification. **GAP-SPEC-1:** exact 5.8/development-firmware semantics, asymmetric
receipt validation/status mappings, and device-specific complexity requirements remain unqualified by a
versioned normative original. Keep source-supported SDK gate descriptions and device observations
separate; seek vendor confirmation for conformance/intent decisions in Pass2. Stop broad research here.
