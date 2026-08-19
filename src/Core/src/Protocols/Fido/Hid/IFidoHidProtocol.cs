// Copyright 2025 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;

namespace Yubico.YubiKit.Core.Protocols.Fido.Hid;

/// <summary>
/// Protocol interface for FIDO HID communication using CTAP HID framing.
/// Supports CTAP HID channel initialization and YubiKey Management vendor commands.
/// </summary>
/// <remarks>
///     Implementations admit one full logical exchange at a time. An overlapping operation throws
///     <see cref="InvalidOperationException" /> immediately. A token is checked before entry; an exchange already
///     in flight runs to completion to avoid stranding the device mid-transaction.
/// </remarks>
public interface IFidoHidProtocol : IProtocol
{
    /// <summary>
    /// Initializes the CTAP HID channel if it has not already been initialized.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a CTAP vendor command and receives the response.
    /// Used for Management application over HID.
    /// </summary>
    /// <param name="command">The CTAP command byte (e.g., 0xC2 for READ_CONFIG).</param>
    /// <param name="data">The command payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response data from the YubiKey.</returns>
    Task<ReadOnlyMemory<byte>> SendVendorCommandAsync(
        byte command,
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets whether the HID channel has been initialized.
    /// </summary>
    bool IsChannelInitialized { get; }

    /// <summary>
    /// Gets the firmware version reported during channel initialization.
    /// </summary>
    FirmwareVersion? FirmwareVersion { get; }
}