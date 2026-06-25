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

using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Core.UnitTests.Devices;

/// <summary>
///     Covers <c>ApplicationSession.InitializeCoreAsync</c>'s SCP guard: SCP is only valid on a SmartCard
///     protocol. This is the Core contract that backs the Phase 38 FIDO2 rule (ISC-9.1) — supplying
///     <c>scpKeyParams</c> while a non-SmartCard transport (e.g. the default HID FIDO) is selected throws,
///     rather than silently ignoring the requested secure channel.
/// </summary>
public class ApplicationSessionScpTests
{
    [Fact]
    public async Task InitializeCore_WithScpOnNonSmartCardProtocol_ThrowsNotSupported()
    {
        var session = new TestSession();
        using var protocol = new NonSmartCardProtocol();
        using var scp = Scp03KeyParameters.Default;

        var ex = await Assert.ThrowsAsync<NotSupportedException>(
            () => session.RunInitializeAsync(
                protocol,
                new FirmwareVersion(5, 7, 0),
                scp,
                TestContext.Current.CancellationToken));

        Assert.Contains("SmartCard", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(session.IsInitialized);
        Assert.False(session.IsAuthenticated);
    }

    [Fact]
    public async Task InitializeCore_WithoutScpOnNonSmartCardProtocol_Succeeds()
    {
        var session = new TestSession();
        using var protocol = new NonSmartCardProtocol();

        await session.RunInitializeAsync(
            protocol,
            new FirmwareVersion(5, 7, 0),
            scpKeyParams: null,
            TestContext.Current.CancellationToken);

        Assert.True(session.IsInitialized);
        Assert.False(session.IsAuthenticated);
    }

    private sealed class TestSession : ApplicationSession
    {
        public Task RunInitializeAsync(
            IProtocol protocol,
            FirmwareVersion firmwareVersion,
            ScpKeyParameters? scpKeyParams,
            CancellationToken cancellationToken) =>
            InitializeCoreAsync(protocol, firmwareVersion, configuration: null, scpKeyParams, cancellationToken);
    }

    // A protocol that is deliberately NOT an ISmartCardProtocol (mirrors a HID FIDO/OTP protocol for the
    // purposes of the SCP guard).
    private sealed class NonSmartCardProtocol : IProtocol
    {
        public void Configure(FirmwareVersion version, ProtocolConfiguration? configuration = null)
        {
        }

        public void Dispose()
        {
        }
    }
}