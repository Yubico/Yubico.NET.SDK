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
    /// The response to the get version command, containing the YubiKey's
    /// firmware version.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the partner Response class to <see cref="VersionCommand"/>.
    /// </para>
    /// <para>
    /// The data returned is <see cref="FirmwareVersion"/>.
    /// </para>
    /// <para>
    /// Example:
    /// </para>
    /// <code language="csharp">
    ///   IList&lt;IYubiKeyDevice&gt; keys = YubiKey.GetList();
    ///   foreach (IYubiKeyDevice key in keys)
    ///   {
    ///       IYubiKeyConnection connection = key.Connect(YubiKeyApplication.Piv);
    ///       VersionCommand versionCmd = new VersionCommand();
    ///       VersionResponse versionRsp = connection.SendCommand(versionCmd);<br/>
    ///       if (versionNum.Status != ResponseStatus.Success)
    ///       {
    ///         // Handle error
    ///       }
    ///
    ///       FirmwareVersion versionNum = versionRsp.GetData();
    ///   }
    /// </code>
    /// </remarks>
    public sealed class VersionResponse : PivResponse, IYubiKeyResponseWithData<FirmwareVersion>
    {
        private const int VersionLength = 3;

        /// <summary>
        /// Constructs a VersionResponse based on a ResponseApdu received from
        /// the YubiKey.
        /// </summary>
        /// <param name="responseApdu">
        /// The object containing the response APDU<br/>returned by the YubiKey.
        /// </param>
        public VersionResponse(ResponseApdu responseApdu) :
            base(responseApdu)
        {
        }

        /// <summary>
        /// Gets the version from the YubiKey response.
        /// </summary>
        /// <returns>
        /// The data in the response APDU, presented as a FirmwareVersion object.
        /// </returns>
        /// <exception cref="MalformedYubiKeyResponseException">
        /// Thrown when the <c>ResponseApdu.Data</c> does not meet the expectations
        /// of the parser.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <see cref="YubiKeyResponse.Status"/> is not <see cref="ResponseStatus.Success"/>.
        /// </exception>
        public FirmwareVersion GetData()
        {
            if (Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(StatusMessage);
            }

            if (ResponseApdu.Data.Length < VersionLength)
            {
                throw new MalformedYubiKeyResponseException()
                {
                    ResponseClass = nameof(VersionResponse),
                    ExpectedDataLength = VersionLength,
                    ActualDataLength = ResponseApdu.Data.Length,
                };
            }

            var responseApduDataSpan = ResponseApdu.Data.Span;
            return new FirmwareVersion
            {
                Major = responseApduDataSpan[0],
                Minor = responseApduDataSpan[1],
                Patch = responseApduDataSpan[2]
            };
        }
    }
}
