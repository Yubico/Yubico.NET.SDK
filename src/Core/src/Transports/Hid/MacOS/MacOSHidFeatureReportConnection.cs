// Copyright 2025 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Yubico.YubiKit.Core.Devices;

namespace Yubico.YubiKit.Core.Transports.Hid.MacOS;

/// <summary>Synchronous expert feature reports over the connection-owned macOS OTP worker.</summary>
[SupportedOSPlatform("macos")]
internal sealed class MacOSHidFeatureReportConnection : IHidConnection
{
    private readonly MacOSOtpHidConnection _connection;

    public MacOSHidFeatureReportConnection(long entryId) : this(entryId, IOKitDeviceLifetime.Instance) { }

    internal MacOSHidFeatureReportConnection(long entryId, IIOKitDeviceLifetime lifetime)
    {
        // The public expert API is synchronous; the native open and any partial-open cleanup
        // still belong to the same worker as report I/O and checked shutdown.
        _connection = MacOSOtpHidConnection.OpenAsync(entryId, lifetime, CancellationToken.None,
            featureMetadata: true).GetAwaiter().GetResult();
    }

    public int InputReportSize => _connection.InputReportSize;
    public int OutputReportSize => _connection.OutputReportSize;
    public ConnectionType Type => ConnectionType.Hid;

    public byte[] GetReport()
    {
        ReadOnlyMemory<byte> report = _connection.GetReportAsync().GetAwaiter().GetResult();
        // The OTP owner allocates one whole array per GET and relinquishes it to the caller.
        if (!MemoryMarshal.TryGetArray(report, out ArraySegment<byte> segment) || segment.Array is not { } array ||
            segment.Offset != 0 || segment.Count != array.Length)
            throw new InvalidOperationException("OTP report owner did not return a whole array.");
        return array;
    }

    public void SetReport(byte[] report)
    {
        ArgumentNullException.ThrowIfNull(report);
        _connection.SendReportAsync(report).GetAwaiter().GetResult();
    }

    public void Dispose() => _connection.Dispose();

    public ValueTask DisposeAsync() => _connection.DisposeAsync();
}
