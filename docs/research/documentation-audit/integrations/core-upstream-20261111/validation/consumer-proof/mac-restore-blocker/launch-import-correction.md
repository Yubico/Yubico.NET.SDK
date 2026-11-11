# Routing CLI import-context correction

The coordinator reported an outside-logger Python 3.14 failure before descriptor or SDK admission:
argparse's colorization/dataclasses import path reached the same-directory diagnosis helper
`mac-restore-blocker/inspect.py` instead of standard-library inspect, producing a missing `signature`
AttributeError. This is a helper-namespace/import-context failure, not another restore/auth/SDK fault.
No exact raw coordinator transcript file was supplied here; this records the reported failure rather
than inventing one. The original helper path/bytes and all v2 files remain unchanged.

The old `restore-bound-first` descriptor and partials are absent. A fresh `restore-isolated-first`
suffix is proposed anyway. No creator, checker, SDK command or capture ran during this correction.

`proposed-lane-v3.sh` differs from v2 only in:

1. `python3 -P -B` for the routing helper's **create** invocation.
2. `python3 -P -B` for that same helper's **check** invocation.
3. One extra build `--retain-text` for this versioned correction receipt, linking the actual v3 caller
   to the unchanged routing descriptor's v2 baseline-launcher identity. The descriptor itself, its
   three properties/Core admission hash, atomic creation, build retention and post-build gate are unchanged.

No blanket safe-path flag is added to proof-v5 or the logger: their deliberate local imports remain
available. routing-v2 imports only stdlib modules and locates data from `__file__`; it has no local module
dependency. Python 3.14 `-P` excludes its script directory from the unsafe initial import-path entry.
There is no helper rename/delete, stdlib patch, global PYTHONPATH, credential or environment-policy change.

Actual harmless CLI validation passed:

```bash
python3 -P -B docs/research/documentation-audit/integrations/core-upstream-20261111/validation/consumer-proof/mac-restore-blocker/routing-v2.py --help
```

It exited 0 with the expected create/check argparse help. Isolated Python resolved inspect to
`/usr/lib/python3.14/inspect.py`, with callable `inspect.signature`. Bash syntax and exact three-edit
source comparison also pass. This closes the AST-only/import-chain gap, not the unexecuted Mac build.

Next coordinator command:

```bash
cd /home/rupicola/Code/y/yubikit-core-docs-integration
P=docs/research/documentation-audit/integrations/core-upstream-20261111/validation/consumer-proof
bash "$P/mac-restore-blocker/proposed-lane-v3.sh" restore-isolated-first
```

The run still must prove actual `[1.18.1, )` / integration-variant restore metadata and pass the original
v5 compilation/capture/live guards. No success, authentication or Mac/runtime qualification is promised.
