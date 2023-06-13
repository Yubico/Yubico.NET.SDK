// Copyright 2022 Yubico AB
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
using System.Formats.Cbor;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Fido2.Commands
{
    /// <summary>
    /// Stores arbitrary data on the YubiKey. This command does not format the
    /// data (the FIDO2 standard specifies a format for
    /// <c>serializedLargeBlobArray</c>), it simply stores whatever byte array it
    /// is given.
    /// &gt; [!WARNING]
    /// &gt; While storing arbitrary data that does not follow the standard's
    /// &gt; formatting specification is possible, it is not recommended.
    /// &gt; See the <xref href="Fido2Blobs">User's Manual entry</xref> on FIDO2
    /// &gt; Blobs and the documentation for the method
    /// <see cref="Fido2Session.SetSerializedLargeBlobArray"/>.
    /// </summary>
    /// <remarks>
    /// The partner Response class is <see cref="SetLargeBlobResponse"/>.
    /// Specified in CTAP as "authenticatorLargeBlobs".
    /// <para>
    /// The standard specifies one command called
    /// "<c>authenticatorLargeBlobs</c>". It takes input that specifies whether
    /// to get or set. The SDK breaks this into two commands.
    /// </para>
    /// <para>
    /// The standard specifies the format of large blob data, however, this
    /// command does not format the input data, nor does it verify that the data
    /// is formatted correctly. It stores whatever data it is given.
    /// </para>
    /// <para>
    /// Note that this command will replace any data currently stored as a large
    /// blob on the YubiKey. To update the current data, get the current data
    /// (using <see cref="GetLargeBlobCommand"/>), "edit" it and then call this
    /// command.
    /// </para>
    /// </remarks>
    public sealed class SetLargeBlobCommand : IYubiKeyCommand<SetLargeBlobResponse>
    {
        private const byte CtapGetLargeBlobCmd = 0x0C;
        private const int CborMapCountInit = 5;
        private const int CborMapCountUpdate = 4;
        private const int CborKeySet = 2;
        private const int CborKeyOffset = 3;
        private const int CborKeyLength = 4;
        private const int CborKeyAuth = 5;
        private const int CborKeyProtocol = 6;

        private readonly byte[] _blobData;
        private readonly int _offset;
        private readonly int _length;
        private readonly int _protocol;
        private readonly byte[] _pinUvAuth;

        /// <summary>
        /// Gets the YubiKeyApplication to which this command belongs.
        /// </summary>
        /// <value>
        /// YubiKeyApplication.Fido2
        /// </value>
        public YubiKeyApplication Application => YubiKeyApplication.Fido2;

        /// <summary>
        /// Constructs an instance of the <see cref="SetLargeBlobCommand" /> class.
        /// </summary>
        /// <remarks>
        /// This will store all the data given by the input arg <c>blobData</c>.
        /// The <c>offset</c> and <c>length</c> arguments do not refer to the
        /// offset and length of the input data, but rather the offset inside the
        /// full blob on the YubiKey and the length is the total length of data
        /// that will be stored.
        /// <para>
        /// The <c>length</c> argument is used only when the offset is 0. If the
        /// input <c>offset</c> arg is not 0, this method will ignore
        /// <c>length</c>.
        /// </para>
        /// <para>
        /// Each call to the set command must contain "maxFragmentLength" or
        /// fewer bytes. The value of "maxFragmentLength" (from the standard) is
        /// the message size minus 64. See the
        /// <see cref="AuthenticatorInfo.MaximumMessageSize"/> property in the
        /// return from the <see cref="GetInfoCommand"/>. If the total length to
        /// set is more than "maxFragmentLength", make multiple calls to the
        /// <c>SetLargeBlobCommand</c>. The first call will use an <c>offset</c>
        /// of zero and the length will be the total length. Each successive call
        /// will set the <c>offset</c> to pick up where the last set left off,
        /// and the <c>length</c> arg will be ignored.
        /// </para>
        /// <para>
        /// This command will not determine "maxFragmentLength". If the input
        /// data is too long, this command will send it to the YubiKey, which
        /// will likely not store the data and return an error. If this is the
        /// first call to <c>Set</c> (<c>offset</c> is zero), and the input
        /// <c>blobData</c> is longer than the length, this command will send the
        /// data into the YubiKey which will likely not store the data and return
        /// an error.
        /// </para>
        /// <para>
        /// Each call to <c>Set</c> must provide the "pinUvAuthParam", which the
        /// standard defines as
        /// <code>
        ///   authenticate (pinUvAuthToken,
        ///       32 x 0xff || 0x0c 00 || uint32LittleEndian(offset) ||
        ///       SHA-256(contents of set byte string)
        /// </code>
        /// See <see cref="PinProtocols.PinUvAuthProtocolBase.AuthenticateUsingPinToken(byte[],byte[])"/>.
        /// Note that this is not the "normal" process. All other commands
        /// require only the PinUvAuthToken and they compute the PinUvAuthParam.
        /// However, because computing the AuthParam requires digesting data,
        /// this command requires the caller make the computations.
        /// </para>
        /// <para>
        /// It is the responsibility of the caller to keep track of the offset.
        /// </para>
        /// </remarks>
        /// <param name="blobData">
        /// The data to store.
        /// </param>
        /// <param name="offset">
        /// The offset into the currently stored blob where the command should
        /// begin storing.
        /// </param>
        /// <param name="length">
        /// If the <c>offset</c> is zero, this is the total number of bytes to
        /// store. Otherwise this argument is ignored.
        /// </param>
        /// <param name="pinUvAuthParam">
        /// The authentication (using the pinUvAuthToken) of the data to store
        /// (with some other bytes).
        /// </param>
        /// <param name="protocol">
        /// The PIN UV Auth protocol used to compute the <c>pinUvAuthParam</c>.
        /// </param>
        public SetLargeBlobCommand(
            ReadOnlyMemory<byte> blobData,
            int offset, int length,
            ReadOnlyMemory<byte> pinUvAuthParam, int protocol)
        {
            _blobData = blobData.ToArray();
            _offset = offset;
            _length = length;
            _pinUvAuth = pinUvAuthParam.ToArray();
            _protocol = protocol;
        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu()
        {
            int count = (_offset == 0) ? CborMapCountInit : CborMapCountUpdate;
            var cborWriter = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            cborWriter.WriteStartMap(count);
            cborWriter.WriteInt32(CborKeySet);
            cborWriter.WriteByteString(_blobData);
            cborWriter.WriteInt32(CborKeyOffset);
            cborWriter.WriteInt32(_offset);
            if (_offset == 0)
            {
                cborWriter.WriteInt32(CborKeyLength);
                cborWriter.WriteInt32(_length);
            }
            cborWriter.WriteInt32(CborKeyAuth);
            cborWriter.WriteByteString(_pinUvAuth);
            cborWriter.WriteInt32(CborKeyProtocol);
            cborWriter.WriteInt32(_protocol);
            cborWriter.WriteEndMap();
            byte[] encodedParams = cborWriter.Encode();

            byte[] payload = new byte[encodedParams.Length + 1];
            payload[0] = CtapGetLargeBlobCmd;
            Array.Copy(encodedParams, 0, payload, 1, encodedParams.Length);

            return new CommandApdu()
            {
                Ins = CtapConstants.CtapHidCbor,
                Data = payload
            };
        }

        /// <inheritdoc />
        public SetLargeBlobResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new SetLargeBlobResponse(responseApdu);
    }
}
