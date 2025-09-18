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
using System.Globalization;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Scp.Commands;

internal class ExternalAuthenticateResponse : ScpResponse
{
    /// <summary>
    ///     Constructs an ExternalAuthenticateResponse based on a ResponseApdu received from the YubiKey.
    /// </summary>
    /// <param name="responseApdu">
    ///     The ResponseApdu that corresponds to the issuance of
    ///     this command.
    /// </param>
    public ExternalAuthenticateResponse(ResponseApdu responseApdu) :
        base(responseApdu)
    {
        if (responseApdu is null)
        {
            throw new ArgumentNullException(nameof(responseApdu));
        }

        if (responseApdu.SW == SWConstants.Success)
        {
            return;
        }

        string message = string.Format(
            CultureInfo.CurrentCulture,
            $"{ExceptionMessages.IncorrectExternalAuthenticateData}" + " " +
            $"SW: 0x{responseApdu.SW.ToString("X4", CultureInfo.InvariantCulture)}");

        // Example output: IncorrectExternalAuthenticateData SW: 0x6300
        throw new ArgumentException(message, nameof(responseApdu));
    }
}
