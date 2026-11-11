// Copyright 2024 Yubico AB
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
using System.Security.Cryptography;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Utilities;
using Yubico.YubiKit.Piv.Backends;

namespace Yubico.YubiKit.Piv.Bio;

internal static class BioProtocol
{
    private const byte Cla = 0x00;
    private const byte InsGetMetadata = 0xF7;
    private const byte InsVerify = 0x20;
    private const byte TagRetries = 0x06;
    private const byte TagConfigured = 0x07;
    private const byte TagTemporaryPinActive = 0x08;
    private const byte TagTemporaryPin = 0x01;
    private const byte TagRequestTemporaryPin = 0x02;
    private const byte TagVerifyUv = 0x03;
    private const int TemporaryPinLength = 16;
    private const int ShortTlvHeaderLength = 2;
    private static ILogger Logger => YubiKitLogging.CreateLogger(nameof(BioProtocol));

    internal static async Task<BioMetadata> GetBioMetadataAsync(IBackend backend, CancellationToken cancellationToken = default)
    {
        var command = new ApduCommand(Cla, InsGetMetadata, 0, KeyReference.Bio);
        var response = await backend.SendAsync(command, throwOnError: false, cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureAvailable(response);
            // Python YubiKit 4ca60f7 piv.py:1111-1133: retries are required; the two flags are optional.
            var fields = TlvHelper.DecodeDictionary(response.Data);
            byte retries = ReadByte(fields, TagRetries, required: true);
            byte configured = ReadByte(fields, TagConfigured);
            byte temporaryPin = ReadByte(fields, TagTemporaryPinActive);
            if (configured > 1 || temporaryPin > 1)
                throw new ApduException("Invalid biometric metadata flag.");
            Logger.LogDebug("PIV: Biometric metadata configured={Configured}, retries={Retries}, temporary PIN active={Active}", configured == 1, retries, temporaryPin == 1);
            return new BioMetadata(configured == 1, retries, temporaryPin == 1);
        }
        catch (Exception ex) when (ex is ArgumentException or IndexOutOfRangeException)
        {
            throw new ApduException("Malformed biometric metadata response.", ex);
        }
        finally
        {
            response.Clear();
        }
    }

    private static byte ReadByte(IDictionary<int, ReadOnlyMemory<byte>> fields, byte tag, bool required = false)
    {
        if (!fields.TryGetValue(tag, out var value))
        {
            if (required)
                throw new ApduException($"Missing biometric metadata field 0x{tag:X2}.");
            return 0;
        }
        if (value.Length != 1)
            throw new ApduException($"Invalid biometric metadata field 0x{tag:X2}.");
        return value.Span[0];
    }

    internal static async Task<byte[]?> VerifyUvAsync(
        IBackend backend,
        UserVerification userVerification = UserVerification.Verify,
        CancellationToken cancellationToken = default)
    {
        byte[] data = userVerification switch
        {
            UserVerification.CheckOnly => [],
            UserVerification.Verify => [TagVerifyUv, 0],
            UserVerification.VerifyAndRequestTemporaryPin => [TagRequestTemporaryPin, 0],
            _ => throw new ArgumentOutOfRangeException(nameof(userVerification))
        };
        Logger.LogDebug("PIV: Biometric verification mode {Mode}", userVerification);
        var command = new ApduCommand(Cla, InsVerify, 0, KeyReference.Bio, data);
        var response = await backend.SendAsync(command, throwOnError: false, cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureVerified(response);
            if (userVerification != UserVerification.VerifyAndRequestTemporaryPin)
                return null;
            if (response.Data.Length != TemporaryPinLength)
                throw new ApduException("Biometric verification did not return a 16-byte temporary PIN.");
            return response.Data.ToArray();
        }
        finally
        {
            response.Clear();
        }
    }

    internal static async Task VerifyTemporaryPinAsync(IBackend backend, ReadOnlyMemory<byte> temporaryPin, CancellationToken cancellationToken = default)
    {
        if (temporaryPin.Length != TemporaryPinLength)
            throw new ArgumentException("Temporary PIN must be exactly 16 bytes.", nameof(temporaryPin));
        byte[] data = new byte[ShortTlvHeaderLength + TemporaryPinLength];
        try
        {
            data[0] = TagTemporaryPin;
            data[1] = TemporaryPinLength;
            temporaryPin.Span.CopyTo(data.AsSpan(ShortTlvHeaderLength));
            var command = new ApduCommand(Cla, InsVerify, 0, KeyReference.Bio, data);
            var response = await backend.SendAsync(command, throwOnError: false, cancellationToken).ConfigureAwait(false);
            try
            {
                EnsureVerified(response);
            }
            finally
            {
                response.Clear();
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(data);
        }
    }

    private static void EnsureAvailable(ApduResponse response)
    {
        if (response.SW is SWConstants.FileOrApplicationNotFound or SWConstants.DataNotFound or SWConstants.InsNotSupported)
            throw new NotSupportedException("Biometric verification is not supported by this YubiKey.");
        if (!response.IsOK())
            throw ApduException.FromStatusWord(response.SW, "Biometric operation failed");
    }

    private static void EnsureVerified(ApduResponse response)
    {
        if (SWConstants.ExtractRetryCount(response.SW) is { } retries)
            throw new InvalidPinException(retries, "Biometric verification failed.");
        EnsureAvailable(response);
    }
}