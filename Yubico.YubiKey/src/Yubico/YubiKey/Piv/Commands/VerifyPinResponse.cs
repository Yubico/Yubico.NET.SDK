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
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Piv.Commands
{
    /// <summary>
    ///     The response to verifying the PIN.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This is the partner Response class to <see cref="VerifyPinCommand" />.
    ///     </para>
    ///     <para>
    ///         To determine the result of the command, first look at the
    ///         <see cref="YubiKeyResponse.Status" />. If <c>Status</c> is not one of
    ///         the following values then an error has occurred and <see cref="GetData" />
    ///         will throw an exception.
    ///     </para>
    ///     <list type="table">
    ///         <listheader>
    ///             <term>Status</term>
    ///             <description>Description</description>
    ///         </listheader>
    ///         <item>
    ///             <term>
    ///                 <see cref="ResponseStatus.Success" />
    ///             </term>
    ///             <description>The PIN verified. GetData returns <c>null</c>.</description>
    ///         </item>
    ///         <item>
    ///             <term>
    ///                 <see cref="ResponseStatus.AuthenticationRequired" />
    ///             </term>
    ///             <description>
    ///                 The PIN did not verify. GetData returns the number
    ///                 of retries remaining. If the number of retries is 0, the PIN
    ///                 is blocked.
    ///             </description>
    ///         </item>
    ///     </list>
    ///     <para>
    ///         Example:
    ///     </para>
    ///     <code language="csharp">
    ///   /* This example assumes the application has a method to collect a PIN.
    ///    */
    ///   byte[] pin;<br />
    /// 
    ///   IYubiKeyConnection connection = key.Connect(YubiKeyApplication.Piv);<br />
    ///   pin = CollectPin();
    ///   var verifyPinCommand = new VerifyPinCommand(pin);
    ///   VerifyPinResponse verifyPinResponse = connection.SendCommand(verifyPinCommand);<br />
    ///   if (resetRetryResponse.Status == ResponseStatus.AuthenticationRequired)
    ///   {
    ///     int retryCount = resetRetryResponse.GetData();
    ///     /* report the retry count */
    ///   }
    ///   else if (verifyPinResponse.Status != ResponseStatus.Success)
    ///   {
    ///     // Handle error
    ///   }
    /// 
    ///   CryptographicOperations.ZeroMemory(pin)
    /// </code>
    /// </remarks>
    public sealed class VerifyPinResponse : PivResponse, IYubiKeyResponseWithData<int?>
    {
        /// <summary>
        ///     Constructs a VerifyPinResponse based on a ResponseApdu received from
        ///     the YubiKey.
        /// </summary>
        /// <param name="responseApdu">
        ///     The object containing the response APDU<br />returned by the YubiKey.
        /// </param>
        public VerifyPinResponse(ResponseApdu responseApdu) :
            base(responseApdu)
        {
        }

        /// <inheritdoc />
        protected override ResponseStatusPair StatusCodeMap
        {
            get
            {
                switch (StatusWord)
                {
                    case short statusWord when PivPinUtilities.HasRetryCount(statusWord):
                        int remainingRetries = PivPinUtilities.GetRetriesRemaining(statusWord);
                        return new ResponseStatusPair(
                            ResponseStatus.AuthenticationRequired,
                            string.Format(
                                CultureInfo.CurrentCulture, ResponseStatusMessages.PivPinPukFailedWithRetries,
                                remainingRetries));

                    case SWConstants.AuthenticationMethodBlocked:
                        return new ResponseStatusPair(
                            ResponseStatus.AuthenticationRequired, ResponseStatusMessages.PivPinPukBlocked);

                    case SWConstants.SecurityStatusNotSatisfied:
                        return new ResponseStatusPair(
                            ResponseStatus.AuthenticationRequired,
                            ResponseStatusMessages.PivSecurityStatusNotSatisfied);

                    default:
                        return base.StatusCodeMap;
                }
            }
        }

        /// <summary>
        ///     Gets the number of PIN retries remaining, if applicable.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         First look at the
        ///         <see cref="YubiKeyResponse.Status" />. If <c>Status</c> is not one of
        ///         the following values then an error has occurred and <see cref="GetData" />
        ///         will throw an exception.
        ///     </para>
        ///     <list type="table">
        ///         <listheader>
        ///             <term>Status</term>
        ///             <description>Description</description>
        ///         </listheader>
        ///         <item>
        ///             <term>
        ///                 <see cref="ResponseStatus.Success" />
        ///             </term>
        ///             <description>The PIN verified. GetData returns <c>null</c>.</description>
        ///         </item>
        ///         <item>
        ///             <term>
        ///                 <see cref="ResponseStatus.AuthenticationRequired" />
        ///             </term>
        ///             <description>
        ///                 The PIN did not verify. GetData returns the number
        ///                 of retries remaining. If the number of retries is 0, the PIN
        ///                 is blocked.
        ///             </description>
        ///         </item>
        ///     </list>
        /// </remarks>
        /// <returns>
        ///     <c>null</c> if the PIN verifies, or the number of retries remaining if
        ///     the PIN does not verify.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///     Thrown if <see cref="YubiKeyResponse.Status" /> is not <see cref="ResponseStatus.Success" />
        ///     or <see cref="ResponseStatus.AuthenticationRequired" />.
        /// </exception>
        [SuppressMessage(
            "Style", "IDE0046:Convert to conditional expression",
            Justification = "Readability, avoiding nested conditionals.")]
        public int? GetData()
        {
            if (Status != ResponseStatus.Success && Status != ResponseStatus.AuthenticationRequired)
            {
                throw new InvalidOperationException(StatusMessage);
            }

            if (PivPinUtilities.HasRetryCount(StatusWord))
            {
                return PivPinUtilities.GetRetriesRemaining(StatusWord);
            }

            if (StatusWord == SWConstants.AuthenticationMethodBlocked)
            {
                return 0;
            }

            return null;
        }
    }
}
