// Copyright 2025 Yubico AB
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

namespace Yubico.YubiKit.Core.Sessions;

/// <summary>
/// Provides application identifiers used to select YubiKey applets.
/// </summary>
/// <remarks>
/// Each read returns a fresh copy. Returning one shared buffer would be cheaper, but
/// <see cref="System.Runtime.InteropServices.MemoryMarshal.TryGetArray{T}" /> recovers the array behind a
/// <see cref="ReadOnlyMemory{T}" />, so a shared buffer could be written through from anywhere in the
/// process and would corrupt applet selection for every session afterwards. These are read once per
/// session open, immediately before device input and output, so the copy is not worth avoiding.
/// </remarks>
public static class ApplicationIds
{
    private static readonly byte[] _management = [0xA0, 0x00, 0x00, 0x05, 0x27, 0x47, 0x11, 0x17];
    private static readonly byte[] _otp = [0xA0, 0x00, 0x00, 0x05, 0x27, 0x20, 0x01, 0x01];
    private static readonly byte[] _fidoU2f = [0xA0, 0x00, 0x00, 0x06, 0x47, 0x2F, 0x00, 0x01];
    private static readonly byte[] _fido2 = [0xA0, 0x00, 0x00, 0x06, 0x47, 0x2F, 0x00, 0x01];
    private static readonly byte[] _oath = [0xA0, 0x00, 0x00, 0x05, 0x27, 0x21, 0x01];
    private static readonly byte[] _openPgp = [0xD2, 0x76, 0x00, 0x01, 0x24, 0x01];
    private static readonly byte[] _piv = [0xA0, 0x00, 0x00, 0x03, 0x08, 0x00, 0x00, 0x10, 0x00];
    private static readonly byte[] _yubiHsmAuth = [0xA0, 0x00, 0x00, 0x05, 0x27, 0x21, 0x07, 0x01];
    private static readonly byte[] _securityDomain = [0xA0, 0x00, 0x00, 0x01, 0x51, 0x00, 0x00, 0x00];

    /// <summary>Gets the application identifier that selects the YubiKey Management applet.</summary>
    public static ReadOnlyMemory<byte> Management => _management.ToArray();

    /// <summary>Gets the application identifier that selects the YubiKey OTP applet.</summary>
    public static ReadOnlyMemory<byte> Otp => _otp.ToArray();

    /// <summary>
    /// Gets the application identifier that selects the FIDO U2F applet. This identifier is provided for
    /// U2F-era interoperability and is not used by the SDK's own sessions.
    /// </summary>
    public static ReadOnlyMemory<byte> FidoU2f => _fidoU2f.ToArray();

    /// <summary>Gets the application identifier that selects the FIDO2 applet.</summary>
    public static ReadOnlyMemory<byte> Fido2 => _fido2.ToArray();

    /// <summary>Gets the application identifier that selects the OATH applet.</summary>
    public static ReadOnlyMemory<byte> Oath => _oath.ToArray();

    /// <summary>Gets the application identifier that selects the OpenPGP card applet.</summary>
    public static ReadOnlyMemory<byte> OpenPgp => _openPgp.ToArray();

    /// <summary>Gets the application identifier that selects the PIV applet.</summary>
    public static ReadOnlyMemory<byte> Piv => _piv.ToArray();

    /// <summary>Gets the application identifier that selects the YubiHSM Auth applet.</summary>
    public static ReadOnlyMemory<byte> YubiHsmAuth => _yubiHsmAuth.ToArray();

    /// <summary>Gets the application identifier that selects the Security Domain applet.</summary>
    public static ReadOnlyMemory<byte> SecurityDomain => _securityDomain.ToArray();
}
