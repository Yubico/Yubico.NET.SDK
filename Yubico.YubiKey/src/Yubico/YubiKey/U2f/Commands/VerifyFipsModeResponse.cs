﻿// Copyright 2025 Yubico AB
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

namespace Yubico.YubiKey.U2f.Commands;

/// <summary>
///     The response to the <see cref="VerifyFipsModeCommand" /> command, containing the response from the YubiKey.
/// </summary>
public sealed class VerifyFipsModeResponse : U2fResponse, IYubiKeyResponseWithData<bool>
{
    /// <summary>
    ///     Constructs a VerifyFipsModeResponse based on a ResponseApdu received from the YubiKey.
    /// </summary>
    /// <param name="responseApdu">
    ///     The object containing the response APDU returned by the YubiKey.
    /// </param>
    public VerifyFipsModeResponse(ResponseApdu responseApdu) :
        base(responseApdu)
    {
    }

    /// <inheritdoc />
    protected override ResponseStatusPair StatusCodeMap =>
        StatusWord switch
        {
            SWConstants.FunctionNotSupported => new ResponseStatusPair(
                ResponseStatus.Success, ResponseStatusMessages.BaseSuccess),
            _ => base.StatusCodeMap
        };

    #region IYubiKeyResponseWithData<bool> Members

    /// <summary>
    ///     Gets the response data, presented as a boolean value.
    /// </summary>
    /// <returns>
    ///     Returns true if (and only if) the YubiKey U2F application is currently in "FIPS Approved mode".
    /// </returns>
    public bool GetData() =>
        StatusWord switch
        {
            SWConstants.Success => true,
            SWConstants.FunctionNotSupported => false,
            _ => throw new InvalidOperationException(StatusMessage)
        };

    #endregion
}
