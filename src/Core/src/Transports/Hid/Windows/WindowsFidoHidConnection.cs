// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;

namespace Yubico.YubiKit.Core.Transports.Hid.Windows;

/// <summary>Thin typed FIDO facade over the independently rooted Windows native lifetime.</summary>
internal sealed class WindowsFidoHidConnection : IFidoHidConnection, ITerminalWakeControl
{
    private readonly WindowsFidoHidNativeOwner _owner;

    private WindowsFidoHidConnection(WindowsFidoHidNativeOwner owner) => _owner = owner;

    public int PacketSize => 64;
    public ConnectionType Type => ConnectionType.HidFido;

    /// <summary>Transfers the original physical claim to the owner before native acquisition.</summary>
    internal static async Task<WindowsFidoHidConnection> OpenAsync(string path, IWindowsHidReportApi reportApi,
        CancellationToken cancellationToken, IDisposable? physicalClaim = null)
    {
        var owner = await WindowsFidoHidNativeOwner.OpenAsync(path, reportApi, cancellationToken, physicalClaim).ConfigureAwait(false);
        try { return new WindowsFidoHidConnection(owner); }
        catch (Exception constructionError)
        {
            try { await owner.ShutdownAsync().ConfigureAwait(false); }
            catch (Exception releaseError) { throw new AggregateException(constructionError, releaseError); }
            throw;
        }
    }

    /// <summary>Copies 64 payload bytes; terminal completion follows native retirement and clearing.</summary>
    public Task SendAsync(ReadOnlyMemory<byte> packet, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return packet.Length != 64
            ? throw new ArgumentException("FIDO packets must contain exactly 64 bytes.", nameof(packet))
            : _owner.SendAsync(packet, cancellationToken);
    }

    /// <summary>Transfers independent validated payload storage through the actual operation TCS task.</summary>
    /// <remarks>Caller owns successful storage and its sensitive-data clearing. Cancellation waits for native completion.</remarks>
    public Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default) =>
        _owner.ReceiveAsync(true, cancellationToken);

    /// <summary>Stops admission and requests cancellation; native completion still controls retirement.</summary>
    public void RequestTerminalWake() => _owner.RequestShutdown();

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

    ~WindowsFidoHidConnection() => _owner?.RequestShutdown();
}