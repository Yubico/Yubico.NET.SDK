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
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Piv.Backend;
using Yubico.YubiKit.Piv.DataObjects;

namespace Yubico.YubiKit.Piv.UnitTests.DataObjects;

/// <summary>
/// Security regression test: <see cref="PivDataObjectProtocol.PutObjectAsync"/> builds its outgoing
/// command payload in an internal <c>ArrayBufferWriter&lt;byte&gt;</c>. Per <see cref="ApduCommand"/>'s
/// documented ownership contract, whoever owns that buffer must zero it after transmission if it may
/// carry sensitive material (PIV PIN-only mode routes a plaintext management key through this exact
/// path via <c>PutObjectAsync(PrintedInformation, ...)</c>).
/// </summary>
public class PivDataObjectProtocolTests
{
    [Fact]
    public async Task PutObjectAsync_ZeroesInternalWriteBufferAfterTransmission()
    {
        byte[] managementKeyLikeData = Enumerable.Range(1, 24).Select(i => (byte)i).ToArray();
        var backend = new CapturingBackend();

        await PivDataObjectProtocol.PutObjectAsync(
            backend,
            isAuthenticated: true,
            PivDataObject.PrintedInformation,
            managementKeyLikeData,
            TestContext.Current.CancellationToken);

        Assert.NotNull(backend.LastCommand);
        Assert.True(MemoryMarshal.TryGetArray(backend.LastCommand!.Value.Data, out var segment));

        // The full backing array (not just the logical Data slice) must be zeroed - the writer
        // buffer is not shared with anything else after the command is sent.
        Assert.All(segment.Array!.AsSpan(segment.Offset, segment.Count).ToArray(), b => Assert.Equal(0, b));
    }

    [Fact]
    public async Task PutObjectAsync_TransmitsRealDataBeforeZeroing()
    {
        // Sanity check for the test above: prove the captured command actually contained the real
        // plaintext bytes at the moment of transmission (i.e. the zeroing assertion isn't vacuously
        // true because nothing was ever sent).
        byte[] managementKeyLikeData = Enumerable.Range(1, 24).Select(i => (byte)i).ToArray();
        var backend = new CapturingBackend();
        byte[]? observedAtSendTime = null;
        backend.OnSend = command => observedAtSendTime = command.Data.ToArray();

        await PivDataObjectProtocol.PutObjectAsync(
            backend,
            isAuthenticated: true,
            PivDataObject.PrintedInformation,
            managementKeyLikeData,
            TestContext.Current.CancellationToken);

        Assert.NotNull(observedAtSendTime);
        Assert.True(observedAtSendTime!.AsSpan().IndexOf(managementKeyLikeData) >= 0);
    }

    [Fact]
    public async Task PutObjectAsync_NonSecretData_StillZeroesBufferHarmlessly()
    {
        // Zeroing must not corrupt behavior for the many non-secret callers (certs, CHUID, CCC,
        // ADMIN DATA); this only proves the fix doesn't throw / break the normal success path.
        var backend = new CapturingBackend();

        var exception = await Record.ExceptionAsync(() => PivDataObjectProtocol.PutObjectAsync(
            backend,
            isAuthenticated: true,
            PivDataObject.Chuid,
            new byte[] { 0x30, 0x02, 0xAA, 0xBB },
            TestContext.Current.CancellationToken));

        Assert.Null(exception);
    }

    [Fact]
    public async Task PutObjectAsync_DeleteObject_DoesNotThrowWhenZeroingEmptyBuffer()
    {
        var backend = new CapturingBackend();

        var exception = await Record.ExceptionAsync(() => PivDataObjectProtocol.PutObjectAsync(
            backend,
            isAuthenticated: true,
            PivDataObject.Chuid,
            data: null,
            TestContext.Current.CancellationToken));

        Assert.Null(exception);
    }

    // === GetObjectAsync / UnwrapDataObjectResponse: original response buffer must be zeroed ===

    [Fact]
    public async Task GetObjectAsync_WrappedResponse_ZeroesOriginalResponseBuffer()
    {
        // Simulates reading the PRINTED object while PIN-protected mode is enabled: the raw GET
        // DATA response is "53 LL [ 88 LL2 [ 89 LL3 <24-byte clear-text management key> ] ]".
        byte[] key = Enumerable.Range(1, 24).Select(i => (byte)i).ToArray();
        byte[] printedInner = [0x88, 0x1A, 0x89, 0x18, .. key];
        byte[] wrapped = [0x53, (byte)printedInner.Length, .. printedInner];

        var backend = new GetDataCapturingBackend(wrapped);

        var result = await PivDataObjectProtocol.GetObjectAsync(
            backend, PivDataObject.PrintedInformation, TestContext.Current.CancellationToken);

        // Sanity: the returned value is a distinct, correctly-unwrapped copy of the real data -
        // the zeroing assertion below isn't vacuously true because nothing was ever produced.
        Assert.Equal(printedInner, result.ToArray());

        Assert.NotNull(backend.CapturedRawDataArray);
        // RawData includes the trailing 2 SW bytes, which are not part of the unwrapped payload
        // and are not required to be zeroed; only the response's Data portion must be.
        int dataLength = backend.CapturedCount - 2;
        Assert.All(
            backend.CapturedRawDataArray!.AsSpan(backend.CapturedOffset, dataLength).ToArray(),
            b => Assert.Equal(0, b));
    }

    [Fact]
    public async Task GetObjectAsync_UnwrappedResponse_DoesNotZeroReturnedBuffer()
    {
        // The "not wrapped" branch returns the original buffer directly (no extra copy is made),
        // so GetObjectAsync must NOT zero it here - the returned value IS the caller's deliverable,
        // and zeroing it would corrupt the data the caller asked for.
        byte[] notWrapped = [0x30, 0x02, 0xAA, 0xBB]; // does not start with 0x53

        var backend = new GetDataCapturingBackend(notWrapped);

        var result = await PivDataObjectProtocol.GetObjectAsync(
            backend, PivDataObject.Chuid, TestContext.Current.CancellationToken);

        Assert.Equal(notWrapped, result.ToArray());
    }

    private sealed class GetDataCapturingBackend(byte[] responseWithoutSw) : IPivBackend
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

    private sealed class CapturingBackend : IPivBackend
    {
        public ApduCommand? LastCommand { get; private set; }

        public Action<ApduCommand>? OnSend { get; set; }

        public Task<PivInitialization> InitializeAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Not needed for these tests.");

        public Task<ApduResponse> SendAsync(
            ApduCommand command,
            bool throwOnError = true,
            CancellationToken cancellationToken = default)
        {
            OnSend?.Invoke(command);
            LastCommand = command;
            return Task.FromResult(new ApduResponse([], unchecked((short)0x9000)));
        }
    }
}
