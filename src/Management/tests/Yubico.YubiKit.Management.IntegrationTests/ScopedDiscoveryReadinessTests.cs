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

using Xunit.Abstractions;
using Xunit.Sdk;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Management.IntegrationTests;

/// <summary>Hardware-free regressions of the read-only suite's actual reference-scan readiness loop.</summary>
public class ScopedDiscoveryReadinessTests(ITestOutputHelper output)
{
    private const int AuthorizedSerial = 12345678;
    private const int NonallowlistedSerial = 87654321;

    [Theory]
    [InlineData(null)]
    [InlineData(NonallowlistedSerial)]
    public async Task ReferenceScanAsync_OnlyCompleteNonallowlistedDevice_ReturnsEmptyForCallerPrerequisite(int? serial)
    {
        var bystander = new ScanView("bystander", serial, Metadata(serial));
        var suite = new ScopedDiscoveryReadOnlyTests(output, new AllowList(new FixtureAllowListProvider()));
        var attempts = 0;

        var reference = await suite.ReferenceScanAsync(CancellationToken.None, () =>
        {
            attempts++;
            return Task.FromResult<IReadOnlyList<IYubiKey>>([bystander]);
        });

        Assert.Empty(reference);
        Assert.Equal(1, attempts);
    }

    [Fact]
    public async Task ReferenceScanAsync_MetadataCompleteSeriallessDevice_ReturnsOnlyAuthorizedFixture()
    {
        var fixture = new ScanView("authorized", AuthorizedSerial, Metadata(AuthorizedSerial));
        var securityKey = new ScanView("security-key", null, Metadata(null));
        var suite = new ScopedDiscoveryReadOnlyTests(output, new AllowList(new FixtureAllowListProvider()));
        var attempts = 0;

        var reference = await suite.ReferenceScanAsync(CancellationToken.None, () =>
        {
            attempts++;
            return Task.FromResult<IReadOnlyList<IYubiKey>>([fixture, securityKey]);
        });

        Assert.Same(fixture, Assert.Single(reference));
        Assert.Equal(1, attempts);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(AuthorizedSerial)]
    [InlineData(NonallowlistedSerial)]
    public async Task ReferenceScanAsync_ViewWithoutMetadata_ExhaustsReadinessRetries(int? incompleteSerial)
    {
        var fixture = new ScanView("authorized", AuthorizedSerial, Metadata(AuthorizedSerial));
        var incomplete = new ScanView("incomplete", incompleteSerial, null);
        var suite = new ScopedDiscoveryReadOnlyTests(output, new AllowList(new FixtureAllowListProvider()));
        var attempts = 0;

        var failure = await Assert.ThrowsAsync<FailException>(() => suite.ReferenceScanAsync(CancellationToken.None, () =>
        {
            attempts++;
            return Task.FromResult<IReadOnlyList<IYubiKey>>([fixture, incomplete]);
        }));

        Assert.Equal(3, attempts);
        Assert.Contains("missing expected interfaces after three scans", failure.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(NonallowlistedSerial)]
    public async Task ReferenceScanAsync_OnlyNonallowlistedDeviceWithoutMetadata_FailsBeforeCallerPrerequisite(int? serial)
    {
        var incomplete = new ScanView("incomplete", serial, null);
        var suite = new ScopedDiscoveryReadOnlyTests(output, new AllowList(new FixtureAllowListProvider()));
        var attempts = 0;

        var failure = await Assert.ThrowsAsync<FailException>(() => suite.ReferenceScanAsync(CancellationToken.None, () =>
        {
            attempts++;
            return Task.FromResult<IReadOnlyList<IYubiKey>>([incomplete]);
        }));

        Assert.Equal(3, attempts);
        Assert.Contains("Incomplete metadata", failure.Message);
    }

    private static DeviceInfo Metadata(int? serial) =>
        DeviceInfo.CreateFromTlvs([], new FirmwareVersion(5, 8, 0)) with { SerialNumber = serial };

    private sealed class FixtureAllowListProvider : IAllowListProvider
    {
        public IReadOnlyList<int> GetList() => [AuthorizedSerial];
        public string OnInvalidInputErrorMessage() => "The deterministic fixture allow list must not be empty.";
    }

    private sealed class ScanView(string id, int? serial, DeviceInfo? metadata) : IYubiKey
    {
        public string DeviceId => id;
        public int? SerialNumber => serial;
        public DeviceInfo? CachedDeviceInfo => metadata;
        public ConnectionType AvailableConnections => ConnectionType.HidFido;

        public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
            where TConnection : class, IConnection =>
            throw new InvalidOperationException("Readiness checks must not open a connection.");
    }
}