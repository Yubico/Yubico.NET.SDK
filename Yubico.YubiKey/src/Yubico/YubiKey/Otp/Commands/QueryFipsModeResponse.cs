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

namespace Yubico.YubiKey.Otp.Commands;

/// <summary>
///     The response to the <see cref="QueryFipsModeCommand" /> command, containing the YubiKey's
///     current FIPS status.
/// </summary>
public class QueryFipsModeResponse : OtpResponse, IYubiKeyResponseWithData<bool>
{
    private const int FipsModeLength = 1;

    /// <summary>
    ///     Constructs a QueryFipsModeResponse based on a ResponseApdu received from the YubiKey.
    /// </summary>
    /// <param name="responseApdu">
    ///     The object containing the response APDU returned by the YubiKey.
    /// </param>
    public QueryFipsModeResponse(ResponseApdu responseApdu) :
        base(responseApdu)
    {
    }

    #region IYubiKeyResponseWithData<bool> Members

    /// <summary>
    ///     Gets the FIPS status.
    /// </summary>
    /// <returns>The data in the ResponseAPDU, presented as a boolean value.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when <see cref="YubiKeyResponse.Status" /> is not <see cref="ResponseStatus.Success" />.
    /// </exception>
    /// <exception cref="MalformedYubiKeyResponseException">
    ///     Thrown when the data received from the YubiKey does not
    ///     match the expectations of the parser.
    /// </exception>
    public bool GetData()
    {
        if (Status != ResponseStatus.Success)
        {
            throw new InvalidOperationException(StatusMessage);
        }

        if (ResponseApdu.Data.Length != FipsModeLength)
        {
            throw new MalformedYubiKeyResponseException
            {
                ResponseClass = nameof(QueryFipsModeResponse),
                ExpectedDataLength = FipsModeLength,
                ActualDataLength = ResponseApdu.Data.Length
            };
        }

        return ResponseApdu.Data.Span[0] == 1;
    }

    #endregion
}
