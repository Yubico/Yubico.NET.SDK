---
uid: FidoU2fReset
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

# FIDO U2F Reset

It is possible to reset the U2F application on version 4 FIPS series YubiKeys. A reset
will replace the U2F key along with the attestation key and its cert. It will also remove
the PIN requirement, if there is one. In other words, it will reset the U2F application to
factory default settings.

However, there are some caveats:

* Only version 4 FIPS series YubiKeys can be reset. No other YubiKeys support U2F reset.
  This includes non-FIPS version 4 and version 5 FIPS series YubiKeys.
* The YubiKey will no longer be able to perform authentication with credentials previously
  created with the U2F application by that YubiKey.
* After a U2F reset, the YubiKey will no longer be in FIPS mode.
* After reset, it is not possible to put the YubiKey into FIPS mode, even if it had been
  in FIPS mode, and even if you set it with a password.
* The U2F application will be configured with a new attestation cert, which includes
  information that the YubiKey has been reset, and hence cannot be in FIPS mode.
* The process of resetting is a bit complicated; the .NET YubiKey API does not include a
  higer-level `U2fSession` method for performing the entire operation. Instead, you must
  send a series of lower-level commands described below.

## Steps

Resetting the U2F application is something you hope you never need to do. Generally, the
only reason to reset the U2F application is if the password has been blocked. Note that
the password is needed to add new credentials, but not to authenticate existing
credentials. That means that you probably will not want to reset even if the PIN is
blocked. You will obtain a new YubiKey for new credentials and continue using the old
YubiKey for the existing ones.

Furthermore, it is possible to have more than one YubiKey registered with a relying party.
This means that a new YubiKey can be registered with the old one's relying parties. The
old YubiKey can still be used to authenticate its credentials, and the new YubiKey can be
used to authenticate old and new credentials.

Nonetheless, if you decide to reset, here's what needs to be done:

1. "Reboot" the YubiKey by removing and re-inserting. Obtain a connection to the YubiKey
   once it has been reinserted. This will likely be done using a listener class (see
   [YubiKeyDeviceListener](xref:Yubico.YubiKey.YubiKeyDeviceListener)). 
2. Within a time limit from the reboot (about 5 seconds), complete the reset command:

    1. Send the [ResetCommand](xref:Yubico.YubiKey.U2f.Commands.ResetCommand). The initial
       command should return `ResponseStatus.ConditionsNotSatisfied` (i.e. the `Status`
       property of the [ResetResponse](xref:Yubico.YubiKey.U2f.Commands.ResetResponse)
       object is `ConditionsNotSatisfied` which happens when the `StatusWord` property is
       `SWConstants.ConditionsNotSatisfied`).
    2. Touch the YubiKey's contact.
    3. Send the `ResetCommand` again.
    4. If the `Status` of the `ResetResponse` is `Success`, then the U2F application has
       been reset. If it is `Failed`, then the process was not completed within the time
       limit.

## Sample code

The U2F sample program contains a class that demonstrates how to execute these steps. It
is located at `Yubico.YubiKey/examples/U2fSampleCode`. The code that actually performs the
reset is in `.../U2fSampleCode/YubiKeyOperations/U2fReset.cs`.

At the moment, there is no simple SDK API that can reset the U2F application. That is,
there is no single `U2fSession` method to call. Rather, the sample code demonstrates how
you can

1. create a listener to determine when the YubiKey is removed and reinserted, and
2. call the lower-level SDK command API to perform the actual reset.

Hence, if you want to add the option to reset the U2F applicaiton in your app, one option
is to study the sample code and write something similar. You might be able to
"cut-and-paste" the `U2fReset` class in the sample code, replacing the messages and
`KeyCollector` to fit your needs. 
