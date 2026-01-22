// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.IO.Compression;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.SmartCard;

namespace Yubico.YubiKit.Piv;

public sealed partial class PivSession
{
    /// <summary>
    /// Gets the certificate stored in the specified slot.
    /// </summary>
    /// <param name="slot">The slot to read from.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The certificate, or null if the slot is empty.</returns>
    public async Task<X509Certificate2?> GetCertificateAsync(
        PivSlot slot,
        CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("PIV: Getting certificate from slot 0x{Slot:X2}", (byte)slot);

        if (_protocol is null)
        {
            throw new InvalidOperationException("Session not initialized");
        }

        // Get the object ID for this slot
        int objectId = GetCertificateObjectId(slot);

        // Read the data object
        var certData = await GetObjectAsync(objectId, cancellationToken).ConfigureAwait(false);
        
        if (certData.IsEmpty)
        {
            return null;
        }

        // Parse TLV: TAG 0x53 [ TAG 0x70 (cert) + TAG 0x71 (info) + TAG 0xFE (LRC) ]
        var span = certData.Span;
        
        // Expect TAG 0x53 (Data)
        if (span.Length < 4 || span[0] != 0x53)
        {
            Logger.LogWarning("PIV: Invalid certificate format in slot 0x{Slot:X2}", (byte)slot);
            return null;
        }

        // Parse outer 0x53 tag length
        int offset = 1;
        int outerLength = ParseTlvLength(span, ref offset);
        if (outerLength < 0)
        {
            Logger.LogWarning("PIV: Invalid TLV length in slot 0x{Slot:X2}", (byte)slot);
            return null;
        }

        byte[]? certBytes = null;
        bool isCompressed = false;

        while (offset < span.Length - 2) // Need at least tag + length
        {
            byte tag = span[offset++];
            if (offset >= span.Length) break;
            
            int length = ParseTlvLength(span, ref offset);
            if (length < 0) break;
            
            if (tag == 0x70) // Certificate
            {
                if (offset + length <= span.Length)
                {
                    certBytes = span.Slice(offset, length).ToArray();
                }
                offset += length;
            }
            else if (tag == 0x71) // Certificate info (compression flag)
            {
                if (length > 0 && offset < span.Length && span[offset] == 0x01)
                {
                    isCompressed = true;
                }
                offset += length;
            }
            else if (tag == 0xFE) // LRC
            {
                break;
            }
            else
            {
                offset += length;
            }
        }

        if (certBytes == null || certBytes.Length == 0)
        {
            return null;
        }

        // Decompress if needed
        if (isCompressed)
        {
            using var input = new MemoryStream(certBytes);
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            await gzip.CopyToAsync(output, cancellationToken).ConfigureAwait(false);
            certBytes = output.ToArray();
        }

#pragma warning disable SYSLIB0057 // X509Certificate2(byte[]) is obsolete
        return new X509Certificate2(certBytes);
#pragma warning restore SYSLIB0057
    }

    /// <summary>
    /// Stores a certificate in the specified slot.
    /// </summary>
    /// <param name="slot">The slot to write to.</param>
    /// <param name="certificate">The certificate to store.</param>
    /// <param name="compress">Whether to compress the certificate (default: auto-compress if > 1856 bytes).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    public async Task StoreCertificateAsync(
        PivSlot slot,
        X509Certificate2 certificate,
        bool compress = false,
        CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("PIV: Storing certificate in slot 0x{Slot:X2}", (byte)slot);

        if (_protocol is null)
        {
            throw new InvalidOperationException("Session not initialized");
        }

        if (!_isAuthenticated)
        {
            throw new InvalidOperationException("Management key authentication required to store certificates");
        }

        var certBytes = certificate.RawData;
        bool shouldCompress = compress || certBytes.Length > 1856;

        if (shouldCompress)
        {
            using var output = new MemoryStream();
            using (var gzip = new GZipStream(output, CompressionLevel.Optimal))
            {
                await gzip.WriteAsync(certBytes, cancellationToken).ConfigureAwait(false);
            }
            certBytes = output.ToArray();
        }

        // Build TLV: TAG 0x70 (cert) + TAG 0x71 (info) + TAG 0xFE (LRC)
        // Note: PutObjectAsync adds the outer 0x53 wrapper
        var dataList = new List<byte>();

        // TAG 0x70 (Certificate)
        dataList.Add(0x70);
        if (certBytes.Length > 127)
        {
            dataList.Add(0x82); // 2-byte length
            dataList.Add((byte)(certBytes.Length >> 8));
            dataList.Add((byte)(certBytes.Length & 0xFF));
        }
        else
        {
            dataList.Add((byte)certBytes.Length);
        }
        dataList.AddRange(certBytes);

        // TAG 0x71 (Certificate info)
        dataList.Add(0x71);
        dataList.Add(0x01);
        dataList.Add((byte)(shouldCompress ? 0x01 : 0x00));

        // TAG 0xFE (LRC - error detection)
        dataList.Add(0xFE);
        dataList.Add(0x00);

        // Write to object
        int objectId = GetCertificateObjectId(slot);
        await PutObjectAsync(objectId, dataList.ToArray(), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes the certificate from the specified slot.
    /// </summary>
    public async Task DeleteCertificateAsync(
        PivSlot slot,
        CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("PIV: Deleting certificate from slot 0x{Slot:X2}", (byte)slot);

        if (_protocol is null)
        {
            throw new InvalidOperationException("Session not initialized");
        }

        if (!_isAuthenticated)
        {
            throw new InvalidOperationException("Management key authentication required to delete certificates");
        }

        int objectId = GetCertificateObjectId(slot);
        await PutObjectAsync(objectId, null, cancellationToken).ConfigureAwait(false);
    }

    private int GetCertificateObjectId(PivSlot slot)
    {
        return slot switch
        {
            PivSlot.Authentication => PivDataObject.Authentication,
            PivSlot.Signature => PivDataObject.Signature,
            PivSlot.KeyManagement => PivDataObject.KeyManagement,
            PivSlot.CardAuthentication => PivDataObject.CardAuthentication,
            PivSlot.Attestation => PivDataObject.Attestation,
            PivSlot.Retired1 => PivDataObject.Retired1,
            PivSlot.Retired2 => PivDataObject.Retired2,
            PivSlot.Retired3 => PivDataObject.Retired3,
            PivSlot.Retired4 => PivDataObject.Retired4,
            PivSlot.Retired5 => PivDataObject.Retired5,
            PivSlot.Retired6 => PivDataObject.Retired6,
            PivSlot.Retired7 => PivDataObject.Retired7,
            PivSlot.Retired8 => PivDataObject.Retired8,
            PivSlot.Retired9 => PivDataObject.Retired9,
            PivSlot.Retired10 => PivDataObject.Retired10,
            PivSlot.Retired11 => PivDataObject.Retired11,
            PivSlot.Retired12 => PivDataObject.Retired12,
            PivSlot.Retired13 => PivDataObject.Retired13,
            PivSlot.Retired14 => PivDataObject.Retired14,
            PivSlot.Retired15 => PivDataObject.Retired15,
            PivSlot.Retired16 => PivDataObject.Retired16,
            PivSlot.Retired17 => PivDataObject.Retired17,
            PivSlot.Retired18 => PivDataObject.Retired18,
            PivSlot.Retired19 => PivDataObject.Retired19,
            PivSlot.Retired20 => PivDataObject.Retired20,
            _ => throw new ArgumentException($"Slot 0x{(byte)slot:X2} does not support certificates", nameof(slot))
        };
    }
    
    /// <summary>
    /// Parses a TLV length field and advances the offset.
    /// </summary>
    private static int ParseTlvLength(ReadOnlySpan<byte> data, ref int offset)
    {
        if (offset >= data.Length) return -1;
        
        byte firstByte = data[offset++];
        
        // Short form: length is 0-127
        if (firstByte <= 0x7F)
        {
            return firstByte;
        }
        
        // Long form: first byte indicates number of length bytes
        int numLengthBytes = firstByte & 0x7F;
        
        if (numLengthBytes == 0 || numLengthBytes > 3 || offset + numLengthBytes > data.Length)
        {
            return -1;
        }
        
        int length = 0;
        for (int i = 0; i < numLengthBytes; i++)
        {
            length = (length << 8) | data[offset++];
        }
        
        return length;
    }
}
