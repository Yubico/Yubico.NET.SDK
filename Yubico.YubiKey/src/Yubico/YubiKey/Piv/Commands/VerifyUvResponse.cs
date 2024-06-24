// Copyright 2024 Yubico AB
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
    /// The response to biometric verification.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the partner Response class to <see cref="VerifyUvCommand"/>.
    /// </para>
    /// <para>
    /// To determine the result of the command, first look at the
    /// <see cref="YubiKeyResponse.Status"/>. If <c>Status</c> is not one of
    /// the following values then an error has occurred and <see cref="GetData"/>
    /// will throw an exception.
    /// </para>
    /// <list type="table">
    /// <listheader>
    /// <term>Status</term>
    /// <description>Description</description>
    /// </listheader>
    ///
    /// <item>
    /// <term><see cref="ResponseStatus.Success"/></term>
    /// <description>The biometric authentication succeeded. GetData returns temporary pin if requested.
    /// </description>
    /// </item>
    ///
    /// <item>
    /// <term><see cref="ResponseStatus.AuthenticationRequired"/></term>
    /// <description>The biometric authentication failed. GetData returns null. <see cref="AttemptsRemaining"/> 
    /// returns the number of retries remaining. If the number of retries is 0, the biometric authentication is 
    /// blocked and the client should use PIN authentication <see cref="VerifyPinCommand"/>.</description>
    /// </item>
    /// </list>
    ///
    /// <para>
    /// Example:
    /// </para>
    /// <code language="csharp">
    ///   IYubiKeyConnection connection = key.Connect(YubiKeyApplication.Piv);<br/>
    ///   var verifyUvCommand = new VerifyUvCommand(false, false);
    ///   VerifyUvResponse verifyUvResponse = connection.SendCommand(verifyUvCommand);<br/>
    ///   if (verifyUvResponse.Status == ResponseStatus.AuthenticationRequired)
    ///   {
    ///     int retryCount = verifyUvResponse.AttemptsRemaining;
    ///     /* report the retry count */
    ///   }
    ///   else if (verifyUvResponse.Status != ResponseStatus.Success)
    ///   {
    ///     /* handle error */
    ///   }
    /// </code>
    /// </remarks>
    public sealed class VerifyUvResponse : PivResponse, IYubiKeyResponseWithData<ReadOnlyMemory<byte>>
    {
        private readonly bool _requestTemporaryPin;

        /// <summary>
        /// Indicates how many biometric match retries are left (biometric match retry counter) until a biometric
        /// verification is blocked. 
        /// </summary>
        /// <remarks>
        /// The value is returned only if a authentication failed. To get remaining biometric attempts when not
        /// performing authentication, use <see cref="PivBioMetadata.AttemptsRemaining"/>.
        /// </remarks>
        public int? AttemptsRemaining
        {
            get
            {
                if (Status == ResponseStatus.AuthenticationRequired)
                {
                    if (PivPinUtilities.HasRetryCount(StatusWord))
                    {
                        return PivPinUtilities.GetRetriesRemaining(StatusWord);
                    }
                    if (StatusWord == SWConstants.AuthenticationMethodBlocked)
                    {
                        return 0;
                    }
                }
                return null;
            }
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
                        return new ResponseStatusPair(ResponseStatus.AuthenticationRequired, string.Format(CultureInfo.CurrentCulture, ResponseStatusMessages.PivBioUVFailedWithRetries, remainingRetries));

                    case SWConstants.AuthenticationMethodBlocked:
                        return new ResponseStatusPair(ResponseStatus.AuthenticationRequired, ResponseStatusMessages.PivBioUvBlocked);

                    case SWConstants.SecurityStatusNotSatisfied:
                        return new ResponseStatusPair(ResponseStatus.AuthenticationRequired, ResponseStatusMessages.PivSecurityStatusNotSatisfied);

                    default:
                        return base.StatusCodeMap;
                }
            }
        }

        /// <summary>
        /// Constructs a VerifyUvResponse based on a ResponseApdu received from
        /// the YubiKey.
        /// </summary>
        /// <param name="responseApdu">
        /// The object containing the response APDU<br/>returned by the YubiKey.
        /// </param>
        /// <param name="requestTemporaryPin">
        /// True means that a temporary PIN was requested.
        /// </param>
        public VerifyUvResponse(ResponseApdu responseApdu, bool requestTemporaryPin) :
            base(responseApdu)
        {
            _requestTemporaryPin = requestTemporaryPin;
        }

        /// <summary>
        /// Gets the temporary PIN if requested.
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
        /// <description>The biometric authentication succeeded. If requested, GetData returns the temporary PIN.</description>
        /// </item>
        ///
        /// <item>
        /// <term><see cref="ResponseStatus.AuthenticationRequired"/></term>
        /// <description>The biometric authentication did not succeed. <see cref="AttemptsRemaining"/> contains number of
        /// of retries remaining. If the number of retries is 0 the biometric authentication is blocked and the 
        /// client should use PIN authentication (<see cref="VerifyPinCommand"/>).</description>
        /// </item>
        /// </list>
        /// </remarks>
        /// <returns>
        /// <c>null</c> if the PIN verifies, or the number of retries remaining if
        /// the PIN does not verify.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if <see cref="YubiKeyResponse.Status"/> is not <see cref="ResponseStatus.Success"/>
        /// or <see cref="ResponseStatus.AuthenticationRequired"/>.
        /// </exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Readability, avoiding nested conditionals.")]
        public ReadOnlyMemory<byte> GetData()
        {
            if (Status != ResponseStatus.Success && Status != ResponseStatus.AuthenticationRequired)
            {
                throw new InvalidOperationException(StatusMessage);
            }

            if (_requestTemporaryPin)
            {
                return ResponseApdu.Data;
            }

            return ReadOnlyMemory<byte>.Empty;
        }
    }
}
