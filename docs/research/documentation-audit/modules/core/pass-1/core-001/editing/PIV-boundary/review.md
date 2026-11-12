# Independent PIV boundary content/citation review

**Verdict: CORRECTIONS REQUIRED — one bounded example-cleanup qualification.** This is a
fresh review of CORE-PIV-X001–X006, separate from OpenPGP, not another 513-unit/module audit.
The reviewed patch SHA-256 is
`5a442195caab2674e3162a35bdffe9d4ab58e54ad72d7f33f6beeb20ab1ae3dd`.
No complete-batch accepted hash is issued before the correction below is rechecked.

## Mandatory correction

**Limit the examples' cleanup claim to the protected scope actually shown.**
`src/Piv/README.md:180–201,215–237`, CORE-PIV-X003/X006, creates PIN storage before
the corresponding `try`: signing creates `pin` at line 183, then evaluates the hashing
call at line 184 before entering `try` at line 186. If that setup fails, line 198's
clearing never runs. Decryption similarly creates `pin` at line 221 and `expected` at
line 222 before entering `try` at line 224. Its finally covers work inside that try,
not failures during the preceding setup. No failing provider or allocation was executed
in this review; these are source-visible conditional lifetime boundaries.

`editing/PIV-boundary/example-inventory.json:143,155` nevertheless describes both as
“complete caller cleanup”; the README path reason at `path-decisions.json:44` repeats
that unqualified claim. `src/Piv/examples/PivTool/README.md:4–5,145–149` directs readers
to the examples as complete cleanup scopes without making the setup boundary explicit.

Add a short local qualification explaining that the shown finally covers the operation
after setup, and that production adaptation must protect each acquired secret immediately,
including later setup/hash failure. Make the two inventory resource-scope explanations and
the README retention reason equally precise. The existing example bodies can remain exact;
do not silently change executable SDK/test source or claim an allocation-failure test ran.
This is the released example/ownership documentation facet, not a new Core bug, a request to
repair the SDK or an optional style preference. Clearing one buffer also does not clear an
independent provider/source copy.

## Other content and citations

I read the complete thirteen changed product files, patch, delivery report and editing records,
the approved style/house rules and current primary release, latest PIV source/candidate/path/
dependency/provenance packet, and the actual relevant factory, backend, management/PIN-only,
crypto/response/TLV/prompt and test-helper/assertion context. Retained reasons are substantive
and consistent with the completed source investigation apart from the cleanup overstatement
above; no silently omitted required product D facet was found in the six proposals.

- Factories bind before SELECT, read FD before effective-version configuration/SCP, rebind the
  backend and discover management type before readiness. Direct creation borrows; successful
  convenience creation transfers hidden ownership. Failed creation and caller-owned SCP/prompt
  lifetimes are not conflated with physical release.
- Protocol authentication, local management authentication and card PIN state stay separate.
  Detected-version management fallback is independent of the effective override; major-zero
  fixtures prove SDK policy rather than physical 5.8 firmware. PIN-only active-key checks,
  6982-only candidate rejection, restoration order and default-before-delete remain intact.
- Core guards each logical exchange, not authentication's two sends or whole PIN-only/reset/
  metadata-plus-crypto tasks. PIV request/resolution and local unpadding are outside that guard.
  Admitted traffic uses None; outer-wait cancellation and protocol drain are not public-task
  completion or permission to release borrowed inputs. Reset retains its protocol with no
  reselection/handshake. No OTP callback-inside-guard self-wait conclusion is imported.
- Actual response Clear reaches its copied owner, including SW, not constructor inputs,
  transport/chained/SCP/provider storage. TLV aliases and independently transferred outputs
  remain distinct. The internal X25519 result is not described as an EC point. The parser's
  missing algorithm-specific result-size admission remains PIVIMP-007, not a repaired invariant.
- Changed tests now name membership versus full order/erasure, synthetic version policy,
  Data-only observation, timing-dependent GET overlap, the unawaited-read assertion-failure
  gap and between-await hotplug windows. The existing reopening expectation is not converted
  into unconditional native release. Assertions and executable messages remain unchanged.

For consequential citation context I checked the retained original SP 800-73-4 derivative,
Part 2 §§2.4.2–2.4.3 (newline lines 3780–3879), and the complete manufacturer ResetPivCommand
capture, especially lines 29–63. Revision 4 is May 2015/updated February 8, 2016, withdrawn
July 15, 2024 and superseded; its card SHALL state/ASCII-decimal PIN and binary-permitted PUK
requirements are not universal SDK input or per-process isolation rules. Manufacturer F9/Bio
reset routing is not authorization or observed device state. Existing RFC 7748 (IRTF
Informational, January 2016, §§5–6.1), crypto/profile and vendor-gate statements retain their
original authority map in source-analysis.md; no new mathematical or profile claim is added.
Python remains comparison evidence at `4ca60f706af930459138d8dc0f0f953480e1c7a4`.

The original PC/SC/API evidence establishes requested UNPOWER, not observed physical power or
PIN isolation. Revision-5 NIST PDF acceptance, exact deployed native/header correspondence and
unseen ISO clauses remain gaps. B6's SCP03 1.1.2 Public Release versus 1.1.2.6 Public Review,
SCP11 1.3.0.13 Public Review versus unverified final 1.4/profile, and a/c versus b host-auth phases
are preserved. Setup flags, generic version/catalog data and plaintext PIV success certify none
of those profiles.

## Preservation and handoff

Fresh read-only checks verified thirteen product before/after hashes and the exact patch, all
121 disposition paths/reasons and 31 unchanged referrals, nineteen unchanged Markdown bodies,
23 unchanged XML bodies, 124 well-formed XML blocks and `git diff --check`. Every changed C#
line is a standalone comment and every other complete source line is identical to pinned Git,
preserving imports/directives, declarations/defaults/attributes, assertions and executable help.
All PIV editor input hashes still match the primary records. OpenPGP's separate inventory
omission makes the combined XML denominator 28, not 25; PIV's own count of 23 is correct.

The eleven routed keys exactly match the original packet, with `piv-001:PIVCORE-001..003`
retaining their spelling/aliases. PIVIMP-001–010 and earlier narrow resolved scopes remain as
defined in inherited-outcomes.md and the original findings. **PIVIMP-005 remains confirmed/
unfixed**: historical tool RSA failed original-message verification and Ed25519 verified
SHA256(message), while direct SDK controls passed. Documentation and generic Core metadata
have not fixed it or expanded hardware qualification.

Serial 125's two 6A88 results were INITIALIZE UPDATE failures before EXTERNAL AUTHENTICATE or
RESET. Dated serial 103/125 baselines are not new acceptance or current state. F9/private/
attestation material and deliberately unread protected PRINTED restoration scope stay protected;
unread PRINTED supplies no independent byte-absence claim. All physical touch/hotplug and touch-
dependent cancellation/recovery lanes remain DEFERRED, including their historical prerequisites.
The unready Windows concurrency delta requires its separate future pin/evidence/review handoff.

Only this review and its identity file were written. No product/editor-record edit, SDK build,
test, formatter, crypto/provider/native/device operation, Git index/ref/commit/push or subagent
ran. Static preservation is not compiler binding, cref/API acceptance, example compilation or
runtime qualification; those remain coordinator gates. Recheck only this bounded correction,
finish/publish Core Pass 1 and the deferred handoff, then stop.
