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
using System.Globalization;
using System.Linq;
using System.Security;
using Yubico.YubiKey.Oath.Commands;

namespace Yubico.YubiKey.Oath
{
    // This portion of the OathSession class contains operations related to
    // authentication.
    public sealed partial class OathSession : IDisposable
    {
        /// <summary>
        /// Attempts to verify the password.
        /// </summary>
        /// <remarks>
        /// Performs mutual authentication with the YubiKey using the password collected by the key collector.
        /// You need to verify the password only once per session if the authentication is configured
        /// for the OATH application on the YubiKey.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// There is no <c>KeyCollector</c> loaded.
        /// </exception>
        public bool TryVerifyPassword()
        {
            EnsureKeyCollector();

            bool passwordVerified = false;

            var keyEntryData = new KeyEntryData()
            {
                Request = KeyEntryRequest.VerifyOathPassword,
            };

            try
            {
                if (KeyCollector!(keyEntryData) == true) {

                    ReadOnlyMemory<byte> password = keyEntryData.GetCurrentValue();
                    var validateCommand = new ValidateCommand(password, _oathData);
                    ValidateResponse verifyResponse = Connection.SendCommand(validateCommand);

                    if (verifyResponse.Status == ResponseStatus.Success)
                    {
                        passwordVerified = verifyResponse.GetData();
                    }
                }
            }
            finally
            {
                keyEntryData.Clear();

                keyEntryData.Request = KeyEntryRequest.Release;
                _ = KeyCollector!(keyEntryData);
            }

            return passwordVerified;
        }

        /// <summary>
        /// Verify the password, throw an exception if the user cancels or the verification failed.
        /// </summary>
        /// <remarks>
        /// This is the same as <c>TryVerifyPassword</c>, except this method will throw an exception
        /// if the <c>KeyCollector</c> indicates user cancellation or the verification failed due to
        /// the authentication is not enabled for the YubiKey or the incorrect password was provided.
        /// <para>
        /// See the <see cref="TryVerifyPassword"/> method for further documentation
        /// on this method.
        /// </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// There is no <c>KeyCollector</c> loaded.
        /// </exception>
        /// <exception cref="SecurityException">
        /// Unable to verify password either because <c>KeyCollector</c> was canceled by the user or
        /// the authentication is not enabled on the YubiKey or the incorrect password was provided.
        /// </exception>
        public void VerifyPassword()
        {
            if (TryVerifyPassword() == false)
            {
                throw new SecurityException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.UnableToVerifyOathPassword));
            }
        }

        /// <summary>
        /// Sets the password.
        /// </summary>
        /// <remarks>
        /// If the authentication was previously configured on the YubiKey, this method will prompt for
        /// the current password to verify, as well as a new password to change to using the KeyCollector callback.
        /// If the authentication is not configured, this method will collect only a new password to set.
        /// In this case, the challenge supplied from the YubiKey will be an empty byte array meaning
        /// no password was set yet.
        /// The password can be any string of bytes, however most applications will choose to encode
        /// a user supplied string using UTF-8. Next, 1,000 rounds of PBKDF2 are applied with a salt
        /// supplied by the YubiKey, ensuring an extra level of security against brute force attacks.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// There is no <c>KeyCollector</c> loaded, the new password to set is incorrect,
        /// or the <c>SetPasswordCommand</c> failed.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// The user canceled password collection.
        /// </exception>
        /// <exception cref="SecurityException">
        /// Unable to verify password because the incorrect current password was provided.
        /// </exception>
        public void SetPassword()
        {
            EnsureKeyCollector();

            var keyEntryData = new KeyEntryData()
            {
                Request = KeyEntryRequest.SetOathPassword,
            };

            try
            {
                if (KeyCollector!(keyEntryData) == true)
                {
                    ReadOnlyMemory<byte> currentPassword = keyEntryData.GetCurrentValue();
                    ReadOnlyMemory<byte> newPassword = keyEntryData.GetNewValue();

                    if (currentPassword.Span.SequenceEqual(newPassword.Span))
                    {
                        throw new InvalidOperationException(ExceptionMessages.IncorrectOathNewPassword);
                    }

                    if (newPassword.IsEmpty)
                    {
                        throw new InvalidOperationException(ExceptionMessages.InvalidOathPassword);
                    }

                    var validateCommand = new ValidateCommand(currentPassword, _oathData);
                    ValidateResponse verifyResponse = Connection.SendCommand(validateCommand);

                    if (verifyResponse.StatusMessage == ResponseStatusMessages.OathAuthNotEnabled ||
                        (verifyResponse.Status == ResponseStatus.Success && verifyResponse.GetData() == true))
                    {
                        SetPassword(newPassword);
                    }
                    else
                    {
                        throw new SecurityException(
                            string.Format(
                                CultureInfo.CurrentCulture,
                                ExceptionMessages.UnableToVerifyOathPassword));
                    }
                }
                else
                {
                    throw new OperationCanceledException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.IncompleteCommandInput));
                }
            }
            finally
            {
                keyEntryData.Clear();

                keyEntryData.Request = KeyEntryRequest.Release;
                _ = KeyCollector!(keyEntryData);
            }
        }

        /// <summary>
        /// Unsets the password.
        /// </summary>
        /// <remarks>
        /// Removes the authentication.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// There is no <c>KeyCollector</c> loaded. Or the <c>SetPasswordCommand</c> failed.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// The user canceled password collection.
        /// </exception>
        /// <exception cref="SecurityException">
        /// Unable to verify password either because the authentication is not enabled on the YubiKey or
        /// the incorrect password was provided.
        /// </exception>
        public void UnsetPassword()
        {
            EnsureKeyCollector();

            var keyEntryData = new KeyEntryData()
            {
                Request = KeyEntryRequest.VerifyOathPassword,
            };

            try
            {
                if (KeyCollector!(keyEntryData) == true)
                {
                    ReadOnlyMemory<byte> currentPassword = keyEntryData.GetCurrentValue();
                    var validateCommand = new ValidateCommand(currentPassword, _oathData);
                    ValidateResponse verifyResponse = Connection.SendCommand(validateCommand);

                    if (verifyResponse.StatusMessage == ResponseStatusMessages.OathAuthNotEnabled)
                    {
                        throw new SecurityException(verifyResponse.StatusMessage);
                    }
                    else if (verifyResponse.Status == ResponseStatus.Success && verifyResponse.GetData() == true)
                    {
                        SetPassword(Array.Empty<byte>());
                    }
                    else
                    {
                        throw new SecurityException(
                            string.Format(
                                CultureInfo.CurrentCulture,
                                ExceptionMessages.UnableToVerifyOathPassword));
                    }
                }
                else
                {
                    throw new OperationCanceledException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.IncompleteCommandInput));
                }
            }
            finally
            {
                keyEntryData.Clear();

                keyEntryData.Request = KeyEntryRequest.Release;
                _ = KeyCollector!(keyEntryData);
            }
        }

        // Sends the <c>SetPasswordCommand</c> to the YubiKey, throws if the command failed.
        private void SetPassword(ReadOnlyMemory<byte> password)
        {
            var setPasswordCommand = new SetPasswordCommand(password, _oathData);
            SetPasswordResponse setPasswordResponse = Connection.SendCommand(setPasswordCommand);

            if (setPasswordResponse.Status == ResponseStatus.Success)
            {
                SelectOathResponse response = Connection.SendCommand(new SelectOathCommand());
                _oathData = response.GetData();
            }
            else
            {
                throw new InvalidOperationException(setPasswordResponse.StatusMessage);
            }
        }
    }
}
