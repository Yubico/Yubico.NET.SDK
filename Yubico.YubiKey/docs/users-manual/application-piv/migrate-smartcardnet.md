---
uid: UsersManualPivMigrateSmartcardNet
summary: *content
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

# Migrating from SmartCard.NET to the SDK

You might be using a product called the Yubico SmartCard.NET API, a C# library containing
classes an application developer can call to get YubiKey functionality.

If you want to migrate to the SDK, you will need to change your code. This document
describes what classes and methods to call in the SDK to perform the operations available
in the SmartCard.NET API.

- [Migrating from SmartCard.NET to the SDK](#migrating-from-smartcardnet-to-the-sdk)
  - [Making a connection](#making-a-connection)
    - [SDK](#sdk)
  - [Get the firmware version](#get-the-firmware-version)
    - [SDK](#sdk-1)
  - [Get the serial number](#get-the-serial-number)
    - [SDK](#sdk-2)
  - [Change the management key](#change-the-management-key)
    - [SDK](#sdk-3)
  - [Change the PIN](#change-the-pin)
    - [SDK](#sdk-4)
  - [Change the PUK](#change-the-puk)
    - [SDK](#sdk-5)
  - [Unblock the PIN](#unblock-the-pin)
    - [SDK](#sdk-6)
  - [Write MSROOTS](#write-msroots)
    - [SDK](#sdk-7)
  - [Read MSROOTS](#read-msroots)
    - [SDK](#sdk-8)
  - [Delete MSROOTS](#delete-msroots)
    - [SDK](#sdk-9)
  - [Create attestation statement](#create-attestation-statement)
    - [SDK](#sdk-10)
  - [Reset the card](#reset-the-card)
    - [SDK](#sdk-11)

## Making a connection

When using the SmartCard.NET product, you started with this:

```csharp
    IEnumerable<YkSmartCard> smartCardList = YkSmartCard.GetSmartCards();
```

At this point you enumerate through the list to find the YubiKey you want to use. It can
be something as simple as

```csharp
    YkSmartCard ykSmartCard = smartCardList.First();
```

You now have the object that you will use to call the YubiKey.

### SDK

See the User's Manual [entry on making a connection](/sdk-programming-guide/making-a-connection.md) for details.

Note that to make a connection, you must specify which application you want to use. There
are six possible applications: OTP, FIDO, FIDO2, OATH, OpenPgpCard, and PIV.

To migrate from the SmartCard.NET API, you will use the PIV application, building a
`PivSession` object.

If building a PIV session, your code will likely look something like this.

```csharp
    IEnumerable<IYubiKeyDevice> yubiKeyList = YubiKeyDevice.FindAll();
    IYubiKeyDevice yubiKeyToUse = yubiKeyList.First();

    using var pivSession = new PivSession(yubiKeyToUse);
```

## Get the firmware version

In the SmartCard.NET API, here is how you get the firmware version.

```csharp
    byte[] versionNumber = ykSmartCard.GetFirmwareVersion();
```

The result is five bytes. Only the first three bytes make up the actual version number.

```csharp
    byte versionMajor = versionNumber[0];
    byte versionMinor = versionNumber[1];
    byte versionPatch = versionNumber[2];
```

### SDK

With the SDK, you do not need to perform any operation other than choosing the YubiKey,
because one of the properties in the `IYubiKeyDevice` is `FirmwareVersion`.

```csharp
    IEnumerable<IYubiKeyDevice> yubiKeyList = YubiKeyDevice.FindAll();
    IYubiKeyDevice yubiKeyToUse = yubiKeyList.First();

    FirmwareVersion versionNumber = yubiKeyToUse.FirmwareVersion;
```

Inside the `FirmwareVersion` class are

```csharp
    byte versionMajor = versionNumber.Major;
    byte versionMinor = versionNumber.Minor;
    byte versionPatch = versionNumber.Patch;
```

## Get the serial number

In the SmartCard.NET API, here is how you get the serial number.

```csharp
    string serialNumber = ykSmartCard.GetSerialNumber();
```

If you want the value as an int, you can use `Parse` or `TryParse`.

```csharp
   bool isParsed = Int32.TryParse(serialNumber, out int serialNumberAsInt);
```

### SDK

With the SDK, you do not need to perform any operation other than choosing the YubiKey,
because one of the properties in the `IYubiKeyDevice` is `SerialNumber`.

```csharp
    IEnumerable<IYubiKeyDevice> yubiKeyList = YubiKeyDevice.FindAll();
    IYubiKeyDevice yubiKeyToUse = yubiKeyList.First();

    int serialNumber = yubiKeyToUse.SerialNumber;
```

If you want the serial number as a string (as was returned in the SmartCard.NET API), use
the `ToString` method. To get the exact same result, set the format.

```csharp
    string serialNumberString = yubiKeyToUse.SerialNumber.ToString("00000000");
```

## Change the management key

In the SmartCard.NET API, here is how you change the management key.

```csharp
    byte[] oldKey = CollectMgmtKey();
    byte[] newKey = CollectNewMgmtKey();

    ykSmartCard.ChangeManagementKey(oldKey, newKey);
```

### SDK

To change the management key, use the `PivSession.TryChangeManagementKey`.

In order to change the management key, the existing key must be authenticated first. There
is a method in the `PivSession` class, `TryAuthenticateManagementKey`, but it is called
automatically by the `TryChangeManagementKey` method if needed. Hence, you can
authenticate the existing key first if you want, but it is not necessary.

The methods that authenticate the current management key and change it will obtain the
keys using the `KeyCollector`. see the User's Manual entry on
[delegates](/sdk-programming-guide/delegates-in-sdk.md) for a discussion of the `KeyCollector`.

```csharp
    using var pivSession = new PivSession(yubiKeyToUse)
    {
        bool isChanged = pivSession.TryChangeManagementKey();
        if (!isChanged)
        {
            // handle error case.
        }
    }
```

## Change the PIN

In the SmartCard.NET API, here is how you change the PIN.

```csharp
    string oldPin = CollectPin();
    string newPin = CollectNewPin();

    ykSmartCard.ChangePin(oldPin, newPin, out int retriesRemaining);
```

### SDK

The method to change the PIN will obtain the current and new PINs using the
`KeyCollector`. see the User's Manual entry on [delegates](/sdk-programming-guide/delegates-in-sdk.md) for a
discussion of the `KeyCollector`.

```csharp
    using var pivSession = new PivSession(yubiKeyToUse)
    {
        bool isChanged = pivSession.TryChangePin();
        if (!isChanged)
        {
            // handle error case.
        }
    }
```

## Change the PUK

In the SmartCard.NET API, here is how you change the PUK.

```csharp
    string oldPuk = CollectPuk();
    string newPuk = CollectNewPuk();

    ykSmartCard.ChangePuk(oldPuk, newPuk, out int retriesRemaining);
```

### SDK

The method to change the PUK will obtain the current and new PUKs using the
`KeyCollector`. see the User's Manual entry on [delegates](/sdk-programming-guide/delegates-in-sdk.md) for a
discussion of the `KeyCollector`.

```csharp
    using var pivSession = new PivSession(yubiKeyToUse)
    {
        bool isChanged = pivSession.TryChangePuk();
        if (!isChanged)
        {
            // handle error case.
        }
    }
```

## Unblock the PIN

In the SmartCard.NET API, here is how you use the PUK to unblock the PIN.

```csharp
    string puk = CollectPuk();
    string newPin = CollectNewPin();

    ykSmartCard.UnblockPin(puk, newPin, out int retriesRemaining);
```

### SDK

The method to change the recover the PIN using the PUK will obtain the PUK and new PIN
using the `KeyCollector`. see the User's Manual entry on [delegates](/sdk-programming-guide/delegates-in-sdk.md)
for a discussion of the `KeyCollector`.

```csharp
    using var pivSession = new PivSession(yubiKeyToUse)
    {
        bool isChanged = pivSession.TryResetPin();
        if (!isChanged)
        {
            // handle error case.
        }
    }
```

## Write MSROOTS

In the SmartCard.NET API, here is how you load the MSROOTS data onto the YubiKey.

```csharp
    // Note that there is a limit of 3058 bytes for the data.
    byte[] msRootsData = CollectMsRootsData();
    string pin = CollectPin();

    var memoryStream = new MemoryStream(msRootsData);

    ykSmartCard.WriteMsRootsData(pin, memoryStream);
```

### SDK

The method to write the MSROOTS requires management key authentication. The method will
make the appropriate calls to authenticate the management key, if it has not been
authenticated yet, just make sure the `KeyCollector` has been loaded. see the User's
Manual entry on [delegates](/sdk-programming-guide/delegates-in-sdk.md) for a discussion of the `KeyCollector`.

Note that there are two versions of this method, one that takes in a byte array and
another that takes in a `Stream`.

```csharp
    using var pivSession = new PivSession(yubiKeyToUse)
    {
        byte[] msRootsData = CollectMsRootsData();
        bool isWritten = pivSession.WriteMsroots(msrootsData);
        if (!isWritten)
        {
            // handle error case.
        }
    }
```

```csharp
    using var pivSession = new PivSession(yubiKeyToUse)
    {
        byte[] msRootsData = CollectMsRootsData();
        var memoryStream = new MemoryStream(msRootsData);

        bool isWritten = pivSession.WriteMsrootsStream(memoryStream);
        if (!isWritten)
        {
            // handle error case.
        }
    }
```

## Read MSROOTS

In the SmartCard.NET API, here is how you obtain the MSROOTS data from the YubiKey.

```csharp
    Stream getData = ykSmartCard.ReadMsroots();
```

### SDK

Note that there are two versions of this method, one that returns a byte array and another
that returns a `Stream`.

```csharp
    using var pivSession = new PivSession(yubiKeyToUse)
    {
        byte[] msrootsContents = pivSession.ReadMsroots();
    }
```

```csharp
    using var pivSession = new PivSession(yubiKeyToUse)
    {
        Stream msrootsContents = pivSession.ReadMsrootsStream();
    }
```

## Delete MSROOTS

In the SmartCard.NET API, here is how you delete any MSROOTS data on the YubiKey.

```csharp
    ykSmartCard.DeleteMsroots(pinString);
```

### SDK

The method to delete the MSROOTS requires management key authentication. The method will
make the appropriate calls to authenticate the management key, if it has not been
authenticated yet, just make sure the `KeyCollector` has been loaded. see the User's
Manual entry on [delegates](/sdk-programming-guide/delegates-in-sdk.md) for a discussion of the `KeyCollector`.

```csharp
    using var pivSession = new PivSession(yubiKeyToUse)
    {
        pivSession.DeleteMsroots();
    }
```

## Create attestation statement

You can obtain an attestation statement (which is an X.509 certificate) only for keys
generated by the YubiKey itself. You cannot get one from a key imported into the YubiKey.

Suppose you generated a key pair in Slot 9A. Here's how you would get an attestation
statement for that key pair.

```csharp
    var cspParams = new CspParameters(1, "Microsoft Base Smart Card Crypto Provider");
    var rsaProvider = new RSACryptoServiceProvider(cspParams);
    string containerGuid = rsaProvider.CspKeyContainerInfo.UniqueKeyContainerName;

    byte[] attestationStatement = ykSmartCard.Attest(containerGuid);
```

### SDK

```csharp
    using var pivSession = new PivSession(yubiKeyToUse)
    {
        bool isValid = pivSession.TryCreateAttestationStatement(0x9A, out X509Certificate2? attestationStatement);
        if (!isValid)
        {
            // Handle error case.
        }
    }
```

## Reset the card

In the SmartCard.NET API, here is how you load the MSROOTS data onto the YubiKey.

```csharp
    ykSmartCard.ResetCard();
```

### SDK

To reset the PIV application on the YubiKey, both the PIN and PUK must be blocked. The
SDK's reset method will perform the necessary operations needed to block the PIN and PUK.
That is, simply call this method to reset, there's no need to do any work yourself to make
sure the PIN and PUK are blocked.

```csharp
    using var pivSession = new PivSession(yubiKeyToUse)
    {
        bool isReset = pivSession.TryResetPiv();
        if (!isReset)
        {
            // handle error case.
        }
    }
```
