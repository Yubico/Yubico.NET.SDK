# Cryptogram follow-up — focused original sources and history

Investigator: GPT-6.1 Sol; accessed 2026-11-04. This is a new bounded investigation prompted by an actual
56-byte-response failure. One focused research round and one focused follow-up were used. No new device
operation, build/test, Core implementation read or product change was performed. Prior investigation and
source records remain unchanged.

## Research sequence and authority

1. Context7 resolved `yubikey-manager` to `/yubico/yubikey-manager` and queried the hsmauth parser and
   optional card-cryptogram behavior. Its generated `_autodocs/api-reference/yubikit.hsmauth.md` summary
   describes a “48-byte response”. Original executable source below instead slices without a length
   equality check. The generated summary is **not** authority for a strict48 wire contract.
2. Initial focused web search: `YubiHSM Auth CALCULATE response "host cryptogram" 56`. Read current/pinned
   vendor Python hsmauth parser/calculation and its path history. Retrieved the original initial-parser,
   removal and version-override commit patches. Enumerated only relevant vendor C hsmauth file paths.
3. Focused follow-up search: `"YubiHSM Auth" "host cryptogram" "CALCULATE"`. Read pinned vendor C
   calculate implementation/header, focused C history and the asymmetric-support patch. Re-read exact
   current Python parser and original historical parser. Stop: no versioned applet wire specification or
   documented firmware-introduction boundary was located. Generic YubiHSM CREATE/AUTHENTICATE SESSION
   search results describe the **HSM**, not the YubiKey CALCULATE response, and were excluded as proof
   of this applet's tail format.

All original network reads in this follow-up succeeded; there was no new HTTP failure to retain. The
first local excerpt selector stopped at `@classmethod` and omitted Python's parse body; the follow-up
explicitly captured lines203–220. This was an excerpt-selection deficiency, corrected below, not evidence
that the method was missing. No original-source statement was found that definitively identifies this
device's final8bytes as a host cryptogram. Vendor implementation/history is corroboration, not normative
applet-spec authority or live qualification.

## V1 — Current YubiKey Manager Python hsmauth source

Title: `yubikit/hsmauth.py`, class `SessionKeys`, methods `_calculate_session_keys` and
`calculate_session_keys_symmetric`. Original vendor repository, host-side library implementation.

Latest file path commit returned by GitHub API:
`296cb587ba15f57672356d8445393a96b41803fb`, author date2025-09-23,
message “HSMAUTH: add change credential password”. Access date is2026-11-04; commit date is not a
firmware release date. `main` and this pinned revision returned byte-identical entire files:
SHA256 `e3573d5580928b84818939b6eb1ae0083c7d01793589746eace80db32fe2cab3`.

Original URLs:
- https://raw.githubusercontent.com/Yubico/yubikey-manager/main/yubikit/hsmauth.py
- https://github.com/Yubico/yubikey-manager/blob/296cb587ba15f57672356d8445393a96b41803fb/yubikit/hsmauth.py#L203-L220
- https://api.github.com/repos/Yubico/yubikey-manager/commits?path=yubikit/hsmauth.py&per_page=100

Exact parser at lines203–220:
```python
class SessionKeys(NamedTuple):
    """YubiHSM Session Keys."""

    key_senc: bytes
    key_smac: bytes
    key_srmac: bytes

    @classmethod
    def parse(cls, response: bytes) -> SessionKeys:
        key_senc = response[:16]
        key_smac = response[16:32]
        key_srmac = response[32:48]

        return cls(
            key_senc=key_senc,
            key_smac=key_smac,
            key_srmac=key_srmac,
        )
```

At596–628, `_calculate_session_keys` appends TAG_RESPONSE for truthy `card_crypto`, calls
`self.protocol.send_apdu(0, INS_CALCULATE, 0, 0, data)` and returns `res`. At630–653 the symmetric method
passes its optional `card_crypto` to that helper and passes the result to `SessionKeys.parse`.

Actor/strength/conditions: host parser accepts/slices a byte string; a supplied nonempty cryptogram is
encoded. There is **no** parser length equality check, response-variant firmware gate or tail validation.
Consequently this Python source would ignore a56byte input's suffix; it also silently permits other
lengths through slicing. This fact does not prove that our physical device's first48bytes have the same
meaning, nor justify accepting arbitrary extra bytes in C#.

## V2 — Python initial56 handling and explicit removal

Original initial commit:
https://github.com/Yubico/yubikey-manager/commit/31d39f64b73e534787382dab4675c0274616627b
Title/message: “HSMAUTH: Add lib for the YubiHSM Auth application”, author date2023-07-13.
Original file SHA256 `cc294104b018add0035544b889bd049a12911ee1a0962fd668aa2748aadfed68`.
Relevant original source at180–201:
```python
@dataclass(frozen=True)
class SessionKeys:
    key_senc: bytes
    key_smac: bytes
    key_srmac: bytes
    card_crypto: Optional[bytes]

    @classmethod
    def parse(cls, response) -> "SessionKeys":
        key_senc = response[:16]
        key_smac = response[16:32]
        key_srmac = response[32:48]
        card_crypto = None
        if len(response) == 56:
            card_crypto = response[48:]

        return cls(
            key_senc=key_senc,
            key_smac=key_smac,
            key_srmac=key_srmac,
            card_crypto=card_crypto,
        )
```

Removal commit:
https://github.com/Yubico/yubikey-manager/commit/47c801e2f4d4b64ac6931e39d962bdbee72c7553
Title/message: “HSMAUTH: remove card_crypto from SessionKeys class”, author date2023-08-01.
Exact relevant changes:
```diff
     key_senc: bytes
     key_smac: bytes
     key_srmac: bytes
-    card_crypto: Optional[bytes]
@@
         key_senc = response[:16]
         key_smac = response[16:32]
         key_srmac = response[32:48]
-        card_crypto = None
-        if len(response) == 56:
-            card_crypto = response[48:]
@@
             key_senc=key_senc,
             key_smac=key_smac,
             key_srmac=key_srmac,
-            card_crypto=card_crypto,
```
On2023-08-04, `e6e475b7233c1ec2a5a3807d37967485d163cd56`, “HSMAUTH: change `SessionKeys` to
NamedTuple”, changed the container representation; its patch leaves the three slices unchanged.

Meaning: handling a56byte input was explicitly present in vendor host code in2023. The removed field
was named **card_crypto**, not host_crypto; neither the commit message nor this patch explains the tail's
cryptographic meaning, supply/omission conditions or firmware version. This is evidence against claiming
“a newly introduced firmware extension”, but not a firmware history or proof of the observed tail.

## V3 — Current vendor C path: deliberately different request and strict response

Title: `ykhsmauth_calculate_ex`, Yubico `yubihsm-shell/ykhsmauth/ykhsmauth.c`.
Status: current master tree pinned at `6195c1b687aab56308e9598a0d66941813c94ae5`; no released-version
association inferred. Actor: host C request encoder/parser. Strength: implementation, not normative spec.

Original:
https://github.com/Yubico/yubihsm-shell/blob/6195c1b687aab56308e9598a0d66941813c94ae5/ykhsmauth/ykhsmauth.c#L561-L631
Full file SHA256 `e34324f4716340f867ab112c1cc4e1ac11006e21d758a3f98dcb2b6220431b14`.
Header full file SHA256 `823212991500d2a17379d3639d284de5c1d11847d28488abaaefd8eb3b42ed17`:
https://github.com/Yubico/yubihsm-shell/blob/6195c1b687aab56308e9598a0d66941813c94ae5/ykhsmauth/ykhsmauth.h#L97-L107

Header defines SESSION_KEY_LEN16, CARD_CRYPTO_LEN8, HOST_CRYPTO_LEN8. Calculation lines591–594:
```c
  // Only send card_crypto for asym auth
  if (card_crypto && card_crypto_len > YKHSMAUTH_CARD_CRYPTO_LEN) {
    add_tag(&apdu, YKHSMAUTH_TAG_RESPONSE, card_crypto, card_crypto_len, 0);
  }
```
Lines613–631, after success-status handling:
```c
  if (recv_len != 3 * YKHSMAUTH_SESSION_KEY_LEN) {
    if (state->verbose) {
      fprintf(stderr, "Wrong length returned: %zu\n", (size_t) recv_len);
    }
    return YKHSMAUTHR_GENERIC_ERROR;
  }

  uint8_t *ptr = data;

  memcpy(key_s_enc, ptr, YKHSMAUTH_SESSION_KEY_LEN);
  ptr += YKHSMAUTH_SESSION_KEY_LEN;
  memcpy(key_s_mac, ptr, YKHSMAUTH_SESSION_KEY_LEN);
  ptr += YKHSMAUTH_SESSION_KEY_LEN;
  memcpy(key_s_rmac, ptr, YKHSMAUTH_SESSION_KEY_LEN);
  ptr += YKHSMAUTH_SESSION_KEY_LEN;

  // Ignore host crypto for now

  return YKHSMAUTHR_SUCCESS;
```

The host-crypto comment is real, but the preceding guard rejects56. It is not executable evidence that
the C library accepts/ignores a56byte suffix. The C request deliberately withholds an8byte symmetric
card cryptogram, whereas Python/C# send it. This difference is consequential when comparing behavior.
It supplies no recommendation to users to omit HSM authentication checks.

History original:
https://github.com/Yubico/yubihsm-shell/commit/08937b47458a9e0f30ea94a16885d7a82fb4dabb
“Support for Asymmetric yubihsm-auth (#175)”, author date2021-10-06. Relevant changes add
`ykhsmauth_calculate_ex`, the `card_crypto_len > CARD_CRYPTO_LEN` request condition, CARD/HOST lengths8,
and `// Ignore host crypto for now`, while retaining the strict3×16 response check. Thus this comment
and suppression are longstanding host implementation choices, not a recently added firmware explanation.

Recent path-history entries retrieved:
- `4a16ee7243daf766daf69cd5eae589f53eb2f12c`,2026-09-08: chained-response support.
- `48220c03637254fd396f4e0747d72d3b238ba2a0`,2026-06-30: extended-APDU support.
- `b1a4f978c97113a41739a0af0cb9d2ae8a742bb2`,2024-09-05: challenge firmware check.
- `79cabf6b31c2b89fc4b425229875c449f5d6f245`,2024-03-27: get_challenge_ex.
These titles alone prove no cryptogram-format change; only the relevant2021 patch was inspected in detail.

## V4 — Development version normalization is a separate host concern

https://github.com/Yubico/yubikey-manager/commit/c910e5d25ba675ac2bfc97d35f14a4cc0314864f
Title: “Override version checks for 0.0.1”, author date2025-04-22. Original hsmauth patch:
```diff
-        self._version = _parse_select(self.protocol.select(AID.HSMAUTH))
+        self._version = _override_version.patch(
+            _parse_select(self.protocol.select(AID.HSMAUTH))
+        )
```
This establishes that ykman/Python has an explicit version-patching path. Its implementation and exact
mapping were outside this focused source read. Therefore ykman5.8.0 and SDK-effective0.0.1 must remain
separate observations; neither is an independent proof of a release-version56byte wire contract.

## Bounded conclusion / gap

Known vendor parser history supports the **existence of56byte response handling in host code**, and a
vendor C comment names host crypto, but the inspected originals do not establish the supplied-symmetric-
cryptogram variant's normative byte order or firmware boundary. No manual found within this bounded round
explicitly says `S-ENC || S-MAC || S-RMAC || host-cryptogram` for this applet response. Preserve both the
historical `card_crypto` naming and current C guard; do not reconcile them by assertion. Actual raw
response comparison is needed before assigning the observed56bytes' regions cryptographic meaning.
