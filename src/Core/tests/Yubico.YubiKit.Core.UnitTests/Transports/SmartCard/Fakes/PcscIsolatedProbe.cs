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

using System.Diagnostics;
using System.Runtime.ExceptionServices;

namespace Yubico.YubiKit.Core.UnitTests.Transports.SmartCard.Fakes;

internal static class PcscIsolatedProbe
{
    private const string ProbeEnvironment = "YUBIKIT_PCSC_ISOLATED_PROBE";
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public static async Task RunAsync(
        string probe,
        Type containingType,
        string methodName,
        Func<Task> runProbe)
    {
        var childProbe = Environment.GetEnvironmentVariable(ProbeEnvironment);
        if (childProbe is not null)
        {
            Assert.Equal(probe, childProbe);
            await runProbe();
            Console.WriteLine($"PROBE-COMPLETE:{probe}");
            return;
        }

        var artifactPath = Path.Combine(Path.GetTempPath(), $"yubikit-pcsc-{probe}-probe.log");
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = Environment.ProcessPath
                    ?? throw new InvalidOperationException("The unit-test executable path is unavailable."),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };
        process.StartInfo.ArgumentList.Add("--filter-method");
        process.StartInfo.ArgumentList.Add($"{containingType.FullName}.{methodName}");
        process.StartInfo.Environment[ProbeEnvironment] = probe;

        Assert.True(process.Start());
        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();
        using var watchdog = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var waitCancellation = CancellationTokenSource.CreateLinkedTokenSource(Ct, watchdog.Token);
        Exception? waitFailure = null;
        try
        {
            await process.WaitForExitAsync(waitCancellation.Token);
        }
        catch (Exception ex)
        {
            waitFailure = ex;
        }
        finally
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync(CancellationToken.None);
        }

        var output = await standardOutput;
        var error = await standardError;
        await File.WriteAllTextAsync(artifactPath, output + error, CancellationToken.None);

        if (waitFailure is not null)
            ExceptionDispatchInfo.Capture(waitFailure).Throw();
        Assert.True(process.ExitCode == 0, $"Isolated probe failed. Output: {artifactPath}");
        Assert.Contains($"PROBE-COMPLETE:{probe}", output, StringComparison.Ordinal);
        var outputLines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Assert.Contains("total: 1", outputLines);
        Assert.Contains("failed: 0", outputLines);
        Assert.Contains("succeeded: 1", outputLines);
    }
}