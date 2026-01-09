// Copyright 2025 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Core.Hid;

/// <summary>
/// Protocol interface for FIDO HID communication using CTAP HID framing.
/// Supports both FIDO2/U2F operations (CTAPHID_MSG) and YubiKey Management vendor commands.
/// </summary>
public interface IFidoProtocol : IProtocol
{
    /// <summary>
    /// Transmits an APDU command over HID and receives the response.
    /// Used for FIDO2/U2F applications that use CTAPHID_MSG.
    /// </summary>
    /// <param name="command">The APDU command to transmit.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response data from the YubiKey.</returns>
    Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
        ApduCommand command,
        CancellationToken cancellationToken = default);
    
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
    /// Selects an application on the YubiKey. For HID, this returns version info.
    /// </summary>
    /// <param name="applicationId">The application ID (unused for HID).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Application version information.</returns>
    Task<ReadOnlyMemory<byte>> SelectAsync(
        ReadOnlyMemory<byte> applicationId,
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
