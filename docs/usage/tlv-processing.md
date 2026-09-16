# TLV processing

`Yubico.YubiKit.Core.Utilities` provides `Tlv` and `TlvHelper` for the tag-length-value encoding used by
smart-card applets. Both own buffers, so disposal and zeroing rules matter; this page is the canonical set
of usage examples.

```csharp
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Utilities;

// DecodeList returns a disposable collection whose Tlv buffers are cleared on dispose.
using var tlvs = TlvHelper.DecodeList(responseData);
var certificateTlv = tlvs.FirstOrDefault(t => t.Tag == 0x53);

// TryFindValue returns a new buffer containing the matching value.
if (TlvHelper.TryFindValue(0x53, responseData.Span, out var certificate))
{
    // Use certificate.Span. Clear certificate when its contents are sensitive.
}

// EncodeAndDisposeList is convenient for inline Tlv objects.
Memory<byte> encodedData = TlvHelper.EncodeAndDisposeList(
    new Tlv(0x5C, new byte[] { 0x5F, 0xC1, 0x02 }),
    new Tlv(0x53, certificateData));

// EncodeList leaves disposal to the caller.
using var tagList = new Tlv(0x5C, new byte[] { 0x5F, 0xC1, 0x02 });
using var certificateValue = new Tlv(0x53, certificateData);
Memory<byte> explicitlyOwnedEncoding = TlvHelper.EncodeList([tagList, certificateValue]);
```

`Tlv` and the collection returned by `DecodeList` own buffers and must be disposed. `EncodeAndDisposeList`
disposes only the `Tlv` inputs passed to that call; `EncodeList` does not dispose its inputs. Neither method
clears source buffers supplied to `Tlv` constructors, returned encodings, or intermediate encodings used for
nesting.

For public, nonsecret nested data, encode the inner elements and use that encoding as the outer value:

```csharp
Memory<byte> publicKeyBody = TlvHelper.EncodeAndDisposeList(
    new Tlv(0x81, modulusBytes),
    new Tlv(0x82, exponentBytes));
Memory<byte> publicKeyTemplate = TlvHelper.EncodeAndDisposeList(
    new Tlv(0x7F49, publicKeyBody));
```

The calls dispose their `Tlv` inputs, but not `modulusBytes`, `exponentBytes`, `publicKeyBody`, or
`publicKeyTemplate`. These values are public, so the concise example does not clear them.

For sensitive nested data, clear every caller-owned source buffer and every returned or intermediate encoding
in a `finally` block:

```csharp
using System.Security.Cryptography;

Memory<byte> innerEncoding = Memory<byte>.Empty;
Memory<byte> outerEncoding = Memory<byte>.Empty;

try
{
    innerEncoding = TlvHelper.EncodeAndDisposeList(
        new Tlv(0x81, privateKeyBytes));
    outerEncoding = TlvHelper.EncodeAndDisposeList(
        new Tlv(0x7F49, innerEncoding));

    UseSensitiveEncoding(outerEncoding.Span);
}
finally
{
    CryptographicOperations.ZeroMemory(outerEncoding.Span);
    CryptographicOperations.ZeroMemory(innerEncoding.Span);
    CryptographicOperations.ZeroMemory(privateKeyBytes);
}
```

