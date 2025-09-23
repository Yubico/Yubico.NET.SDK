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

using System.Security;
using System.Security.Cryptography;
using Yubico.YubiKit.Core.PlatformInterop.Desktop.Cryptography;

namespace Yubico.YubiKit.Core.Core.Cryptography;

/// <summary>
/// An OpenSSL implementation of the ICmacPrimitives interface, exposing CMAC
/// primitives to the SDK.
/// </summary>
internal sealed class CmacPrimitivesOpenSsl : ICmacPrimitives
{
    private bool _disposed;
    private readonly SafeEvpCmacCtx _cmacCtx;

    /// <summary>
    /// Build a new OpenSSL CMAC Primitives object setting the algorithm to
    /// the default: AES-128.
    /// </summary>
    public CmacPrimitivesOpenSsl()
        : this(CmacBlockCipherAlgorithm.Aes128)
    {
    }

    /// <summary>
    /// Build a new OpenSSL CMAC Primitives object that uses the specified
    /// algorithm.
    /// </summary>
    public CmacPrimitivesOpenSsl(CmacBlockCipherAlgorithm algorithm)
    {
        _cmacCtx = PlatformInterop.Desktop.Cryptography.NativeMethods.CmacEvpMacCtxNew();
        _cmacCtx.BlockCipherAlgorithm = algorithm;
    }

    /// <inheritdoc />
    public void CmacInit(ReadOnlySpan<byte> keyData)
    {
        if (keyData.Length != _cmacCtx.BlockCipherAlgorithm.KeyLength())
        {
            throw new ArgumentException("ExceptionMessages.InvalidCmacInput");
        }

        byte[] keyBytes = keyData.ToArray();

        try
        {
            if (PlatformInterop.Desktop.Cryptography.NativeMethods.CmacEvpMacInit(
                    _cmacCtx,
                    (int)_cmacCtx.BlockCipherAlgorithm,
                    keyBytes,
                    keyBytes.Length) == 0)
            {
                throw new SecurityException("ExceptionMessages.CmacFailed");
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(keyBytes);
        }
    }

    /// <inheritdoc />
    public void CmacUpdate(ReadOnlySpan<byte> dataToMac)
    {
        byte[] dataBytes = dataToMac.ToArray();

        try
        {
            if (PlatformInterop.Desktop.Cryptography.NativeMethods.CmacEvpMacUpdate(_cmacCtx, dataBytes, dataBytes.Length) == 0)
            {
                throw new SecurityException("ExceptionMessages.CmacFailed");
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(dataBytes);
        }
    }

    /// <inheritdoc />
    public void CmacFinal(Span<byte> macBuffer)
    {
        if (macBuffer.Length != _cmacCtx.BlockCipherAlgorithm.MacLength())
        {
            throw new ArgumentException("ExceptionMessages.InvalidCmacInput");
        }
        byte[] outputBuffer = new byte[macBuffer.Length];

        if (PlatformInterop.Desktop.Cryptography.NativeMethods.CmacEvpMacFinal(_cmacCtx, outputBuffer, outputBuffer.Length, out int outputLength) == 0)
        {
            throw new SecurityException("ExceptionMessages.CmacFailed");
        }

        if (outputLength != outputBuffer.Length)
        {
            throw new SecurityException("ExceptionMessages.CmacFailed");
        }

        outputBuffer.AsSpan().CopyTo(macBuffer);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _cmacCtx.Dispose();
        _disposed = true;
    }
}