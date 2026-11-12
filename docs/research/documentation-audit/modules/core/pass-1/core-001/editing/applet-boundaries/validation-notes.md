# Static validation receipt

Run in `/home/rupicola/Code/y/yubikit-core-docs-edit-applet-boundaries` only:

```text
python3 docs/research/documentation-audit/modules/core/pass-1/core-001/editing/applet-boundaries/collect-static.py
eligible_paths: 233
edited_paths: 37
examples_all_languages: 107
CSharp_checks: 27
static_issues: []
missing_local_link_targets: []
```

The successor collector also runs `git diff --check` and records its success. Its excluded-path raw
ledger comparisons and explicit BOM/CRLF Git comparisons pass; the earlier raw comparison failure is
retained in static-attempt-1.txt. The source checks preserve all non-comment C# lines exactly apart
from terminal newlines. XML fragment parsing is structural, not compiler/cref/API validation.

A separate read-only link/fence check over edit-applet-boundaries.md, citations.md and
decisions-and-referrals.md found balanced fences and no missing target files across this worktree plus
the primary read-only input root. Four citations.md source-register targets resolve in the primary
only (SecurityDomain, Oath, YubiOtp, YubiHsm); they await the coordinator's normal record integration.
No external link/anchor/renderer check or independent content/citation acceptance is claimed.

All example bodies are inventoried with exact text/ranges/hashes. No C# compilation, SDK test,
formatter, native/device/cryptographic/HSM-connector operation, index/ref/commit/push or subagent ran.
This receipt supports static preservation only. Examples remain VALID-NOT-CHECKED in this edit.
