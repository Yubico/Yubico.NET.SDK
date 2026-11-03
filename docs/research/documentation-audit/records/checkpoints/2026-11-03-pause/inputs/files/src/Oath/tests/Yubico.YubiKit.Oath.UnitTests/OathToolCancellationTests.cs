// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using System.Buffers;
using System.Security.Cryptography;
using Yubico.YubiKit.Cli.Commands.Infrastructure;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Oath.Examples.OathTool;
using Yubico.YubiKit.Oath.Examples.OathTool.Cli;
using Yubico.YubiKit.Oath.Examples.OathTool.Commands;

namespace Yubico.YubiKit.Oath.UnitTests;

public class OathToolCancellationTests
{
    [Theory]
    [InlineData("before-input")]
    [InlineData("during-input")]
    [InlineData("after-input")]
    [InlineData("before-validation")]
    [InlineData("during-validation")]
    public async Task Unlock_CancellationPropagatesAndClearsOnlyOwnedBuffers(string stage)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        using var prompted = new SecretOwner();
        using var caller = new SecretOwner();
        var session = new FakeSession { IsLocked = true };
        var reader = new FakeReader(token =>
        {
            Assert.Equal(cancellation.Token, token);
            if (stage is "during-input" or "after-input")
            {
                cancellation.Cancel();
            }

            if (stage == "during-input")
            {
                token.ThrowIfCancellationRequested();
            }

            return prompted;
        });
        if (stage == "before-input")
        {
            cancellation.Cancel();
        }

        session.OnDerive = () =>
        {
            if (stage == "before-validation")
            {
                cancellation.Cancel();
            }
        };
        session.OnOperation = token =>
        {
            Assert.Equal(cancellation.Token, token);
            cancellation.Cancel();
            return Task.FromCanceled(cancellation.Token);
        };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => OathSessionHelper.UnlockSessionAsync(
            session, stage is "before-validation" or "during-validation" ? caller : null,
            cancellation.Token, reader));

        Assert.False(caller.Disposed);
        Assert.Contains((byte)0x42, caller.Bytes);
        Assert.All(session.DerivedKey, value => Assert.Equal(0, value));
        if (stage == "after-input")
        {
            Assert.True(prompted.Disposed);
            Assert.All(prompted.Bytes, value => Assert.Equal(0, value));
        }

        if (stage is "before-input" or "during-input" or "after-input" or "before-validation")
        {
            Assert.Equal(0, session.Operations);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task UnsuccessfulUnlock_DisposesSessionButNotCallerInput(bool cancel)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        using var caller = new SecretOwner();
        var session = new FakeSession { IsLocked = true };
        session.OnOperation = _ =>
        {
            if (cancel)
            {
                cancellation.Cancel();
                return Task.FromCanceled(cancellation.Token);
            }

            return Task.FromException(new InvalidOperationException("rejected"));
        };
        var services = Services(session);

        if (cancel)
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                OathSessionHelper.CreateUnlockedSessionAsync(caller, cancellation.Token, services));
        }
        else
        {
            Assert.Null(await OathSessionHelper.CreateUnlockedSessionAsync(caller, cancellation.Token, services));
        }

        Assert.True(session.Disposed);
        Assert.False(caller.Disposed);
        Assert.All(caller.Bytes, value => Assert.Equal(0x42, value));
        Assert.All(session.DerivedKey, value => Assert.Equal(0, value));
    }

    [Fact]
    public async Task PasswordConfirmation_CancellationDisposesSessionAndStopsKeyChange()
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var session = new FakeSession();
        var reader = new FakeReader(token =>
        {
            Assert.Equal(cancellation.Token, token);
            cancellation.Cancel();
            token.ThrowIfCancellationRequested();
            return null;
        });
        var services = Services(session, reader);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            AccessCommand.ChangeAsync(cancellationToken: cancellation.Token, services: services));

        Assert.True(reader.ConfirmationRead);
        Assert.True(session.Disposed);
        Assert.Equal(0, session.Operations);
    }

    [Theory]
    [InlineData("after-input")]
    [InlineData("before-set")]
    [InlineData("during-set")]
    public async Task NewPasswordCancellation_ClearsPromptedBytesAndDerivedKey(string stage)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        using var prompted = new SecretOwner();
        var session = new FakeSession();
        var reader = new FakeReader(token =>
        {
            Assert.Equal(cancellation.Token, token);
            if (stage == "after-input") { cancellation.Cancel(); }
            return prompted;
        });
        session.OnDerive = () => { if (stage == "before-set") { cancellation.Cancel(); } };
        session.OnOperation = token =>
        {
            Assert.Equal(cancellation.Token, token);
            cancellation.Cancel();
            return Task.FromCanceled(cancellation.Token);
        };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            AccessCommand.ChangeAsync(cancellationToken: cancellation.Token, services: Services(session, reader)));

        Assert.True(session.Disposed);
        Assert.True(prompted.Disposed);
        Assert.All(prompted.Bytes, value => Assert.Equal(0, value));
        Assert.All(session.DerivedKey, value => Assert.Equal(0, value));
        Assert.Equal(stage == "during-set" ? 1 : 0, session.Operations);
    }

    [Fact]
    public async Task UnconfirmedPassword_IsCancellationNotAuthenticationFailure()
    {
        var session = new FakeSession();
        int exitCode = await AccessCommand.ChangeAsync(
            cancellationToken: TestContext.Current.CancellationToken, services: Services(session));
        Assert.Equal(ExitCode.UserCancelled, exitCode);
        Assert.True(session.Disposed);
        Assert.Equal(0, session.Operations);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CancelledSetup_DisposesAnyReturnedSessionAndDoesNotMutate(bool hasSession)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var session = new FakeSession();
        var services = new OathToolServices(_ =>
        {
            cancellation.Cancel();
            return Task.FromResult<IOathSession?>(hasSession ? session : null);
        }, new FakeReader(_ => null), (_, _) => ValueTask.FromResult(true));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            AccessCommand.ChangeAsync(clear: true, cancellationToken: cancellation.Token, services: services));

        Assert.Equal(hasSession, session.Disposed);
        Assert.Equal(0, session.Operations);
    }

    [Theory]
    [InlineData("add")]
    [InlineData("uri")]
    [InlineData("delete")]
    [InlineData("rename")]
    [InlineData("reset")]
    public async Task ConfirmationCancellation_DoesNotMutate(string command)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var session = new FakeSession();
        var services = new OathToolServices(_ => Task.FromResult<IOathSession?>(session), new FakeReader(_ => null),
            (_, token) =>
            {
                Assert.Equal(cancellation.Token, token);
                cancellation.Cancel();
                return ValueTask.FromCanceled<bool>(cancellation.Token);
            });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            InvokeAsync(command, services, false, cancellation.Token));

        Assert.Equal(0, session.Operations);
        // Add/URI/reset may ask before opening; delete/rename have to find the account first.
        if (command is "delete" or "rename")
        {
            Assert.True(session.Disposed);
        }
    }

    [Theory]
    [InlineData("add")]
    [InlineData("uri")]
    [InlineData("delete")]
    [InlineData("rename")]
    [InlineData("single-code")]
    [InlineData("bulk-code")]
    [InlineData("list")]
    [InlineData("reset")]
    [InlineData("clear")]
    [InlineData("set")]
    public async Task OperationCancellation_ReachesCliBoundaryAndDisposesSession(string command)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        using var password = new SecretOwner();
        var session = new FakeSession { CancelList = command == "list" };
        session.OnOperation = token =>
        {
            Assert.Equal(cancellation.Token, token);
            cancellation.Cancel();
            return Task.FromCanceled(cancellation.Token);
        };
        var services = Services(session);

        int exitCode = await OathToolApplication.RunAsync(["info"],
            (_, token) => command switch
            {
                "set" => AccessCommand.ChangeAsync(newPasswordBytes: password, cancellationToken: token, services: services),
                "clear" => AccessCommand.ChangeAsync(clear: true, cancellationToken: token, services: services),
                _ => InvokeAsync(command, services, true, token)
            }, cancellation.Token);

        Assert.Equal(ExitCode.UserCancelled, exitCode);
        Assert.Equal(1, session.Operations);
        Assert.True(session.Disposed);
        Assert.False(password.Disposed);
        Assert.All(password.Bytes, value => Assert.Equal(0x42, value));
        Assert.All(session.DerivedKey, value => Assert.Equal(0, value));
    }

    [Fact]
    public async Task AlreadyCancelled_DoesNotBootstrap()
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cancellation.Cancel();
        int calls = 0;
        int exitCode = await OathToolApplication.RunAsync(["reset", "--force"], (_, _) =>
        {
            calls++;
            return Task.FromResult(0);
        }, cancellation.Token);
        Assert.Equal(ExitCode.UserCancelled, exitCode);
        Assert.Equal(0, calls);
    }

    [Theory]
    [InlineData("add")]
    [InlineData("uri")]
    [InlineData("delete")]
    [InlineData("rename")]
    [InlineData("reset")]
    public async Task DeclinedConfirmation_ReturnsRepositoryCancellationCode(string command)
    {
        var session = new FakeSession();
        var services = new OathToolServices(_ => Task.FromResult<IOathSession?>(session), new FakeReader(_ => null),
            (_, _) => ValueTask.FromResult(false));
        int exitCode = await InvokeAsync(command, services, false, TestContext.Current.CancellationToken);
        Assert.Equal(ExitCode.UserCancelled, exitCode);
        Assert.Equal(0, session.Operations);
    }

    [Fact]
    public async Task CancelledBoundary_WritesCancellationInsteadOfAuthenticationFailure()
    {
        using var output = new StringWriter();
        int exitCode = await OathToolApplication.RunAsync(["info"],
            (_, _) => Task.FromException<int>(new OperationCanceledException()),
            TestContext.Current.CancellationToken, output);
        Assert.Equal(ExitCode.UserCancelled, exitCode);
        Assert.Contains("Operation cancelled.", output.ToString());
        Assert.DoesNotContain("Authentication failed", output.ToString());
    }

    [Theory]
    [InlineData("A")]
    [InlineData("AAA")]
    [InlineData("AAAAAA")]
    [InlineData("MY=")]
    [InlineData("MY=======")]
    [InlineData("========")]
    [InlineData("M=Y=====")]
    [InlineData("MZXW6YTB!")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Add_MalformedOrAbsentSecret_NeverOpensSession(string encoded)
    {
        int opens = 0;
        var services = new OathToolServices(_ => { opens++; return Task.FromResult<IOathSession?>(null); },
            new FakeReader(_ => null), (_, _) => ValueTask.FromResult(true));
        int exit = await AccountsCommand.AddAsync("name", encoded, force: true,
            cancellationToken: TestContext.Current.CancellationToken, services: services);
        Assert.Equal(1, exit);
        Assert.Equal(0, opens);
    }

    [Theory]
    [InlineData("MY======", "f")]
    [InlineData("MZXQ====", "fo")]
    [InlineData("MZXW6===", "foo")]
    [InlineData("MZXW6YQ=", "foob")]
    [InlineData("MZXW6YTB", "fooba")]
    [InlineData("m z x w 6 y t b o i", "foobar")]
    [InlineData("MZXW6YTBOI======", "foobar")]
    [InlineData("MZ", "f")]
    public async Task Add_RfcSecretBytes_ReachPutThenAreDisposed(string encoded, string expected)
    {
        foreach (string input in new[] { encoded, encoded.TrimEnd('=') })
        {
            var session = new FakeSession();
            session.OnPut = data => Assert.True(CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.ASCII.GetBytes(expected), data.Secret.Span));
            int exit = await AccountsCommand.AddAsync("name", input, force: true,
                cancellationToken: TestContext.Current.CancellationToken, services: Services(session));
            Assert.Equal(0, exit);
            Assert.Equal(1, session.Operations);
            Assert.True(session.PutSecret.Span.IndexOfAnyExcept((byte)0) < 0);
        }
    }

    private static OathToolServices Services(FakeSession session, ISecureCredentialReader? reader = null) =>
        new(_ => Task.FromResult<IOathSession?>(session), reader ?? new FakeReader(_ => null),
            (_, _) => ValueTask.FromResult(true));

    private static Task<int> InvokeAsync(string command, OathToolServices services, bool force, CancellationToken token) => command switch
    {
        "add" => AccountsCommand.AddAsync("name", "MY", force: force, cancellationToken: token, services: services),
        "uri" => AccountsCommand.UriAsync("otpauth://totp/name?secret=MY", force: force, cancellationToken: token, services: services),
        "delete" => AccountsCommand.DeleteAsync("name", force, cancellationToken: token, services: services),
        "rename" => AccountsCommand.RenameAsync("name", "new", force, cancellationToken: token, services: services),
        "single-code" => AccountsCommand.CodeAsync("name", cancellationToken: token, services: services),
        "bulk-code" => AccountsCommand.CodeAsync(cancellationToken: token, services: services),
        "list" => AccountsCommand.ListAsync(cancellationToken: token, services: services),
        "reset" => ResetCommand.ExecuteAsync(force, token, services),
        _ => throw new InvalidOperationException(command)
    };

    private sealed class SecretOwner : IMemoryOwner<byte>
    {
        public byte[] Bytes { get; } = [0x42, 0x42];
        public Memory<byte> Memory => Bytes;
        public bool Disposed { get; private set; }
        public void Dispose()
        {
            Disposed = true;
            CryptographicOperations.ZeroMemory(Bytes);
        }
    }

    private sealed class FakeReader(Func<CancellationToken, IMemoryOwner<byte>?> read) : ISecureCredentialReader
    {
        public bool ConfirmationRead { get; private set; }
        public IMemoryOwner<byte>? ReadCredential(CredentialReaderOptions options, CancellationToken cancellationToken = default) => read(cancellationToken);
        public IMemoryOwner<byte>? ReadCredentialWithConfirmation(CredentialReaderOptions options, CancellationToken cancellationToken = default)
        {
            ConfirmationRead = true;
            return read(cancellationToken);
        }
    }

    private sealed class FakeSession : IOathSession
    {
        public bool IsLocked { get; init; }
        public bool Disposed { get; private set; }
        public int Operations { get; private set; }
        public bool CancelList { get; init; }
        public byte[] DerivedKey { get; private set; } = [];
        public Action OnDerive { get; set; } = () => { };
        public Action<CredentialData> OnPut { get; set; } = _ => { };
        public ReadOnlyMemory<byte> PutSecret { get; private set; }
        public Func<CancellationToken, Task> OnOperation { get; set; } = _ => Task.CompletedTask;
        public FirmwareVersion FirmwareVersion => new(5, 7, 0);
        public ConnectionType ConnectionType => ConnectionType.SmartCard;
        public bool IsInitialized => !Disposed;
        public bool IsAuthenticated => false;
        public string DeviceId => "fake";
        public ReadOnlyMemory<byte> Salt => ReadOnlyMemory<byte>.Empty;
        public bool IsPasswordProtected => IsLocked;
        public bool IsSupported(Feature feature) => true;
        public void EnsureSupports(Feature feature) { }
        public void Dispose() => Disposed = true;
        public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
        public byte[] DeriveKey(ReadOnlyMemory<byte> password) { OnDerive(); DerivedKey = [1, 2, 3]; return DerivedKey; }
        private Task Operate(CancellationToken token) { Operations++; return OnOperation(token); }
        public async Task<IReadOnlyList<Credential>> ListCredentialsAsync(CancellationToken cancellationToken = default)
        {
            if (CancelList) { await Operate(cancellationToken); }
            return [new Credential("fake", "name"u8.ToArray(), null, "name", OathType.Totp, 30, false)];
        }
        public Task ValidateAsync(ReadOnlyMemory<byte> key, CancellationToken cancellationToken = default) => Operate(cancellationToken);
        public Task PutCredentialAsync(CredentialData credentialData, CancellationToken cancellationToken = default)
        {
            PutSecret = credentialData.Secret;
            OnPut(credentialData);
            return Operate(cancellationToken);
        }
        public Task DeleteCredentialAsync(Credential credential, CancellationToken cancellationToken = default) => Operate(cancellationToken);
        public async Task<Credential> RenameCredentialAsync(Credential credential, string? newIssuer, string newName, CancellationToken cancellationToken = default)
        { await Operate(cancellationToken); return credential; }
        public async Task<Code> CalculateCodeAsync(Credential credential, long? timestamp = null, CancellationToken cancellationToken = default)
        { await Operate(cancellationToken); throw new InvalidOperationException("Expected cancellation"); }
        public async Task<IReadOnlyDictionary<Credential, Code?>> CalculateAllAsync(long? timestamp = null, CancellationToken cancellationToken = default)
        { await Operate(cancellationToken); return new Dictionary<Credential, Code?>(); }
        public Task ResetAsync(CancellationToken cancellationToken = default) => Operate(cancellationToken);
        public Task SetKeyAsync(ReadOnlyMemory<byte> key, CancellationToken cancellationToken = default) => Operate(cancellationToken);
        public Task UnsetKeyAsync(CancellationToken cancellationToken = default) => Operate(cancellationToken);
        public Task<ReadOnlyMemory<byte>> CalculateAsync(Credential credential, ReadOnlyMemory<byte> challenge, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<T> AuthenticateAndRetryAsync<T>(Func<CancellationToken, Task<T>> operation, Func<CancellationToken, Task<ReadOnlyMemory<byte>>> passwordProvider, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AuthenticateAndRetryAsync(Func<CancellationToken, Task> operation, Func<CancellationToken, Task<ReadOnlyMemory<byte>>> passwordProvider, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}