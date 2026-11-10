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
using System.Runtime.InteropServices;

namespace Yubico.YubiKit.Tests.Shared.Infrastructure;

/// <summary>
///     Announces an instruction to the operator of an attended (<see cref="TestCategories.RequiresUserPresence" />)
///     test: always on the console, and spoken aloud where a platform speech engine exists.
/// </summary>
/// <remarks>
///     Speech is best-effort and never affects the test outcome. The operator usually cannot watch the
///     runner's console while handling hardware, and test-host output is often captured rather than shown
///     live, so a spoken cue is the only reliable real-time signal. Set <c>YUBIKEY_TEST_QUIET=1</c> to
///     suppress speech. Engines: <c>say</c> on macOS, <c>System.Speech</c> through PowerShell on Windows,
///     <c>spd-say</c> or <c>espeak</c> on Linux.
/// </remarks>
public static class OperatorCue
{
    private static readonly bool Quiet = Environment.GetEnvironmentVariable("YUBIKEY_TEST_QUIET") is "1" or "true";

    /// <summary>Prints <paramref name="instruction" /> and speaks it, returning once speech has finished.</summary>
    /// <param name="instruction">Plain text; keep it short enough to act on.</param>
    /// <param name="echo">Optional secondary sink, such as xUnit's <c>ITestOutputHelper.WriteLine</c>.</param>
    public static void Announce(string instruction, Action<string>? echo = null)
    {
        Console.WriteLine($">>> {instruction}");
        echo?.Invoke($">>> {instruction}");
        if (Quiet)
            return;

        try
        {
            using var process = Process.Start(CreateSpeechStartInfo(instruction));
            process?.WaitForExit(TimeSpan.FromSeconds(15));
        }
        catch (Exception)
        {
            // No speech engine, or it failed: the console line above is the fallback.
        }
    }

    private static ProcessStartInfo CreateSpeechStartInfo(string text)
    {
        var info = new ProcessStartInfo { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            info.FileName = "say";
            info.ArgumentList.Add(text);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            info.FileName = "powershell";
            info.ArgumentList.Add("-NoProfile");
            info.ArgumentList.Add("-Command");
            info.ArgumentList.Add(
                "Add-Type -AssemblyName System.Speech; " +
                $"(New-Object System.Speech.Synthesis.SpeechSynthesizer).Speak('{text.Replace("'", "''")}')");
        }
        else
        {
            // spd-say returns immediately; espeak blocks. Prefer the one that is installed.
            info.FileName = File.Exists("/usr/bin/spd-say") ? "spd-say" : "espeak";
            if (info.FileName == "spd-say")
                info.ArgumentList.Add("--wait");
            info.ArgumentList.Add(text);
        }

        return info;
    }
}
