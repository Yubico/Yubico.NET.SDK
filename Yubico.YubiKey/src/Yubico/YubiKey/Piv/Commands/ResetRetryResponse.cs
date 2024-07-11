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
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Piv.Commands
{
    /// <summary>
    /// The response to resetting the PIN, using the PUK (PIN Unblocking Key).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the partner Response class to <see cref="ResetRetryCommand"/>.
    /// </para>
    /// <para>
    /// To determine the result of the command, first look at the
    /// <see cref="YubiKeyResponse.Status"/>. If <c>Status</c> is not one of
    /// the following values then an error has occurred and <see cref="GetData"/>
    /// will throw an exception.
    ///
    /// <list type="table">
    /// <listheader>
    /// <term>Status</term>
    /// <description>Description</description>
    /// </listheader>
    ///
    /// <item>
    /// <term><see cref="ResponseStatus.Success"/></term>
    /// <description>The PIN was successfully reset. GetData returns
    /// <c>null</c>.</description>
    /// </item>
    ///
    /// <item>
    /// <term><see cref="ResponseStatus.AuthenticationRequired"/></term>
    /// <description>The PUK did not verify. GetData returns the number
    /// of retries remaining. If the number of retries is 0, the PUK
    /// is blocked.</description>
    /// </item>
    /// </list>
    ///
    /// Example:
    /// </para>
    /// <code language="csharp">
    ///   using System.Security.Cryptography;<br/>
    ///   /* This example assumes the application has a method to collect a
    ///    * PIN/PUK.
    ///    */
    ///   byte[] puk;
    ///   byte[] newPin;<br/>
    ///
    ///   IYubiKeyConnection connection = key.Connect(YubiKeyApplication.Piv);<br/>
    ///   puk = CollectPuk();
    ///   newPin = CollectNewPin();
    ///   var resetRetryCommand = new ResetRetryCommand(puk, newPin);
    ///   ResetRetryResponse resetRetryResponse = connection.SendCommand(resetRetryCommand);<br/>
    ///   if (resetRetryResponse.Status != ResponseStatus.Success)
    ///   {
    ///     if (resetRetryResponse.Status == ResponseStatus.AuthenticationRequired)
    ///     {
    ///         int retryCount = resetRetryResponse.GetData();
    ///         /* report the retry count */
    ///     }
    ///     else
    ///     {
    ///         // Handle error
    ///     }
    ///   }
    ///
    ///   CryptographicOperations.ZeroMemory(puk);
    ///   CryptographicOperations.ZeroMemory(newPin);
    /// </code>
    /// </remarks>
    public sealed class ResetRetryResponse : PivResponse, IYubiKeyResponseWithData<int?>
    {
        /// <inheritdoc />
        protected override ResponseStatusPair StatusCodeMap
        {
            get
            {
                switch (StatusWord)
                {
                    case short statusWord when PivPinUtilities.HasRetryCount(statusWord):
                        int remainingRetries = PivPinUtilities.GetRetriesRemaining(statusWord);
                        return new ResponseStatusPair(ResponseStatus.AuthenticationRequired, string.Format(CultureInfo.CurrentCulture, ResponseStatusMessages.PivPinPukFailedWithRetries, remainingRetries));

                    case SWConstants.AuthenticationMethodBlocked:
                        return new ResponseStatusPair(ResponseStatus.AuthenticationRequired, ResponseStatusMessages.PivPinPukBlocked);

                    case SWConstants.SecurityStatusNotSatisfied:
                        return new ResponseStatusPair(ResponseStatus.AuthenticationRequired, ResponseStatusMessages.PivSecurityStatusNotSatisfied);

                    default:
                        return base.StatusCodeMap;
                }
            }
        }

        /// <summary>
        /// Constructs a ResetRetryResponse based on a ResponseApdu received from
        /// the YubiKey.
        /// </summary>
        /// <param name="responseApdu">
        /// The object containing the response APDU<br/>returned by the YubiKey.
        /// </param>
        public ResetRetryResponse(ResponseApdu responseApdu) :
            base(responseApdu)
        {
        }

        /// <summary>
        /// Gets the number of PUK retries remaining, if applicable.
        /// </summary>
        /// <remarks>
        /// <para>
        /// First look at the
        /// <see cref="YubiKeyResponse.Status"/>. If <c>Status</c> is not one of
        /// the following values then an error has occurred and <see cref="GetData"/>
        /// will throw an exception.
        /// </para>
        ///
        /// <list type="table">
        /// <listheader>
        /// <term>Status</term>
        /// <description>Description</description>
        /// </listheader>
        ///
        /// <item>
        /// <term><see cref="ResponseStatus.Success"/></term>
        /// <description>The PIN was successfully reset. GetData returns
        /// <c>null</c>.</description>
        /// </item>
        ///
        /// <item>
        /// <term><see cref="ResponseStatus.AuthenticationRequired"/></term>
        /// <description>The PUK did not verify. GetData returns the number
        /// of retries remaining. If the number of retries is 0, the PUK
        /// is blocked.</description>
        /// </item>
        /// </list>
        /// </remarks>
        /// <returns>
        /// <c>null</c> if the PIN is reset, or the number of retries remaining if
        /// the PUK does not verify.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if <see cref="YubiKeyResponse.Status"/> is not <see cref="ResponseStatus.Success"/>
        /// or <see cref="ResponseStatus.AuthenticationRequired"/>.
        /// </exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Readability, avoiding nested conditionals.")]
        public int? GetData()
        {
            if (Status != ResponseStatus.Success &&
                Status != ResponseStatus.AuthenticationRequired &&
                Status != ResponseStatus.ConditionsNotSatisfied)
            {
                throw new InvalidOperationException(StatusMessage);
            }

            if (PivPinUtilities.HasRetryCount(StatusWord))
            {
                return PivPinUtilities.GetRetriesRemaining(StatusWord);
            }
            else if (StatusWord == SWConstants.AuthenticationMethodBlocked)
            {
                return 0;
            }
            else
            {
                return null;
            }
        }
    }
}
