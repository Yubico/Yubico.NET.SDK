# OpenPGP correction response — editor recheck

**Status: source-verified corrections prepared; independent successor review pending.**
This responds to the three mandatory items in the byte-preserved predecessor `../review.md`.
It is not a second independent review or a complete-batch accepted hash.

1. **Reset actor/return:** current `src/OpenPgp/README.md:529–531` distinguishes `Task ResetAsync`
   cache refresh from the example's additional `GetApplicationRelatedDataAsync` read/return.
   Checked actual `OpenPgpSession.Reset.cs:25–75` and `OpenPgpSession.cs:165–173`; no contradiction.
   Unchecked E6/44, partial-state, ATT and retained-SCP qualifications remain unchanged.
   Maps to CORE-PGP-X005/X008.
2. **Actual state names:** current `src/OpenPgp/CLAUDE.md:37–39` names
   `IOpenPgpBackend? _backend` and inherited `ApplicationSession.FirmwareVersion`.
   Checked actual fields `OpenPgpSession.cs:70–74`, local detected/logged version/configuration
   `:135–159`, and Core property/configuration `ApplicationSession.cs:73,392–394` in this worktree.
   No `_version` field or retained raw-version member is inferred. Maps to X001/X006.
3. **Omitted XML layouts:** the complete 93-path scan now accounts for five XML bodies. The
   following three whole source files were read, retained, and independently hash-matched to
   the review's values and pinned content. They are illustrative protocol/layout fragments,
   not executable C# or new semantic-unit investigation credit. Maps to X002/X003/X006/X008.

| Body | Exact retained `<code>` body SHA-256 |
| --- | --- |
| `src/OpenPgp/src/ExtendedCapabilities.cs:24–32` | `7f3c2b6be7ad1485d6a4a94ecac0f249349d41fd49a38776d545ae30c7233a1f` |
| `src/OpenPgp/src/OpenPgpAid.cs:24–29` | `a0c9d14d41535ea38c4d268ed5a0052b6135aa7dc1003a091ccb5be7c3829f1f` |
| `src/OpenPgp/src/PwStatus.cs:23–31` | `9cc46a1ddf217db965cafbbab45ccf6d2d32ca72afe34a9ed5e484902c906f6f` |

The successor inventory and report correct the prior two-body count without changing those
prior records. All 17 Markdown and five XML bodies are unchanged. Existing consequential
citations retain the exact edition/status/section/actor/strength/application qualifications
in the predecessor citation notes and independent review; these corrections add no normative
claim or new external/library API question. No compilation or runtime proof is manufactured.
