# YubiOTP hardware authorization and baseline

User explicitly authorized any integration and one-off tests against the connected YubiKey, then
approved execution of the plan. Mutations are within scope; human touch/power-cycle timing will be
coordinated if needed. Record device identity/firmware/actual transport and readable pre/post state.
Secrets cannot generally be read back for restoration. No other applet mutation is planned.

Read root testing guidance and docs/TESTING.md hardware authorization/multi-transport semantics.
Existing gitignored Tests.Shared allow list includes serial125 and9681620; no configuration edited.
Actual connected identity remains to be established. Source investigation and fixture/transport review
precede integration test execution; initial ykman list/info/otp info commands are read-only.

Context7 `/yubico/yubikey-manager` resolved and queried on2026-11-04 for CLI inventory syntax; original
README.adoc and _autodocs/api-reference/ykman.cli.md document `list --serials`, `--device SERIAL info`,
and `otp info`. Installed CLI version/help are retained alongside actual outputs. Discovery via ykman
is independent evidence; SDK discovery/transport outcomes require their own results.

## Established initial state

Read-only ykman5.9.2 commands completed2026-11-04: one attached serial**103**, YubiKey5NFC Enhanced PIN,
firmware**5.8.0.beta.0**, USB-A; OTP/FIDO/CCID enabled, NFC enabled. `otp info` reports both slots empty.
Serial103 is already on the existing allow list. Logs: initial-list.log, initial-info.log,
initial-slots.log, ykman-version.log. No device mutation has occurred at this checkpoint.

Coordinator read shared test-infrastructure instructions and allow-list configuration narrowly for
test execution, not a cross-module documentation audit. Readable empty slots establish no retained slot
credential to restore; final slot/connection state still requires explicit verification.
