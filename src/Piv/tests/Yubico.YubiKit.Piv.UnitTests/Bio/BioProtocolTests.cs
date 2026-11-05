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

using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Piv.Backends;
using Yubico.YubiKit.Piv.Bio;

namespace Yubico.YubiKit.Piv.UnitTests.Bio;

/// <summary>
/// Security regression test: <see cref="BioProtocol.VerifyUvAsync"/> copies the plaintext
/// temporary PIN into its returned deliverable, but the original VERIFY response buffer (the
/// device's raw APDU response, also holding the temporary PIN in the clear) must be zeroed too.
/// </summary>
public class BioProtocolTests
{
    [Theory]
    [InlineData(UserVerification.Verify, new byte[] { 0x03, 0x00 })]
    [InlineData(UserVerification.VerifyAndRequestTemporaryPin, new byte[] { 0x02, 0x00 })]
    [InlineData(UserVerification.CheckOnly, new byte[] { })]
    public async Task VerifyUvAsync_EncodesTheRequestedCeremony(UserVerification mode, byte[] expectedData)
    {
        var backend = new BioCapturingBackend(mode == UserVerification.VerifyAndRequestTemporaryPin ? new byte[16] : []);
        byte[]? temporaryPin = await BioProtocol.VerifyUvAsync(backend, mode, TestContext.Current.CancellationToken);
        try
        {
            var command = Assert.IsType<ApduCommand>(backend.LastCommand);
            Assert.Equal(0x20, command.Ins);
            Assert.Equal(0x96, command.P2);
            Assert.Equal(expectedData, command.Data.ToArray());
        }
        finally
        {
            if (temporaryPin is not null)
                CryptographicOperations.ZeroMemory(temporaryPin);
        }
    }
    [Theory]
    [InlineData(new byte[] { 0x08, 0x01, 0x01, 0x9F, 0x33, 0x01, 0x42, 0x06, 0x01, 0x07, 0x07, 0x01, 0x00 }, false, 7, true)]
    [InlineData(new byte[] { 0x06, 0x01, 0x09 }, false, 9, false)]
    [InlineData(new byte[] { 0x07, 0x01, 0x01, 0x06, 0x01, 0x04 }, true, 4, false)]
    public async Task GetBioMetadataAsync_ParsesTaggedFieldsRegardlessOfOrder(byte[] data, bool configured, int retries, bool temporaryPin)
    {
        var backend = new BioCapturingBackend(data);
        var metadata = await BioProtocol.GetBioMetadataAsync(backend, TestContext.Current.CancellationToken);

        Assert.Equal(configured, metadata.IsConfigured);
        Assert.Equal(retries, metadata.RetriesRemaining);
        Assert.Equal(temporaryPin, metadata.HasTemporaryPin);
        Assert.Equal(data, backend.SourceData);
    }

    [Theory]
    [InlineData(new byte[] { 0x07, 0x01, 0x01 })]
    [InlineData(new byte[] { 0x06, 0x02, 0x04 })]
    [InlineData(new byte[] { 0x06, 0x00 })]
    [InlineData(new byte[] { 0x06, 0x01, 0x04, 0x07, 0x02, 0x00, 0x01 })]
    public async Task GetBioMetadataAsync_MalformedKnownFields_DoesNotFabricateState(byte[] data)
    {
        await Assert.ThrowsAsync<ApduException>(() => BioProtocol.GetBioMetadataAsync(new BioCapturingBackend(data), TestContext.Current.CancellationToken));
    }
    [Fact]
    public async Task VerifyUvAsync_WithTemporaryPin_ZeroesOriginalResponseBuffer()
    {
        byte[] temporaryPin = Enumerable.Range(1, 16).Select(i => (byte)i).ToArray();

        var backend = new BioCapturingBackend(temporaryPin);

        var result = await BioProtocol.VerifyUvAsync(
            backend,

            UserVerification.VerifyAndRequestTemporaryPin,
            TestContext.Current.CancellationToken);

        // Sanity: the returned (distinct) copy still has the real temporary PIN.
        Assert.NotNull(result);
        Assert.Equal(temporaryPin, result);

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
            BioProtocol.VerifyUvAsync(
                backend,

                UserVerification.CheckOnly,
                TestContext.Current.CancellationToken));

        Assert.Equal(3, exception.RetriesRemaining);
    }

    [Fact]
    public async Task VerifyUvAsync_UnknownMode_ThrowsBeforeSending()
    {
        var backend = new FixedStatusWordBackend(unchecked((short)0x9000));

        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            BioProtocol.VerifyUvAsync(
                backend,

                (UserVerification)99,
                TestContext.Current.CancellationToken));

        Assert.Equal("userVerification", exception.ParamName);
        Assert.Equal(0, backend.SendCount);
    }

    private sealed class FixedStatusWordBackend(short statusWord) : IBackend
    {
        public int SendCount { get; private set; }

        public Task<Initialization> ReadInitialStateAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Not needed for these tests.");

        public Task<ApduResponse> SendAsync(
            ApduCommand command,
            bool throwOnError = true,
            CancellationToken cancellationToken = default)
        {
            SendCount++;
            return Task.FromResult(new ApduResponse(Array.Empty<byte>(), statusWord));
        }
    }

    private sealed class BioCapturingBackend(byte[] responseWithoutSw) : IBackend
    {
        public byte[] SourceData => responseWithoutSw;
        public ApduCommand? LastCommand { get; private set; }
        public byte[]? CapturedRawDataArray { get; private set; }

        public int CapturedOffset { get; private set; }

        public int CapturedCount { get; private set; }

        public Task<Initialization> ReadInitialStateAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Not needed for these tests.");

        public Task<ApduResponse> SendAsync(
            ApduCommand command,
            bool throwOnError = true,
            CancellationToken cancellationToken = default)
        {
            LastCommand = command;
            var response = new ApduResponse(responseWithoutSw, unchecked((short)0x9000));

            Assert.True(MemoryMarshal.TryGetArray(response.RawData, out var segment));
            CapturedRawDataArray = segment.Array;
            CapturedOffset = segment.Offset;
            CapturedCount = segment.Count;

            return Task.FromResult(response);
        }
    }
}