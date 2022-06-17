---
uid: FidoU2fFipsMode
---

<!-- Copyright 2021 Yubico AB

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License. -->

# FIDO U2F and FIPS

There are FIPS versions of YubiKey. These can be used by applications that require FIPS
certification or the use of FIPS-certified products. However, there are some complexities
to using a YubiKey for FIDO U2F in a FIPS environment.

First of all, in order to be FIPS-compliant, a product can use only FIPS-specified
algorithms in FIPS-certified products. Because FIPS does not mention FIDO U2F, it would
seem that it is not possible to use U2F in a FIPS-compliant way. However, NIST (the
National Institute of Standards and Technology, the government agency that oversees FIPS)
has allowed U2F to be used in FIPS-compliant applications if the YubiKey's U2F application
requires a PIN.

The U2F standard does not say anything about setting a PIN on the application. The
standard does not expect a PIN would be required to use U2F, only touch. However, because
the U2F standard does not forbid the use of PINs, it is possible to configure an
application to require one.

## YubiKey 4 FIPS series

Only version 4 FIPS series YubiKeys can be used for FIDO U2F in a FIPS environment. The
U2F application of all other YubiKey models cannot be FIPS-compliant. This includes the
version 5 FIPS series YubiKeys. Even though it is a FIPS-certified device, its FIDO U2F
application is not FIPS-compliant. Note that a version 5 FIPS series YubiKey supports
FIDO2 and that can be FIPS-compliant.

You can determine programmatically whether a given YubiKey is a 4 FIPS Series key with the
[GetDeviceInfoCommand](u2f-commands.md#get-device-info).

```c#
    var getDeviceInfoCmd = new GetDeviceInfoCommand();
    GetDeviceInfoResponse getDeviceInfoRsp = connection.SendCommand(getDeviceInfoCmd);
    YubiKeyDeviceInfo deviceInfo = getDeviceInfoRsp.GetData();

    if (deviceInfo.IsFipsSeries && (deviceInfo.FirmwareVersion.Major == 4))
    {
        // This is a version 4 FIPS YubiKey.
    }
```

## FIPS mode

Even though a version 4 FIPS YubiKey is a FIPS-certified device, the FIDO U2F application
is not itself FIPS-compliant until it is set with a PIN.

During manufacturing, YubiKeys are not configured with a U2F PIN. Therefore, a YubiKey's
U2F application is not in FIPS mode by default.

After setting the PIN, the YubiKey's U2F application is in FIPS mode. You can
programmatically determine if a YubiKey is in FIPS mode or not with
[VerifyFipsModeCommand](u2f-commands.md#verify-fips-mode).

```c#
    var getDeviceInfoCmd = new GetDeviceInfoCommand();
    GetDeviceInfoResponse getDeviceInfoRsp = connection.SendCommand(getDeviceInfoCmd);
    YubiKeyDeviceInfo deviceInfo = getDeviceInfoRsp.GetData();

    // Is this YubiKey 4 FIPS series? 
    if (deviceInfo.IsFipsSeries && (deviceInfo.FirmwareVersion.Major == 4))
    {
        // If it is YubiKey 4 FIPS series, we can get the FIPS mode.
        var vfyFipsModeCmd = new VerifyFipsModeCommand();
        VerifyFipsModeResponse vfyFipsModeRsp = connection.SendCommand(vfyFipsModeCmd);
        if (vfyFipsMode.GetData())
        {
            // If the return from GetData is true, then this is
            // YubiKey 4 FIPS series in FIPS mode.
        }
        // Note that if the YubiKey is not version 4 FIPS series, the
        // VerifyFipsModeCommand is undefined. A call to VerifyFipsModeResponse.GetData
        // will result in an exception. 
    }
```

### Setting FIPS mode

To put the U2F application of a YubiKey 4 FIPS Series key into FIPS mode, you must set the
U2F PIN. Call the [SetPinCommand](u2f-commands.md#set-pin). Its input is the current PIN
and the new PIN. If there is no current PIN yet, simply pass in an `Empty`.

```c#
    // This is the ASCII PIN "123456".
    byte[] newPin = new byte[] {
        0x31, 0x32, 0x33, 0x34, 0x35, 0x36
    };

    // To set the PIN the first time, pass in an Empty currentPin.
    var setPinCmd = new SetPinCommand(ReadOnlyMemory<byte>.Empty, newPin);
    SetPinResponse setPinRsp = connection.SendCommand(setPinCmd);
    if (setPinRsp.Status != ResponseStatus.Success)
    {
        // handle error here
    }
```

The U2F PIN can be any binary data from 6 to 32 bytes long. It will likely be input by the
end user at the keyboard, which would make it a normal password.

Once you set the password, the YubiKey will be in FIPS mode and the
`VerifyFipsModeCommand` will return true.

## Removing the PIN

Once a PIN is set on the U2F application, it is not possible to remove it with the
`SetPinCommand`. If you send the SetPin command to the YubiKey with an "Empty" new PIN,
the YubiKey will not reset the PIN, instead it will return an error. The SDK will throw an
exception.

The only way to remove a U2F PIN is to reset the key's U2F application. A reset generally
restores the application to its original factory settings. However, with YubiKey 4 FIPS
series, the reset also deletes the attestation key and cert and the U2F application will
no longer be able to be set to FIPS mode. At this point, if you try to set the PIN, the
YubiKey will return an error.

Note also that when you reset the U2F application, the master secret is changed, so all
previous U2F registrations will be lost.

## Summary

What this all means is that if you have a version 4 FIPS series YubiKey,

* upon manufacture the U2F application is not in FIPS mode
* to put it into FIPS mode, set the PIN
* it is possible to change the PIN
* once it is in FIPS mode with a PIN, it is not possible to remove the PIN or take it out
of FIPS mode, except by resetting the entire application
* upon reset, all registrations using that YubiKey will be lost
* once the U2F application has been reset, it is no longer in FIPS mode and can no longer
be put into FIPS mode
