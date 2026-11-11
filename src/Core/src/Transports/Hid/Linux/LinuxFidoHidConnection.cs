// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;

namespace Yubico.YubiKit.Core.Transports.Hid.Linux;

// Not yet selected by the typed factory (L5). The direct facade shares this native owner.
internal sealed class LinuxFidoHidConnection : IFidoHidConnection, ITerminalWakeControl
{
    private readonly LinuxFidoHidNativeOwner _owner;
    private LinuxFidoHidConnection(LinuxFidoHidNativeOwner owner) => _owner = owner;
    public int PacketSize => 64;
    public ConnectionType Type => ConnectionType.HidFido;

    internal static async Task<LinuxFidoHidConnection> OpenAsync(string path, ILinuxHidReportApi reportApi,
        ILinuxHidReadReactor reactor, CancellationToken cancellationToken, IDisposable? physicalClaim = null)
    {
        var owner = await LinuxFidoHidNativeOwner.OpenAsync(path, reportApi, reactor, cancellationToken, physicalClaim).ConfigureAwait(false);
        try { return new LinuxFidoHidConnection(owner); }
        catch (Exception constructionError)
        {
            try { await owner.ShutdownAsync().ConfigureAwait(false); }
            catch (Exception releaseError) { throw new AggregateException(constructionError, releaseError); }
            throw;
        }
    }

    public Task SendAsync(ReadOnlyMemory<byte> packet, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return packet.Length != 64
            ? throw new ArgumentException("FIDO packets must contain exactly 64 bytes.", nameof(packet))
            : _owner.SendAsync(packet, cancellationToken);
    }

    public Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default) =>
        _owner.ReceiveAsync(true, cancellationToken);

    public void RequestTerminalWake() => _owner.Terminal(new ObjectDisposedException(nameof(LinuxFidoHidConnection)));
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
    ~LinuxFidoHidConnection() => _owner?.RequestShutdown();
}