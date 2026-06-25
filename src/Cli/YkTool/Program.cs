// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console.Cli;
using Yubico.YubiKit.Cli.Commands.Management;
using Yubico.YubiKit.Cli.Commands.Oath;
using Yubico.YubiKit.Cli.Commands.OpenPgp;
using Yubico.YubiKit.Cli.Commands.HsmAuth;
using Yubico.YubiKit.Cli.Commands.Otp;
using Yubico.YubiKit.Cli.Commands.Piv;
using Yubico.YubiKit.Cli.Commands.Fido;
using Yubico.YubiKit.Cli.Commands.Infrastructure;

var app = new CommandApp();

app.Configure(config =>
{
    config.SetApplicationName("yk");
    config.SetApplicationVersion("1.0.0-preview");
    config.SetInterceptor(new YkCommandInterceptor());

    // ── Management ───────────────────────────────────────────────────────────
    config.AddBranch("management", management =>
    {
        management.SetDescription("YubiKey device configuration and information.");

        management.AddCommand<ManagementInfoCommand>("info")
            .WithDescription("Display device information: firmware, serial, capabilities.");

        management.AddCommand<ManagementConfigCommand>("config")
            .WithDescription("Display current device configuration: capabilities, timeouts, flags.");

        management.AddCommand<ManagementResetCommand>("reset")
            .WithDescription("Factory reset the YubiKey (requires firmware 5.6.0+).");
    });

    // ── FIDO2 ────────────────────────────────────────────────────────────────
    config.AddBranch("fido", fido =>
    {
        fido.SetDescription("FIDO2/WebAuthn credential and PIN management.");

        fido.AddCommand<FidoInfoCommand>("info")
            .WithDescription("Display FIDO2 application status.");

        fido.AddCommand<FidoResetCommand>("reset")
            .WithDescription("Reset all FIDO2 data.");

        fido.AddBranch("access", access =>
        {
            access.SetDescription("Manage FIDO2 PIN.");

            access.AddCommand<FidoAccessSetPinCommand>("set-pin")
                .WithDescription("Set the initial PIN.");

            access.AddCommand<FidoAccessChangePinCommand>("change-pin")
                .WithDescription("Change the PIN.");

            access.AddCommand<FidoAccessVerifyPinCommand>("verify-pin")
                .WithDescription("Verify the PIN.");
        });

        fido.AddBranch("config", fidoConfig =>
        {
            fidoConfig.SetDescription("Configure authenticator settings.");

            fidoConfig.AddCommand<FidoConfigToggleAlwaysUvCommand>("toggle-always-uv")
                .WithDescription("Toggle always-UV setting.");

            fidoConfig.AddCommand<FidoConfigEnableEpAttestationCommand>("enable-ep-attestation")
                .WithDescription("Enable enterprise attestation.");
        });

        fido.AddBranch("credentials", credentials =>
        {
            credentials.SetDescription("Manage discoverable credentials.");

            credentials.AddCommand<FidoCredentialsListCommand>("list")
                .WithDescription("List stored credentials.");

            credentials.AddCommand<FidoCredentialsDeleteCommand>("delete")
                .WithDescription("Delete a credential.");
        });

        fido.AddBranch("fingerprints", fingerprints =>
        {
            fingerprints.SetDescription("Manage fingerprint enrollments.");

            fingerprints.AddCommand<FidoFingerprintsListCommand>("list")
                .WithDescription("List enrolled fingerprints.");

            fingerprints.AddCommand<FidoFingerprintsAddCommand>("add")
                .WithDescription("Enroll a new fingerprint.");

            fingerprints.AddCommand<FidoFingerprintsDeleteCommand>("delete")
                .WithDescription("Delete a fingerprint enrollment.");

            fingerprints.AddCommand<FidoFingerprintsRenameCommand>("rename")
                .WithDescription("Rename a fingerprint enrollment.");
        });
    });

    // ── OATH ─────────────────────────────────────────────────────────────────
    config.AddBranch("oath", oath =>
    {
        oath.SetDescription("TOTP/HOTP one-time password credential management.");

        oath.AddCommand<OathInfoCommand>("info")
            .WithDescription("Display OATH application status.");

        oath.AddCommand<OathResetCommand>("reset")
            .WithDescription("Reset all OATH data.");

        oath.AddBranch("access", access =>
        {
            access.SetDescription("Manage OATH password.");

            access.AddCommand<OathAccessChangePasswordCommand>("change-password")
                .WithDescription("Change or remove the OATH password.");
        });

        oath.AddBranch("accounts", accounts =>
        {
            accounts.SetDescription("Manage OATH accounts.");

            accounts.AddCommand<OathAccountsListCommand>("list")
                .WithDescription("List all stored accounts.");

            accounts.AddCommand<OathAccountsAddCommand>("add")
                .WithDescription("Add a new OATH account.");

            accounts.AddCommand<OathAccountsCodeCommand>("code")
                .WithDescription("Generate OTP codes.");

            accounts.AddCommand<OathAccountsDeleteCommand>("delete")
                .WithDescription("Remove an account.");

            accounts.AddCommand<OathAccountsRenameCommand>("rename")
                .WithDescription("Rename an account.");

            accounts.AddCommand<OathAccountsUriCommand>("uri")
                .WithDescription("Add account from otpauth:// URI.");
        });
    });

    // ── OpenPGP ──────────────────────────────────────────────────────────────
    config.AddBranch("openpgp", openpgp =>
    {
        openpgp.SetDescription("OpenPGP card key and certificate management.");

        openpgp.AddCommand<OpenPgpInfoCommand>("info")
            .WithDescription("Display general status of the OpenPGP application.");

        openpgp.AddCommand<OpenPgpResetCommand>("reset")
            .WithDescription("Reset all OpenPGP data.");

        openpgp.AddBranch("access", access =>
        {
            access.SetDescription("Manage PIN, Admin PIN, and Reset Code.");

            access.AddCommand<OpenPgpAccessSetRetriesCommand>("set-retries")
                .WithDescription("Set PIN retry counts.");

            access.AddCommand<OpenPgpAccessChangePinCommand>("change-pin")
                .WithDescription("Change the User PIN.");

            access.AddCommand<OpenPgpAccessChangeAdminPinCommand>("change-admin-pin")
                .WithDescription("Change the Admin PIN.");

            access.AddCommand<OpenPgpAccessSetResetCodeCommand>("set-reset-code")
                .WithDescription("Set or change the Reset Code.");

            access.AddCommand<OpenPgpAccessUnblockPinCommand>("unblock-pin")
                .WithDescription("Unblock the User PIN using a Reset Code.");
        });

        openpgp.AddBranch("keys", keys =>
        {
            keys.SetDescription("Manage OpenPGP keys.");

            keys.AddCommand<OpenPgpKeysSetTouchCommand>("set-touch")
                .WithDescription("Set touch policy for a key slot.");

            keys.AddCommand<OpenPgpKeysImportCommand>("import")
                .WithDescription("Import a private key from a PEM file.");

            keys.AddCommand<OpenPgpKeysGenerateCommand>("generate")
                .WithDescription("Generate a new key on the YubiKey.");

            keys.AddCommand<OpenPgpKeysAttestCommand>("attest")
                .WithDescription("Generate an attestation certificate for a key.");
        });

        openpgp.AddBranch("certificates", certs =>
        {
            certs.SetDescription("Manage OpenPGP certificates.");

            certs.AddCommand<OpenPgpCertificatesExportCommand>("export")
                .WithDescription("Export a certificate.");

            certs.AddCommand<OpenPgpCertificatesImportCommand>("import")
                .WithDescription("Import a certificate.");

            certs.AddCommand<OpenPgpCertificatesDeleteCommand>("delete")
                .WithDescription("Delete a certificate.");
        });
    });

    // ── PIV ──────────────────────────────────────────────────────────────────
    config.AddBranch("piv", piv =>
    {
        piv.SetDescription("PIV smart card key and certificate management.");

        piv.AddCommand<PivInfoCommand>("info")
            .WithDescription("Display PIV application status and slot overview.");

        piv.AddCommand<PivResetCommand>("reset")
            .WithDescription("Reset PIV application to factory defaults.");

        piv.AddBranch("access", access =>
        {
            access.SetDescription("Manage PIN, PUK, and management key.");

            access.AddCommand<PivAccessChangePinCommand>("change-pin")
                .WithDescription("Change the PIN.");

            access.AddCommand<PivAccessChangePukCommand>("change-puk")
                .WithDescription("Change the PUK.");

            access.AddCommand<PivAccessUnblockPinCommand>("unblock-pin")
                .WithDescription("Unblock the PIN using PUK.");

            access.AddCommand<PivAccessSetManagementKeyCommand>("set-management-key")
                .WithDescription("Change the management key.");
        });

        piv.AddBranch("keys", keys =>
        {
            keys.SetDescription("Manage PIV keys.");

            keys.AddCommand<PivKeysGenerateCommand>("generate")
                .WithDescription("Generate a new key pair on the YubiKey.");

            keys.AddCommand<PivKeysAttestCommand>("attest")
                .WithDescription("Generate an attestation certificate for a slot.");
        });

        piv.AddBranch("certificates", certs =>
        {
            certs.SetDescription("Manage PIV certificates.");

            certs.AddCommand<PivCertificatesExportCommand>("export")
                .WithDescription("Export a certificate from a slot.");

            certs.AddCommand<PivCertificatesImportCommand>("import")
                .WithDescription("Import a certificate to a slot.");

            certs.AddCommand<PivCertificatesDeleteCommand>("delete")
                .WithDescription("Delete a certificate from a slot.");
        });
    });

    // ── YubiHSM Auth ─────────────────────────────────────────────────────────
    config.AddBranch("hsm-auth", hsmAuth =>
    {
        hsmAuth.SetDescription("YubiHSM Auth credential management.");

        hsmAuth.AddCommand<HsmAuthInfoCommand>("info")
            .WithDescription("Display YubiHSM Auth application status.");

        hsmAuth.AddCommand<HsmAuthResetCommand>("reset")
            .WithDescription("Reset YubiHSM Auth applet to factory defaults.");

        hsmAuth.AddBranch("access", access =>
        {
            access.SetDescription("Manage YubiHSM Auth management key.");

            access.AddCommand<HsmAuthAccessChangeManagementKeyCommand>("change-management-key")
                .WithDescription("Change the management key.");
        });

        hsmAuth.AddBranch("credentials", credentials =>
        {
            credentials.SetDescription("Manage YubiHSM Auth credentials.");

            credentials.AddCommand<HsmAuthCredentialsListCommand>("list")
                .WithDescription("List stored credentials.");

            credentials.AddCommand<HsmAuthCredentialsAddCommand>("add")
                .WithDescription("Add a new credential.");

            credentials.AddCommand<HsmAuthCredentialsDeleteCommand>("delete")
                .WithDescription("Delete a credential.");

            credentials.AddCommand<HsmAuthCredentialsGenerateCommand>("generate")
                .WithDescription("Generate an asymmetric credential on-device.");
        });
    });

    // ── Yubico OTP ───────────────────────────────────────────────────────────
    config.AddBranch("otp", otp =>
    {
        otp.SetDescription("Yubico OTP slot configuration.");

        otp.AddCommand<OtpInfoCommand>("info")
            .WithDescription("Display OTP slot status.");

        otp.AddCommand<OtpSwapCommand>("swap")
            .WithDescription("Swap slot 1 and slot 2 configurations.");

        otp.AddCommand<OtpDeleteCommand>("delete")
            .WithDescription("Delete a slot configuration.");

        otp.AddCommand<OtpChalRespCommand>("chalresp")
            .WithDescription("Program HMAC-SHA1 challenge-response.");

        otp.AddCommand<OtpHotpCommand>("hotp")
            .WithDescription("Program HOTP (event-based OTP).");

        otp.AddCommand<OtpStaticCommand>("static")
            .WithDescription("Program static password.");

        otp.AddCommand<OtpYubiOtpCommand>("yubiotp")
            .WithDescription("Program Yubico OTP.");

        otp.AddCommand<OtpCalculateCommand>("calculate")
            .WithDescription("Perform HMAC-SHA1 challenge-response calculation.");

        otp.AddCommand<OtpNdefCommand>("ndef")
            .WithDescription("Configure NDEF for NFC.");

        otp.AddCommand<OtpSettingsCommand>("settings")
            .WithDescription("Update slot settings (flags only).");
    });
});

return app.Run(args);
