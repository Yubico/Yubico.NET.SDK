// Part of toolchain.cs, pulled in through #:include. See TOOLCHAIN.md.

using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using static Output;

/// <summary>What a finished process printed, and where its full output was saved.</summary>
sealed record ProcessResult(int ExitCode, string Output, string? LogPath);

/// <summary>
/// Every process the toolchain starts goes through here, so each one is announced with "▶ RUN", its
/// output reaches the run log, and in the default (non --verbose) mode only the lines that matter
/// reach the console. The full output of a step is always in its own log file.
/// </summary>
static class Exec
{
    // MSBuild/compiler diagnostics ("file(1,2): error CS0029: ..." and "error MSB1009: ...").
    static readonly Regex s_errorLine = new(@"(:\s*error\s+\w+\d*:|^\s*error\s+\w+\d*:)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    const int TailLinesOnFailure = 30;

    /// <summary>Show the whole output live (reports such as crap/complexity), or only error lines.</summary>
    public enum Display { Stream, ErrorsOnly }

    public static ProcessResult Run(
        string fileName,
        string arguments,
        Display display,
        string? stepName = null,
        string workingDirectory = "",
        params string?[] secrets)
    {
        var shown = Mask($"{fileName} {arguments}", secrets);
        var logPath = stepName is null ? null : RunLog.StepLogPath(stepName);
        PrintRun(shown);
        if (logPath is not null && display == Display.ErrorsOnly && !RunLog.Verbose)
            Console.WriteLine($"  output: {RunLog.Display(logPath)}");

        var startInfo = new ProcessStartInfo(fileName, arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = workingDirectory,
        };

        var output = new StringBuilder();
        var shownErrors = new HashSet<string>(StringComparer.Ordinal);
        using var log = logPath is null ? null : new StreamWriter(logPath, append: false) { AutoFlush = true };
        log?.WriteLine($"$ {shown}");
        var stream = display == Display.Stream || RunLog.Verbose;

        void OnLine(string? line, bool isError)
        {
            if (line is null) return;
            lock (output)
            {
                output.AppendLine(line);
                log?.WriteLine(line);
                if (stream)
                    (isError ? Console.Error : Console.Out).WriteLine(line);
                else if (s_errorLine.IsMatch(line) && shownErrors.Add(line.Trim()))
                    Console.WriteLine($"  {line.Trim()}"); // MSBuild repeats each error in its summary
            }
        }

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException($"Failed to start {fileName}");
        process.OutputDataReceived += (_, e) => OnLine(e.Data, isError: false);
        process.ErrorDataReceived += (_, e) => OnLine(e.Data, isError: true);
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        return new ProcessResult(process.ExitCode, output.ToString(), logPath);
    }

    /// <summary>Like <see cref="Run"/>, but a non-zero exit code fails the target.</summary>
    public static ProcessResult RunOrFail(
        string fileName,
        string arguments,
        Display display,
        string? stepName = null,
        string workingDirectory = "",
        params string?[] secrets)
    {
        var result = Run(fileName, arguments, display, stepName, workingDirectory, secrets);
        if (result.ExitCode == 0)
            return result;

        // Without --verbose the console saw only error lines. When there were none (a crash, a failed
        // restore, a script error) show the end of the output, which is where the cause usually is.
        if (display == Display.ErrorsOnly && !RunLog.Verbose && !s_errorLine.IsMatch(result.Output))
        {
            Console.WriteLine($"  last {TailLinesOnFailure} lines:");
            foreach (var line in result.Output.Split('\n').Select(l => l.TrimEnd('\r')).Where(l => l.Length > 0).TakeLast(TailLinesOnFailure))
                Console.WriteLine($"  | {line}");
        }

        var where = result.LogPath is null ? "" : $"; full output: {RunLog.Display(result.LogPath)}";
        throw new ToolchainException($"`{Mask($"{fileName} {arguments}", secrets)}` exited with code {result.ExitCode}{where}");
    }

    static string Mask(string text, string?[] secrets) =>
        secrets.Where(s => !string.IsNullOrEmpty(s)).Aggregate(text, (current, secret) => current.Replace(secret!, "***"));
}

/// <summary>Shorthands for the dotnet commands the targets run.</summary>
static class Dotnet
{
    /// <summary>Runs `dotnet {arguments}`, showing only error lines unless --verbose; fails on a non-zero exit.</summary>
    public static ProcessResult Run(string arguments, string stepName, params string?[] secrets) =>
        Exec.RunOrFail("dotnet", arguments, Exec.Display.ErrorsOnly, stepName, secrets: secrets);

    /// <summary>Runs `dotnet {arguments}` with its output shown live (the output is the result, e.g. a report).</summary>
    public static ProcessResult Stream(string arguments, string workingDirectory = "") =>
        Exec.RunOrFail("dotnet", arguments, Exec.Display.Stream, workingDirectory: workingDirectory);

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

        Exec.RunOrFail("bash", $"\"{relativePath}\"", Exec.Display.Stream, workingDirectory: repoRoot);
    }
}
