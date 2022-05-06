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

However, the management key is a Triple-DES key, which is 24 binary bytes, or (beginning
with YubiKey 5.4.2) an AES key, which can be 16, 24, or 32 binary bytes. While it is easy
for someone to enter a six to eight character PIN using a keyboard, how does one enter 16,
24, or 32 binary bytes? Can anyone remember that many binary bytes? And how does one enter
them using a keyboard? As characters `'2' '9' 'A' ' 7' '0' 'B'` for `0x29 A7 0B` and so
on?

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

In addition, this adds another way a YubiKey's PIV application can become unusable. This
is discussed below in the section [Failures and recovery](#failures-and-recovery).

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
[GET and PUT DATA](xref:UsersManualPivCommands#get-data). In this mode, there is also
information stored in ADMIN DATA. It is simply a bit indicating whether the YubiKey is
configured for PIN-protected or not.

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

Note that in this mode, if a PIN has already been verified, the SDK will need to retrieve
the PIN again in order to derive the management key. This is because in normal PIN
verification, the PIN is collected and verified, but not saved.

If an application calls one of the Change PIN methods, the SDK will update the management
key. However, if some other application has overwritten the contents of ADMIN DATA and/or
PRINTED, the SDK will not be able to perform an update. It is a good idea to call the
[PivSession.TryRecoverPinOnlyMode](xref:Yubico.YubiKey.Piv.PivSession.TryRecoverPinOnlyMode%2a).
method before changing the PIN.

See the section below ["Failures and recovery"](#failures-and-recovery) for more
information on possible failures and how to recover.

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

Remember, it is not a good idea to set a YubiKey to PIN-derived, either alone or with
PIN-protected. That is, unless you have an older YubiKey that works with an older
application that supports only PIN-derived, you should not set a YubiKey to either
`PinDerived` or `PinDerived | PinProtected`.

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

### The management key's algorithm

Even though the YubiKey is PIN-only, there still is a management key, it is either stored
in PRINTED or derived from the PIN. Before version 5.4.2 of the YubiKey, a management key
was Triple-DES. Beginning with 5.4.2, though, it is possible to use either a Triple-DES or
an AES management key.

For a YubiKey before 5.4.2, to set it PIN-only will mean the management key will be
Triple-DES. But if the YubiKey is 5.4.2 or later, if you set it to PIN-only, you can
specify the management key's algorithm as well.

Suppose you specify the algorithm to be AES-128. If PIN-protected, the SDK will possibly
generate a new, 16-byte, random key, change the management key to this new value, then
store it in PRINTED. If PIN-derived, the SDK will generate a salt, derive a 16-byte value
from that salt and the PIN, change the management key to this new value, then store the
salt in ADMIN DATA.

If you want to set a YubiKey to PIN-only, and want to use AES if possible, but Tiple-DES
otherwise, then you will likely use code that looks something like this.

```csharp
    using (var pivSession = new PivSession(yubiKey))
    {
        pivSession.KeyCollector = SomeKeyCollector;

        PivAlgorithm mgmtKeyAlgorithm = yubiKey.HasFeature(YubiKeyFeature.PivAesManagementKey) ?
            PivAlgorithm.Aes128 : PivAlgorithm.TripleDes;
        pivSession.SetYubiKeyPivPinOnly(PivPinOnlyMode.PinProtected, mgmtKeyAlgorithm);
    }
```

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

The `ImportCertificate` method requires management key authentication in order to execute.
Under the covers it will call the `PivSession.AuthenticateManagementKey` method. That
method will determine that the YubiKey is PIN-only, will request the PIN, obtain the
management key, and authenticate. While the `KeyCollector` must obtain the PIN, it will
not request the user supply the management key.

Note that if a YubiKey is set for PIN-derived, each time the management key is
authenticated, PIN entry is required. That is, even if the PIN has already been verified,
in order to authenticate the management key, the PIN must be entered again.

## ADMIN DATA

One of the many PIV data objects on the YubiKey is known as ADMIN DATA. See also the
documentation on [GET DATA and PUT DATA](commands.md#getvendordatatable) and
[YubiKey-specific data](commands.md#getvendordatatable).

This is where information about PIN-only is stored. It contains a field that indicates
whether a YubiKey is configured for PIN-protected or not. It also contains a field that
holds a salt. If there is no salt there, the YubiKey is not configured for PIN-derived. If
there is a salt, then the YubiKey's management key is PIN-derived.

## Getting the PIN-only mode

If you want to know to which mode a YubiKey is configured, call
[PivSession.GetPinOnlyMode](xref:Yubico.YubiKey.Piv.PivSession.GetPinOnlyMode%2a). This
method looks at ADMIN DATA to determine the mode. It will return an enum
([PivPinOnlyMode](xref:Yubico.YubiKey.Piv.PivPinOnlyMode)) indicating the mode.

If it returns `PivPinOnlyMode.None`, then the YubiKey is not set to PIN-only. If the mode
is `PivPinOnlyMode.PinProtected`, then the YubiKey has already been configured to
PIN-protected.

The enum is a bit field, so it is possible to get `PinProtected | PinDerived`, because it
is possible to set a YubiKey to both PIN-protected and PIN-derived.

It is also possible to get the value `PinProtectedUnavailable | PinDerivedUnavailable`.
This means that it is not possible to set this YubiKey to PIN-only because some other
application has written incompatible data to the storage locations. This might not be
accurate, however, because the `GetPinOnlyMode` method returns a value based on the
contents of ADMIN DATA only, it never looks inside PRINTED. The next section discusses
failures, including `Unavailable`, and how to recover.

## Failures and recovery

The PIN-only system will break down if some application overwrites the contents of ADMIN
DATA and/or PRINTED. It has been documented that one should never write data to these
objects, and alternative storage locations are provided. It is very unlikely that any
application will write to either of these objects, but it is possible. This section
outlines what can go wrong and how to recover.

First of all, the SDK is written to generally try something, and if that doesn't work, try
something else. For example, when authenticating a management key, the code will check to
see if the YubiKey is PIN-only. If it is not, the SDK will perform regular authentication.
Or if the YubiKey has data indicating it is PIN-only, the SDK will try to authenticate
using PIN-only, but if that does not work, it will again fall back and perform regular
authentication.

If nothing works, the SDK will throw an exception, or in the case of
`TryAuthenticateManagementKey`, return `false`.

This generally happens if one application sets a YubiKey to PIN-only, and another
application overwrites the information in ADMIN DATA and/or PRINTED. If this happens, it
is possible an application will no longer be able to perform PIV operations that require
management key authentication, such as generating a key pair or importing a certificate,
because the management key is lost.

However, it is also possible that recovery from some of these failures is achievable,
using the method
[PivSession.TryRecoverPinOnlyMode](xref:Yubico.YubiKey.Piv.PivSession.TryRecoverPinOnlyMode%2a).

### ADMIN DATA contents

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

If the mode was PIN-derived, then the salt has been lost and there is no way to recover.

However, suppose the YubiKey had been configured for PIN-protected. The management key
might still be in PRINTED, and it will be possible to recover.

### PRINTED contents

If a YubiKey is configured for PIN-protected, then the contents of PRINTED will be

```text
    53 1C
       88 1A
          89 18
             <24 bytes>
```

If someone overwrites those contents, the management key is lost.

In this case, you can call `GetPinOnlyMode`, and it might return `PinProtected`, yet
authenticating the management key fails because the SDK cannot find the key data. The SDK
will call on the `KeyCollector` to retrieve the key.

### The Recover method

If you believe a YubiKey is configured for PIN-only, but `GetPinOnlyMode` returns
`Unavailable` or the SDK is unable to authenticate the management key, call
`TryRecoverPinOnlyMode`. This will read the contents of ADMIN DATA and PRINTED, and try to
determine if it is still possible to authenticate using one of the PIN-only techniques. If
so, it will authenticate the management key using that technique.

The return of this method is the enum `PivPinOnlyMode`. This will report the result of the
recovery effort.

First of all, if you call the `Recover` method for a YubiKey that has not been configured
for PIN-only, the return will likely be `None`. There is nothing to recover and the
management key will not be authenticated. Or if the YubiKey has been configured for
PIN-only and ADMIN DATA and PRINTED have not been overwritten, they contain the
appropriate data, then the method will authenticate the management key and return the mode
or modes.

If recovery is needed, and if this method is able to recover, it will also overwrite the
contents of ADMIN DATA and/or PRINTED. This means that your call will overwrite some other
application's data. That is, some other application wrote that data to one or both of the
data objects for a reason and is possibly dependent on the information currently in there.
The `Recover` method will, if it is able to recover, remove that data and the other
application will experience failures.

Note that in order to set the objects ADMIN DATA and PRINTED, the management key must be
authenticated. If the data in the two storage locations is such that the method just can't
recover, it will not be able to set them and they will remain unchanged.

This method will try to authenticate using the PIN-only methods, and if they fail, it will
try the default management key, and if that fails, it will try to collect the management
key using the `KeyCollector`. If it is able to authenticate using the default or the
collected key, it will clear the contents of ADMIN DATA and PRINTED.

To know the result of the recovery process, check the return value.

* `None`: One possibility is that no data was found in ADMIN DATA and PRINTED.
* `None`: Another possibility is that invalid data was found in ADMIN DATA and/or PRINTED,
the management key was authenticated using the default key or the `KeyCollector`, and the
storage locations were cleared of any data.
* `PinProtected`: The method was able to find the management key in PRINTED and
authenticate. ADMIN DATA is now set with the correct information (there will be no salt).
* `PinDerived`: it was able to authenticate the management key using a value derived
from the PIN and salt. PRINTED is now empty, ADMIN DATA indicates the YubiKey is
PIN-derived (with the correct sale) but not PIN-protected.
* `PinProtected | PinDerived`: The method was able to find the management key in PRINTED
and authenticate. The ADMIN DATA contained a salt and the key derived was the same one
found in PRINTED. Both are set with the correct information.
* `PinProtectedUnavailable`: ADMIN DATA had indicated the YubiKey was PIN-protected but
not PIN-derived. The data in PRINTED was incorrect, the management key is not
authenticated. The data in ADMIN DATA and PRINTED was not changed.
* `PinDerivedUnavailable`: ADMIN DATA had indicated the YubiKey was PIN-derived, but the
key derived from the PIN and salt did not authenticate. There was no data in PRINTED. The
method could not authenticate using the default key or the `KeyCollector`. The contents
were not changed.
* `PinProtectedUnavailable | PinDerivedUnavailable`: The information in both was
incorrect, the management key was not authenticated, the contents were not changed.

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
