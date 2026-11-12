# Source authority and bounded reuse

SDK source is fixed at `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`; metadata checkpoint
`da66f5d01ba594993974ddd26723e8c300c0a454` changes no implementation authority.
`source-manifest.json` binds all 121 complete candidate files, selected unit/range identities and
the consequential reused records/originals to SHA-256 and pinned Git identities where applicable.
Original PDFs/text captures and frozen runs remain in place; this packet creates no duplicate
checkout, imports no Python/vendor package and runs no cryptographic computation.

## Consequential authorities

| Authority / exact revision and status | Section / actor / strength | PIV applicability and bound |
| --- | --- | --- |
| NIST SP 800-73-4, May 2015, updated February 8, 2016; withdrawn July 15, 2024 and superseded by revision 5 | Combined three-part original retained in `piv-001/evidence/nist-sp800-73-4-part1.pdf` despite local filename; Part 2 §2.4.2, derivative newline coordinates 3792–3823; card authentication security indicators shall update; PIV application status clears on application change, Global PIN status differs | Original combined text reread. It explains application/card state, with no per-handle/process/PCSC isolation rule and no YubiKey PIN Once-until-reset guarantee |
| Same revision 4 original | Part 2 §2.4.3, derivative 3830–3856; card profile shall enforce 6–8 ASCII-decimal PIN bytes/right FF padding; PUK shall be 8 bytes and may contain binary octets | SDK byte-length checks and digit-filter UI policy differ; profile adoption is a deployment decision. No revision 5 compliance upgrade |
| NIST SP 800-73-5 Part 2, July 2024 Final | §2.4.2, B1 original-source extraction and final-publication record; card SHALL authentication/application/global-state distinctions | B1's direct PDF returned 403; extracted text is retained, not authenticated PDF bytes. Revision 4 original corroborates narrower state distinction; independent revision 5 original review remains required for retained citation |
| PC/SC Workgroup Part 5, revision 2.01.01, September 2005; corrected Final Release 2.01.00 name history | B1 retained original PDF/text; §2.1 resource-manager access/transactions and §3.2.5.2 desired disconnect disposition/end transaction if possible | Supports requested disposition/resource-manager actors, not physical PIN/power isolation. Windows and pcsc-lite C APIs retain their own contracts |
| Microsoft WinSCard API source snapshot retained by B1, accessed November 11, 2026; no pinned Windows build | SCardConnect share mode, SCardDisconnect dwDisposition and SCardReleaseContext status; API contract for resource-manager/handle owner | UNPOWER requests power down; native return status and device-state observation remain separate. No native calls ran |
| .NET runtime tag v10.0.0, B1 retained `Task-v10.0.0.cs` | WaitAsync/CancellationPromise:2832–2939; runtime implementation removes continuation/disposes timer/registration when wait promise completes | Reread actual source. A wait-only timeout/cancellation leaves original operation owned by its original task; no hardware cancellation inference |
| Context7 `/dotnet/docs`, queried November 12, 2026; retrieved current docs corpus, no immutable revision returned | “Cancel non-cancelable async operations,” “Understand the three cancellation meanings,” “Handle abandoned operations safely,” and WaitOnlyTimeoutDemo | Orientation for operation-versus-wait cancellation. Pinned runtime source above supplies implementation-specific evidence; no new API example or experiment was executed |
| Python YubiKey Manager `4ca60f706af930459138d8dc0f0f953480e1c7a4` | Retained `piv-001/evidence/python-yubikit-piv.py`: constructor697–723, Bio/reset736–785, sign1135–1160, decrypt1162–1183; `python-ykman-piv.py`:PivmanData209–273 | Immutable implementation comparator. Python establishes SCP before version probe; C# PIV probes FD before SCP. Its reset preflight and ciphertext-length decrypt differ; neither is normative desired C# policy |
| Same immutable Python pin | PivmanData timestamp getter 214 and encoder 259–260 use big-endian four-byte struct; protected object 264–273 stores nested key | Original source reread. Supports existing PIVIMP-009 interoperability difference, not a NIST timestamp requirement |
| Yubico ResetPivCommand vendor capture, retained from November 5, 2026; unversioned API guidance | `vendor-reset-command.txt:29–63`: retain F9, require blocked PIN/PUK, Bio MPE uses device-reset route | Reread capture; vendor capability/routing advice is separate from campaign authorization and current fixture state |
| Original PIV source register, frozen `piv-001/evidence/source-analysis.md` | Exact SP 800-78-4 / FIPS 201-2 / FIPS 186-5 / RFC 8017 / RFC 8032 / RFC 7748 / RFC 3279 and vendor status/section/actor distinctions | Reused historical adjudication, not a fresh independent reread of every crypto/profile clause. This packet makes no new mathematical or profile-conformance claim |
| GP SCP03 v1.1.2 Public Release, March 2019; SCP11 v1.3.0.13 Public Review, August 2023 | B6 final source register/manifest: final SCP03 empty/status/Le and draft SCP11 variant/host-auth phase | Exact retained original hashes/clauses remain in B6. Code-cited SCP03 v1.1.2.6 Public Review exact clauses, final SCP11 v1.4 and complete YubiKey profile remain unverified; PIV call sites inherit qualification, not certification |
| ISO/IEC 7816-4:2020 edition 4, amendment 1:2023, catalogue confirmed 2025 | B6 official catalogue/abstract only; exact target APDU/Le/channel/6C clauses unavailable after bounded follow-up | Report observable formatter behavior and leave exact ISO conformance open; no guessed subsection or unseen host-MUST |
| Published NativeShims 1.18.1 source / platform header evidence retained by B4 | B4-C006/Q001: common PCI pointer producer and platform-specific widths/reader encoding | Exact installed producer/header correspondence remains unverified. PIV reaches common SmartCard send; source or package names do not identify loaded artifacts |

The original PIV source-analysis derivative coordinates count newline-delimited lines. Python
`str.splitlines()` additionally splits form-feed page separators, so its transient numbered view
can differ from `read`/ripgrep on extracted PDF text. Section titles, original byte hashes and
newline coordinates identify the citation; this packet preserved every derivative byte.

## Reuse and scope

All eight completed family reports A/B1–B6/C, the editing release, inherited registry/routing/seeds,
prior PIV publication handoff/findings/source analysis and original PIV engineering definitions were
read. Narrow Core source reads checked actual shared session/options/presence/exchange/PCSC/response/
TLV/SCP-parameter call paths; those primary units retain their family owners and receive no duplicate
coverage credit. The packet uses the prior source and Python evidence rather than new web research.

The only new library-documentation lookup was the bounded Context7 cancellation query. It required
no native, SDK or device execution. One guessed historical evidence path `piv-001/evidence/sources.md`
was absent; directory inspection located the actual `source-analysis.md` and original manifest.
That navigation failure changed no source or historical record.

Native producer, actual release/erasure, profile adoption, exact ISO clauses, current device state and
the user's future Windows delta remain explicit qualifications. Fresh editing, independent original-
citation review and hardware qualification are separate gates, not an indefinite research queue.

The coordinator's active editing-release metadata changed during this packet. Its first observed
hash remains in the manifest, while the reread current bytes/hash are retained separately in
`metadata-input-current.md`. This is a live metadata input, not a frozen source archive. The packet
records that the intermediate observed bytes were not retained, rather than claiming their byte
preservation or changing the implementation pin. See the failed and final static verification receipts.
