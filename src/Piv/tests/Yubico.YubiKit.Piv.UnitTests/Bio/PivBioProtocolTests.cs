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
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Piv.Backend;
using Yubico.YubiKit.Piv.Bio;

namespace Yubico.YubiKit.Piv.UnitTests.Bio;

/// <summary>
/// Security regression test: <see cref="PivBioProtocol.VerifyUvAsync"/> copies the plaintext
/// temporary PIN into its returned deliverable, but the original GET DATA response buffer (the
/// device's raw APDU response, also holding the temporary PIN in the clear) must be zeroed too.
/// </summary>
public class PivBioProtocolTests
{
    [Fact]
    public async Task VerifyUvAsync_WithTemporaryPin_ZeroesOriginalResponseBuffer()
    {
        byte[] temporaryPin = Enumerable.Range(1, 16).Select(i => (byte)i).ToArray();

        var backend = new BioCapturingBackend(temporaryPin);

        var result = await PivBioProtocol.VerifyUvAsync(
            backend,
            NullLogger.Instance,
            requestTemporaryPin: true,
            checkOnly: false,
            TestContext.Current.CancellationToken);

        // Sanity: the returned (distinct) copy still has the real temporary PIN.
        Assert.NotNull(result);
        Assert.Equal(temporaryPin, result!.Value.ToArray());

        Assert.NotNull(backend.CapturedRawDataArray);
        int dataLength = backend.CapturedCount - 2; // exclude trailing SW bytes
        Assert.All(
            backend.CapturedRawDataArray!.AsSpan(backend.CapturedOffset, dataLength).ToArray(),
            b => Assert.Equal(0, b));
    }

    [Fact]
    public async Task VerifyUvAsync_WrongFingerprint_ThrowsInvalidPinExceptionWithRetriesRemaining()
    {
        var backend = new FixedStatusWordBackend(unchecked((short)0x63C3)); // 3 retries remaining

        var exception = await Assert.ThrowsAsync<InvalidPinException>(() =>
            PivBioProtocol.VerifyUvAsync(
                backend,
                NullLogger.Instance,
                requestTemporaryPin: false,
                checkOnly: true,
                TestContext.Current.CancellationToken));

        Assert.Equal(3, exception.RetriesRemaining);
    }

    private sealed class FixedStatusWordBackend(short statusWord) : IPivBackend
    {
        public Task<PivInitialization> InitializeAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Not needed for these tests.");

        public Task<ApduResponse> SendAsync(
            ApduCommand command,
            bool throwOnError = true,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new ApduResponse(Array.Empty<byte>(), statusWord));
    }

    private sealed class BioCapturingBackend(byte[] responseWithoutSw) : IPivBackend
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
