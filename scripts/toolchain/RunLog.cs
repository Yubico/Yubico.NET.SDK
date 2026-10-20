// Part of toolchain.cs, pulled in through #:include. See TOOLCHAIN.md.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using static Output;

/// <summary>A target that ran, and how it ended.</summary>
sealed record TargetRun(string Name, bool Succeeded, TimeSpan Duration, string? Error);

/// <summary>
/// Saves every toolchain run that executes a target under artifacts/toolchain/runs/&lt;timestamp&gt;-&lt;targets&gt;/:
/// run.log (the full console output), one log per step (each build, test project, pack, ...), and
/// result.json. artifacts/toolchain/latest points at the newest run, and only the newest
/// <see cref="KeptRuns"/> runs are kept. The run ends with a RESULT block that states the outcome,
/// the failing tests, what to run next, and where the logs are.
/// </summary>
static class RunLog
{
    const int KeptRuns = 5;
    const int NextFilterTestLimit = 5;

    static readonly Regex s_ansi = new(@"\x1b\[[0-9;]*m", RegexOptions.Compiled);
    static readonly object s_lock = new();
    static readonly List<TargetRun> s_targets = [];
    static readonly List<(string Target, ProjectResult Result)> s_projects = [];
    static readonly StringBuilder s_pending = new();

    static string s_repoRoot = "";
    static string s_command = "";
    static string[] s_requestedTargets = [];
    static DateTimeOffset s_started;
    static string? s_runDir;
    static StreamWriter? s_runLog;
    static string? s_currentTarget;
    static int s_step;

    public static bool Verbose { get; private set; }

    public static string ToolchainDir(string repoRoot) => Path.Combine(Conventions.ArtifactsDir(repoRoot), "toolchain");

    /// <summary>
    /// Starts capturing console output. Nothing is written to disk until the first target starts, so
    /// --help, --list-targets and usage errors leave no run behind.
    /// </summary>
    public static void Start(string repoRoot, string[] args, string[] requestedTargets, bool verbose)
    {
        s_repoRoot = repoRoot;
        s_command = $"dotnet toolchain.cs -- {string.Join(" ", args.Select(ShellQuote))}".TrimEnd();
        s_requestedTargets = requestedTargets.Length > 0 ? requestedTargets : ["default"];
        s_started = DateTimeOffset.Now;
        Verbose = verbose;

        Console.SetOut(new TeeWriter(Console.Out, WriteToLog));
        Console.SetError(new TeeWriter(Console.Error, WriteToLog));
    }

    /// <summary>A new log file for one step of the current run, or null when nothing is being saved.</summary>
    public static string? StepLogPath(string stepName)
    {
        lock (s_lock)
        {
            if (s_runDir is null) return null;
            var safeName = Regex.Replace(stepName, @"[^A-Za-z0-9._-]+", "-");
            return Path.Combine(s_runDir, $"{++s_step:00}-{safeName}.log");
        }
    }

    /// <summary>Repo-relative form of a path for messages.</summary>
    public static string Display(string path) => ProjectDiscovery.RepoRelative(s_repoRoot, path);

    public static void Record(ProjectResult result)
    {
        lock (s_lock) s_projects.Add((s_currentTarget ?? "", result));
    }

    /// <summary>Runs one target's action and records how it ended.</summary>
    public static void RunTarget(string name, Action action)
    {
        EnsureRunDirectory();
        s_currentTarget = name;
        var stopwatch = Stopwatch.StartNew();
        try
        {
            action();
            lock (s_lock) s_targets.Add(new TargetRun(name, true, stopwatch.Elapsed, null));
        }
        catch (Exception ex)
        {
            lock (s_lock) s_targets.Add(new TargetRun(name, false, stopwatch.Elapsed, ex.Message));
            throw;
        }
        finally
        {
            s_currentTarget = null;
        }
    }

    /// <summary>
    /// Prints the RESULT block, writes result.json, and returns the process exit code. A run that never
    /// started a target (help, target lists, usage errors) prints nothing extra.
    /// </summary>
    public static int Finish(string? usageError)
    {
        if (usageError is not null)
        {
            PrintColored($"✗ {usageError}", ConsoleColor.Red);
            Console.WriteLine("Run `dotnet toolchain.cs -- --help` for targets and options.");
        }

        if (s_runDir is null)
            return usageError is null ? 0 : 2;

        var succeeded = usageError is null && s_targets.Count > 0 && s_targets.All(t => t.Succeeded);
        PrintResult(succeeded);
        WriteResultJson(succeeded);

        lock (s_lock)
        {
            s_runLog?.Flush();
            s_runLog?.Dispose();
            s_runLog = null;
        }

        return succeeded ? 0 : 1;
    }

    static void PrintResult(bool succeeded)
    {
        var separator = new string('=', 60);
        var projects = s_projects.Select(p => p.Result).ToArray();

        Console.WriteLine($"\n{separator}");
        PrintColored($"RESULT: {(succeeded ? "PASSED" : "FAILED")}  ({s_command})", succeeded ? ConsoleColor.Green : ConsoleColor.Red);

        var nameWidth = s_targets.Max(t => t.Name.Length) + 2;
        foreach (var target in s_targets)
        {
            var marker = target.Succeeded ? "✓" : "✗";
            var error = target.Error is null ? "" : $"  {target.Error}";
            Console.WriteLine($"  {marker} {target.Name.PadRight(nameWidth)}{target.Duration.TotalSeconds,6:F1}s{error}");
        }

        if (projects.Length > 0)
        {
            var counted = projects.Where(p => p.Counts is not null).Select(p => p.Counts!).ToArray();
            var tests = counted.Length == 0
                ? ""
                : $"; tests: {counted.Sum(c => c.Executed)} executed, {counted.Sum(c => c.Failed)} failed, {counted.Sum(c => c.Skipped)} skipped";
            Console.WriteLine(
                $"Projects: {projects.Count(p => p.Outcome == Outcome.Passed)} passed, " +
                $"{projects.Count(p => p.Outcome == Outcome.Failed)} failed, " +
                $"{projects.Count(p => p.Outcome == Outcome.NoTestsExecuted)} with no tests executed{tests}");
        }

        foreach (var (target, project) in s_projects.Where(p => p.Result.Outcome == Outcome.Failed))
        {
            Console.WriteLine($"Failed: {project.Project} ({target}) - {project.Reason}");
            foreach (var test in project.FailedTests)
                Console.WriteLine($"  - {test.Name}: {test.Message}");
            if (project.LogPath is not null)
                Console.WriteLine($"  output: {Display(project.LogPath)}");
        }

        foreach (var next in NextSteps())
            Console.WriteLine($"Next: {next}");

        Console.WriteLine($"Run log: {Display(Path.Combine(s_runDir!, "run.log"))} (result.json and step logs beside it; newest run: artifacts/toolchain/latest/)");
        Console.WriteLine(separator);
    }

    // A re-run command narrowed to what failed, so the next attempt is fast and its output small.
    static IEnumerable<string> NextSteps()
    {
        foreach (var (target, project) in s_projects.Where(p => p.Result.Outcome == Outcome.Failed && p.Target is "test"))
        {
            var integration = project.Project.EndsWith(".IntegrationTests", StringComparison.Ordinal) ? " --integration" : "";
            var methods = project.FailedTests.Select(t => t.Method).Distinct().Take(NextFilterTestLimit).ToArray();
            yield return methods.Length == 0
                ? $"dotnet toolchain.cs -- {target}{integration} --project {project.Project} --verbose"
                : $"dotnet toolchain.cs -- {target}{integration} --project {project.Project} --filter \"{string.Join("|", methods.Select(m => $"FullyQualifiedName~{m}"))}\"";
        }
    }

    static void WriteResultJson(bool succeeded)
    {
        static JsonArray Array(IEnumerable<JsonNode?> items) => new([.. items]);

        var result = new JsonObject
        {
            ["status"] = succeeded ? "passed" : "failed",
            ["command"] = s_command,
            ["started"] = s_started.ToString("o"),
            ["durationSeconds"] = Math.Round((DateTimeOffset.Now - s_started).TotalSeconds, 1),
            ["runDirectory"] = Display(s_runDir!),
            ["targets"] = Array(s_targets.Select(t => (JsonNode)new JsonObject
            {
                ["name"] = t.Name,
                ["status"] = t.Succeeded ? "passed" : "failed",
                ["durationSeconds"] = Math.Round(t.Duration.TotalSeconds, 1),
                ["error"] = t.Error,
            })),
            ["projects"] = Array(s_projects.Select(p => (JsonNode)new JsonObject
            {
                ["target"] = p.Target,
                ["project"] = p.Result.Project,
                ["outcome"] = p.Result.Outcome switch
                {
                    Outcome.Passed => "passed",
                    Outcome.Failed => "failed",
                    _ => "no-tests-executed",
                },
                ["reason"] = p.Result.Reason,
                ["executed"] = p.Result.Counts?.Executed,
                ["failed"] = p.Result.Counts?.Failed,
                ["skipped"] = p.Result.Counts?.Skipped,
                ["failedTests"] = Array(p.Result.FailedTests.Select(t => (JsonNode)new JsonObject { ["name"] = t.Name, ["message"] = t.Message })),
                ["log"] = p.Result.LogPath is null ? null : Display(p.Result.LogPath),
            })),
            ["next"] = Array(NextSteps().Select(n => (JsonNode)JsonValue.Create(n))),
        };

        File.WriteAllText(
            Path.Combine(s_runDir!, "result.json"),
            result.ToJsonString(new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
    }

    static void EnsureRunDirectory()
    {
        lock (s_lock)
        {
            if (s_runDir is not null) return;

            var runsDir = Path.Combine(ToolchainDir(s_repoRoot), "runs");
            Directory.CreateDirectory(runsDir);
            PruneOldRuns(runsDir);

            var baseName = $"{s_started:yyyyMMdd-HHmmss-fff}-{string.Join("+", s_requestedTargets)}";
            var runDir = Path.Combine(runsDir, Regex.Replace(baseName, @"[^A-Za-z0-9._+-]+", "-"));
            for (var suffix = 2; Directory.Exists(runDir); suffix++)
                runDir = Path.Combine(runsDir, $"{baseName}-{suffix}");
            Directory.CreateDirectory(runDir);

            s_runLog = new StreamWriter(Path.Combine(runDir, "run.log"), append: false) { AutoFlush = true };
            s_runLog.WriteLine($"# {s_command}");
            s_runLog.WriteLine($"# started:   {s_started:yyyy-MM-dd HH:mm:ss zzz}");
            s_runLog.WriteLine($"# directory: {s_repoRoot}");
            s_runLog.WriteLine($"# git:       {Git("rev-parse", "--abbrev-ref", "HEAD")} @ {Git("rev-parse", "--short", "HEAD")}{(Git("status", "--porcelain").Length > 0 ? " (uncommitted changes)" : "")}");
            s_runLog.WriteLine($"# runtime:   {RuntimeInformation.FrameworkDescription} on {RuntimeInformation.OSDescription}");
            s_runLog.WriteLine($"# projects:  names drop the '{Conventions.ProjectPrefix}' prefix");
            s_runLog.WriteLine();
            s_runLog.Write(s_pending.ToString());
            s_pending.Clear();
            s_runDir = runDir;

            PointLatestAt(runDir);
        }
    }

    static void PruneOldRuns(string runsDir)
    {
        // Run directory names start with a sortable timestamp (to the millisecond, so runs started in the
        // same second still sort by start time). Keep room for the run being created.
        foreach (var old in Directory.GetDirectories(runsDir).Order(StringComparer.Ordinal).SkipLast(KeptRuns - 1))
        {
            try { Directory.Delete(old, recursive: true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    // artifacts/toolchain/latest is a symlink to the newest run; latest.txt names it on systems
    // where creating the link is not allowed (Windows without developer mode).
    static void PointLatestAt(string runDir)
    {
        var toolchainDir = ToolchainDir(s_repoRoot);
        var relativeRunDir = Path.GetRelativePath(toolchainDir, runDir);
        File.WriteAllText(Path.Combine(toolchainDir, "latest.txt"), Display(runDir) + Environment.NewLine);

        var latest = Path.Combine(toolchainDir, "latest");
        try
        {
            if (new FileInfo(latest).LinkTarget is not null || File.Exists(latest))
                File.Delete(latest);
            else if (Directory.Exists(latest))
                Directory.Delete(latest);
            Directory.CreateSymbolicLink(latest, relativeRunDir);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }

    static void WriteToLog(string text)
    {
        lock (s_lock)
        {
            text = s_ansi.Replace(text, "");
            if (s_runLog is not null)
                s_runLog.Write(text);
            else if (s_runDir is null)
                s_pending.Append(text);
        }
    }

    // Quotes an argument the shell would split or interpret (a filter such as "A|B" or "A&B"), so the
    // command shown in the RESULT block can be pasted back as is.
    static string ShellQuote(string argument) =>
        argument.Length > 0 && Regex.IsMatch(argument, @"^[A-Za-z0-9_./:=~@+,-]+$")
            ? argument
            : $"\"{argument.Replace("\"", "\\\"")}\"";

    static string Git(params string[] arguments)
    {
        try
        {
            var startInfo = new ProcessStartInfo("git") { RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = s_repoRoot };
            foreach (var argument in arguments)
                startInfo.ArgumentList.Add(argument);
            using var process = Process.Start(startInfo)!;
            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();
            return process.ExitCode == 0 ? output : "?";
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return "?";
        }
    }

    /// <summary>Writes to the console and hands the same text to the run log.</summary>
    sealed class TeeWriter(TextWriter console, Action<string> log) : TextWriter
    {
        public override Encoding Encoding => console.Encoding;

        public override void Write(char value)
        {
            console.Write(value);
            log(value.ToString());
        }

        public override void Write(string? value)
        {
            console.Write(value);
            if (value is not null) log(value);
        }

        public override void WriteLine(string? value)
        {
            console.WriteLine(value);
            log((value ?? "") + Environment.NewLine);
        }

        public override void Flush() => console.Flush();
    }
}
