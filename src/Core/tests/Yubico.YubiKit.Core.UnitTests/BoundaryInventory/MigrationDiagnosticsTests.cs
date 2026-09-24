// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Native.Desktop.SCard;
using Yubico.YubiKit.Core.Protocols.Otp.Hid;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
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
            "Transmitting APDU: {CommandApdu}",
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
        var connection = new FailingOtpConnection(response);
        using var provider = new RecordingProvider();
        using var factory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Trace).AddProvider(provider));
        // OtpHidProtocol accepts a logger directly; do not rely on changing a static logger after initialization.
        using var protocol = new OtpHidProtocol(connection, factory.CreateLogger<OtpHidProtocol>());

        try
        {
            _ = await Assert.ThrowsAsync<IOException>(() => protocol.SendAndReceiveAsync(0x13, command, Ct));

            string[] events = provider.Events;
            Assert.True(connection.FrameSent);
            Assert.True(connection.ResetAttempted);
            Assert.True(connection.ResponseRead);
            Assert.Contains(events, entry => entry.Contains("Sending OTP slot command", StringComparison.Ordinal));
            Assert.Contains(events, entry => entry.Contains("Unable to reset OTP HID state", StringComparison.Ordinal));
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
            Assert.Contains(events, entry => entry.Contains("Transmitting APDU:", StringComparison.Ordinal));
            Assert.Contains(events, entry => entry.Contains("CommandApdu=CLA:", StringComparison.Ordinal));
            AssertNoPayload(events, command);
            AssertNoPayload(events, response);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(command);
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

    private sealed class FailingOtpConnection(byte[] response) : IOtpHidConnection
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
                // The SDK formats the exception via the logger; a native exception's own message is external.
                throw new IOException("native reset failed");
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