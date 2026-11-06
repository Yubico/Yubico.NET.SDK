// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

namespace Yubico.YubiKit.Cli.Shared.Device;

/// <summary>How to handle multiple eligible devices without an interactive terminal.</summary>
public enum NonInteractiveSelection
{
    /// <summary>Return no selection; the caller requires an explicit device choice.</summary>
    RequireChoice,

    /// <summary>Select the first eligible device in discovery order.</summary>
    FirstEligible,

    /// <summary>Prefer a device exposing the first configured transport, otherwise the first eligible device.</summary>
    PreferFirstTransport
}