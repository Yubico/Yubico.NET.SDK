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

using System.Diagnostics;

namespace Yubico.YubiKit.Cli.Commands.UnitTests.AuditV2;

/// <summary>
///     Runs a single test method of this test assembly in a child process. Used for repros whose
///     failure mode terminates the process (e.g. a negative <c>stackalloc</c> size, which the .NET 10
///     runtime turns into a fatal stack overflow) so the shared test host is never destabilised.
/// </summary>
/// <remarks>
///     The child test body must call <see cref="SkipUnlessChild" /> first; in a normal run it is
///     reported as skipped and only executes when launched by <see cref="RunAsync" />.
/// </remarks>
internal static class ChildProcessTestRunner
{
    private const string ChildMarkerVariable = "YUBIKIT_AUDITV2_CHILD_TEST";

    public static void SkipUnlessChild(string childTestName)
    {
        if (Environment.GetEnvironmentVariable(ChildMarkerVariable) != childTestName)
        {
            Assert.Skip("Child-process body; executed only via ChildProcessTestRunner.");
        }
    }

    public static async Task<(int ExitCode, string Output)> RunAsync(
        Type testClass,
        string methodName,
        CancellationToken cancellationToken)
    {
        var assemblyPath = testClass.Assembly.Location;
        var processPath = Environment.ProcessPath ?? "dotnet";
        var hostIsDotnet = string.Equals(
            Path.GetFileNameWithoutExtension(processPath), "dotnet", StringComparison.OrdinalIgnoreCase);

        var startInfo = new ProcessStartInfo(processPath)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        if (hostIsDotnet)
        {
            startInfo.ArgumentList.Add("exec");
            startInfo.ArgumentList.Add(assemblyPath);
        }

        startInfo.ArgumentList.Add("--filter-method");
        startInfo.ArgumentList.Add($"{testClass.FullName}.{methodName}");
        startInfo.Environment[ChildMarkerVariable] = methodName;
        startInfo.Environment["DOTNET_DbgEnableMiniDump"] = "0";

        using var process = Process.Start(startInfo)
                            ?? throw new InvalidOperationException("Failed to start child test process.");

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(90));

        var stdout = process.StandardOutput.ReadToEndAsync(timeout.Token);
        var stderr = process.StandardError.ReadToEndAsync(timeout.Token);
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw;
        }

        // stderr first: a fatal runtime error ("Stack overflow.") is printed there.
        return (process.ExitCode, (await stderr) + (await stdout));
    }
}
