using System.Reflection;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Transports.Hid;

namespace Yubico.YubiKit.PublicApi.UnitTests;

public sealed class HidConnectionCompatibilityTests
{
    [Fact]
    public async Task ExternalConnection_WorksThroughFidoRawAdapter()
    {
        var consumer = new ConsumerConnection(64, ConnectionType.HidFido);
        IFidoHidConnection raw = Assert.IsAssignableFrom<IFidoHidConnection>(CreateAdapter(
            "Yubico.YubiKit.Core.Protocols.Fido.Hid.FidoHidConnection", consumer));
        Assert.Equal(ConnectionType.HidFido, raw.Type);
        Assert.Equal(64, raw.PacketSize);

        await ExerciseReportsAsync(consumer, raw.SendAsync, raw.ReceiveAsync);
        await raw.DisposeAsync();
        Assert.Equal(1, consumer.DisposeCount);
    }

    [Fact]
    public async Task ExternalConnection_WorksThroughOtpRawAdapter()
    {
        var consumer = new ConsumerConnection(8, ConnectionType.HidOtp);
        IOtpHidConnection raw = Assert.IsAssignableFrom<IOtpHidConnection>(CreateAdapter(
            "Yubico.YubiKit.Core.Protocols.Otp.Hid.OtpHidConnection", consumer));
        Assert.Equal(ConnectionType.HidOtp, raw.Type);
        Assert.Equal(8, raw.FeatureReportSize);

        await ExerciseReportsAsync(consumer, raw.SendAsync, raw.ReceiveAsync);
        raw.Dispose();
        Assert.Equal(1, consumer.DisposeCount);
    }

    private static object CreateAdapter(string name, IHidConnection connection)
    {
        // Only construction uses reflection: these real SDK adapters are internal.
        Type adapter = typeof(IHidConnection).Assembly.GetType(name)
            ?? throw new InvalidOperationException($"Missing adapter {name}");
        return Activator.CreateInstance(adapter, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null, args: [connection], culture: null)
            ?? throw new InvalidOperationException($"Cannot construct adapter {name}");
    }

    private static async Task ExerciseReportsAsync(
        ConsumerConnection consumer,
        Func<ReadOnlyMemory<byte>, CancellationToken, Task> send,
        Func<CancellationToken, Task<ReadOnlyMemory<byte>>> receive)
    {
        byte[] callerInput = new byte[consumer.OutputReportSize];
        callerInput[0] = 0x5a;
        await send(callerInput, CancellationToken.None);
        Assert.Equal(1, consumer.SendCount);
        byte[] sdkCopy = Assert.IsType<byte[]>(consumer.CapturedReport);
        Assert.NotSame(callerInput, sdkCopy);
        Assert.All(sdkCopy, value => Assert.Equal(0, value));
        Assert.Equal(0x5a, callerInput[0]);

        ReadOnlyMemory<byte> response = await receive(CancellationToken.None);
        Assert.Equal(1, consumer.ReceiveCount);
        Assert.Equal(consumer.InputReportSize, response.Length);
        Assert.Equal(0x37, response.Span[0]);
    }

    private sealed class ConsumerConnection(int size, ConnectionType type) : IHidConnection
    {
        private readonly byte[] _response = CreateResponse(size);
        public int InputReportSize => size;
        public int OutputReportSize => size;
        public ConnectionType Type => type;
        public byte[]? CapturedReport { get; private set; }
        public int SendCount { get; private set; }
        public int ReceiveCount { get; private set; }
        public int DisposeCount { get; private set; }

        private static byte[] CreateResponse(int size)
        {
            byte[] response = new byte[size];
            response[0] = 0x37;
            return response;
        }

        public void SetReport(byte[] report)
        {
            SendCount++;
            CapturedReport = report;
        }

        public byte[] GetReport()
        {
            ReceiveCount++;
            return _response;
        }

        public void Dispose() => DisposeCount++;
        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}