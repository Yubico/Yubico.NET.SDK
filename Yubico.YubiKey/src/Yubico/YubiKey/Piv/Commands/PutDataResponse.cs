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

using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Piv.Commands;

/// <summary>
///     The response to the PUT DATA command.
/// </summary>
/// <remarks>
///     This is the partner Response class to <see cref="PutDataCommand" />.
///     <para>
///         There is no data returned by this class (no <c>GetData</c> method). To
///         determine the result of the command, look at the <c>Status</c> property.
///     </para>
///     <para>
///         There are two possible outcomes: (1) the data was PUT (<c>Success</c>),
///         (2) an error occurred. For example, if the management key or PIN was
///         not verified, the <c>Status</c> would be <c>AuthenticationRequired</c>.
///     </para>
///     <para>
///         Example:
///     </para>
///     <code language="csharp">
///   /* This example assumes there is some code that will build an encoded
///      certificate from an X509Certificate2 object. */
///   byte[] encodedCertificate = PivPutDataEncodeCertificate(certObject);<br />
///   IYubiKeyConnection connection = key.Connect(YubiKeyApplication.Piv);<br />
///   PutDataCommand putDataCommand = new PutDataCommand(
///       PivDataTag.Authentication, encodedCertificate);
///   PutDataResponse putDataResponse = connection.SendCommand(putDataCommand);<br />
///   if (getDataResponse.Status != ResponseStatus.Success)
///   {
///       /* handle case where the the PUT did not work. */
///   }
/// </code>
/// </remarks>
public sealed class PutDataResponse : PivResponse
{
    /// <summary>
    ///     Constructs a <c>PutDataResponse</c> object based on a ResponseApdu
    ///     received from the YubiKey.
    /// </summary>
    /// <param name="responseApdu">
    ///     The object containing the response APDU<br />returned by the YubiKey.
    /// </param>
    public PutDataResponse(ResponseApdu responseApdu) :
        base(responseApdu)
    {
    }
}
