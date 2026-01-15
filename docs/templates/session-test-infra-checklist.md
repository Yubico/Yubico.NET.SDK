# Session Test Infrastructure Checklist (Template)

Use this checklist when adding a new session module.

## Shared test helpers

- [ ] `WithXxxSessionAsync` helper exists (shared test project)
- [ ] Helper supports:
  - [ ] unauthenticated session
  - [ ] authenticated session (if applicable)
  - [ ] custom `ProtocolConfiguration` (when relevant)
  - [ ] passing known firmware version (when supported)
- [ ] Test helper ensures disposal of session and connection

## Unit tests

- [ ] Unit tests cover:
  - [ ] parsing/encoding models
  - [ ] error conditions + argument validation
  - [ ] idempotent initialization behavior (where applicable)

## Integration tests

- [ ] Integration tests are clearly marked (hardware-required)
- [ ] Firmware minimums are explicit in test attributes
- [ ] Tests avoid relying on device global state unless reset is part of the test
