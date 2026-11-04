# YubiHSM Auth hardware scope

User approved this cycle with YubiKey-only verification, carrying forward permission for integration
and focused tests on the connected dedicated test key. Physical touch and insert/remove timing require
human coordination. Initial identity/app state are being established; earlier serial 103 evidence is
historical until freshly confirmed. HSM/connector interoperability remains a separate unverified dimension.

Coordinator read root/module/test instructions and docs/TESTING.md. Context7
`/yubico/yubikey-manager` documents `--device SERIAL`, `info`, `hsmauth info` and `hsmauth credentials list`
as inventory operations. Installed CLI help/version and actual outputs will accompany the results.
Test effects and final applet cleanup are reviewed before mutation; owned inputs and outputs are cleared
in focused probe code, and logs retain operation metadata rather than secret values.

## Initial readback, 2026-11-04

ykman 5.9.2 found one connected device: serial **103**, YubiKey 5 NFC Enhanced PIN USB-A,
metadata firmware **5.8.0.beta.0**, OTP/FIDO/CCID enabled. YubiHSM Auth reports applet version **5.8.0**
and management retries **8/8**. LIST reports one asymmetric credential, `test-asymmetric`, touch off,
eight retries. This is the actual starting state; the earlier OTP empty-slot result described another
applet. Existing test reset setup will erase this HsmAuth credential under the approved test-key scope.
Its private key is unreadable, so final cleanup will establish a documented empty/default applet state.

Serial 103 is already in the unchanged Tests.Shared allow list. Coordinator read its instructions,
README and configuration narrowly for execution; shared implementation and Core remain outside this
module investigation. Logs retain exact command, UTC and exit status. No mutation at this checkpoint.
