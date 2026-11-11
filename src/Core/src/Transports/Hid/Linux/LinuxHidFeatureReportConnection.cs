// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Yubico.YubiKit.Core.Devices;

namespace Yubico.YubiKit.Core.Transports.Hid.Linux;

/// <summary>Synchronous expert feature-report facade over the Linux OTP native lifetime.</summary>
[SupportedOSPlatform("linux")]
internal sealed class LinuxHidFeatureReportConnection : IHidConnection
{
    private readonly LinuxOtpHidNativeOwner _owner;

    public LinuxHidFeatureReportConnection(string devNode) : this(devNode, LinuxHidReportApi.Instance) { }

    internal LinuxHidFeatureReportConnection(string devNode, ILinuxHidReportApi api) =>
        _owner = LinuxOtpHidNativeOwner.OpenAsync(devNode, api, CancellationToken.None).GetAwaiter().GetResult();

    public int InputReportSize => 8;
    public int OutputReportSize => 8;
    public ConnectionType Type => ConnectionType.Hid;

    public byte[] GetReport()
    {
        var report = _owner.ReceiveAsync(CancellationToken.None).GetAwaiter().GetResult();
        // The owner returns an exact, exclusively owned array; transfer without a second copy.
        return MemoryMarshal.TryGetArray(report, out var segment) && segment.Array is { } array && segment.Offset == 0 && segment.Count == array.Length
            ? array
            : throw new InvalidOperationException("Linux OTP owner returned non-array storage.");
    }

    public void SetReport(byte[] report)
    {
        ArgumentNullException.ThrowIfNull(report);
        _owner.SendAsync(report, CancellationToken.None).GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        _owner.Dispose();
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await _owner.ShutdownAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    ~LinuxHidFeatureReportConnection() => _owner?.RequestShutdown();
}