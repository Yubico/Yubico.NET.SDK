// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using Yubico.YubiKit.Core.Transports.Hid.Windows;

namespace Yubico.YubiKit.Core.UnitTests.Transports.Hid.Windows;

internal sealed class WindowsOtpReportApi : IWindowsHidFeatureReportApi
{
    internal Action<string>? Before { get; set; }
    internal WindowsHidFeatureReportMetadata Metadata { get; set; } = new(1, 6, 9);
    internal WindowsHidRelease Release { get; set; } = new(true, 0);
    internal uint FeatureError { get; set; }
    internal Exception? OpenError { get; set; }
    internal int Opens, Gets, Sets, Closes;
    internal byte[]? Buffer { get; private set; }
    internal Thread? Worker { get; private set; }

    public WindowsHidFeatureReportMetadata Open(string devicePath)
    {
        Assert.Equal("feature", devicePath);
        Worker = Thread.CurrentThread;
        Opens++;
        // This is the zero-access feature seam, not proof of actual CreateFile flags.
        Before?.Invoke("open");
        if (OpenError is not null) throw OpenError;
        return Metadata;
    }

    public WindowsHidFeatureResult GetFeature(byte[] nativeReport)
    {
        Assert.Same(Worker, Thread.CurrentThread);
        Assert.Equal(Metadata.FeatureReportByteLength, nativeReport.Length);
        Assert.Equal(0, nativeReport[0]);
        Buffer = nativeReport;
        Gets++;
        nativeReport.AsSpan(1).Fill(0xA7); // Also models a failed partial GET.
        Before?.Invoke("get");
        return new(FeatureError);
    }

    public WindowsHidFeatureResult SetFeature(byte[] nativeReport)
    {
        Assert.Same(Worker, Thread.CurrentThread);
        Assert.Equal(Metadata.FeatureReportByteLength, nativeReport.Length);
        Assert.Equal(0, nativeReport[0]);
        Buffer = nativeReport;
        Sets++;
        Before?.Invoke("set");
        return new(FeatureError);
    }

    public WindowsHidRelease CloseOnce()
    {
        Assert.Same(Worker, Thread.CurrentThread);
        Closes++;
        Before?.Invoke("close");
        return Release;
    }
}

internal sealed class WindowsOtpClaim : IDisposable
{
    internal int Releases;
    public void Dispose() => Interlocked.Increment(ref Releases);
}