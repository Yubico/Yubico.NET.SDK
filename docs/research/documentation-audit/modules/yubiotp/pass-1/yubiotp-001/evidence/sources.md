# Independent source evidence — 2026-11-04

Investigator: GPT-6.1 Sol, report-only. Initial lookup plus one focused follow-up completed; stop here.
`source-capture.json` and `source-followup.json` retain URLs, HTTP results, full-response hashes and sizes.
These are independently fetched originals, not inherited audit conclusions. No source establishes blanket
conformance or execution on the connected key. Tagged vendor implementation is corroboration, not a normative
specification. Current unversioned web pages do not become versioned standards merely by being vendor hosted.

## S1 — The YubiKey Manual, 3.3

Original vendor PDF: `vendor-technical-manual-v3.3.pdf`; extracted text beside it.
Cover: **Version 3.3, 17 September 2014**; revision history §1.4 says updated for 3.3 firmware.
Footer copyright 2015 is not a newer protocol edition. Status: published vendor technical manual, not an
IETF standard. Actors: device firmware, provisioning application, verifier. Applicability: documented legacy
YubiKey/NEO generations; do not extend every firmware caveat to 5.x.

Consequential clauses (extracted-text line numbers):

- §5.5.3–5.5.4, lines1175–1183: “an intra-character pacing time of **10 ms**”; “**20 ms**”; combined,
  “the total delay is **30 ms**.” Descriptive device behavior, not host scheduling guarantees.
- §5.6.5, lines1389–1392: fast triggering “only applies when one configuration is written. If both
  configurations are set, the flag has no effect.” Not “slot2 only.”
- §5.6, lines1350–1356: serial visibility flags are device-specific and “merged, i.e. if a flag is set in
  any of the configuration, the device consider the flag to be set.” Clearing one slot is not a global hide.
- §5.6.6, lines1395–1419 lists all three update masks, including `EXTFLAG_ALLOW_UPDATE`; “keep the
  EXTFLAG_DORMANT flag set when updating ... Once cleared, the flag cannot be set without a complete
  re-write.” Device behavior, relevant to whole-mask updates and dormant limitations.
- §5.7, lines1441–1449: configured access password “must be supplied at each subsequent update attempt”;
  mismatching password rejects update; each YubiKey2 configuration has its own code. Does not protect
  challenge calculations or encrypt host-to-device traffic.
- §5.5.12, lines1298–1310: protect configuration2 rejects writes from slot1, but a pre-existing protecting
  slot2 makes writing the bit to slot1 ineffective. Pair with tagged header's precise bit definition; do not
  generalize the existing negative-only hardware test into proof of every order combination.
- §5.3.3, lines1041–1073: raw scan codes, high bit denotes SHIFT; “YubiKey2.1 supports1–16 characters ...
  YubiKey2.2 supports up to38.” Type-wide SDK minimum2.0 does not enforce this length-dependent limit.
- §2.4, lines487–501: HOTP from2.1; preset moving factor from2.2. Does not define its wire scaling.
- §2.5, lines520–534: six-byte Yubico OTP challenge produces variable response fields, “different responses
  even if the challenge is the same”; HMAC mode deterministic for identical challenge. The manual's broad
  “0–64” HMAC description does **not** settle empty-input marker behavior.
- §6.1.1, lines1480–1493: private ID is XORed with challenge; verifier reverses that XOR and result “shall
  then match the issued challenge.” Device encrypts a ticket, not merely AES applied to six bytes.
- §6.1.6, lines1557–1564: checksum residual `0xf0b8`; invalid OTP “shall be rejected.” This actor is the
  **OTP verifier**; do not present it as a new SDK HMAC/application authentication requirement.
- §6.5, lines1648–1667: feature report8bytes, seven payload; programming application “shall read the sequence
  number ... prior”; unchanged sequence means failure. Legacy USB guidance, not proof of SDK's special
  SmartCard5.x exceptions or Core framing implementation.
- §7.2, lines1736–1750: NDEF message configured for an **existing OTP configuration**, URI concatenated with
  a generated OTP. The configured56-byte block is not the final on-tag NDEF message.

Manual3.4 linked by ykpersonalize returns404 from original vendor URL; recorded. A mirror search result
was not promoted to original-vendor3.4 evidence. No unbounded hunt for a newer edition.

## S2 — yubikey-personalization v1.20.0

Original tagged `ykcore/ykdef.h`, `ykpers.c`, `ykpersonalize.1.adoc` retained. Status: released vendor
implementation/manual at explicit tag; descriptive API/firmware reference, not a standards requirement.

- `ykdef.h`, configuration structure: `fixed` is “Fixed data in **binary format**.” Constructor input is
  raw bytes; modhex is how the key emits the public ID.
- `ykpersonalize.1.adoc`, Configuration flags: `pacing-10ms`10ms, `pacing-20ms`20ms;
  `hmac-lt64`: “Whatever is in the last byte of the challenge is used as end of input marker
  (backtracking from end of payload).” Does not explicitly state minimum retained byte.
- `ykpers.c`, `ykp_set_oath_imf`: `imf > 65535*16` rejects, `%16` rejects;
  “IMF/16 is16 bits stored big-endian in uid[4]”. Getter returns encoded value `<<4`.
  Thus SDK IMF0x30000 encodes0003, corresponding to device counter48, not196608. This is a strong
  implementation discrepancy corroborated by the vendor manual's0..1048560 step16 contract; real-key
  counter proof remains a separate probe. RFC4226 defines counter hashing, not vendor scaling.
- `ykpersonalize.1.adoc`, YubiKey3+: scanmap “**45 unique bytes** ... order
  cbdefghijklnrtuvCBDEFGHIJKLNRTUV0123456789!\t\r”. SDK38 is the static-password field capacity,
  not this map's vendor length. `ykdef.h` `SCAN_MAP` corroborates the45-character mapping order.
- `ykdef.h`, masks: all EXT bits including AllowUpdate; ticket mask excludes protection/mode; CFG update
  mask bits04/08 are keyboard pacing aliases, **not permission to change challenge touch/short mode**.
- `ykpersonalize.1.adoc`, YubiKey2.3+: swap requires both slots configured updatable.
- `ykpers.c`, command checks: scan-map firmware3+, update/swap2.3+, NDEF family-specific; these are vendor
  constraints, not all enforced by the SDK. No claim that firmware alone implies NFC hardware.

## S3 — yubikey-manager 5.8.0, yubikit/yubiotp.py

Tagged vendor implementation, not a normative source. Captured full file. Focused symbols:

- `calculate_hmac_sha1`: `challenge.ljust(HMAC_CHALLENGE_SIZE, b"\1" if challenge.endswith(b"\0") else b"\0")`.
  Proves host padding agreement at this version, including empty zero fill; not minimum device hashing length.
- `HmacSha1SlotConfiguration`: initializes HMAC_LT64. Qualify “only mode ykman programs” to the reviewed
  version/path rather than every version, raw API, and program.
- `ConfigState.is_touch_triggered`: “triggered by touch (**not challenge-response**)”. This contradicts
  the SDK's interpretation of cached flags as `RequireTouch` policy. Header wording alone is ambiguous;
  seek actual keyboard/non-touch-HMAC/touch-HMAC status matrix. Do not silently change callback behavior.
- `_YubiOtpSmartCardBackend.write_update`: compares increment; zero requires previous>0 and empty
  status flags, or firmware `5.0.0 <= version <5.4.3`. SDK instead bypasses validation for all5.0–<5.5,
  accepts zero after any predecessor and returns short status without rejecting it. These are discrepancies,
  not authority to replace the SDK logic during Pass1.
- `update_configuration`: rejects access-code changes on firmware4.3.2–<4.3.6. SDK lacks that gate.
- `HotpSlotConfiguration.imf`: `%16`, range0..0xFFFF0 and `imf >>4`, independently corroborating S2.
- `_build_ndef_config`: URI byte prefix/table and UTF8/text`02en`,54byte data limit. Its `None` default URI
  differs from SDK's all-zero request; sibling agreement cannot establish SDK disable semantics.
- Session initialization: NFC-specific Management selection and NEO max-version path differ from SDK's
  unconditional Management probe and raw OTP-version state object. Document the SDK's actual path.

## S4/S5 — NFC Forum original RTD text, edition1.0, 24 July2006

Original publisher landing pages retained: “Purchase Specification”; no full publisher-hosted clauses obtained.
Original NFC Forum PDFs fetched from an explicitly identified CCC mirror, not attributed to that mirror as
the author. URI appendixB says **Final**,1.0July2006; Text appendixB shows1.0July2006 but its **status cell is
blank** in the extracted original: do not infer Final from the URI edition. PDFs retained for visual check.
`pdftotext -layout` exited0 for both, with these stderr warnings retained verbatim:

```text
Syntax Error: Expected the default config, but wasn't able to find it, or it isn't a Dictionary
Syntax Error: Expected the default config, but wasn't able to find it, or it isn't a Dictionary
```

Scope: host NDEF encoder/reader, not YubiKey provisioning command framing/firmware limits.

- URI §3.2.1/Table2: type`U`55; one-byte identifier plus UTF8 URI bytes.
- URI §3.2.2/Table3: encoder/decoder “MUST” use table; encoder “MAY” choose00; indices00..23hex,
  subsequent entries RFU; compliant producer “MUST NOT” emit RFU codes. SDK table matches these36
  entries including no-prefix00. This does not mandate longest-prefix selection; that is SDK policy.
- URI §3.2.3: encoding “MUST be UTF-8” subject to scheme; length is “bytes, not ... characters”.
  Reader “SHALL” discard control00..31/invalid UTF8. SDK does not parse/validate URI schemes or prevent
  those controls; avoid advertising complete URI validation. Producer enforcement decision belongs Pass2.
- Text §3.2.1 Tables2/3: type`T`54, status high bit0UTF8/1UTF16, bit6 “MUST” zero, low6bits language
  length; §3.3 language code “MAY NOT be omitted”. `02en` is SDK's fixed EnglishUTF8 choice, not a
  requirement to use English. §3.2.1 CRLF line breaks “MUST”; SDK does not normalize lines. Do not turn
  every text input into a conformance claim; unsupported language/UTF16 options remain feature gaps.

## S6 — RFC4226, December2005, Informational

Original RFC Editor text retained. §3 invokes RFC2119 terminology; RFC status explicitly says it does not
specify an Internet standard. §4 R6 secret “MUST be at least128bits”,160bits “RECOMMEND”; SDK exact20bytes
is a narrower API/device contract, not a general HMAC key rule. §5.1 counter8bytes synchronized between
generator/client and validator/server. §5.2 `HOTP(K,C)=Truncate(HMAC-SHA-1(K,C))`; §5.3 minimum6digits,
possibly7/8; §7.3/7.4 validator throttling/resynchronization responsibilities. Does not define provisioning
IMF scaling, NDEF, slots, or Yubico OTP AES responses. Public XML's RFC reference is relevant and sound.

## S7 — .NET via Context7

Resolved `.NET` to `/dotnet/docs` and queried string immutability/sensitive-memory ownership on2026-11-04.
Returned original documentation links:

- https://github.com/dotnet/docs/blob/main/docs/csharp/fundamentals/strings/index.md : “Once you create
  a string, you can't change its characters.”
- https://github.com/dotnet/docs/blob/main/docs/framework/data/adonet/privacy-and-data-security.md :
  immutable strings cannot be deterministically deleted; source is .NET Framework ADO.NET guidance,
  **not** authority to introduce SecureString/DPAPI into this cross-platform.NET10 module.
- https://github.com/dotnet/docs/blob/main/docs/csharp/advanced-topics/performance/index.md :
  ReadOnlySpan/ReadOnlyMemory provide readonly views over memory, not proof of ownership/erasure.

These support correcting “zero your string” prose, not promising full heap erasure. API borrowing/copying
and actual cleanup conclusions come from local implementations. No build/test/API experimentation executed.
