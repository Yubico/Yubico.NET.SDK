// Copyright 2025 Yubico AB
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

using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Protocols.Otp.Hid;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Core.Utilities;

namespace Yubico.YubiKit.Core.Devices;

/// <summary>
///     Reads read-only <see cref="DeviceInfo" /> from a YubiKey over any supported transport.
/// </summary>
/// <remarks>
///     This is Core-owned read-only discovery logic. It is used by composite-device discovery and is
///     shared with the Management module, which delegates its device-info read to this reader.
///     Mutating Management operations (configuration writes, set mode, reset) are owned by Management.
/// </remarks>
internal static class DeviceInfoReader
{
    private const int TagMoreDeviceInfo = 0x10;

    // SmartCard: Management GetDeviceInfo APDU instruction byte.
    private const byte InsGetDeviceInfo = 0x1D;

    // FIDO HID: Management read-config CTAP vendor command byte.
    private const byte CtapReadConfig = 0xC2;

    /// <summary>
    ///     Reads the full multi-page device info from the device over the supplied protocol.
    /// </summary>
    /// <param name="protocol">
    ///     A Core protocol over SmartCard, FIDO HID, or OTP HID. The reader does not own or dispose it.
    /// </param>
    /// <param name="defaultVersion">
    ///     Optional fallback firmware version passed to <see cref="DeviceInfo.CreateFromTlvs" />. When
    ///     <c>null</c>, the firmware version is derived from the device-info TLVs.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The parsed <see cref="DeviceInfo" />.</returns>
    public static async Task<DeviceInfo> ReadAsync(
        IProtocol protocol,
        FirmwareVersion? defaultVersion,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(protocol);

        byte page = 0;
        var allPagesTlvs = new List<Tlv>();

        var remainingPages = 1;
        while (remainingPages > 0)
        {
            --remainingPages;
            var pageTlvs = await ReadPageAsync(protocol, page, cancellationToken).ConfigureAwait(false);

            Tlv? moreData = null;
            foreach (var tlv in pageTlvs)
            {
                if (tlv.Tag != TagMoreDeviceInfo)
                {
                    continue;
                }

                if (moreData is not null)
                {
                    pageTlvs.Dispose();
                    DisposeAll(allPagesTlvs);
                    throw new BadResponseException($"Duplicate more-data tags in device info page {page}.");
                }

                moreData = tlv;
            }

            if (moreData is not null && moreData.Length > 0)
            {
                remainingPages = moreData.Value.Span[^1];
            }

            foreach (var tlv in pageTlvs)
            {
                if (tlv.Tag == TagMoreDeviceInfo)
                {
                    tlv.Dispose();
                    continue;
                }

                allPagesTlvs.Add(tlv);
            }

            ++page;
        }

        using var allTlvs = new DisposableTlvList(allPagesTlvs);
        return DeviceInfo.CreateFromTlvs([.. allTlvs], defaultVersion);
    }

    private static void DisposeAll(IEnumerable<Tlv> tlvs)
    {
        foreach (var tlv in tlvs)
        {
            tlv.Dispose();
        }
    }

    private static async Task<DisposableTlvList> ReadPageAsync(
        IProtocol protocol,
        byte page,
        CancellationToken cancellationToken)
    {
        var encodedResult = await ReadRawPageAsync(protocol, page, cancellationToken).ConfigureAwait(false);

        if (encodedResult.Length < 1)
            throw new BadResponseException($"Empty response for page {page}");

        var declaredLength = encodedResult[0];
        var actualLength = encodedResult.Length - 1;
        if (actualLength != declaredLength)
        {
            throw new BadResponseException(
                $"Invalid device info length for page {page}: declared {declaredLength}, actual {actualLength}.");
        }

        return TlvHelper.DecodeList(encodedResult.AsSpan()[1..]);
    }

    private static Task<byte[]> ReadRawPageAsync(IProtocol protocol, byte page, CancellationToken cancellationToken) =>
        protocol switch
        {
            ISmartCardProtocol sc => ReadSmartCardPageAsync(sc, page, cancellationToken),
            IFidoHidProtocol fido => ReadFidoPageAsync(fido, page, cancellationToken),
            IOtpHidProtocol otp => ReadOtpPageAsync(otp, page, cancellationToken),
            _ => throw new NotSupportedException(
                $"The protocol type {protocol.GetType().Name} is not supported for reading device info. " +
                "Supported types: ISmartCardProtocol, IFidoHidProtocol, IOtpHidProtocol.")
        };

    private static async Task<byte[]> ReadSmartCardPageAsync(
        ISmartCardProtocol protocol,
        byte page,
        CancellationToken cancellationToken)
    {
        var apdu = new ApduCommand
        {
            Cla = 0,
            Ins = InsGetDeviceInfo,
            P1 = page,
            P2 = 0
        };

        var response = await protocol.TransmitAndReceiveAsync(apdu, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return response.Data.ToArray();
    }

    private static async Task<byte[]> ReadFidoPageAsync(
        IFidoHidProtocol protocol,
        byte page,
        CancellationToken cancellationToken)
    {
        var pagePayload = new byte[] { page };
        var response = await protocol.SendVendorCommandAsync(CtapReadConfig, pagePayload, cancellationToken)
            .ConfigureAwait(false);
        return response.ToArray();
    }

    private static async Task<byte[]> ReadOtpPageAsync(
        IOtpHidProtocol protocol,
        byte page,
        CancellationToken cancellationToken)
    {
        // CMD_YK4_CAPABILITIES with page payload. Page 0 sends an empty payload (Java sends null
        // which becomes a zero-filled frame); page > 0 sends a single page byte.
        var pagePayload = page == 0 ? ReadOnlyMemory<byte>.Empty : new byte[] { page };
        var response = await protocol.SendAndReceiveAsync(OtpConstants.CmdYk4Capabilities, pagePayload, cancellationToken)
            .ConfigureAwait(false);

        // Response format: [length][TLV data...][CRC16]. Validate CRC over length + data + CRC.
        var totalLength = response.Span[0] + 1 + 2;
        if (totalLength > response.Length)
        {
            throw new BadResponseException(
                $"OTP response length field ({response.Span[0]}) exceeds buffer size ({response.Length}).");
        }

        if (!ChecksumUtils.CheckCrc(response.Span, totalLength))
            throw new BadResponseException("Invalid CRC in OTP response");

        // Return data without CRC: [length][TLV data...].
        return response[..(response.Span[0] + 1)].ToArray();
    }
}
