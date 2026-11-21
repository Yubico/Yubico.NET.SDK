# Pass 2 review packet

Use the [final ledger](pass-2-review-ledger.md) and [decision log](pass-2-review-decisions.md) for card decisions, deferrals and PR attribution. The [master](pass-2-applet-master.md) and its review and correction records retain their original code pins and conclusions; they do not describe the current upstream tree.

The [baseline umbrella #788](https://github.com/Yubico/Yubico.NET.SDK/pull/788) stays open until final delivery. `yubikit-pass2-base` supersedes `yubikit-docs-audit` as the delivery base; references to the old base are historical.

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
| WebAuthn | [#798](https://github.com/Yubico/Yubico.NET.SDK/pull/798) | Final candidate `d6318ff55`; Copilot round 3 and CI pending |

Hardware qualification is separate from software delivery. The 17 T1 items and `CORE-SD-X007` remain open or partial. Q10 was compile-only; its hardware qualification is still open. One [Management metadata read on Linux FIDO HID](https://github.com/Yubico/Yubico.NET.SDK/pull/791#issuecomment-7064068544) is a bounded result, not module qualification. The CTAP GetInfo comparison is incomplete after a temporary formatter failure; field parity is not established. See the ledger decisions and delivery PR notes for scope. Explicit deferrals remain deferred, not jobs to run.

The [frozen audit archive](https://github.com/Yubico/Yubico.NET.SDK/tree/db72706b9ebda1f9ff95fede125e270c755966d0/docs/research/documentation-audit) contains the supporting evidence and the [approved writing style](https://github.com/Yubico/Yubico.NET.SDK/blob/db72706b9ebda1f9ff95fede125e270c755966d0/docs/research/documentation-audit/records/style.md).
