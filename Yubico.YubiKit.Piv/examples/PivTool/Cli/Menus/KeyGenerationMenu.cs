// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Yubico.YubiKit.Piv.Examples.PivTool.Cli.Output;
using Yubico.YubiKit.Piv.Examples.PivTool.Cli.Prompts;
using Yubico.YubiKit.Piv.Examples.PivTool.PivExamples;

namespace Yubico.YubiKit.Piv.Examples.PivTool.Cli.Menus;

/// <summary>
/// CLI menu for key generation operations.
/// </summary>
public static class KeyGenerationMenu
{
    public static async Task RunAsync(CancellationToken cancellationToken = default)
    {
        OutputHelpers.WriteHeader("Key Generation");

        var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (selection is null)
        {
            return;
        }

        OutputHelpers.WriteActiveDevice(selection.DisplayName);

        // Select slot
        var slot = SlotSelector.SelectSlot("Select slot for key generation:");

        // Select algorithm
        var algorithm = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select algorithm:")
                .AddChoices(
                [
                    "RSA 2048",
                    "RSA 3072",
                    "RSA 4096",
                    "ECC P-256",
                    "ECC P-384"
                ]));

        var pivAlgorithm = algorithm switch
        {
            "RSA 2048" => PivAlgorithm.Rsa2048,
            "RSA 3072" => PivAlgorithm.Rsa3072,
            "RSA 4096" => PivAlgorithm.Rsa4096,
            "ECC P-256" => PivAlgorithm.EccP256,
            "ECC P-384" => PivAlgorithm.EccP384,
            _ => PivAlgorithm.EccP256
        };

        // Select PIN policy
        var pinPolicy = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select PIN policy:")
                .AddChoices(["Default", "Never", "Once", "Always"])) switch
        {
            "Never" => PivPinPolicy.Never,
            "Once" => PivPinPolicy.Once,
            "Always" => PivPinPolicy.Always,
            _ => PivPinPolicy.Default
        };

        // Select touch policy
        var touchPolicy = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select touch policy:")
                .AddChoices(["Default", "Never", "Always", "Cached"])) switch
        {
            "Never" => PivTouchPolicy.Never,
            "Always" => PivTouchPolicy.Always,
            "Cached" => PivTouchPolicy.Cached,
            _ => PivTouchPolicy.Default
        };

        // Authenticate management key
        await using var session = await selection.Device.CreatePivSessionAsync(cancellationToken: cancellationToken);
        OutputHelpers.SetupTouchNotification(session);
        using var mgmtKey = PinPrompt.GetManagementKeyWithDefault("Management key");
        
        if (mgmtKey is null)
        {
            return;
        }

        var authResult = await PinManagement.AuthenticateAsync(session, mgmtKey.Memory.Span.ToArray(), cancellationToken);
        if (!authResult.Success)
        {
            OutputHelpers.WriteError(authResult.ErrorMessage ?? "Failed to authenticate");
            return;
        }

        // Generate key
        await AnsiConsole.Status()
            .StartAsync("Generating key...", async ctx =>
            {
                var result = await KeyGeneration.GenerateKeyAsync(
                    session, slot, pivAlgorithm, pinPolicy, touchPolicy, cancellationToken);

                if (result.Success)
                {
                    OutputHelpers.WriteSuccess($"Key generated in slot {result.Slot}");
                    OutputHelpers.WriteKeyValue("Algorithm", result.Algorithm.ToString());
                    OutputHelpers.WriteKeyValue("Public Key Size", $"{result.PublicKey.Length} bytes");
                }
                else
                {
                    OutputHelpers.WriteError(result.ErrorMessage ?? "Key generation failed");
                }
            });
    }
}
