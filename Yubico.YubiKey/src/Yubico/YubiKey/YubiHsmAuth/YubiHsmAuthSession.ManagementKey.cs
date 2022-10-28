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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security;
using Yubico.YubiKey.YubiHsmAuth.Commands;

namespace Yubico.YubiKey.YubiHsmAuth
{
    // This portion of the YubiHSM Auth Session class contains operations
    // related to the management key
    public partial class YubiHsmAuthSession
    {
        /// <summary>
        /// Get the number of retries remaining for the management key.
        /// </summary>
        /// <remarks>
        /// There is a limit of 8 attempts to authenticate with the management key
        /// before the management key is blocked. Once the management key is
        /// blocked, the application must be reset before performing operations
        /// which require authentication with the management key (such as adding
        /// credentials, deleting credentials, and changing the management key).
        /// To reset the application, see <see cref="ResetApplication"/>.
        /// Supplying the correct management key before the management key is
        /// blocked will reset the retry counter to 8.
        /// </remarks>
        /// <returns>
        /// The number of retries, as an integer.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// The command to retrieve the number of retries failed.
        /// </exception>
        public int GetManagementKeyRetries()
        {
            GetManagementKeyRetriesResponse retryCountResponse =
                Connection.SendCommand(new GetManagementKeyRetriesCommand());

            if (retryCountResponse.Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(retryCountResponse.StatusMessage);
            }

            return retryCountResponse.GetData();
        }

        /// <summary>
        /// Change the management key.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The management key is 16 bytes long, and is required when performing
        /// operations that add or delete credentials (
        /// <see cref="AddCredentialCommand"/> and
        /// <see cref="DeleteCredentialCommand"/>, respectively).
        /// </para>
        /// <para>
        /// There is a limit of 8 attempts to authenticate with the management key
        /// before the management key is blocked. Once the management key is
        /// blocked, the application must be reset before performing operations
        /// which require authentication with the management key (such as adding
        /// credentials, deleting credentials, and changing the management key).
        /// To reset the application, see <see cref="ResetApplication"/>.
        /// Supplying the correct management key before the management key is
        /// blocked will reset the retry counter to 8.
        /// </para>
        /// <para>
        /// The caller is responsible for controlling the buffers which hold
        /// the management keys and should overwrite the data after the command
        /// is sent. The user's manual entry
        /// <xref href="UsersManualSensitive">"Sensitive Data"</xref> has further
        /// details and recommendations for handling this kind of data.
        /// </para>
        /// </remarks>
        /// <returns>
        /// True, when the management key has been changed successfully. False,
        /// when authentication failed and the management key was not changed.
        /// When this method returns false, <paramref name="retriesRemaining"/>
        /// gives the number of retries remaining to authenticate with the
        /// management key.
        /// </returns>
        /// <param name="currentManagementKey">
        /// The current value of the management key. It must be exactly 16
        /// bytes long (see <see cref="ChangeManagementKeyCommand.ValidManagementKeyLength"/>).
        /// The default value is all zeros.
        /// </param>
        /// <param name="newManagementKey">
        /// The new value of the management key. It must be exactly 16
        /// bytes long (see <see cref="ChangeManagementKeyCommand.ValidManagementKeyLength"/>).
        /// </param>
        /// <param name="retriesRemaining">
        /// When the command fails to authenticate the management key, this
        /// value gives the number of retries remaining.
        /// </param>
        public bool TryChangeManagementKey(
            ReadOnlyMemory<byte> currentManagementKey,
            ReadOnlyMemory<byte> newManagementKey,
            [NotNullWhen(false)]
            out int? retriesRemaining)
        {
            retriesRemaining = null;

            ChangeManagementKeyCommand changeMgmtKeyCmd =
                new ChangeManagementKeyCommand(currentManagementKey, newManagementKey);
            ChangeManagementKeyResponse changeMgmtKeyRsp =
                Connection.SendCommand(changeMgmtKeyCmd);

            if (changeMgmtKeyRsp.Status == ResponseStatus.Success)
            {
                return true;
            }
            else if (changeMgmtKeyRsp.Status == ResponseStatus.AuthenticationRequired)
            {
                retriesRemaining = changeMgmtKeyRsp.RetriesRemaining!;
                return false;
            }
            else
            {
                // We don't expect to receive any other response statuses, but
                // just in case
                throw new InvalidOperationException(changeMgmtKeyRsp.StatusMessage);
            }
        }

        /// <summary>
        /// Change the management key, throw an exception if the operation failed.
        /// </summary>
        /// <remarks>
        /// There is a limit of 8 attempts to authenticate with the management key
        /// before the management key is blocked. Once the management key is
        /// blocked, the application must be reset before performing operations
        /// which require authentication with the management key (such as adding
        /// credentials, deleting credentials, and changing the management key).
        /// To reset the application, see <see cref="ResetApplication"/>.
        /// Supplying the correct management key before the management key is
        /// blocked will reset the retry counter to 8.
        /// </remarks>
        /// <param name="currentManagementKey">
        /// The current value of the management key. It must be exactly 16
        /// bytes long (see <see cref="ChangeManagementKeyCommand.ValidManagementKeyLength"/>).
        /// The default value is all zeros.
        /// </param>
        /// <param name="newManagementKey">
        /// The new value of the management key. It must be exactly 16
        /// bytes long (see <see cref="ChangeManagementKeyCommand.ValidManagementKeyLength"/>).
        /// </param>
        /// <exception cref="SecurityException">
        /// The <paramref name="currentManagementKey"/> was incorrect.
        /// </exception>
        public void ChangeManagementKey(
            ReadOnlyMemory<byte> currentManagementKey,
            ReadOnlyMemory<byte> newManagementKey)
        {
            if (!TryChangeManagementKey(currentManagementKey, newManagementKey, out int? retryCount))
            {
                throw new SecurityException(string.Format(
                                CultureInfo.CurrentCulture,
                                ExceptionMessages.YubiHsmAuthMgmtKeyAuthFailed,
                                retryCount));
            }
        }
    }
}
