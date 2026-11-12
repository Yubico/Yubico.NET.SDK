# Independent Sol confirmation: EX-R1 output scope

## PASS — pilot readiness released after checkout handoff

The explicit coordinator approval satisfies EX-R1's scope-authorization alternative. The **17-body A/C
compile pilot is ready once the current unit run releases exclusive checkout ownership**. No routing
code change or further compiler framework is required. Actual compilation and compiler/output
acceptance remain the coordinator's later gate; this confirmation grants no example execution.

Source baseline remains `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`. Observed metadata HEAD was
`da66f5d01ba594993974ddd26723e8c300c0a454`; metadata advancement alone does not invalidate this scope
approval. The original review remains unchanged at
`fac2e84d2bb68242f24859d4de2cd9b7bc8813d8d08e7e97c216ad5cafb9185c`.

## Accepted scope and exact delta

- `approved-scope.json` explicitly authorizes ordinary primary
  `src/{Core,Piv,Ctap2,WebAuthn}/src/{bin,obj}` clean/rebuild/restore trees. DocExamples alone uses its
  dedicated `core-001/jobs/Yubico.YubiKit.Audit20261111.DocExamples/{bin,obj}`. All are home-backed.
- The corrected preparation accurately distinguishes source/configuration before/after equality from
  preservation of disposable SDK output bytes. It makes no claim that excluding `bin/obj` preserves
  their contents or that rebuilt bytes substitute for earlier historical outputs.
- The project closure is exactly the original host plus those four SDK projects. Other worktrees,
  frozen archives, historical external copies, `/tmp` copies and the separate qualified DocCheck jobs
  directory remain outside this build's authorized output targets. No such output was written by this
  review. This is scoped verification, not a new whole-archive or other-worktree preservation claim.
- Current descriptor SHA-256 is
  `4fb843caa8e7faf5852382d664d2418e05bd1816753f544fbae1f46b9303213c`.
  Its **185 rows** retain all **183 original rows** exactly and add only the explicit scope approval
  and corrected preparation. No arbitrary source/tool hash was repinned. The old descriptor,
  preparation, provenance and static report remain byte-exact snapshots.
- All accepted A/C source documents, XML bodies, wrappers, host/configuration, capture hook, run wrapper,
  qualified parser and logger participants are unchanged. Manifest SHA-256 remains
  `f864583c4f36050eb1543a1f4cc1ca2a78c7001577ec8ea13c4715575b6b6f60`:
  **17 prepared bodies** (four complete / 13 typed fragments), **two internal exclusions** and
  **22 historical exclusions**. No IVT, fake helper or body rewriting was introduced.
- The unchanged capture plan binds actual Csc reference/input/output paths and hashes; it does not
  impose guessed dependency DLL paths. Final host DLL equality to actual compiler output, original
  reference/PDB identities, strict zero-warning compilation and complete original command evidence
  remain mandatory. Compiling these bodies establishes no runtime/provider/device behavior.

## Fresh read-only checks

Inspected the correction and verifier before executing:

```bash
python3 -B docs/research/documentation-audit/modules/core/pass-1/core-001/validation/examples/output-scope-correction/verify.py
```

Exit **0**. It checked the preserved predecessor hashes, original review, 183 unchanged rows, exactly
two added inputs, actual five-project closure/output ownership, unchanged owned-record hashes and
the original extraction/wrapper verifier. The latter again passed exact body reconstruction, library
and selector context, root imports and producer/late-hook readiness. The current source/context
population was 682 files; this is not a compilation count.

Separate byte reads verified the submitted successor descriptor hash and **36 output/reference/PDB
identity rows** against the existing DocCheck build seal's original external files. Qualified
foundation/logger/binder and checker-profile hashes remain their accepted values. This review did not
invoke a build, runtime, compiler capture, unit test, process qualification or device/native operation.

## Coordinator sequence

Wait for the current unit run to release the primary checkout; do not overlap its compiler/output
ownership. With exclusive release and the current build skill loaded, run:

```bash
E=docs/research/documentation-audit/modules/core/pass-1/core-001/validation/examples
python3 -B "$E/verify.py"
python3 -B "$E/run.py" --label examples-ac-first
```

Accept actual compilation only after inspecting the successful zero-warning toolchain RESULT, complete
original v2 receipt/streams/retention/quiescence, before/actual Csc membership and final output evidence,
and `accepted-compilation.json`. Preserve any first failure and return source diagnostics to the
accepted example owner. No example DLL is run. Existing source-diagnostic and fresh-attempt rules stand.
No duplicate SDK-info or logger requalification is required.

## Reviewed identities

| File, relative to `validation/examples/` | SHA-256 |
| --- | --- |
| `preparation-identities.json` | `4fb843caa8e7faf5852382d664d2418e05bd1816753f544fbae1f46b9303213c` |
| `output-scope-correction/approved-scope.json` | `ac4f388e6c2ad2036ddb83f66cd9dcb6e50f163ca9f2a37d623c35daf6735c79` |
| `output-scope-correction/handoff.md` | `ebad957ec23b0a92d05baf89eda62af7e288131038fc73e92b26611f02698c27` |
| `output-scope-correction/verification.json` | `5cffa452d6b9197a4f5d5930d592cd220a4c2f06491dc7ddf4e38d825dc81424` |

Only this review and the separately assigned record-authority review are written by this reviewer.
No product edits, formatter, Git mutation or subagent ran. Prior reviews and historical proofs remain
intact; final Pass-1 coverage, platform lanes, Windows handoff and PIV physical deferral are unchanged.
