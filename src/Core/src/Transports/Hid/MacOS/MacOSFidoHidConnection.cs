// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;

namespace Yubico.YubiKit.Core.Transports.Hid.MacOS;

internal sealed class MacOSFidoHidConnection : IFidoHidConnection, ITerminalWakeControl
{
    private readonly MacOSFidoHidNativeOwner _owner;
    private MacOSFidoHidConnection(MacOSFidoHidNativeOwner owner) => _owner = owner;
    public int PacketSize => MacOSFidoHidNativeOwner.FidoPacketSize;
    internal int InputReportSize => _owner.InputReportSize;
    internal int OutputReportSize => _owner.OutputReportSize;
    public ConnectionType Type => ConnectionType.HidFido;

    internal static async Task<MacOSFidoHidConnection> OpenAsync(long entryId, IMacOSHidInputBridge inputBridge,
        CancellationToken cancellationToken, IDisposable? physicalClaim = null, Action? beforeSendPublication = null)
    {
        MacOSFidoHidNativeOwner owner;
        try
        {
            owner = new MacOSFidoHidNativeOwner(entryId, inputBridge, physicalClaim, beforeSendPublication);
        }
        catch
        {
            physicalClaim?.Dispose();
            throw;
        }
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await owner.OpenAsync(cancellationToken).ConfigureAwait(false);
            return new MacOSFidoHidConnection(owner);
        }
        catch
        {
            await owner.ShutdownAsync().ConfigureAwait(false);
            throw;
        }
    }

    public Task SendAsync(ReadOnlyMemory<byte> packet, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (packet.Length != MacOSFidoHidNativeOwner.FidoPacketSize)
        {
            throw new ArgumentException("FIDO packet must be exactly 64 bytes", nameof(packet));
        }
        return _owner.SendAsync(packet, cancellationToken);
    }
    internal Task SendReportAsync(ReadOnlyMemory<byte> report) => _owner.SendAsync(report, CancellationToken.None);
    public Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default) =>
        _owner.ReceiveAsync(cancellationToken);
    public void RequestTerminalWake() => _owner.Terminal(MacOSFidoHidNativeOwner.ShutdownTerminalReason);
    public void Dispose()
    {
        if (_owner.IsWorkerThread)
        {
            throw new InvalidOperationException("Cannot drain the native worker from itself.");
        }
        _owner.ShutdownAsync().GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }
    public async ValueTask DisposeAsync()
    {
        await _owner.ShutdownAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }
    ~MacOSFidoHidConnection() => _ = _owner.ShutdownAsync();
}