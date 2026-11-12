# Management boundary source register

## Reused exact originals

A's `investigation/A/sources/captures.json` identifies the retained **YubiKey 5 Series Configuration
Reference Guide**, unversioned manufacturer publication, access UTC 2026-11-11T22:37:06.851496+00:00,
SHA-256 `b707e6b2874a1de53149285a9567ff7262b8e6714839658ca42312b4def41538`.
The full retained page was independently read here. Concepts/Capabilities describes manufacturing-set
supported and user-configurable enabled masks; Configuration Lock requires the correct 16-byte value in
each protected SET. Reading Information identifies INS 1D / OTP 13 / FIDO C2 and common length/TLV format.
Writing Information identifies INS 1C / OTP 15 / FIDO C3, complete replacement masks and same-command current
code. Reset tag 0C requests reboot after all TLVs parse/authorization succeeds. This is descriptive
manufacturer device/configuration-software guidance, without RFC-style normative strength. It describes
5-series/Security Key and compatibility with YubiKey 4 capabilities; it is no universal firmware-floor proof.

The page's Enabled NFC write row incorrectly says 0C while its read row/current encoder/pinned comparator
use 0E. Its ordinary capability table lists 0100 unused. Retain these source limitations rather than
copying that typo or treating it as authority for modern HsmAuth/FIPS/qualifier/component metadata.
Its form-factor table explicitly lists 41/81 USB-A keychain product flags; timeouts use seconds and
TouchEject applies in pure CCID mode. It gives no authoritative tag 14/15 widths/mapping. A's initial
research and focused follow-up already bounded that gap; this packet makes no duplicate broad search.

Shared Python source remains commit `4ca60f706af930459138d8dc0f0f953480e1c7a4`, project 5.9.3-dev.0,
from `sources/python/manifest.json` and `context.md`. `yubikit/management.py` SHA-256
`7cc29f5803f36c945ec613849c65ee9ea83f7d4e260880ebab7096a4dcbe3192` supplies the existing A/C comparator;
`yubikit/core/__init__.py` hash is
`524305273499d3a0fc37ff58c207a180cf3d280490bb48c4596507ed8a56061c`.
No duplicate fetch/import/package/device execution occurred. FIPS mapping/low-nibble form-factor masking,
full unsigned capability conversion and firmware precedence comparisons are reused from A's inspected
passages. They remain implementation comparisons, never device certification or conformance authority.
C's context retains exact page-count history `13f8a21c34e2a8ef483c155d08eb730e7fa59fe9`; no complete history claim.

## Focused current manufacturer reset passages

`sources/captures.json` retains current original HTML, actual UTC timestamps/status/Last-Modified,
byte counts and hashes. The separately fetched readable originals and retained captures were checked
for the same consequential passages:

- **YubiKey Bio Multi-protocol Edition considerations and quirks**, Resetting a YubiKey Bio MPE and
  DeviceReset APDUs. Unversioned manufacturer product/legacy-SDK description. Host sends Management
  SELECT followed by `00 1F 00 00`, with no data/Le; device returns 9000 success or 6f00 failure in its table.
  It describes combined PIV/FIDO/shared-PIN reset and application-specific resets on other families.
  Its v1 method examples are historical API syntax, not current v2 examples. SHA-256
  `180641a30a73463bca30121f2186919b4452725f193ae825341476d5c7fd2c5a`.
- **DeviceResetCommand**, Remarks. Same product actor/scope; only Bio MPE, deletes FIDO credentials,
  fingerprints/shared PIN and PIV keys/certificates/data except F9. The broader ALL applications statement
  and PUK-default phrase must be read alongside the Bio page's no-PUK explanation. Neither passage
  explicitly establishes Management configuration-lock erasure or current v2 session/handle reuse.
  SHA-256 `e818b46ccfbcc6bf3c58b1b7283a4adc0ebe6fe3322d9c38cffebe6e1235bbf0`.

Those publications establish supported product/reset command context, rather than a final
GlobalPlatform/ISO conformance profile or new physical reset evidence. The earlier Management research
already used an initial pass and focused follow-up on lock/configuration/lifetime ambiguity; retain
MGMT-IMP-010's precise bounded gap instead of inventing a supported recovery guarantee.

## Framework orientation and Core mechanisms

Context7 resolved `/dotnet/docs` and queried C# using declarations/await-using reverse disposal order
and exception/finally context. Returned official sources include
`https://github.com/dotnet/docs/blob/main/docs/standard/garbage-collection/implementing-disposeasync.md`
(Acceptable pattern three) and `docs/csharp/language-reference/statements/using.md`. These unversioned
language/API explanations support enclosing/reverse resource scopes. They supply no SDK device-state
or native-release guarantee. No runtime or compiler experiment followed.

The actual Management disposal dispatch/failure paths were read directly in ApplicationSession,
YubiKeyConnectionExtensions, ExchangeGuard and selected protocol methods. B1 portable, B2 Linux,
B3 macOS, B5 framing and C ownership conclusions retain their named source registers. B4 Windows and
B6 APDU/SCP are still active owners; their detailed native/wire/profile conclusions remain pending.

All new files here are source/record evidence only. Fresh independent citation review and current
SDK/compiler/native/device verification remain separate gates.
