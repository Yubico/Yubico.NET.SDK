// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using System.Runtime.Versioning;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Native;

namespace Yubico.YubiKit.Core.Transports.Hid.MacOS;

/// <summary>
///     Synchronous HID report compatibility surface over the persistent macOS FIDO input owner.
/// </summary>
[SupportedOSPlatform("macos")]
internal sealed class MacOSHidIOReportConnection : IHidConnection
{
    private readonly MacOSFidoHidConnection _connection;

    public MacOSHidIOReportConnection(long entryId) : this(entryId, new NativeHidInputBridge()) { }

    internal MacOSHidIOReportConnection(long entryId, IHidInputBridge bridge)
    {
        // This expert API intentionally blocks the caller while its worker owns native open.
        try
        {
            _connection = MacOSFidoHidConnection.OpenAsync(entryId, bridge, CancellationToken.None)
                .GetAwaiter().GetResult();
        }
        catch (InvalidOperationException ex) when (ex is not UnrecoveredConnectionException)
        {
            throw new PlatformApiException("Failed to open HID report connection.", ex);
        }
    }

    public int InputReportSize => _connection.InputReportSize;
    public int OutputReportSize => _connection.OutputReportSize;
    public ConnectionType Type => ConnectionType.Hid;

    public byte[] GetReport()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(6));
        try
        {
            return _connection.ReceiveAsync(timeout.Token).GetAwaiter().GetResult().ToArray();
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            // Cancellation detaches only this reader. The native input owner keeps running,
            // so a later report is available on the next synchronous read.
            throw new PlatformApiException("Timed out waiting for HID input report after six seconds.");
        }
    }

    public void SetReport(byte[] report)
    {
        ArgumentNullException.ThrowIfNull(report);
        _connection.SendReportAsync(report).GetAwaiter().GetResult();
    }

    public void Dispose() => _connection.Dispose();

    public ValueTask DisposeAsync() => _connection.DisposeAsync();
}