// Copyright 2025 Yubico AB
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

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Yubico.Core.Devices.Hid.UnitTests
{
    /// <summary>
    /// Demo test to verify instance ID correlation works correctly.
    /// This demonstrates the pattern we'll use for correlating telemetry logs to specific tests.
    /// </summary>
    public class InstanceIdCorrelationDemo
    {
        private readonly ITestOutputHelper _output;
        private static int _listenerCounter;

        public InstanceIdCorrelationDemo(ITestOutputHelper output)
        {
            _output = output;
        }

        /// <summary>
        /// Simulates how MacOSHidDeviceListener assigns instance IDs
        /// </summary>
        class DemoListener : IDisposable
        {
#pragma warning disable IDE0032
            private readonly string _instanceId;
#pragma warning restore IDE0032
            private readonly ILogger _log;
            private readonly Thread? _backgroundThread;

            public DemoListener(ILogger logger)
            {
                _log = logger;
                _instanceId = $"L{Interlocked.Increment(ref _listenerCounter):D3}";
                _log.LogInformation("[TELEMETRY][{InstanceId}] Constructor called on thread {ThreadId}",
                    _instanceId, Environment.CurrentManagedThreadId);

                // Start background thread
                _backgroundThread = new Thread(() => BackgroundWork())
                {
                    IsBackground = true
                };
                _backgroundThread.Start();
            }

            // CRITICAL: Expose instance ID so tests can correlate logs
            public string InstanceId => _instanceId;

            private void BackgroundWork()
            {
                _log.LogInformation("[TELEMETRY][{InstanceId}] Background thread started on thread {ThreadId}",
                    _instanceId, Environment.CurrentManagedThreadId);
                Thread.Sleep(100);
                _log.LogInformation("[TELEMETRY][{InstanceId}] Background thread exiting", _instanceId);
            }

            public void Dispose()
            {
                _log.LogInformation("[TELEMETRY][{InstanceId}] Dispose() called on thread {ThreadId}",
                    _instanceId, Environment.CurrentManagedThreadId);
                _backgroundThread?.Join();
            }
        }

        [Fact]
        public void SingleTest_CorrelationWorks()
        {
            _output.WriteLine("=== TEST START: SingleTest_CorrelationWorks ===");

            var logger = Logging.Log.GetLogger("Demo");
            var listener = new DemoListener(logger);

            // WITH exposed InstanceId property, tests can log correlation:
            _output.WriteLine($"[TEST] Created listener with InstanceId: {listener.InstanceId}");
            _output.WriteLine($"[TEST] All logs tagged [TELEMETRY][{listener.InstanceId}] belong to this test");

            Thread.Sleep(150);
            listener.Dispose();

            _output.WriteLine("=== TEST END ===");
            _output.WriteLine($"Expected log pattern: [TELEMETRY][{listener.InstanceId}] Constructor/Background/Dispose");
        }

        [Fact]
        public async Task ParallelTests_CorrelationStillWorks()
        {
            _output.WriteLine("=== TEST START: ParallelTests_CorrelationStillWorks ===");

            var logger = Logging.Log.GetLogger("Demo");

            // Create 3 listeners in parallel - each test logs which instance it created
            var tasks = new[]
            {
                Task.Run(() => {
                    var l1 = new DemoListener(logger);
                    _output.WriteLine($"[TEST-Task1] Created listener: {l1.InstanceId}");
                    Thread.Sleep(100);
                    l1.Dispose();
                    _output.WriteLine($"[TEST-Task1] Disposed listener: {l1.InstanceId}");
                }),
                Task.Run(() => {
                    var l2 = new DemoListener(logger);
                    _output.WriteLine($"[TEST-Task2] Created listener: {l2.InstanceId}");
                    Thread.Sleep(100);
                    l2.Dispose();
                    _output.WriteLine($"[TEST-Task2] Disposed listener: {l2.InstanceId}");
                }),
                Task.Run(() => {
                    var l3 = new DemoListener(logger);
                    _output.WriteLine($"[TEST-Task3] Created listener: {l3.InstanceId}");
                    Thread.Sleep(100);
                    l3.Dispose();
                    _output.WriteLine($"[TEST-Task3] Disposed listener: {l3.InstanceId}");
                })
            };

            await Task.WhenAll(tasks);

            _output.WriteLine("=== TEST END ===");
            _output.WriteLine("In parallel test output, search for instance ID (e.g., L001) to see all related logs");
        }
    }
}
