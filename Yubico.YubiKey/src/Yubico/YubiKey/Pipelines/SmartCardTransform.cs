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
using Yubico.Core.Devices.SmartCard;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Pipelines;

/// <summary>
///     Represents an ApduPipeline backed by a direct connection
///     to a specific application on a smartcard.
/// </summary>
internal class SmartCardTransform : IApduTransform
{
    private readonly ISmartCardConnection _smartCardConnection;

    public SmartCardTransform(ISmartCardConnection smartCardConnection)
    {
        if (smartCardConnection is null)
        {
            throw new ArgumentNullException(nameof(smartCardConnection));
        }

        _smartCardConnection = smartCardConnection;
    }

    #region IApduTransform Members

    public void Cleanup()
    {
    }

    public ResponseApdu Invoke(CommandApdu command, Type commandType, Type responseType) =>
        _smartCardConnection.Transmit(command);

    public void Setup()
    {
    }

    #endregion
}
