## Footprint: monolith vs modular

**v1** ships two assemblies. You take all of it, always.

| v1 assembly | KiB |
|---|--:|
| `Yubico.YubiKey` | 676 |
| `Yubico.Core` | 215 |
| **total, unavoidable** | **890** |

**v2** ships ten. You reference what you use.

| Package | KiB | | Package | KiB |
|---|--:|---|---|--:|
| Core | 577 | | WebAuthn | 130 |
| Fido2 | 197 | | OpenPgp | 110 |
| Piv | 165 | | Oath | 62 |
| SecurityDomain | 56 | | YubiOtp | 56 |
| YubiHsm | 51 | | Management | 37 |

| Scenario | KiB | vs v1 |
|---|--:|---|
| **v2, PIV app** (Core + Piv) | **742** | **−148, 17 % smaller** |
| v2, all ten | 1,441 | +550, 62 % larger |

A PIV-only app ships **less** than v1 while getting async and AOT. Taking everything
costs more — but v2 also ships a WebAuthn client layer that v1 has no equivalent of.

*Not apples-to-apples: v1 targets `netstandard2.1`, v2 targets `net10.0`.*

---

## Footprint: Native AOT

Verification host linking all ten libraries — a whole-SDK upper bound.

| | Native AOT | Framework-dependent |
|---|--:|--:|
| Executable | **3.11 MiB** | — |
| NativeShims sidecar | 3.71 MiB | 3.71 MiB |
| **Peak RSS** | **11.7 MiB** | 51.5 MiB → **4.4× less** |
| **CPU (user + sys)** | **~20 ms** | ~170 ms → **~an order of magnitude less** |
| Wall, median of 10 | 528 ms | 628 ms |

**What "11.7 MiB resident" means.** Native AOT compiles to a standalone native
executable — no JIT, no runtime install, no warm-up. The process starts already
compiled, so it allocates a fraction of the managed heap a JIT'd process needs. For a
CLI, an installer, a service, or anything short-lived, that is the difference between
feeling instant and feeling like a .NET app.

---

## Footprint: how these were collected

**Machine.** Apple M1, 8 cores, 16 GB, macOS 15.7.7, .NET SDK 10.0.100, RID `osx-arm64`.
v2 @ `d04d59aa`. v1 @ `fd16960a`, `netstandard2.1`. **6 YubiKeys physically attached.**

| Number | Provenance |
|---|---|
| v2 assembly sizes | **Measured** — `stat` on `bin/Release/net10.0/*.dll` |
| v1 assembly sizes | **Measured** — `dotnet build -c Release -f netstandard2.1` |
| AOT exe, RSS, CPU, wall | **Measured** — `dotnet publish -r osx-arm64 -p:PublishAot=true` of `verification/NativeAotVerification`; `/usr/bin/time -l`; wall = median of 10 after 3 warm-ups; RSS and CPU from one representative run |
| Framework-dependent baseline | **Measured** — same project, `-p:PublishAot=false --self-contained false` |
| AOT link coverage, support contract | **Quoted** — PR #578, `docs/NATIVE-AOT.md` |
| Recurring AOT CI | **CI** — `native-aot.yml` run `34121554932` on `yubikit`, macOS arm64, **hardware-free** (asserts `Found 0 YubiKey(s)`) |

⚠️ **Two caveats.** Wall time is dominated by I/O enumerating six attached keys, not by
startup: subtracting CPU leaves **508 ms** (AOT) and **458 ms** (framework-dependent) of
non-CPU time. Second, `/usr/bin/time -l` quantises CPU to 10 ms, so "20 ms vs 170 ms" is
two ticks against seventeen — **the ratio is order-of-magnitude, not 8.5× to two
significant figures.**

BenchmarkDotNet throughput and allocation were **not collected** — the project does not
compile at this SHA (`DEFERRED.md` #1). Single machine, single run set: **ballpark**.
