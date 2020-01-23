// Copyright 2021 Yubico AB
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
using Yubico.YubiKey.Fido2.Commands;
using Yubico.Core.Devices.Hid;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Pipelines
{
    /// <summary>
    /// Represents an ApduPipeline backed by a direct connection
    /// to the U2F/FIDO2 application.
    /// </summary>
    internal class FidoTransform : IApduTransform
    {
        private const int PacketSize = 64;
        private const int MaxPayloadSize = 7609; // 64 - 7 + 128 * (64 - 5)

        private const int InitHeaderSize = 7;
        private const int InitDataSize = PacketSize - InitHeaderSize;
        private const int ContinuationHeaderSize = 5;
        private const int ContinuationDataSize = PacketSize - ContinuationHeaderSize;

        private const byte CtapHidInitCmd = 0x06;
        private const byte CtapHidKeepAliveCmd = 0x3b;
        private const uint CtapHidBroadcastChannelId = 0xffffffff;

        internal readonly IHidConnection _hidConnection;

        private uint? _channelId;

        public bool IsChannelIdAcquired => _channelId.HasValue;

        public FidoTransform(IHidConnection hidConnection)
        {
            if (hidConnection is null)
            {
                throw new ArgumentNullException(nameof(hidConnection));
            }

            _hidConnection = hidConnection;
        }

        public void Setup() => AcquireCtapHidChannel();

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

            byte[] responseData = TransmitCommand(_channelId!.Value, ctapCmd, ctapData);

            ResponseApdu responseApdu = ctapCmd == (byte)CtapHidCommand.Ctap1Message
                ? new ResponseApdu(responseData)
                : new ResponseApdu(responseData, SWConstants.Success);

            return responseApdu;
        }

        public void Cleanup() => _channelId = null;

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

        private static int GetPacketBcnt(byte[] packet) =>
            (packet[5] << 8) | (packet[6]);

        private byte[] TransmitCommand(uint channelId, byte commandByte, byte[] data)
        {
            SendRequest(channelId, commandByte, data);

            byte[] responseData = ReceiveResponse();

            return responseData;
        }

        private void SendRequest(uint channelId, byte commandByte, ReadOnlySpan<byte> data)
        {
            // send init request packet
            bool requestFitsInInit = data.Length <= InitDataSize;
            ReadOnlySpan<byte> dataInInitPacket = requestFitsInInit ? data : data.Slice(0, InitDataSize);
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

        private byte[] ReceiveResponse()
        {
            // get init response packet 
            byte[] responseInitPacket = _hidConnection.GetReport();
            while (responseInitPacket[4] == (CtapHidKeepAliveCmd | 0b1000_0000))
            {
                responseInitPacket = _hidConnection.GetReport();
            }
            int responseDataLength = GetPacketBcnt(responseInitPacket);

            if (responseDataLength > MaxPayloadSize)
            {
                throw new MalformedYubiKeyResponseException(ExceptionMessages.Ctap2MalformedResponse);
            }

            // allocate space for the result
            byte[] responseData = new byte[responseDataLength + PacketSize];
            responseInitPacket.AsSpan(InitHeaderSize).CopyTo(responseData);

            // get continuation response packets if necessary
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
        /// Acquires a CTAPHID channel by sending CTAPHID_INIT to the broadcast channel.
        /// </summary>
        /// <returns>A fresh CTAPHID channel</returns>
        private void AcquireCtapHidChannel()
        {
            // start by calling CTAPHID_INIT
            using var rng = RandomNumberGenerator.Create();
            byte[] nonce = new byte[8];
            rng.GetBytes(nonce);
            byte[] response = TransmitCommand(CtapHidBroadcastChannelId, CtapHidInitCmd, nonce);

            Span<byte> receivedNonce = response.AsSpan(0, 8);

            if (!nonce.AsSpan().SequenceEqual(receivedNonce))
            {
                throw new MalformedYubiKeyResponseException(ExceptionMessages.Ctap2MalformedResponse);
            }

            uint cid = BinaryPrimitives.ReadUInt32BigEndian(response.AsSpan(8, 4));

            _channelId = cid;
        }
    }
}
