// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using Yubico.YubiKit.Core.Devices;

namespace Yubico.YubiKit.Core.Transports.Hid.Linux;

// Typed factory/discovery selection and claim transfer belong to L5.
internal sealed class LinuxOtpHidConnection : IOtpHidConnection
{
    private readonly LinuxOtpHidNativeOwner _owner;
    private LinuxOtpHidConnection(LinuxOtpHidNativeOwner owner) => _owner = owner;
    public int FeatureReportSize => 8;
    public ConnectionType Type => ConnectionType.HidOtp;

    internal static async Task<LinuxOtpHidConnection> OpenAsync(string path, ILinuxHidReportApi reportApi,
        CancellationToken cancellationToken, IDisposable? physicalClaim = null)
    {
        var owner = await LinuxOtpHidNativeOwner.OpenAsync(path, reportApi, cancellationToken, physicalClaim).ConfigureAwait(false);
        try { return new LinuxOtpHidConnection(owner); }
        catch (Exception constructionError)
        {
            try { await owner.ShutdownAsync().ConfigureAwait(false); }
            catch (Exception releaseError) { throw new AggregateException(constructionError, releaseError); }
            throw;
        }
    }

    public Task SendAsync(ReadOnlyMemory<byte> report, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return report.Length != 8
            ? throw new ArgumentException("OTP feature reports must contain exactly 8 bytes.", nameof(report))
            : _owner.SendAsync(report, cancellationToken);
    }

    public Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default) =>
        _owner.ReceiveAsync(cancellationToken);

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

    ~LinuxOtpHidConnection() => _owner?.RequestShutdown();
}