// Part of toolchain.cs, pulled in through #:include. See TOOLCHAIN.md.

using System.Diagnostics;
using System.Text;

static class DotnetProcess
{
    // Runs `dotnet {arguments}` with live output and returns the test counts from the runner's own summary,
    // or null when no summary was recognised. Throws like Run on a non-zero exit code.
    public static TestCounts? RunTestAndReadCounts(string arguments)
    {
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
            throw new InvalidOperationException($"dotnet {arguments} exited with code {process.ExitCode}");

        return TestCounts.Parse(output.ToString());
    }

    public static (int ExitCode, string Output) RunAndCapture(IReadOnlyList<string> arguments)
    {
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
