## Footprint: what you ship

**Managed assemblies** (Release, `net10.0`) — install only what you use:

| Assembly | KiB | | Assembly | KiB |
|---|--:|---|---|--:|
| Core | 577 | | WebAuthn | 130 |
| Fido2 | 197 | | OpenPgp | 110 |
| Piv | 165 | | Oath | 62 |
| SecurityDomain | 56 | | YubiOtp | 56 |
| YubiHsm | 51 | | Management | 37 |
(REVIEW: Would be nice to have size comparisons for the v1 .NET SDK (home/dyallo/Code/y/Yubico.NET.SDK, the v1 SDK is a monolithic assembly, this is a modularized one. Consider adding a table comparing the two approaches)

**All ten: 1,441 KiB = 1.41 MiB.** A PIV-only app pays Core + Piv = **742 KiB**.

*These are assembly sizes from `bin/Release/net10.0`, not `.nupkg` sizes.*

**Native AOT**, verification host linking all ten libraries:

| | Native AOT | Framework-dependent |
|---|--:|--:|
| Executable | **3.11 MiB** | — |
| NativeShims sidecar | 3.71 MiB | 3.71 MiB |
| **Peak RSS** | **11.7 MiB** | 51.5 MiB → **4.4× less** |
| **CPU (user + sys)** | **~20 ms** | ~170 ms, **8.5x less** (REVIEW: added 8.5x less for clarity) |
| Wall, median of 10 | 528 ms | 628 ms |
---

## Footprint: how these were collected

**Machine.** Apple M1, 8 cores, 16 GB, macOS 15.7.7, .NET SDK 10.0.100, RID `osx-arm64`.
Repo @ `d04d59aa`. **6 YubiKeys physically attached.**

| Number | Provenance |
|---|---|
| Assembly sizes | **Measured** — `stat` on `bin/Release/net10.0/*.dll` |
| AOT exe, RSS, CPU, wall | **Measured** — `dotnet publish -r osx-arm64 -p:PublishAot=true` of `verification/NativeAotVerification`; `/usr/bin/time -l`; wall = median of 10 after 3 warm-ups; RSS and CPU from a single representative run |
| Framework-dependent baseline | **Measured** — same project, `-p:PublishAot=false --self-contained false` |
| AOT link coverage, support contract | **Quoted** — PR #578, `docs/NATIVE-AOT.md` |
| Recurring AOT CI | **CI** — `native-aot.yml` run `34121554932` on `yubikit`, macOS arm64, **hardware-free** (asserts `Found 0 YubiKey(s)`) |

⚠️ **Two caveats.** Wall time is dominated by I/O enumerating six attached keys, not by
startup: subtracting CPU leaves **508 ms** (AOT) and **458 ms** (framework-dependent) of
non-CPU time — same order, ~11 % apart, and both far larger than either CPU figure.
Second, `/usr/bin/time -l` quantises CPU to 10 ms, so "20 ms vs 170 ms" is two ticks
against seventeen: **treat the CPU ratio as "roughly an order of magnitude", not 8.5×.**

BenchmarkDotNet throughput and allocation were **not collected** — the project does not
compile at this SHA (`DEFERRED.md` #1). Single machine, single run set: **ballpark**.
