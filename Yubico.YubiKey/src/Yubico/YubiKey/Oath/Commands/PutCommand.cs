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
using System.Text;
using Yubico.Core.Buffers;
using Yubico.Core.Iso7816;
using Yubico.Core.Tlv;

namespace Yubico.YubiKey.Oath.Commands;

/// <summary>
///     Adds or overwrites an OATH credential.
/// </summary>
public class PutCommand : IYubiKeyCommand<OathResponse>
{
    private const byte PutInstruction = 0x01;
    private const byte NameTag = 0x71;
    private const byte SecretTag = 0x73;
    private const byte PropertyTag = 0x78;
    private const byte RequireTouchPropertyByte = 0x02;
    private const byte ImfTag = 0x7a;
    private const byte ImfDataLength = 0x04;
    private const int DefaultDigits = 6;
    private const HashAlgorithm DefaultAlgorithm = HashAlgorithm.Sha1;
    private const CredentialType DefaultType = CredentialType.Totp;
    private const int MinimalSecretLength = 14;

    /// <summary>
    ///     Constructs an instance of the <see cref="PutCommand" /> class.
    /// </summary>
    public PutCommand()
    {
    }

    /// <summary>
    ///     Constructs an instance of the <see cref="PutCommand" /> class.
    /// </summary>
    /// ///
    /// <param name="credential">
    ///     The credential to add or overwrite.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     The credential is null.
    /// </exception>
    public PutCommand(Credential credential)
    {
        if (credential is null)
        {
            throw new ArgumentNullException(nameof(credential));
        }

        Credential = credential;
    }

    /// <summary>
    ///     The credential to add or overwrite.
    /// </summary>
    public Credential? Credential { get; set; }

    #region IYubiKeyCommand<OathResponse> Members

    /// <summary>
    ///     Gets the YubiKeyApplication to which this command belongs.
    /// </summary>
    /// <value>
    ///     YubiKeyApplication.Oath
    /// </value>
    public YubiKeyApplication Application => YubiKeyApplication.Oath;

    /// <inheritdoc />
    public CommandApdu CreateCommandApdu()
    {
        if (Credential is null)
        {
            throw new InvalidOperationException(ExceptionMessages.InvalidCredential);
        }

        byte[] nameBytes = Encoding.UTF8.GetBytes(Credential.Name);
        byte[] secretDecoded = Base32.DecodeText(Credential.Secret ?? string.Empty);
        byte[] fullKey = new byte[2 + Math.Max(secretDecoded.Length, MinimalSecretLength)];

        byte algorithm = (byte)(Credential.Algorithm ?? DefaultAlgorithm);
        byte type = (byte)(Credential.Type ?? DefaultType);

        fullKey[0] = (byte)(algorithm | type);
        fullKey[1] = (byte)(Credential.Digits ?? DefaultDigits);
        Array.Copy(secretDecoded, 0, fullKey, 2, secretDecoded.Length);

        var tlvWriter = new TlvWriter();
        tlvWriter.WriteValue(NameTag, nameBytes);
        tlvWriter.WriteValue(SecretTag, fullKey);

        if (Credential.RequiresTouch ?? false)
        {
            tlvWriter.WriteEncoded(new[] { PropertyTag, RequireTouchPropertyByte });
        }

        if (Credential.Type == CredentialType.Hotp && Credential.Counter.HasValue && Credential.Counter.Value > 0)
        {
            byte[] buffer = new byte[ImfDataLength];
            var span = new Span<byte>(buffer);
            BinaryPrimitives.WriteInt32LittleEndian(span[..ImfDataLength], Credential.Counter.Value);
            tlvWriter.WriteValue(ImfTag, buffer);
        }

        return new CommandApdu
        {
            Ins = PutInstruction,
            Data = tlvWriter.Encode()
        };
    }

    /// <inheritdoc />
    public OathResponse CreateResponseForApdu(ResponseApdu responseApdu) => new(responseApdu);

    #endregion
}
