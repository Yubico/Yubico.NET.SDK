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

using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Protocols.Otp.Hid;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Core.Protocols;

/// <summary>
///     Typed binding between an opened YubiKey connection and the Core protocol that drives it.
/// </summary>
public abstract class YubiKeyProtocol : IProtocol
{
    public abstract ConnectionType ConnectionType { get; }
    public abstract IProtocol Inner { get; }

    public static SmartCard Create(
        ISmartCardConnection connection,
        ILoggerFactory? loggerFactory = null)
    {
        ArgumentNullException.ThrowIfNull(connection);

        var resolvedLoggerFactory = ResolveLoggerFactory(loggerFactory);
        return new SmartCard(new PcscProtocol(
            connection,
            logger: resolvedLoggerFactory.CreateLogger<PcscProtocol>()));
    }

    public static FidoHid Create(
        IFidoHidConnection connection,
        ILoggerFactory? loggerFactory = null)
    {
        ArgumentNullException.ThrowIfNull(connection);

        var resolvedLoggerFactory = ResolveLoggerFactory(loggerFactory);
        return new FidoHid(new FidoHidProtocol(
            connection,
            resolvedLoggerFactory.CreateLogger<FidoHidProtocol>()));
    }

    public static OtpHid Create(
        IOtpHidConnection connection,
        ILoggerFactory? loggerFactory = null)
    {
        ArgumentNullException.ThrowIfNull(connection);

        var resolvedLoggerFactory = ResolveLoggerFactory(loggerFactory);
        return new OtpHid(new OtpHidProtocol(
            connection,
            resolvedLoggerFactory.CreateLogger<OtpHidProtocol>()));
    }

    public static YubiKeyProtocol Create(
        IConnection connection,
        ILoggerFactory? loggerFactory = null)
    {
        ArgumentNullException.ThrowIfNull(connection);

        return connection switch
        {
            ISmartCardConnection smartCard => Create(smartCard, loggerFactory),
            IFidoHidConnection fidoHid => Create(fidoHid, loggerFactory),
            IOtpHidConnection otpHid => Create(otpHid, loggerFactory),
            _ => throw new NotSupportedException(
                $"Connection type {connection.GetType().Name} is not supported by Core protocol binding.")
        };
    }

    public void Configure(FirmwareVersion version, ProtocolConfiguration? configuration = null) =>
        Inner.Configure(version, configuration);

    public void Dispose() =>
        Inner.Dispose();

    internal YubiKeyProtocol Rebind(IProtocol protocol)
    {
        ArgumentNullException.ThrowIfNull(protocol);

        if (ReferenceEquals(protocol, Inner))
            return this;

        return this switch
        {
            SmartCard => protocol is ISmartCardProtocol smartCardProtocol
                ? new SmartCard(smartCardProtocol)
                : throw new InvalidOperationException(
                    $"Cannot bind {protocol.GetType().Name} to a SmartCard protocol."),
            FidoHid => protocol is IFidoHidProtocol fidoHidProtocol
                ? new FidoHid(fidoHidProtocol)
                : throw new InvalidOperationException(
                    $"Cannot bind {protocol.GetType().Name} to a FIDO HID protocol."),
            OtpHid => protocol is IOtpHidProtocol otpHidProtocol
                ? new OtpHid(otpHidProtocol)
                : throw new InvalidOperationException(
                    $"Cannot bind {protocol.GetType().Name} to an OTP HID protocol."),
            _ => throw new InvalidOperationException(
                $"Unsupported protocol binding type {GetType().Name}.")
        };
    }

    private static ILoggerFactory ResolveLoggerFactory(ILoggerFactory? loggerFactory) =>
        loggerFactory ?? YubiKitLogging.LoggerFactory;

    public sealed class SmartCard(ISmartCardProtocol protocol) : YubiKeyProtocol
    {
        public ISmartCardProtocol Protocol { get; } =
            protocol ?? throw new ArgumentNullException(nameof(protocol));

        public override ConnectionType ConnectionType => ConnectionType.SmartCard;
        public override IProtocol Inner => Protocol;
    }

    public sealed class FidoHid(IFidoHidProtocol protocol) : YubiKeyProtocol
    {
        public IFidoHidProtocol Protocol { get; } =
            protocol ?? throw new ArgumentNullException(nameof(protocol));

        public override ConnectionType ConnectionType => ConnectionType.HidFido;
        public override IProtocol Inner => Protocol;
    }

    public sealed class OtpHid(IOtpHidProtocol protocol) : YubiKeyProtocol
    {
        public IOtpHidProtocol Protocol { get; } =
            protocol ?? throw new ArgumentNullException(nameof(protocol));

        public override ConnectionType ConnectionType => ConnectionType.HidOtp;
        public override IProtocol Inner => Protocol;
    }
}