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

using System.Buffers.Binary;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Utilities;
using Yubico.YubiKit.Piv.Backends;
using Yubico.YubiKit.Piv.Certificates;

namespace Yubico.YubiKit.Piv.UnitTests.Certificates;

/// <summary>
/// Exact-value coverage for the <c>Slot</c> to PIV certificate data-object-id mapping in
/// <see cref="CertificateProtocol.GetCertificateObjectId(Slot)"/>. Each mapped slot arm
/// asserts the specific <see cref="DataObject"/> constant it must resolve to, so that changing
/// any single arm's mapping fails a test.
/// </summary>
public class PivCertificateObjectIdTests
{
    [Theory]
    [InlineData(Slot.Authentication, DataObject.Authentication)]
    [InlineData(Slot.Signature, DataObject.Signature)]
    [InlineData(Slot.KeyManagement, DataObject.KeyManagement)]
    [InlineData(Slot.CardAuthentication, DataObject.CardAuthentication)]
    [InlineData(Slot.Attestation, DataObject.Attestation)]
    [InlineData(Slot.Retired1, DataObject.Retired1)]
    [InlineData(Slot.Retired2, DataObject.Retired2)]
    [InlineData(Slot.Retired3, DataObject.Retired3)]
    [InlineData(Slot.Retired4, DataObject.Retired4)]
    [InlineData(Slot.Retired5, DataObject.Retired5)]
    [InlineData(Slot.Retired6, DataObject.Retired6)]
    [InlineData(Slot.Retired7, DataObject.Retired7)]
    [InlineData(Slot.Retired8, DataObject.Retired8)]
    [InlineData(Slot.Retired9, DataObject.Retired9)]
    [InlineData(Slot.Retired10, DataObject.Retired10)]
    [InlineData(Slot.Retired11, DataObject.Retired11)]
    [InlineData(Slot.Retired12, DataObject.Retired12)]
    [InlineData(Slot.Retired13, DataObject.Retired13)]
    [InlineData(Slot.Retired14, DataObject.Retired14)]
    [InlineData(Slot.Retired15, DataObject.Retired15)]
    [InlineData(Slot.Retired16, DataObject.Retired16)]
    [InlineData(Slot.Retired17, DataObject.Retired17)]
    [InlineData(Slot.Retired18, DataObject.Retired18)]
    [InlineData(Slot.Retired19, DataObject.Retired19)]
    [InlineData(Slot.Retired20, DataObject.Retired20)]
    public void GetCertificateObjectId_MappedSlot_ReturnsExpectedObjectId(Slot slot, int expectedObjectId)
    {
        int actual = CertificateProtocol.GetCertificateObjectId(slot);

        Assert.Equal(expectedObjectId, actual);
    }

    [Fact]
    public void GetCertificateObjectId_UnmappedSlot_ThrowsArgumentExceptionWithSlotSpecificMessage()
    {
        var slot = (Slot)0x00;

        var exception = Assert.Throws<ArgumentException>(() => CertificateProtocol.GetCertificateObjectId(slot));

        Assert.Equal("slot", exception.ParamName);
        Assert.StartsWith("Slot 0x00 does not support certificates", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("7100")]
    [InlineData("710100")]
    [InlineData("710102")]
    public async Task GetCertificateAsync_AdmitsDerOnlyForUncompressedMode(string certInfo)
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest("CN=PIV unit test", key, HashAlgorithmName.SHA256);
        using var expected = request.CreateSelfSigned(DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddDays(1));
        var backend = new CertificateBackend(expected.RawData, Convert.FromHexString(certInfo));
        var token = TestContext.Current.CancellationToken;

        if (certInfo == "710102")
        {
            await Assert.ThrowsAsync<NotSupportedException>(() =>
                CertificateProtocol.GetCertificateAsync(backend, Slot.Authentication, token));
            return;
        }

        using var actual = await CertificateProtocol.GetCertificateAsync(backend, Slot.Authentication, token);
        Assert.NotNull(actual);
        Assert.Equal(expected.RawData, actual.RawData);
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 0)]
    [InlineData(true, 1)]
    public async Task GetCertificateAsync_ReadsCompressedDerAndChecksNetIdLength(bool netId, int lengthDelta)
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest("CN=PIV unit test", key, HashAlgorithmName.SHA256);
        using var expected = request.CreateSelfSigned(DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddDays(1));
        var backend = new CertificateBackend(Compress(expected.RawData, netId, lengthDelta), [0x71, 0x01, 0x01]);
        var token = TestContext.Current.CancellationToken;

        if (lengthDelta != 0)
        {
            await Assert.ThrowsAsync<InvalidDataException>(() =>
                CertificateProtocol.GetCertificateAsync(backend, Slot.Authentication, token));
            return;
        }

        using var actual = await CertificateProtocol.GetCertificateAsync(backend, Slot.Authentication, token);
        Assert.NotNull(actual);
        Assert.Equal(expected.RawData, actual.RawData);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetCertificateAsync_StopsHighExpansionBeforeLoadingCertificate(bool netId)
    {
        var backend = new CertificateBackend(Compress(new byte[64 * 1024 + 1], netId), [0x71, 0x01, 0x01]);

        var failure = await Assert.ThrowsAsync<InvalidDataException>(() =>
            CertificateProtocol.GetCertificateAsync(backend, Slot.Authentication, TestContext.Current.CancellationToken));

        Assert.Contains("64 KiB", failure.Message);
    }

    private static byte[] Compress(byte[] input, bool netId, int lengthDelta = 0)
    {
        using var output = new MemoryStream();
        if (netId)
        {
            Span<byte> header = [0x01, 0x00, 0x00, 0x00];
            BinaryPrimitives.WriteUInt16LittleEndian(header[2..], unchecked((ushort)(input.Length + lengthDelta)));
            output.Write(header);
        }

        using (Stream encoder = netId
            ? new ZLibStream(output, CompressionMode.Compress, leaveOpen: true)
            : new GZipStream(output, CompressionMode.Compress, leaveOpen: true))
        {
            encoder.Write(input);
        }

        return output.ToArray();
    }

    private sealed class CertificateBackend(byte[] certificate, byte[] certInfo) : IBackend
    {
        public Task<Initialization> ReadInitialStateAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<ApduResponse> SendAsync(
            ApduCommand command,
            bool throwOnError = true,
            CancellationToken cancellationToken = default)
        {
            Assert.Equal(0xCB, command.Ins);
            byte[] length = new byte[BerLength.EncodingSize(certificate.Length)];
            BerLength.Write(length, certificate.Length);
            return Task.FromResult(new ApduResponse([0x70, .. length, .. certificate, .. certInfo, 0xFE, 0x00], unchecked((short)0x9000)));
        }
    }
}
