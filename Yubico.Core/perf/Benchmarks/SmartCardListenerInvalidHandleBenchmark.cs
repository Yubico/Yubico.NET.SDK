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
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using Yubico.Core.Devices.SmartCard;
using Yubico.Core.Performance.Legacy;
using Yubico.Core.Performance.Mocks;

namespace Yubico.Core.Performance.Benchmarks
{
    /// <summary>
    /// Benchmark proving PR #445's recovery wiring fixes the SCARD_E_INVALID_HANDLE busy-loop.
    ///
    /// Production logs from YubiPCS 1.9.1.2 show ~3,700 SCardGetStatusChange invocations/sec
    /// under persistent SCARD_E_INVALID_HANDLE failures (RDS session disconnect scenario).
    ///
    /// Expected ratio: Legacy/Fixed >= 1000x, because the legacy listener spins in a tight loop
    /// while the #445 fix backs off with 1-second delays.
    /// </summary>
    [SimpleJob(RunStrategy.Monitoring, RuntimeMoniker.Net80,
               warmupCount: 1, iterationCount: 5, invocationCount: 1)]
    public class SmartCardListenerInvalidHandleBenchmark
    {
        private static readonly TimeSpan ObservationWindow = TimeSpan.FromSeconds(1);

        private AlwaysInvalidHandleScardInterop _legacyMock = null!;
        private AlwaysInvalidHandleScardInterop _fixedMock = null!;

        [IterationSetup(Target = nameof(LegacyListener_InvocationsInOneSecond))]
        public void SetupLegacy() => _legacyMock = new AlwaysInvalidHandleScardInterop();

        [IterationSetup(Target = nameof(FixedListener_InvocationsInOneSecond))]
        public void SetupFixed() => _fixedMock = new AlwaysInvalidHandleScardInterop();

        [Benchmark(Baseline = true, Description = "develop (pre-#445) — busy spin")]
        public int LegacyListener_InvocationsInOneSecond()
        {
            using var listener = new LegacyDesktopSmartCardDeviceListener(_legacyMock);
            Thread.Sleep(ObservationWindow);
            return _legacyMock.Invocations;
        }

        [Benchmark(Description = "#445 fix — bounded recovery")]
        public int FixedListener_InvocationsInOneSecond()
        {
            using var listener = new DesktopSmartCardDeviceListener(_fixedMock);
            Thread.Sleep(ObservationWindow);
            return _fixedMock.Invocations;
        }

        [GlobalCleanup]
        public void Report()
        {
            // Last-iteration counters surface to the BDN stdout file.
            Console.WriteLine($"[FINAL] Legacy invocations: {_legacyMock?.Invocations ?? -1}");
            Console.WriteLine($"[FINAL] Fixed  invocations: {_fixedMock?.Invocations ?? -1}");
        }
    }
}
