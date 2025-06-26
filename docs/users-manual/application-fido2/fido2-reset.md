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

It is possible to reset the FIDO2 application on YubiKeys that support FIDO2. A reset will
remove any credentials and set the application to "no PIN".

However, there are some caveats:

* The YubiKey will no longer be able to perform authentication with credentials previously
  created with the FIDO2 application by that YubiKey.
* The process of resetting is a bit complicated; the .NET YubiKey API does not include a
  higer-level `Fido2Session` method for performing the entire operation. Instead, you must
  send a series of lower-level commands described below.

> [!NOTE]
> The individual FIDO reset can be used with YubiKey Bio Multi-protocol Edition keys *only if* the FIDO application is not "blocked" (check the key's [ResetBlocked](xref:Yubico.YubiKey.YubiKeyDevice.ResetBlocked) property to confirm). Otherwise, the [device-wide reset](xref:UsersManualBioMpe#resetting-a-yubikey-bio-mpe) must be used instead. 

## Steps

Resetting the FIDO2 application is something you hope you never need to do. Generally, the
only reason to reset the FIDO2 application is if the password has been blocked.

Nonetheless, if you decide to reset, here's what needs to be done:

1. "Reboot" the YubiKey by removing and re-inserting. Obtain a connection to the YubiKey
   once it has been reinserted. This will likely be done using a listener class (see
   [YubiKeyDeviceListener](xref:Yubico.YubiKey.YubiKeyDeviceListener)).
2. Within a time limit from the reboot (about 5 seconds), complete the reset command:

    1. Send the [ResetCommand](xref:Yubico.YubiKey.Fido2.Commands.ResetCommand). The
       initial command should return `0x6F30` (i.e. the `StatusWord` property of the
       [ResetResponse](xref:Yubico.YubiKey.Fido2.Commands.ResetResponse) object is
       `0x6F30`, the `Status` property is `Failed`).
    2. The user must remove then reinsert the YubiKey.
    3. Once the YubiKey has been reinserted, make a connection and send the
       `ResetCommand` again. The YubiKey will not respond with the `ResetResponse`
       immediately. It will not complete the reset until the user touches the contact.
       If the user does not touch the contact within a time limit, the YubiKey will
       return the `ResetResponse` with a `StatusWord` of `0x6F3A`, the CTAP error of
       timeout (the `Status` property will be `Failed`).
    4. If the user touches the contact within the time limit, then the FIDO2 application
       will be able to reset and the return will be `Success` (the `StatusWord` property
       will be `0x9000` and the `Status` property will be `Success`).

## Sample code

The FIDO2 sample program contains a class that demonstrates how to execute these steps. It
is located at `Yubico.YubiKey/examples/Fido2SampleCode`. The code that actually performs the
reset is in `.../Fido2SampleCode/YubiKeyOperations/Fido2Reset.cs`.

At the moment, there is no simple SDK API that can reset the FIDO2 application. That is,
there is no single `Fido2Session` method to call. Rather, the sample code demonstrates how
you can

1. create a listener to determine when the YubiKey is removed and reinserted, and
2. call the lower-level SDK command API to perform the actual reset.

Hence, if you want to add the option to reset the FIDO2 application in your app, one
option is to study the sample code and write something similar. You might be able to
"cut-and-paste" the `Fido2Reset` class in the sample code, replacing the messages and
`KeyCollector` to fit your needs. 
