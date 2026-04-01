// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.Cli.Output;
using Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.Cli.Prompts;
using Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.HsmAuthExamples;

namespace Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.Cli.Menus;

public static class CredentialMenu
{
    public static async Task ListAsync(CancellationToken cancellationToken = default)
    {
        OutputHelpers.WriteHeader("List Credentials");

        var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (selection is null) return;

        OutputHelpers.WriteActiveDevice(selection.DisplayName);

        await using var session = await selection.Device.CreateHsmAuthSessionAsync(
            cancellationToken: cancellationToken);
        await ListCredentials.RunAsync(session, cancellationToken);
    }

    public static async Task AddSymmetricAsync(CancellationToken cancellationToken = default)
    {
        OutputHelpers.WriteHeader("Add Symmetric Credential");

        var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (selection is null) return;

        OutputHelpers.WriteActiveDevice(selection.DisplayName);

        await using var session = await selection.Device.CreateHsmAuthSessionAsync(
            cancellationToken: cancellationToken);
        await AddSymmetricCredential.RunAsync(session, cancellationToken);
    }

    public static async Task AddDerivedAsync(CancellationToken cancellationToken = default)
    {
        OutputHelpers.WriteHeader("Add Derived Credential");

        var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (selection is null) return;

        OutputHelpers.WriteActiveDevice(selection.DisplayName);

        await using var session = await selection.Device.CreateHsmAuthSessionAsync(
            cancellationToken: cancellationToken);
        await AddDerivedCredential.RunAsync(session, cancellationToken);
    }

    public static async Task DeleteAsync(CancellationToken cancellationToken = default)
    {
        OutputHelpers.WriteHeader("Delete Credential");

        var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (selection is null) return;

        OutputHelpers.WriteActiveDevice(selection.DisplayName);

        await using var session = await selection.Device.CreateHsmAuthSessionAsync(
            cancellationToken: cancellationToken);
        await DeleteCredential.RunAsync(session, cancellationToken);
    }

    public static async Task GenerateAsync(CancellationToken cancellationToken = default)
    {
        OutputHelpers.WriteHeader("Generate Asymmetric Credential");

        var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (selection is null) return;

        OutputHelpers.WriteActiveDevice(selection.DisplayName);

        await using var session = await selection.Device.CreateHsmAuthSessionAsync(
            cancellationToken: cancellationToken);
        await GenerateAsymmetricCredential.RunAsync(session, cancellationToken);
    }
}
