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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Yubico.Core.Iso7816;
using Yubico.YubiKey.U2f.Commands;
using Yubico.YubiKey.InterIndustry.Commands;

namespace Yubico.YubiKey.U2f
{
    // This portion of the U2fSession class contains code for PIN operations.
    public sealed partial class U2fSession : IDisposable
    {
        /// <summary>
        /// For a version 4 FIPS series YubiKey that is not yet in FIPS mode (no
        /// PIN is yet set), this will call on the <see cref="KeyCollector"/> to
        /// obtain a PIN and use it to set the U2F application with that PIN and
        /// put it into FIPS mode.
        /// </summary>
        public void SetPin()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// For a version 4 FIPS series YubiKey that is not yet in FIPS mode (no
        /// PIN is yet set), this will call on the <see cref="KeyCollector"/> to
        /// obtain a PIN and use it to set the U2F application with that PIN and
        /// put it into FIPS mode. If the caller cancels (the return from the
        /// <c>KeyCollector</c> is <c>false</c>), this will return <c>false</c>.
        /// </summary>
        public bool TrySetPin()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// For a version 4 FIPS series YubiKey that is not yet in FIPS mode (no
        /// PIN is yet set), this will set the U2F application with the given
        /// PIN. If the PIN given is invalid (e.g. not long enough or too long),
        /// this will throw an exception.
        /// </summary>
        public bool SetPin(ReadOnlyMemory<byte> pin)
        {
            throw new NotImplementedException ();
        }

        /// <summary>
        /// For a version 4 FIPS series YubiKey that is in FIPS mode, this will
        /// call on the <see cref="KeyCollector"/> to obtain the current and a
        /// new PIN and use them to change the U2F PIN.
        /// </summary>
        public void ChangePin()
        {
            throw new NotImplementedException ();
        }

        /// <summary>
        /// For a version 4 FIPS series YubiKey that is in FIPS mode, this will
        /// call on the <see cref="KeyCollector"/> to obtain the current and a
        /// new PIN and use them to change the U2F PIN. If the caller cancels
        /// (the return from the <c>KeyCollector</c> is <c>false</c>), this will
        /// return <c>false</c>.
        /// </summary>
        public bool TryChangePin()
        {
            throw new NotImplementedException ();
        }

        /// <summary>
        /// For a version 4 FIPS series YubiKey that is in FIPS mode, this will
        /// use the given current and new PINs to change the U2F PIN. If the
        /// wrong current PIN is provided, this method will return <c>false</c>.
        /// If the new PIN is invalid, this method will throw an exception.
        /// </summary>
        public bool TryChangePin(ReadOnlyMemory<byte> currentPin, ReadOnlyMemory<byte> newPin, out int? retriesRemaining)
        {
            throw new NotImplementedException ();
        }

        /// <summary>
        /// For a version 4 FIPS series YubiKey that is in FIPS mode, this will
        /// call on the <see cref="KeyCollector"/> to obtain the current PIN and
        /// verify it.
        /// </summary>
        public void VerifyPin()
        {
            _ = CommonVerifyPin(true);
        }

        /// <summary>
        /// For a version 4 FIPS series YubiKey that is in FIPS mode, this will
        /// call on the <see cref="KeyCollector"/> to obtain the current PIN and
        /// verify it. If the caller cancels (the return from the
        /// <c>KeyCollector</c> is <c>false</c>), this will return <c>false</c>.
        /// </summary>
        public bool TryVerifyPin()
        {
            return CommonVerifyPin(false);
        }

        // This is similar to TryVerifyPin(), except if the throwOnCancel arg is
        // true, then this will throw an exception if the user cancels. Otherwise
        // it returns false on cancel.
        private bool CommonVerifyPin(bool throwOnCancel)
        {
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
        /// For a version 4 FIPS series YubiKey that is in FIPS mode, this will
        /// verify the given PIN. If the provided value does not verify (wrong
        /// PIN), the method will return <c>false</c>.
        /// </summary>
        public bool TryVerifyPin(ReadOnlyMemory<byte> pin)
        {
            var verifyCommand = new VerifyPinCommand(pin);
            VerifyPinResponse verifyResponse = Connection.SendCommand(verifyCommand);

            if (verifyResponse.Status == ResponseStatus.Success)
            {
                return true;
            }
            if (verifyResponse.StatusWord == SWConstants.AuthenticationMethodBlocked)
            {
                throw new SecurityException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.NoMoreRetriesRemaining));
            }

            return false;
        }
    }
}
