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
using System.Text;
using Yubico.Core.Iso7816;
using Yubico.Core.Tlv;

namespace Yubico.YubiKey.Oath.Commands;

/// <summary>
///     Performs CALCULATE of OTP (One-Time Password) values for one named credential.
/// </summary>
public class CalculateCredentialCommand : OathChallengeResponseBaseCommand, IYubiKeyCommand<CalculateCredentialResponse>
{
    private const byte CalculateInstruction = 0xA2;
    private const byte NameTag = 0x71;
    private const byte ChallengeTag = 0x74;

    /// <summary>
    ///     Constructs an instance of the <see cref="CalculateCredentialCommand" /> class.
    ///     The ResponseFormat will be set to its default value which is truncated.
    /// </summary>
    public CalculateCredentialCommand()
    {
    }

    /// <summary>
    ///     Constructs an instance of the <see cref="CalculateCredentialCommand" /> class.
    /// </summary>
    /// <param name="credential">
    ///     The credential to calculate.
    /// </param>
    /// <param name="responseFormat">
    ///     Full or truncated response to receive back.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     The credential is null.
    /// </exception>
    public CalculateCredentialCommand(Credential credential, ResponseFormat responseFormat)
    {
        if (credential is null)
        {
            throw new ArgumentNullException(nameof(credential));
        }

        Credential = credential;
        ResponseFormat = responseFormat;
    }

    /// <summary>
    ///     The credential to calculate.
    /// </summary>
    public Credential? Credential { get; set; }

    /// <summary>
    ///     Full or truncated response to receive back.
    /// </summary>
    /// <value>
    ///     The default value for the response is truncated.
    /// </value>
    public ResponseFormat ResponseFormat { get; set; } = ResponseFormat.Truncated;

    #region IYubiKeyCommand<CalculateCredentialResponse> Members

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

        if (Credential.Type is null)
        {
            throw new InvalidOperationException(ExceptionMessages.InvalidCredentialType);
        }

        if (Credential.Period is null)
        {
            throw new InvalidOperationException(ExceptionMessages.InvalidCredentialPeriod);
        }

        byte[] nameBytes = Encoding.UTF8.GetBytes(Credential.Name);

        var tlvWriter = new TlvWriter();
        tlvWriter.WriteValue(NameTag, nameBytes);

        if (Credential.Type == CredentialType.Totp)
        {
            tlvWriter.WriteValue(ChallengeTag, GenerateTotpChallenge(Credential.Period));
        }
        else
        {
            tlvWriter.WriteValue(ChallengeTag, new byte[8]);
        }

        return new CommandApdu
        {
            Ins = CalculateInstruction,
            P2 = (byte)ResponseFormat,
            Data = tlvWriter.Encode()
        };
    }

    /// <inheritdoc />
    public CalculateCredentialResponse CreateResponseForApdu(ResponseApdu responseApdu)
    {
        if (Credential is null)
        {
            throw new InvalidOperationException(ExceptionMessages.InvalidCredential);
        }

        return new CalculateCredentialResponse(responseApdu, Credential);
    }

    #endregion
}
