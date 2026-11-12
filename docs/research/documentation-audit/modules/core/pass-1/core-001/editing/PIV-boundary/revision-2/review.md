# Independent PIV revision-2 correction review

**PASS — full PIV documentation batch accepted at patch SHA-256
`dc35e1032236c9363640a280dc722b9c10c33c8c2567433946d5521da514a8ce`.**
No mandatory content/citation correction remains. This verdict combines the predecessor's
independent full-unit review with the focused, coordinator-authorized successor recheck. It
does not certify compilation, allocation-failure behavior, provider erasure or runtime results.
Apply this full successor instead of the predecessor full patch.

## Cleanup correction accepted — CORE-PIV-X003/X006

I read both complete current examples and neighboring task prerequisites/prose, the PivTool
directions, correction delta/ranges, report/response, path/resource decisions and exact old/new
body records. The authorized Markdown-fence changes close the original protected-scope gap:

- **Signing, `src/Piv/README.md:180–203`:** empty digest/signature holders are initialized
  before acquiring the PIN at line 185. The next statement enters try; hashing at line 188,
  PIN verification, metadata lookup, host-verifier acquisition, awaited signing and verification
  are all inside it. Finally clears the acquired PIN and successfully assigned digest/signature
  storage. Hash/setup failure now reaches that cleanup. The disposable verifier stays scoped
  inside try, while the awaited session encloses the example.
- **Decryption, `src/Piv/README.md:216–240`:** session, certificate and host RSA object have
  disposal scopes before PIN acquisition. Empty expected/plaintext holders precede the PIN
  at line 224; try follows immediately. Known-plaintext allocation, encryption, PIN verification,
  awaited decryption and comparison are inside it. Finally clears the PIN, successfully assigned
  known plaintext and returned plaintext. Ciphertext is not being mistaken for secret plaintext.
- If acquisition throws before returning storage, these examples have no successfully acquired
  buffer to clear. They make no promise about an unreturned allocation or provider's internal
  storage. Empty holders keep the shown finally safe before later assignments complete. The
  inline message fixture is public demo input; real additional source/provider/application copies
  retain their own cleanup duties.
- README lines 247–250 and PivTool's introduction/closing directions now describe these actual
  local scopes without claiming all-copy/provider erasure. The two changed inventory entries and
  README path reason agree. Shared imports, typed prerequisites, real APIs, format/oracle checks
  and resource owners are preserved; the fragments are not relabeled standalone compiled programs.

Exactly two Markdown bodies changed:

| Body | Accepted current body SHA-256 |
| --- | --- |
| Signing | `103e840c132a11514d5eb311163470add49d4ca27ba7ff272905154a08b79abb` |
| Decryption | `bcc54c0acd350e75df97a2de3ca16fd64247245ca5f6dba6a7d0cfecaaa93818` |

Their complete predecessor/current text and hashes are correctly retained in the successor
inventory. These documentation examples are the only authorized executable-looking body changes;
no actual SDK/test C# file changed from the reviewed predecessor.

## Fresh preservation checks

Read-only checks independently verified the exact full patch and its equality to the current
thirteen-file PIV diff; every source-pin/predecessor/current changed-file hash; all thirteen
saved predecessor products against the prior review; and all 121 current disposition hashes.
All ten changed-batch C# files remain byte-identical to the reviewed predecessors. Product
differences from that review are restricted to README and PivTool README.

All nineteen Markdown and 23 XML bodies are inventoried with matching actual hashes: two
Markdown bodies changed, seventeen Markdown and all 23 XML bodies stayed exact. Combined with
OpenPGP, this is **36 Markdown / 28 XML, two changed / 62 unchanged**. Fifteen predecessor
editing/report/review files and the sixteen successor records indexed by record-identities.json
match their recorded hashes. Provenance and all 31 outside-owner referrals are byte-identical
to the predecessor; whitespace checks pass. Earlier records, reviews, patches and reports were
not rewritten. The editor record index remains its original snapshot, not an index silently
regenerated to include this later independent review.

`review-identities.json` accepts the full patch and exact thirteen-path after-hash manifest,
not just the correction delta. The latter is
`ab97bd5d8df7afaf7d4b9f1e6b591d8c9c701ab7acc1066930cfe03fd0205b72` and is relative to the
reviewed predecessor; do not apply it after the full successor patch.

## Unchanged gates and handoff

No citation, SDK mechanism, assertion, runtime help or API changes accompany this correction.
Earlier reviewed ownership/version/authentication/PIN-only/prompt/reset descriptions and exact
source/revision/status/actor/strength/profile distinctions retain their acceptance and limits.
All historical IDs/statuses and engineering/policy holds remain. In particular PIVIMP-005 is
still confirmed/unfixed; serial 125's 6A88 precedes EXTERNAL AUTHENTICATE/RESET, and historical
device baselines are not new qualification. F9/private/attestation and unread protected PRINTED
restoration scope remain protected. Disposal/UNPOWER proves no observed physical PIN/power or
cross-process state; touch/hotplug/touch-dependent cancellation/recovery stay deferred. The
unready Windows concurrency delta remains a separate future handoff.

Compiler, independent token/API/cref checks, complete example compilation and runtime acceptance
remain coordinator gates. No build/test/formatter, crypto/provider/native/device operation,
Git mutation, subagent, product edit or editor/history rewrite ran. Only this review and its
identity file were written. Finish the existing Core Pass-1 validation/publication and deferred
handoff, then stop; no wider audit, implementation, tooling or planning project is requested.
