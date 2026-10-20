// Part of toolchain.cs, pulled in through #:include. See TOOLCHAIN.md.

using System.Diagnostics;
using System.Text;
using static Output;

/// <summary>Every way the toolchain starts a dotnet process. Each one announces its command with "▶ RUN".</summary>
static class Dotnet
{
    /// <summary>
    /// Runs `dotnet {arguments}` with live output; throws on a non-zero exit code. Values in
    /// <c>secrets</c> (API keys) are masked in the announced command line.
    /// </summary>
    public static void Run(string arguments, string workingDirectory = "", params string?[] secrets)
    {
        var shown = secrets.Where(s => !string.IsNullOrEmpty(s)).Aggregate(arguments, (text, secret) => text.Replace(secret!, "***"));
        PrintRun($"dotnet {shown}");
        SimpleExec.Command.Run("dotnet", arguments, workingDirectory, noEcho: true);
    }

    /// <summary>
    /// Runs `dotnet {arguments}` with live output and returns the test counts from the runner's own summary,
    /// or null when no summary was recognised. Throws like <see cref="Run"/> on a non-zero exit code.
    /// </summary>
    public static TestCounts? RunTests(string arguments)
    {
        PrintRun($"dotnet {arguments}");
        var startInfo = new ProcessStartInfo("dotnet", arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        var output = new StringBuilder();
        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start dotnet process");
        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is null) return;
            Console.WriteLine(e.Data);
            lock (output) output.AppendLine(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is null) return;
            Console.Error.WriteLine(e.Data);
        };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        if (process.ExitCode != 0)
            throw new ToolchainException($"dotnet exited with code {process.ExitCode}");

        return TestCounts.Parse(output.ToString());
    }

    /// <summary>Runs `dotnet` silently and returns its exit code and combined stdout + stderr.</summary>
    public static (int ExitCode, string Output) Capture(IReadOnlyList<string> arguments)
    {
        PrintRun($"dotnet {string.Join(" ", arguments.Select(a => a.Contains(' ') ? $"\"{a}\"" : a))} (output captured)");
        var startInfo = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start dotnet process");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        process.WaitForExit();

        return (process.ExitCode, stdout.GetAwaiter().GetResult() + stderr.GetAwaiter().GetResult());
    }
}

static class Bash
{
    /// <summary>Runs a repo script with bash from the repo root; fails clearly when the script is missing.</summary>
    public static void RunScript(string repoRoot, string relativePath)
    {
        var script = Path.Combine(repoRoot, relativePath);
        if (!File.Exists(script))
            throw new ToolchainException($"Missing {script}");

        PrintRun($"bash {relativePath}");
        SimpleExec.Command.Run("bash", $"\"{script}\"", workingDirectory: repoRoot, noEcho: true);
    }
}
