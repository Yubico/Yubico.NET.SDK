---
uid: Fido2LargeBlobs
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

# FIDO2 large blobs ("largeBlobs" option)

When you get the [AuthenticatorInfo](xref:Yubico.YubiKey.Fido2.AuthenticatorInfo), you can
check the options to see if "largeBlobs" is supported.

```C#
    using (fido2Session = new Fido2Session(yubiKeyDevice))
    {
        if (fido2Session.AuthenticatorInfo.GetOptionValue("largeBlobs") == OptionValue.True)
        {
            . . .
        }
    }
```

If it does, you can add arbitrary data. There are two possibilities for this data:

* arbitrary data not formatted or encoded
* arbitrary data encoded following the FIDO2 standard

The standard specifies a correct encoding of the large blobs data (see below). However, it
also specifies that the responsibility of making sure the data is properly encoded belongs
to the client (e.g. the browser), not the authenticator (i.e. the YubiKey). This means
that whatever data you supply, the YubiKey will accept it and store it. When you retrieve
that data, it is returned exactly how it was stored.

Hence, it is possible to store absolutely arbitrary data. With the SDK it is possible by
storing and retrieving large blobs through the
[SetLargeBlobCommand](xref:Yubico.YubiKey.Fido2.Commands.SetLargeBlobCommand) and
[GetLargeBlobCommand](xref:Yubico.YubiKey.Fido2.Commands.GetLargeBlobCommand).
However, that is not recommended, as other FIDO2 clients may be expecting well formed
data.

The SDK also offers a way to store data where the SDK performs all the formatting and
encoding/decoding through the
[SetLargeBlobArray](xref:Yubico.YubiKey.Fido2.Fido2Session.SetLargeBlobArray%2a) and
[GetCurrentLargeBlobArray](xref:Yubico.YubiKey.Fido2.Fido2Session.GetCurrentLargeBlobArray%2a)
methods in the [Fido2Session](xref:Yubico.YubiKey.Fido2.Fido2Session) class.

## Why the standard specifies the client encode

During the FIDO2 operations, the client might want to get the large blob data. However,
there is no way of knowing in advance which client will be making this request. Will it be
Chrome on Windows? Safari or Mac? Firefox on Linux? Or some other client?

If the large blobs data is encoded in a standard way, each client will be able to read any
data stored by any other client.

If you know that there will be only one client that ever operates using your code, and
that client will be able to read un-encoded arbitrary data, then you will likely be able
to get away with it. But otherwise, a client might reject the un-encoded data, and might
even reject the authentication. This is why it is dangerous to store an un-encoded large
blob.

So why is it the client's job to encode? The standard could have specified that the client
provides whatever data it wants and the authenticator must encode it. However, that would
require the authenticator contain the code to compress/decompress, encrypt/decrypt, and
Cbor encode/decode. Because authenticators, such as the YubiKey, have very limited space
and computing power, the FIDO2 standard specifies that these operations be performed by
the client which typically runs on a far more capable device.

## How much data

The standard also declares that the total number of bytes that can be stored is really
`MaximumSerializedLargeBlobArray` minus 64. The reason is that there is "overhead". There
are the encoding bytes (tags and lengths), of course, but there are also other bytes in
the blob array, including a message digest, authentication tags, and nonces.

Suppose the `MaximumSerializedLargeBlobArray` property is 1024 (the standard specifies
that the maximum allowed length is at least 1024). That means you will have space for
about 960 bytes. However, that's not entirely accurate either. The standard also specifies
that the data be compressed. It is possible you have 1,000 bytes to store, but it
compresses to 600 bytes, so it fits.

Hence, what you really have is space for 960 bytes of compressed data. The SDK will
perform the compression and decompression if you call the `Fido2Session.SetLargeBlobArray`
and `Fido2Session.GetCurrentLargeBlobArray` methods. If you want to know the length the
compressed data will be before calling the SDK, use the C#
`System.IO.Compression.DeflateStream` class. But you must always pass the uncompressed
data to the `SetLargeBlobArray` method.

## Per credential data

The standard specifies that the data to be stored is encrypted. It also specifies that the
key used to encrypt/decrypt is the
"[LargeBlobKey](xref:Yubico.YubiKey.Fido2.GetAssertionData.LargeBlobKey)". This key is
associated with a specific credential. That is, if there are no credentials on your
YubiKey, then there is no `LargeBlobKey`, you cannot encrypt any large blob data, so you
cannot store a large blob. If you have two credentials on your YubiKey, then there are two
`LargeBlobKeys`. Hence, any data you encrypt using one of the keys will be tied to that
key's credential. This also means that it is not possible to store any "general",
unencrypted data.

The standard specifies that the large blob stored is actually an array. Each element in
the array is data encrypted by one of the `LargeBlobKeys`. That is, each element in the
array is data associated with one credential.