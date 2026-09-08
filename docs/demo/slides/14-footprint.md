## Footprint: monolith vs modular

**v1** ships two assemblies. You take all of it, always.

| v1 assembly | KiB |
|---|--:|
| `Yubico.YubiKey` — every applet | 676 |
| `Yubico.Core` — transports, protocols | 215 |
| **total, unavoidable** | **890** |

**v2** ships ten. You reference what you use.

| Package | KiB | | Package | KiB |
|---|--:|---|---|--:|
| **Core** | **577** | | WebAuthn | 130 |
| Fido2 | 197 | | OpenPgp | 110 |
| Piv | 165 | | Oath | 62 |
| SecurityDomain | 56 | | YubiOtp | 56 |
| YubiHsm | 51 | | Management | 37 |

| Scenario | KiB | vs v1 |
|---|--:|---|
| **v2, PIV app** (Core + Piv) | **742** | −148, **17 % smaller** |
| v2, all ten | 1,441 | +550, 62 % larger |

---

## Why is the saving only 17 %?

Fair question. Modularity should win bigger. Two things cap it.

**1. `Core` is the floor, and it grew 2.7×.** v1's shared layer was 215 KiB; v2's is
577 KiB. A PIV app pays that before it pays for PIV. The nine applets are cheap
(37–197 KiB each) — skipping eight of them is where the 148 KiB actually comes from.

**2. v2 emits far more IL per line of source.**

| | v1 | v2 |
|---|--:|--:|
| Source | 113,439 LOC | 77,512 LOC |
| Assemblies | 890 KiB | 1,441 KiB |
| **Bytes per line** | **8.0** | **19.0** — 2.4× |
| `async` methods | **1** | **488** |
| `record` types | — | 94 |

v2 has **35 % less source** and **62 % more binary**. Every `async` method compiles to
a generated state-machine type — fields for each local that survives an `await`, a
`MoveNext` switch, builder plumbing. Every `record` generates `Equals`, `GetHashCode`,
`ToString`, `PrintMembers`, `Clone`, `Deconstruct` and two operators.

**So the size story is a trade, not a win.** async-everywhere and value-semantics cost
roughly 2.4× the IL per line. Modularity buys most of that back — but only for
consumers who skip applets. Take all ten and you pay for the trade in full.

*Not apples-to-apples: v1 targets `netstandard2.1`, v2 targets `net10.0`. v1 has no
WebAuthn client layer at all.*

<!-- Anchors: v1 sizes measured, repo @fd16960a, dotnet build -c Release -f netstandard2.1;
     v2 sizes stat on bin/Release/net10.0;
     LOC via find+wc over src, excluding obj/bin;
     async counts via rg 'async (Task|ValueTask|IAsyncEnumerable)';
     v1 WebAuthn absence: no WebAuthn path under Yubico.YubiKey/src -->

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

Note this cuts against the IL-size trade above: the IL grows, but AOT never ships it.

---

## Footprint: how these were collected

**Machine.** Apple M1, 8 cores, 16 GB, macOS 15.7.7, .NET SDK 10.0.100, RID `osx-arm64`.
v2 @ `d04d59aa`. v1 @ `fd16960a`, `netstandard2.1`. **6 YubiKeys physically attached.**

| Number | Provenance |
|---|---|
| v2 assembly sizes | **Measured** — `stat` on `bin/Release/net10.0/*.dll` |
| v1 assembly sizes | **Measured** — `dotnet build -c Release -f netstandard2.1` at `fd16960a` |
| LOC, async and record counts | **Measured** — `find`+`wc` and `rg` over `src`, excluding `obj/` and `bin/` |
| AOT exe, RSS, CPU, wall | **Measured** — `dotnet publish -r osx-arm64 -p:PublishAot=true` of `verification/NativeAotVerification`; `/usr/bin/time -l`; wall = median of 10 after 3 warm-ups |
| Framework-dependent baseline | **Measured** — same project, `-p:PublishAot=false --self-contained false` |
| AOT link coverage, support contract | **Quoted** — PR #578, `docs/NATIVE-AOT.md` |
| Recurring AOT CI | **CI** — `native-aot.yml` run `34121554932` on `yubikit`, macOS arm64, **hardware-free** |

⚠️ **Caveats.** Wall time is dominated by I/O enumerating six attached keys, not
startup: subtracting CPU leaves 508 ms (AOT) and 458 ms (framework-dependent).
`/usr/bin/time -l` quantises CPU to 10 ms, so 20 vs 170 ms is two ticks against
seventeen — **order-of-magnitude, not 8.5× to two significant figures.** Bytes-per-line
is a proxy, not a causal measurement: it does not isolate async from records, nullable
metadata or generics. BenchmarkDotNet numbers were **not** collected (`DEFERRED.md` #1).
Single machine, single run set: **ballpark**.
