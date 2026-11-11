// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console.Cli;
using Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Commands;

var app = new CommandApp();

app.Configure(config =>
{
    config.SetApplicationName("openpgp");
    config.SetApplicationVersion("1.0.0-preview");

    config.AddCommand<InfoCommand>("info")
        .WithDescription("Display general status of the OpenPGP application.");

    config.AddCommand<ResetCommand>("reset")
        .WithDescription("Reset all OpenPGP data.");

    config.AddBranch("access", access =>
    {
        access.SetDescription("Manage PIN, Admin PIN, and Reset Code.");

        access.AddCommand<AccessSetRetriesCommand>("set-retries")
            .WithDescription("Set PIN retry counts.");

        access.AddCommand<AccessChangePinCommand>("change-pin")
            .WithDescription("Change the User PIN.");

        access.AddCommand<AccessChangeAdminPinCommand>("change-admin-pin")
            .WithDescription("Change the Admin PIN.");

        access.AddCommand<AccessSetResetCodeCommand>("set-reset-code")
            .WithDescription("Set or change the Reset Code.");

        access.AddCommand<AccessUnblockPinCommand>("unblock-pin")
            .WithDescription("Unblock the User PIN using a Reset Code.");
    });

    config.AddBranch("keys", keys =>
    {
        keys.SetDescription("Manage OpenPGP keys.");

        keys.AddCommand<KeysSetTouchCommand>("set-touch")
            .WithDescription("Set touch policy for a key slot.");

        keys.AddCommand<KeysImportCommand>("import")
            .WithDescription("Import a private key from a PEM file.");

        keys.AddCommand<KeysGenerateCommand>("generate")
            .WithDescription("Generate a new key on the YubiKey.");

        keys.AddCommand<KeysAttestCommand>("attest")
            .WithDescription("Generate an attestation certificate for a key.");
    });

    config.AddBranch("certificates", certs =>
    {
        certs.SetDescription("Manage OpenPGP certificates.");

        certs.AddCommand<CertificatesExportCommand>("export")
            .WithDescription("Export a certificate.");

        certs.AddCommand<CertificatesImportCommand>("import")
            .WithDescription("Import a certificate.");

        certs.AddCommand<CertificatesDeleteCommand>("delete")
            .WithDescription("Delete a certificate.");
    });
});

return app.Run(args);