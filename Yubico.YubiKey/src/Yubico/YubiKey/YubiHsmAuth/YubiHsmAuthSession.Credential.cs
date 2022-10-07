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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Security;
using Yubico.YubiKey.YubiHsmAuth.Commands;

namespace Yubico.YubiKey.YubiHsmAuth
{
    // This portion of the YubiHSM Auth Session class contains operations
    // related to credentials
    public partial class YubiHsmAuthSession
    {
        /// <summary>
        /// Add a credential.
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
        /// <param name="managementKey">
        /// The secret used to authenticate to the application prior to adding
        /// or removing credentials. It must be exactly 16 bytes long (see
        /// <see cref="ChangeManagementKeyCommand.ValidManagementKeyLength"/>).
        /// </param>
        /// <param name="credentialWithSecrets">
        /// The credential to be added.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Either a credential with that label
        /// already exists, or there is no space to add the credential.
        /// </exception>
        /// <exception cref="SecurityException">
        /// Authentication with the management key failed.
        /// </exception>
        public void AddCredential(
            ReadOnlyMemory<byte> managementKey,
            CredentialWithSecrets credentialWithSecrets)
        {
            bool success = TryAddCredential(managementKey, credentialWithSecrets, out int? mgmtKeyRetries);

            if (!success)
            {
                throw new SecurityException(string.Format(
                                CultureInfo.CurrentCulture,
                                ExceptionMessages.YubiHsmAuthMgmtKeyAuthFailed,
                                mgmtKeyRetries));
            }
        }

        /// <summary>
        /// Add a credential.
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
        /// True, when the credential has been added successfully. False,
        /// when authentication with the management key failed.
        /// When this method returns false, <paramref name="managementKeyRetries"/>
        /// gives the number of retries remaining to authenticate with the
        /// management key.
        /// </returns>
        /// <param name="managementKey">
        /// The secret used to authenticate to the application prior to adding
        /// or removing credentials. It must be exactly 16 bytes long (see
        /// <see cref="ChangeManagementKeyCommand.ValidManagementKeyLength"/>).
        /// </param>
        /// <param name="credentialWithSecrets">
        /// The credential to be added.
        /// </param>
        /// <param name="managementKeyRetries">
        /// When the command fails to authenticate the management key, this
        /// value gives the number of retries remaining.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Either a credential with that label
        /// already exists, or there is no space to add the credential.
        /// </exception>
        public bool TryAddCredential(
            ReadOnlyMemory<byte> managementKey,
            CredentialWithSecrets credentialWithSecrets,
            [NotNullWhen(false)] out int? managementKeyRetries
            )
        {
            managementKeyRetries = null;

            AddCredentialCommand addCredCmd =
                new AddCredentialCommand(managementKey, credentialWithSecrets);
            AddCredentialResponse addCredRsp = Connection.SendCommand(addCredCmd);

            if (addCredRsp.Status != ResponseStatus.Success)
            {
                if (addCredRsp.Status == ResponseStatus.AuthenticationRequired)
                {
                    managementKeyRetries = addCredRsp.RetriesRemaining!;
                    return false;
                }
                else
                {
                    throw new InvalidOperationException(addCredRsp.StatusMessage);
                }
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Remove a credential.
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
        /// <param name="managementKey">
        /// The secret used to authenticate to the application prior to adding
        /// or removing credentials. It must be exactly 16 bytes long (see
        /// <see cref="ChangeManagementKeyCommand.ValidManagementKeyLength"/>).
        /// </param>
        /// <param name="label">
        /// The label of the credential to be deleted. The string must meet the
        /// same requirements as <see cref="Credential.Label"/>.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// The credential was not found.
        /// </exception>
        /// <exception cref="SecurityException">
        /// Authentication with the management key failed.
        /// </exception>
        public void DeleteCredential(
            ReadOnlyMemory<byte> managementKey,
            string label)
        {
            bool success = TryDeleteCredential(managementKey, label, out int? mgmtKeyRetries);

            if (!success)
            {
                throw new SecurityException(string.Format(
                                CultureInfo.CurrentCulture,
                                ExceptionMessages.YubiHsmAuthMgmtKeyAuthFailed,
                                mgmtKeyRetries));
            }
        }

        /// <summary>
        /// Remove a credential.
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
        /// <param name="managementKey">
        /// The secret used to authenticate to the application prior to adding
        /// or removing credentials. It must be exactly 16 bytes long (see
        /// <see cref="ChangeManagementKeyCommand.ValidManagementKeyLength"/>).
        /// </param>
        /// <param name="label">
        /// The label of the credential to be deleted. The string must meet the
        /// same requirements as <see cref="Credential.Label"/>.
        /// </param>
        /// <param name="managementKeyRetries">
        /// When the command fails to authenticate the management key, this
        /// value gives the number of retries remaining.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// The credential was not found.
        /// </exception>
        public bool TryDeleteCredential(
            ReadOnlyMemory<byte> managementKey,
            string label,
            [NotNullWhen(false)] out int? managementKeyRetries)
        {
            managementKeyRetries = null;

            DeleteCredentialCommand deleteCredCmd =
                new DeleteCredentialCommand(managementKey, label);
            DeleteCredentialResponse deleteCredRsp =
                Connection.SendCommand(deleteCredCmd);

            if (deleteCredRsp.Status != ResponseStatus.Success)
            {
                if (deleteCredRsp.Status == ResponseStatus.AuthenticationRequired)
                {
                    managementKeyRetries = deleteCredRsp.RetriesRemaining!;
                    return false;
                }
                else
                {
                    throw new InvalidOperationException(deleteCredRsp.StatusMessage);
                }
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Get the public properties of all <see cref="Credential"/>s in the
        /// YubiHSM Auth application, along with the number of retries remaining
        /// for each.
        /// </summary>
        /// <returns>
        /// A list of credentials and the number of retries remaining for each
        /// credential's password.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Failed to retrieve the list of credentials present in the
        /// application.
        /// </exception>
        public IReadOnlyList<CredentialRetryPair> ListCredentials()
        {
            ListCredentialsResponse listCredsRsp =
                Connection.SendCommand(new ListCredentialsCommand());

            if (listCredsRsp.Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(listCredsRsp.StatusMessage);
            }

            return listCredsRsp.GetData();
        }
    }
}
