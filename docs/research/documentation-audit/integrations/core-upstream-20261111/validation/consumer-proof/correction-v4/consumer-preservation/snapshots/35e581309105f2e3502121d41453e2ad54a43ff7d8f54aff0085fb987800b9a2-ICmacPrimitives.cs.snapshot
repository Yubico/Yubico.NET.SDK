// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0

namespace Yubico.YubiKit.Core.Cryptography;

/// <summary>
/// Provides replaceable cipher-based message authentication code operations.
/// </summary>
internal interface ICmacPrimitives : IDisposable
{
    /// <summary>Initializes a CMAC operation with <paramref name="keyData"/>.</summary>
    void CmacInit(ReadOnlySpan<byte> keyData);

    /// <summary>Appends data to the current CMAC operation.</summary>
    void CmacUpdate(ReadOnlySpan<byte> dataToMac);

    /// <summary>Completes the current CMAC operation.</summary>
    void CmacFinal(Span<byte> macBuffer);
}