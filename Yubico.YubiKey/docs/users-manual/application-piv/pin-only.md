---
uid: UsersManualPivPinOnlyMode
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

# PIV PIN-only mode

There are a number of PIV operations that require management key authentication in order
to execute, such as generating a key pair or importing a certificate (complete list
[here](xref:UsersManualPinPukMgmtKey#operations-that-require-the-management-key)).
This is a requirement specified by the PIV standard.

However, the management key is a Triple-DES key, which is 24 binary bytes. While it is
easy for someone to enter a six to eight character PIN using a keyboard, how does one
enter 24 binary bytes? Can anyone remember 24 binary bytes? And how does one enter them
using a keyboard? As characters `'2' '9' 'A' ' 7' '0' 'B'` for `0x29A70B` and so on?

To help address these concerns, developers that build applications that use the PIV
capabilities of a YubiKey can determine how the management key is managed. One possibility
is to configure a YubiKey to be PIN-only. This means that any operation that requires the
management key will only require the PIN. For example, normally to generate a new key
pair, the application must supply the management key. But for a YubiKey configured for
PIN-only, it is possible to generate a key pair with only the PIN provided.

Note that this does not remove the management key, it simply means the SDK will be able to
authenticate the management key if the PIN is correctly verified. The management key is
still required, but now the user no longer needs to supply it.

Note also that this is for the PIV PIN only. This has no effect on PINs or passwords of
any other YubiKey application (OATH, FIDO, OpenPGP)

While this improves usability, there is a tradeoff. When the SDK sets a YubiKey to
PIN-only, it blocks the PUK as well (this is discussed below). This means it becomes much
more likely a YubiKey becomes unusable if a PIN is forgotten. If you use the PIN-only
feature, make sure everyone is aware that recovering from a lost PIN is likely impossible.

## Management key authentication

When we say the management key must be authenticated in order to execute some operations,
we mean that the management key must be authenticated in the same session. 

For example, suppose you want to import two certificates.

```csharp
    using (var pivSession = new PivSession(yubiKey))
    {
        pivSession.KeyCollector = SomeKeyCollector;

        pivSession.ImportCertificate(PivSlot.Authentication, someCert);
        pivSession.ImportCertificate(PivSlot.KeyManagement, anotherCert);
    }
```

During the first call to `Import`, the SDK will call on the `KeyCollector` to return the
management key. It will be authenticated and the certificate imported. The second time the
`Import` is called, there is no need to collect and authenticate the management key, the
first auth was good for the entire session.

You could call Authenticate yourself.

```csharp
    using (var pivSession = new PivSession(yubiKey))
    {
        pivSession.KeyCollector = SomeKeyCollector;
        pivSession.AuthenticateManagementKey();

        pivSession.ImportCertificate(PivSlot.Authentication, someCert);
        pivSession.ImportCertificate(PivSlot.KeyManagement, anotherCert);
    }
```

During the first call to `Import`, there will be no need to collect and authenticate the
management key, it is running in a session in which the management key has already been
authenticated.

As we will see below, there is a way to authenticate a management key in a session without
collecting it from the KeyCollector or requiring the user to enter it. Only the PIN is
required. In this way, any operation can be performed with only the PIN supplied during
the session.

## PIN-only modes

There are two PIN-only modes: PIN-protected and PIN-derived.

Which should you use? PIN-protected. There is a section below listing the ways
PIN-protected is the superior mode.

Then why is PIN-derived offered? Backwards compatibility. This is a feature offered
several years ago. It is possible that there are YubiKeys in use today that are set to
PIN-derived and the SDK will support them.

### PIN-protected

In this mode, the SDK stores the management key in the PRINTED data object. For
background, see the User's Manual entries on
[Data Objects](xref:UsersManualPivObjects) and
[GET and PUT DATA](xref:UsersManualPivCommands#get-data).

If the SDK encounters an operation that requires management key authentication, it will
collect the management key from the PRINTED data object and authenticate. That operation
and any subsequent operation that requires management key authentication will work during
that session.

PIN verification is needed in order to retrieve the data from the PRINTED data object.
That means the SDK can retrieve the management key only if the PIN has been verified. In
this way the YubiKey can be PIN-only.

Because the management key is stored in a data object that is protected by the PIN, we say
this mode is PIN-protected.

### PIN-derived

> [!WARNING]
> You should not use PIN-derived mode. This feature is provided only for backwards
> compatibility.

In this mode, the SDK will generate a random salt. Then it will derive a management key
from the PIN and salt. It will store the salt in the ADMIN DATA object.

If management key authentication is needed, the SDK collects the ADMIN DATA and the PIN.
It can then derive the management key and authenticate.

### PUK blocked

The SDK code that implements these modes will also block the PUK. The reason is so that it
is harder for a malicious administrator to take over a YubiKey.

Generally the PIN and management key are owned by the end user, the YubiKey's owner. An
administrator owns the PUK and can reset the PIN if the user forgets it. The malicious
administrator can change the PIN and do damage, but without the management key the damage
is limited.

If a YubiKey is PIN-only and the PUK is not blocked, then the PUK's owner can change the
PIN without knowing the PIN and therefore have control over the management key as well.
This person now has complete control of the YubiKey.

By blocking the PUK only the YubiKey's owner has control of the management key. This
reduces usability because it means recovering from a lost PIN is virtually impossible.
This is the tradeoff for improving usability with respect to the management key.

## Configure a YubiKey for PIN-only

A YubiKey must first be configured for PIN-only. With the SDK, call 
[PivSession.SetPinOnlyMode](xref:Yubico.YubiKey.Piv.PivSession.SetPinOnlyMode%2a).
That call requires you to specify the mode, `PinProtected`, `PinDerived`, or both.

```csharp
    using (var pivSession = new PivSession(yubiKey))
    {
        pivSession.KeyCollector = SomeKeyCollector;

        pivSession.SetYubiKeyPivPinOnly(PivPinOnlyMode.PinProtected);
    }
```

In order to set a YubiKey to PIN-only the management key must be authenticated. This is
necessary even if it has already been authenticated. The `SetYubiKeyPivPinOnly` method
will try to authenticate using the default management key. If that works, there will be no
call to the `KeyCollector` to retrieve the management key, the user will not have to enter
it somehow. If the default key does not work, then the method will call the
`KeyCollector`.

It is also necessary to verify the PIN. If it has already been verified and the mode to
set is `PinProtected`, the method won't collect the PIN again. But if the mode is
`PinDerived` (or both), whether the PIN has been verified or not, this method will call on
the `KeyCollector` to retrieve the PIN.

If the mode is PIN-protected, and the current management key is the default, this method
will generate a new random key, change the management key to this new value, and store it
in PRINTED. If the management key is not the default, this method will collect the current
key, it won't change it, and it will store it in PRINTED.

If the mode is PIN-derived, this method will change the management key, whether it is the
default or not.

## Authenticating the management key in subsequent sessions

Once a YubiKey has been configured to PIN-only, the SDK will be able to authenticate the
management key using the PIN only in each new `PivSession`. For example,

```csharp
    using (var pivSession = new PivSession(yubiKey))
    {
        pivSession.KeyCollector = SomeKeyCollector;

        pivSession.ImportCertificate(PivSlot.Authentication, someCert);
    }
```

The `ImporeCertificate` method requires management key authentication in order to execute.
Under the covers it will call the `PivSession.AuthenticateManagementKey` method. That
method will determine that the YubiKey is PIN-only, will request the PIN, obtain the
management key, and authenticate. While the `KeyCollector` must obtain the PIN, it will
not request the user supply the management key.

Note that if a YubiKey is set for PIN-derived, each time the management key is
authenticated, PIN entry is required. That is, even if the PIN has already been verified,
in order to authenticate the management key, the PIN must be entered again.

## Exceptions and other failures

The code is written to generally try something, and if that doesn't work, try something
else. For example, when authenticating a management key, the code will check to see if the
YubiKey is PIN-only. If not, perform regular authentication. Or if the YubiKey has data
indicating it is PIN-only, the SDK will try to authenticate using PIN-only, but if that
does not work, perform regular authentication.

However, it is possible the SDK will throw an exception if something goes wrong, rather
than trying something else. This generally happens if the data in the ADMIN DATA and/or
PRINTED storage areas is malformed. And even then, it has to be malformed in a particular
way.

The data in ADMIN DATA is supposed to be encoded as follows.

```text
    53 len
       80 L1
          81 01 (optional)
             --bit field, PUK blocked, Mgmt Key stored in protected data--
          82 L2 (optional)
             --salt, 16 bytes--
          83 L3 (optional)
             --time the PIN was last updated--
```

Suppose some application stores some other information in there and it looks like this.

```text
    53 len
       A1 len
          --something--
```

The SDK will assume the YubiKey is not PIN-only and move on, no exception thrown.

But suppose the data is the following.

```text
    53 len
       80 L1
          81 01
             03
          82 20
             --32 random bytes--
```

That seems to be correct, but it isn't. The salt, if there is one, must be 16 bytes, no
more no less. In this case, the data says the YubiKey is PIN-derived, but the data for
PIN-derived is wrong. The SDK might be able to move on, but it is possible it will throw
an `ArgumentException`. It will not try to authenticate using some other method.

Similarly, if the ADMIN DATA indicates that the YubiKey is PIN-protected, it expects the
data in PRINTED to be encoded as follows.

```text
    53 1C
       88 1A
          89 18
             <24 bytes>
```

If the data is encoded like this (the PIV-specified encoding of PRINTED)

```text
    53 L1
       01 len
          --Name, ASCII text, up to 125 bytes--
       02 len
          --Employee afiliation, ASCII text, up to 20 bytes--
       04 len
          --Expiration date, ASCII numbers YYYYMMMDD, fixed at 9 bytes--
       05 len
          --Agency Card Serial Number, ASCII text, up to 20 bytes--
       06 len
          --Issuer Id, ASCII text, up to 15 bytes--
       07 len
          --Org affiliation, line 1, ASCII text, up to 20 bytes--
       08 len
          --Org affiliation, line 2, ASCII text, up to 20 bytes--
       FE 00 (LRC, unused in PIV)
```

Then the SDK will not try to authenticate using PIN-protected and will try something else,
such as authenticating using the Key Collector.

But if the data is encoded like this

```
    53 1E
       88 1C
          89 1A
             <26 bytes>
```

The SDK might throw `ArgumentException` rather than try something else.

Another possibility is you call `GetPinOnlyMode`, it returns `PinProtected`, yet when the
`AuthenticateManagementKey` method is called, that fails and the Key Collector is
contacted. This can happen if the ADMIN DATA says a YubiKey is PIN-protected, but the data
in PRINTED is malformed.

Generally, if some other application is using ADMIN DATA and/or PRINTED, then you will
not be able to set a YubiKey to be PIN-only. It is also possible that you set a YubiKey to
PIN-only, but some other application mangles the stored data, and when your application
performs some action that requires management key authentication, the result could be
either an exception or a call to the Key Collector.

## PIN-protected vs. PIN-derived

If you want to set a YubiKey to PIN-only and need to decide which mode to use, here are
the reasons you will want to use PIN-protected.

1. PIN-protected is more secure
2. PIN-protected does not require entering the PIN as much (greater usability)

There is only one reason to use PIN-derived.

1. The application calling on the SDK must be compatible with another existing application
that can recognize only PIN-derived

The SDK will be able to determine if an existing YubiKey is set to PIN-derived and
automatically use it. That is, there is no need to set new YubiKeys to PIN-derived just
because older YubiKeys exist set for this mode.

Furthermore, it is possible to reset a YubiKey to PIN-protected if it is currently set to
PIN-derived. Your application that calls on the SDK can check the mode of each YubiKey it
encounters
([PivSession.GetPinOnlyMode](xref:Yubico.YubiKey.Piv.PivSession.GetPinOnlyMode%2a)) and
reset to PIN-protected mode only.
