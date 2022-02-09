---
uid: UsersManualPivObjects
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

# PIV data objects

The YubiKey's PIV application has space for storing certain elements other than keys and
certificates. Most of these elements are defined by the PIV standard, but there are also
Yubico-defined items. It is also possible for an app to store its own data in its own
locations.

A data object is made up of a tag and data. The tag is simply a number and the data is
different for each tag. That is, a particular number will be defined as a PIV DataTag,
and associated with it is a set of elements that are combined into a single blob of data
following a specific encoding.

## The DataTags

There are three classes of DataTag in the YubiKey:

- PIV standard-defined tags
- Yubico-defined tags
- undefined tags

On a YubiKey, any number between `0x005F0000` and `0x005FFFFF` (inclusive) can be a valid
DataTag. In addition, there are two numbers not in that range that are valid DataTags:
`0x0000007E` and `0x00007F61`.

The following table lists the numbers the PIV standard defines as DataTags (see also the
[table of PIV tags](commands.md#getdatatable) in the article on PIV commands).

#### Table 1A: PIV standard-defined DataTags
|  Number  |  Name  |  PIN required for read  |
| :------: | :----: | :---------------------: |
| 0x0000007E | DISCOVERY | No |
| 0x00007F61 | BITGT | No |
| 0x005FC101 | Card Auth (cert) | No |
| 0x005FC102 | CHUID | No |
| 0x005FC103 | Fingerprints | Yes |
| 0x005FC104 | -unused- | No |
| 0x005FC105 | Auth (cert) | No |
| 0x005FC106 | Security | No |
| 0x005FC107 | CCC | No |
| 0x005FC108 | Facial Image | Yes |
| 0x005FC109 | Printed | Yes |
| 0x005FC10A | Signature (cert) | No |
| 0x005FC10B | Key Mgmt (cert) | No |
| 0x005FC10C | Key History | No |
| 0x005FC10D - 0x005FC120 | Retired (certs) | No |
| 0x005FC121 | Iris | Yes |
| 0x005FC122 | SM Signer (cert) | No |
| 0x005FC123 | PC Ref Data | No |

This next table lists the numbers Yubico defines as DataTags (see also the
[table of Yubico tags](commands.md#getvendordatatable) in the article on PIV commands).

#### Table 1B: Yubico-defined DataTags
|  Number  |  Name  |  PIN required for read  |
| :------: | :----: | :---------------------: |
| 0x005FFF00 | Admin Data | No |
| 0x005FFF01 | Attestation Cert | No |
| 0x005FFF10 | MSCMAP | No |
| 0x005FFF11 - 0x005FFF15 | MSROOTS | No |

Finally, these are the numbers a YubiKey will accept as a DataTag, but currently have no
specific meaning or data assigned to them. None of them require PIN verification in order
to read the contents.

#### Table 1C: Undefined DataTags
|  Number range  |  Count  |  PIN required for read  |
| :------------: | :-----: | :---------------------: |
| 0x005F0000 - 0x005FC100 | over 6 million possible numbers | No |
| 0x005FC124 - 0x005FFEFF | over 6 million possible numbers | No |
| 0x005FFF02 - 0x005FFF0F | 14 numbers | No |
| 0x005FFF16 - 0x005FFFFF | 223 numbers | No |

It is possible for an application to store whatever information it wants on a YubiKey
under an undefined DataTag. However, there are space limitations. It is possible to store
at most approximately 3,052 bytes under any single undefined DataTag, and the total space
on a YubiKey for all storage is about 51,000 bytes.

## The Data

Associated with each DataTag is a specified set of elements that make up the data, along
with a definition of its encoding. The encoding is a TLV structure. TLV stands for
"tag-length-value". So there is a DataTag for the data itself, specifying where, in the
YubiKey, the object will be stored. Then there are tags used to encode the data itself. 

The YubiKey itself will enforce only one part of the encoding, the initial tag and length.
Most elements are encoded as

```
  53 length
     something
```

There are two exceptions: Discovery and BITGT, see the
[entry on commands](commands.md#getdatatable).

The YubiKey verifies that the data for a data object sent in has the leading `53` tag (or
the two exceptions) with a correct length, but other than that, it does not check the
encoding. However, the SDK itself makes sure any input data follows the defined encoding.
For example, if you want to store CHUID data in the CHUID storage area, the SDK can
encode it for you if you use the
[CHUID class](xref:Yubico.YubiKey.Piv.Objects.CardholderUniqueId). But if you use the
[GetDataCommand](xref:Yubico.YubiKey.Piv.Commands.GetDataCommand), you must make sure the
data is properly encoded. If you want to store some other data in the CHUID area, not
encoded as defined, you will have to use a different tool.

The encoding definitions are specified in the
[table of PIV tags](commands.md#getdatatable).

## Reading and writing data objects

In the SDK, there are two ways to read data into and write data out of these storage
locations:

- [PivSession.Read](xref:Yubico.YubiKey.Piv.PivSession.ReadObject%2A) and
  [PivSession.Write](xref:Yubico.YubiKey.Piv.PivSession.WriteObject%2A)
- [GET DATA](commands.md#get-data) and [PUT DATA](commands.md#put-data)

## `PivSession ReadObject` and `WriteObject`

These methods require a subclass of
[PivDataObject](xref:Yubico.YubiKey.Piv.Objects.PivDataObject). Each subclass is a
representation of a data object. It will know what data it holds and how to Encode and
Decode it.

For example, the [CHUID class](xref:Yubico.YubiKey.Piv.Objects.CardholderUniqueId)
represents the PIV standard's Cardholder Unique ID. It contains properties for each
element of a CHUID:

- FASC Number
- GUID
- Expiration Date

It knows how to encode these three elements into a single byte array following the PIV
standard, and how to decode a CHUID encoding into the three elements.

The `Write` method will be able to take the data out of the `PivDataObject` it is given
and store it in the appropriate location on the YubiKey. The `Read` method will be able to
retrieve the requested data from the YubiKey and return the appropriate `PivDataObject`
object containing that data. For example,

```csharp
    using (var pivSession = new PivSession(yubiKey))
    {
        var collectorObj = new SomeKeyCollector();
        pivSession.KeyCollector = collectorObj.KeyCollectorDelegate;

        KeyHistory history = pivSession.ReadObject<KeyHistory>();
        if (history.IsEmpty)
        {
            // There was no KeyHistory data on the YubiKey
            // Code to handle this case here.
        }

        DisplayResults(
            history.OnCardCertificates, history.OffCardCertificates, history.OffCardCertificateUrl);
```
```csharp
    using (var pivSession = new PivSession(yubiKey))
    {
        var collectorObj = new SomeKeyCollector();
        pivSession.KeyCollector = collectorObj.KeyCollectorDelegate;

        // Build a KeyHistory to store.
        var history = new KeyHistory();
        history.OnCardCertificates = 1;
        history.OffCardCertificates = 2;
        history.OffCardCertificateUrl = new Uri("file://user/certs");

        pivSession.WriteObject(history);
    }
```

Currently there are `PivDataObjects` for the following DataTags:

- [CHUID](xref:Yubico.YubiKey.Piv.Objects.CardholderUniqueId)
- [CCC](xref:Yubico.YubiKey.Piv.Objects.CardCapabilityContainer)
- [Key History](xref:Yubico.YubiKey.Piv.Objects.KeyHistory)
- [Admin Data](xref:Yubico.YubiKey.Piv.Objects.AdminData)
- [Pin-Protected Data](xref:Yubico.YubiKey.Piv.Objects.PinProtectedData)
  (a special case, see below)

If you need to store/retrieve other elements, use `GET DATA` and `PUT DATA`. Yubico will
add more Data Objects based on customer demand.

## The data stored and `IsEmpty`

Lets look at Key History as an example.

The [KeyHistory](xref:UsersManualPivCommands#encoded-key-history) data object is specified
by the PIV standard to contain three things:

- number of on-card certificates
- number of off-card certificates
- URL where the off-card certificates can be found

The [KeyHistory](xref:Yubico.YubiKey.Piv.Objects.KeyHistory) class contains properties for
each of these elements.

Suppose you call

```csharp
    using KeyHistory keyHistory = pivSession.ReadObject<KeyHistory>();
```

Upon return, look at the
[IsEmpty](xref:Yubico.YubiKey.Piv.Objects.PivDataObject.IsEmpty%2a) property. If it is
`true`, then there was no `KeyHistory` data on the YubiKey. It also means that the data at
the other properties (`OnCardCertificates`, etc.) is meaningless. Sure, if you access the
`OnCardCertificates`, it will say zero. But because the object is empty, that value is not
necessarily accurate.

If `IsEmpty` is `false`, then the Read operation was able to find Key History data on the
YubiKey, decode it, and set the new `KeyHistory` object with the data it found.

Look at the properties, this is what had been written to the YubiKey. Maybe the
`OnCardCertificates` is `2`. But it can also be zero. But now because we know that there
was indeed data on the YubiKey in the Key History storage area, we know that number
reflects what was stored there.

There is also a property

```
    public Uri? OffCardCertificateUrl { get; set; }

    // Note that the `Uri` class is a reference type, so `Uri?` means it is a
    // "nullable reference type". It is NOT `Nullable<Uri>`. That is only
    // possible with value types, such as `ReadOnlyMemory<T>`.
```

Check to see if it is null. If so, then there was no URL. The standard specifies that it
is possible the Key History data has no URL.

```
    if (!(keyHistory.OffcardCertificateUrl is null))
    {
        ProcessUrl(keyHistory.OffcardCertificateUrl);
    }
```

The data in the storage location is simply what some application has set it to. It is not
placed there by the YubiKey. For example, suppose there are four PIV key slots on a
YubiKey that have both keys and certificates. The YubiKey itself will not set the Key
History data object. If you read the Key History, it will be empty.

Now suppose an application sets the Key History, and says there are two
`OnCardCertificates`. The YubiKey is not going to check the input against the contents of
the slots. Even though there are four certificates on the card, the Key History will be
set to two.

### Writing data

When you create a new instance of a `PivDataObject`, it starts out as empty, `IsEmpty` is
`true`. The contents of the other properties are meaningless, although they might start
out as zero or null. Some properties are fixed (e.g. see the CHUID FASC number) so their
initial value is correct, even if an object is empty.

If you tried to encode or Write this object
(see [Encode](xref:Yubico.YubiKey.Piv.Objects.PivDataObject.Encode) and
[Write](xref:Yubico.YubiKey.Piv.PivSession.WriteObject%2A)), you would get an exception.

When you set one of the properties, the object is no longer empty. For example,

```csharp
    using var adminData = new AdminData();
    // At this point, adminData.IsEmpty is true.

    adminData.PinProtected = true;
    // At this point, adminData.IsEmpty is false.
```

Now you can Encode or Write. Because you have not set the `Salt` nor the `PinLastUpdated`,
the encoding won't include those elements.

```text
    The encoded ADMIN DATA is
    53 length
       80 length
          81 01
             --optional bit field--
          82 length
             --optional salt--
          83 length
             --optional PinLastUpdated time

    Hence, the encoding with a bit field but
    no salt and no time value is the following.

    53 05
       80 03
          81 01
             02
```

It is possible to set a property to "no contents" and the object will not be empty. For
example,

```csharp
    using var pinProtected = new PinProtectedData();
    // IsEmpty is true;

    pinProtected.ManagementKey = null;
    // IsEmpty is now false, the object is not empty, even though
    // we set it to contain no management key.

    byte[] encoding = pinProtected.Encode();
    // This will produce an output with no data
    //   53 04
    //      88 02
    //         89 00
```

## Using an alternate DataTag

It is possible to store data specified by a sppecific DataTag under an alternate numer.
That is, there are specific DataTags defined for specific data constructions. For example,
there is a DataTag for CHUID (`0x005FC102`), and specific data formatted following a
specific TLV construction. However, if you want to store CHUID data under an alternate
DataTag (it will still be the CHUID data formatted following the CHUID definition), you
can set the DataTag.

See the [DataTag](xref:Yubico.YubiKey.Piv.Objects.PivDataObject.DataTag%2a) property.

You will likely never have a use case in your application for an alternate DataTag, but
this feature is available for those rare cases when it can be useful. For example, someone
might want to use a specific CHUID for one application, and a different CHUID for a second
application. Hence, there could be two CHUIDs stored on a single YubiKey, one under the
CHUID DataTag and one under an alternate tag.

Note that it can be dangerous to store data under an alternate DataTag, because some tags
require the PIN to read and others do not. For example, if you store some sensitive data
in the PRINTED storage area, PIN verification is required to retrieve it. But suppose you
store that data under an alternate tag, one that is currently undefined (such as
`0x005F0010`).  That storage area does not require the PIN to retrieve the data.

The tables above include a column indicating whether a DataTag requires the PIN for
reading or not.

The SDK makes it easy to store data under a different DataTag, as long as there is a
[PivDataObject](xref:Yubico.YubiKey.Piv.Objects.PivDataObject) class for the tag. For
example, there is a class
[CardholderUniqueId](xref:Yubico.YubiKey.Piv.Objects.CardholderUniqueId) for the CHUID
DataTag. In this case, to store the CHUID data under a different number, set the
[DataTag](xref:Yubico.YubiKey.Piv.Objects.PivDataObject.DataTag%2a) property.

It is not possible to change the <c>DataTag</c> to just any integer value. The new tag
must be a number that is among the set of undefined tags.

If you change the `DataTag`, then the data specified in the object, including its format,
will be stored under a different tag. For example, if you build a
[CardholderUniqueId](xref:Yubico.YubiKey.Piv.Objects.CardholderUniqueId) object and leave
the `DataTag` alone, then when you store the data it will be stored in the YubiKey's CHUID
storage area. But if you build the object and then change the `DataTag` to, say,
`0x005F0010` (one of the undefined numbers), when you store the data, it will be the CHUID
data formatted according to the PIV specification for CHUID, but stored in the
`0x005F0010` storage area.

## `PinProtectedData`

This is an unusual `PivDataObject` because there is no specified Data Object called
"PIN-Protected Data". It is used to store a specific set ot elements in the PRINTED
storage area. Currently, only the YubiKey's management key is included in the set.

It would be possible to create a `PivDataObject` for PRINTED, just as there are classes
for CHUID, CCC, and so on. There is none, however, because the PRINTED storage area is
really designed for "credit-card-like" smart cards, storing the information printed on the
card itself (and other data). But the YubiKey is not such a smart card, so there is no
such printed information and no need to use the PRINTED storage area.

Because this is an "unused" Data Object, Yubico uses it to store the management key if the
customer wants a "PIN-only" YubiKey. If, in the future, Yubico decides to store more
PIN-protected data, this will be extended (see below).

### PIN-only

There are many PIV operations that require management key authentication in order to
execute. For example, a YubiKey will not generate a new private key unless the
management key has been authenticated in the current session.

In order to authenticate, the management key must be entered. But that might not be an
easy operation. The PIV PIN will almost certainly be "keyboard characters" and is at
most eight characters long. It is easy for an application to pop up a window to enter
the PIN, and it is not too hard for a user to remember an 8-character value.

But the management key is 24 binary bytes. How does a user enter binary data? And very few
people could remember such a long value.

Some applications prefer to configure the YubiKey to PIN-only. There are two ways to do
that on a YubiKey: PIN-derived and PIN-protected.

> [!WARNING]
> PIN-derived should never be used and is provided only for backwards compatibility.

PIN-protected simply stores the management key in the PRINTED storage area and retrieves
it whenever it is needed. The YubiKey will not return the data inside PRINTED unless the
PIN has been verified in the current session. In this way, the management key is
PIN-protected.

In order to authenticate the management key, verify the PIN, retrieve the data from the
PRINTED storage area, decode, and use the resulting 24 bytes to authenticate.

Note that there are `PivSession` methods that will do all this work for you. Most
applications will never use the `PinProtectedData` class directly.

### Encoding

The PIV standard specifies the encoding format of the data stored in PRINTED. When storing
the management key, however, another format is used. In this way, it is possible to know
whether the data in the Data Object is PRINTED or PIN-protected management key. If the
data in the storage area looks like the following

```text
    53 length
       01 length
          --data--
       02 length
          --data--
       03 length
          --data--
       04 length
          --data--
       05 length
          --data--
       06 length
          --data--
       07 length
          --data--
       08 length
          --data--
       FE 00
```

then this is PRINTED data. If, however, it is encoded as the following

```text
    53 1C
       88 1A
          89 18
             --management key--
```

then it is the PIN-protected management key.

### Using `PinProtectedData`

To store something using the `PinProtectedData` class create an instance, load the data
into the appropriate property and call `PivSession.WriteObject`.

To read the PIN-protected data out of a YubiKey, call the `PivSession.ReadObject` method.
The result is an object. Look at the properties you are interested in to see any data
retrieved.

```csharp
    using var pinProtect = new PinProtectedData();

    pinProtect.ManagementKey = mgmtKeyData;
    pivSession.WriteObject(pinProtect);


    PinProtectedData getPinProtect = pivSession.ReadObject<PibnProtectedData>();
    if (!(getPinProtect.ManagementKey is null))
    {
        // process mgmt key.
    }
```

### Future extensions

For now, the only thing that can be PIN-protected using this construction is the
management key. Hence, there is a property in the `PinProtectedData` class called
`ManagementKey`. In the future, however, if Yubico decides to store some other data in the
PRINTED storage area, this class will be updated with other properties.
