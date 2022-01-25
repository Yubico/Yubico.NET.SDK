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
different for each tag. That is, a particular number will be defined as a PIV Data Tag,
and associated with it is a set of elements that are combined into a single blob of data
following a specific encoding.

## The DataTags

There are three classes of DataTag in the YubiKey:

- PIV standard-defined tags
- Yubico-defined tags
- undefined tags

On a YubiKey, any number between `0x005F0000` and `0x005FFFFF` (inclusive) can be a valid
tag. In addition, there are two numbers not in that range that are valid tags:
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
[CHUID class](xref:Yubico.YubiKey.Piv.Objects.CardholderUniqueId)). But if you use the
[GetDataCommand](xref:Yubico.YubiKey.Piv.Commands.GetDataCommand), you must make sure the
data is properly encoded. If you want to store some other data in the CHUID area, not
encoded as defined, you will have to use a different tool.

The encoding definitions are specified in the
[table of PIV tags](commands.md#getdatatable).

## Changing the DataTag

Changing the DataTag means storing the data under an alternate tag. That is, there are
specific tags defined for specific data constructions. For example, there is a tag for
CHUID (`0x005FC102`), and specific data formatted following a specific TLV construction.
However, if you want to store CHUID data under an alternate tag (it will still be the
CHUID data formatted following the CHUID definition), you can set the DataTag.

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
