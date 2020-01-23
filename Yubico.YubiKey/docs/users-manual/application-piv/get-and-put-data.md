---
uid: UsersManualPivGetAndPutData
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

# PIV Get and Put Data

There is a PIV command called [GET DATA](commands.md#get-data). It is a general
purpose command that takes in a "tag" indicating what data to get, and returns a byte
array to be parsed by the caller. In this way there is one command for many different data
elements, which is more efficient than creating many commands, one for each type of data
element. There are currently 21 tags supported in the SDK, so rather than have 21
commands, with GET DATA there is one command with 21 different possible arguments.

Some of the data elements to get are available "out of the box". That is, the YubiKey is
manufactured with some data elements loaded. For example, the "Discovery" element contains
the application AID (so applications can verify they are communicating with a PIV card)
and the PIN usage policy.

Other elements are initially empty. For example, upon manufacture, there is no "Signature"
key or cert (see [Piv Slots](slots.md#table-1-list-of-piv-slots)). The caller must
generate or import a key, and obtain a certificate. So until that happens, calling GET
DATA with the tag of "Signing Cert" will return "NoData".

The [PUT DATA](commands.md#put-data) command will fill those empty elements, or it can
replace the data currently in an element.

# Vendor-Defined Get and Put Data

The PIV standard defines a set of tags for a smart card to implement in order to be
compliant. It also allows vendors to define their own tags. The YubiKey has a set of
vendor-defined tags. The SDK has the ability to get and put these elements, but the API
to do so directly is not public. That is, it is not possible for an application outside
the SDK to directly get or put data using these tags. However, there are "upper-layer"
classes that will store and retrieve the data. The calling application will have access
to these data elements, just not directly through the GET DATA and PUT DATA classes.

## Data format

The standard-defined data elements are specified with standard-defined data formats. To be
compliant with the PIV standard, a device must return the data for a supported tag in the
format described. For example, the standard specifies that the return from a GET DATA call
with the tag of "Discovery" must be the following.

```
7E 12
   4F 0B
      A0 00 00 03 08 00 00 10 00 01 00 (Application AID, fixed)
   5F 2F 02
      xx yy (PIN Usage Policy)
```

See the documentation on the [GET DATA command](commands.md#get-data) for descriptions
of every tag and the format of data.

The vendor-defined elements also have specified formats.

If you execute the PUT DATA command through the SDK, then the data must follow this
format. The YubiKey does not enforce the format of the input data based on the tag,
although it does enforce size limitations. The reason is to reduce the size of the code on
the space-constrained processor that powers a YubiKey. Because of this, it is possible to
put "arbitrary" data into many elements. That cannot be done using the SDK's public API,
but it is possible that other software packages will place non-standard data onto a
YubiKey.

When called upon to GET DATA, the YubiKey will return whatever data was loaded. If
non-specified data was put into an element, the GET DATA will return that non-specified
data.

## Overloaded elements

Because the YubiKey itself allows any input data, applications and users (including
applications built by Yubico) have "overloaded" some of the elements. There are cases of
non-specified data being loaded onto YubiKeys.

For example, Yubico overloads the "Printed" element. That element is really for smart
cards (think of a credit card and the name, number, bank, etc. printed on the card).
Because it is "unused", Yubico stores important information there. The PUT DATA command
accepts it because the YubiKey does not enforce the format, and the GET DATA simply
returns the loaded data exactly as it was put.

Note that you should never overwrite the information in the Printed tag. If you do, it
could make your YubiKey unusable.

### Recommendation

It almost goes without saying that Yubico does not recommend doing this. If you do
overload a data object and store some non-specified data on the YubiKey, the behavior of
the YubiKey itself is not defined.

If you feel there is no way to build your application without loading non-specified data
into one of the data objects, at the very least do NOT overload these elements:

```
  CHUID
  Cardholder Capability Container (CCC)
  Discovery
  Biometric Information Gropt Template (BITGT)
  Printed
    certificates:
  Authentication
  Signature
  Key Management
  Card Authentication
  Retired 1 - Retired 20
    vendor-defined:
  Attestation
  Admin Data
  MSCMAP
  MSROOTS 1 - MSROOTS 5
```

### Certificates

If you generate or load a private key into one of the private key slots (e.g. Signature or
one of the Retired key slots), you can use PUT DATA to load its accompanying certificate.
Although the YubiKey is manufactured with these data elements empty, do not consider them
"unused". You should treat them as unavailable for overload.

#### Attestation cert

The YubiKey is manufactured with an attestation key and cert. This allows you to create an
attestation statement (which is an X.509 certificate) that verifies a key was generated by
the YubiKey. Rarely will a user (or administrator) want to replace the attestation key and
cert. However, it is possible. If you do, it is imperative that you replace the
attestation key and cert at the same time.

### MSCMAP and MSROOTS

These are vendor-defined elements. These tags were created so that Yubico libraries
(minidriver, SDK) can better interface with the Microsoft Smart Card Base Crypto Service
Provider (CSP). There will likely never be a scenario where an application will need to
use the data the SDK will PUT into and GET from these objects. If your application uses
the Base CSP, and you use a YubiKey, any necessary operations with the MSCMAP will be
handled by the SDK.

## Parsing the response

When you execute GET DATA using the
[GetDataCommand](xref:Yubico.YubiKey.Piv.Commands.GetDataCommand), you get a
response object:
[GetDataResponse](xref:Yubico.YubiKey.Piv.Commands.GetDataResponse). You can now
call the `GetData` method to see the data returned.

The data returned is a byte array. It is whatever was in the element. If it follows the
standard, it will be an encoding. Each response's encoding is documented in the
[PIV Commands](commands.md#get-data) page. It will be your responsibility to parse
the encoding or extract the data you want.

The reason we do not parse it is because there are many data formats. There are other
classes that deal with parsing some of the data objects returned by a GET DATA or GET
VENDOR DATA command. Probably the most useful will be the class that can parse a byte
array containing an encoded cert, and build an `X509Certificate2` object.
