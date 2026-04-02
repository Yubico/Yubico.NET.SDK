// Copyright 2025 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

namespace Yubico.YubiKit.Core.Hid.Constants;

/// <summary>
/// CTAP HID protocol constants as defined in the FIDO CTAP specification.
/// </summary>
internal static class CtapConstants
{
    // CTAP HID Commands
    public const byte CtapHidMsg = 0x03;        // CTAP1/U2F raw message
    public const byte CtapHidCbor = 0x10;       // CTAP2 CBOR encoded message
    public const byte CtapHidInit = 0x06;       // Initialize channel
    public const byte CtapHidPing = 0x01;       // Echo data through local processing
    public const byte CtapHidCancel = 0x11;     // Cancel outstanding request
    public const byte CtapHidError = 0x3F;      // Error response
    public const byte CtapHidKeepAlive = 0x3B;  // Processing status notification

    // YubiKey Management Vendor Commands (CTAP_TYPE_INIT | CTAP_VENDOR_FIRST + offset)
    public const byte CtapVendorFirst = 0x40;
    public const byte CtapYubikeyDeviceConfig = 0xC0;  // 0x80 | 0x40
    public const byte CtapReadConfig = 0xC2;           // 0x80 | 0x42
    public const byte CtapWriteConfig = 0xC3;          // 0x80 | 0x43

    // Packet Structure
    public const int PacketSize = 64;
    public const int MaxPayloadSize = 7609;     // 64 - 7 + 128 * (64 - 5)
    
    public const int InitHeaderSize = 7;
    public const int InitDataSize = PacketSize - InitHeaderSize;  // 57 bytes
    
    public const int ContinuationHeaderSize = 5;
    public const int ContinuationDataSize = PacketSize - ContinuationHeaderSize;  // 59 bytes

    // Channel Management
    public const uint BroadcastChannelId = 0xFFFFFFFF;
    public const int NonceSize = 8;
    
    // Bit masks
    public const byte InitPacketMask = 0x80;    // Bit 7 set for init packets
}
