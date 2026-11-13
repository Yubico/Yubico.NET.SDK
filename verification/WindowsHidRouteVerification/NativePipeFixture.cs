// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using System.IO.Pipes;
using Yubico.YubiKit.Core.Transports.Hid.Windows;

namespace WindowsHidRouteVerification;

// The server uses the BCL's native byte-mode pipe implementation. The client is opened ONLY
// by WindowsHidReportApi.Open (CreateHidFile, reference acquisition and BindHandle).
internal sealed class NativePipeFixture : IDisposable
{
    internal static readonly WindowsHidReportMetadata Metadata = new(0xf1d0, 1, 65, 65);
    private readonly string _name = $"yubikit-w1-{Guid.NewGuid():N}";
    internal NamedPipeServerStream Peer { get; }
    internal string Path => @"\\.\pipe\" + _name;
    internal Task Connected { get; private set; }

    internal NativePipeFixture()
    {
        Peer = new NamedPipeServerStream(_name, PipeDirection.InOut, 1,
            PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly,
            4096, 4096);
        Connected = Peer.WaitForConnectionAsync();
    }

    internal async Task<WindowsFidoHidConnection> OpenAsync(ObservedReportApi api)
    {
        WindowsFidoHidConnection connection = await WindowsFidoHidConnection.OpenAsync(Path, api, CancellationToken.None);
        await Connected;
        return connection;
    }

    internal void PrepareReopen()
    {
        Peer.Disconnect();
        Connected = Peer.WaitForConnectionAsync();
    }

    internal static byte[] Report(byte marker)
    {
        var report = new byte[65];
        report.AsSpan(1).Fill(marker);
        return report;
    }

    public void Dispose() => Peer.Dispose();
}