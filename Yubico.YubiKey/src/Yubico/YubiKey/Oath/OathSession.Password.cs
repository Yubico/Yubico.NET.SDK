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
using System.Security;
using Yubico.Core.Iso7816;
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
                Request = KeyEntryRequest.VerifyOathPassword
            };

            try
            {
                if (KeyCollector!(keyEntryData))
                {
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
        /// See the <see cref="TryVerifyPassword()"/> method for further documentation
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
        /// Try to verify using the given password.
        /// </summary>
        /// <remarks>
        /// Perform mutual authentication with the YubiKey using the password
        /// given. You need to verify the password only once per session if
        /// authentication is enabled for the OATH application on the YubiKey.
        /// <para>
        /// If the OATH application is not password-protected
        /// (<c>IsPasswordProtected</c> is <c>false</c>), the given password
        /// obviously does not verify, so the method will return <c>false</c>.
        /// </para>
        /// <para>
        /// If the password has already been verified during this session, this
        /// method will try to verify it again. If it fails, the return will be
        /// <c>false</c>, but the session will still be verified and it will
        /// still be able to perform operations.
        /// </para>
        /// </remarks>
        /// <param name="password">
        /// The password to verify.
        /// </param>
        /// <returns>
        /// A boolean, <c>true</c> if the password verifies, and <c>false</c>
        /// otherwise.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// The YubiKey had some error, such as unreliable connection.
        /// </exception>
        public bool TryVerifyPassword(ReadOnlyMemory<byte> password)
        {
            var validateCommand = new ValidateCommand(password, _oathData);
            ValidateResponse verifyResponse = Connection.SendCommand(validateCommand);

            if (verifyResponse.Status == ResponseStatus.Success)
            {
                return verifyResponse.GetData();
            }

            if (verifyResponse.StatusWord == SWConstants.InvalidCommandDataParameter
                || verifyResponse.StatusWord == SWConstants.ReferenceDataUnusable)
            {
                return false;
            }

            // If the response was anything else, that is an error.
            throw new InvalidOperationException(verifyResponse.StatusMessage);
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
                Request = KeyEntryRequest.SetOathPassword
            };

            try
            {
                if (KeyCollector!(keyEntryData))
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

                    if (!IsPasswordProtected)
                    {
                        currentPassword = ReadOnlyMemory<byte>.Empty;
                    }

                    if (!TrySetPassword(currentPassword, newPassword))
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
        /// Verify the <c>currentPassword</c> in order to set the OATH
        /// application in the YubiKey to be password-protected with the given
        /// <c>newPassword</c>.
        /// </summary>
        /// <remarks>
        /// If the OATH application on the YubiKey is not yet password-protected,
        /// you should pass in an <c>Empty</c> <c>currentPassword</c> argument.
        /// If you pass in an actual password, this method will try to verify it,
        /// which will fail, the method will return <c>false</c>, and the
        /// application will not be set with the <c>newPassword</c>.
        /// <para>
        /// If the OATH application is already password-protected, the current
        /// password must be verified before setting. Hence, this method will
        /// verify the <c>currentPassword</c>. Once that password has been
        /// verified, this method will be able to set the OATH application on the
        /// YubiKey with the <c>newPassword</c>. This is how the password is
        /// changed. If the <c>currentPassword</c> does not verify, then this
        /// method will return <c>false</c>.
        /// </para>
        /// <para>
        /// To see if the OATH application is password-protected or not, look at
        /// the property <see cref="IsPasswordProtected"/>.
        /// </para>
        /// <para>
        /// For example,
        /// <code language="csharp">
        ///    bool isSetToNewPassword = false;
        ///    if (oathSession.IsPasswordProtected)
        ///    {
        ///        // The OATH application is set to password-Protected,
        ///        // this call will change it to a new password.
        ///        isSetToNewPassword = oathSession.TrySetPassword(currentPassword, newPassword);
        ///    }
        ///    else
        ///    {
        ///        // The OATH application is not yet password-protected,
        ///        // this call will set it to be so.
        ///        isSetToNewPassword =
        ///            oathSession.TrySetPassword(ReadOnlyMemory&lt;byte&gt;.Empty, newPassword);
        ///    }
        /// </code>
        /// </para>
        /// <para>
        /// Note that if the OATH application is password-protected, and the
        /// password has already been verified, it is still necessary to pass in
        /// the current password. For example,
        /// <code language="csharp">
        ///    if (!oathSession.TryVerifyPassword(currentPassword))
        ///    {
        ///        // Some error handling code, maybe exit.
        ///    }
        ///      . . . // Some other code, more operations
        ///    bool isSetToNewPassword = oathSession.TrySetPassword(currentPassword, newPassword);
        /// </code>
        /// If the password has already been verified in the session, and an
        /// <c>Empty</c> <c>currentPassword</c> is passed in, this method will
        /// return false. If the wrong password is passed in, this method will
        /// try to verify it, which will fail, the method will return
        /// <c>false</c>, and the application will not be set with the
        /// <c>newPassword</c>, even though the current password had been
        /// verified in the session previously.
        /// <para>
        /// Note also that the only way to get a <c>false</c> return is if the
        /// <c>currentPassword</c> does not verify.
        /// </para>
        /// </para>
        /// </remarks>
        /// <param name="currentPassword">
        /// If the OATH application is already password-protected, then this is
        /// the current password. If it is not password-protected, you must pass
        /// in an <c>Empty</c> value.
        /// </param>
        /// <param name="newPassword">
        /// The password to which the OATH application will be set.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// The YubiKey had some error, such as unreliable connection.
        /// </exception>
        /// <returns>
        /// A boolean, <c>true</c> if the OATH application is set to the given
        /// password, and <c>false</c> otherwise.
        /// </returns>
        public bool TrySetPassword(ReadOnlyMemory<byte> currentPassword, ReadOnlyMemory<byte> newPassword)
        {
            if (IsPasswordProtected || !currentPassword.IsEmpty)
            {
                if (!TryVerifyPassword(currentPassword))
                {
                    return false;
                }
            }

            var setPasswordCommand = new SetPasswordCommand(newPassword, _oathData);
            SetPasswordResponse setPasswordResponse = Connection.SendCommand(setPasswordCommand);

            if (setPasswordResponse.Status == ResponseStatus.Success)
            {
                SelectOathResponse response = Connection.SendCommand(new SelectOathCommand());
                _oathData = response.GetData();

                return true;
            }

            if (setPasswordResponse.Status == ResponseStatus.AuthenticationRequired)
            {
                // The only way to reach this code, is if OATH is
                // password-protected, the password has not yet been verified,
                // and the caller passed in an Empty currentPassword. We have
                // declared that this case returns false.
                return false;
            }

            throw new InvalidOperationException(setPasswordResponse.StatusMessage);
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
            // This version of Unset will throw an exception if OATH is not
            // password-protected.
            if (!IsPasswordProtected)
            {
                throw new SecurityException(ResponseStatusMessages.OathAuthNotEnabled);
            }

            EnsureKeyCollector();

            var keyEntryData = new KeyEntryData()
            {
                Request = KeyEntryRequest.VerifyOathPassword
            };

            try
            {
                if (KeyCollector!(keyEntryData))
                {
                    if (!TryUnsetPassword(keyEntryData.GetCurrentValue()))
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
        /// Try to set the OATH application in the YubiKey to no longer be
        /// password-protected. This operation requires verifying the current
        /// password provided.
        /// </summary>
        /// <remarks>
        /// If the OATH application is password-protected, the current password
        /// must be verified before unsetting. Hence, this method will verify the
        /// <c>password</c>. Once that password has been verified, this method
        /// will be able to set the OATH application on the YubiKey to no longer
        /// be password-protected. If the <c>password</c> does not verify, then
        /// this method will return <c>false</c>.
        /// <para>
        /// If the OATH application on the YubiKey is not yet password-protected,
        /// this method will ignore the <c>password</c> argument, do nothing, and
        /// return <c>true</c>.
        /// </para>
        /// <para>
        /// To see if the OATH application is password-protected or not, look at
        /// the property <see cref="IsPasswordProtected"/>.
        /// </para>
        /// <para>
        /// For example,
        /// <code language="csharp">
        ///    if (oathSession.IsPasswordProtected)
        ///    {
        ///        // The OATH application is set to password-Protected,
        ///        // this call will change it to no longer password-protected
        ///        bool isUnset = oathSession.TryUnsetPassword(currentPassword);
        ///    }
        /// </code>
        /// </para>
        /// <para>
        /// Note that if the OATH application is password-protected, and the
        /// password has already been verified, then it is not necessary to pass
        /// in the current password. For example,
        /// <code language="csharp">
        ///    if (oathSession.TryVerifyPassword(currentPassword)
        ///    {
        ///        bool isUnset =
        ///            oathSession.TryUnsetPassword(ReadOnlyMemory&lt;byte&gt;.Empty);
        ///    }
        /// </code>
        /// If the password has already been verified in the session, this method
        /// will not try to verify the <c>password</c>. That is, the
        /// <c>password</c> can be the wrong value or <c>Empty</c>, and if the
        /// password has already been verified, this method will succeed.
        /// <para>
        /// Note also that the only way to get a <c>false</c> return is if the
        /// OATH application is password-protected, the password has not yet been
        /// verified in the session, and the provided password is <c>Empty</c> or
        /// does not verify.
        /// </para>
        /// </para>
        /// </remarks>
        /// <param name="password">
        /// If the OATH application is already password-protected, then this is
        /// the current password.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// The YubiKey had some error, such as unreliable connection.
        /// </exception>
        /// <returns>
        /// A boolean, <c>true</c> if the OATH application is unset, and
        /// <c>false</c> otherwise.
        /// </returns>
        public bool TryUnsetPassword(ReadOnlyMemory<byte> password) =>
            TrySetPassword(password, ReadOnlyMemory<byte>.Empty);
    }
}
