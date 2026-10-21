// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Yubico.YubiKit.Core.Credentials;

namespace Yubico.YubiKit.YubiHsm;

/// <summary>Identifies the credential targeted by a YubiHSM Auth credential request.</summary>
public sealed record HsmAuthCredentialPromptContext : CredentialPromptContext
{
    /// <summary>Gets the exact target label. Use Kind to distinguish its password from the applet-wide management key.</summary>
    public required string CredentialLabel { get; init; }
}