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

using Microsoft.Extensions.Logging.Abstractions;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Piv.Backend;
using Yubico.YubiKit.Piv.Cryptography;

namespace Yubico.YubiKit.Piv.UnitTests.Cryptography;

/// <summary>
/// Security regression tests: <c>ParseCryptoResponse</c> (used by both <c>SignOrDecryptAsync</c>/
/// <c>DecryptAsync</c> and <c>CalculateSecretAsync</c>) copies the needed TLV value out of the raw
/// AUTHENTICATE response but must also zero the original response buffer, since it carries the raw
/// decrypted plaintext / ECDH shared secret in the clear. Additionally, <c>DecryptAsync</c> must zero
/// its intermediate <c>rawDecrypted</c> copy once it has been re-copied into <c>rawBytes</c>.
/// </summary>
public class PivCryptographicOperationsTests
{
    [Fact]
    public void CopyAndZeroSource_CopiesValueAndZeroesSourceBuffer()
    {
        byte[] source = Enumerable.Range(1, 32).Select(i => (byte)i).ToArray();
        byte[] sourceSnapshot = (byte[])source.Clone();

        var result = PivCryptographicOperations.CopyAndZeroSource(source);

        Assert.Equal(sourceSnapshot, result);
        Assert.All(source, b => Assert.Equal(0, b));
    }

    [Fact]
    public async Task SignOrDecryptAsync_ZeroesOriginalResponseBuffer()
    {
        // Simulates an RSA decrypt / ECC sign response: 7C LL [ 82 LL2 <raw plaintext/signature> ].
        byte[] payload = Enumerable.Range(1, 32).Select(i => (byte)i).ToArray();
        byte[] inner = [0x82, (byte)payload.Length, .. payload];
        byte[] wrapped = [0x7C, (byte)inner.Length, .. inner];

        var backend = new CryptoCapturingBackend(wrapped);

        var result = await PivCryptographicOperations.SignOrDecryptAsync(
            backend,
            NullLogger.Instance,
            PivSlot.Authentication,
            PivAlgorithm.EccP256,
            new byte[32],
            TestContext.Current.CancellationToken);

        // Sanity: the returned (distinct, correctly-unwrapped) copy still has the real data.
        Assert.Equal(payload, result.ToArray());

        Assert.NotNull(backend.CapturedRawDataArray);
        int dataLength = backend.CapturedCount - 2; // exclude trailing SW bytes
        Assert.All(
            backend.CapturedRawDataArray!.AsSpan(backend.CapturedOffset, dataLength).ToArray(),
            b => Assert.Equal(0, b));
    }

    [Fact]
    public async Task CalculateSecretAsync_ZeroesOriginalResponseBuffer()
    {
        // Simulates an ECDH response: 7C LL [ 82 LL2 <raw shared secret> ].
        byte[] sharedSecret = Enumerable.Range(1, 32).Select(i => (byte)i).ToArray();
        byte[] inner = [0x82, (byte)sharedSecret.Length, .. sharedSecret];
        byte[] wrapped = [0x7C, (byte)inner.Length, .. inner];

        var backend = new CryptoCapturingBackend(wrapped);

        using var peer = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        var peerPublicKey = Yubico.YubiKit.Core.Cryptography.ECPublicKey.CreateFromParameters(peer.PublicKey.ExportParameters());

        var result = await PivCryptographicOperations.CalculateSecretAsync(
            backend,
            NullLogger.Instance,
            PivSlot.KeyManagement,
            peerPublicKey,
            TestContext.Current.CancellationToken);

        Assert.Equal(sharedSecret, result.ToArray());

        Assert.NotNull(backend.CapturedRawDataArray);
        int dataLength = backend.CapturedCount - 2;
        Assert.All(
            backend.CapturedRawDataArray!.AsSpan(backend.CapturedOffset, dataLength).ToArray(),
            b => Assert.Equal(0, b));
    }

    private sealed class CryptoCapturingBackend(byte[] responseWithoutSw) : IPivBackend
    {
        public byte[]? CapturedRawDataArray { get; private set; }

        public int CapturedOffset { get; private set; }

        public int CapturedCount { get; private set; }

        public Task<PivInitialization> InitializeAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Not needed for these tests.");

        public Task<ApduResponse> SendAsync(
            ApduCommand command,
            bool throwOnError = true,
            CancellationToken cancellationToken = default)
        {
            var response = new ApduResponse(responseWithoutSw, unchecked((short)0x9000));

            Assert.True(MemoryMarshal.TryGetArray(response.RawData, out var segment));
            CapturedRawDataArray = segment.Array;
            CapturedOffset = segment.Offset;
            CapturedCount = segment.Count;

            return Task.FromResult(response);
        }
    }
}