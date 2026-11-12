# Boundary record correction

The first final record check failed because the investigator had misread the credential retry helper.
An intermediate draft and commentary claimed the asymmetric test expected `Invalid credential password`
while the helper emitted `Credential password verification failed`. The latter text was an investigator
error. It is not present at the relevant helper site.

Fresh complete helper context, `src/YubiHsm/src/HsmAuthSession.cs:974–986`, distinguishes management
message `Management key verification failed` from credential message **`Invalid credential password`**.
`HsmAuthRetryExceptionTests.cs:204` expects that same credential substring. The pinned source and
inventory hashes remained unchanged throughout; there is no source/output-binding or device failure
finding from this assertion. The historical passing-run record needs no reopening on this mistaken basis.

The mistaken addition to X007, its unit-candidate association and the summary claim were removed.
The final ledger returns that test unit to read-clean; X007 retains its separate source-backed exception
XML scope correction and original diagnostic/test-oracle aliases. Final counts are eight candidate,
73 context and 79 clean units. `verification-attempt-1.txt` retains the failed checker output; the
successor checker verifies the actual matching strings without executing an SDK test.

This correction is investigator bookkeeping, not independent review or product remediation. No SDK
code, test assertion, frozen archive, central record, device state or historical result changed.
