// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using System.Text;
using Yubico.YubiKit.Cli.Commands.Oath;
using Yubico.YubiKit.Cli.Shared.Output;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Oath;

namespace Yubico.YubiKit.Cli.Commands.UnitTests.Oath;

public sealed class OathHelpersTests
{
    [Fact]
    public async Task UnlockIfNeededAsync_WithArgvPassword_WarnsOnStderrOnly()
    {
        var session = new FakeOathSession();
        using var console = new ConsoleCapture();

        var result = await OathHelpers.UnlockIfNeededAsync(session, "test-password");

        Assert.True(result);
        Assert.Contains(OathHelpers.ArgvPasswordWarning, console.ErrorOutput);
        Assert.Empty(console.Output);
        Assert.Single(session.ValidatedKeys);
    }

    [Fact]
    public async Task UnlockIfNeededAsync_WithArgvPassword_UsesApprovedWarningText()
    {
        var session = new FakeOathSession();
        using var console = new ConsoleCapture();

        await OathHelpers.UnlockIfNeededAsync(session, "test-password");

        Assert.Contains("inherently insecure", console.ErrorOutput);
        Assert.Contains("testing or demos", console.ErrorOutput);
        Assert.Contains("yk oath accounts list", console.ErrorOutput);
        Assert.DoesNotContain("is secure", console.ErrorOutput, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secure alternative", console.ErrorOutput, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("safe", console.ErrorOutput, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UnlockIfNeededAsync_WithArgvPassword_ZerosOwnedPasswordAndDerivedKey()
    {
        var session = new FakeOathSession();
        using var console = new ConsoleCapture();

        await OathHelpers.UnlockIfNeededAsync(session, "test-password");

        Assert.All(session.DerivedPasswordMemory.ToArray(), b => Assert.Equal(0, b));
        Assert.All(session.ValidatedKeys.Single().ToArray(), b => Assert.Equal(0, b));
    }

    [Fact]
    public async Task UnlockIfNeededAsync_WithPromptedCredential_DoesNotWarn()
    {
        var session = new FakeOathSession();
        using var console = new ConsoleCapture();
        using var prompted = SecureCredential.FromUtf8String("prompt-password");

        var result = await OathHelpers.UnlockIfNeededAsync(
            session,
            password: null,
            promptCredentialFactory: () => prompted);

        Assert.True(result);
        Assert.DoesNotContain(OathHelpers.ArgvPasswordWarning, console.ErrorOutput);
        Assert.Empty(console.Output);
        Assert.Equal(Encoding.UTF8.GetBytes("prompt-password"), session.DerivedPasswordBytes);
        Assert.All(GetOwnedBuffer(prompted), b => Assert.Equal(0, b));
    }

    [Fact]
    public async Task UnlockIfNeededAsync_WithEmptyPassword_UsesPromptedCredentialWithoutWarning()
    {
        var session = new FakeOathSession();
        using var console = new ConsoleCapture();
        using var prompted = SecureCredential.FromUtf8String("prompt-password");

        var result = await OathHelpers.UnlockIfNeededAsync(
            session,
            password: string.Empty,
            promptCredentialFactory: () => prompted);

        Assert.True(result);
        Assert.DoesNotContain(OathHelpers.ArgvPasswordWarning, console.ErrorOutput);
        Assert.Empty(console.Output);
        Assert.Equal(Encoding.UTF8.GetBytes("prompt-password"), session.DerivedPasswordBytes);
        Assert.All(GetOwnedBuffer(prompted), b => Assert.Equal(0, b));
    }

    private static byte[] GetOwnedBuffer(SecureCredential credential)
    {
        var field = typeof(SecureCredential).GetField("_buffer", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SecureCredential buffer field not found.");

        return (byte[])field.GetValue(credential)!;
    }

    private sealed class FakeOathSession : IOathSession
    {
        private readonly byte[] _derivedKey = [0x01, 0x02, 0x03, 0x04];

        public FirmwareVersion FirmwareVersion { get; } = new(5, 8, 0);
        public bool IsInitialized => true;
        public bool IsAuthenticated => true;
        public string DeviceId => "test-device";
        public ReadOnlyMemory<byte> Salt => new byte[] { 0x00 };
        public bool IsLocked { get; set; } = true;
        public ReadOnlyMemory<byte> DerivedPasswordMemory { get; private set; }
        public byte[] DerivedPasswordBytes { get; private set; } = [];
        public List<ReadOnlyMemory<byte>> ValidatedKeys { get; } = [];

        public byte[] DeriveKey(ReadOnlyMemory<byte> passwordUtf8)
        {
            DerivedPasswordMemory = passwordUtf8;
            DerivedPasswordBytes = passwordUtf8.ToArray();
            return (byte[])_derivedKey.Clone();
        }

        public Task ValidateAsync(ReadOnlyMemory<byte> key, CancellationToken cancellationToken = default)
        {
            ValidatedKeys.Add(key);
            return Task.CompletedTask;
        }

        public bool IsSupported(Feature feature) => true;
        public void EnsureSupports(Feature feature) { }
        public Task<IReadOnlyList<Credential>> ListCredentialsAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task PutCredentialAsync(CredentialData credentialData, bool requireTouch = false, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteCredentialAsync(Credential credential, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Credential> RenameCredentialAsync(Credential credential, string? newIssuer, string newName, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ReadOnlyMemory<byte>> CalculateAsync(Credential credential, ReadOnlyMemory<byte> challenge, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Code> CalculateCodeAsync(Credential credential, long? timestamp = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Dictionary<Credential, Code?>> CalculateAllAsync(long? timestamp = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task ResetAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SetKeyAsync(ReadOnlyMemory<byte> key, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UnsetKeyAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class ConsoleCapture : IDisposable
    {
        private readonly TextWriter _originalOut = Console.Out;
        private readonly TextWriter _originalError = Console.Error;
        private readonly StringWriter _out = new();
        private readonly StringWriter _error = new();

        public ConsoleCapture()
        {
            Console.SetOut(_out);
            Console.SetError(_error);
        }

        public string Output => _out.ToString();
        public string ErrorOutput => _error.ToString();

        public void Dispose()
        {
            Console.SetOut(_originalOut);
            Console.SetError(_originalError);
            _out.Dispose();
            _error.Dispose();
        }
    }
}
