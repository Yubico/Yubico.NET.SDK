---
uid: UsersManualPivKeepingTrack
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

# Keeping track of PIV slot contents

For details about [PIV slots, see the User's Manual entry](slots.md).

Upon manufacture, the only PIV slots that contain anything are

* 80 - PIN
* 81 - PUK
* 9B - Management Key
* F9 - Attestation

The programs that use the PIV application will fill the contents of the other slots with
keys and certificates. How does a program keep track of the slots? Which slot contains a
key? What is the algorithm and size? What is its PIN and touch policy? Was it generated or
imported? What is its associated public key?

## Prior to version 5.3

For YubiKeys with version numbers before 5.3, all this information is up to the program to
manage. That is, the program that calls on an "older" YubiKey to perform PIV operations
must keep a record somehow of which slots have keys, what algorithm (and size) each of the
keys is, and what the PIN and touch policies are.

Furthermore, for keys generated on the older YubiKey, the only way to get the public key
is during generation. That is, the YubiKey generation operation returns the public key,
however, that is the only time the older YubiKey will return that public key. There is no
function to call to retrieve a public key from a slot at any other time.

For these older YubiKeys, it is the program's responsibility to capture the public key at
generation time, store it somewhere to be accessible, and create a cert request (or
self-signed cert). It is then the program's responsibility to obtain a cert and load it
into the appropriate slot.

Of course, if there is a cert loaded into a slot, then it is possible to get that cert
out. A program can retrieve the public key out of the cert, and obtain the algorithm and
key size.

However, a cert will not specify the PIN and touch policies.

## 5.3 and later

Beginning with YubiKey version 5.3, it is possible to obtain "metadata" about a slot. For
a private key, this data includes the algorithm and key size, the public key, the PIN and
touch policies, and whether the key was generated or imported.

The generation operation will return the public key as before, but now there is a function
to call to retrieve the public key at any time.

See the documentation for the
[PivSession.GetMetadata](xref:Yubico.YubiKey.Piv.PivSession.GetMetadata%2a) method,
and the [Get Metadata](commands.md#get-metadata) command.

## Sample code

There is some sample code demonstrating how to use the SDK to perform PIV operations. Part
of that sample code is a class that keeps slot contents. That code might help you as you
design your application.
