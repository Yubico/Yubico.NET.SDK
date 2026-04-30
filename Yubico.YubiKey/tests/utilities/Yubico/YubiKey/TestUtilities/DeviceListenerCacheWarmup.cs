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

using System.Linq;
using System.Threading;

namespace Yubico.YubiKey.TestUtilities
{
    /// <summary>
    /// Workaround for a YubiKeyDeviceListener startup race observed on macOS:
    /// MacOSHidDeviceListener arms its IOKit run loop on a background thread,
    /// so the first call to YubiKeyDeviceListener.Update() (which runs
    /// synchronously in the listener's constructor) can complete before the
    /// HID layer has fired its initial arrival callbacks. The result is a
    /// transient empty cache for ~hundreds of milliseconds after the listener
    /// is created, which makes test base classes that enumerate in their
    /// instance constructor see DeviceNotFoundException even when a device is
    /// plugged in.
    ///
    /// Intended use: invoke from a test class's static constructor (runs once,
    /// before any [Fact] body) so the listener has time to populate before
    /// instance construction kicks off.
    ///
    /// SDK fix tracked separately. Until then, every Fido2/Hid integration
    /// test class that doesn't already do its own warm-up should call this.
    /// </summary>
    public static class DeviceListenerCacheWarmup
    {
        /// <summary>
        /// Poll YubiKeyDevice.FindAll() until it returns at least one device,
        /// or until the timeout elapses. Returns silently in either case;
        /// downstream tests are expected to SKIP cleanly via
        /// DeviceNotFoundException if no device materialises.
        /// </summary>
        /// <param name="timeoutMilliseconds">Total wall-clock budget. Default 5s.</param>
        /// <param name="pollIntervalMilliseconds">Poll interval. Default 100ms.</param>
        public static void WaitForFirstDevice(
            int timeoutMilliseconds = 5000,
            int pollIntervalMilliseconds = 100)
        {
            try
            {
                int iterations = timeoutMilliseconds / pollIntervalMilliseconds;
                for (int i = 0; i < iterations; i++)
                {
                    if (YubiKeyDevice.FindAll().Any())
                    {
                        return;
                    }

                    Thread.Sleep(pollIntervalMilliseconds);
                }
            }
            catch
            {
                // Swallow — if enumeration throws, downstream test skip path
                // (DeviceNotFoundException) handles user feedback.
            }
        }
    }
}
