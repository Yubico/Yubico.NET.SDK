// Copyright 2022 Yubico AB
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
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Xunit;
using Yubico.YubiKey.Fido2;
using Yubico.YubiKey.Otp;
using Yubico.YubiKey.Piv;
using Yubico.YubiKey.TestUtilities;
using Log = Yubico.Core.Logging.Log;

namespace Yubico.YubiKey
{
    class ThreadIdEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                "ThreadId", Environment.CurrentManagedThreadId));
        }
    }

    public class ReclaimTimeoutTests
    {
        [Trait(TraitTypes.Category, TestCategories.Elevated)]
        [Fact]
        public void SwitchingBetweenTransports_ForcesThreeSecondWait()
        {
            // Force the old behavior even for newer YubiKeys.
            AppContext.SetSwitch(YubiKeyCompatSwitches.UseOldReclaimTimeoutBehavior, true);

            using Logger? log = new LoggerConfiguration()
                .Enrich.With(new ThreadIdEnricher())
                .WriteTo.Console(
                    outputTemplate: "{Timestamp:HH:mm:ss.fffffff} [{Level}] ({ThreadId})  {Message}{NewLine}{Exception}")
                .CreateLogger();

            Log.CustomLoggerFactory = LoggerFactory.Create(
                builder => builder
                    .AddSerilog(log)
                    .AddFilter(level => level >= LogLevel.Information));

            // TEST ASSUMPTION: This test requires FIDO. On Windows, that means this test case must run elevated (admin).
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice();

            // Ensure all interfaces are active
            if (testDevice.EnabledUsbCapabilities != YubiKeyCapabilities.All)
            {
                testDevice.SetEnabledUsbCapabilities(YubiKeyCapabilities.All);
                Thread.Sleep(TimeSpan.FromSeconds(2)); // Give the YubiKey time to reboot and be rediscovered.
                testDevice = TestDeviceSelection.RenewDeviceEnumeration(testDevice.SerialNumber!.Value);
            }

            // Run the tests - Keyboard -> SmartCard -> FIDO
            var sw1 = Stopwatch.StartNew();
            using (var otp = new OtpSession(testDevice))
            {
                // Running the OtpSession constructor calls the OTP interface
            }
            sw1.Stop();

            var sw2 = Stopwatch.StartNew();
            using (var piv = new PivSession(testDevice))
            {
                // Running the PivSession constructor calls the SmartCard interface
            }
            sw2.Stop();

            var sw3 = Stopwatch.StartNew();
            using (var fido2 = new Fido2Session(testDevice))
            {
                // Running the Fido2Session constructor calls the FIDO interface
            }
            sw3.Stop();

            const long expectedLapse = 3000;
            Assert.True(sw1.ElapsedMilliseconds > expectedLapse);
            Assert.True(sw2.ElapsedMilliseconds > expectedLapse);
            Assert.True(sw3.ElapsedMilliseconds > expectedLapse);
        }
    }
}
