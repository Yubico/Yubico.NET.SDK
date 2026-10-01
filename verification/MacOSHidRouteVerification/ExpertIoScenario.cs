using System.Diagnostics;
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Native;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.Hid;

internal static class ExpertIoScenario
{
    internal static async Task<int> RunAsync(int serial)
    {
        try
        {
            IReadOnlyList<IYubiKey> devices = await YubiKeyManager.FindAllAsync();
            IYubiKey[] selected = devices.Where(d => d.SerialNumber == serial &&
                d.SupportsConnection(ConnectionType.HidFido)).ToArray();
            IReadOnlyList<IHidInterface> hidInterfaces = await FindHidInterfaces.Create().FindAllAsync();
            IHidInterface[] fidoInterfaces = hidInterfaces.Where(d => d.InterfaceType == HidInterfaceType.Fido).ToArray();
            if (devices.Count != 1 || selected.Length != 1 || fidoInterfaces.Length != 1)
            {
                Console.Error.WriteLine($"BLOCKED: serial={serial} discovered {devices.Count} keys, matched {selected.Length} keys, {fidoInterfaces.Length} FIDO HID interfaces; cannot uniquely associate interface");
                return 2;
            }

            Console.WriteLine($"SELECTED serial={serial} id={selected[0].DeviceId} interface={fidoInterfaces[0].ReaderName}");
            await using (IHidConnection io = fidoInterfaces[0].ConnectToIOReports())
            {
                if (io.GetType().Name != "MacOSHidIOReportConnection")
                    throw new InvalidOperationException("Expert I/O did not use built-in macOS facade");
                var timer = Stopwatch.StartNew();
                try
                {
                    _ = io.GetReport(); // No command sent: must expire without poisoning this connection.
                    throw new InvalidOperationException("Unsolicited GetReport returned a report");
                }
                catch (PlatformApiException ex) when (ex.Message == "Timed out waiting for HID input report after six seconds.")
                {
                    if (timer.Elapsed < TimeSpan.FromSeconds(6))
                        throw new InvalidOperationException($"GetReport timed out too early: {timer.Elapsed}", ex);
                    Console.WriteLine($"PHASE_EXPERT_TIMEOUT serial={serial} elapsedMs={timer.Elapsed.TotalMilliseconds:F0} exception={ex.GetType().Name}");
                }

                await using RawFidoHidSession raw = await RawFidoHidSession.CreateAsync(new ExpertIoConnection(io));
                await AssertGetInfoAsync(raw);
                Console.WriteLine($"PHASE_EXPERT_SAME_CONNECTION_INFO serial={serial}");
            }

            await using (IHidConnection reopened = fidoInterfaces[0].ConnectToIOReports())
            {
                await using RawFidoHidSession raw = await RawFidoHidSession.CreateAsync(new ExpertIoConnection(reopened));
                await AssertGetInfoAsync(raw);
            }
            Console.WriteLine($"PASS mode=expert-io serial={serial} timeout=1 sameConnectionInfo=1 reopenInfo=1");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"FAIL mode=expert-io serial={serial}: {ex}");
            return 1;
        }
        finally
        {
            await YubiKeyManager.ShutdownAsync();
        }
    }

    private static async Task AssertGetInfoAsync(RawFidoHidSession raw)
    {
        ReadOnlyMemory<byte> response = await raw.SendAndReceiveAsync(0x10, new byte[] { 0x04 });
        if (response.Length < 2 || response.Span[0] != 0)
            throw new InvalidOperationException("CTAPHID_INIT + authenticatorGetInfo returned no successful payload");
    }

    // Verification-only adapter: the production packet adapter is internal, while this mode
    // must exercise IHidInterface.ConnectToIOReports rather than the managed device route.
    private sealed class ExpertIoConnection(IHidConnection io) : IFidoHidConnection
    {
        public ConnectionType Type => ConnectionType.HidFido;
        public int PacketSize => 64;

        public Task SendAsync(ReadOnlyMemory<byte> packet, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (packet.Length != PacketSize)
                throw new ArgumentException("Expected 64-byte FIDO packet", nameof(packet));
            byte[] report = packet.ToArray();
            try { io.SetReport(report); }
            finally { CryptographicOperations.ZeroMemory(report); }
            return Task.CompletedTask;
        }

        public Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            byte[] report = io.GetReport();
            if (report.Length != PacketSize)
                throw new InvalidOperationException($"Expected 64-byte FIDO packet, got {report.Length}");
            return Task.FromResult<ReadOnlyMemory<byte>>(report);
        }

        // The caller owns the IHidConnection, and disposes it after the borrowed raw session.
        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}