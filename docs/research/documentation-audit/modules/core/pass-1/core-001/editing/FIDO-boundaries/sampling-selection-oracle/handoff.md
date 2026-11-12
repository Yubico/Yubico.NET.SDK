# SAMPLE-WA-001: selection-test oracle comment

Corrected only the assertion-introduction comment in
`src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/Client/WebAuthnClientGetAssertionTests.cs`,
`GetAssertion_MatchedCredential_SelectAsync_IsIdempotent`, for selected unit
`e29522f04d40349a`. The before bytes matched accepted PRIMARY exactly (SHA-256
`9f12c9ace45d8fe8e534a700557c353a67fd4707a257cfd73def6bec0abd84d5`). PRIMARY was read-only.

The comment now describes equal CredentialId/Signature bytes and one backend GetAssertion call in
total. It no longer implies an instance-identity assertion or whole-response comparison. The test
name, assertions, imports, declarations, API behavior and fixture setup remain unchanged.
The patch tool also adds the missing final newline; every non-comment line remains identical.

## Source checked before editing

- Complete fixture: `WebAuthnClientGetAssertionTests.cs:1–533`, including constructor setup at 36–51
  and complete selected method at 132–163. The mock returns one credential response; the test obtains
  the first match and selects it twice. Its two equality assertions compare only CredentialId and
  Signature arrays. `Received(1).GetAssertionAsync` checks one backend call. There is no `Assert.Same`
  or full-response equality assertion.
- Complete implementation: `src/WebAuthn/src/Client/Authentication/MatchedCredential.cs:1–96`.
  Constructor lines 60–69 retain a `Lazy<Task<AuthenticationResponse>>`; SelectAsync at 92–95 returns
  its cached task through WaitAsync. The actual cached-instance behavior is distinct from the test's
  narrower oracle and is not changed or disputed by this comment correction. No library clarification
  or new external research was needed.

## Return and preservation

- `focused.patch`: the exact fixture diff against accepted PRIMARY / `before.snapshot`.
- `verification.json`: exact before/after hashes, patch hash, changed ranges, source identities and
  static preservation results.
- `preserved-inputs.json`: all prior FIDO editor/review/revision records and the pre-edit 310-path
  product identities. Earlier full 15-file patch, decisions, reviews, example bodies and history are
  preserved, not regenerated or overwritten.

The sampler owns edited-unit credit and any WebAuthn test-stratum expansion. This bounded correction
allocates no runtime finding or engineering ID and makes no wider fixture/module clean claim.
Focused independent review remains pending. Static preservation is not an executed test, compiler
result or device qualification. No SDK build/test, formatter, cryptographic/native/device call,
Git mutation, subagent, PRIMARY write or whole-module audit occurred.
