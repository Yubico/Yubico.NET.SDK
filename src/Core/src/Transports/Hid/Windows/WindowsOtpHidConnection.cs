// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using Yubico.YubiKit.Core.Devices;

namespace Yubico.YubiKit.Core.Transports.Hid.Windows;

/// <summary>Typed eight-byte Windows OTP feature connection over one owned blocking native lifetime.</summary>
internal sealed class WindowsOtpHidConnection : IOtpHidConnection
{
    private readonly WindowsOtpHidNativeOwner _owner;

    private WindowsOtpHidConnection(WindowsOtpHidNativeOwner owner) => _owner = owner;

    /// <summary>Transfers the original claim to an owner before opening the zero-access feature seam.</summary>
    /// <remarks>Failed open or incompatible typed metadata drains checked cleanup before propagating failure.</remarks>
    internal static async Task<WindowsOtpHidConnection> OpenAsync(string devicePath, IWindowsHidFeatureReportApi reportApi,
        CancellationToken cancellationToken, IDisposable? physicalClaim = null)
    {
        var owner = await WindowsOtpHidNativeOwner.OpenAsync(devicePath, reportApi, cancellationToken, physicalClaim).ConfigureAwait(false);
        try
        {
            if (owner.Metadata.FeatureReportByteLength != 9)
                throw new IOException("Typed OTP requires eight feature payload bytes.");
            return new WindowsOtpHidConnection(owner);
        }
        catch (Exception constructionError)
        {
            try { await owner.ShutdownAsync().ConfigureAwait(false); }
            catch (Exception releaseError) { throw new AggregateException(constructionError, releaseError); }
            throw;
        }
    }

    /// <inheritdoc />
    public int FeatureReportSize => 8;
    /// <inheritdoc />
    public ConnectionType Type => ConnectionType.HidOtp;

    /// <summary>Copies eight payload bytes before returning the actual operation task; clears staging after native return.</summary>
    /// <remarks>Cancellation prevents dispatch only. Accepted GET/SET retain storage and admission until native return; success wins late cancellation.</remarks>
    public Task SendAsync(ReadOnlyMemory<byte> report, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return report.Length != 8
            ? throw new ArgumentException("OTP feature reports must contain exactly 8 bytes.", nameof(report))
            : _owner.SendAsync(report, cancellationToken);
    }

    /// <summary>Returns the actual GET task, yielding a separate caller-owned eight-byte payload array.</summary>
    /// <remarks>
    /// Native staging is cleared before completion, including on failure. Successful payload storage is
    /// not cleared at disposal; the caller owns its sensitive-data cleanup. Cancellation cannot detach
    /// a dispatched GET. HidD success supplies no actual transfer count.
    /// </remarks>
    public Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default) =>
        _owner.ReceiveAsync(cancellationToken);

    /// <summary>Synchronously drains the shared native lifetime, with no timeout and no worker self-wait.</summary>
    public void Dispose()
    {
        _owner.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>Observes the shared actual drain; unproven release retains the original owner and physical claim.</summary>
    public ValueTask DisposeAsync()
    {
        var closed = _owner.ShutdownAsync();
        GC.SuppressFinalize(this); // Owner is already rooted and shutdown requested.
        return new ValueTask(closed);
    }

    /// <summary>Requests shutdown only; never waits or releases a claim on the finalizer thread.</summary>
    ~WindowsOtpHidConnection() => _owner?.RequestShutdown();
}