// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0

using System.Security.Cryptography;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;

namespace Yubico.YubiKit.Core.Cryptography;

/// <summary>
/// Creates the default .NET CMAC primitive implementation.
/// </summary>
internal static class CmacPrimitives
{
    /// <summary>
    /// Creates a CMAC primitive for <paramref name="algorithm"/>.
    /// </summary>
    internal static ICmacPrimitives Create(CmacBlockCipherAlgorithm algorithm) =>
        algorithm == CmacBlockCipherAlgorithm.Aes128
            ? new AesCmacPrimitives()
            : throw new NotSupportedException("SCP only supports AES-128 CMAC.");

    private sealed class AesCmacPrimitives : ICmacPrimitives
    {
        private AesCmac? _cmac;
        private bool _disposed;

        public void CmacInit(ReadOnlySpan<byte> keyData)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (keyData.Length != 16)
            {
                throw new ArgumentException("Key must be 16 bytes.", nameof(keyData));
            }

            _cmac?.Dispose();
            _cmac = new AesCmac(keyData);
        }

        public void CmacUpdate(ReadOnlySpan<byte> dataToMac)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            (_cmac ?? throw new InvalidOperationException("CMAC has not been initialized.")).AppendData(dataToMac);
        }

        public void CmacFinal(Span<byte> macBuffer)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (macBuffer.Length != 16)
            {
                throw new ArgumentException("The CMAC output buffer must be 16 bytes.", nameof(macBuffer));
            }

            byte[] mac = (_cmac ?? throw new InvalidOperationException("CMAC has not been initialized."))
                .GetHashAndReset();
            try
            {
                mac.CopyTo(macBuffer);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(mac);
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _cmac?.Dispose();
            _disposed = true;
        }
    }
}