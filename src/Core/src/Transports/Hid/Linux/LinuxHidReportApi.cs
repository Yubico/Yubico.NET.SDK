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

using System.Buffers;
using System.Buffers.Binary;
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Native;
using Yubico.YubiKit.Core.Native.Linux.Libc;
using NativeMethods = Yubico.YubiKit.Core.Native.Linux.Libc.NativeMethods;

namespace Yubico.YubiKit.Core.Transports.Hid.Linux;

/// <summary>Adapts synchronous hidraw syscalls to borrowed unnumbered payloads with checked native framing/counts.</summary>
/// <param name="native">Borrowed syscall seam; calls retain handles only for their synchronous native use.</param>
/// <remarks>
/// Does not schedule work, own a connection lifetime or enforce typed FIDO/OTP send lengths.
/// Native framing is scoped and zeroed on every exit; caller storage remains caller-owned.
/// See <see cref="ILinuxHidReportApi"/> for blocking, sizing and checked-release contracts.
/// </remarks>
internal sealed class LinuxHidReportApi(ILinuxHidNativeApi native) : ILinuxHidReportApi
{
    private const int MaxNativeReportSize = 16384; // Linux HID_MAX_BUFFER_SIZE.
    private const int MaxNativeFeatureSize = (1 << 14) - 1; // Generic ioctl size field.

    /// <summary>Gets the stateless production report adapter; connection owners retain their own descriptors and claims.</summary>
    internal static ILinuxHidReportApi Instance { get; } = new LinuxHidReportApi(new LinuxHidNativeApi());

    /// <inheritdoc />
    public LinuxFileSafeHandle Open(string path, NativeMethods.OpenFlags flags)
    {
        var handle = native.Open(path, flags | NativeMethods.OpenFlags.O_CLOEXEC);
        if (!handle.IsInvalid)
            return handle;
        int error = System.Runtime.InteropServices.Marshal.GetLastPInvokeError();
        handle.Dispose();
        throw new PlatformApiException("open", error, "Failed to open Linux HID report descriptor.");
    }

    /// <inheritdoc />
    public (int InputSize, int OutputSize) GetReportSizes(LinuxFileSafeHandle handle)
    {
        Span<byte> sizeBuffer = stackalloc byte[4];
        sizeBuffer.Clear();
        var sizeResult = native.Ioctl(handle, (nuint)NativeMethods.HIDIOCGRDESCSIZE, sizeBuffer);
        // Preserve the existing standard FIDO fallback when descriptor queries are unavailable.
        if (sizeResult.Count < 0)
            return (64, 64);
        int size = BinaryPrimitives.ReadInt32LittleEndian(sizeBuffer);
        // hidraw_ioctl rejects len > HID_MAX_DESCRIPTOR_SIZE - 1 (4095).
        if (size is <= 0 or >= 4096)
            throw new InvalidDataException("Invalid Linux HID descriptor size.");

        byte[] rented = ArrayPool<byte>.Shared.Rent(NativeMethods.DescriptorSize);
        try
        {
            Span<byte> descriptor = rented.AsSpan(0, NativeMethods.DescriptorSize);
            descriptor.Clear();
            BinaryPrimitives.WriteInt32LittleEndian(descriptor, size);
            var result = native.Ioctl(handle, (nuint)NativeMethods.HIDIOCGRDESC, descriptor);
            if (result.Count < 0)
                return (64, 64);
            int actualSize = BinaryPrimitives.ReadInt32LittleEndian(descriptor);
            return actualSize <= 0 || actualSize > size
                ? throw new InvalidDataException("Invalid returned Linux HID descriptor size.")
                : LinuxHidReportSizes.Parse(descriptor.Slice(4, actualSize));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    /// <inheritdoc />
    public LinuxReadResult TryRead(LinuxFileSafeHandle handle, Span<byte> payload)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(payload.Length, MaxNativeReportSize);
        var result = native.Read(handle, payload);
        if (result.Count > payload.Length || result.Count < -1)
            throw new PlatformApiException("read", 0, "Invalid native Linux HID read count.");
        var disposition = result.Count switch
        {
            > 0 => LinuxReadDisposition.Data,
            0 => LinuxReadDisposition.EndOfStream,
            _ => result.Error switch
            {
                NativeMethods.EAGAIN => LinuxReadDisposition.WouldBlock,
                NativeMethods.EINTR => LinuxReadDisposition.Interrupted,
                _ => LinuxReadDisposition.Error
            }
        };
        return new LinuxReadResult(disposition, result.Count > 0 ? (int)result.Count : 0, result.Error);
    }

    /// <inheritdoc />
    public void Write(LinuxFileSafeHandle handle, ReadOnlySpan<byte> payload) =>
        SendFramedReport(handle, payload, feature: false);

    /// <inheritdoc />
    public void GetFeature(LinuxFileSafeHandle handle, Span<byte> payload)
    {
        RequireSize(payload.Length, 8);
        Span<byte> framed = stackalloc byte[9];
        framed.Clear();
        try
        {
            // USB usbhid_get_raw_report preserves synthetic report ID zero at offset 0;
            // its positive count includes that byte. Do not copy padded/short responses.
            RequireComplete(native.Ioctl(handle, 0xC0094807, framed), 9, "ioctl(HIDIOCGFEATURE)");
            if (framed[0] != 0)
                throw new InvalidDataException("Unexpected numbered OTP feature report.");
            framed[1..].CopyTo(payload);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(framed);
        }
    }

    /// <inheritdoc />
    public void SetFeature(LinuxFileSafeHandle handle, ReadOnlySpan<byte> payload) =>
        SendFramedReport(handle, payload, feature: true);

    private void SendFramedReport(LinuxFileSafeHandle handle, ReadOnlySpan<byte> payload, bool feature)
    {
        // Direct synchronous facades historically forward the caller's payload length.
        // Typed FIDO/OTP connections enforce 64/8 separately; this seam supplies only bounded
        // native framing. Feature length must also fit the ioctl's 14-bit size field.
        int maxNativeSize = feature ? MaxNativeFeatureSize : MaxNativeReportSize;
        ArgumentOutOfRangeException.ThrowIfGreaterThan(payload.Length, maxNativeSize - 1);
        int framedLength = payload.Length + 1;
        byte[]? rented = null;
        Span<byte> framed = framedLength <= 512
            ? stackalloc byte[framedLength]
            : (rented = ArrayPool<byte>.Shared.Rent(framedLength)).AsSpan(0, framedLength);
        try
        {
            framed[0] = 0;
            payload.CopyTo(framed[1..]);
            if (feature)
            {
                nuint request = (nuint)NativeMethods.HIDIOCSFEATURE | ((nuint)framedLength << 16);
                RequireComplete(native.Ioctl(handle, request, framed), framedLength, "ioctl(HIDIOCSFEATURE)");
            }
            else
            {
                RequireComplete(native.Write(handle, framed), framedLength, "write");
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(framed);
            if (rented is not null)
                ArrayPool<byte>.Shared.Return(rented);
        }
    }

    /// <inheritdoc />
    public LinuxReleaseOutcome CloseOnce(LinuxFileSafeHandle handle) => handle.CloseOnce();

    private static void RequireSize(int actual, int expected)
    {
        if (actual != expected)
            throw new ArgumentException($"Expected a {expected}-byte report payload.");
    }

    private static void RequireComplete(LinuxIoResult result, int expected, string operation)
    {
        if (result.Count != expected)
            throw new PlatformApiException(operation, result.Count < 0 ? result.Error : 0,
                $"Linux HID report transfer incomplete: expected {expected} native bytes, got {result.Count}.");
    }
}