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
using Yubico.YubiKey.U2f.Commands;
using Yubico.Core.Logging;


namespace Yubico.YubiKey.U2f
{
    // This portion of the U2fSession class contains code for PIN operations.
    public sealed partial class U2fSession : IDisposable
    {
        /// <summary>
        /// For a version 4 FIPS series YubiKey that does not have a PIN set,
        /// this will call on the <see cref="KeyCollector"/> to obtain a PIN and
        /// use it to set the U2F application with that PIN.
        /// </summary>
        /// <remarks>
        /// A version 4 FIPS series YubiKey is manufactured with no PIN set on
        /// the U2F application. At this point, the YubiKey is not in FIPS mode.
        /// Once the PIN is set, it is in FIPS mode.
        /// <para>
        /// Once a PIN is set, it is possible to change it (see
        /// <see cref="ChangePin"/>), however, the only way to remove a PIN is to
        /// reset the entire U2F application. After reset, the YubiKey's U2F
        /// application is no longer in FIPS mode, and furthermore, it can never
        /// be put into FIPS mode again. It can be set with a PIN again, but that
        /// will not put a reset YubiKey into FIPS mode.
        /// </para>
        /// <para>
        /// The PIN is binary data and must be at least 6 and no more than 32
        /// bytes long. If the user enters a value too short or too long, this
        /// method will not set the PIN, but it will call the <c>KeyCollector</c>
        /// again requesting the user enter a new PIN.
        /// </para>
        /// <para>
        /// While the PIN can be any binary value, most PINs will be letters,
        /// numbers, and other characters entered from a keyboard. It is the
        /// responsibility of the app to determine how a character typed at a
        /// keyboard is represented as a byte. Almost certainly the best encoding
        /// will be UTF-8. In UTF-8, each ASCII character ie encoded with the
        /// single byte that is the ASCII character. For example, the character
        /// "5" in ASCII is 0x35. In UTF-8, it is 0x35. The character "C" is 0x43
        /// in both ASCII and UTF-8.
        /// </para>
        /// <para>
        /// Note that a PIN is needed to perform U2F registration, but not
        /// authentication.
        /// </para>
        /// </remarks>
        /// <exception cref="SecurityException">
        /// The YubiKey is not version 4 FIPS series, or the U2F application is
        /// already set with a PIN, or the PIN is blocked.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// The user cancelled. This happens when this method calls the
        /// <c>KeyCollector</c> and it returns <c>false</c>.
        /// </exception>
        public void SetPin()
        {
            _log.LogInformation("Set the U2F PIN using the KeyCollector.");
            if (TrySetPin())
            {
                return;
            }

            throw new OperationCanceledException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.IncompleteCommandInput));
        }

        /// <summary>
        /// For a version 4 FIPS series YubiKey that does not have a PIN set,
        /// this will call on the <see cref="KeyCollector"/> to obtain a PIN and
        /// use it to set the U2F application with that PIN. If the caller
        /// cancels (the return from the <c>KeyCollector</c> is <c>false</c>),
        /// this will return <c>false</c>.
        /// </summary>
        /// <remarks>
        /// See the documentation for <see cref="SetPin"/> for more information
        /// on setting a PIN.
        /// </remarks>
        /// <returns>
        /// A boolean, <c>true</c> if the PIN is set, <c>false</c> if the user
        /// cancels PIN collection.
        /// </returns>
        /// <exception cref="SecurityException">
        /// The YubiKey is not version 4 FIPS series, or the U2F application is
        /// already set with a PIN, or the PIN is blocked.
        /// </exception>
        public bool TrySetPin()
        {
            _log.LogInformation("Try to set the U2F PIN using the KeyCollector.");
            Func<KeyEntryData, bool> keyCollector = EnsureKeyCollector();

            var keyEntryData = new KeyEntryData()
            {
                Request = KeyEntryRequest.SetU2fPin,
            };

            try
            {
                while (keyCollector(keyEntryData) == true)
                {
                    if (TrySetPin(keyEntryData.GetCurrentValue()))
                    {
                        return true;
                    }

                    keyEntryData.IsRetry = true;
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
        /// For a version 4 FIPS series YubiKey that does not have a PIN set,
        /// this will try to set the PIN using the given <c>pin</c>.
        /// </summary>
        /// <remarks>
        /// See the documentation for <see cref="SetPin"/> for more information
        /// on setting a PIN.
        /// <para>
        /// If the input <c>pin</c> is less than 6 or more than 32 bytes long,
        /// this method will return <c>false</c>. However, this method will throw
        /// an exception if the U2F application is already set, the PIN is
        /// blocked, or the YubiKey is not version 4 FIPS series.
        /// </para>
        /// </remarks>
        /// <returns>
        /// A boolean, <c>true</c> if the PIN is set, <c>false</c> if the user
        /// cancels PIN collection.
        /// </returns>
        /// <exception cref="SecurityException">
        /// The U2F application is already set with a PIN, or the PIN is blocked.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// The YubiKey is not version 4 FIPS series.
        /// </exception>
        public bool TrySetPin(ReadOnlyMemory<byte> pin)
        {
            _log.LogInformation("Try to set the U2F PIN using a provided value.");
            var setCommand = new SetPinCommand(ReadOnlyMemory<byte>.Empty, pin);
            SetPinResponse setResponse = Connection.SendCommand(setCommand);

            return setResponse.StatusWord switch
            {
                SWConstants.Success => true,
                SWConstants.VerifyFail => throw new SecurityException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.AlreadySet)),
                SWConstants.AuthenticationMethodBlocked => throw new SecurityException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.NoMoreRetriesRemaining)),
                SWConstants.InsNotSupported => throw new NotSupportedException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.YubiKeyNotFips)),
                _ => false,
            };
        }

        /// <summary>
        /// For a version 4 FIPS series YubiKey that has a PIN set on the U2F
        /// application, this will call on the <see cref="KeyCollector"/> to
        /// obtain the current and a new PIN and use them to change the U2F PIN.
        /// </summary>
        /// <remarks>
        /// A version 4 FIPS series YubiKey is manufactured with no PIN set on
        /// the U2F application. At this point, the YubiKey is not in FIPS mode.
        /// Once the PIN is set, it is in FIPS mode See <see cref="SetPin"/>.
        /// After it has been set, it is possible to change the PIN to a new
        /// value.
        /// <para>
        /// Once a PIN is set, however, the only way to remove a PIN is to reset
        /// the entire U2F application. After reset, the YubiKey's U2F
        /// application is no longer in FIPS mode, and furthermore, it can never
        /// be put into FIPS mode again. It can be set with a PIN again, but that
        /// will not put a reset YubiKey into FIPS mode.
        /// </para>
        /// <para>
        /// The current PIN must be entered, even if the PIN has been verified in
        /// the current session. If the wrong current PIN is entered, the YubiKey
        /// will decrement the retries remaining count, and this method will call
        /// on the <c>KeyCollector</c> for the current and new PIN again (the
        /// <c>KeyEntryData.IsRetry</c> property will be <c>true</c>). See the
        /// user's manual entry on
        /// <xref href="FidoU2fFipsMode#retries"> FIDO U2F FIPS mode</xref>
        /// retries for more information.
        /// </para>
        /// <para>
        /// The PIN is binary data and must be at least 6 and no more than 32
        /// bytes long. If the user enters a value too short or too long, this
        /// method will not change the PIN, but it will call the
        /// <c>KeyCollector</c> again requesting the user enter a new PIN.
        /// </para>
        /// <para>
        /// While the PIN can be any binary value, most PINs will be letters,
        /// numbers, and other characters entered from a keyboard. It is the
        /// responsibility of the app to determine how a character typed at a
        /// keyboard is represented as a byte. Almost certainly the best encoding
        /// will be UTF-8. In UTF-8, each ASCII character ie encoded with the
        /// single byte that is the ASCII character. For example, the character
        /// "5" in ASCII is 0x35. In UTF-8, it is 0x35. The character "C" is 0x43
        /// in both ASCII and UTF-8.
        /// </para>
        /// <para>
        /// Note that if the SDK calls the <c>KeyCollector</c> to try again, it
        /// will not specify what the problem is, wrong current PIN or invalid
        /// new PIN. Hence, it would be a good idea if your <c>KeyCollector</c>
        /// checked the length of the new PIN and reject it before passing it on
        /// to the SDK. If so, then you know a retry means incorrect current PIN.
        /// </para>
        /// <para>
        /// Note that a PIN is needed to perform U2F registration, but not
        /// authentication.
        /// </para>
        /// </remarks>
        /// <exception cref="SecurityException">
        /// The YubiKey is not version 4 FIPS series, or the PIN is blocked.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// The user cancelled. This happens when this method calls the
        /// <c>KeyCollector</c> and it returns <c>false</c>.
        /// </exception>
        public void ChangePin()
        {
            _log.LogInformation("Change the U2F PIN using the KeyCollector.");
            if (TryChangePin())
            {
                return;
            }

            throw new OperationCanceledException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.IncompleteCommandInput));
        }

        /// <summary>
        /// For a version 4 FIPS series YubiKey that has a PIN set on the U2F
        /// application, this will call on the <see cref="KeyCollector"/> to
        /// obtain the current and a new PIN and use them to change the U2F PIN.
        /// If the caller cancels (the return from the <c>KeyCollector</c> is
        /// <c>false</c>), this will return <c>false</c>.
        /// </summary>
         /// <remarks>
        /// See the documentation for <see cref="ChangePin"/> for more information
        /// on changing a PIN.
        /// </remarks>
        /// <returns>
        /// A boolean, <c>true</c> if the PIN is changed, <c>false</c> if the user
        /// cancels PIN collection.
        /// </returns>
        /// <exception cref="SecurityException">
        /// The YubiKey is not version 4 FIPS series, or the PIN is blocked.
        /// </exception>
        public bool TryChangePin()
        {
            _log.LogInformation("Try to change the U2F PIN using the KeyCollector.");
            Func<KeyEntryData, bool> keyCollector = EnsureKeyCollector();

            var keyEntryData = new KeyEntryData()
            {
                Request = KeyEntryRequest.ChangeU2fPin,
            };

            try
            {
                while (keyCollector(keyEntryData) == true)
                {
                    if (TryChangePin(keyEntryData.GetCurrentValue(), keyEntryData.GetNewValue()))
                    {
                        return true;
                    }

                    keyEntryData.IsRetry = true;
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
        /// For a version 4 FIPS series YubiKey that has a PIN set on the U2F
        /// application, this will use the provided current and new PINs to
        /// change the U2F PIN. If the current PIN given is not correct, or the
        /// new PIN is not a correct length, this method will return <c>false</c>.
        /// </summary>
        /// <remarks>
        /// See the documentation for <see cref="ChangePin"/> for more information
        /// on changing a PIN.
        /// </remarks>
        /// <returns>
        /// A boolean, <c>true</c> if the PIN is changed, <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="SecurityException">
        /// The PIN is blocked.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// The YubiKey is not version 4 FIPS series.
        /// </exception>
        public bool TryChangePin(ReadOnlyMemory<byte> currentPin, ReadOnlyMemory<byte> newPin)
        {
            _log.LogInformation("Try to change the U2F PIN using provided values.");
            var setCommand = new SetPinCommand(currentPin, newPin);
            SetPinResponse setResponse = Connection.SendCommand(setCommand);

            return setResponse.StatusWord switch
            {
                SWConstants.Success => true,
                SWConstants.AuthenticationMethodBlocked => throw new SecurityException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.NoMoreRetriesRemaining)),
                SWConstants.InsNotSupported => throw new NotSupportedException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.YubiKeyNotFips)),
                _ => false,
            };
        }

        /// <summary>
        /// For a version 4 FIPS series YubiKey that has a PIN set on the U2F
        /// application, this will call on the <see cref="KeyCollector"/> to
        /// obtain the current PIN and verify it.
        /// </summary>
        /// <remarks>
        /// A version 4 FIPS series YubiKey is manufactured with no PIN set on
        /// the U2F application. At this point, the YubiKey is not in FIPS mode.
        /// Once the PIN is set, it is in FIPS mode See <see cref="SetPin"/>.
        /// After it has been set, it is necessary to verify the PIN in order to
        /// perform registration. Note that the PIN is not needed for
        /// authentication.
        /// <para>
        /// Note that if the PIN is not verified and the <see cref="Register"/>
        /// method is called, the SDK will call this <c>Verify</c> method. Hence,
        /// it is likely an app will never need to call this method directly.
        /// </para>
        /// <para>
        /// If the wrong current PIN is entered, the YubiKey will decrement the
        /// retries remaining count, and this method will call on the
        /// <c>KeyCollector</c> for the current PIN again (the
        /// <c>KeyEntryData.IsRetry</c> property will be <c>true</c>). See the
        /// user's manual entry on
        /// <xref href="FidoU2fFipsMode#retries"> FIDO U2F FIPS mode</xref>
        /// retries for more information.
        /// </para>
        /// <para>
        /// The PIN is binary data and must be at least 6 and no more than 32
        /// bytes long. If the user enters a value too short or too long, this
        /// method will try to verify that value, the YubiKey will reject it, and
        /// this method will call the <c>KeyCollector</c> again requesting the
        /// user enter the PIN.
        /// </para>
        /// <para>
        /// While the PIN can be any binary value, most PINs will be letters,
        /// numbers, and other characters entered from a keyboard. It is the
        /// responsibility of the app to determine how a character typed at a
        /// keyboard is represented as a byte. Almost certainly the best encoding
        /// will be UTF-8. In UTF-8, each ASCII character ie encoded with the
        /// single byte that is the ASCII character. For example, the character
        /// "5" in ASCII is 0x35. In UTF-8, it is 0x35. The character "C" is 0x43
        /// in both ASCII and UTF-8.
        /// </para>
        /// </remarks>
        /// <exception cref="SecurityException">
        /// The YubiKey is not version 4 FIPS series, or the PIN is blocked.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// The user cancelled. This happens when this method calls the
        /// <c>KeyCollector</c> and it returns <c>false</c>.
        /// </exception>
        public void VerifyPin() => _ = CommonVerifyPin(true);

        /// <summary>
        /// For a version 4 FIPS series YubiKey that has a PIN set on the U2F
        /// application, this will call on the <see cref="KeyCollector"/> to
        /// obtain the current PIN and verify it. If the caller cancels (the
        /// return from the <c>KeyCollector</c> is <c>false</c>), this will
        /// return <c>false</c>.
        /// </summary>
        /// <remarks>
        /// See the documentation for <see cref="VerifyPin"/> for more information
        /// on verifying a PIN.
        /// </remarks>
        /// <returns>
        /// A boolean, <c>true</c> if the PIN is verified, <c>false</c> if the user
        /// cancels PIN collection.
        /// </returns>
        /// <exception cref="SecurityException">
        /// The YubiKey is not version 4 FIPS series, or the PIN is blocked.
        /// </exception>
        public bool TryVerifyPin() => CommonVerifyPin(false);

        // This is similar to TryVerifyPin(), except if the throwOnCancel arg is
        // true, then this will throw an exception if the user cancels. Otherwise
        // it returns false on cancel.
        private bool CommonVerifyPin(bool throwOnCancel)
        {
            _log.LogInformation("Verify the U2F PIN using the KeyCollector.");
            Func<KeyEntryData, bool> keyCollector = EnsureKeyCollector();

            var keyEntryData = new KeyEntryData()
            {
                Request = KeyEntryRequest.VerifyU2fPin,
            };

            try
            {
                while (keyCollector(keyEntryData) == true)
                {
                    if (TryVerifyPin(keyEntryData.GetCurrentValue()))
                    {
                        return true;
                    }

                    keyEntryData.IsRetry = true;
                }
            }
            finally
            {
                keyEntryData.Clear();

                keyEntryData.Request = KeyEntryRequest.Release;
                _ = keyCollector(keyEntryData);
            }

            if (throwOnCancel)
            {
                throw new OperationCanceledException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.IncompleteCommandInput));
            }

            return false;
        }

        /// <summary>
        /// For a version 4 FIPS series YubiKey that has a PIN set on the U2F
        /// application, this try to verify the given <c>pin</c>. If the PIN is
        /// not verified, this method will return <c>false</c>.
        /// </summary>
        /// <remarks>
        /// See the documentation for <see cref="VerifyPin"/> for more information
        /// on verifying a PIN.
        /// </remarks>
        /// <returns>
        /// A boolean, <c>true</c> if the PIN is verified, <c>false</c>
        /// otherwise.
        /// </returns>
        /// <exception cref="SecurityException">
        /// The PIN is blocked.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// The YubiKey is not version 4 FIPS series.
        /// </exception>
        public bool TryVerifyPin(ReadOnlyMemory<byte> pin)
        {
            _log.LogInformation("Try to verify the U2F PIN using a provided value.");
            var verifyCommand = new VerifyPinCommand(pin);
            VerifyPinResponse verifyResponse = Connection.SendCommand(verifyCommand);

            return verifyResponse.StatusWord switch
            {
                SWConstants.Success => true,
                SWConstants.AuthenticationMethodBlocked => throw new SecurityException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.NoMoreRetriesRemaining)),
                SWConstants.InsNotSupported => throw new NotSupportedException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.YubiKeyNotFips)),
                _ => false,
            };
        }
    }
}
