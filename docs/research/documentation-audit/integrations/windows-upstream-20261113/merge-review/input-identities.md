# Reconciliation input identities

Preparation capture on 2026-11-13. All Git operations were read-only object/status/diff/grep operations. The existing preview object was consumed; merge-tree was not rerun and no index/worktree merge was made.

| Input | Identity |
|---|---|
| Audited parent / observed HEAD | `d5d01ef99f1478e4d49911194c17667b2450d2fd` |
| Incoming parent | `e322712d609aaf03b9bd1cf2964a99326728866a` |
| Natural merge base | `5746e72ed9a848d08b5782ddde025aa4a4b522ef` |
| Existing merge-tree preview tree | `0d9a3ad551951d4e91e6797ce36709e71f3c6fdd` |
| `../intake/summary.json` SHA-256 | `31c0bf6b31910216ad141aab83991f86d2c7f1c641255decd804adafbc5c0aaf` |
| Frozen audit `modules/core/pass-1/core-001/editing/integration/current-products.json` SHA-256 | `2626e78f146a0dc1d28f8604632e33a58891a98a13a5c1cb2b7d9ed90225b14b` |

The audit map contains 363 product records; intake contains 126 incoming change records and 19 intersections. The map is frozen evidence, not a file to regenerate for this integration. The incoming parent and audited parent identify the complete source inputs; `git show <identity>:<path>` reconstructs every reviewed blob without editing the index. Renamed units use their predecessor path on base/ours and their new path on incoming/preview.

## Conflicted-unit blob identities

Columns are Git blob IDs, not SHA-256 content hashes. `absent` is intentional incoming deletion. Both deleted-file preview blobs equal ours exactly, so no second copy needs interpretation.

| Path (new path for renames) | Base | Ours | Incoming | Preview |
|---|---|---|---|---|
| `CLAUDE.md` | `a9d7bd1053048602ffb06d22d908344e85a8cacc` | `369d98b4fe9abd49765fac5d235b29ffe07b3dd0` | `18a580a572fc639397399acebd09048d2ac30cd6` | `57dd8e3d65ad2350e43b7ecfaae95d7283b39fe6` |
| `docs/architecture/raw-access-tiers.md` | `972bfd17096aefba247cbddf2705a17f1cf545c5` | `35468dce9d0b65f7c40d3b1b9f1dc179e1ff7118` | `4a15c3d85a93e7fc892bbfa67ff20bf0af4b0ae9` | `709358a1582ee620d4203dc72143ac5bbd3130a3` |
| `src/Core/src/Devices/HidConnectionSlot.cs` | `febe52df52754dd3576854ec6cdcddeabc6556c6` | `2e644093aadc15d90e78df027f70bba2426bfd4e` | `6df52812583a2d076926f53c2b4fae27786a9032` | `e232a62220aaaaad35d2e015511a46980b6a371c` |
| `src/Core/src/Native/Windows/HidD/IWindowsHidReportAccess.cs` | `aaf49db2df96961f748e71fb4d8cbe756748f0a9` | `0e8e825bf76fb396b62d4ccc1f58be912e477151` | absent | `0e8e825bf76fb396b62d4ccc1f58be912e477151` |
| `src/Core/src/Native/Windows/HidD/WindowsHidReportAccess.cs` | `2f5dd9c7e975f9799c09dcf5c3b8d5d46bbbe0ab` | `c450d5165950533e4dc96c48866e256e8e5ac277` | absent | `c450d5165950533e4dc96c48866e256e8e5ac277` |
| `src/Core/src/Transports/Hid/Linux/LinuxUdevHidEventSource.cs` | `fcbc3dd25765e4a8c37d2ed8e7be55c7eba778a8` | `d405ced896fde002891ba80b2c674b1854012a6e` | `28191deef42401e7e28cc01de70da17ad7d9b8cc` | `9641c77d84c9c5cc0834f749128df79453a770ff` |
| `src/Core/src/Transports/Hid/MacOS/IMacOSHidReportApi.cs` | `88e37ab5dc73abf859b12162def0a2c0302491b3` | `d11583111463ddd52ab555ec5f62c72548be1b8f` | `29606027e5a44ce8b4073e1a9ab8ab0ff9ec4458` | `9b3432a92e3f0888941f481964739f7e3e2fc840` |
| `src/Core/src/Transports/SmartCard/PcscNativeResources.cs` | `e4773c7a9bd4707af1f4197e2998b4f86c0bb906` | `d5aa411656e2a7b2f5003f7ec5617444a3e34207` | `b5c1b6843a8ebbd2b59ff127cbaafcba1ea6c140` | `061f5d1788306edafa18f77a7bab55f08a2d1d34` |
| `src/Piv/README.md` | `60e93ef3611b7de74f5c626c3588917fc83b9ebf` | `675973f668b5870eec48218ae25ca976ff6b354a` | `96f6bd9b7497242540c522143346af15a029381c` | `abbf78b787fecbdb172ff356ba24cd2374b5549d` |

Predecessors are `src/Core/src/Transports/Hid/Linux/LinuxHidEventSource.cs` and `src/Core/src/Transports/Hid/MacOS/IIOKitDeviceLifetime.cs`. The full units and their base/ours/incoming/preview differences were obtained with Git show, alongside both sides' diffs and moved/extracted recipients; no conflicted worktree files were edited.

## Frozen preservation boundaries

Tracked subtree IDs are identical between audited parent and preview:

| Protected root | Git tree ID |
|---|---|
| `docs/research/documentation-audit/modules/core/pass-1` | `0147dce151bfc6acda20c80613ddee78ac712dd0` |
| `docs/research/documentation-audit/integrations/core-upstream-20261111` | `09100d96942972eb173a3f6980508a61d09436a2` |
| `docs/research/documentation-audit/integrations/piv-pr745-20261105` | `3868a53872165c4c5593e4f6b52e660a3092fab0` |

On-disk fingerprints additionally cover untracked/ignored files inside those roots. For each regular file, in sorted path order, hash the UTF-8 repository-relative POSIX path, NUL, lowercase SHA-256 of its bytes, then newline into one SHA-256 accumulator. Count and size are diagnostic; the aggregate covers paths and bytes.

| Protected root | Files | Bytes | Aggregate SHA-256 |
|---|---:|---:|---|
| Whole Core Pass 1 | 5226 | 453652968 | `6939f308bca6f97573f8a9e79145c2d27b464c62ef2924c161832b94f629ece0` |
| Frozen Core upstream integration | 8326 | 233391605 | `5afe057c26475840f0c2c9775ab4c8dfd0e02f92f1d517533c06b896b236d0fb` |
| Frozen PIV PR745 integration | 114 | 2256182 | `3afc466bbd3a937107a27af1ff7fa3d0baf866450f3a47d509cb9d860071cbe7` |

The original user files and primary worktree listed in intake remain outside this owner's write scope. Preparation output is limited to new `RUN/merge-review/*`. These fingerprints establish a comparison baseline, not a claim that later actors have already preserved it. Recheck before eventual acceptance.

A second read-only fingerprint pass after authoring the preparation files reproduced all three aggregates, file counts and byte totals exactly. At that pass, tracked worktree diff, cached/index diff and `git ls-files -u` output were each zero bytes. JSON parsing confirmed the decision map's 19 overlap path lists exactly match intake's 19 intersections and mark nine conflicts. These are preparation consistency checks, not product tests or a merged-source acceptance result.

## Evidence boundary

No PR/evidence forensics were duplicated. Incoming Windows/macOS historical run claims and the exact human PIV pool-seam rollback instruction are received by the sibling evidence owner/parent intake. The current source's identity is not interchangeable with any historical producer, runtime, firmware or OS. No Windows/macOS native run or PIV physical-release claim is made by this preparation.
