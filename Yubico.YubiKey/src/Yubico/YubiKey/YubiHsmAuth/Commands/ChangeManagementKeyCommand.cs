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
using System.Globalization;
using Yubico.Core.Iso7816;
using Yubico.Core.Tlv;

namespace Yubico.YubiKey.YubiHsmAuth.Commands
{
    /// <summary>
    /// The command class for changing the management key.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The management key is required when performing operations that add or
    /// delete credentials (<see cref="AddCredentialCommand"/> and
    /// <see cref="DeleteCredentialCommand"/>, respectively).
    /// </para>
    /// <para>
    /// There is a limit of 8 attempts to authenticate with the management key
    /// before the management key is blocked. Once the management key is
    /// blocked, the application must be reset before performing operations
    /// which require authentication with the management key (such as adding
    /// credentials, deleting credentials, and changing the management key).
    /// To reset the application, see <see cref="ResetApplicationCommand"/>.
    /// Supplying the correct management key before the management key is
    /// blocked will reset the retry counter to 8.
    /// </para>
    /// <para>
    /// The partner response class is <see cref="ChangeManagementKeyResponse"/>.
    /// </para>
    /// </remarks>
    public sealed class ChangeManagementKeyCommand : IYubiKeyCommand<ChangeManagementKeyResponse>
    {
        private const byte SetManagementKeyInstruction = 0x08;

        private readonly ReadOnlyMemory<byte> _currentManagementKey;
        private readonly ReadOnlyMemory<byte> _newManagementKey;

        /// <summary>
        /// The management key must be exactly 16 bytes.
        /// </summary>
        /// <remarks>
        /// The management key is supplied as an argument to the constructor
        /// <see cref="ChangeManagementKeyCommand(ReadOnlyMemory{byte}, ReadOnlyMemory{byte})"/>.
        /// </remarks>
        public const int ValidManagementKeyLength = 16;

        /// <inheritdoc/>
        public YubiKeyApplication Application => YubiKeyApplication.YubiHsmAuth;

        /// <summary>
        /// Change the management key.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The management key is required when performing operations that add or
        /// delete credentials (<see cref="AddCredentialCommand"/> and
        /// <see cref="DeleteCredentialCommand"/>, respectively).
        /// </para>
        /// <para>
        /// The caller is responsible for controlling the buffers which hold
        /// the management keys and should overwrite the data after the command
        /// is sent. The user's manual entry
        /// <xref href="UsersManualSensitive">"Sensitive Data"</xref> has further
        /// details and recommendations for handling this kind of data.
        /// </para>
        /// </remarks>
        /// <param name="currentManagementKey">
        /// The current value of the management key. The default value is all
        /// zeros.
        /// </param>
        /// <param name="newManagementKey">
        /// The new value of the management key.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown when a management key has an invalid length.
        /// </exception>
        public ChangeManagementKeyCommand(
            ReadOnlyMemory<byte> currentManagementKey,
            ReadOnlyMemory<byte> newManagementKey)
        {
            _currentManagementKey = currentManagementKey.Length == ValidManagementKeyLength
                ? currentManagementKey
                : throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.YubiHsmAuthInvalidMgmtKeyLength,
                        currentManagementKey.Length));

            _newManagementKey = newManagementKey.Length == ValidManagementKeyLength
                ? newManagementKey
                : throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.YubiHsmAuthInvalidMgmtKeyLength,
                        newManagementKey.Length));
        }

        /// <inheritdoc/>
        public CommandApdu CreateCommandApdu() =>
            new CommandApdu()
            {
                Ins = SetManagementKeyInstruction,
                Data = BuildDataField()
            };

        /// <inheritdoc/>
        public ChangeManagementKeyResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new ChangeManagementKeyResponse(responseApdu);

        /// <summary>
        /// Build the <see cref="CommandApdu.Data"/> field from the given data.
        /// </summary>
        /// <returns>
        /// Data formatted as a TLV.
        /// </returns>
        private byte[] BuildDataField()
        {
            var tlvWriter = new TlvWriter();
            tlvWriter.WriteValue(DataTagConstants.ManagementKey, _currentManagementKey.Span);
            tlvWriter.WriteValue(DataTagConstants.ManagementKey, _newManagementKey.Span);

            byte[] returnValue = tlvWriter.Encode();
            tlvWriter.Clear();

            return returnValue;
        }
    }
}
