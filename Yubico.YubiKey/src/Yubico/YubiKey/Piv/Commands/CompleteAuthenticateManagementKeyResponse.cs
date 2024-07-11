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
using Yubico.Core.Tlv;

namespace Yubico.YubiKey.Piv.Commands
{
    /// <summary>
    /// The response to the complete authenticate management key command.
    /// </summary>
    /// <remarks>
    /// This is the partner Response class to <see
    /// cref="CompleteAuthenticateManagementKeyCommand"/>.
    /// <para>
    /// The data returned is an <see cref="AuthenticateManagementKeyResult"/>
    /// enum.
    /// </para>
    /// <para>
    /// See the comments for the class
    /// <see cref="InitializeAuthenticateManagementKeyCommand"/>, there is a
    /// lengthy discussion of the process of authenticating the management key,
    /// including descriptions of the challenges and responses.
    /// </para>
    /// </remarks>
    public sealed class CompleteAuthenticateManagementKeyResponse
        : PivResponse, IYubiKeyResponseWithData<AuthenticateManagementKeyResult>
    {
        private const int EncodingTag = 0x7C;
        private const int ResponseTag = 0x82;

        private ReadOnlyMemory<byte> YubiKeyAuthenticationExpectedResponse { get; }

        /// <inheritdoc />
        protected override ResponseStatusPair StatusCodeMap =>
            StatusWord switch
            {
                SWConstants.ConditionsNotSatisfied => new ResponseStatusPair(
                    ResponseStatus.AuthenticationRequired, ResponseStatusMessages.PivSecurityStatusNotSatisfied),
                _ => base.StatusCodeMap,
            };

        /// <summary>
        /// Constructs a CompleteAuthenticateManagementKeyResponse based on a ResponseApdu
        /// received from the YubiKey.
        /// </summary>
        /// <remarks>
        /// The caller must also pass in the YubiKey Authentication Expected Response.
        /// These arguments must all be non-null. However, in the case of single
        /// authentication, there will be no YubiKey Authentication Expected Response, so that
        /// argument will be "empty", with a Length of zero.
        /// <para>
        /// If there is data in the <paramref name="yubiKeyAuthenticationExpectedResponse"/>, it must be 8 bytes,
        /// no more, no less.
        /// </para>
        /// <para>
        /// If this is mutual authentication, the response APDU will contain the
        /// response to YubiKey Authentication Challenge.
        /// </para>
        /// </remarks>
        /// <param name="responseApdu">
        /// The object containing the Response APDU returned by the YubiKey.
        /// </param>
        /// <param name = "yubiKeyAuthenticationExpectedResponse">
        /// The bytes the off-card app expects the YubiKey Authentication Response to be.
        /// </param>
        public CompleteAuthenticateManagementKeyResponse(
            ResponseApdu responseApdu,
            ReadOnlyMemory<byte> yubiKeyAuthenticationExpectedResponse)
            : base(responseApdu)
        {
            YubiKeyAuthenticationExpectedResponse = yubiKeyAuthenticationExpectedResponse;
        }

        /// <summary>
        /// Determines the result of the management key authentication.
        /// </summary>
        /// <remarks>
        /// If this is mutual authentication, this method will compare the value from
        /// the response APDU to the <c>YubiKey Authentication Challenge</c>. If they are the
        /// same, the YubiKey will have authenticated itself to the Off-Card app.
        /// <para>
        /// It is suggested to check the value of <see cref="YubiKeyResponse.Status"/> before calling
        /// this method. If the value is neither <see cref="ResponseStatus.Success"/> nor
        /// <see cref="ResponseStatus.AuthenticationRequired"/>, this method will throw an exception.
        /// </para>
        /// </remarks>
        /// <returns>
        /// An <c>AuthenticateManagementKeyResult</c> enum.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if <see cref="YubiKeyResponse.Status"/> is neither
        /// <see cref="ResponseStatus.Success"/> nor <see cref="ResponseStatus.AuthenticationRequired"/>.
        /// </exception>
        /// <exception cref="MalformedYubiKeyResponseException">
        /// Thrown when the data provided does not meet the expectations, and cannot be parsed.
        /// </exception>
        public AuthenticateManagementKeyResult GetData()
        {
            // Verify the data is correct, and whether it is a single or mutual
            // auth.
            // If the data is
            //  7C 0A 82 08 <R2 (8 bytes)>, 90 00
            //  7C 12 82 10 <R2 (16 bytes)>, 90 00
            // the value is mutual auth.
            // If the StatusWord is 9000 (Success) and there is no data, this is
            // single auth.
            // If the StatusWord is 6985, then the management key did not
            // authenticate (the off board app did not authenticate). This can be
            // single or mutual. If YubiKeyAuthenticationExpectedResponse.Length is not 0, this is mutual.
            // Any other StatusWord is an error.
            switch (Status)
            {
                default:
                    throw new InvalidOperationException(StatusMessage);

                case ResponseStatus.AuthenticationRequired:
                    return YubiKeyAuthenticationExpectedResponse.Length == 0
                        ? AuthenticateManagementKeyResult.SingleAuthenticationFailed
                        : AuthenticateManagementKeyResult.MutualOffCardAuthenticationFailed;

                case ResponseStatus.Success:
                    // If there's no data, this is single auth. Make sure the
                    // caller did not pass in any YubiKey Authentication Expected Response data.
                    if (ResponseApdu.Data.Length == 0)
                    {
                        return YubiKeyAuthenticationExpectedResponse.Length == 0
                            ? AuthenticateManagementKeyResult.SingleAuthenticated
                            : throw new MalformedYubiKeyResponseException(
                                string.Format(
                                    CultureInfo.CurrentCulture,
                                    ExceptionMessages.InvalidApduResponseData));
                    }

                    // This is a mutual auth response. And by reaching this
                    // point, we know that the YubiKey authenticated the Off-Card
                    // app. Init the result to YubiKeyAuthenticationFailed, which
                    // means the OffCard authenticated. If the expected response
                    // is correct, change it to fully authenticated.
                    var tlvReader = new TlvReader(ResponseApdu.Data);
                    if (tlvReader.TryReadNestedTlv(out tlvReader, EncodingTag))
                    {
                        if (tlvReader.TryReadValue(out ReadOnlyMemory<byte> responseValue, ResponseTag))
                        {
                            return MemoryExtensions.SequenceEqual(
                                responseValue.Span, YubiKeyAuthenticationExpectedResponse.Span)
                                ? AuthenticateManagementKeyResult.MutualFullyAuthenticated
                                : AuthenticateManagementKeyResult.MutualYubiKeyAuthenticationFailed;
                        }
                    }

                    throw new MalformedYubiKeyResponseException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.InvalidApduResponseData));
            }
        }
    }
}
