// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Ctap2;
using Yubico.YubiKit.Ctap2.Examples.FidoTool.FidoExamples;

namespace Yubico.YubiKit.Cli.Commands.UnitTests.FidoTool;

public class ResetAuthenticatorTests
{
    [Theory]
    [InlineData(42, true, true)]
    [InlineData(99, true, false)]
    [InlineData(null, true, true)]
    [InlineData(null, false, false)]
    public async Task ResetAsync_UsesConnectionThatWasChecked(int? serial, bool matchingFingerprint, bool success)
    {
        var ct = TestContext.Current.CancellationToken;
        var device = new ReaderWithReplacementCard();
        var aaguid = new byte[16];
        if (!matchingFingerprint)
            aaguid[0] = 1;
        var fingerprint = new AuthenticatorInfo
        {
            Versions = ["FIDO_2_0"],
            Aaguid = aaguid,
            FirmwareVersion = new FirmwareVersion(5, 8, 0)
        };
        var preflight = new ResetAuthenticator.ResetPreflightInfo(device, ConnectionType.SmartCard, serial, fingerprint);

        var result = await ResetAuthenticator.ResetAsync(preflight, ct);

        Assert.Equal(success, result.Success);
        Assert.Equal(0, device.Replacement.Resets);
        Assert.Equal(1, device.Opens);
        Assert.Equal(success ? 1 : 0, device.First.Resets);
        Assert.Equal(1, device.First.Disposals);
        if (serial is not null)
        {
            Assert.Equal(2, device.First.ManagementReads); // Initialization, then an explicit live read.
            Assert.Equal(success ? 2 : 0, device.First.FidoSelections); // Before reset, then refresh.
        }
        else
        {
            Assert.Equal(0, device.First.ManagementReads);
        }
    }

    [Theory]
    [InlineData(7, 8, false, false)] // Live production version must gate even if GetInfo looks newer.
    [InlineData(7, 7, true, true)] // A live beta qualifier permits the below-threshold numeric version.
    [InlineData(0, 7, false, true)] // Live unknown firmware is optimistic, even with numeric GetInfo.
    public async Task ResetAsync_UsesFirmwareFromTheSameLiveSerialCheck(byte managementMinor, byte appletMinor, bool beta, bool success)
    {
        var ct = TestContext.Current.CancellationToken;
        var device = new ReaderWithReplacementCard(managementMinor, appletMinor, beta);
        var target = new ResetAuthenticator.ResetPreflightInfo(device, ConnectionType.SmartCard, 42,
            new AuthenticatorInfo { Versions = ["FIDO_2_0"], Aaguid = new byte[16] });

        var result = await ResetAuthenticator.ResetAsync(target, ct);

        Assert.Equal(success, result.Success);
        if (!success)
            Assert.Contains("FIDO2 over USB SmartCard requires firmware", result.ErrorMessage);
        Assert.Equal(1, device.Opens);
        Assert.Equal(2, device.First.ManagementReads);
        Assert.Equal(success ? 2 : 1, device.First.FidoSelections);
        Assert.Equal(success ? 1 : 0, device.First.Resets);
        Assert.Equal(1, device.First.Disposals);
        Assert.Equal(0, device.Replacement.Resets);
    }

    private sealed class ReaderWithReplacementCard(byte managementMinor = 8, byte appletMinor = 8, bool beta = false) : IYubiKey
    {
        public string DeviceId => "software-reset-reader";
        public ConnectionType AvailableConnections => ConnectionType.SmartCard;
        public int Opens { get; private set; }
        public ScriptedCard First { get; } = new(42, managementMinor, appletMinor, beta);
        public ScriptedCard Replacement { get; } = new(99);

        public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
            where TConnection : class, IConnection =>
            Task.FromResult((++Opens == 1 ? First : Replacement) as TConnection
                ?? throw new NotSupportedException("Only the scripted SmartCard reader is available."));
    }

    private sealed class ScriptedCard(byte serial, byte managementMinor = 8, byte appletMinor = 8, bool beta = false) : ISmartCardConnection
    {
        // GetInfo versions, AAGUID, and 5.8.0 firmware, with CTAP and ISO status bytes.
        private static readonly byte[] Info = Convert.FromHexString("00A30181684649444F5F325F300350000000000000000000000000000000000E1A000508009000");
        private static readonly byte[] ManagementAid = Convert.FromHexString("A000000527471117");
        private static readonly byte[] FidoAid = Convert.FromHexString("A0000006472F0001");
        private bool _managementSelected;
        private bool _fidoSelected;
        public int Resets { get; private set; }
        public int Disposals { get; private set; }
        public int ManagementReads { get; private set; }
        public int FidoSelections { get; private set; }
        public ConnectionType Type => ConnectionType.SmartCard;
        public Transport Transport => Transport.Usb;

        public Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(ReadOnlyMemory<byte> command,
            CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(Disposals != 0, this);
            ReadOnlyMemory<byte> response = command.Span[1] switch
            {
                0xA4 => Select(command.Span.Slice(5, command.Span[4])),
                0x1D when _managementSelected => ReadConfig(),
                0x10 when _fidoSelected && command.Span[5] == 0x04 => ReadFidoInfo(),
                0x10 when _fidoSelected && command.Span[5] == 0x07 => Reset(),
                _ => throw new InvalidOperationException("Command was sent without selecting its applet.")
            };
            return Task.FromResult(response);
        }

        private byte[] Select(ReadOnlySpan<byte> aid)
        {
            _managementSelected = aid.SequenceEqual(ManagementAid);
            _fidoSelected = aid.SequenceEqual(FidoAid);
            if (_fidoSelected)
            {
                FidoSelections++;
                return [0x90, 0x00];
            }
            if (_managementSelected)
                return [0x90, 0x00];
            throw new InvalidOperationException("Unexpected applet AID.");
        }

        private byte[] ReadConfig()
        {
            ManagementReads++;
            byte major = managementMinor == 0 ? (byte)0 : (byte)5;
            // SERIAL_NUMBER (02), FIRMWARE_VERSION (05), and optional beta VERSION_QUALIFIER (19).
            return beta
                ? [24, 0x02, 4, 0, 0, 0, serial, 0x05, 3, major, managementMinor, 0,
                    0x19, 11, 1, 3, major, managementMinor, 0, 2, 1, 1, 3, 1, 1, 0x90, 0x00]
                : [11, 0x02, 4, 0, 0, 0, serial, 0x05, 3, major, managementMinor, 0, 0x90, 0x00];
        }

        private byte[] ReadFidoInfo() => [.. Info.AsSpan(0, Info.Length - 4), appletMinor, 0, 0x90, 0x00];

        private byte[] Reset()
        {
            Resets++;
            return [0, 0x90, 0x00];
        }

        public bool SupportsExtendedApdu() => false;
        public IDisposable BeginTransaction(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("This fixture does not use transactions.");
        public void Dispose() => Disposals++;
        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
