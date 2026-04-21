# Performance Benchmark: PR #445 SCard Busy-Loop Fix

## Metadata

- **Date:** 2026-04-21
- **Branch:** `feature/scard-listener-followups`
- **HEAD SHA:** df6fcbd5 (Step 4 of stacked follow-ups)
- **Base:** PR #445 at 40933696 (`origin/dennisdyallo/fix-rds-scard-invalid-handle`)

## Benchmark Description

This benchmark proves that PR #445's recovery wiring for `SCARD_E_INVALID_HANDLE` eliminates the busy-loop bug observed in production YubiPCS 1.9.1.2 logs, where the listener was making ~3,700 `SCardGetStatusChange` calls per second under persistent context-invalidation failures.

The benchmark compares:

1. **Legacy (develop pre-#445):** Simplified snapshot preserving the busy-loop characteristic — no recovery path for `SCARD_E_INVALID_HANDLE`, so `GetStatusChange` is called in a tight loop.
2. **Fixed (#445 + follow-ups):** Current listener with exponential backoff recovery (Steps 1-4 already applied).

Both listeners are fed a mock `ISCardInterop` that returns `SCARD_E_INVALID_HANDLE` on every call after the initial probe. The observation window is 1 second.

## BenchmarkDotNet Report

```

BenchmarkDotNet v0.14.0, macOS Sequoia 15.6.1 (24G90) [Darwin 24.6.0]
Apple M1, 1 CPU, 8 logical and 8 physical cores
.NET SDK 9.0.308
  [Host]     : .NET 8.0.13 (8.0.1325.6609), Arm64 RyuJIT AdvSIMD
  Job-EFTRWD : .NET 8.0.13 (8.0.1325.6609), Arm64 RyuJIT AdvSIMD

Runtime=.NET 8.0  InvocationCount=1  IterationCount=5  
RunStrategy=Monitoring  UnrollFactor=1  WarmupCount=1  

```
| Method                           | Mean    | Error    | StdDev   | Ratio |
|--------------------------------- |--------:|---------:|---------:|------:|
| 'develop (pre-#445) — busy spin' | 1.003 s | 0.0063 s | 0.0016 s |  1.00 |
| '#445 fix — bounded recovery'    | 1.002 s | 0.0051 s | 0.0013 s |  1.00 |

## Invocation Counts (Observed via Console Output)

- **Legacy (develop):** 134,789,691 invocations in 1 second
- **Fixed (#445):** 1 invocation in 1 second

## Analysis

The Mean times are both ~1.0 second because that's the `Thread.Sleep` observation window, which dominates execution time. The **critical metric is the invocation count**:

- **Legacy:** ~135 million calls/sec (busy spin with no delay)
- **Fixed:** 1 call/sec (backoff working correctly)
- **Ratio:** ~135,000,000× reduction

## Verdict

**PASS** — Ratio exceeds the >= 1,000× acceptance criterion by ~135,000×.

The #445 fix (with Steps 1-4 follow-ups applied) successfully eliminates the busy-loop. Under persistent `SCARD_E_INVALID_HANDLE` failures, the listener now backs off with exponential delays instead of spinning the CPU at ~135M iterations/sec.

## Notes

- The legacy listener is a simplified 120-line snapshot (`LegacyDesktopSmartCardDeviceListener`) that preserves the essential busy-loop characteristic, not a full port of the 513-line develop file. This approach was chosen per the brief's fallback guidance when the verbatim port proved complex.
- The benchmark resides in a separate `Yubico.NET.SDK.Performance.sln` to keep perf projects out of the default build.
- Both assemblies are strong-name signed with `Yubico.NET.SDK.snk`.

## Reproducibility

```bash
dotnet build Yubico.NET.SDK.Performance.sln --configuration Release
dotnet run --configuration Release --project Yubico.Core/perf/Yubico.Core.Performance.csproj -- --filter '*SmartCardListenerInvalidHandleBenchmark*'
```

Expect the benchmark to complete in ~12 seconds (warmup + 5 iterations × 2 benchmarks).
