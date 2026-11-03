// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Xunit.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Oath.IntegrationTests;

/// <summary>
///     Diagnostic INITIALIZE UPDATE acceptance only: never establishes or verifies a secure channel.
/// </summary>
public sealed class OathScpReferenceProbeTests(ITestOutputHelper output)
{
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, CustomFilter = typeof(Serial125Only))]
    [Trait(TestCategories.Category, TestCategories.RequiresHardware)]
    public async Task InitializeUpdate_ExplicitFfAndZero_ReportsAcceptanceOnly(YubiKeyTestState state)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        // Binding and authorization remain entirely in the shared WithYubiKey harness.
        Assert.Equal(125, state.SerialNumber);
        output.WriteLine($"Device serial={state.SerialNumber}, firmware={state.FirmwareVersion}");
        output.WriteLine("Diagnostic only: no EXTERNAL AUTHENTICATE, key loading, applet reset, or handshake verification.");

        var observations = new List<(byte RequestedKvn, ushort Status, int Length)>();
        foreach (byte keyVersion in new byte[] { 0xFF, 0x00 })
        {
            // Each iteration owns a fresh connection, and the raw session only borrows it.
            var connection = await state.Device.ConnectAsync<ISmartCardConnection>(timeout.Token);
            try
            {
                var raw = await RawSmartCardSession.CreateAsync(connection, timeout.Token);
                try
                {
                    var select = new ApduCommand(0x00, 0xA4, 0x04, 0x00, ApplicationIds.Oath);
                    var selected = await raw.TransmitAndReceiveAsync(select, throwOnError: false, timeout.Token);
                    try
                    {
                        output.WriteLine($"SELECT header=00 A4 04 00; SW={(ushort)selected.SW:X4}; dataLength={selected.Data.Length}");
                        Assert.True(selected.IsOK(), "OATH SELECT failed; do not expand this probe to other applet contexts.");
                    }
                    finally
                    {
                        CryptographicOperations.ZeroMemory(MemoryMarshal.AsMemory(selected.RawData).Span);
                    }

                    byte[] challenge = new byte[8];
                    try
                    {
                        RandomNumberGenerator.Fill(challenge);
                        // Exactly one INITIALIZE UPDATE call per connection; no retries or authentication.
                        var command = new ApduCommand(0x80, 0x50, keyVersion, 0x00, challenge);
                        var response = await raw.TransmitAndReceiveAsync(command, throwOnError: false, timeout.Token);
                        try
                        {
                            output.WriteLine($"INITIALIZE UPDATE header=80 50 {keyVersion:X2} 00; requestedKvn={keyVersion:X2}; SW={(ushort)response.SW:X4}; dataLength={response.Data.Length}");
                            if (response.IsOK() && response.Data.Length >= 12)
                            {
                                output.WriteLine($"Selected metadata: KVN={response.Data.Span[10]:X2}; protocolId={response.Data.Span[11]:X2}");
                            }
                            observations.Add((keyVersion, (ushort)response.SW, response.Data.Length));
                        }
                        finally
                        {
                            CryptographicOperations.ZeroMemory(MemoryMarshal.AsMemory(response.RawData).Span);
                        }
                    }
                    finally
                    {
                        CryptographicOperations.ZeroMemory(challenge);
                    }
                }
                finally
                {
                    // Cleanup has an independent finite wait budget even if the operation token expired.
                    // All exchanges above are awaited before disposing the borrowed session.
                    await raw.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(15), CancellationToken.None);
                }
            }
            finally
            {
                // Ordinary connection release only; never send an applet RESET command.
                await connection.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(15), CancellationToken.None);
            }
        }

        // Rejections remain test failures, not an unconditional diagnostic pass. Both requested values
        // are observed before checking acceptance; even a pass proves no cryptogram or handshake.
        Assert.Equal(2, observations.Count);
        Assert.All(observations, observation =>
        {
            Assert.True(observation.Status == 0x9000,
                $"INITIALIZE UPDATE KVN={observation.RequestedKvn:X2} rejected: SW={observation.Status:X4}, dataLength={observation.Length}; secure-channel establishment was not attempted.");
            Assert.True(observation.Length >= 29, "INITIALIZE UPDATE success response is shorter than the SCP03 structure.");
        });
    }

    public sealed class Serial125Only : IYubiKeyFilter
    {
        public bool Matches(YubiKeyTestState device) => device.SerialNumber == 125;
        public string GetDescription() => "Exclusive diagnostic device serial 125 only";
    }
}
