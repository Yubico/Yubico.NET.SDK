---
uid: Fido2Reset
---

<!-- Copyright 2022 Yubico AB

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License. -->

# FIDO2 Reset

The FIDO2 application can be reset on all YubiKeys that support FIDO2. A reset will
remove any credentials present and set the application to the "no PIN" state.

However, there are some caveats:

* The YubiKey will no longer be able to perform authentication with credentials that were removed from the FIDO2 application during the reset.
* The process of resetting is a bit complicated; the .NET YubiKey API does not include a
  higher-level `Fido2Session` method for performing the entire operation. Instead, you must
  use a lower-level command class as described below.

> [!NOTE]
> The individual FIDO reset can be used with YubiKey Bio Multi-protocol Edition keys *only if* the FIDO application is not "blocked" (check the key's [ResetBlocked](xref:Yubico.YubiKey.YubiKeyDevice.ResetBlocked) property to confirm). Otherwise, the [device-wide reset](xref:UsersManualBioMpe#resetting-a-yubikey-bio-mpe) must be used instead.

## Steps

To perform a FIDO2 reset, complete the following:

1. "Reboot" the YubiKey by removing it from and reinserting it into the host device. Connect to the YubiKey and its FIDO2 application once it has been reinserted. This will likely be done using a listener class (see [YubiKeyDeviceListener](xref:Yubico.YubiKey.YubiKeyDeviceListener)).

1. Within a time limit from the reboot (10 seconds for YubiKeys with firmware version 5.5.4 and later or 5 seconds for firmware versions prior to 5.5.2), send the [ResetCommand](xref:Yubico.YubiKey.Fido2.Commands.ResetCommand).

   > [!NOTE]
   > The reboot requirement and 10-second timeout are mandated by the [CTAP 2.1 standard](https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-errata-20220621.html#authenticatorReset).

1. The YubiKey will not respond with the [ResetResponse](xref:Yubico.YubiKey.Fido2.Commands.ResetResponse) immediately. Within 30 seconds, a user must touch the contact of the YubiKey. If the touch does not occur in time, the YubiKey will return the `ResetResponse` with a `StatusWord` of `0x6F3A`, the CTAP error of timeout (the `Status` property will be `Failed`). If the user touches the contact within the time limit, then the FIDO2 application will be reset (the `StatusWord` property will be `0x9000`, and the `Status` property will be `Success`).

   See the [FIDO2 reset APDU documentation](xref:Fido2ResetApdu) for information on other possible `StatusWord` responses.

## Sample code

The [FIDO2 sample program](https://github.com/Yubico/Yubico.NET.SDK/tree/HEAD/Yubico.YubiKey/examples/Fido2SampleCode) (located under Yubico.YubiKey/examples/Fido2SampleCode/) contains a [class](https://github.com/Yubico/Yubico.NET.SDK/blob/HEAD/Yubico.YubiKey/examples/Fido2SampleCode/YubiKeyOperations/Fido2Reset.cs) (/Fido2SampleCode/YubiKeyOperations/Fido2Reset.cs) that demonstrates how to execute the FIDO2 reset steps. This includes code for:

- creating a listener to determine when the YubiKey is removed and reinserted
- notifying the user to remove, reinsert, and touch the YubiKey
- calling the lower-level SDK command API to perform the reset once the key has been rebooted