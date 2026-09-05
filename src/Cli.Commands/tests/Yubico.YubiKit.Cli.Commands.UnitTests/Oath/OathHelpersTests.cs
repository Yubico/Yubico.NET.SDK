// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using System.Buffers;
using System.Text;
using Yubico.YubiKit.Cli.Commands.Oath;
using Yubico.YubiKit.Cli.Shared.Output;
using Yubico.YubiKit.Core.Devices;
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
        var prompted = new TrackingCredential("prompt-password");

        var result = await OathHelpers.UnlockIfNeededAsync(
            session,
            password: null,
            promptCredentialFactory: () => prompted);

        Assert.True(result);
        Assert.DoesNotContain(OathHelpers.ArgvPasswordWarning, console.ErrorOutput);
        Assert.Empty(console.Output);
        Assert.Equal(Encoding.UTF8.GetBytes("prompt-password"), session.DerivedPasswordBytes);
        Assert.True(prompted.WasDisposed);
    }

    [Fact]
    public async Task UnlockIfNeededAsync_WithEmptyPassword_UsesPromptedCredentialWithoutWarning()
    {
        var session = new FakeOathSession();
        using var console = new ConsoleCapture();
        var prompted = new TrackingCredential("prompt-password");

        var result = await OathHelpers.UnlockIfNeededAsync(
            session,
            password: string.Empty,
            promptCredentialFactory: () => prompted);

        Assert.True(result);
        Assert.DoesNotContain(OathHelpers.ArgvPasswordWarning, console.ErrorOutput);
        Assert.Empty(console.Output);
        Assert.Equal(Encoding.UTF8.GetBytes("prompt-password"), session.DerivedPasswordBytes);
        Assert.True(prompted.WasDisposed);
    }


    private sealed class FakeOathSession : IOathSession
    {
        private readonly byte[] _derivedKey = [0x01, 0x02, 0x03, 0x04];

        public FirmwareVersion FirmwareVersion { get; } = new(5, 8, 0);
        public ConnectionType ConnectionType => ConnectionType.SmartCard;
        public bool IsInitialized => true;
        public bool IsAuthenticated => true;
        public string DeviceId => "test-device";
        public ReadOnlyMemory<byte> Salt => new byte[] { 0x00 };
        public bool IsLocked { get; set; } = true;
        public bool IsPasswordProtected { get; set; } = true;
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
        public Task PutCredentialAsync(CredentialData credentialData, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteCredentialAsync(Credential credential, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Credential> RenameCredentialAsync(Credential credential, string? newIssuer, string newName, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ReadOnlyMemory<byte>> CalculateAsync(Credential credential, ReadOnlyMemory<byte> challenge, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Code> CalculateCodeAsync(Credential credential, long? timestamp = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyDictionary<Credential, Code?>> CalculateAllAsync(long? timestamp = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task ResetAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SetKeyAsync(ReadOnlyMemory<byte> key, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UnsetKeyAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public async Task<T> AuthenticateAndRetryAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            Func<CancellationToken, Task<ReadOnlyMemory<byte>>> passwordProvider,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await operation(cancellationToken).ConfigureAwait(false);
            }
            catch (OathException ex) when (ex.Reason == OathFailureReason.Locked)
            {
                ReadOnlyMemory<byte> password = await passwordProvider(cancellationToken).ConfigureAwait(false);
                byte[] key = DeriveKey(password);
                try
                {
                    await ValidateAsync(key, cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    Array.Clear(key);
                }

                return await operation(cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task AuthenticateAndRetryAsync(
            Func<CancellationToken, Task> operation,
            Func<CancellationToken, Task<ReadOnlyMemory<byte>>> passwordProvider,
            CancellationToken cancellationToken = default)
        {
            await AuthenticateAndRetryAsync<object?>(
                async ct =>
                {
                    await operation(ct).ConfigureAwait(false);
                    return null;
                },
                passwordProvider,
                cancellationToken).ConfigureAwait(false);
        }

        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    [Fact]
    public async Task UnlockIfNeededAsync_WhenUserDeclinesPrompt_FailsWithoutValidating()
    {
        var session = new FakeOathSession();
        using var console = new ConsoleCapture();

        var result = await OathHelpers.UnlockIfNeededAsync(
            session,
            password: null,
            promptCredentialFactory: () => null);

        Assert.False(result);
        Assert.Empty(session.ValidatedKeys);
        Assert.DoesNotContain(OathHelpers.ArgvPasswordWarning, console.ErrorOutput);
    }

    /// <summary>
    /// Records disposal so tests can assert the consumer released the credential it was handed.
    /// Zeroing itself is <see cref="SecureCredential"/>'s contract and is tested in
    /// Cli.Shared.UnitTests, not here.
    /// </summary>
    private sealed class TrackingCredential(string value) : IMemoryOwner<byte>
    {
        private readonly byte[] _bytes = Encoding.UTF8.GetBytes(value);

        public bool WasDisposed { get; private set; }

        public Memory<byte> Memory => _bytes;

        public void Dispose() => WasDisposed = true;
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
