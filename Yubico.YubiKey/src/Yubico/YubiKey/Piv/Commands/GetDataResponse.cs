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
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Piv.Commands
{
    /// <summary>
    /// The response to the get data command, containing the information
    /// requested.
    /// </summary>
    /// <remarks>
    /// This is the partner Response class to <see cref="GetDataCommand"/>.
    /// <para>
    /// The data returned is a byte array presented as a <c>ReadOnlyMemory</c>.
    /// It is the raw data returned by the YubiKey.
    /// </para>
    /// <para>
    /// It is possible that the data requested is not on the YubiKey. For
    /// example, suppose you request the certificate in slot 9A, but there is no
    /// certificate in that slot yet. The YubiKey will return the Status Word
    /// <c>6A82</c> (<c>SWConstants.FileOrApplicationNotFound</c>) and no data. In
    /// that case, <c>Status</c> will be <c>NoData</c> and the <c>GetData</c>
    /// method will throw an exception (there is no data).
    /// </para>
    /// <para>
    /// It is possible the YubiKey could not return the requested data
    /// because authentication was required (PIN or management key). If so, it
    /// will return the Status Word <c>6982</c>
    /// (<c>SWConstants.SecurityStatusNotSatisfied</c>) and no data. In that
    /// case, <c>Status</c> will be <c>AuthenticationRequired</c> and the
    /// <c>GetData</c> method will throw an exception.
    /// </para>
    /// <para>
    /// Your code can get the response object and check <c>Status</c> and, if
    /// <c>Success</c>, call <c>GetData</c>. If the <c>Status</c> is anything
    /// else, don't call <c>GetData</c>.
    /// </para>
    /// <para>
    /// Example:
    /// </para>
    /// <code language="csharp">
    ///   IYubiKeyConnection connection = key.Connect(YubiKeyApplication.Piv);<br/>
    ///   GetDataCommand getDataCommand = new GetDataCommand(PivDataTag.Chuid);
    ///   GetDataResponse getDataResponse = connection.SendCommand(getDataCommand);<br/>
    ///   if (getDataResponse.Status != ResponseStatus.Success)
    ///   {
    ///     // Handle error
    ///   }
    ///   byte[] getChuid = getDataResponse.GetData();
    /// </code>
    /// </remarks>
    public sealed class GetDataResponse : PivResponse, IYubiKeyResponseWithData<ReadOnlyMemory<byte>>
    {
        /// <inheritdoc />
        protected override ResponseStatusPair StatusCodeMap =>
            StatusWord switch
            {
                SWConstants.AuthenticationMethodBlocked => new ResponseStatusPair(ResponseStatus.AuthenticationRequired, ResponseStatusMessages.BaseAuthenticationMethodBlocked),
                _ => base.StatusCodeMap,
            };

        /// <summary>
        /// Constructs a GetDataResponse based on a ResponseApdu received from
        /// the YubiKey.
        /// </summary>
        /// <param name="responseApdu">
        /// The object containing the response APDU<br/>returned by the YubiKey.
        /// </param>
        public GetDataResponse(ResponseApdu responseApdu) :
            base(responseApdu)
        {
        }

        /// <summary>
        /// Gets the data requested from the YubiKey response.
        /// </summary>
        /// <remarks>
        /// The return is a byte array, the raw data returned by the YubiKey. It
        /// is the caller's responsibility to parse that data. For example, if
        /// the data returned is a certificate, the caller will likely want to
        /// build an <c>X509Certificate</c> object from the data.
        /// <para>
        /// It is a good idea to call this method, only if the <c>Status</c> is
        /// <c>Success</c>. If it is not <c>Success</c>, an exception will be
        /// thrown.
        /// </para>
        /// <para>
        /// If the Status property is <c>ResponseStatus.NoData</c>,
        /// then that particular element is not availaible. For example, if you
        /// request the cert in slot 9A, but there is no cert in slot 9A, then
        /// this will be the response. In this case, this method will throw an
        /// exception.
        /// </para>
        /// <para>
        /// If the Status is <c>ResponseStatus.AuthenticationRequired</c>, then
        /// the data is not available unless the PIN or
        /// management key is entered. In this case, this method will throw an
        /// exception.
        /// </para>
        /// </remarks>
        /// <returns>
        /// The data in the response APDU, presented as a byte array.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <see cref="YubiKeyResponse.Status"/> is not <see cref="ResponseStatus.Success"/>.
        /// </exception>
        public ReadOnlyMemory<byte> GetData() => StatusWord switch
        {
            SWConstants.Success => ResponseApdu.Data,
            _ => throw new InvalidOperationException(StatusMessage),
        };
    }
}
