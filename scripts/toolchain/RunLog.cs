// Part of toolchain.cs, pulled in through #:include. See TOOLCHAIN.md.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using static Output;

/// <summary>The distinct compiler/MSBuild warnings one step produced, by code.</summary>
sealed record StepWarnings(string Step, IReadOnlyList<string> Codes, string? LogPath);

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
    const string ResultFile = "result.txt";

    static readonly Regex s_ansi = new(@"\x1b\[[0-9;]*m", RegexOptions.Compiled);
    static readonly object s_lock = new();
    static readonly List<TargetRun> s_targets = [];
    static readonly List<(string Target, ProjectResult Result)> s_projects = [];
    static readonly List<StepWarnings> s_warnings = [];
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

    public static void RecordWarnings(string step, IReadOnlyList<string> codes, string? logPath)
    {
        lock (s_lock) s_warnings.Add(new StepWarnings(step, codes, logPath));
    }

    /// <summary>Runs one target's action and records how it ended.</summary>
    public static void RunTarget(string name, Action action)
    {
        if (EnsureRunDirectory())
        {
            // First thing on screen, so a run cut short (a shell timeout) can still be found afterwards.
            Console.WriteLine($"▶ toolchain: {string.Join(" ", s_requestedTargets)}  (log: {Display(Path.Combine(s_runDir!, "run.log"))})");
            WarnIfAnotherRunIsActive();
        }

        Console.WriteLine($"\n▶ target: {name}");
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

            // Expected failures are summed up by their message; anything else is a toolchain bug and
            // needs its stack trace.
            if (ex is ToolchainException)
                PrintColored($"✗ {name}: {ex.Message}", ConsoleColor.Red);
            else
                PrintColored($"✗ {name}: toolchain error (please report): {ex}", ConsoleColor.Red);
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

        try
        {
            if (File.Exists(ActiveRunMarker) && File.ReadAllText(ActiveRunMarker).StartsWith($"{Environment.ProcessId}\t", StringComparison.Ordinal))
                File.Delete(ActiveRunMarker);
        }
        catch (IOException)
        {
        }

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
        var lines = BuildResult(succeeded);

        // The RESULT block is the run's one summary: print it, and keep a copy for `last`.
        Console.WriteLine();
        Console.WriteLine(lines[^1]);
        PrintColored(lines[0], succeeded ? ConsoleColor.Green : ConsoleColor.Red);
        foreach (var line in lines.Skip(1))
            Console.WriteLine(line);

        File.WriteAllLines(Path.Combine(s_runDir!, ResultFile), lines);
    }

    static List<string> BuildResult(bool succeeded)
    {
        var lines = new List<string>
        {
            $"RESULT: {(succeeded ? "PASSED" : "FAILED")}  ({s_command})",
            "Targets:",
        };

        var targetWidth = s_targets.Max(t => t.Name.Length) + 2;
        foreach (var target in s_targets)
        {
            var error = target.Error is null ? "" : $"  {target.Error}";
            lines.Add($"  {(target.Succeeded ? "✓" : "✗")} {target.Name.PadRight(targetWidth)}{target.Duration.TotalSeconds,6:F1}s{error}");
        }

        // Every project once, with what went wrong nested under it.
        var markerWidth = new[] { PassMarker, FailMarker, NoTestsMarker }.Max(m => m.Length) + 2;
        foreach (var group in s_projects.GroupBy(p => p.Target))
        {
            lines.Add($"Projects ({group.Key}):");
            var nameWidth = group.Max(p => p.Result.Project.Length) + 2;
            foreach (var (_, project) in group)
            {
                var marker = project.Outcome switch
                {
                    Outcome.Passed => PassMarker,
                    Outcome.Failed => FailMarker,
                    _ => NoTestsMarker,
                };
                lines.Add($"  {marker.PadRight(markerWidth)}{project.Project.PadRight(nameWidth)}{project.Reason}".TrimEnd());
                if (project.Outcome != Outcome.Failed)
                    continue;

                foreach (var test in project.FailedTests)
                    lines.Add($"      - {test.Name}: {test.Message}");
                if (project.LogPath is not null)
                    lines.Add($"      output: {Display(project.LogPath)}");
            }
        }

        var projects = s_projects.Select(p => p.Result).ToArray();
        if (projects.Length > 0)
        {
            var counted = projects.Where(p => p.Counts is not null).Select(p => p.Counts!).ToArray();
            var tests = counted.Length == 0
                ? ""
                : $"; tests: {counted.Sum(c => c.Executed)} executed, {counted.Sum(c => c.Failed)} failed, {counted.Sum(c => c.Skipped)} skipped";
            lines.Add(
                $"Totals: projects: {projects.Count(p => p.Outcome == Outcome.Passed)} passed, " +
                $"{projects.Count(p => p.Outcome == Outcome.Failed)} failed, " +
                $"{projects.Count(p => p.Outcome == Outcome.NoTestsExecuted)} with no tests executed{tests}");
        }

        // Warnings never fail a run, and the console only shows error lines, so count them here.
        lines.Add($"Warnings: {s_warnings.Sum(w => w.Codes.Count)}");
        foreach (var step in s_warnings)
        {
            var codes = string.Join(", ", step.Codes.GroupBy(c => c).OrderByDescending(g => g.Count()).Select(g => g.Count() == 1 ? g.Key : $"{g.Key} x{g.Count()}"));
            var where = step.LogPath is null ? "" : $"  output: {Display(step.LogPath)}";
            lines.Add($"  {step.Step}: {step.Codes.Count} ({codes}){where}");
        }

        lines.AddRange(NextSteps().Select(next => $"Next: {next}"));
        lines.Add($"Run log: {Display(Path.Combine(s_runDir!, "run.log"))} (result.json and step logs beside it; reprint this block: dotnet toolchain.cs -- last)");
        lines.Add(new string('=', 60));
        return lines;
    }

    /// <summary>Prints the RESULT block of the newest saved run, for `dotnet toolchain.cs -- last`.</summary>
    public static int PrintLast(string repoRoot)
    {
        var latest = Path.Combine(ToolchainDir(repoRoot), "latest.txt");
        var runDir = File.Exists(latest) ? Path.Combine(repoRoot, File.ReadAllText(latest).Trim()) : null;
        var result = runDir is null ? null : Path.Combine(runDir, ResultFile);
        if (result is null || !File.Exists(result))
        {
            Console.WriteLine("No saved run with a RESULT block yet (runs are saved under artifacts/toolchain/runs/).");
            return 1;
        }

        Console.WriteLine($"Newest saved run: {ProjectDiscovery.RepoRelative(repoRoot, runDir!)} (finished {File.GetLastWriteTime(result):yyyy-MM-dd HH:mm:ss})");
        foreach (var line in File.ReadAllLines(result))
            Console.WriteLine(line);
        return 0;
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
            ["warningCount"] = s_warnings.Sum(w => w.Codes.Count),
            ["warnings"] = Array(s_warnings.Select(w => (JsonNode)new JsonObject
            {
                ["step"] = w.Step,
                ["count"] = w.Codes.Count,
                ["codes"] = Array(w.Codes.Select(c => (JsonNode)JsonValue.Create(c))),
                ["log"] = w.LogPath is null ? null : Display(w.LogPath),
            })),
            ["next"] = Array(NextSteps().Select(n => (JsonNode)JsonValue.Create(n))),
        };

        File.WriteAllText(
            Path.Combine(s_runDir!, "result.json"),
            result.ToJsonString(new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
    }

    /// <summary>Creates the run directory on first use; true when this call created it.</summary>
    static bool EnsureRunDirectory()
    {
        lock (s_lock)
        {
            if (s_runDir is not null) return false;

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
            return true;
        }
    }

    static string ActiveRunMarker => Path.Combine(ToolchainDir(s_repoRoot), "active-run.txt");

    // Two runs in one checkout (parallel agents) build into the same bin/obj folders and can fail in
    // confusing ways. Say so up front; the marker names the other run's process and log.
    static void WarnIfAnotherRunIsActive()
    {
        try
        {
            if (File.Exists(ActiveRunMarker))
            {
                var parts = File.ReadAllText(ActiveRunMarker).Split('\t', 2);
                if (int.TryParse(parts[0], out var pid) && pid != Environment.ProcessId && IsRunning(pid))
                    PrintWarn($"Another toolchain run is active in this checkout (process {pid}, {(parts.Length > 1 ? parts[1].Trim() : "unknown log")}); parallel builds can collide");
            }

            File.WriteAllText(ActiveRunMarker, $"{Environment.ProcessId}\t{Display(Path.Combine(s_runDir!, "run.log"))}");
        }
        catch (IOException)
        {
        }
    }

    static bool IsRunning(int pid)
    {
        try
        {
            using var process = Process.GetProcessById(pid);
            return !process.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
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
