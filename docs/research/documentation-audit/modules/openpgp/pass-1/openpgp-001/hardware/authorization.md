# Connected-key authorization and baseline

User explicitly grants full use of the connected dedicated YubiKey for integration tests, configurations,
ad hoc scripts and ykman. PIN/KDF/reset/key changes are within that scope. Coordinator establishes actual
device/app state and test effects, serializes commands, coordinates physical touch/insert-remove timing
and verifies cleanup. Factory-attestation private-key replacement is not added merely for coverage;
consequential irreversible effects receive explicit scope/recovery documentation.

Read root/OpenPGP/test instructions and docs/TESTING.md. Context7 `/yubico/yubikey-manager` was consulted
for serial selection, OpenPGP inventory and reset APIs. Original vendor behavior and exact installed
client/version remain evidence dependencies, especially reset scope and attestation exceptions.
Commands/logs retain metadata and statuses rather than PINs, private keys or derived secrets.

Initial read-only CLI identity: serial 103, YubiKey 5 NFC Enhanced PIN USB-A, metadata 5.8.0.beta.0,
OTP/FIDO/CCID and all applets enabled; OpenPGP card 3.4, app version 5.8.0. PIN/reset/admin retries 3/0/3,
signature policy Once, KDF disabled. ykman version 5.9.2. Further vendor-client baseline will record
actual key/certificate/UIF state, especially attestation identity, before any reset.

Coordinator inspected the full shared session helper and KDF/slow-test file. Each selected integration
method requests reset-before and resource disposal after; helper performs no final reset. The KDF test
changes card configuration and both PIN paths before its verification. It will run as a separate case
after focused investigation rather than use a smoke label to imply read-only behavior.
