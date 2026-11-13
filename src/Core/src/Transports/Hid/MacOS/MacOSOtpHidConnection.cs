// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using Yubico.YubiKit.Core.Devices;

namespace Yubico.YubiKit.Core.Transports.Hid.MacOS;

internal sealed class MacOSOtpHidConnection : IOtpHidConnection
{
    private readonly MacOSOtpHidNativeOwner _owner;
    private MacOSOtpHidConnection(MacOSOtpHidNativeOwner owner) => _owner = owner;
    public ConnectionType Type => ConnectionType.HidOtp;
    public int FeatureReportSize => MacOSOtpHidNativeOwner.FeatureReportLength;
    internal int InputReportSize => _owner.InputReportSize;
    internal int OutputReportSize => _owner.OutputReportSize;

    internal static async Task<MacOSOtpHidConnection> OpenAsync(long entryId, IMacOSHidReportApi reportApi,
        CancellationToken cancellationToken, IDisposable? physicalClaim = null, bool featureMetadata = false)
    {
        MacOSOtpHidNativeOwner owner;
        try { owner = new MacOSOtpHidNativeOwner(entryId, reportApi, physicalClaim, featureMetadata); }
        catch { physicalClaim?.Dispose(); throw; }
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await owner.OpenAsync(cancellationToken).ConfigureAwait(false);
            return new MacOSOtpHidConnection(owner);
        }
        catch (Exception openFailure)
        {
            try { await owner.ShutdownAsync().ConfigureAwait(false); }
            catch (UnrecoveredConnectionException cleanupFailure)
            {
                throw new UnrecoveredConnectionException("macOS OTP open and native cleanup failed",
                    new AggregateException(openFailure, cleanupFailure));
            }
            catch (Exception cleanupFailure) { throw new AggregateException(openFailure, cleanupFailure); }
            throw;
        }
    }

    public Task SendAsync(ReadOnlyMemory<byte> report, CancellationToken cancellationToken = default) =>
        _owner.SendAsync(report, cancellationToken);
    internal Task SendReportAsync(ReadOnlyMemory<byte> report) => _owner.SendAsync(report, CancellationToken.None, expert: true);
    public Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default) =>
        _owner.ReceiveAsync(cancellationToken);
    internal Task<ReadOnlyMemory<byte>> GetReportAsync() => _owner.ReceiveAsync(CancellationToken.None, expert: true);
    public void Dispose()
    {
        if (_owner.IsWorkerThread)
            throw new InvalidOperationException("Cannot drain the OTP native worker from itself.");
        _owner.ShutdownAsync().GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }
    public async ValueTask DisposeAsync()
    {
        if (_owner.IsWorkerThread)
            throw new InvalidOperationException("Cannot drain the OTP native worker from itself.");
        await _owner.ShutdownAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }
    ~MacOSOtpHidConnection() => _ = _owner.ShutdownAsync();
}