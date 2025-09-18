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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security;
using Yubico.YubiKey.YubiHsmAuth.Commands;

namespace Yubico.YubiKey.YubiHsmAuth;

// This portion of the YubiHSM Auth Session class contains operations
// related to the management key
public partial class YubiHsmAuthSession
{
    /// <summary>
    ///     Get the number of retries remaining for the management key.
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
    ///     The number of retries, as an integer.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///     The command to retrieve the number of retries failed.
    /// </exception>
    public int GetManagementKeyRetries()
    {
        var response = Connection.SendCommand(new GetManagementKeyRetriesCommand());
        if (response.Status != ResponseStatus.Success)
        {
            throw new InvalidOperationException(response.StatusMessage);
        }

        return response.GetData();
    }

    /// <summary>
    ///     Change the management key, using the <see cref="KeyCollector" />
    ///     to retrieve the current and new management keys.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Compared to <see cref="TryChangeManagementKey(ReadOnlyMemory{byte}, ReadOnlyMemory{byte}, out int?)" />
    ///         which only attempts authentication once, this method automatically
    ///         retries authentication while there are retries remaining.
    ///     </para>
    ///     <para>
    ///         The management key is 16 bytes long, and is required when performing
    ///         operations that add or delete credentials (
    ///         <see cref="AddCredentialCommand" /> and
    ///         <see cref="DeleteCredentialCommand" />, respectively).
    ///     </para>
    ///     <para>
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
    ///         When the current and new management keys are needed, the
    ///         <see cref="KeyCollector" /> is called with <see cref="KeyEntryData.Request" />
    ///         set to <see cref="KeyEntryRequest.ChangeYubiHsmAuthManagementKey" />.
    ///         The <c>KeyCollector</c> gets the current and new management keys from the
    ///         user, saves them using
    ///         <see cref="KeyEntryData.SubmitValues(ReadOnlySpan{byte}, ReadOnlySpan{byte})" />,
    ///         and returns <c>true</c>. Each key must be exactly 16 bytes long (see
    ///         <see cref="ChangeManagementKeyCommand.ValidManagementKeyLength" />). If the
    ///         command succeeds (the management key is changed), this method returns
    ///         <c>true</c>.
    ///     </para>
    ///     <para>
    ///         If authentication with the current management key fails and there are
    ///         retries remaining, the <c>KeyCollector</c> will be called again with
    ///         the same <c>Request</c>, but <see cref="KeyEntryData.IsRetry" /> will
    ///         be <c>true</c> and <see cref="KeyEntryData.RetriesRemaining" /> will be
    ///         set appropriately. When there are no retries remaining, a
    ///         <see cref="SecurityException" /> will be thrown.
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
    ///     <c>True</c>, when the management key has been changed successfully.
    ///     <c>False</c> when the <c>KeyCollector</c> returns <c>false</c>
    ///     (usually indicating user cancellation).
    /// </returns>
    /// <exception cref="SecurityException">
    ///     Authentication failed and there are no retries remaining.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     A key collector was not supplied (<see cref="KeyCollector" /> was
    ///     null).
    /// </exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when a management key has an invalid length.
    /// </exception>
    public bool TryChangeManagementKey()
    {
        var keyCollector = GetKeyCollector();

        var keyEntryData = new KeyEntryData
        {
            Request = KeyEntryRequest.ChangeYubiHsmAuthManagementKey
        };

        try
        {
            while (keyCollector(keyEntryData))
            {
                bool managementKeyChanged = TryChangeManagementKey(
                    keyEntryData.GetCurrentValue(),
                    keyEntryData.GetNewValue(),
                    out int? retriesRemaining);

                if (managementKeyChanged)
                {
                    return true;
                }

                if (retriesRemaining.HasValue && retriesRemaining.Value > 0)
                {
                    keyEntryData.IsRetry = true;
                    keyEntryData.RetriesRemaining = retriesRemaining;
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
    ///     Change the management key.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The management key is 16 bytes long, and is required when performing
    ///         operations that add or delete credentials (
    ///         <see cref="AddCredentialCommand" /> and
    ///         <see cref="DeleteCredentialCommand" />, respectively).
    ///     </para>
    ///     <para>
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
    ///         The caller is responsible for controlling the buffers which hold
    ///         the management keys and should overwrite the data after the command
    ///         is sent. The user's manual entry
    ///         <xref href="UsersManualSensitive">"Sensitive Data"</xref> has further
    ///         details and recommendations for handling this kind of data.
    ///     </para>
    /// </remarks>
    /// <returns>
    ///     True, when the management key has been changed successfully. False,
    ///     when authentication failed and the management key was not changed.
    ///     When this method returns false, <paramref name="retriesRemaining" />
    ///     gives the number of retries remaining to authenticate with the
    ///     management key.
    /// </returns>
    /// <param name="currentManagementKey">
    ///     The current value of the management key. It must be exactly 16
    ///     bytes long (see <see cref="ChangeManagementKeyCommand.ValidManagementKeyLength" />).
    ///     The default value is all zeros.
    /// </param>
    /// <param name="newManagementKey">
    ///     The new value of the management key. It must be exactly 16
    ///     bytes long (see <see cref="ChangeManagementKeyCommand.ValidManagementKeyLength" />).
    /// </param>
    /// <param name="retriesRemaining">
    ///     When the command fails to authenticate the management key, this
    ///     value gives the number of retries remaining.
    /// </param>
    /// <exception cref="ArgumentException">
    ///     Thrown when a management key has an invalid length.
    /// </exception>
    public bool TryChangeManagementKey(
        ReadOnlyMemory<byte> currentManagementKey,
        ReadOnlyMemory<byte> newManagementKey,
        [NotNullWhen(false)] out int? retriesRemaining)
    {
        retriesRemaining = null;

        var changeMgmtKeyCmd = new ChangeManagementKeyCommand(currentManagementKey, newManagementKey);

        var changeMgmtKeyRsp =
            Connection.SendCommand(changeMgmtKeyCmd);

        if (changeMgmtKeyRsp.Status == ResponseStatus.Success)
        {
            return true;
        }

        if (changeMgmtKeyRsp.Status == ResponseStatus.AuthenticationRequired)
        {
            retriesRemaining = changeMgmtKeyRsp.RetriesRemaining!;

            return false;
        }

        // We don't expect to receive any other response statuses, but
        // just in case
        throw new InvalidOperationException(changeMgmtKeyRsp.StatusMessage);
    }

    /// <summary>
    ///     Change the management key, throw an exception if the operation failed.
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
    /// <param name="currentManagementKey">
    ///     The current value of the management key. It must be exactly 16
    ///     bytes long (see <see cref="ChangeManagementKeyCommand.ValidManagementKeyLength" />).
    ///     The default value is all zeros.
    /// </param>
    /// <param name="newManagementKey">
    ///     The new value of the management key. It must be exactly 16
    ///     bytes long (see <see cref="ChangeManagementKeyCommand.ValidManagementKeyLength" />).
    /// </param>
    /// <exception cref="SecurityException">
    ///     The <paramref name="currentManagementKey" /> was incorrect.
    /// </exception>
    public void ChangeManagementKey(
        ReadOnlyMemory<byte> currentManagementKey,
        ReadOnlyMemory<byte> newManagementKey)
    {
        if (!TryChangeManagementKey(currentManagementKey, newManagementKey, out int? retryCount))
        {
            throw new SecurityException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.YubiHsmAuthMgmtKeyAuthFailed,
                    retryCount));
        }
    }
}
