// Copyright 2025 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using System.Runtime.Versioning;
using Yubico.YubiKit.Core.Devices;

namespace Yubico.YubiKit.Core.Transports.Hid.MacOS;

/// <summary>Synchronous feature reports over the connection-owned macOS OTP worker.</summary>
/// <remarks>
///     Expert GET transfers an eight-byte caller-owned array and pads a successful short native result with
///     zeros. Typed OTP receive instead rejects a short result. SET preserves supported caller lengths.
///     Construction and report calls wait for the worker; disposal waits for admitted work and checked close.
/// </remarks>
[SupportedOSPlatform("macos")]
internal sealed class MacOSHidFeatureReportConnection : IHidConnection
{
    private readonly MacOSOtpHidConnection _connection;

    public MacOSHidFeatureReportConnection(long entryId) : this(entryId, MacOSHidReportApi.Instance) { }

    internal MacOSHidFeatureReportConnection(long entryId, IMacOSHidReportApi reportApi)
    {
        // The public report API is synchronous; the native open and any partial-open cleanup
        // still belong to the same worker as report I/O and checked shutdown.
        _connection = MacOSOtpHidConnection.OpenAsync(entryId, reportApi, CancellationToken.None,
            featureMetadata: true).GetAwaiter().GetResult();
    }

    public int InputReportSize => _connection.InputReportSize;
    public int OutputReportSize => _connection.OutputReportSize;
    public ConnectionType Type => ConnectionType.Hid;

    public byte[] GetReport()
    {
        return _connection.GetReportAsync().GetAwaiter().GetResult();
    }

    public void SetReport(byte[] report)
    {
        ArgumentNullException.ThrowIfNull(report);
        _connection.SendReportAsync(report).GetAwaiter().GetResult();
    }

    public void Dispose() => _connection.Dispose();

    public ValueTask DisposeAsync() => _connection.DisposeAsync();
}