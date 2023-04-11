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

When you build a [Fido2Session](xref:Yubico.YubiKey.Fido2.Fido2Session) object, check the
[AuthenticatorInfo](xref:Yubico.YubiKey.Fido2.AuthenticatorInfo) to see if "largeBlobs" is
supported.

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
[SetSerializedLargeBlobArray](xref:Yubico.YubiKey.Fido2.Fido2Session.SetSerializedLargeBlobArray%2a) and
[GetSerializedLargeBlobArray](xref:Yubico.YubiKey.Fido2.Fido2Session.GetSerializedLargeBlobArray%2a)
methods in the [Fido2Session](xref:Yubico.YubiKey.Fido2.Fido2Session) class.

## Why the standard specifies the client encode

During the FIDO2 operations, the client might want to get the large blob data. However,
there is no way of knowing in advance which client will be making this request. Will it be
Chrome on Windows? Safari on Mac? Firefox on Linux? Or some other client?

If the large blobs data is encoded in a standard way, each client will be able to read any
data stored by any other client.

If you know that there will be only one client that ever operates using your code, and
that client will be able to read un-encoded arbitrary data, then you will likely be able
to get away with it. But otherwise, a client might reject the un-encoded data, and might
even reject the authentication. This is why it is not recommended to store an un-encoded
large blob.

So why is it the client's job to encode? The standard could have specified that the client
provides whatever data it wants and the authenticator must encode it. However, that would
require the authenticator contain the code to compress/decompress, encrypt/decrypt, and
CBOR encode/decode. Because authenticators, such as the YubiKey, have very limited space
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
perform the compression and decompression if you call the
`Fido2Session.SetSerializedLargeBlobArray` and `Fido2Session.GetSerializedLargeBlobArray`
methods. If you want to know the length the compressed data will be before calling the
SDK, use the C# `System.IO.Compression.DeflateStream` class. But you must always pass the
uncompressed data to the `SerializedLargeBlobArray.AddEntry` method.

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

### Making a credential with a `LargeBlobKey`

If a YubiKey supports the large blob option, you must make a credential with the large
blob extension set to true.

```csharp
    var mcParams = new MakeCredentialParameters(relyingParty, userEntity)
    {
        ClientDataHash = clientDataHash
    };
    mcParams.AddOption(AuthenticatorOptions.rk, true);
    mcParams.AddExtension("largeBlobKey", new byte[] { 0xF5 });
    isValid = fido2Session.TryVerifyPin(pinBytes, null, null, out retries, out reboot);
    MakeCredentialData mcData = fido2Session.MakeCredential(mcParams);
```

The `MakeCredentialData` returned has a property for the
[LargeBlobKey](xref:Yubico.YubiKey.Fido2.MakeCredentialData.LargeBlobKey). If you want,
you can save that key for later on when you want to set or read any data stored against
this credential. However, you will most likely not save it, but instead get the
`LargeBlobKey` each time you need it by getting an assertion.

```csharp
    var gaParams = new GetAssertionParameters(relyingParty, clientDataHash);
    gaParams.AddExtension("largeBlobKey", new byte[] { 0xF5 });
    IReadOnlyList<GetAssertionData> assertions = fido2.GetAssertions(gaParams);
```
If a credential was made with the "largeBlobKey" extension then
[assertions[i].LargeBlobKey](xref:Yubico.YubiKey.Fido2.GetAssertionData.LargeBlobKey) will
not be null, and will contain the same large blob key that was returned by the make
credential call.

## The serialized large blob array

The standard specifies that the data store be a "serialized large blob array". This is the
concatenation of the "large blob array" and a digest.

```txt
   CBOR-encoded large blob array || left-16( SHA-256(CBOR-encoded large blob array) )
```

This means that to store large blobs, the caller must build each entry, combine the
entries into a single buffer encoded following the CBOR rules, then use SHA-256 to
digest the encoding, and store the concatenation of the two.

Most of this work is done by the SDK using the 
[SerializedLargeBlobArray](xref:Yubico.YubiKey.Fido2.SerializedLargeBlobArray) class
and the `Fido2Session.SetSerializedLargeBlobArray` method.

## The initial serialized large blob array

The standard specifies that an authenticator that supports large blobs must be
manufactured with an initial value. That value is an empty array, followed by the digest
of the empty array.

```txt
   80 76 be 8b 52 8d 00 75 f7 aa e9 8d 6f a5 7a 6d 3c
```

The `80` is the CBOR encoding of an empty array (`8x` is the CBOR tag for array of `x`
entries, so `83` is an array of three entries and `80` is an array of zero entries).
Perform SHA-256 on the single byte `80` and retain only the first (or "left") 16 bytes,
and the result is `76 BE ... 3C`.

## Getting the current large blob

```csharp
    SerializedLargeBlobArray currentLargeBlob = fido2Session.GetSerializedLargeBlobArray();
```

This returns the contents of the YubiKey's large blob, decoded into a new
`SerializedLargeBlobArray` object. You can check the `Entries` property to see how many
elements have been stored.

```csharp
    int count = currentLargeBlob.Entries.Count;
```

If the YubiKey contains only the initial large blob data, the `count` will be zero.

You can also call the
[IsDigestVerified](xref:Yubico.YubiKey.Fido2.SerializedLargeBlobArray.IsDigestVerified)
method to verify that the digest value is correct.

### Updating the current large blob, add an entry

Once you have the current large blob data, you can add, remove, or "edit" entries. To add
an entry, you need the `LargeBlobKey`.

```csharp
    currentLargeBlob.AddEntry(dataToAdd, assertion[i].LargeBlobKey);
```

At this point, the `EncodedArray` and `Digest` properties are now null. When we first
obtained the serialized large blob array, the object presented the encoded large blob
array along with the digest. Now, if we add a new entry, that old encoding and old digest
are no longer valid. If you want, you can call the `Encode` method, but there's no need.
The SDK will call it when you try to store this new large blob.

### Updating the current large blob, remove an entry

If there is an entry you no longer want stored (e.g. a credential is removed, so any large
blobs associated can be removed), call the `RemoveEntry` method.

```csharp
    currentLargeBlob.RemoveEntry(index);
```

Note that this removes the entry at the given index, so you will likely need to decrypt
entries to make sure you have the index of the one you want to remove. Note also that it
will not remove it from the YubiKey, it will only remove it from the
`SerializedLargeBlobArray` object. Once you set the YubiKey with the new object, the new
array will overwrite the old array, meaning the removed entry no longer exists on the
YubiKey.

### Updating the current large blob, "edit" an entry

If you want to keep an entry, but change the contents, you must build your new blob data
from the old data, call `AddEntry` with the new blob data (and appropriate large blob
key), and call `RemoveEntry` on the old entry's index.
