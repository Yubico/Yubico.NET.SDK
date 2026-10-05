// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using System.Buffers;
using System.Security.Cryptography;

namespace Yubico.YubiKit.Core.Credentials;

internal static class CredentialAcquisition
{
    internal static async Task<IMemoryOwner<byte>> AcquireAsync(
        ICredentialPrompt prompt, CredentialPromptContext context, CancellationToken cancellationToken,
        Action<bool> markCallback)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // ValueTask may complete synchronously. Materialize it once so cancellation can detach
        // without abandoning ownership of a later successful result.
        Task<IMemoryOwner<byte>?> request;
        markCallback(true);
        try
        {
            request = prompt.RequestSecretAsync(context, cancellationToken).AsTask();
        }
        finally
        {
            markCallback(false);
        }
        IMemoryOwner<byte>? owner;
        try
        {
            owner = await request.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _ = ReleaseLateAsync(request);
            throw;
        }

        return owner ?? throw new CredentialPromptDeclinedException(context.Kind);
    }

    private static async Task ReleaseLateAsync(Task<IMemoryOwner<byte>?> request)
    {
        try
        {
            IMemoryOwner<byte>? owner = await request.ConfigureAwait(false);
            if (owner is not null)
                Release(owner);
        }
        catch
        {
            // A faulted/cancelled provider transfers no owner. Observe its failure.
        }
    }

    internal static void Release(IMemoryOwner<byte> owner)
    {
        try
        {
            CryptographicOperations.ZeroMemory(owner.Memory.Span);
        }
        finally
        {
            owner.Dispose();
        }
    }
}