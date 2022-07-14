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
            // If the return is false, then the YubiKey is version 4
            // FIPS series, but it is not in FIPS mode.
        }
        // Note that if the YubiKey is not version 4 FIPS series, the
        // VerifyFipsModeCommand is undefined. A call to VerifyFipsModeResponse.GetData
        // will result in an exception. 
    }
```

### Setting FIPS mode

To put the U2F application of a YubiKey 4 FIPS Series key into FIPS mode, you must set the
U2F PIN. Call [U2fSession.SetPin](xref:Yubico.YubiKey.U2f.U2fSession.SetPin%2a), which
obtains the PIN from the `KeyCollector`, or the
[U2fSession.TrySetPin](xref:Yubico.YubiKey.U2f.U2fSession.TrySetPin%2a) method that takes
in the PIN (no `KeyCollector`).

```c#
    // This is the ASCII PIN "123456".
    byte[] newPin = new byte[] {
        0x31, 0x32, 0x33, 0x34, 0x35, 0x36
    };

    using (var u2fSession = new U2fSession(yubiKey))
    {
        if (!u2fSession.TrySetPin(newPin))
        {
            // If this fails, call some error handling code.
        }
    }
```

The U2F PIN can be any binary data from 6 to 32 bytes long. It will likely be input by the
end user at the keyboard, which would make it similar to a normal password.

Once you set the PIN, the YubiKey will be in FIPS mode and the `VerifyFipsModeCommand`
will return true.

## Retries

If a caller wants to verify or change a PIN, the current PIN must be entered. If a wrong
value is provided, the PIN won't be verified or changed and the caller can try again.
However, there are limits to how many times a wrong value can be entered.

If an incorrect PIN is entered three times in a row, the U2F application is blocked. The
only way to unblock it is to reset it. It is important to know that after resetting, the
YubiKey can no longer be put into FIPS mode.

Note that with the FIDO2 application on the YubiKey 5 FIPS series, the PIN retry count is
eight. However, that is FIDO2 on YubiKey 5. The total retry count fo the U2F application
on YubiKey 4 FIPS series is three.

If the correct PIN is verified before the U2F application is blocked, the retries
remaining count returns to three.

Unfortunately in the version 4 FIPS series YubiKey, it is not possible to know how many
U2F PIN retries are remaining. That is, if the wrong PIN has been entered, the SDK will
return to the caller indicating that the wrong PIN was entered, but will not be able to
report the number of retries remaining.

## Removing the PIN

Once a PIN is set on the U2F application, it is not possible to remove it. That is, if
you call `U2fSession.TrySetPin` (or the SetPin command) with an "Empty" PIN, the YubiKey
will not reset the PIN, instead it will return an error. The SDK will throw an exception.

The only way to remove a U2F PIN is to reset the key's U2F application. A reset generally
restores the application to its original factory settings. However, with YubiKey 4 FIPS
series, the reset also deletes the attestation key and cert (they are replaced with a
"reset" key and cert) and the U2F application will no longer be able to be set to FIPS
mode. At this point, if you try to set the PIN, the YubiKey will set the PIN, but it will
not be in FIPS mode. The YubiKey will still be able to register new U2F credentials, but
they will not be "FIPS" credentials.

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
