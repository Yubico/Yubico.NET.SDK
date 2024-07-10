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
using System.Security;
using Yubico.YubiKey.YubiHsmAuth.Commands;

namespace Yubico.YubiKey.YubiHsmAuth
{
    // This portion of the YubiHSM Auth Session class contains operations
    // related to credentials
    public partial class YubiHsmAuthSession
    {
        /// <summary>
        ///     Add a credential.
        /// </summary>
        /// <remarks>
        ///     There is a limit of 8 attempts to authenticate with the management key
        ///     before the management key is blocked. Once the management key is
        ///     blocked, the application must be reset before performing operations
        ///     which require authentication with the management key (such as adding
        ///     credentials, deleting credentials, and changing the management key).
        ///     To reset the application, see <see cref="ResetApplication" />.
        ///     Supplying the correct management key before the management key is
        ///     blocked will reset the retry counter to 8.
        /// </remarks>
        /// <param name="managementKey">
        ///     The secret used to authenticate to the application prior to adding
        ///     or removing credentials. It must be exactly 16 bytes long (see
        ///     <see cref="ChangeManagementKeyCommand.ValidManagementKeyLength" />).
        /// </param>
        /// <param name="credentialWithSecrets">
        ///     The credential to be added.
        /// </param>
        /// <exception cref="InvalidOperationException">
        ///     Either a credential with that label
        ///     already exists, or there is no space to add the credential.
        /// </exception>
        /// <exception cref="SecurityException">
        ///     Authentication with the management key failed.
        /// </exception>
        public void AddCredential(
            ReadOnlyMemory<byte> managementKey,
            CredentialWithSecrets credentialWithSecrets)
        {
            bool success = TryAddCredential(managementKey, credentialWithSecrets, out int? mgmtKeyRetries);

            if (!success)
            {
                throw new SecurityException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.YubiHsmAuthMgmtKeyAuthFailed,
                        mgmtKeyRetries));
            }
        }

        /// <summary>
        ///     Add a credential.
        /// </summary>
        /// <remarks>
        ///     There is a limit of 8 attempts to authenticate with the management key
        ///     before the management key is blocked. Once the management key is
        ///     blocked, the application must be reset before performing operations
        ///     which require authentication with the management key (such as adding
        ///     credentials, deleting credentials, and changing the management key).
        ///     To reset the application, see <see cref="ResetApplication" />.
        ///     Supplying the correct management key before the management key is
        ///     blocked will reset the retry counter to 8.
        /// </remarks>
        /// <returns>
        ///     True, when the credential has been added successfully. False,
        ///     when authentication with the management key failed.
        ///     When this method returns false, <paramref name="managementKeyRetries" />
        ///     gives the number of retries remaining to authenticate with the
        ///     management key.
        /// </returns>
        /// <param name="managementKey">
        ///     The secret used to authenticate to the application prior to adding
        ///     or removing credentials. It must be exactly 16 bytes long (see
        ///     <see cref="ChangeManagementKeyCommand.ValidManagementKeyLength" />).
        /// </param>
        /// <param name="credentialWithSecrets">
        ///     The credential to be added.
        /// </param>
        /// <param name="managementKeyRetries">
        ///     When the command fails to authenticate the management key, this
        ///     value gives the number of retries remaining.
        /// </param>
        /// <exception cref="InvalidOperationException">
        ///     Either a credential with that label
        ///     already exists, or there is no space to add the credential.
        /// </exception>
        public bool TryAddCredential(
            ReadOnlyMemory<byte> managementKey,
            CredentialWithSecrets credentialWithSecrets,
            [NotNullWhen(false)] out int? managementKeyRetries)
        {
            managementKeyRetries = null;

            var addCredCmd =
                new AddCredentialCommand(managementKey, credentialWithSecrets);

            AddCredentialResponse addCredRsp = Connection.SendCommand(addCredCmd);

            if (addCredRsp.Status != ResponseStatus.Success)
            {
                if (addCredRsp.Status == ResponseStatus.AuthenticationRequired)
                {
                    managementKeyRetries = addCredRsp.RetriesRemaining!;

                    return false;
                }

                throw new InvalidOperationException(addCredRsp.StatusMessage);
            }

            return true;
        }

        /// <summary>
        ///     Add a credential. This method uses the <see cref="KeyCollector" />
        ///     to retrieve the management key and will retry authentication
        ///     while there are retries remaining.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Compared to <see cref="TryAddCredential(ReadOnlyMemory{byte}, CredentialWithSecrets, out int?)" />
        ///         and <see cref="AddCredential(ReadOnlyMemory{byte}, CredentialWithSecrets)" />
        ///         which only attempt authentication once, this method
        ///         automatically retries authentication while there are retries remaining.
        ///     </para>
        ///     <para>
        ///         The management key is used to authenticate to the application prior to
        ///         operations such as adding or removing credentials. It must be exactly
        ///         16 bytes long (see
        ///         <see cref="ChangeManagementKeyCommand.ValidManagementKeyLength" />).
        ///         There is a limit of 8 attempts to authenticate with the management key
        ///         before the management key is blocked. Once the management key is
        ///         blocked, the application must be reset before performing operations
        ///         which require authentication with the management key (such as adding
        ///         credentials, deleting credentials, and changing the management key).
        ///         To reset the application, see <see cref="ResetApplication" />.
        ///         Supplying the correct management key before the management key is
        ///         blocked will reset the retry counter to 8.
        ///     </para>
        ///     <para>
        ///         When the management key is needed, the <see cref="KeyCollector" /> is
        ///         called with <see cref="KeyEntryData.Request" /> set to
        ///         <see cref="KeyEntryRequest.AuthenticateYubiHsmAuthManagementKey" />.
        ///         The <c>KeyCollector</c> gets the management key from the user,
        ///         saves it using <see cref="KeyEntryData.SubmitValue(ReadOnlySpan{byte})" />,
        ///         and returns <c>true</c>. If the command succeeds (the credential is
        ///         removed), this method returns <c>true</c>.
        ///     </para>
        ///     <para>
        ///         If authentication fails and there are retries remaining, the
        ///         <c>KeyCollector</c> will be called again with the same <c>Request</c>,
        ///         but <see cref="KeyEntryData.IsRetry" /> will be <c>true</c> and
        ///         <see cref="KeyEntryData.RetriesRemaining" /> will be set appropriately.
        ///         When there are no retries remaining, a <see cref="SecurityException" />
        ///         will be thrown.
        ///     </para>
        ///     <para>
        ///         The only time this method returns <c>false</c> is when the
        ///         <c>KeyCollector</c> cancels the operation by returning <c>false</c>.
        ///         Cancellation usually happens when the user has clicked a "Cancel"
        ///         button.
        ///     </para>
        ///     <para>
        ///         In all situations, when this method ends, it will tell the
        ///         <c>KeyCollector</c> it is done by calling it with the <c>Request</c>
        ///         set to <see cref="KeyEntryRequest.Release" />.
        ///     </para>
        /// </remarks>
        /// <param name="credentialWithSecrets">
        ///     The credential to be added.
        /// </param>
        /// <returns>
        ///     <c>True</c> when the credential was successfully added.
        ///     <c>False</c> when the <c>KeyCollector</c> returns <c>false</c>
        ///     (usually indicating user cancellation).
        /// </returns>
        public bool TryAddCredential(CredentialWithSecrets credentialWithSecrets)
        {
            Func<KeyEntryData, bool>? keyCollector = GetKeyCollector();

            var keyEntryData = new KeyEntryData
            {
                Request = KeyEntryRequest.AuthenticateYubiHsmAuthManagementKey
            };

            try
            {
                while (keyCollector(keyEntryData))
                {
                    bool credentialAdded =
                        TryAddCredential(
                            keyEntryData.GetCurrentValue(),
                            credentialWithSecrets,
                            out int? managementKeyRetries);

                    // Command succeeded
                    if (credentialAdded)
                    {
                        return true;
                    }

                    // Command failed. Retry if possible, otherwise throw exception.
                    if (managementKeyRetries.HasValue && managementKeyRetries.Value > 0)
                    {
                        keyEntryData.IsRetry = true;
                        keyEntryData.RetriesRemaining = managementKeyRetries;
                    }
                    else
                    {
                        throw new SecurityException(
                            string.Format(
                                CultureInfo.CurrentCulture,
                                ExceptionMessages.NoMoreRetriesRemaining));
                    }
                }
            }
            finally
            {
                keyEntryData.Clear();

                keyEntryData.Request = KeyEntryRequest.Release;
                _ = keyCollector(keyEntryData);
            }

            return false;
        }

        /// <summary>
        ///     Remove a credential. This method uses the <see cref="KeyCollector" />
        ///     to retrieve the management key, and will retry authentication
        ///     while there are retries remaining.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Compared to <see cref="TryDeleteCredential(ReadOnlyMemory{byte}, string, out int?)" />
        ///         and <see cref="DeleteCredential(ReadOnlyMemory{byte}, string)" />
        ///         which only attempt authentication once, this method
        ///         automatically retries authentication while there are retries remaining.
        ///     </para>
        ///     <para>
        ///         The management key is used to authenticate to the application prior to
        ///         operations such as adding or removing credentials. It must be exactly
        ///         16 bytes long (see
        ///         <see cref="ChangeManagementKeyCommand.ValidManagementKeyLength" />).
        ///         There is a limit of 8 attempts to authenticate with the management key
        ///         before the management key is blocked. Once the management key is
        ///         blocked, the application must be reset before performing operations
        ///         which require authentication with the management key (such as adding
        ///         credentials, deleting credentials, and changing the management key).
        ///         To reset the application, see <see cref="ResetApplication" />.
        ///         Supplying the correct management key before the management key is
        ///         blocked will reset the retry counter to 8.
        ///     </para>
        ///     <para>
        ///         When the management key is needed, the <see cref="KeyCollector" /> is
        ///         called with <see cref="KeyEntryData.Request" /> set to
        ///         <see cref="KeyEntryRequest.AuthenticateYubiHsmAuthManagementKey" />.
        ///         The <c>KeyCollector</c> gets the management key from the user,
        ///         saves it using <see cref="KeyEntryData.SubmitValue(ReadOnlySpan{byte})" />,
        ///         and returns <c>true</c>. If the command succeeds (the credential is
        ///         removed), this method returns <c>true</c>.
        ///     </para>
        ///     <para>
        ///         If authentication fails and there are retries remaining, the
        ///         <c>KeyCollector</c> will be called again with the same <c>Request</c>,
        ///         but <see cref="KeyEntryData.IsRetry" /> will be <c>true</c> and
        ///         <see cref="KeyEntryData.RetriesRemaining" /> will be set appropriately.
        ///         When there are no retries remaining, a <see cref="SecurityException" />
        ///         will be thrown.
        ///     </para>
        ///     <para>
        ///         The only time this method returns <c>false</c> is when the
        ///         <c>KeyCollector</c> cancels the operation by returning <c>false</c>.
        ///         Cancellation usually happens when the user has clicked a "Cancel"
        ///         button.
        ///     </para>
        ///     <para>
        ///         In all situations, when this method ends, it will tell the
        ///         <c>KeyCollector</c> it is done by calling it with the <c>Request</c>
        ///         set to <see cref="KeyEntryRequest.Release" />.
        ///     </para>
        /// </remarks>
        /// <returns>
        ///     <c>True</c> when the credential was successfully removed.
        ///     <c>False</c> when the <c>KeyCollector</c> returns <c>false</c>
        ///     (usually indicating user cancellation).
        /// </returns>
        /// <param name="label">
        ///     The label of the credential to be deleted. The string must meet the
        ///     same requirements as <see cref="Credential.Label" />.
        /// </param>
        /// <exception cref="InvalidOperationException">
        ///     The <see cref="KeyCollector" /> is <c>null</c> or the credential was
        ///     not found.
        /// </exception>
        /// <exception cref="SecurityException">
        ///     Authentication failed and there are no retries remaining.
        /// </exception>
        public bool TryDeleteCredential(string label)
        {
            Func<KeyEntryData, bool>? keyCollector = GetKeyCollector();

            var keyEntryData = new KeyEntryData
            {
                Request = KeyEntryRequest.AuthenticateYubiHsmAuthManagementKey
            };

            try
            {
                while (keyCollector(keyEntryData))
                {
                    bool credentialDeleted =
                        TryDeleteCredential(
                            keyEntryData.GetCurrentValue(),
                            label,
                            out int? managementKeyRetries);

                    // Command succeeded
                    if (credentialDeleted)
                    {
                        return true;
                    }

                    // Command failed. Retry if possible, otherwise throw exception.
                    if (managementKeyRetries.HasValue && managementKeyRetries.Value > 0)
                    {
                        keyEntryData.IsRetry = true;
                        keyEntryData.RetriesRemaining = managementKeyRetries;
                    }
                    else
                    {
                        throw new SecurityException(
                            string.Format(
                                CultureInfo.CurrentCulture,
                                ExceptionMessages.NoMoreRetriesRemaining));
                    }
                }
            }
            finally
            {
                keyEntryData.Clear();

                keyEntryData.Request = KeyEntryRequest.Release;
                _ = keyCollector(keyEntryData);
            }

            return false;
        }

        /// <summary>
        ///     Remove a credential.
        /// </summary>
        /// <remarks>
        ///     There is a limit of 8 attempts to authenticate with the management key
        ///     before the management key is blocked. Once the management key is
        ///     blocked, the application must be reset before performing operations
        ///     which require authentication with the management key (such as adding
        ///     credentials, deleting credentials, and changing the management key).
        ///     To reset the application, see <see cref="ResetApplication" />.
        ///     Supplying the correct management key before the management key is
        ///     blocked will reset the retry counter to 8.
        /// </remarks>
        /// <param name="managementKey">
        ///     The secret used to authenticate to the application prior to adding
        ///     or removing credentials. It must be exactly 16 bytes long (see
        ///     <see cref="ChangeManagementKeyCommand.ValidManagementKeyLength" />).
        /// </param>
        /// <param name="label">
        ///     The label of the credential to be deleted. The string must meet the
        ///     same requirements as <see cref="Credential.Label" />.
        /// </param>
        /// <exception cref="InvalidOperationException">
        ///     The credential was not found.
        /// </exception>
        /// <exception cref="SecurityException">
        ///     Authentication with the management key failed.
        /// </exception>
        public void DeleteCredential(
            ReadOnlyMemory<byte> managementKey,
            string label)
        {
            bool success = TryDeleteCredential(managementKey, label, out int? mgmtKeyRetries);

            if (!success)
            {
                throw new SecurityException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.YubiHsmAuthMgmtKeyAuthFailed,
                        mgmtKeyRetries));
            }
        }

        /// <summary>
        ///     Remove a credential.
        /// </summary>
        /// <remarks>
        ///     There is a limit of 8 attempts to authenticate with the management key
        ///     before the management key is blocked. Once the management key is
        ///     blocked, the application must be reset before performing operations
        ///     which require authentication with the management key (such as adding
        ///     credentials, deleting credentials, and changing the management key).
        ///     To reset the application, see <see cref="ResetApplication" />.
        ///     Supplying the correct management key before the management key is
        ///     blocked will reset the retry counter to 8.
        /// </remarks>
        /// <param name="managementKey">
        ///     The secret used to authenticate to the application prior to adding
        ///     or removing credentials. It must be exactly 16 bytes long (see
        ///     <see cref="ChangeManagementKeyCommand.ValidManagementKeyLength" />).
        /// </param>
        /// <param name="label">
        ///     The label of the credential to be deleted. The string must meet the
        ///     same requirements as <see cref="Credential.Label" />.
        /// </param>
        /// <param name="managementKeyRetries">
        ///     When the command fails to authenticate the management key, this
        ///     value gives the number of retries remaining.
        /// </param>
        /// <exception cref="InvalidOperationException">
        ///     The credential was not found.
        /// </exception>
        public bool TryDeleteCredential(
            ReadOnlyMemory<byte> managementKey,
            string label,
            [NotNullWhen(false)] out int? managementKeyRetries)
        {
            managementKeyRetries = null;

            var deleteCredCmd =
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

                throw new InvalidOperationException(deleteCredRsp.StatusMessage);
            }

            return true;
        }

        /// <summary>
        ///     Get the public properties of all <see cref="Credential" />s in the
        ///     YubiHSM Auth application, along with the number of retries remaining
        ///     for each.
        /// </summary>
        /// <returns>
        ///     A list of credentials and the number of retries remaining for each
        ///     credential's password.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///     Failed to retrieve the list of credentials present in the
        ///     application.
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
