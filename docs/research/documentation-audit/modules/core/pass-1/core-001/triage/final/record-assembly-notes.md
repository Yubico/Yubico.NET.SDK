# Record assembly notes

The first records-only join stopped before candidate output assembly: it assumed Packet C used
`investigation/C/candidates.md`. Packet C's completed authority is `findings.json`; that explicit
record route was corrected. The failure was a `FileNotFoundError`, not an SDK/source/test result.
Only the new `preserved-inputs.json` had been written. Earlier maps and product files were untouched.

A second record-only attempt stopped on the OpenPGP project file's recorded mixed newline form:
19 CRLF endings and one final LF, versus the pinned LF blob. The completed boundary file-identity
record explicitly qualifies normalized equality. The join now admits that exact representation only
after matching the original inventory raw hash and normalized pinned bytes. This is checkout
representation handling, not source drift or new semantic investigation. No candidate output had
been written at that stop.
