// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Protocols.Otp.Hid;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Core.Protocols;

/// <summary>
///     Creates Core protocol implementations from opened YubiKey connections.
/// </summary>
internal static class ProtocolFactory
{
    public static IProtocol Create(IConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        return connection switch
        {
            ISmartCardConnection smartCard => Create(smartCard),
            IFidoHidConnection fidoHid => Create(fidoHid),
            IOtpHidConnection otpHid => Create(otpHid),
            _ => throw new NotSupportedException(
                $"Connection type {connection.GetType().Name} is not supported by ProtocolFactory.")
        };
    }

    public static ISmartCardProtocol Create(ISmartCardConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        return new PcscProtocol(
            connection,
            logger: YubiKitLogging.CreateLogger<PcscProtocol>());
    }

    public static IFidoHidProtocol Create(IFidoHidConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        return new FidoHidProtocol(
            connection,
            YubiKitLogging.CreateLogger<FidoHidProtocol>());
    }

    public static IOtpHidProtocol Create(IOtpHidConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        return new OtpHidProtocol(
            connection,
            YubiKitLogging.CreateLogger<OtpHidProtocol>());
    }
}