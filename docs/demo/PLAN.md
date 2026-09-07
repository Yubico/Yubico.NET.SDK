# Plan: v2 SDK demo deck

Frozen at the start of execution. The independent reviewer (Phase 7) compares the
delivered output against this document. Deviations are expected to be justified,
not silently absorbed.

## Audience and intent

Teammates who work primarily in `yubikey-manager` (`main` and the rust
experiment), `yubikit-android`, `yubikit-swift`, and `python-fido2`. The deck
gives them a *feel* for the v2 .NET SDK, not a reference manual.

## Canonical source

`/Users/Dennis.Dyall/Code/y/Yubico.YubiKit.NET.SDK`, branch `yubikit`, pinned at
`d04d59aae63981588f6dc047eb0d788b681a8b8d` (PR #644).

Explicitly **not** `/Users/Dennis.Dyall/Code/y/Yubico.NET.SDK`, whose `yubikit`
branch is stale at PR #578.

Peer repositories, pinned:

| Repo | Branch | SHA |
|---|---|---|
| `yubikey-manager` | `main` | `4ca60f706af930459138d8dc0f0f953480e1c7a4` |
| `yubikey-manager` | `experiment/rust` | `90940e9bb734e4daec04bdb17dae155ff3c85c57` |
| `yubikit-android` | `main` | `f46268563437ac52910001222a77741d229b9b99` |
| `yubikit-swift` | **`release/1.4.0`** | `c76ae973d6` *(repinned — see Amendments)* |
| `python-fido2` | `main` | `5bc9d3a1c8c34a3c4ca408366e630b620db47faa` |

## Working location

Worktree `/Users/Dennis.Dyall/Code/y/worktrees/yubikit-demo-deck`, branch
`demo/v2-slide-deck`, based on `d04d59aa`.

## Deliverables

1. `docs/demo/*.md` — Marp deck, one file per topic slide, topic-keyed filenames.
2. `docs/demo/assets/` — diagrams, reusing `docs/architecture/images/` plus one
   or two newly rendered SVGs.
3. `docs/demo/CLAIMS.md` — every factual claim mapped to a source anchor.
4. `docs/demo/DEFERRED.md` — bugs and incorrectness found along the way.
5. `docs/demo/PLAN.md` — this document.
6. Built `deck.html` and `deck.pdf`.
7. An independent reviewer report.

## Standing policy: defer, do not fix

Any bug, incorrect documentation, or inconsistency encountered during this work
is recorded in `DEFERRED.md` with a source anchor and a short description. No
fixes are made. Production source under `src/` is not modified by this workload.

## Phase 0 — Setup

- Fetch the canonical repo; pin the SHA. If `origin/yubikit` has moved past the
  earlier reconnaissance point, re-verify the observability and session-shape
  findings before proceeding.
- Fetch all four peer repos; pin their SHAs.
- Create the worktree.
- Write `PLAN.md` and an empty `DEFERRED.md` before any slide is written, so the
  reviewer compares against a frozen artifact.

## Phase 1 — Grounding discipline

`CLAIMS.md` is a table: claim, slide, source `file:line`, SHA, kind.

- **Code claims** are verified against `src/`, never against documentation
  alone. Precedent: `DeviceChanges` was documented but deleted from source.
- **Doc claims** covering rationale or recorded decisions cite the document and
  are marked `kind=doc`.
- **Peer claims** cite `repo@sha:file:line`.
- **Quoted claims**, such as the PR #586 measurements, are marked `kind=quoted`.
- A claim that cannot be anchored is **cut**, not softened.
- Phase 6 re-greps every anchor to catch drift.

## Phase 2 — Parallel research

### 2a. Peer SDK extraction

Four `explore` agents, one per repository, each returning:

- Per applet, the canonical minimal "hello world", three lines or fewer.
- How the SDK surfaces device attach and detach, or an explicit statement that
  it does not.
- The async and error model in one line.
- An exact `file:line` for every item.

### 2b. Footprint collection

Cheapest sources first.

| Metric | Source | Cost |
|---|---|---|
| Per-package size, ten packages | Local `artifacts/packages/*.nupkg` | free |
| Shared versus static shim, AOT probe binary, package growth | PR #586 body, labelled as quoted | free |
| AOT support contract and evidence boundaries | PR #578, `docs/NATIVE-AOT.md`, `docs/research/native-aot-readiness-data.md` | free |
| Recurring AOT CI evidence | `native-aot.yml` runs via `gh` | cheap |
| AOT binary size, cold start, peak RSS | One `dotnet publish -r osx-arm64 -p:PublishAot=true` of `verification/NativeAotVerification`, then `hyperfine` and `/usr/bin/time -l` | one build |
| Allocation and throughput | `benchmarks/` ShortRun, no-hardware benchmarks only | one run |

## Phase 3 — Architecture and observability slides

| # | Key | Source |
|---|---|---|
| 00 | `intro` | `docs/v2-highlights.md`, `docs/v1-to-v2-comparison.md` |
| 01 | `pipeline` | `L2-layered-stack.svg` |
| 02 | `transports` | `L4-connection.svg`, `L3-apdu-sequence.svg`, `L3b-fido2-ctap-sequence.svg` |
| 03 | `discovery` | `L4-discovery.svg`, `device-discovery-guarantees.md` |
| 04 | `identity-merging` | `CompositeDeviceMerger`, `DiscoveryIdentityReader`, `IDeviceTopologyResolver`; `device-identity.md` decisions D2 serial-only, D3 instance retention, D6 referential equality, D7 single-interface `DeviceId` |
| 05 | `observability-events` | `StartMonitoring()` and `await foreach (var e in YubiKeyManager.WatchAsync())`. That is the entire API |
| 06 | `observability-architecture` | v1 500 ms polling to v2 OS-notification plus coalescing; `DeviceEventHub`. Render the `event-driven-device-discovery.md` before/after as SVG |
| 07 | `observability-contracts` | Subscribe on first enumeration; per-enumeration buffer, overflow faults rather than drops; `StartMonitoring` interval no-op; one stream, no Rx |
| 08 | `observability-logging` | Static `YubiKitLogging`, never inject `ILogger`, sensitive-data rule |

Slide 07 may be merged into 05 and 06 if it proves thin after grounding.

## Phase 4 — Session model and applets

### Slide 09, `session-model`

The **ownership** axis. Two forms:

```csharp
// Convenience — session owns a hidden connection, disposes it with the session
await using var piv = await yubiKey.CreatePivSessionAsync();

// Direct factory — caller owns the connection; caller disposes both.
// One connection admits one live session: dispose N before creating N+1.
await using var conn = await yubiKey.ConnectAsync<ISmartCardConnection>();
await using (var piv  = await PivSession.CreateAsync(conn))  { }
await using (var mgmt = await ManagementSession.CreateAsync(conn)) { }
```

Plus: the uniform `SessionCreationOptions?` and defaulted `CancellationToken`
tail, `await using` always, and the fact that this shape is enforced by
`src/PublicApi/tests/Yubico.YubiKit.PublicApi.UnitTests/AppletSessionShapeTests.cs`.

The sequential-reuse snippet must be verified against a real call site under
`src/Cli.Commands/`, not taken from documentation alone.

### Slide 09b, `raw-access-tiers`

The **tier** axis, distinct from ownership. Tier 0 applet sessions, Tier 1 raw
sessions via `CreateRaw*SessionAsync`, Tier 2 raw connections. One line each on
when to drop down, plus the Tier 2 warning that the caller assumes APDU
framing, chaining, response correlation, CRC validation, keep-alive handling,
and recovery. May be cut if the slide count feels heavy.

### Slides 10 to 18, nine applets

Management, PIV, OATH, OpenPGP, FIDO2, WebAuthn, SecurityDomain, YubiHSM Auth,
YubiOTP. Each slide is strictly:

- One line on what the session does.
- One C# operation in the Tier 0 convenience form, two to three lines.
- At most two peer SDKs, one to three lines each, chosen by audience relevance:
  PIV and OATH to `yubikey-manager` plus `yubikit-android`; FIDO2 and WebAuthn
  to `python-fido2` plus `yubikit-swift`; OpenPGP to `yubikey-manager`;
  YubiHSM Auth and SecurityDomain to `yubikey-manager` only.
- One line naming the single most notable difference.

No exhaustive method tables. The goal is feel, not reference.

## Phase 5 — Footprint and close

**Slide 19, `footprint`.** The numbers, with a provenance footer on the slide
itself: machine, RID, .NET version, repository SHA, and per-number labelling of
CI-sourced versus locally measured versus quoted. Anything not measured on this
run is marked indicative.

**Slide 20, `takeaways`.** Where to look in the repository, what is alpha, what
comes next.

Target is roughly twenty-one slides.

## Phase 6 — Verify and build

1. Re-grep every `CLAIMS.md` anchor against the pinned SHA; fix or cut drift.
2. `npx @marp-team/marp-cli` to produce `deck.html` and `deck.pdf`.
3. Visual pass for slide overflow; trim.

## Phase 7 — Independent review

Dispatch a `reviewer` subagent with no prior context from the authoring session.
It receives this plan, the full deck output, `CLAIMS.md`, the built PDF, and read
access to the pinned repository SHA.

Its charter:

1. Does the output match this plan? Enumerate every deviation, including silent
   omissions.
2. Spot-check at least fifteen `CLAIMS.md` anchors against actual source. Report
   any claim that is wrong, unanchored, or anchored to a document where a code
   anchor was required.
3. Flag any claim appearing on a slide that has no `CLAIMS.md` row.
4. Confirm the one-to-three line budget on applet slides is honoured.
5. Confirm the footprint slide's provenance labelling is honest.
6. Return a verdict of pass, concerns, or fail, with severity-tagged findings.

The reviewer is read-only and fixes nothing. Its report is relayed intact.

## Risks

- **Drift.** `origin/yubikit` moves quickly, roughly sixty-six pull requests in
  three weeks. Mitigated by pinning a SHA and by the Phase 6 re-grep.
- **Peer snippet accuracy.** The four peer codebases are less familiar.
  Mitigated by `file:line` anchoring and reviewer spot-checks.
- **Footprint honesty.** Mixing quoted, CI, and locally measured numbers is the
  easiest place to mislead. Mitigated by per-number provenance labels.
- **Scope creep on applet slides.** The one-to-three line constraint is the most
  likely thing to erode. The reviewer checks it explicitly.

## Abbreviations

SDK, software development kit. SHA, secure hash algorithm, here a Git commit
identifier. AOT, ahead-of-time compilation. RID, runtime identifier. RSS,
resident set size. CI, continuous integration. PR, pull request. APDU,
application protocol data unit. CRC, cyclic redundancy check. Rx, Reactive
Extensions. SVG, scalable vector graphics. PDF, portable document format. PIV,
personal identity verification. OATH, Initiative for Open Authentication. HSM,
hardware security module. OTP, one-time password. OS, operating system. CTAP,
client to authenticator protocol.


---

# Amendments (post-review, approved 2026-09-07)

These changes were approved after the first independent review. They are recorded here
so the second review compares against the amended contract, not the original.

## A1 — Swift repinned from `main` to `release/1.4.0`

**This amendment was originally written with false supporting evidence. Corrected below
after the second review caught it.**

### What was claimed, and why it was wrong

The first version of A1 asserted that `main` was 89 commits behind `release/1.3.0`, that
`release/1.3.0` was not an ancestor of `main`, and that
`YubiKit/YubiKit/FIDO/WebAuthn/Client/Client.swift` did not exist on `main`.

**All three were false.** They were produced by running `git show main:` and
`git rev-list main..` against a **stale local `main` ref** at `4833bb7dc9` (an old 1.2.0
merge) rather than against `origin/main`. The correct facts:

- `origin/main` is `f5a01653ec`, 2026-05-06 — which is the SHA the plan already recorded.
- `git rev-list --count origin/main..origin/release/1.3.0` is **0**. `release/1.3.0` is
  fully merged into `origin/main`; the tip of `origin/main` is literally
  `Merge remote-tracking branch 'origin/release/1.3.0'`.
- **`Client.swift` does exist on `origin/main`.**

### The actual justification for the repin

The repin still stands, on narrower and verifiable grounds:

- `git rev-list --count origin/main..origin/release/1.4.0` is **58**. `release/1.4.0`
  @ `c76ae973`, 2026-09-07, is the development tip.
- It is **contemporaneous with the .NET pin** — both dated 2026-09-07 — whereas
  `origin/main` is four months stale.
- The files the deck cites genuinely differ between the two refs. For example
  `CTAPSession.swift`'s `getInfo` is at `:42` on `origin/main` and `:49` on
  `release/1.4.0`. Anchors are branch-sensitive, so the branch must be stated.

`release/1.4.0` is therefore the honest comparison point for "what the Swift team works
in today". It is **not** true that `main` lacks the WebAuthn client. All Swift anchors
are verified against `release/1.4.0` and marked `swift@1.4.0` in `CLAIMS.md`.

## A2 — Slide count: 38 → 27, target ~21 not reached

The original target was "roughly twenty-one". Delivered is **27**. The gap is structural,
not scope creep:

- Nine applets at one slide each is a hard floor of 9 slides.
- Observability was requested as its own section: 4 slides.
- Footprint provenance honestly requires 2 slides.

Trimmed since review: intro 2→1, pipeline 2→1, transports 3→2, discovery 2→1,
identity 3→2, observability 3+1→2, raw tiers 2→1, session model 3→2 (duplicate removed),
footprint 3→2, takeaways 3→1. Applet slides untouched at 9.

27 is the floor that preserves every explicitly requested topic. Recorded rather than
concealed.

## A3 — Package sizes are assembly sizes

`PLAN.md` Phase 2b specified `artifacts/packages/*.nupkg`. Delivered numbers are
`stat` on `bin/Release/net10.0/*.dll` at the pinned SHA. `.nupkg` artifacts are not
produced at this SHA and would conflate compressed package overhead with code size.
The slide labels the column `KB` per assembly and the provenance table says so.

## A4 — CI provenance row added

Phase 5 required per-number labelling across measured / quoted / **CI**. The first
draft omitted the CI axis. Slide 14 now cites `native-aot.yml` run `34121554932`
on branch `yubikit`, and states it is hardware-free.

## A5 — CPU ratio de-precisioned

`/usr/bin/time -l` quantises CPU to 10 ms. "8.5×" was two significant figures on a
two-tick measurement. The slide now says "roughly an order of magnitude" and states
the quantisation explicitly.

## A6 — Corrections to factual errors found in review

- YubiOTP transport claim was inverted. Order is `[SmartCard, HidOtp]`. A dedicated
  multi-transport slide now covers Management (3 transports), FIDO2, WebAuthn and
  YubiOTP, plus the FW 5.8.0+ USB-CCID path and the SCP-forces-SmartCard rule.
- "Nine applets, enforced by tests" corrected to **eight**. WebAuthn is excluded by
  design and its deviation is now the point of the slide.
- PIV delta corrected: all three SDKs return real certificate types; the .NET
  difference is nullability.
- OATH delta corrected: Android is `Map<Credential, @Nullable Code>`, same semantics.
- Footprint total corrected 1.37 → 1.41 MiB (1,441 KB).
- `CreateWebAuthnClientAsync` snippet corrected to the real signature.
- Duplicate tier slide removed; orphaned `observability-before-after.svg` now used.

## A7 — Delta lines exceed "one line" on five applet slides

The plan says each applet slide carries "One line naming the single most notable
difference". Five slides carry two or three:

| Slide | Differences named |
|---|---|
| Management | device shortcut; three transports; Swift's two overloads |
| FIDO2 | eager `GET_INFO` in python-fido2; dual transport + FW 5.8.0+ |
| WebAuthn | origin + suffix checker required; python-fido2 ships the server half |
| SecurityDomain | SCP as a creation option; SCP forces SmartCard |
| YubiOTP | dual-transport prefers SmartCard; Swift has no session |

This is a real over-run of the stated budget, not a reinterpretation of it. It is
recorded rather than silently kept: the extra lines carry the transport corrections
that the first review required, and cutting them would re-introduce the ambiguity that
made the YubiOTP claim wrong in the first place. The code-block budget is honoured
everywhere.

## A8 — Coverage-matrix slide is an addition, not in the original plan

`slides/13-coverage-matrix.md` was not in the planned inventory. It was added because
the applet slides each show at most two peers, which leaves the reader unable to see
overall coverage. It is one slide, it is not a method table, and every cell is anchored
in `CLAIMS.md` C1–C4 plus the per-applet rows. Recorded as an addition.

## A9 — Unused assets retained

`assets/L0-context.svg` and `assets/L1-assembly-deps.svg` are copied from
`docs/architecture/images/` but appear on no slide. They are retained deliberately as
backup material for questions about system context and assembly dependencies, which are
plausible audience questions the deck does not otherwise answer. They add ~40 KB and no
build cost. `observability-before-after.svg`, previously orphaned, is now used on **deck slide 10**
(`slides/06-observability-architecture.md:3`).


## A10 — Slide numbering in these amendments

A4 and A9 originally used two different numbering schemes (source-file index versus deck
position), and A9's number was simply wrong. All amendments now cite **deck slide
position** and name the source file, e.g. "deck slide 10 (`slides/06-...md`)".

## A11 — "Async all the way down" corrected to a qualified claim

The intro slide asserted "no sync-over-async, no blocking transport calls", echoing
`docs/v2-highlights.md:40-41`. That is false against shipped public API:
`YubiKeyManager.Shutdown()` is a synchronous wrapper and
`ISmartCardConnection.BeginTransaction` is a blocking synchronous method. The slide now
says "async on the golden path" with the exceptions named, matching the wording slide 02
already used. The upstream documentation defect is recorded in `DEFERRED.md` #6 and, per
standing instruction, **not fixed**.

## A12 — Monitoring surface claim narrowed honestly

Slide 10 said "That is all of it" about four monitoring members, anchored to a line range
that excluded `Shutdown`/`ShutdownAsync` — an anchor narrowed such that the
exhaustiveness claim read true against its own citation. The slide now says "the
monitoring **control** surface is five members" and names the teardown pair separately.
Anchor widened to `PublicAPI.Unshipped.txt:986-992`.
