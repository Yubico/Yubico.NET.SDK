// Copyright 2026 Yubico AB
// SPDX-License-Identifier: Apache-2.0

namespace Yubico.YubiKit.Piv;

/// <summary>Specifies whether management-key rotation stores a recovery copy on the card.</summary>
public enum ManagementKeyStorage
{
    /// <summary>Do not store a PIN-protected copy. Remove the old copy if ADMIN DATA records one.</summary>
    NotStored,

    /// <summary>Store a copy in the PIN-gated PRINTED object.</summary>
    PinProtected,
}