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
using System.Buffers.Binary;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Piv.Commands;

/// <summary>
///     The response to the get serial number command, containing the YubiKey's
///     serial number.
/// </summary>
/// <remarks>
///     <para>
///         This is the partner Response class to <see cref="GetSerialNumberCommand" />.
///     </para>
///     <para>
///         The data returned is an <c>int</c>.
///     </para>
///     <para>
///         Example:
///     </para>
///     <code language="csharp">
///   IYubiKeyConnection connection = key.Connect(YubiKeyApplication.Piv);
///   SerialCommand serialCommand = new GetSerialNumberCommand();
///   GetSerialNumberResponse serialResponse = connection.SendCommand(serialCommand);<br />
///   if (serialResponse.Status == ResponseStatus.Success)
///   {
///     int serialNum = serialResponse.GetData();
///   }
/// </code>
/// </remarks>
public sealed class GetSerialNumberResponse : PivResponse, IYubiKeyResponseWithData<int>
{
    private const int SerialNumberLength = 4;

    /// <summary>
    ///     Constructs a GetSerialNumberResponse based on a ResponseApdu received
    ///     from the YubiKey.
    /// </summary>
    /// <param name="responseApdu">
    ///     The object containing the response APDU<br />returned by the YubiKey.
    /// </param>
    public GetSerialNumberResponse(ResponseApdu responseApdu) :
        base(responseApdu)
    {
    }

    #region IYubiKeyResponseWithData<int> Members

    /// <summary>
    ///     Gets the serial number from the YubiKey response.
    /// </summary>
    /// <returns>
    ///     The data in the response APDU, presented as an int.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown if <see cref="YubiKeyResponse.Status" /> is not equal to
    ///     <see cref="ResponseStatus.Success" />.
    /// </exception>
    /// <exception cref="MalformedYubiKeyResponseException">
    ///     Thrown when the <c>ResponseApdu.Data</c> does not meet the expectations
    ///     of the parser.
    /// </exception>
    public int GetData()
    {
        if (Status != ResponseStatus.Success)
        {
            throw new InvalidOperationException(StatusMessage);
        }

        if (ResponseApdu.Data.Length < SerialNumberLength)
        {
            throw new MalformedYubiKeyResponseException
            {
                ResponseClass = nameof(GetSerialNumberResponse),
                ExpectedDataLength = SerialNumberLength,
                ActualDataLength = ResponseApdu.Data.Length
            };
        }

        return BinaryPrimitives.ReadInt32BigEndian(ResponseApdu.Data.Span[..4]);
    }

    #endregion
}
