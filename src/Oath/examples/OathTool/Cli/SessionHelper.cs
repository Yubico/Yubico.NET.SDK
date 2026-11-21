// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using System.Buffers;
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Oath.Credentials;
using Yubico.YubiKit.Oath.Examples.OathTool.Cli.Output;

namespace Yubico.YubiKit.Oath.Examples.OathTool.Cli;

/// <summary>
/// Shared helpers for device selection, session creation, and password unlock.
/// </summary>
public static class OathSessionHelper
{
    /// <summary>
    /// Selects a device and creates an OATH session. If the session is locked,
    /// prompts for (or uses the provided) password bytes to unlock it.
    /// Returns null if device selection fails or unlock fails.
    /// </summary>
    /// <remarks>
    /// The caller disposes a returned session. Unsuccessful setup disposes the session before returning
    /// or propagating cancellation. Redirected password input cannot interrupt its synchronous line read;
    /// cancellation is checked before reading and after the reader returns.
    /// </remarks>
    /// <param name="passwordBytes">
    /// Pre-encoded password bytes (e.g., from CLI flag). Caller retains ownership; this method does not dispose.
    /// If null and the session is locked, the user will be prompted interactively.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="services">Application I/O dependencies; null uses the console and device selector.</param>
    public static async Task<IOathSession?> CreateUnlockedSessionAsync(
        IMemoryOwner<byte>? passwordBytes = null,
        CancellationToken cancellationToken = default,
        OathToolServices? services = null)
    {
        services ??= OathToolServices.Default;
        cancellationToken.ThrowIfCancellationRequested();
        var session = await services.OpenSession(cancellationToken);
        if (session is null)
        {
            return null;
        }

        bool transferred = false;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (session.IsLocked && !await UnlockSessionAsync(
                    session, passwordBytes, cancellationToken, services.CredentialReader))
            {
                return null;
            }

            transferred = true;
            return session;
        }
        finally
        {
            if (!transferred)
            {
                await session.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Unlocks a password-protected OATH session.
    /// Uses the provided password bytes, or prompts interactively via <see cref="ConsoleCredentialReader"/> if null.
    /// </summary>
    /// <remarks>
    /// Propagates cancellation without reporting authentication failure. Clears the derived key and any
    /// prompted password after validation finishes, including on failure. Does not dispose the session
    /// or the caller's password buffer.
    /// </remarks>
    /// <param name="session">The locked OATH session.</param>
    /// <param name="passwordBytes">
    /// Pre-encoded password bytes. Caller retains ownership; this method does not dispose.
    /// If null, the user is prompted interactively.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="credentialReader">Credential reader; null uses the console.</param>
    public static async Task<bool> UnlockSessionAsync(
        IOathSession session,
        IMemoryOwner<byte>? passwordBytes = null,
        CancellationToken cancellationToken = default,
        ISecureCredentialReader? credentialReader = null)
    {
        IMemoryOwner<byte>? prompted = null;
        byte[]? key = null;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (passwordBytes is null)
            {
                var reader = credentialReader ?? new ConsoleCredentialReader();
                prompted = reader.ReadCredential(OathCredentialOptions.ForOathPassword(), cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (prompted is null)
                {
                    throw new OperationCanceledException("Password entry cancelled or unavailable.", cancellationToken);
                }

                passwordBytes = prompted;
            }

            key = session.DeriveKey(passwordBytes.Memory);
            cancellationToken.ThrowIfCancellationRequested();
            await session.ValidateAsync(key, cancellationToken);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            OutputHelpers.WriteError($"Authentication failed: {ex.Message}");
            return false;
        }
        finally
        {
            if (key is not null)
            {
                CryptographicOperations.ZeroMemory(key);
            }

            prompted?.Dispose();
        }
    }
}