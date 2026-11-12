# Static receipt preparation failures

These are record-tool failures, not SDK/compiler/test results. They changed no product behavior.

1. The first `record.py` invocation stopped while looking up
   `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:docs/research/documentation-audit/core.md`:
   `fatal: path 'docs/research/documentation-audit/core.md' exists on disk, but not in 'fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef'`.
   The later audit metadata has a base/current identity but no product-pin blob. The corrected receipt
   records null for that absent pin identity instead of inventing source presence.
2. The first final status-scope check used default `git status --porcelain`, which collapsed the new
   owned record directory to `?? docs/research/documentation-audit/modules/core/pass-1/core-001/editing/`.
   Its exact failure was `out-of-scope status: ?? docs/research/documentation-audit/modules/core/pass-1/core-001/editing/`.
   The successor uses `--untracked-files=all` to check actual file paths. No unexpected file was removed
   or reclassified as owned merely to satisfy the check.

The latest `static-preservation.json` and `final-checks.json` give the successor results and limits.
