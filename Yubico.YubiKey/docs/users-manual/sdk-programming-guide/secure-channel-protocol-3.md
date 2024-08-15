---
uid: UsersManualScp03
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

# Secure Channel Protocol 3 ("SCP03")

Commands sent to the YubiKey, or responses from the YubiKey, may contain
senstive data that should not leak to or be tampered with by other applications
on the host machine. The operating system of the host machine may provide at
least one layer of protection by isolating applications from each other using
separation of memory spaces, permissioned access to system resources like
devices, and other techniques.

The YubiKey also supports an additional layer of protection that aims to provide
confidentiality and intergrity of communication to and from the YubiKey, using a
standardized protocol called "Secure Channel Protocol 3" (commonly referred to
as "SCP03"). This standard prescribes methods to encrypt and authenticate smart card
(CCID) messages. That is, APDUs and responses are encrypted and contain checksums. If
executed properly, the only entities that can see the contents of the messages (and
verify their correctness) will be the YubiKey itself and authorized applications. This
protocol is produced by <a href="https://globalplatform.org/">GlobalPlatform</a>, an industry consortium of hardware security
vendors that produce standards.

Think of SCP03 as wrapping or unwrapping commands and responses. Before sending the actual
command to the YubiKey, wrap it in an SCP03 package. The YubiKey will be able to unwrap it
and execute the recovered command. After the YubiKey builds the actual response, it wraps
it in an SCP03 package, and the SDK can then unwrap the package and process the result.

Only YubiKey 5 Series devices with firmware version 5.3 or later support the Secure
Channel Protocol (version 3). Other SCP03 versions are not supported by any YubiKey. In
addition, while the PIV, OATH, OpenPGP, and YubiCrypt applications use the smart card
protocols, only the PIV application supports sending and receiving SCP03 messages.

SCP03 relies entirely on symmetric cryptography. Hence, its security is dependent on
making sure only authorized applications have access to the symmetric keys. The standard
specifies no method for distributing keys securely.

This added layer of protection makes the most sense when the communication
channel between the host machine and the device could feasibly be compromised.
For example, if you tunnel YubiKey commands and responses over the Internet, in
addition to standard web security protocols like TLS, it could makes sense to
leverage SCP03 as an added layer of defense. Additionally, several 'card
management systems' use SCP03 to securely remotely manage devices.

> [!NOTE]
> SCP03 works only with SmartCard applications, namely PIV, OATH, and OpenPgp.
> However, SCP03 is supported only on series 5 YubiKeys with firmware version 5.3
> and later, and only the PIV application.

## Static Keys

SCP03 relies on a set of shared, secret, symmetric cryptographic keys, called the
["static keys"](xref:Yubico.YubiKey.Scp03.StaticKeys), which are known to the application
and the YubiKey.

Most YubiKeys are manufactured with a default set of keys. The value of these keys is
specified by the standard, so they are not secret. It is important to emphasize that using
the default SCP03 keys to connect to a device offers *no additional protection* over
cleartext communication.

It is possible to manufacture YubiKeys with a non-default SCP03 key set (this will be a
custom order, see your sales rep if you are interested in a custom order), or to change
the keys on a YubiKey at any time. Hence, if you want to take advantage of SCP03, your
first task will be to make sure the YubiKey is loaded with a set of keys that only
authorized applications will know. See the sections below on
[replacing the default key set](#replacing-the-default-key-set) for a discussion on how to
do that.

The three keys that comprise the `StaticKeys` are 16 byte, AES-128 cryptographic
keys, referred to in the GlobalPlatform SCP03 Specification as the channel encryption key
(Key-ENC), channel MAC key (Key-MAC), and data encryption key (Key-DEK). In the SDK, these
keys are held in a [StaticKeys](xref:Yubico.YubiKey.Scp03.StaticKeys) object.

### Three key sets

A YubiKey can contain up to three SCP03 key sets. Think of the YubiKey as having three
slots for SCP03 keys.

```txt
   slot 1:   ENC   MAC   DEK
   slot 2:   ENC   MAC   DEK
   slot 3:   ENC   MAC   DEK
```

Each key is 16 bytes. YubiKeys do not support any other key size.

Standard YubiKeys are manufactured with one key set, and each key in that set is the
default value. The default value (prescribed by the standard) is `0x40 41 42 ... 4F`.

```txt
   slot 1:   ENC(default)  MAC(default)  DEK(default)
   slot 2:   --empty--
   slot 3:   --empty--
```

The SCP03 standard specifies that each key set be given a Key Version Number (KVN). That
is, when you specify a particular key set, you won't specify it by slot number but rather
by KVN. Think of the KVN as the key set's name.

The standard declares that the default key set will have the KVN of 255 (0xFF). It also
specifies that the KVN for a non-default key set can be any number from 0x01 to 0x7F. The
standard places no other restrictions on the KVN. For example, a standard-compliant device
that holds three key sets could allow a caller to specify 0x5A, 0x21, 0x30 as the three
KVNs.

However, the standard also specifies that a device can put its own limitations onto the
KVNs, and that's what the YubiKey does. A YubiKey only supports KVNs of 1, 2, 3, and 255.

In addition, each key in the set is given a Key Identifier (KeyId). The YubiKey allows
only 1, 2, and 3 as the KeyIds, but there is no place in the SDK where a KeyId is needed.
That is, if you use the SDK to perform SCP03 on the YubiKey, the KeyId will never be used.

This is the initial state of the standard YubiKey.

```txt
   slot 1: KVN=0xff  KeyId=1:ENC(default)  KeyId=2:MAC(default)  KeyId=3:DEK(default)
   slot 2:   --empty--
   slot 3:   --empty--
```

When you add or replace a key set, you must specify the Key Version Number. Remember, the
YubiKey allows only 1, 2, or 3. For example, if you replace the default key set and you
specify 1 as the Key Version Number, your YubiKey's SCP03 key situation would look like
this:

```txt
   slot 1: KVN=1  ENC(new)  MAC(new)  DEK(new)
   slot 2:   --empty--
   slot 3:   --empty--
```

Suppose you have a YubiKey that has only the default key set, and you put a new SCP03 key
set onto the device. But this time, you specify 2 as the Key Version Number. In this case,
the new key set would be placed into the second slot, but the default key set would still
be removed.

```txt
   slot 1:   --empty--
   slot 2: KVN=2  ENC(new)  MAC(new)  DEK(new)
   slot 3:   --empty--
```

This happens only when the default key set is on the YubiKey. Once the default key set has
been removed, it is possible to add a new key set without removing a previous one. That
is, after the default key set has been removed, you can add a new key set to an empty slot
without removing any existing key set.

For example, if there is a key set for KVN=2, and only KVN=2, and you add a key set to
KVN=3, the YubiKey's state will be this:

```txt
   slot 1:   --empty--
   slot 2: KVN=2  ENC       MAC       DEK
   slot 3: KVN=3  ENC(new)  MAC(new)  DEK(new)
```

It is also possible to replace a key set, that is described [below](#replacing-a-key-set).

#### Slot number and KVN

In SCP03, there is no concept of slots or slot number. We use those terms just to
illustrate the model. They are meant to describe a location on the YubiKey where key sets
reside.

When writing code to use SCP03, you use the KVN. That is what the standard specifies.
Your code will always specify a KVN, and the only KVNs you can use with a YubiKey are
1, 2, 3, and 255.

You can imagine the actual key data located in slots on the YubiKey, and in the model
described in this document, the slot number and KVN are almost always the same. That is,
slot 1 holds the key set with KVN=1, and so on. The exception is when the only key set
on a YubiKey is the default key. In that case, slot 1 is holding the key set with
KVN=255.

### The `StaticKeys` class

In order to perform SCP03 operations in the SDK, you must supply one of the key sets
currently residing on the YubiKey. This is done using the `StaticKeys` class.

```csharp
    using var scp03Keys = new StaticKeys(keyDataMac, keyDataEnc, keyDataDek)
    {
        KeyVersionNumber = 2
    }
```

The `StaticKeys` class implements `IDisposable`. Hence, you should use the `using` keyword
when instantiating. When the object goes out of scope, the `Dispose` method will be called
and the key data will be overwritten.

When you supply a `StaticKeys` object to the SDK (version 1.9 and later), the object is
cloned (a deep copy is made).

### Managing the keys

It is the responsibility of the application to know which SCP03 keys are loaded on a
YubiKey. There are no calls to return "metadata". For example, there is no command that
can return how many key sets are loaded on a YubiKey or whether the default key has been
replaced.

Your application must know if a particular YubiKey has been programmed at manufacture with
non-default keys, or if the YubiKey is still configured with the default key set. If you
replace the default key set, your application must manage the keys that were loaded
because it will need to provide those keys the next time you use SCP03.

Your application must know if more than one key set is loaded onto a YubiKey, what the key
data is, and what Key Version Number is used for each set.

## Using SCP03

There are two categories of SCP03 operations:

* Using SCP03 to secure Smart Card communications
* Performing SCP03 operations, such as changing or adding a key set

### Securing Smart Card communications

Suppose you are performing PIV operations. You would normally get a YubiKey and then
instantiate the `PivSession` class.

```csharp
    if (!YubiKeyDevice.TryGetYubiKey(serialNumber, out IYubiKeyDevice yubiKeyDevice))
    {
        // error, can't find YubiKey
    }
    using (var pivSession = new PivSession(yubiKeyDevice))
    {
      . . .
    }
```

In order to use SCP03 to securely communicate with the YubiKey, all you need to do is
obtain your `StaticKeys` and supply that key set to the `PivSession` constructor.

```csharp
    if (!YubiKeyDevice.TryGetYubiKey(serialNumber, out IYubiKeyDevice yubiKeyDevice))
    {
        // error, can't find YubiKey
    }
    // This is the only change you need to make in order to make sure your PIV operations
    // are protected using SCP03 (assuming you have some method to retrieve the key set).
    using StaticKeys scp03Keys = RetrieveScp03KeySet();
    using (var pivSession = new PivSession(yubiKeyDevice, scp03Keys))
    {
      . . .
    }
```

Once you have built the `PivSession` with SCP03, there's no other SCP03 operation you
need to do. Each PIV operation will now be protected with SCP03.

Under the covers, each command sent to the YubiKey will first pass through an SCP03 object
which encrypts the data and appends a checksum. That is, before "leaving the SDK", each
command is "wrapped" using SCP03.

The YubiKey returns responses that have been protected using SCP03. Before parsing the
response, the SDK must verify the checksum and decrypt the data. In other words, the first
thing the SDK does is remove the response's SCP03 "wrapper".

If you are calling commands directly instead of using the `PivSession`, you can still use
SCP03 by making a connection using
[ConnectScp03](xref:Yubico.YubiKey.IYubiKeyDevice.ConnectScp03%2A):

```csharp
    if (!YubiKeyDevice.TryGetYubiKey(serialNumber, out IYubiKeyDevice yubiKeyDevice))
    {
        // error, can't find YubiKey
    }

   using IYubiKeyConnection connection = yubiKeyDevice.ConnectScp03(YubiKeyApplication.Piv, scp03Keys);
```

### Performing SCP03 operations

It is possible you must perform some SCP03 operation directly. That is, you want to
perform an operation that is not wrapping a PIV command or unwrapping a response. For
example, you might need to replace the default SCP03 key set. In this case, use the
[Scp03Session](xref:Yubico.YubiKey.Scp03.Scp03Session) class.

```csharp
    if (!YubiKeyDevice.TryGetYubiKey(serialNumber, out IYubiKeyDevice yubiKeyDevice))
    {
        // error, can't find YubiKey
    }
    using StaticKeys scp03Keys = RetrieveScp03KeySet();
    using (var scp03Session = new Scp03Session(yubiKeyDevice, scp03Keys))
    {
      . . .
    }
```

Operations "inside" the `Scp03Session` will be protected using SCP03. That is, in order to
preform an SCP03 operation, one or more commands will be sent to the YubiKey. These
commands are instructing the YubiKey to perform some set of SCP03 operations. Each of
these commands, and the responses, will be wrapped using SCP03 as well.

The SDK has methods that perform these SCP03 operations:

* [Replace the default key set](#replacing-the-default-key-set)
* [Add a new key set](#adding-a-new-key-set)
* [Replace a non-default key set](#replacing-a-key-set)
* [Removing a key set](#removing-a-key-set)
* [Removing all key sets](#removing-all-key-sets) (reset the YubiKey to the default key set)

#### Replacing the default key set

One of the first things you want to do with SCP03 is to replace the default key set. To do
so, call [Scp03Session.PutKeySet](xref:Yubico.YubiKey.Scp03.Scp03Session.PutKeySet%2A).

```csharp
    bool isValid = YubiKeyDevice.TryGetYubiKey(serialNumber, out IYubiKeyDevice yubiKeyDevice))
    // using the no-arg constructor will build a StaticKeys object using the default
    // key set with the KVN=0xFF.
    using var scp03Keys = new StaticKeys();
    using (var scp03Session = new Scp03Session(yubiKeyDevice, scp03Keys))
    {
        // Assume you have some method that will retrieve the key set you want to use.
        // Perhaps it uses a key derivation function based on a YubiKey's serial number.
        // In this sample, assume the GetStaticKeys method will return a StaticKeys object
        // with the KVN of the input arg, in this case, the KVN will be 1.
        using StaticKeys newKeys = GetStaticKeys(1);
        scp03Session.PutKeySet(newKeys);
    }
```

The `StaticKeys` object contains a `KeyVersionNumber`. Remember, the KVN is essentially
the key set's "name". The only KVNs for non-default key sets the YubiKey allows are
1, 2, and 3.

Once a new key set has been added, the default key set is no longer available. That is,
even if you specify KVN=2 or 3 when you put a new key set onto a YubiKey, the default
key set will not remain. You might think that the default key set is in slot 1, and if
you put a new key set into slot 2 (KVN=2), the default key set will not be affected.
However, if a YubiKey is set with the default key set, and you call the `PutKeySet`
method, the default key set will be removed, no matter what the new key set's KVN is.

#### Adding a new key set

To add a new key set, simply make sure the KVN of the `StaticKeys` you add is not the same
as the KVN of the existing key set. For example, suppose you replaced the default key set,
specifying the new key set's KVN as 1.

```txt
   slot 1: KVN=1  ENC       MAC       DEK
   slot 2:   --empty--
   slot 3:   --empty--
```

To add a new key set with KVN=2, do the following:

```csharp
    bool isValid = YubiKeyDevice.TryGetYubiKey(serialNumber, out IYubiKeyDevice yubiKeyDevice);
    // Assume you have a method to retrieve key sets, and you specify retrieving
    // the key set with KVN of 1.
    using StaticKeys scp03Keys = GetStaticKeys(1);
    using (var scp03Session = new Scp03Session(yubiKeyDevice, scp03Keys))
    {
        // Now get the key set with KVN of 2. You have the keys, they just have
        // not been loaded onto the YubiKey yet.
        StaticKeys newKeys = GetStaticKeys(2);
        // If you want, make sure the StaticKeys object has the correct KVN.
        newKeys.KeyVersionNumber = 2;
        scp03Session.PutKeySet(newKeys);
        newKeys.Clear();
    }
```

```txt
   slot 1: KVN=1  ENC       MAC       DEK
   slot 2: KVN=2  ENC(new)  MAC(new)  DEK(new)
   slot 3:   --empty--
```

#### Replacing a key set

In order to replace a key set, use the `PutKeySet` method. However, there is one
restriction: you can replace a key set only if you build the `Scp03Session` using the key
set that is to be replaced.

```csharp
    // This works
    // Replace the key set with KVN=1 with new keys.
    // Use the current KVN=1 key set to build the Scp03Session, get the new key set (the
    // StaticKeys object holding the new key set will have KeyVersionNumber=1), and Put that
    // new key set.
    using StaticKeys scp03Keys = GetStaticKeys(1);
    using (var scp03Session = new Scp03Session(yubiKeyDevice, scp03Keys))
    {
        // Assume you have a program that will retrieve a new key set for the given KVN.
        using StaticKeys newKeySet = GetNewStaticKeys(1);
        newKeySet.KeyVersionNumber = 1;
        scp03Session.PutKeySet(newKeySet);
    }
```

The following demonstrates an attempt to replace a key set that will not work. The result
will be an exception. In this case, suppose a YubiKey has two key sets loaded, KVN=1 and
KVN=2.

```txt
   slot 1: KVN=1  ENC       MAC       DEK
   slot 2: KVN=2  ENC       MAC       DEK
   slot 3:   --empty--
```

```csharp
    // This does NOT work
    // Assume you have a key set with KVN of 1, and you already have a key set with KVN of 2.
    // Use the current KVN=1 key set to build the Scp03Session, get the new key set (the
    // StaticKeys object holding the new key set will have KeyVersionNumber=2), and try to
    // Put that new key set.
    using StaticKeys scp03Keys = GetStaticKeys(1);
    using (var scp03Session = new Scp03Session(yubiKeyDevice, scp03Keys))
    {
        using StaticKeys newKeySet = GetNewStaticKeys(2);
        newKeySet.KeyVersionNumber = 2;
        scp03Session.PutKeySet(newKeySet);
    }
```

The `PutKeySet` method will throw an exception. In order to change the key set with KVN=2,
you must create the `Scp03Session` using the key set with KVN=2.

#### Removing a key set

To delete a key set, simply call the `DeleteKeySet` method. There is one restriction: the
key set used to build the `Scp03Session` must NOT be the key set deleted, unless there are
no more key sets on the YubiKey. Otherwise no key set will be deleted and the SDK will
throw an exception.

```csharp
    using StaticKeys scp03Keys = GetStaticKeys(1);
    using (var scp03Session = new Scp03Session(yubiKeyDevice, scp03Keys))
    {
        scp03Session.DeleteKeySet(2, false);
    }
```

Notice that this sample code created the `Scp03Session` using KVN=1, but deleted the key
set with KVN=2.

#### Removing all key sets

After you use one of the key sets to remove the other two, it is possible to remove that
last key set. If it is removed, then the YubiKey will reset its SCP03 program to the
original, default state. Namely, there will be one key set, and it will be the default
(KVN=0xff).

For example, suppose you have three SCP03 key sets on the YubiKey.

```txt
   slot 1: KVN=1  ENC       MAC       DEK
   slot 2: KVN=2  ENC       MAC       DEK
   slot 3: KVN=3  ENC       MAC       DEK
```

Use the KVN=3 key set to delete key sets 1 and 2.

```csharp
    using StaticKeys scp03Keys = GetStaticKeys(3);
    using (var scp03Session = new Scp03Session(yubiKeyDevice, scp03Keys))
    {
        scp03Session.DeleteKeySet(1, false);
        scp03Session.DeleteKeySet(2, false);
    }
```

```txt
   slot 1:   --empty--
   slot 2:   --empty--
   slot 3: KVN=3  ENC       MAC       DEK
```

Now it is possible to delete the KVN=3 key set using an `Scp03Session` created using
the KVN=3 key set. Pass true for the second argument.

```csharp
    using StaticKeys scp03Keys = GetStaticKeys(3);
    using (var scp03Session = new Scp03Session(yubiKeyDevice, scp03Keys))
    {
        scp03Session.DeleteKeySet(3, true);
    }
```

Passing `true` lets the SDK know this is the last key to be deleted. It can then call the
DELETE KEY command with the appropriate parameters, and the YubiKey will be able to delete
the key set. After this delete, the state of SCP03 key sets will be the following:

```txt
   slot 1: KVN=0xff  KeyId=1:ENC(default)  KeyId=2:MAC(default)  KeyId=3:DEK(default)
   slot 2:   --empty--
   slot 3:   --empty--
```
