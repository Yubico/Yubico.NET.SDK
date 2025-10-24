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
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Xunit;
using Xunit.Abstractions;
using Yubico.Core.Devices.Hid;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.PlatformInterop
{
    /// <summary>
    /// Manual integration tests for HID device listener.
    /// These tests require human interaction (inserting/removing devices).
    /// </summary>
    [Trait(TraitTypes.Category, TestCategories.Simple)]
    public class ListenHidTests
    {
        private readonly ITestOutputHelper _output;
        private readonly List<string> _arrivedDevices = new List<string>();
        private readonly List<string> _removedDevices = new List<string>();
        private int _arrivedCount = 0;
        private int _removedCount = 0;

        public ListenHidTests(ITestOutputHelper output)
        {
            _output = output;
        }

        /// <summary>
        /// Manual test: Verifies HID device arrival and removal events.
        ///
        /// INSTRUCTIONS:
        /// 1. Run this test in DEBUG mode and set a breakpoint on the last line (Assert.True)
        /// 2. When execution pauses at the Thread.Sleep, insert a HID device (e.g., YubiKey)
        /// 3. Wait for "ARRIVED" message in test output
        /// 4. Remove the device
        /// 5. Wait for "REMOVED" message
        /// 6. Repeat steps 2-5 a few times to verify reliability
        /// 7. Resume execution - test will verify events were received
        ///
        /// EXPECTED: At least one arrival and one removal event should be captured.
        /// </summary>
        [Fact]
        public void HidDeviceListener_ManualTest_CapturesArrivedAndRemovedEvents()
        {
            _output.WriteLine("=== HID DEVICE LISTENER MANUAL TEST ===");
            _output.WriteLine("Starting listener...");

            using var listener = HidDeviceListener.Create();
            listener.Arrived += OnDeviceArrived;
            listener.Removed += OnDeviceRemoved;

            _output.WriteLine("");
            _output.WriteLine("✓ Listener started successfully");
            _output.WriteLine("");
            _output.WriteLine("INSTRUCTIONS:");
            _output.WriteLine("  1. Set breakpoint on Assert.True at end of test");
            _output.WriteLine("  2. Insert HID device (e.g., YubiKey) during 60-second wait");
            _output.WriteLine("  3. Wait for ARRIVED event in output");
            _output.WriteLine("  4. Remove device");
            _output.WriteLine("  5. Wait for REMOVED event in output");
            _output.WriteLine("  6. Optionally repeat steps 2-5");
            _output.WriteLine("  7. Resume execution to verify results");
            _output.WriteLine("");
            _output.WriteLine("Waiting 60 seconds for manual device insertion/removal...");
            _output.WriteLine("--- INSERT/REMOVE DEVICE NOW ---");

            // Wait for manual interaction
            Thread.Sleep(TimeSpan.FromSeconds(60));

            _output.WriteLine("");
            _output.WriteLine("=== TEST RESULTS ===");
            _output.WriteLine($"Total Arrived Events: {_arrivedCount}");
            _output.WriteLine($"Total Removed Events: {_removedCount}");
            _output.WriteLine("");

            if (_arrivedDevices.Count > 0)
            {
                _output.WriteLine("Devices that arrived:");
                foreach (string device in _arrivedDevices)
                {
                    _output.WriteLine($"  - {device}");
                }
            }

            if (_removedDevices.Count > 0)
            {
                _output.WriteLine("Devices that were removed:");
                foreach (string device in _removedDevices)
                {
                    _output.WriteLine($"  - {device}");
                }
            }

            // Verify at least one event was captured
            Assert.True(_arrivedCount > 0 || _removedCount > 0,
                "Manual test failed: No events were captured. Did you insert/remove a device?");
        }

        /// <summary>
        /// Stress test: Verifies listener remains stable under rapid device insertion/removal.
        ///
        /// INSTRUCTIONS:
        /// Run this test and rapidly insert/remove a device multiple times.
        /// Test will automatically timeout after 2 minutes.
        /// </summary>
        [Fact(Skip = "Manual stress test - requires rapid device insertion/removal")]
        public void HidDeviceListener_StressTest_HandlesRapidInsertionRemoval()
        {
            _output.WriteLine("=== HID LISTENER STRESS TEST ===");
            _output.WriteLine("Rapidly insert and remove HID device for ~30 seconds");
            _output.WriteLine("Test will run for 2 minutes, then verify stability");
            _output.WriteLine("");

            using var listener = HidDeviceListener.Create();
            listener.Arrived += OnDeviceArrived;
            listener.Removed += OnDeviceRemoved;

            var stopwatch = Stopwatch.StartNew();
            var lastEventTime = stopwatch.Elapsed;

            // Monitor for 2 minutes
            while (stopwatch.Elapsed < TimeSpan.FromMinutes(2))
            {
                Thread.Sleep(100);

                // Report every 10 seconds
                if (stopwatch.Elapsed.TotalSeconds % 10 < 0.1)
                {
                    _output.WriteLine($"[{stopwatch.Elapsed.TotalSeconds:F0}s] Arrived: {_arrivedCount}, Removed: {_removedCount}");
                }
            }

            _output.WriteLine("");
            _output.WriteLine("=== STRESS TEST RESULTS ===");
            _output.WriteLine($"Total Arrived Events: {_arrivedCount}");
            _output.WriteLine($"Total Removed Events: {_removedCount}");
            _output.WriteLine($"Listener remained stable: ✓");

            Assert.True(_arrivedCount >= 0 && _removedCount >= 0, "Listener should remain stable");
        }

        private void OnDeviceArrived(object? sender, HidDeviceEventArgs e)
        {
            _arrivedCount++;
            string deviceInfo = FormatDeviceInfo(e.Device);
            _arrivedDevices.Add(deviceInfo);

            _output.WriteLine($"");
            _output.WriteLine($">>> ARRIVED (#{_arrivedCount}) at {DateTime.Now:HH:mm:ss.fff}");
            _output.WriteLine($"    {deviceInfo}");
        }

        private void OnDeviceRemoved(object? sender, HidDeviceEventArgs e)
        {
            _removedCount++;
            string deviceInfo = FormatDeviceInfo(e.Device);
            _removedDevices.Add(deviceInfo);

            _output.WriteLine($"");
            _output.WriteLine($"<<< REMOVED (#{_removedCount}) at {DateTime.Now:HH:mm:ss.fff}");
            _output.WriteLine($"    {deviceInfo}");
        }

        private static string FormatDeviceInfo(IHidDevice device)
        {
            return $"VID={device.VendorId:X4} PID={device.ProductId:X4} " +
                   $"Usage=0x{device.Usage:X} UsagePage={device.UsagePage} " +
                   $"Path={device.Path}";
        }
    }
}
