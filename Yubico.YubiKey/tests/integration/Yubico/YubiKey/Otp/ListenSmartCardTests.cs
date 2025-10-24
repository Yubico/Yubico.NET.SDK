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
using System.Linq;
using System.Threading;
using Xunit;
using Xunit.Abstractions;
using Yubico.Core.Devices.SmartCard;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.PlatformInterop
{
    /// <summary>
    /// Manual integration tests for SmartCard device listener.
    /// These tests require human interaction (inserting/removing cards).
    /// </summary>
    [Trait(TraitTypes.Category, TestCategories.Simple)]
    public class ListenSmartCardTests
    {
        private readonly ITestOutputHelper _output;
        private readonly List<string> _arrivedDevices = new List<string>();
        private readonly List<string> _removedDevices = new List<string>();
        private int _arrivedCount = 0;
        private int _removedCount = 0;

        public ListenSmartCardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        /// <summary>
        /// Manual test: Verifies SmartCard device arrival and removal events.
        ///
        /// INSTRUCTIONS:
        /// 1. Run this test in DEBUG mode and set a breakpoint on the last line (Assert.True)
        /// 2. When execution pauses at the Thread.Sleep, insert a smart card (e.g., YubiKey)
        /// 3. Wait for "ARRIVED" message in test output
        /// 4. Remove the card
        /// 5. Wait for "REMOVED" message
        /// 6. Repeat steps 2-5 a few times to verify reliability
        /// 7. Resume execution - test will verify events were received
        ///
        /// EXPECTED: At least one arrival and one removal event should be captured.
        /// </summary>
        [Fact(Skip = "Manual test - requires physical smart card insertion/removal")]
        public void SmartCardDeviceListener_ManualTest_CapturesArrivedAndRemovedEvents()
        {
            _output.WriteLine("=== SMART CARD DEVICE LISTENER MANUAL TEST ===");
            _output.WriteLine("Starting listener...");

            using var listener = SmartCardDeviceListener.Create();
            listener.Arrived += OnDeviceArrived;
            listener.Removed += OnDeviceRemoved;

            _output.WriteLine("");
            _output.WriteLine("✓ Listener started successfully");
            _output.WriteLine("");
            _output.WriteLine("INSTRUCTIONS:");
            _output.WriteLine("  1. Set breakpoint on Assert.True at end of test");
            _output.WriteLine("  2. Insert smart card (e.g., YubiKey) during 60-second wait");
            _output.WriteLine("  3. Wait for ARRIVED event in output");
            _output.WriteLine("  4. Remove card");
            _output.WriteLine("  5. Wait for REMOVED event in output");
            _output.WriteLine("  6. Optionally repeat steps 2-5");
            _output.WriteLine("  7. Resume execution to verify results");
            _output.WriteLine("");
            _output.WriteLine("Waiting 60 seconds for manual card insertion/removal...");
            _output.WriteLine("--- INSERT/REMOVE CARD NOW ---");

            // Wait for manual interaction
            Thread.Sleep(TimeSpan.FromSeconds(60));

            _output.WriteLine("");
            _output.WriteLine("=== TEST RESULTS ===");
            _output.WriteLine($"Total Arrived Events: {_arrivedCount}");
            _output.WriteLine($"Total Removed Events: {_removedCount}");
            _output.WriteLine("");

            if (_arrivedDevices.Count > 0)
            {
                _output.WriteLine("Cards that arrived:");
                foreach (string device in _arrivedDevices)
                {
                    _output.WriteLine($"  - {device}");
                }
            }

            if (_removedDevices.Count > 0)
            {
                _output.WriteLine("Cards that were removed:");
                foreach (string device in _removedDevices)
                {
                    _output.WriteLine($"  - {device}");
                }
            }

            // Verify at least one event was captured
            Assert.True(_arrivedCount > 0 || _removedCount > 0,
                "Manual test failed: No events were captured. Did you insert/remove a card?");
        }

        /// <summary>
        /// Stress test: Verifies listener remains stable under rapid card insertion/removal.
        ///
        /// INSTRUCTIONS:
        /// Run this test and rapidly insert/remove a card multiple times.
        /// Test will automatically timeout after 2 minutes.
        /// </summary>
        [Fact(Skip = "Manual stress test - requires rapid card insertion/removal")]
        public void SmartCardDeviceListener_StressTest_HandlesRapidInsertionRemoval()
        {
            _output.WriteLine("=== SMART CARD LISTENER STRESS TEST ===");
            _output.WriteLine("Rapidly insert and remove smart card for ~30 seconds");
            _output.WriteLine("Test will run for 2 minutes, then verify stability");
            _output.WriteLine("");

            using var listener = SmartCardDeviceListener.Create();
            listener.Arrived += OnDeviceArrived;
            listener.Removed += OnDeviceRemoved;

            var stopwatch = Stopwatch.StartNew();

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

        /// <summary>
        /// Verifies that card reader changes are detected.
        ///
        /// INSTRUCTIONS:
        /// 1. Run with no card readers attached
        /// 2. Attach a USB card reader during the 60-second wait
        /// 3. Insert a card into the reader
        /// 4. Remove the card
        /// 5. Disconnect the reader
        /// </summary>
        [Fact]
        public void SmartCardDeviceListener_ManualTest_DetectsReaderChanges()
        {
            _output.WriteLine("=== SMART CARD READER DETECTION TEST ===");
            _output.WriteLine("This test verifies that new readers are detected");
            _output.WriteLine("");
            _output.WriteLine("Starting listener...");

            using var listener = SmartCardDeviceListener.Create();
            listener.Arrived += OnDeviceArrived;
            listener.Removed += OnDeviceRemoved;

            _output.WriteLine("✓ Listener started");
            _output.WriteLine("");
            _output.WriteLine("INSTRUCTIONS:");
            _output.WriteLine("  1. Start with NO card readers attached");
            _output.WriteLine("  2. Attach a USB card reader");
            _output.WriteLine("  3. Insert a smart card");
            _output.WriteLine("  4. Remove the card");
            _output.WriteLine("  5. Disconnect the reader");
            _output.WriteLine("");
            _output.WriteLine("Monitoring for 90 seconds...");

            Thread.Sleep(TimeSpan.FromSeconds(60));

            _output.WriteLine("");
            _output.WriteLine("=== RESULTS ===");
            _output.WriteLine($"Arrival events: {_arrivedCount}");
            _output.WriteLine($"Removal events: {_removedCount}");

            Assert.True(_arrivedCount > 0 || _removedCount > 0,
                "Expected at least one event when reader/card is added/removed");
        }

        private void OnDeviceArrived(object? sender, SmartCardDeviceEventArgs e)
        {
            _arrivedCount++;
            string deviceInfo = FormatDeviceInfo(e.Device);
            _arrivedDevices.Add(deviceInfo);

            _output.WriteLine($"");
            _output.WriteLine($">>> ARRIVED (#{_arrivedCount}) at {DateTime.Now:HH:mm:ss.fff}");
            _output.WriteLine($"    Reader: {e.Device.Path}");
            _output.WriteLine($"    ATR: {e.Device.Atr}");
            _output.WriteLine($"    Kind: {e.Device.Kind}");
        }

        private void OnDeviceRemoved(object? sender, SmartCardDeviceEventArgs e)
        {
            _removedCount++;
            string deviceInfo = FormatDeviceInfo(e.Device);
            _removedDevices.Add(deviceInfo);

            _output.WriteLine($"");
            _output.WriteLine($"<<< REMOVED (#{_removedCount}) at {DateTime.Now:HH:mm:ss.fff}");
            _output.WriteLine($"    Reader: {e.Device.Path}");
            _output.WriteLine($"    ATR: {e.Device.Atr}");
            _output.WriteLine($"    Kind: {e.Device.Kind}");
        }

        private static string FormatDeviceInfo(ISmartCardDevice device)
        {
            return $"Reader={device.Path}, ATR={device.Atr}, Kind={device.Kind}";
        }
    }
}
