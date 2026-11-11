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
    private const byte TagCertificate = 0x70;
    private const byte TagCertificateInfo = 0x71;
    private const byte TagLrc = 0xFE;
    private const byte CompressedCertificate = 0x01;
    private const byte UncompressedCertificate = 0x00;
    private const int MaximumUncompressedCertificateLength = 1856;
    private const int CertificateInfoTlvLength = 3;
    private const int LrcTlvLength = 2;

    private static ILogger Logger => YubiKitLogging.CreateLogger(nameof(CertificateProtocol));

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

        byte[]? certBytes = tlvDict.TryGetValue(TagCertificate, out var cert)
            ? cert.ToArray()
            : null;

        bool isCompressed = tlvDict.TryGetValue(TagCertificateInfo, out var info)
            && info.Length > 0
            && info.Span[0] == CompressedCertificate;

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
        bool shouldCompress = compression == CertificateCompression.Always || certBytes.Length > MaximumUncompressedCertificateLength;

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
        int totalSize = 1 + certLenSize + certBytes.Length + CertificateInfoTlvLength + LrcTlvLength; // 0x70 + len + cert + 0x71 01 xx + 0xFE 00
        var writer = new ArrayBufferWriter<byte>(totalSize);

        ReadOnlySpan<byte> certTag = [TagCertificate];
        writer.Write(certTag);
        var lenSpan = writer.GetSpan(certLenSize);
        BerLength.Write(lenSpan, certBytes.Length);
        writer.Advance(certLenSize);
        writer.Write(certBytes);

        ReadOnlySpan<byte> infoTlv = [TagCertificateInfo, 1, shouldCompress ? CompressedCertificate : UncompressedCertificate];
        writer.Write(infoTlv);

        ReadOnlySpan<byte> lrcTlv = [TagLrc, 0];
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
        // Retired key references and certificate object identifiers are parallel contiguous ranges.
        if (slot is >= Slot.Retired1 and <= Slot.Retired20)
            return DataObject.Retired1 + ((byte)slot - (byte)Slot.Retired1);
        return slot switch
        {
            Slot.Authentication => DataObject.Authentication,
            Slot.Signature => DataObject.Signature,
            Slot.KeyManagement => DataObject.KeyManagement,
            Slot.CardAuthentication => DataObject.CardAuthentication,
            Slot.Attestation => DataObject.Attestation,
            _ => throw new ArgumentException($"Slot 0x{(byte)slot:X2} does not support certificates", nameof(slot))
        };
    }
}