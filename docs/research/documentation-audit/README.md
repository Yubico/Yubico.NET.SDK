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
| WebAuthn | [#798](https://github.com/Yubico/Yubico.NET.SDK/pull/798) | Final candidate; Copilot round 3 and CI pending |

Hardware qualification is separate from software delivery. All 17 T1 items, `CORE-SD-X007`, and Q10 remain open or partial; Q10 was compile-only. The [Management metadata read](https://github.com/Yubico/Yubico.NET.SDK/pull/791#issuecomment-7064068544) is one Linux FIDO HID result. The completed [.NET/Python GetInfo comparison](https://github.com/Yubico/Yubico.NET.SDK/pull/798#issuecomment-7064157982) on serial 103 over Linux FIDO HID found agreement on selected fields, with absent `preferredPlatformUvAttempts` shown as `0` in Python. No UInt64 vendor command ID was exercised, and built-in UV was absent, so this did not qualify tokenless UV. On the same fixture, one [Selection touch](https://github.com/Yubico/Yubico.NET.SDK/pull/798#issuecomment-7066838223) went from `DeviceWaiting` to `Completed` in 3.87 s with `IsAuthenticated` false; disposal released the connection reservation. This is not cryptographic or UV qualification. One [no-touch HMAC vector](https://github.com/Yubico/Yubico.NET.SDK/pull/793#issuecomment-7064320527) passed on serial 103 over OTP HID: only previously empty slot 2 was programmed, then deleted and verified empty; slot 1 was unchanged. See the ledger and PR notes for scope. Explicit deferrals remain deferred, not jobs to run.

The [frozen audit archive](https://github.com/Yubico/Yubico.NET.SDK/tree/db72706b9ebda1f9ff95fede125e270c755966d0/docs/research/documentation-audit) contains the supporting evidence and the [approved writing style](https://github.com/Yubico/Yubico.NET.SDK/blob/db72706b9ebda1f9ff95fede125e270c755966d0/docs/research/documentation-audit/records/style.md).
