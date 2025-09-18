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
using System.Globalization;
using System.Text;
using Yubico.Core.Iso7816;
using Yubico.Core.Tlv;

namespace Yubico.YubiKey.YubiHsmAuth.Commands;

/// <summary>
///     Remove a credential from the YubiHSM Auth application.
/// </summary>
/// <remarks>
///     <para>
///         The associated response class is
///         <see cref="DeleteCredentialResponse" />.
///     </para>
///     <para>
///         There is a limit of 8 attempts to authenticate with the management key
///         before the management key is blocked. Once the management key is
///         blocked, the application must be reset before performing operations
///         which require authentication with the management key (such as adding
///         credentials, deleting credentials, and changing the management key).
///         To reset the application, see <see cref="ResetApplicationCommand" />.
///         Supplying the correct management key before the management key is
///         blocked will reset the retry counter to 8.
///     </para>
/// </remarks>
public class DeleteCredentialCommand : IYubiKeyCommand<DeleteCredentialResponse>
{
    private const byte DeleteCredentialInstruction = 0x02;

    /// <summary>
    ///     The management key must be exactly 16 bytes.
    /// </summary>
    /// <remarks>
    ///     The management key is supplied as an argument to the constructor.
    /// </remarks>
    public static readonly int ValidManagementKeyLength = 16;

    private readonly Credential _credential = new();

    private readonly ReadOnlyMemory<byte> _managementKey;

    /// <summary>
    ///     Constructs an instance of the <see cref="DeleteCredentialCommand" />
    ///     class.
    /// </summary>
    /// <remarks>
    ///     The <see cref="Label" /> will need to be set before calling
    ///     <see cref="CreateCommandApdu" />.
    /// </remarks>
    /// <param name="managementKey">
    ///     The secret used to authenticate to the application prior to adding
    ///     or removing credentials. See <see cref="ValidManagementKeyLength" />
    ///     for its required length. The application has a default management
    ///     key of all zeros.
    /// </param>
    /// <exception cref="ArgumentException">
    ///     <paramref name="managementKey" /> does not meet the length
    ///     requirements.
    /// </exception>
    public DeleteCredentialCommand(ReadOnlyMemory<byte> managementKey)
    {
        _managementKey = managementKey.Length == ValidManagementKeyLength
            ? managementKey
            : throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.YubiHsmAuthInvalidMgmtKeyLength,
                    managementKey.Length));
    }

    /// <summary>
    ///     Constructs an instance of the <see cref="DeleteCredentialCommand" />
    ///     class.
    /// </summary>
    /// <param name="managementKey">
    ///     The secret used to authenticate to the application prior to adding
    ///     or removing credentials. See <see cref="ValidManagementKeyLength" />
    ///     for its required length. The application has a default management
    ///     key of all zeros.
    /// </param>
    /// <param name="label">
    ///     The label of the credential to be deleted. The string must meet the
    ///     same requirements as <see cref="Credential.Label" />.
    /// </param>
    /// <exception cref="ArgumentException">
    ///     <paramref name="managementKey" /> does not meet the length
    ///     requirements.
    /// </exception>
    public DeleteCredentialCommand(ReadOnlyMemory<byte> managementKey, string label)
        : this(managementKey)
    {
        Label = label;
    }

    /// <inheritdoc cref="Credential.Label" />
    public string Label
    {
        get => _credential.Label;
        set => _credential.Label = value;
    }

    #region IYubiKeyCommand<DeleteCredentialResponse> Members

    public YubiKeyApplication Application => YubiKeyApplication.YubiHsmAuth;

    public CommandApdu CreateCommandApdu() =>
        new()
        {
            Ins = DeleteCredentialInstruction,
            Data = BuildDataField()
        };

    public DeleteCredentialResponse CreateResponseForApdu(ResponseApdu responseApdu) => new(responseApdu);

    #endregion

    private byte[] BuildDataField()
    {
        var tlvWriter = new TlvWriter();
        tlvWriter.WriteValue(DataTagConstants.ManagementKey, _managementKey.Span);
        tlvWriter.WriteString(
            DataTagConstants.Label,
            Label, Encoding.UTF8);

        byte[] tlvBytes = tlvWriter.Encode();
        tlvWriter.Clear();

        return tlvBytes;
    }
}
