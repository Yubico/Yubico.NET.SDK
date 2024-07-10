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
    ///     The response to <see cref="GetBioMetadataCommand" />, containing Bio Metadata about a
    ///     particular key.
    /// </summary>
    /// <remarks>
    ///     The Get Bio Metadata command is available on Bio multi-protocol keys.
    ///     <para>
    ///         This is the partner Response class to <see cref="GetBioMetadataCommand" />.
    ///     </para>
    ///     <para>
    ///         The data returned is a <see cref="PivBioMetadata" />.
    ///     </para>
    ///     <para>
    ///         Example:
    ///     </para>
    ///     <code language="csharp">
    ///   IYubiKeyConnection connection = key.Connect(YubiKeyApplication.Piv);<br />
    ///   GetBioMetadataCommand command = new GetBioMetadataCommand();
    ///   GetBioMetadataResponse response = connection.SendCommand(command);<br />
    ///   if (response.Status == ResponseStatus.Success)
    ///   {
    ///       PivBioMetadata data = response.GetData();
    ///   }
    /// </code>
    /// </remarks>
    public sealed class GetBioMetadataResponse : PivResponse, IYubiKeyResponseWithData<PivBioMetadata>
    {
        /// <summary>
        ///     Constructs a GetBioMetadataResponse based on a ResponseApdu received from
        ///     the YubiKey.
        /// </summary>
        /// <param name="responseApdu">
        ///     The object containing the response APDU<br />returned by the YubiKey.
        /// </param>
        public GetBioMetadataResponse(ResponseApdu responseApdu) :
            base(responseApdu)
        {
        }

        /// <summary>
        ///     Gets the bio metadata from the YubiKey response.
        /// </summary>
        /// <returns>
        ///     The data in the response APDU, presented as a PivBioMetadata object.
        /// </returns>
        /// <exception cref="NotSupportedException">
        ///     Thrown when the device does not support Bio Metadata.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when <see cref="YubiKeyResponse.Status" /> is not <see cref="ResponseStatus.Success" />.
        /// </exception>
        public PivBioMetadata GetData() =>
            Status switch
            {
                ResponseStatus.Success => new PivBioMetadata(ResponseApdu.Data.ToArray()),
                ResponseStatus.NoData => throw new NotSupportedException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.BioMetadataNotSupported)),
                _ => throw new InvalidOperationException(StatusMessage)
            };
    }
}
