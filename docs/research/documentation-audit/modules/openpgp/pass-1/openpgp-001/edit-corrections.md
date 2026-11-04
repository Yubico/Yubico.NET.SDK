# OpenPGP required-review corrections

**Editor:** GPT-6.1 Sol, 2026-11-04. **Status:** PGP-REV-001–005 corrected in the seven assigned
product paths, awaiting exact-identity independent re-review. This is a documentation-only correction batch.
The original `edit-report.md` remains immutable at SHA256
`5f91b64a4ad7f8309eee0731714b06e48c528d7c42f8a2efd2b9100e2013698c`.

## Verification before editing

Read `review.md` and `evidence/review-original-citations.md`, including the bounded RFC 4880 timestamp
follow-up. Checked each requested correction against the current affected contract and relevant executable
behavior without changing it:

- **REV-001:** User VERIFY exception XML and retry-property XML retained an unqualified blocking/count
  implication. `ResolveRemainingRetriesAsync` extracts 63Cx, maps 6983 to zero, and queries C4 for 6982;
  extended verification currently supplies Reset identity to that query. The existing distinct-counter test
  and independently reviewed hardware evidence support fresh User-counter inspection before a recovery decision.
- **REV-002:** The policy summary still said “once per session,” while Card §4.3/§7.2.2 and the existing
  remarks bind authorization to card access state. Reset, another application selection and specific commands
  can end that state independently of host-object lifetime.
- **REV-003:** Both retained test response arrays contain 6A82. Card §7.9 names that status file/application
  not found, and 6A88 referenced data/DO not found. The current F9 loader catches any ApduException and caches
  KdfNone. These fake responses establish the broad fallback/raw payload, not actual KDF absence.
- **REV-004:** The reset summary promised blocking/termination/reactivation. The implementation attempts
  blocking and ignores TERMINATE/ACTIVATE statuses; Card §7.2.17 even permits a successful dummy ACTIVATE in
  operational state. Existing remarks correctly require independent postcondition inspection.
- **REV-005:** The generation-time parser combines four bytes into signed int; the setter writes that int's
  four bytes without date-range/sentinel validation. Card CD defines unsigned-time context and zero's special
  meaning; a negative API value with the upper bit set represents the original unsigned field, not a pre-1970 date.

Read the retained original Card extraction directly: §4.3, §§7.2.2/7.9, CD table §4.4.1 (printed p24,
extraction 927–930), and ACTIVATE §7.2.17. The PDF is the functional specification Version **3.4.1,
March 18, 2020**, despite its URL's 3.4 filename. CD's wording says four big-endian bytes per ordinary
slot, seconds since January 1, 1970, and default 00000000 “not specified.” Retrieved the original
[RFC 4880, OpenPGP Message Format](https://www.rfc-editor.org/rfc/rfc4880.txt), November 2007,
IETF Standards Track: **203706 bytes**, SHA256
`575dc55f0edea3cb490ad92941e83bd755f674c20619efee0a5a5b721b32bf02`, matching the reviewer follow-up.
Its actual §3.5 states: “A time field is an unsigned four-octet number containing the number of seconds
elapsed since midnight, 1 January 1970 UTC.” RFC §3.5 supplies the unsigned time primitive; the Card
CD table supplies its special zero sentinel. This adds no newer packet-profile or validation-policy claim.

## Applied corrections and dependency map

| Review group | Exact edited units | Result |
|---|---|---|
| PGP-REV-001 / C001,C019 / IMP-001,019 | `IOpenPgpSession.VerifyPinAsync` exception XML; exception class remarks, `RetriesRemaining` summary/local remarks and constructor-parameter description | Describe the available reported/extracted/queried count. Identify the current extended-mode 6982 Reset-counter fallback locally; require fresh `GetPinStatusAsync().AttemptsUser` inspection before recovery decisions and diagnosis before another attempt. Admin-specific text remains unchanged. |
| PGP-REV-002 / C018 | `IOpenPgpSession.SetSignaturePinPolicyAsync` summary | State consumption per signature versus retention while card access state remains valid; preserve the existing authentication/invalidation remarks. |
| PGP-REV-003 / C016,C019 / IMP-016,019 | `OpenPgpInvalidPinExceptionTests.KdfNotFoundResponse` comment; `OpenPgpSessionWireTests.VerifyPinAsync_TransmitsVerifyWithUserPinPayload` comment | Name 6A82 accurately and explain the current broad F9-error/KdfNone fallback. Preserve fake bytes, helper names and asserted raw payload. |
| PGP-REV-004 / C005 / IMP-005 | `IOpenPgpSession.ResetAsync` summary | Describe the attempted PIN-blocking/TERMINATE/ACTIVATE sequence. Preserve SDK threshold, ATT exception, failure/partial-state and independent-readback remarks. |
| PGP-REV-005 / new C022 / IMP-018 | Complete `GenerationTimes` type contract; `DiscretionaryDataObjects.GenerationTimes` property; interface generation-time Get/Set units; README metadata task | Explain zero as unspecified, unsigned card seconds, current signed-int display above int.MaxValue, preservation of original bytes/unsigned interpretation, and the setter's unchanged four-byte write without date validation or special zero handling. |

No other product path was edited in this correction batch. No executable/help strings, imports, declarations,
assertions, directives or API snapshots changed. No builds, tests, formatter, hardware, agents or commits ran.
The coordinator retains ownership of final formatter/Roslyn preservation, compilation and execution gates.

## C022 addition — record only, coordinator triage pending

**PGP-C022: generation-time zero sentinel and unsigned field exposed through signed int.** Confirmed
documentation correction from expanded clean-API sampling, associated with existing deferred **PGP-IMP-018**.
The current parser/setter behavior remains unchanged. Future representation/sentinel/range/forward-compatibility
policy requires an implementation decision; these corrections describe the present carrier without inventing one.
Reopen `GenerationTimes.cs`, the discretionary property, interface Get/Set contracts and README metadata guidance
after that policy or implementation changes. The coordinator owns findings-ledger integration, clean-denominator
removal and further API-category expansion; this editor changes no coverage/findings/sampling records.

## Seven-path before/after snapshot

Before identities were captured before this correction batch and match the original review snapshot, including
its separately sampled GenerationTimes entry. Hashes below use exact working bytes, before coordinator formatting.
Paths are relative to `src/OpenPgp/`.

| Path | Before raw SHA256 | Corrected raw SHA256 |
|---|---|---|
| `src/IOpenPgpSession.cs` | b43c0bddd27120854db59e065746c743ea3529042c39a77d16f61ab2e8ba9d47 | 0326d6e87ab8dea3883d38f7b57f7abb4d089cee6ccd3dee237424a18e2999a9 |
| `src/OpenPgpInvalidPinException.cs` | 32131072cb993ce4630c99ac7e1829f1506fc949a6d0d4747b3e1b807ba22220 | cae01b440186b2b19d9b434aed04e5fda82fae45c495fe7f5024dc884586e55d |
| `src/DiscretionaryDataObjects.cs` | 0bb9ea6c281bdedb86f25224c1f0e65397fff055bdfc35ebc905c529e2c5f104 | 49aee7366b5f60ea2dde6d178c9cbbc7a37d2101fb91c314649a2ea1f2b2c1cb |
| `src/GenerationTimes.cs` | 9c2e0ebaaf0c233d542663298af34a4ea71e21d086a155fe45f4e8cad8645339 | 32cf3e8d5546f1d4dc007b284615c15cd856660823b53f6d07b3202622e0458f |
| `README.md` | a9d28e98804316fce007626cc7b579554023c964b882f13ccad44c28af5ed0b5 | db23caf2e117418d76c0c3bbc5c2e14ff84bf267d6c1e0dcd7276dccbfb4e811 |
| `tests/Yubico.YubiKit.OpenPgp.UnitTests/OpenPgpInvalidPinExceptionTests.cs` | f5a334155a99791c95f1c4ec33fabfbcf644fa40efc2680aad6de1a438593ce9 | 0e432c5b41d340606f927699fe8f0258f37e4c2fa56c5debf45092fc25913c63 |
| `tests/Yubico.YubiKit.OpenPgp.UnitTests/OpenPgpSessionWireTests.cs` | 43097e771b3d5e1d567d3a5b4be45b84868b7d70f3cc45572771334223796dcc | 17d6061c6c70feccfb1526f586b24b02d381423ca42090fa86bc4653daba8f19 |

The product set is now **59 paths / 49 C# files**, adding only GenerationTimes to the original authored 58.
The corrected ordered compact path/raw/body manifest SHA256 is
`ee40ed42dc14b34da939e55960f85c3ea293ba17a056ac4b3e1d1f8d059d9480`.
Reproduce using the original 58 product paths plus GenerationTimes, sorted by path; objects contain `path`,
`raw_sha256`, then `body_sha256` for C# only, with compact JSON separators as in the original edit report.

### Preserved executable/API token identities for the six corrected C# files

All six match their captured before identities and HEAD. The whole 49-file C# product set likewise matches HEAD
after excluding comments and whitespace. These are lexical checks, not the coordinator's Roslyn proof.
The hash procedure is unchanged from the original edit report.

```text
90cb3be471942777f866b2d141ba81bdced1f9e766b298dd5cee4edeb7e4ec7b src/IOpenPgpSession.cs
e93d6fdb45f2f5d8ac06066ef9a95daea502df3470b4103c16e8ad7a752be9e8 src/OpenPgpInvalidPinException.cs
d5173f29444800ae401fe04713a90f6436f9afd0f71806e28d4a76bda7883f22 src/DiscretionaryDataObjects.cs
4a8ee01fc4e105b692a197f3802af65884b878a299873ec7bef9fedcda2cd838 src/GenerationTimes.cs
e2300e556f1e8da26f2e5b454f8cf6d226689499ca813959bc9c624cf6069184 tests/Yubico.YubiKit.OpenPgp.UnitTests/OpenPgpInvalidPinExceptionTests.cs
eec2698b463d7d05812d1543d3cc429995af3dc066e780093491760eee2cc6b6 tests/Yubico.YubiKit.OpenPgp.UnitTests/OpenPgpSessionWireTests.cs
```

## Complete snippet identity refresh

All **12 complete fenced C# bodies / 197 body lines** are byte-identical to the independently reviewed
original editor snapshot. Only prose changed; group 7 and later opening line numbers moved by four.
Full-body hashing includes all text between opening-fence newline and closing fence, including the final newline.

| Group | Current opening line | Body lines | Unchanged full-body SHA256 |
|---|---:|---:|---|
| 1 | 50 | 20 | 5542ca500da6f1a37c534e01082300f70d3b8f51744c3fe47e6af0f20007a9c3 |
| 2 | 100 | 11 | 7ee760b780601580a24306e600e47871a9c24d3b92d318a38ddd95c68abf09aa |
| 3 | 120 | 16 | 96e614e3c4c87c063a6d0a1474d866d3a8f806a7569a7348b5c2d6a96a7db884 |
| 4 | 169 | 43 | 35c5b8c7782d30e9e026fce4f7b5921390b70dd56151c9d1175edaa25c4d56e7 |
| 5 | 229 | 9 | a7931e0fab00a90a0fd257662b9382b4b5beb529f2db9b71402d082c80d9f121 |
| 6 | 250 | 9 | a3485ea5b6911f8e4f6602037a075a0a8e5fd8c50771f6a8e02bfc1602a17eec |
| 7 | 289 | 37 | 2d963d0cdccd98cd64cb430c64518d2d897fe9b626408afb287a852b845fb8c8 |
| 8 | 346 | 17 | 754cbe59c499c0360b6c4f6697e1b4c3a54043c5dc808dcd8bebe8af94be32d3 |
| 9 | 382 | 11 | 7f232c1692697178f18b10a4113bc056dfb43b84bce378ac8ccf6f0e182f37d3 |
| 10 | 400 | 8 | 13023e0a198b307520c8060c4e9b271467aeddadcadc47ad127893952a708335 |
| 11 | 420 | 9 | 0823dc3a40e008e70a56579670018d53f610299bde0fdf1efa5d2d871a937aee |
| 12 | 465 | 7 | 64afa4ae312015f8bf4c44aeb8cbab9998bdd6fc9b6b05b2e7b1cd1015136c18 |

## Remaining ownership and evidence boundaries

All corrected XML fragments are structurally well-formed. Fresh lexical/hash/snippet checks are recorded above;
they establish no compilation or runtime result. The reviewer owns required-correction acceptance on these exact
identities; the coordinator applies final scoped formatting and refreshes post-format identities/Roslyn checks.

The coordinator reports completed hardware work and final public-state reconciliation, including ATT UIF Off and
the preserved certificate. `review.md` already reconciles late outcomes against product claims. This correction
batch requires no hardware-driven product change and independently claims no new device execution. Keep that
final evidence status separate from the earlier immutable editor checkpoint's then-pending observations.

Preserve all 14 deferred engineering groups and four Core questions. C022 extends IMP-018's return visit; it neither
adds a runtime fix nor authorizes new timestamp/API validation policy. No other RUN record is edited here.
