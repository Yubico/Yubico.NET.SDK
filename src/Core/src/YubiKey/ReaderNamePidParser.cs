// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Yubico.YubiKit.Core.YubiKey;

/// <summary>
///     Derives a YubiKey USB Product ID from a PC/SC reader name, and identifies known Yubico PIDs.
/// </summary>
/// <remarks>
///     Composite discovery correlates the interfaces of one physical USB YubiKey by USB Product ID. The CCID
///     interface exposes no PID directly, but a USB YubiKey's PC/SC reader name encodes its enabled USB
///     interfaces (e.g. "Yubico YubiKey OTP+FIDO+CCID 00 00"), which maps deterministically to a PID. This
///     mirrors the Rust reference (<c>pid_from_reader_name</c> / <c>pid_from_interfaces</c>). HID interfaces
///     use their real descriptor <c>ProductId</c> instead; this parser is for the CCID reader name only.
/// </remarks>
internal static class ReaderNamePidParser
{
    // Marker that identifies a USB-connected Yubico reader (case-insensitive), per the Rust reference.
    private const string YubicoUsbReaderMarker = "yubico yubikey";

    /// <summary>
    ///     The Security Key (SKY) USB Product ID. SKY is FIDO-HID-only and has no CCID reader; its PID is read
    ///     from the HID descriptor, not parsed from a reader name.
    /// </summary>
    public const ushort SkyPid = 0x0120;

    /// <summary>All USB Product IDs this parser can produce, plus SKY. Used to gate mergeability.</summary>
    private static readonly HashSet<ushort> KnownPids =
    [
        // YubiKey NEO
        0x0110, 0x0111, 0x0112, 0x0113, 0x0114, 0x0115, 0x0116,
        // Security Key
        SkyPid,
        // YubiKey 4/5 and later
        0x0401, 0x0402, 0x0403, 0x0404, 0x0405, 0x0406, 0x0407
    ];

    /// <summary>Whether <paramref name="pid"/> is a known Yubico USB Product ID usable as a merge key.</summary>
    public static bool IsKnownPid(ushort pid) => KnownPids.Contains(pid);

    /// <summary>Whether <paramref name="pid"/> is the Security Key (SKY) Product ID.</summary>
    public static bool IsSky(ushort pid) => pid == SkyPid;

    /// <summary>
    ///     Parses the USB Product ID from a USB YubiKey PC/SC reader name, or returns <c>null</c> when the name
    ///     is not a recognizable USB Yubico reader or the interface combination is not a valid YubiKey PID.
    /// </summary>
    public static ushort? FromReaderName(string? readerName)
    {
        if (string.IsNullOrEmpty(readerName))
            return null;

        var lower = readerName.ToLowerInvariant();
        if (!lower.Contains(YubicoUsbReaderMarker))
            return null;

        var otp = lower.Contains("otp");
        var fido = lower.Contains("fido") || lower.Contains("u2f");
        var ccid = lower.Contains("ccid");
        var isNeo = lower.Contains("neo");

        var pid = FromInterfaces(otp, fido, ccid, isNeo);
        return pid is { } value && IsKnownPid(value) ? value : null;
    }

    private static ushort? FromInterfaces(bool otp, bool fido, bool ccid, bool isNeo) => (isNeo, otp, fido, ccid) switch
    {
        (true, true, false, false) => 0x0110,
        (true, true, false, true) => 0x0111,
        (true, false, false, true) => 0x0112,
        (true, false, true, false) => 0x0113,
        (true, true, true, false) => 0x0114,
        (true, false, true, true) => 0x0115,
        (true, true, true, true) => 0x0116,
        (false, true, false, false) => 0x0401,
        (false, false, true, false) => 0x0402,
        (false, true, true, false) => 0x0403,
        (false, false, false, true) => 0x0404,
        (false, true, false, true) => 0x0405,
        (false, false, true, true) => 0x0406,
        (false, true, true, true) => 0x0407,
        _ => null
    };
}