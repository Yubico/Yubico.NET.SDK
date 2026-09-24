// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Native.Desktop.SCard;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Protocols.Otp.Hid;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Core.UnitTests.Devices;
using Yubico.YubiKit.Core.UnitTests.Protocols.SmartCard.Apdu.Fakes;
using Yubico.YubiKit.Core.UnitTests.Transports.SmartCard.Fakes;

namespace Yubico.YubiKit.Core.UnitTests.BoundaryInventory;

[Collection(DiscoveryWorkerAdmissionCollection.Name)]
public class MigrationDiagnosticsTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public void MigratedConnectionLogSites_AreLimitedToKnownPcscMetadata()
    {
        string root = BoundaryScanner.CoreSourceRoot();
        foreach (string path in new[]
        {
            "Transports/Hid/MacOS/MacOSFidoHidConnection.cs",
            "Transports/Hid/MacOS/MacOSOtpHidConnection.cs"
        })
        {
            Assert.Empty(LogInvocations(Path.Combine(root, path)));
        }

        Assert.Equal(new[]
        {
            "Initializing smart card connection to reader {ReaderName}",
            "Smart card connection initialized to reader {ReaderName}",
            "SCardEndTransaction returned {Error}",
            "SCardEndTransaction returned {Error}"
        }, LogInvocations(Path.Combine(root, "Transports/SmartCard/UsbSmartCardConnection.cs")));
    }

    [Fact]
    public void MigratedProtocolAndDiscoveryLogSites_AreClassified()
    {
        string root = BoundaryScanner.CoreSourceRoot();
        Assert.Equal(new[]
        {
            "Transmitting APDU: CLA 0x{Cla:X2} INS 0x{Ins:X2} P1 0x{P1:X2} P2 0x{P2:X2} Le {Le} data length {Length}",
            "Selecting application ID: {ApplicationId}"
        }, LogInvocations(Path.Combine(root, "Protocols/SmartCard/Apdu/PcscProtocol.cs")));
        Assert.Equal(new[]
        {
            "Getting list of HID devices", "Found {Count} Yubico HID devices",
            "udev native library not available, returning no HID devices: {Message}"
        }, LogInvocations(Path.Combine(root, "Transports/Hid/FindHidInterfaces.cs")));
        Assert.Equal(new[]
        {
            "Connected to YubiKey in reader {ReaderName}",
            "Connected to YubiKey in reader {ReaderName}",
            "Connected to YubiKey in reader {ReaderName}"
        }, LogInvocations(Path.Combine(root, "Devices/PcscConnectionSlot.cs")));
    }

    [Fact]
    public void LogSiteInventory_SeesNewPayloadFormattedCalls()
    {
        Assert.Equal(new[] { "raw {Payload}" }, LogInvocationsInSource("class Probe { void Send() { _logger.LogTrace(\"raw {Payload}\", command); } }"));
    }

    [Fact]
    public void RecordingProvider_CapturesStructuredValuesAndExceptions()
    {
        using var provider = new RecordingProvider();
        const string marker = "ISC56-provider-positive-control";
        provider.CreateLogger("control").LogWarning(new IOException(marker), "control {Payload}", marker);

        Assert.Contains(provider.Events, entry => entry.Contains(marker, StringComparison.Ordinal));
        Assert.Contains(provider.Events, entry => entry.Contains("Payload=ISC56-provider-positive-control", StringComparison.Ordinal));
        Assert.Contains(provider.Events, entry => entry.Contains("IOException: ISC56-provider-positive-control", StringComparison.Ordinal));
    }

    [Fact]
    public void PayloadAssertion_RejectsFragmentedStructuredAndExceptionLeaks()
    {
        byte[] secret = Encoding.ASCII.GetBytes("ISC56-negative-control");
        using var provider = new RecordingProvider();
        provider.CreateLogger("control").LogWarning(new IOException("ISC56 leaked"),
            "fragment {Payload}", "ISC56");
        try
        {
            Assert.Contains(provider.Events, entry => entry.Contains("Payload=ISC56", StringComparison.Ordinal));
            Assert.Contains(provider.Events, entry => entry.Contains("IOException: ISC56", StringComparison.Ordinal));
            Assert.ThrowsAny<Exception>(() => AssertNoPayload(provider.Events, secret));
        }
        finally { CryptographicOperations.ZeroMemory(secret); }
    }

    private static string[] LogInvocations(string file) => LogInvocationsInSource(File.ReadAllText(file));

    private static string[] LogInvocationsInSource(string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        return [.. tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Where(invocation => invocation.Expression is MemberAccessExpressionSyntax member &&
                member.Name.Identifier.ValueText.StartsWith("Log", StringComparison.Ordinal))
            .Select(invocation => invocation.ArgumentList.Arguments
                .Select(argument => argument.Expression)
                .OfType<LiteralExpressionSyntax>()
                .Single(literal => literal.Kind() == SyntaxKind.StringLiteralExpression)
                .Token.ValueText)];
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PcscOpenAndTransactionEnd_LogMetadataButNotCommand(bool asyncEnd)
    {
        const string marker = "ISC56-command-PIN-key-payload";
        byte[] command = Encoding.ASCII.GetBytes(marker);
        var api = new ControlledSCardConnectionApi { EndTransactionResult = ErrorCode.SCARD_E_NOT_TRANSACTED };
        api.ReleaseTransmit.Set();
        using var provider = new RecordingProvider();
        using var factory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Trace).AddProvider(provider));
        using var temporary = YubiKitLogging.UseTemporary(factory);

        try
        {
            var connection = await PcscTestDevices.Create(api).ConnectAsync<ISmartCardConnection>(Ct);
            try
            {
                ReadOnlyMemory<byte> response = await connection.TransmitAndReceiveAsync(command, Ct);
                Assert.Equal(new byte[] { 0x90, 0x00 }, response.ToArray());
                IDisposable transaction = asyncEnd
                    ? await connection.BeginTransactionAsync(Ct)
                    : connection.BeginTransaction(Ct);
                if (asyncEnd)
                    await ((IAsyncDisposable)transaction).DisposeAsync();
                else
                    transaction.Dispose();
            }
            finally
            {
                _ = await Assert.ThrowsAsync<SCardException>(() => connection.DisposeAsync().AsTask());
            }

            string[] events = provider.Events;
            Assert.Contains(events, entry => entry.Contains("Initializing smart card connection to reader", StringComparison.Ordinal));
            Assert.Contains(events, entry => entry.Contains("Smart card connection initialized to reader", StringComparison.Ordinal));
            Assert.Contains(events, entry => entry.Contains("SCardEndTransaction returned", StringComparison.Ordinal));
            Assert.Equal(1, api.TransmitCalls);
            Assert.Equal(1, api.EndTransactionCalls);
            AssertNoPayload(events, command);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(command);
        }
    }

    [Fact]
    public async Task OtpAbandonedExchange_LogsMetadataAndResetFailureWithoutFormattingPayload()
    {
        const string marker = "ISC56-OTP-PIN-key-payload";
        byte[] command = Encoding.ASCII.GetBytes(marker);
        byte[] response = Encoding.ASCII.GetBytes("ISC56-R\0");
        var connection = new FailingOtpConnection(response, $"native reset failed: {marker}");
        using var provider = new RecordingProvider();
        using var factory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Trace).AddProvider(provider));
        // OtpHidProtocol accepts a logger directly; do not rely on changing a static logger after initialization.
        using var protocol = new OtpHidProtocol(connection, factory.CreateLogger<OtpHidProtocol>());

        try
        {
            IOException failure = await Assert.ThrowsAsync<IOException>(() => protocol.SendAndReceiveAsync(0x13, command, Ct));
            Assert.Equal("native response failed", failure.Message);

            string[] events = provider.Events;
            Assert.NotEmpty(events);
            Assert.True(connection.FrameSent);
            Assert.True(connection.ResetAttempted);
            Assert.True(connection.ResponseRead);
            Assert.Contains(events, entry => entry.Contains("Sending OTP slot command", StringComparison.Ordinal));
            Assert.Contains(events, entry => entry.Contains("Unable to reset OTP HID state", StringComparison.Ordinal));
            Assert.Contains(events, entry => entry == $"ExceptionType={typeof(IOException).FullName}");
            AssertNoPayload(events, command);
            AssertNoPayload(events, response);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(command);
            CryptographicOperations.ZeroMemory(response);
        }
    }

    [Fact]
    public async Task FidoFailedCancel_LogsOperationAndFailureTypeWithoutTransportExceptionPayload()
    {
        byte[] secret = Encoding.ASCII.GetBytes("ISC56-FIDO-PIN-key-payload");
        using var provider = new RecordingProvider();
        using var factory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Trace).AddProvider(provider));
        var connection = new FailingFidoConnection($"native cancel failed: {Encoding.ASCII.GetString(secret)}");
        using var protocol = new FidoHidProtocol(connection, factory.CreateLogger<FidoHidProtocol>());
        try
        {
            InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(() => protocol.SendVendorCommandAsync(
                CtapConstants.CtapVendorFirst, secret, UserPresenceNotification.Create(
                    new FailingPrompt(), new UserPresenceContext { Basis = UserPresenceBasis.PolicyRequires, Application = "FIDO2", Scope = "example.com" }), Ct));
            Assert.Equal("prompt failed", failure.Message);

            string[] events = provider.Events;
            Assert.NotEmpty(events);
            Assert.True(connection.CancelAttempted);
            Assert.Contains(events, entry => entry.Contains("Unable to send CTAPHID_CANCEL", StringComparison.Ordinal));
            Assert.Contains(events, entry => entry == $"ExceptionType={typeof(IOException).FullName}");
            AssertNoPayload(events, secret);
        }
        finally { CryptographicOperations.ZeroMemory(secret); }
    }

    [Fact]
    public async Task FidoFailedTerminalValidation_LogsCallbackTypeWithoutCallbackExceptionPayload()
    {
        byte[] secret = Encoding.ASCII.GetBytes("ISC56-callback-PIN-key-payload");
        using var provider = new RecordingProvider();
        using var factory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Trace).AddProvider(provider));
        var connection = new FailingFidoConnection(null, invalidTerminal: true);
        using var protocol = new FidoHidProtocol(connection, factory.CreateLogger<FidoHidProtocol>());
        try
        {
            InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(() => protocol.SendVendorCommandAsync(
                CtapConstants.CtapVendorFirst, secret, UserPresenceNotification.Create(
                    new FailingPrompt(Encoding.ASCII.GetString(secret)),
                    new UserPresenceContext { Basis = UserPresenceBasis.PolicyRequires, Application = "FIDO2", Scope = "example.com" }), Ct));
            Assert.Contains("does not match request command", failure.Message, StringComparison.Ordinal);

            string[] events = provider.Events;
            Assert.NotEmpty(events);
            Assert.Contains(events, entry => entry.Contains("terminal response validation also failed", StringComparison.Ordinal));
            Assert.Contains(events, entry => entry == $"ExceptionType={typeof(InvalidOperationException).FullName}");
            AssertNoPayload(events, secret);
        }
        finally { CryptographicOperations.ZeroMemory(secret); }
    }

    [Fact]
    public async Task PcscProtocol_RealLoggerEmitsMetadataWithoutCommandOrResponsePayload()
    {
        byte[] command = Encoding.ASCII.GetBytes("ISC56-protocol-command-secret");
        byte[] response = Encoding.ASCII.GetBytes("ISC56-protocol-response-secret");
        using var provider = new RecordingProvider();
        using var factory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Trace).AddProvider(provider));
        try
        {
            var connection = new FakeSmartCardConnection();
            byte[] framedResponse = [.. response, 0x90, 0x00];
            connection.EnqueueResponse(framedResponse);
            using var protocol = new PcscProtocol(connection, logger: factory.CreateLogger<PcscProtocol>());
            _ = await protocol.TransmitAndReceiveAsync(new ApduCommand(0, 0xA4, 0, 0, command), cancellationToken: Ct);
            string[] events = provider.Events;
            Assert.NotEmpty(events);
            Assert.Contains(events, entry => entry.Contains("Transmitting APDU:", StringComparison.Ordinal));
            Assert.Contains(events, entry => entry.Contains("Cla=0", StringComparison.Ordinal));
            Assert.Contains(events, entry => entry.Contains($"Length={command.Length}", StringComparison.Ordinal));
            AssertNoPayload(events, command);
            AssertNoPayload(events, response);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(command);
            CryptographicOperations.ZeroMemory(response);
        }
    }

    [Fact]
    public async Task PcscSelect_LogsApplicationIdentifierWithoutResponsePayload()
    {
        ReadOnlyMemory<byte> applicationId = ApplicationIds.Management;
        byte[] response = Encoding.ASCII.GetBytes("ISC56-select-response-secret");
        using var provider = new RecordingProvider();
        using var factory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Trace).AddProvider(provider));
        try
        {
            var connection = new FakeSmartCardConnection();
            byte[] framedResponse = [.. response, 0x90, 0x00];
            connection.EnqueueResponse(framedResponse);
            using var protocol = new PcscProtocol(connection, logger: factory.CreateLogger<PcscProtocol>());
            _ = await protocol.SelectAsync(applicationId, Ct);

            string[] events = provider.Events;
            Assert.NotEmpty(events);
            Assert.Contains(events, entry => entry == $"ApplicationId={Convert.ToHexString(applicationId.Span)}");
            AssertNoPayload(events, response);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(response);
        }
    }

    private static void AssertNoPayload(string[] events, byte[] secret)
    {
        Assert.DoesNotContain(events, entry => entry.Contains(Encoding.ASCII.GetString(secret), StringComparison.Ordinal));
        Assert.DoesNotContain(events, entry => entry.Contains(Convert.ToHexString(secret), StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(events, entry => entry.Contains(Convert.ToBase64String(secret), StringComparison.Ordinal));
        // Report framing can split the marker; its prefix must not appear either.
        var prefix = secret.AsSpan(0, Math.Min(5, secret.Length));
        var text = Encoding.ASCII.GetString(prefix);
        var hex = Convert.ToHexString(prefix);
        Assert.DoesNotContain(events, entry => entry.Contains(text, StringComparison.Ordinal));
        Assert.DoesNotContain(events, entry => entry.Contains(hex, StringComparison.OrdinalIgnoreCase));
    }

    private sealed class FailingOtpConnection(byte[] response, string resetError) : IOtpHidConnection
    {
        private int _reads;
        private int _writes;
        public ConnectionType Type => ConnectionType.HidOtp;
        public int FeatureReportSize => 8;
        public bool FrameSent { get; private set; }
        public bool ResetAttempted { get; private set; }
        public bool ResponseRead { get; private set; }

        public Task SendAsync(ReadOnlyMemory<byte> report, CancellationToken cancellationToken = default)
        {
            if (report.Span[7] == OtpConstants.DummyReportWrite)
            {
                ResetAttempted = true;
                // A native exception's message is external and may contain command data.
                throw new IOException(resetError);
            }
            if (++_writes == 10)
                FrameSent = true;
            return Task.CompletedTask;
        }

        public Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default)
        {
            int read = ++_reads;
            if (read == 1)
                return Task.FromResult<ReadOnlyMemory<byte>>(new byte[] { 0, 5, 4, 3, 0, 0, 0, 0 });
            if (FrameSent && ResponseRead)
                throw new IOException("native response failed");
            // Initial status and ready-to-write status; report body is opaque to diagnostics.
            ResponseRead = true;
            return Task.FromResult<ReadOnlyMemory<byte>>(response);
        }

        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class FailingPrompt(string? message = null) : IUserPresencePrompt
    {
        public ValueTask OnUserPresenceRequestedAsync(UserPresenceContext context, CancellationToken cancellationToken) =>
            ValueTask.FromException(new InvalidOperationException(message ?? "prompt failed"));

        public ValueTask OnUserPresenceResolvedAsync(UserPresenceContext context, UserPresenceOutcome outcome,
            CancellationToken cancellationToken) => ValueTask.CompletedTask;
    }

    private sealed class FailingFidoConnection(string? cancelError, bool invalidTerminal = false) : IFidoHidConnection
    {
        private byte[]? _nonce;
        private int _reads;
        public ConnectionType Type => ConnectionType.HidFido;
        public int PacketSize => CtapConstants.PacketSize;
        public bool CancelAttempted { get; private set; }

        public Task SendAsync(ReadOnlyMemory<byte> packet, CancellationToken cancellationToken = default)
        {
            byte command = (byte)(packet.Span[4] & ~CtapConstants.InitPacketMask);
            if (command == CtapConstants.CtapHidInit)
                _nonce = packet.Span.Slice(CtapConstants.InitHeaderSize, CtapConstants.NonceSize).ToArray();
            if (command == CtapConstants.CtapHidCancel)
            {
                CancelAttempted = true;
                if (cancelError is not null)
                    throw new IOException(cancelError);
            }
            return Task.CompletedTask;
        }

        public Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default)
        {
            byte[] packet = new byte[CtapConstants.PacketSize];
            BinaryPrimitives.WriteUInt32BigEndian(packet, _reads == 0 ? CtapConstants.BroadcastChannelId : 0x01020304);
            packet[4] = (byte)((_reads == 0 ? CtapConstants.CtapHidInit :
                _reads == 1 ? CtapConstants.CtapHidKeepAlive :
                _reads == 2 && invalidTerminal ? CtapConstants.CtapHidInit : CtapConstants.CtapVendorFirst) |
                CtapConstants.InitPacketMask);
            byte[] payload = _reads switch
            {
                0 => [.. _nonce ?? [], 0x01, 0x02, 0x03, 0x04, 0x02, 0x05, 0x08, 0x00, 0x00],
                1 => [CtapConstants.KeepAliveStatusUpNeeded],
                _ => [0x2D]
            };
            packet[5] = (byte)(payload.Length >> 8);
            packet[6] = (byte)payload.Length;
            payload.AsSpan().CopyTo(packet.AsSpan(CtapConstants.InitHeaderSize));
            _reads++;
            return Task.FromResult<ReadOnlyMemory<byte>>(packet);
        }

        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class RecordingProvider : ILoggerProvider
    {
        private readonly ConcurrentQueue<string> _events = new();
        public string[] Events => [.. _events];
        public ILogger CreateLogger(string categoryName) => new RecordingLogger(_events);
        public void Dispose() { }

        private sealed class RecordingLogger(ConcurrentQueue<string> events) : ILogger
        {
            public bool IsEnabled(LogLevel logLevel) => true;
            public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                events.Enqueue(formatter(state, exception));
                if (state is IEnumerable<KeyValuePair<string, object?>> values)
                {
                    foreach (var value in values)
                        events.Enqueue($"{value.Key}={value.Value}");
                }
                if (exception is not null)
                    events.Enqueue(exception.ToString());
            }
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }
}
