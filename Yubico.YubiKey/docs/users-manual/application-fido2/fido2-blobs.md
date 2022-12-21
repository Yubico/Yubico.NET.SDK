---
uid: Fido2Blobs
---

<!-- Copyright 2022 Yubico AB

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License. -->

# FIDO2 Blobs

In computer science, a "blob" is a "Binary Large OBject". It is generally used to describe
data stored in a database, and is often multimedia files (sound, video, etc.).

In FIDO2, a "blob" is arbitrary data. Furthermore, there are two kinds of blobs:

* credential
* large

## Credential Blobs

A credential blob ("credBlob" in the extensions) is a small amount of data stored with a
credential. That is, if an authenticator supports the "credBlob" extension, when making a
credential it is possible to provide whatever information you want and it will be stored
with that newly-made credential. Later on, it is possible to retrieve that data when
getting an assertion for the credential. That is, the assertion is returned along with the
"credBlob".

The standard specifies that if an authenticator allows "credBlobs", it must be able to
store, for each credential, at least 32 bytes. The standard also allows authenticators to
store more. See the
[AuthenticatorInfo.MaximumCredentialBlobLength](xref:Yubico.YubiKey.Fido2.AuthenticatorInfo)
property to determine how many bytes can be stored on any specific YubiKey.

[This article](cred-blobs.md) describes how to store and retrieve information using the
"credBlob" extension.

## Large Blobs

A large blob is a larger amount of arbitrary data. The standard specifies that an
authenticator that supports large blobs must support at least 1024 bytes. However, some of
those bytes are "overhead", which the standard estimates to be 64, so that the actual
amount of data stored will be around <c>maxSerializedLargeBlobArray</c> - 64 (e.g., if
the maximum large blob size is 1024, the total number of bytes that can be stored will
be about 960).

This total number of bytes is for the entire FIDO2 application, not per credential. For
example, if a YubiKey can hold 25 credentials, and you want to store some data with each
credential, you will have about 38 bytes per credential.

[This article](large-blobs.md) describes how to store and retrieve information using the
"largeBlobs" option.
