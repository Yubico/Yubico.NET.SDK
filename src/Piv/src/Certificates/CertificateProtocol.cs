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

using Microsoft.Extensions.Logging;
using System.Buffers;
using System.IO.Compression;
using System.Security.Cryptography.X509Certificates;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Utilities;
using Yubico.YubiKit.Piv.Backends;
using Yubico.YubiKit.Piv.DataObjects;

namespace Yubico.YubiKit.Piv.Certificates;

#pragma warning disable CS1573 // Public XML docs live on PivSession/IPivSession; these are internal protocol helpers.

internal static class CertificateProtocol
{
    private static ILogger Logger => YubiKitLogging.CreateLogger("Yubico.YubiKit.Piv.Certificates.CertificateProtocol");

    /// <summary>
    /// Gets the certificate stored in the specified slot.
    /// </summary>
    /// <param name="slot">The slot to read from.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The certificate, or null if the slot is empty.</returns>
    internal static async Task<X509Certificate2?> GetCertificateAsync(
        IBackend backend,
        Slot slot,
        CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("PIV: Getting certificate from slot 0x{Slot:X2}", (byte)slot);

        // Get the object ID for this slot
        int objectId = GetCertificateObjectId(slot);

        // Read the data object
        var certData = await DataObjectProtocol.GetObjectAsync(backend, objectId, cancellationToken).ConfigureAwait(false);

        if (certData.Length == 0)
        {
            return null;
        }

        // Parse TLV: TAG 0x70 (cert) + TAG 0x71 (info) + TAG 0xFE (LRC)
        // Note: GetObjectAsync already unwraps the outer 0x53 tag
        var tlvDict = TlvHelper.DecodeDictionary(certData.AsSpan());

        byte[]? certBytes = tlvDict.TryGetValue(0x70, out var cert)
            ? cert.ToArray()
            : null;

        bool isCompressed = tlvDict.TryGetValue(0x71, out var info)
            && info.Length > 0
            && info.Span[0] == 0x01;

        if (certBytes is null || certBytes.Length == 0)
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
    /// <param name="compression">Whether to always compress or automatically compress certificates over 1856 bytes.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    internal static async Task StoreCertificateAsync(
        IBackend backend,
        Slot slot,
        X509Certificate2 certificate,
        CertificateCompression compression = CertificateCompression.Automatic,
        CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("PIV: Storing certificate in slot 0x{Slot:X2}", (byte)slot);

        var certBytes = certificate.RawData;
        bool shouldCompress = compression == CertificateCompression.Always || certBytes.Length > 1856;

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
        int certLenSize = BerLength.EncodingSize(certBytes.Length);
        int totalSize = 1 + certLenSize + certBytes.Length + 3 + 2; // 0x70 + len + cert + 0x71 01 xx + 0xFE 00
        var writer = new ArrayBufferWriter<byte>(totalSize);

        // TAG 0x70 (Certificate)
        ReadOnlySpan<byte> certTag = [0x70];
        writer.Write(certTag);
        var lenSpan = writer.GetSpan(certLenSize);
        BerLength.Write(lenSpan, certBytes.Length);
        writer.Advance(certLenSize);
        writer.Write(certBytes);

        // TAG 0x71 (Certificate info)
        ReadOnlySpan<byte> infoTlv = [0x71, 0x01, (byte)(shouldCompress ? 0x01 : 0x00)];
        writer.Write(infoTlv);

        // TAG 0xFE (LRC - error detection)
        ReadOnlySpan<byte> lrcTlv = [0xFE, 0x00];
        writer.Write(lrcTlv);

        // Write to object
        int objectId = GetCertificateObjectId(slot);
        await DataObjectProtocol.PutObjectAsync(backend, objectId, writer.WrittenMemory, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes the certificate from the specified slot.
    /// </summary>
    internal static async Task DeleteCertificateAsync(
        IBackend backend,
        Slot slot,
        CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("PIV: Deleting certificate from slot 0x{Slot:X2}", (byte)slot);

        int objectId = GetCertificateObjectId(slot);
        await DataObjectProtocol.PutObjectAsync(backend, objectId, null, cancellationToken).ConfigureAwait(false);
    }

    internal static int GetCertificateObjectId(Slot slot)
    {
        return slot switch
        {
            Slot.Authentication => DataObject.Authentication,
            Slot.Signature => DataObject.Signature,
            Slot.KeyManagement => DataObject.KeyManagement,
            Slot.CardAuthentication => DataObject.CardAuthentication,
            Slot.Attestation => DataObject.Attestation,
            Slot.Retired1 => DataObject.Retired1,
            Slot.Retired2 => DataObject.Retired2,
            Slot.Retired3 => DataObject.Retired3,
            Slot.Retired4 => DataObject.Retired4,
            Slot.Retired5 => DataObject.Retired5,
            Slot.Retired6 => DataObject.Retired6,
            Slot.Retired7 => DataObject.Retired7,
            Slot.Retired8 => DataObject.Retired8,
            Slot.Retired9 => DataObject.Retired9,
            Slot.Retired10 => DataObject.Retired10,
            Slot.Retired11 => DataObject.Retired11,
            Slot.Retired12 => DataObject.Retired12,
            Slot.Retired13 => DataObject.Retired13,
            Slot.Retired14 => DataObject.Retired14,
            Slot.Retired15 => DataObject.Retired15,
            Slot.Retired16 => DataObject.Retired16,
            Slot.Retired17 => DataObject.Retired17,
            Slot.Retired18 => DataObject.Retired18,
            Slot.Retired19 => DataObject.Retired19,
            Slot.Retired20 => DataObject.Retired20,
            _ => throw new ArgumentException($"Slot 0x{(byte)slot:X2} does not support certificates", nameof(slot))
        };
    }
}