// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Yubico.YubiKit.Core.Devices;

namespace Yubico.YubiKit.Core.UnitTests.Devices;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class DiscoveryWorkerAdmissionCollection
{
    public const string Name = "DiscoveryWorkerAdmission";

    public static async Task WaitUntilIdleAsync(CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));

        while (true)
        {
            var admissions = new List<IDisposable>(DiscoveryWorkerAdmission.MaximumConcurrentWorkers);
            try
            {
                while (DiscoveryWorkerAdmission.TryAcquire(out var admission))
                    admissions.Add(admission);

                if (admissions.Count == DiscoveryWorkerAdmission.MaximumConcurrentWorkers)
                    return;
            }
            finally
            {
                foreach (var admission in admissions)
                    admission.Dispose();
            }

            await Task.Delay(TimeSpan.FromMilliseconds(10), timeout.Token);
        }
    }

    public static async Task<IDisposable> SaturateAsync(CancellationToken cancellationToken)
    {
        await WaitUntilIdleAsync(cancellationToken);

        var admissions = new List<IDisposable>(DiscoveryWorkerAdmission.MaximumConcurrentWorkers);
        try
        {
            for (var i = 0; i < DiscoveryWorkerAdmission.MaximumConcurrentWorkers; i++)
            {
                if (!DiscoveryWorkerAdmission.TryAcquire(out var admission))
                    throw new InvalidOperationException("Could not saturate discovery worker admission for the test.");

                admissions.Add(admission);
            }

            return new AdmissionSet(admissions);
        }
        catch
        {
            foreach (var admission in admissions)
                admission.Dispose();

            throw;
        }
    }

    private sealed class AdmissionSet(IReadOnlyList<IDisposable> admissions) : IDisposable
    {
        public void Dispose()
        {
            foreach (var admission in admissions)
                admission.Dispose();
        }
    }
}