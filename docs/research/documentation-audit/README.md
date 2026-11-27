# Pass 2 review packet

## Current status

All nine applet PRs are merged into `yubikit-pass2-base` at [`4d217833`](https://github.com/Yubico/Yubico.NET.SDK/commit/4d217833964f35120ed54bb47e2d60fcf5328673), with 13 successful GitHub checks on that commit. [PR #788](https://github.com/Yubico/Yubico.NET.SDK/pull/788) remains open as a draft on hold for Core Pass 2. See [current applet state and deliberate differences](pass-2-applet-state-and-divergences.md) for what landed and why.

The 179 ledger rows comprise 157 dispositions covered by merged changes or retained policy, two explicit deferrals, one S1 drop, one fold, and 18 qualification-only rows. Nineteen qualification items remain open or partial, including Q10, which is not a separate ledger row.

Use the [final ledger](pass-2-review-ledger.md) and [decision log](pass-2-review-decisions.md) for card decisions, deferrals and PR attribution. The [master](pass-2-applet-master.md) and its review and correction records retain their original code pins and conclusions; they do not describe the current upstream tree.

## Progress checkpoint — 2026-11-21

**Historical checkpoint, before #798 merged.** The status and resume instructions below describe that checkpoint, not the current state or next steps.

Eight of nine applet PRs (#789–796) are merged into `yubikit-pass2-base` at `1126fda22`. The
[baseline umbrella #788](https://github.com/Yubico/Yubico.NET.SDK/pull/788) remains open pending Dennis's
final delivery decision. `yubikit-pass2-base` supersedes `yubikit-docs-audit` as the delivery base;
references to the old base are historical.

| Module | Delivery | Software status |
| --- | --- | --- |
| OATH | [#789](https://github.com/Yubico/Yubico.NET.SDK/pull/789) | Merged |
| CTAP2 | [#790](https://github.com/Yubico/Yubico.NET.SDK/pull/790) | Merged |
| Management | [#791](https://github.com/Yubico/Yubico.NET.SDK/pull/791) | Merged |
| Security Domain | [#792](https://github.com/Yubico/Yubico.NET.SDK/pull/792) | Merged |
| YubiOTP | [#793](https://github.com/Yubico/Yubico.NET.SDK/pull/793) | Merged |
| YubiHSM Auth | [#794](https://github.com/Yubico/Yubico.NET.SDK/pull/794) | Merged |
| PIV | [#795](https://github.com/Yubico/Yubico.NET.SDK/pull/795) | Merged |
| OpenPGP | [#796](https://github.com/Yubico/Yubico.NET.SDK/pull/796) | Merged |
| WebAuthn | [#798](https://github.com/Yubico/Yubico.NET.SDK/pull/798) | Software complete; documentation and policy follow-through recorded; merge pending Dennis's approval |

The [ledger](pass-2-review-ledger.md) reconciles the 178 original rows plus one fold, with no orphans:
135 covered by merged software, tests, docs, or retained policy; 22 open with WebAuthn; two explicit
deferrals (`CTAP-IMP-030`, `PGP-IMP-009`); one S1 drop (`HSM-IMP-002`); one fold (`CORE-HSM-X005`);
and 18 qualification-only rows.

**Validation.** [Copilot Round 3](https://github.com/Yubico/Yubico.NET.SDK/pull/798#pullrequestreview-6005965165)
reported no new software findings on runtime head `d6318ff55171ff01d51a2f17f419e2db9fd51b09`.
That Release build had zero warnings. Unit tests executed: 502 WebAuthn, 701 CTAP2, 2,253 Core,
159 CLI, and 25 PublicApi, with zero failures and seven skips. Integration tests were compiled only.
Docs QA passed; the migration YAML parsed with 48 mappings and seven manual-review entries. All 11
GitHub checks passed on the pre-checkpoint head `1c3fb6ce0`; CI for this checkpoint must be checked
separately on the latest PR head.

**Hardware.** Nineteen qualification items remain open or partial: 17 T1 items, `CORE-SD-X007`, and
Q10 (one item with three compile-only cases, not an additional ledger row). No device job is in flight;
the hardware has been released. These bounded Linux witnesses on serial 103 do not close whole items:

- The [Management metadata read](https://github.com/Yubico/Yubico.NET.SDK/pull/791#issuecomment-7064068544)
  and [.NET/Python GetInfo comparison](https://github.com/Yubico/Yubico.NET.SDK/pull/798#issuecomment-7064157982)
  used FIDO HID. Selected GetInfo fields agreed; Python showed absent `preferredPlatformUvAttempts` as
  `0`. No UInt64 vendor command ID was exercised, and built-in UV was absent.
- [Selection](https://github.com/Yubico/Yubico.NET.SDK/pull/798#issuecomment-7066838223) went from
  `DeviceWaiting` to `Completed` in 3.87 s with `IsAuthenticated` false; disposal released the
  reservation. This is not cryptographic or tokenless-UV qualification.
- The [no-touch HMAC vector](https://github.com/Yubico/Yubico.NET.SDK/pull/793#issuecomment-7064320527)
  passed over OTP HID. Previously empty slot 2 was programmed, deleted, and verified empty; slot 1
  was unchanged.

One [PIV signing witness](https://github.com/Yubico/Yubico.NET.SDK/pull/795#issuecomment-7066954262) on
serial 103 (Linux USB CCID, base `1126fda22`) used AES-192 management authentication and a test-owned
P-256 key in previously empty Retired 20 (`PinNever`, `TouchAlways`). One signing touch (12.9 s)
produced a DER signature that passed independent ECDSA verification. The test key was deleted, the slot
was confirmed empty, and the connection was released. No PIN, PUK, certificate, F9, reset, or other-key
operations were performed. Q10, hotplug, and cancellation qualification remain pending.

**Resume.** Verify CI on the latest #798 head, then ask Dennis for explicit merge approval. If approved,
preserve the commit history and verify the resulting dependency chain. Keep #788 open until the final
delivery decision. Resume only bounded qualification and concrete format or functionality checks.
[AG-33](pass-2-review-decisions.md#ag-33-review-adjudication-and-compatibility) sets the sibling SDK floor
and permits explicit human adjudication for legacy or experimental findings, not a blanket waiver or
an automatic fourth Copilot round. Preserving existing V1 provisioning is an upgrade goal, not a claim
of global V1 validation. Keep explicit deferrals deferred.

The [frozen audit archive](https://github.com/Yubico/Yubico.NET.SDK/tree/db72706b9ebda1f9ff95fede125e270c755966d0/docs/research/documentation-audit) contains the supporting evidence and the [approved writing style](https://github.com/Yubico/Yubico.NET.SDK/blob/db72706b9ebda1f9ff95fede125e270c755966d0/docs/research/documentation-audit/records/style.md).
