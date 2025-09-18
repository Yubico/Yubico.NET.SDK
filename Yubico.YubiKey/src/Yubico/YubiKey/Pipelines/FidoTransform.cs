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

using System;
using System.Buffers.Binary;
using System.Linq;
using System.Security.Cryptography;
using Yubico.Core.Devices.Hid;
using Yubico.Core.Iso7816;
using Yubico.YubiKey.Fido2.Commands;

namespace Yubico.YubiKey.Pipelines;

/// <summary>
///     Represents an ApduPipeline backed by a direct connection
///     to the U2F/FIDO2 application.
/// </summary>
internal class FidoTransform : IApduTransform, ICancelApduTransform
{
    private const int Ctap1Message = 0x03;
    private const int CtapError = 0x3F;
    private const int CtapHidCbor = 0x10;

    private const int PacketSize = 64;
    private const int MaxPayloadSize = 7609; // 64 - 7 + 128 * (64 - 5)

    private const int InitHeaderSize = 7;
    private const int InitDataSize = PacketSize - InitHeaderSize;
    private const int ContinuationHeaderSize = 5;
    private const int ContinuationDataSize = PacketSize - ContinuationHeaderSize;

    private const byte CtapHidInitCmd = 0x06;
    private const byte CtapHidKeepAliveCmd = 0x3b;
    private const byte CtapHidCancelCmd = 0x11;
    private const uint CtapHidBroadcastChannelId = 0xffffffff;

    internal readonly IHidConnection _hidConnection;

    private uint? _channelId;

    public FidoTransform(IHidConnection hidConnection)
    {
        if (hidConnection is null)
        {
            throw new ArgumentNullException(nameof(hidConnection));
        }

        _hidConnection = hidConnection;
    }

    public bool IsChannelIdAcquired => _channelId.HasValue;

    #region IApduTransform Members

    public void Setup() => AcquireCtapHidChannel();

    // In the case where we received a U2F HID error, the response APDU's
    // data field will contain the error code (1 byte long), and the Status
    // Word will be the closest equivalent ISO7816 status word (or 0x6F00
    // "NoPreciseDiagnosis" if there isn't a good fit).
    public ResponseApdu Invoke(CommandApdu commandApdu, Type commandType, Type responseType)
    {
        if (!IsChannelIdAcquired)
        {
            throw new InvalidOperationException(ExceptionMessages.InvalidChannelId);
        }

        byte ctapCmd = commandApdu.Ins;
        byte[] ctapData = commandApdu.Data.ToArray();

        if (ctapData.Length >= MaxPayloadSize)
        {
            throw new ArgumentException(ExceptionMessages.Ctap2CommandTooLarge, nameof(commandApdu));
        }

        byte[] responseData = TransmitCommand(_channelId!.Value, ctapCmd, ctapData, out byte responseByte);
        var responseApdu = responseByte switch
        {
            Ctap1Message => new ResponseApdu(responseData),
            CtapHidCbor => CtapToApduResponse.ToCtap2ResponseApdu(responseData),
            CtapError => CtapToApduResponse.ToCtap1ResponseApdu(responseData),
            _ => new ResponseApdu(responseData, SWConstants.Success)
        };

        return responseApdu;
    }

    public void Cleanup() => _channelId = null;

    #endregion

    #region ICancelApduTransform Members

    public QueryCancel? QueryCancel { get; set; }

    #endregion

    private static byte[] ConstructInitPacket(uint cid, byte cmd, ReadOnlySpan<byte> data, int totalDataLength)
    {
        byte[] packet = new byte[PacketSize];

        BinaryPrimitives.WriteUInt32BigEndian(packet, cid);

        // always set bit 7 for init packets
        packet[4] = (byte)(cmd | 0b1000_0000);

        packet[5] = (byte)(totalDataLength >> 8);
        packet[6] = (byte)(totalDataLength & 0xFF);

        data.CopyTo(packet.AsSpan(7));

        return packet;
    }

    private static byte[] ConstructContinuationPacket(uint cid, byte seq, ReadOnlySpan<byte> data)
    {
        byte[] packet = new byte[PacketSize];

        BinaryPrimitives.WriteUInt32BigEndian(packet, cid);

        // always unset bit 7 for cont packets
        packet[4] = (byte)(seq & 0b0111_1111);

        data.CopyTo(packet.AsSpan(5));

        return packet;
    }

    // This function applies a mask to remove the initial frame identifier (0x80)
    private static byte GetPacketCmd(byte[] packet) => (byte)(packet[4] & ~0x80);

    private static int GetPacketBcnt(byte[] packet) => (packet[5] << 8) | packet[6];

    private byte[] TransmitCommand(uint channelId, byte commandByte, byte[] data, out byte responseByte)
    {
        SendRequest(channelId, commandByte, data);

        byte cmdByte = commandByte;
        if (data.Length > 0 && commandByte == CtapConstants.CtapHidCbor)
        {
            cmdByte = data[0];
        }

        byte[] responseData = ReceiveResponse(channelId, cmdByte, out responseByte);

        return responseData;
    }

    private void SendRequest(uint channelId, byte commandByte, ReadOnlySpan<byte> data)
    {
        // send init request packet
        bool requestFitsInInit = data.Length <= InitDataSize;
        var dataInInitPacket = requestFitsInInit
            ? data
            : data[..InitDataSize];

        _hidConnection.SetReport(ConstructInitPacket(channelId, commandByte, dataInInitPacket, data.Length));

        if (!requestFitsInInit)
        {
            // send continuation request packets if necessary
            data = data[InitDataSize..];

            byte seq = 0;
            while (data.Length > ContinuationDataSize)
            {
                _hidConnection.SetReport(ConstructContinuationPacket(channelId, seq, data[..ContinuationDataSize]));
                data = data[ContinuationDataSize..];
                seq++;
            }

            _hidConnection.SetReport(ConstructContinuationPacket(channelId, seq, data));
        }
    }

    /// <summary>
    ///     Receives the FIDO U2F HID response message.
    /// </summary>
    /// <remarks>
    ///     The important information in a U2F HID response message are the
    ///     command identifier (1 byte), and the payload data (0-7609 bytes),
    ///     and this method will return both items. The command identifier
    ///     describes what the message is about. Most of the time it will match
    ///     the command identifier sent in the request. However, it's also
    ///     possible for a U2FHID_ERROR to be returned when certain failure
    ///     modes are encountered. This behavior is described in the
    ///     specification document FIDO U2F HID Protocol.
    /// </remarks>
    /// <param name="channelId">
    ///     The id number for the operation.
    /// </param>
    /// <param name="commandByte">
    ///     The byte describing the command currently operating.
    /// </param>
    /// <param name="responseCommand">
    ///     An output parameter containing the command identifier returned by
    ///     the CTAP response.
    /// </param>
    /// <returns>
    ///     A byte array containing the response data.
    /// </returns>
    /// <exception cref="MalformedYubiKeyResponseException">
    ///     Thrown when the response payload size is larger than expected.
    /// </exception>
    private byte[] ReceiveResponse(uint channelId, byte commandByte, out byte responseCommand)
    {
        // get init response packet
        byte[] responseInitPacket = _hidConnection.GetReport();
        while (responseInitPacket[4] == (CtapHidKeepAliveCmd | 0b1000_0000))
        {
            if (QueryCancel is not null && QueryCancel(commandByte))
            {
                _hidConnection.SetReport(
                    ConstructInitPacket(channelId, CtapHidCancelCmd, ReadOnlySpan<byte>.Empty, 0));

                QueryCancel = null;
            }

            responseInitPacket = _hidConnection.GetReport();
        }

        int responseDataLength = GetPacketBcnt(responseInitPacket);

        if (responseDataLength > MaxPayloadSize)
        {
            throw new MalformedYubiKeyResponseException(ExceptionMessages.Ctap2MalformedResponse);
        }

        responseCommand = GetPacketCmd(responseInitPacket);

        // allocate space for the result
        // data formatted as [command identifier][payload data]
        byte[] responseData = new byte[responseDataLength + PacketSize];
        responseInitPacket.AsSpan(InitHeaderSize).CopyTo(responseData);

        // get continuation response packets, if necessary
        if (responseDataLength > InitDataSize)
        {
            int bytesRead = InitDataSize;
            while (bytesRead < responseDataLength - ContinuationDataSize)
            {
                byte[] continuationPacket = _hidConnection.GetReport();
                continuationPacket.AsSpan(ContinuationHeaderSize).CopyTo(responseData.AsSpan(bytesRead));
                bytesRead += ContinuationDataSize;
            }

            byte[] lastContinuationPacket = _hidConnection.GetReport();
            lastContinuationPacket.AsSpan(ContinuationHeaderSize).CopyTo(responseData.AsSpan(bytesRead));
        }

        return responseData.Take(responseDataLength).ToArray();
    }

    /// <summary>
    ///     Acquires a CTAPHID channel by sending CTAPHID_INIT to the broadcast channel.
    /// </summary>
    /// <returns>A fresh CTAPHID channel</returns>
    private void AcquireCtapHidChannel()
    {
        // start by calling CTAPHID_INIT
        using var rng = RandomNumberGenerator.Create();
        byte[] nonce = new byte[8];
        rng.GetBytes(nonce);
        byte[] response = TransmitCommand(CtapHidBroadcastChannelId, CtapHidInitCmd, nonce, out _);

        var receivedNonce = response.AsSpan(0, 8);

        if (!nonce.AsSpan().SequenceEqual(receivedNonce))
        {
            throw new MalformedYubiKeyResponseException(ExceptionMessages.Ctap2MalformedResponse);
        }

        uint cid = BinaryPrimitives.ReadUInt32BigEndian(response.AsSpan(8, 4));

        _channelId = cid;
    }
}
