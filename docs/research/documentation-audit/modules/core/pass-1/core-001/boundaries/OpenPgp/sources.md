# Reused source authority and bounded qualifications

This source investigation reuses the original frozen OpenPGP evidence and completed
Core source registers. `source-identities.json` records actual retained bytes/ranges
and hashes. No frozen archive was rewritten or historical logger executed.

| Original / exact identity and status | Section, actor and strength | Applicability and boundary |
| --- | --- | --- |
| *Functional Specification of the OpenPGP application on ISO Smart Card Operating Systems*, Version3.4.1, March18,2020, Achim Pietig; retained original PDF SHA256 `2d2652a2bd690a6784c733ba6d88539e2af1f6e075f3a3d0af81eb5f68432782` | §§4.3/4.3.1 describe card checking format/length and terminal alignment. §4.3.2 uses shall for present KDF-DO/derived credential state and terminal evaluation; at-least64-byte hash capacity is should. §7.2.2 assigns both P2=81/82 to PW1, with explicit command table absent Le and min6/8/maxC4. §7.2.3 defines concatenated old/new data and absent Le. §7.2.11 defines RSA block handling and A6/7F49/86 ECDH input; card returns plaintext/shared secret, terminal handles content processing. | Published application functional specification, not an ISO publication or proof of every vendor extension/firmware. Exact PDF cover and text derivative were distinguished. Card table supplies concrete OpenPGP caller-profile evidence while B6's unavailable ISO case/Le clauses remain unavailable. |
| Yubico *OpenPGP Attestation*, retained unversioned manufacturer HTML accessed2026-11-04, SHA256 `77da864a47a8ed09c62a61c8981b7ebfbc9f79ee930cffe108fc7344a46c1683` | “Protocol Specification → Attestation Key (Data Objects)” says ATT must use complex CRT, table B603840181; earlier manufacturer text says ATT key/certificate survive reset. Actor is vendor extension producer/host addressing ATT; requirement applies this extension. | No RFC status/revision was invented for this page. Frozen source readback confirms extension encoding/persistence; historical actual same-key/correct-CRT hardware has its separate revision/device limits. Manufacturer root trust remains verifier work. |
| Python YubiKey Manager `4ca60f706af930459138d8dc0f0f953480e1c7a4`, project5.9.3-dev.0; frozen `ykman-yubikit-openpgp.py` SHA256 `bcd266e53411a9611ee133ed5f33e2907294fbe374aa56a19672a151e86a2926` | OpenPgpSession lines987–1331: init order, specific6985 fallback, config/cache, reset checked commands, KDF transitions, byte-limit helper and `_verify(PW.USER,...mode)`; decrypt lines1736–1768 wraps EC input. Actor is sibling SDK implementation; no normative strength. | Selected complete methods were independently read; no package import/tests/crypto/device ran. Python closes/handles immutable bytes differently. Matching Python proves neither Core array erasure nor hardware support. Current source pin is reused, not replaced with installed5.9.2. |
| Core shared Python history, `sources/python/context.md` and original `history/` records | `c910e5d25ba675ac2bfc97d35f14a4cc0314864f` scopes override handling to0.0.1; OATH floor origin `1b87964ba258e3ce39fd2959888dfe7cf81a6843` is OATH-specific source policy. | Reused history conclusions retain C's bounded exact-diff inspection scope. This worker did not reread every historical patch or infer earliest physical support, chronology/publication date or an OpenPGP5.6.3 floor. |
| Microsoft .NET async-disposal guide, Context7 `/dotnet/docs`, queried2026-11-12, `docs/standard/garbage-collection/implementing-disposeasync.md` | Nonsealed implementation guidance introduces virtual DisposeAsyncCore; its generic example calls Dispose(false) after awaited cleanup. Actor is implementer; advisory framework pattern, not a normative SDK guarantee. | Orientation only. Core's actual DisposeSessionAsync finally deliberately calls virtual Dispose(true), so OpenPGP KDF cleanup occurs on async path. No inference from the generic example replaced source inspection. Current main reference has no immutable revision here; it adds no consequential citation requiring a new source contract. |

## Preserved upstream gaps

- B6's retained SCP03 is 1.1.2 Public Release, March 2019; code-cited 1.1.2.6 is
  Public Review, December 2019, and exact clauses remain unavailable after the bounded
  original/focused attempt. Preserve the final edition's specific authority separately.
- Retained SCP11 is 1.3.0.13 Public Review, August 2023. Final 1.4 and exact YubiKey
  profile clauses remain unverified. a/c host authorization includes a correct-MAC
  post-establishment command under that draft; IsAuthenticated/receipt alone is narrower.
- ISO 7816-4:2020 metadata/abstract is identified, while exact normative APDU case,
  corrected-length and logical-channel clauses remain unavailable. No unseen MUST or
  guessed section is introduced; Card 3.4.1's own command tables are a distinct source.
- B4's exact deployed NativeShims/header/producer correspondence, including Linux
  PC/SC native-width concerns, remains open. Published source and current C# routes
  establish no loaded binary, physical release or native buffer-erasure attestation.
- Prior OpenPGP commercial/source firmware differences, ROCA ranges, old-family and
  NFC/provider availability retain frozen-source/hardware qualifications. This packet
  makes no new claim requiring another unbounded source search. B6/C's completed
  initial-and-one-follow-up limits remain intact.
- User Windows CCID/HID concurrency work remains unready and unreviewed. No unfinished
  checkout was inspected. PIV physical touch/hotplug/recovery deferrals stand.

The `openpgp-001` handoff and hardware results preserve original failed setup,
intended-contract failures and successful scoped follow-ups. PIN/KDF identity,
X25519 framing and ATT read remain engineering findings. Independently reviewed
documentation, compiler binding, managed regression and hardware are later gates.
