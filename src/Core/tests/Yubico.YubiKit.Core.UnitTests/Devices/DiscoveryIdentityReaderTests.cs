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

using Microsoft.Extensions.Logging.Abstractions;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;

namespace Yubico.YubiKit.Core.UnitTests.Devices;

[Collection(DiscoveryWorkerAdmissionCollection.Name)]
public class DiscoveryIdentityReaderTests
{
    /// <summary>
    ///     A discovery identity read against a device whose connection open never completes (modeling a card
    ///     busy inside a long native operation, e.g. an in-flight RSA-4096 GENERATE ASYMMETRIC APDU that a
    ///     CancellationToken cannot abort) must give up within a bounded time and degrade to
    ///     <c>null</c> ("serial unknown"), instead of stalling the discovery scan indefinitely while it holds
    ///     the scan lock.
    /// </summary>
    [Fact]
    public async Task TryReadAsync_ConnectNeverCompletes_ReturnsNullWithinBoundedTime()
    {
        var device = new HangingConnectYubiKey();

        try
        {
            var readTask = DiscoveryIdentityReader.TryReadAsync(
                device,
                ConnectionType.SmartCard,
                NullLogger.Instance,
                TestContext.Current.CancellationToken);

            // Generous bound: 3 attempts x per-attempt timeout + retry backoff must all fit well inside it.
            var winner = await Task.WhenAny(
                readTask,
                Task.Delay(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));

            Assert.True(readTask == winner,
                "DiscoveryIdentityReader.TryReadAsync did not complete within 10s when the connection open " +
                "hung; a busy card stalls the whole discovery scan (holds FindYubiKeys._scanLock).");
            Assert.Null(await readTask);
        }
        finally
        {
            device.FailConnect();
            await device.ConnectFinished.Task.WaitAsync(TestContext.Current.CancellationToken);
            await DiscoveryWorkerAdmissionCollection.WaitUntilIdleAsync(TestContext.Current.CancellationToken);
        }
    }

    /// <summary>
    ///     Deliberately ignores the <see cref="CancellationToken" />: the production hang is
    ///     <c>Task.Run(() => SCardTransmit(...))</c> / a blocking native connect, which cannot observe
    ///     cancellation once in flight. A fix that only signals a token into ConnectAsync would not fix the
    ///     real bug; the reader must abandon the wait (e.g. <c>Task.WaitAsync</c>) to stay bounded.
    /// </summary>
    private sealed class HangingConnectYubiKey : IYubiKey, IDiscoveryConnectionProvider
    {
        private readonly TaskCompletionSource<IConnection> _connect =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public string DeviceId => "test:hanging-connect";

        public ConnectionType AvailableConnections => ConnectionType.SmartCard;

        public TaskCompletionSource ConnectFinished { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
            where TConnection : class, IConnection
        {
            try
            {
                return (TConnection)await _connect.Task;
            }
            finally
            {
                ConnectFinished.TrySetResult();
            }
        }

        Task<IConnection> IDiscoveryConnectionProvider.ConnectForDiscoveryAsync(
            ConnectionType connection,
            CancellationToken cancellationToken) =>
            ConnectAsync<IConnection>(cancellationToken);

        public void FailConnect() =>
            _connect.TrySetException(new InvalidOperationException("Released after timeout assertion."));
    }
}