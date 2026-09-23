// Shared by crap.cs and complexity.cs through #:include. See TOOLCHAIN.md.

using System.Diagnostics;

static class Repo
{
    /// <summary>Walks up from the working directory to the folder that contains toolchain.cs.</summary>
    public static string? FindRoot()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "toolchain.cs")))
            dir = dir.Parent;

        return dir?.FullName;
    }

    /// <summary>Repo-relative path with forward slashes, e.g. "src/Piv/src/PivSession.cs".</summary>
    public static string Relative(string repoRoot, string path) =>
        Path.GetRelativePath(repoRoot, path).Replace('\\', '/');

    /// <summary>Runs git in the repo root and captures its output.</summary>
    /// <remarks>
    /// core.quotepath=off keeps non-ASCII paths readable instead of octal-escaped, so they
    /// compare equal to the paths found on disk.
    /// </remarks>
    public static (int ExitCode, string StdOut, string StdErr) Git(string repoRoot, params string[] arguments)
    {
        var start = new ProcessStartInfo("git")
        {
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        start.ArgumentList.Add("-c");
        start.ArgumentList.Add("core.quotepath=off");
        foreach (var argument in arguments)
            start.ArgumentList.Add(argument);

        try
        {
            using var process = Process.Start(start)
                ?? throw new InvalidOperationException("git did not start");

            // Read both streams concurrently: a large diff can fill one pipe while the
            // other is being waited on.
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            process.WaitForExit();

            return (process.ExitCode, stdout.GetAwaiter().GetResult(), stderr.GetAwaiter().GetResult());
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            return (-1, string.Empty, $"could not run git: {ex.Message}");
        }
    }
}
