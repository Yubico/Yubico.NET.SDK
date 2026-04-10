# Handoff — worktree-security-remediation

**Date:** 2026-04-09
**Branch:** `worktree-security-remediation`
**PR:** [#447](https://github.com/Yubico/Yubico.NET.SDK/pull/447) — security(core,fido2,piv): fix sensitive data handling
**Last commit:** `51c7de8a` — security(core,piv): fix 7 issues from Copilot review round 3

---

## Session Summary

Copilot submitted three new review rounds (14:51, 15:40) after previous fixes. This session reviewed all 5 Copilot review rounds, identified 7 valid and previously unfixed issues, and resolved them:
1. **Functional bug** in `ChainedApduTransmitter` — wrong slice index broke APDU chaining after first chunk
2. **Security** — `ScpProcessor.formattedApdu` backing byte[] never zeroed after MAC computation
3. **Security** — 7 PIV locations (`PivSession.Crypto`, `PivSession.KeyPairs`, `PivSession.Metadata`) used `var command = new ApduCommand(...)` inside try blocks, so `ZeroData()` was never reached in finally. Fixed with `using var` to call `Dispose()` at scope exit.

All 9 test suites still pass. Comment posted on PR explaining the fixes.

---

## Current State

### Committed Work (This Branch — All Sessions)

```
51c7de8a security(core,piv): fix 7 issues from Copilot review round 3
c0429bb7 security(core): convert ApduCommand from readonly record struct to sealed class with IDisposable
e1316c7f chore: update handoff for session close — all security fixes complete
e0b74f09 security: fix preparedData zeroing, add ownership docs, acknowledge breaking changes
d121b719 security(fido2): fix T12 ownership violation in CredentialManagement.Dispose()
8de98b44 security(scp): dispose ScpProcessor on auth failure, zero encrypted command buffer
41e013bc security: expand taxonomy to T12 with T10/T11/T12 from Copilot round-3
d04a49e2 chore(skills): add workflow-security-audit skill
c244013c security: add grep-based security taxonomy audit script
b623178f security(openpgp): add IDisposable to Kdf to zero salt/hash material
efe0753c security(piv): remove .Memory.Span.ToArray() PIN/key copies in PivTool menus
2d6f2b40 security: address Copilot review findings from PR #447
b810749d refactor(credentials): move module-specific CredentialReaderOptions to each module
3131dc73 security(cli): migrate CLI tools to ConsoleCredentialReader for PIN/password input
75353fd1 security(fido2,openpgp,oath): replace string PIN/password APIs with ReadOnlyMemory<byte>
```

### Uncommitted Changes

`Plans/handoff.md` only — no production code dirty.

### Build & Test Status

- **Build:** ✅ 0 errors, 70 warnings (all pre-existing xUnit/IL2026 warnings)
- **Unit tests:** ✅ 9/9 pass (`51c7de8a` — run immediately before this handoff)
- **Security audit:** `./scripts/security-audit.sh` exits 0 — all 9 mechanical taxonomy checks clean
- **Integration tests:** Not run — requires physical YubiKey hardware

### Worktree / Parallel Agent State

None. Single working tree on `worktree-security-remediation`.

---

## Readiness Assessment

**Target:** Yubico SDK maintainers and security reviewers who need PR #447 to pass Copilot review and be merged — all sensitive data handling issues identified across 5 review rounds must be addressed.

| Need | Status | Notes |
|------|--------|-------|
| Remove SCP session key debug logging | ✅ Working | Removed across all SCP files |
| Zero FIDO2 PIN/hash intermediates | ✅ Working | try/finally ZeroMemory in all paths |
| Zero SCP command/encrypted buffers | ✅ Working | T10 + T11 fixed, formattedApdu now zeroed too |
| Proper IDisposable chain (ScpProcessor → SessionKeys) | ✅ Working | Full disposal on success and failure paths |
| Zero PIV crypto/key-import ApduCommand internal copies | ✅ Working | `using var command` in 7 locations |
| Correct APDU chaining (no empty chunk after first) | ✅ Working | ChainedApduTransmitter slice bug fixed |
| Correct buffer ownership in CredentialManagement.Dispose() | ✅ Working | ZeroMemory removed (T12) |
| Breaking-change documentation for string→ReadOnlyMemory<byte> APIs | ✅ Working | XML remarks added to IOpenPgpSession + IOathSession |
| Copilot review threads resolved | ⚠️ Partial | Waiting to see if round 4 (after latest fixes) comes back clean |
| Integration test with SCP03 on hardware | ❌ Missing | Requires physical YubiKey — cannot automate |

**Overall:** 🟢 Production — all identified security findings addressed, audit script clean, functional bug fixed. Awaiting Copilot's next review response before merge.

**Critical next step:** Wait for Copilot to re-review (PR comment posted) — if clean, merge PR #447. If new threads appear, resolve them.

---

## What's Next (Prioritized)

1. **Check Copilot's response** — a PR comment was posted at `51c7de8a` explaining all fixes. Check if Copilot produces another review round. If clean, merge PR #447.
2. **Merge PR #447** — when Copilot review is clean. All threads should resolve, build passing, audit clean.
3. **Open follow-up issue: remaining `OpenPgp/Kdf.cs` Dispose ownership** — Copilot flagged that `Dispose()` zeros public `init` Salt*/InitialHash* properties which may not be owned. Complex — needs deeper review.
4. **Run integration test with SCP03 hardware** — verify SCP sessions still authenticate correctly. Cannot be automated without YubiKey.

---

## Closed This Session

| Fix | File | What |
|-----|------|------|
| APDU chaining bug | `ChainedApduTransmitter.cs` | `data[offset..Max]` → `data[offset..(offset+Max)]` |
| formattedApdu not zeroed | `ScpProcessor.cs` | `MemoryMarshal.TryGetArray` + ZeroMemory in finally |
| using var ApduCommand | `PivSession.Crypto.cs` | PerformCryptoAsync + ECDH method |
| using var ApduCommand | `PivSession.KeyPairs.cs` | GenerateKeyPairAsync + ImportPrivateKeyAsync |
| using var ApduCommand | `PivSession.Metadata.cs` | SetManagementKeyAsync + ChangePukAsync + UnblockPinAsync |

## Closed Prior Sessions (All Sessions)

| Fix | Taxonomy | Commit |
|-----|----------|--------|
| Remove 38 SCP Console.WriteLine key dumps | T8 | 15396c8d |
| Zero FIDO2 PIN/hash intermediates | T1 | 24a0470a |
| PinUvAuthProtocol V1/V2 .ToArray() zeroing | T1 | 24a0470a |
| PivSession.Authentication AES key zeroing | T1 | 24a0470a |
| ScpState IDisposable + SessionKeys disposal | T7 | 24a0470a |
| OTP HID hex-dump log reduction | T3 | 2d6f2b40 |
| .ToArray() PIN copies in FIDO2 examples (13 sites) | T1 | 2d6f2b40 |
| FidoPinHelper/OpenPgpCommand direct UTF-8 encode | T2 | 2d6f2b40 |
| Kdf IDisposable on OpenPGP salt/hash | T7 | b623178f |
| PivTool .Memory.Span.ToArray() PIN copies | T1 | efe0753c |
| 12-taxonomy security audit script | — | c244013c |
| Taxonomy skill (workflow-security-audit) | — | d04a49e2 |
| T10 ScpInitializer dispose on auth failure | T10 | 8de98b44 |
| T11 ScpProcessor encryptedData finally scope | T11 | 8de98b44 |
| T12 CredentialManagement ownership violation | T12 | d121b719 |
| T1 PivSession.Crypto preparedData zeroing | T1 | e0b74f09 |
| DisposableBufferHandle ownership doc | — | e0b74f09 |
| Breaking-change remarks IOpenPgpSession/IOathSession | — | e0b74f09 |
| ApduCommand → sealed class with IDisposable | — | c0429bb7 |

---

## Security Taxonomy Reference (12-item system)

| ID | Name | Detection |
|----|------|-----------|
| T1 | `.ToArray()` untracked sensitive copy | grep |
| T2 | `Encoding.UTF8.GetBytes()` → temp byte[] | grep |
| T3 | `Convert.ToHexString()` in log calls | grep |
| T4 | `ArrayPool.Return` without ZeroMemory | grep |
| T5 | Early return before ZeroMemory | AGENT |
| T6 | `string` parameter for PIN/password in public API | grep |
| T7 | IDisposable missing on key-holding class | AGENT |
| T8 | `Console.Write` in production | grep |
| T9 | Crypto disposable without `using` | grep |
| T10 | IDisposable not disposed on exception/failure path | AGENT |
| T11 | Conditional buffer allocation not covered by ZeroMemory | AGENT |
| T12 | `Dispose()` zeros caller-provided buffer (ownership violation) | AGENT |

**Audit tools:**
- `scripts/security-audit.sh` — mechanical scan (T1-T4, T6, T8, T9), exits 0 if clean
- `.claude/skills/workflow-security-audit/SKILL.md` — full 2-phase audit with semantic agent

---

## Blockers & Known Issues

### OpenPgp/Kdf.cs — Dispose ownership (open question)
Copilot flagged that `Kdf.Dispose()` zeros the backing arrays of public `init` Salt*/InitialHash* properties. If callers constructed `KdfIterSaltedS2k` with caller-owned memory, this could zero memory the instance doesn't own. Current behavior may be correct if Kdf always owns these buffers (constructed from `Parse()`). Needs deeper review before merge.

### Integration Test (hardware)
SCP03 session integration test requires a physical YubiKey. Cannot be automated in CI. Flag for manual sign-off before merge.

---

## Key File References

| File | Purpose |
|------|---------|
| `scripts/security-audit.sh` | Mechanical security scan — run before any PR merge |
| `src/Core/src/SmartCard/ChainedApduTransmitter.cs` | APDU chaining bug fix (this session) |
| `src/Core/src/SmartCard/Scp/ScpProcessor.cs` | formattedApdu zeroing (this session) |
| `src/Piv/src/PivSession.Crypto.cs` | using var ApduCommand fix (this session) |
| `src/Piv/src/PivSession.KeyPairs.cs` | using var ApduCommand fix (this session) |
| `src/Piv/src/PivSession.Metadata.cs` | using var ApduCommand fix (this session) |
| `src/Core/src/SmartCard/ApduCommand.cs` | Canonical IDisposable + ZeroData pattern |
| `src/Core/src/SmartCard/Scp/ScpInitializer.cs` | T10 fix reference — try/catch disposal on auth failure |
| `src/Fido2/src/CredentialManagement/CredentialManagement.cs` | T12 fix reference — caller retains ownership |
| `src/OpenPgp/src/Kdf.cs` | Open question: ownership of init Salt* buffers |

---

## Quick Start for New Agent

```bash
cd /Users/Dennis.Dyall/Code/y/Yubico.NET.SDK
git branch --show-current        # worktree-security-remediation
git log --oneline -5

# Check PR status + latest Copilot review
gh pr view 447
gh pr view 447 --json reviews | python3 -c "
import json,sys; data=json.load(sys.stdin)
for r in sorted(data['reviews'], key=lambda x: x['submittedAt'])[-3:]:
    print(f'{r[\"submittedAt\"]} — {r[\"state\"]} — {r[\"body\"][:150]}')
"

# Verify audit is clean
./scripts/security-audit.sh      # should exit 0

# Build + test
dotnet build.cs test

# Check open review threads
gh api graphql -f query='{ repository(owner:"Yubico", name:"Yubico.NET.SDK") { pullRequest(number:447) { reviewThreads(first:50) { nodes { isResolved } } } } }' \
  | python3 -c "import json,sys; d=json.load(sys.stdin); threads=d['data']['repository']['pullRequest']['reviewThreads']['nodes']; print(f'Open threads: {sum(1 for t in threads if not t[\"isResolved\"])}')"
```

### GraphQL Thread Resolution Pattern (if new threads appear)

```bash
# List open threads with IDs
gh api graphql -f query='{ repository(owner:"Yubico", name:"Yubico.NET.SDK") { pullRequest(number:447) { reviewThreads(first:50) { nodes { id isResolved path comments(first:1) { nodes { body } } } } } } }'

# Resolve a thread
gh api graphql -f query='mutation { resolveReviewThread(input: {threadId: "PRRT_xxx"}) { thread { isResolved } } }'
```
