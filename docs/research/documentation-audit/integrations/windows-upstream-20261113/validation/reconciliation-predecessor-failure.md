# API reconciliation assumption correction

The first static reconciliation exited 1 at its assertion that all 20 API snapshots also matched
the incoming parent. All 20 match the audited parent. The incoming comparison instead retains the
audit's three accepted public Base32 entries in `Core/PublicAPI.Unshipped.txt`. No snapshot or product
file was changed. The successor reconciliation records both-parent comparisons explicitly; this
failed assertion remains evidence of the corrected assumption, not a failed compiler or runtime test.

The second attempt exited 1 because it compared the packaged README to the full module README.
`Directory.Build.props` deliberately packages `src/<Module>/PACKAGE_README.md` byte-for-byte as
`README.md`. The successor uses that declared source. No package failure or package mutation occurred.
