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

using System;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Piv.Commands;

/// <summary>
///     The response to the get metadata command, containing information about a
///     particular key.
/// </summary>
/// <remarks>
///     The Get Metadata command is available on YubiKey version 5.3 and later.
///     <para>
///         This is the partner Response class to <see cref="GetMetadataCommand" />.
///     </para>
///     <para>
///         The data returned is a <see cref="PivMetadata" />.
///     </para>
///     <para>
///         Example:
///     </para>
///     <code language="csharp">
///   IYubiKeyConnection connection = key.Connect(YubiKeyApplication.Piv);<br />
///   GetMetadataCommand metadataCommand = new GetMetadataCommand(0x9A);
///   GetMetadataResponse metadataResponse = connection.SendCommand(metadataCommand);<br />
///   if (metadataResponse.Status == ResponseStatus.Success)
///   {
///       PivMetadata keyData = metadataResponse.GetData();
///   }
/// </code>
/// </remarks>
public sealed class GetMetadataResponse : PivResponse, IYubiKeyResponseWithData<PivMetadata>
{
    /// <summary>
    ///     Constructs a GetMetadataResponse based on a ResponseApdu received from
    ///     the YubiKey.
    /// </summary>
    /// <param name="responseApdu">
    ///     The object containing the response APDU<br />returned by the YubiKey.
    /// </param>
    /// <param name="slotNumber">
    ///     The slot for which the metadata references.
    /// </param>
    public GetMetadataResponse(ResponseApdu responseApdu, byte slotNumber) :
        base(responseApdu)
    {
        SlotNumber = slotNumber;
    }

    /// <summary>
    ///     The slot for which the metadata is returned.
    /// </summary>
    /// <value>
    ///     The slot number, see <see cref="PivSlot" />
    /// </value>
    public byte SlotNumber { get; }

    #region IYubiKeyResponseWithData<PivMetadata> Members

    /// <summary>
    ///     Gets the metadata from the YubiKey response.
    /// </summary>
    /// <remarks>
    ///     If the Status is <c>ResponseStatus.NoData</c>, the slot is empty and
    ///     this method will throw an exception. Hence, it is a good idea to check
    ///     <c>Status</c> before calling this method.
    /// </remarks>
    /// <returns>
    ///     The data in the response APDU, presented as a PivMetadata object.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when <see cref="YubiKeyResponse.Status" /> is not <see cref="ResponseStatus.Success" />.
    /// </exception>
    public PivMetadata GetData() =>
        Status switch
        {
            ResponseStatus.Success => new PivMetadata(ResponseApdu.Data.ToArray(), SlotNumber),
            _ => throw new InvalidOperationException(StatusMessage)
        };

    #endregion
}
