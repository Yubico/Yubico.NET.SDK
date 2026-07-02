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

using System.Text.RegularExpressions;

namespace Yubico.YubiKit.Core.UnitTests.RuntimeResilience;

[Trait("Category", "RuntimeResilience")]
public sealed class RuntimeResilienceStaticScanTests
{
    [Fact]
    public void Scanner_FlagsIgnoredNativeStatusChangeResultInsideLoop()
    {
        const string source = """
            private void ListenerThreadProc()
            {
                while (!_shouldStop)
                {
                    _ = NativeMethods.SCardGetStatusChange(_context, 1000, states, states.Length);
                    continue;
                }
            }
            """;

        var finding = RuntimeResilienceScanner.Scan("OldSmartCardListener.cs", source)
            .Single(static finding => finding.Category == "IgnoredNativeResultInLoop");

        Assert.Equal("IgnoredNativeResultInLoop", finding.Category);
        Assert.Equal(5, finding.LineNumber);
    }

    [Fact]
    public void Scanner_FlagsNativeFailureContinueWithoutBackoff()
    {
        const string source = """
            private void ListenerThreadProc()
            {
                while (!_shouldStop)
                {
                    var result = NativeMethods.SCardListReaders(_context, null, out var readers);
                    if (result != ErrorCode.SCARD_S_SUCCESS)
                    {
                        continue;
                    }
                }
            }
            """;

        var finding = Assert.Single(RuntimeResilienceScanner.Scan("OldSmartCardListener.cs", source));

        Assert.Equal("NativeFailureContinueWithoutBackoff", finding.Category);
        Assert.Equal(8, finding.LineNumber);
    }

    [Fact]
    public void Scanner_DoesNotFlagNativeFailureContinueAfterHandler()
    {
        const string source = """
            private void ListenerThreadProc()
            {
                while (!_shouldStop)
                {
                    var result = NativeMethods.SCardListReaders(_context, null, out var readers);
                    if (result != ErrorCode.SCARD_S_SUCCESS)
                    {
                        if (!HandleSCardFailure(result, "SCardListReaders"))
                        {
                            break;
                        }

                        continue;
                    }
                }
            }
            """;

        Assert.Empty(RuntimeResilienceScanner.Scan("SafeSmartCardListener.cs", source));
    }

    [Fact]
    public void Scanner_FlagsCatchRetryWithoutBackoff()
    {
        const string source = """
            private void ListenerThreadProc()
            {
                while (!_shouldStop)
                {
                    try
                    {
                        PollNativeState();
                    }
                    catch (SCardException)
                    {
                        continue;
                    }
                }
            }
            """;

        var finding = Assert.Single(RuntimeResilienceScanner.Scan("OldRetryLoop.cs", source));

        Assert.Equal("CatchRetryWithoutBackoff", finding.Category);
        Assert.Equal(11, finding.LineNumber);
    }

    [Fact]
    public void Scanner_DoesNotFlagCatchRetryWithBackoff()
    {
        const string source = """
            private void ListenerThreadProc()
            {
                while (!_shouldStop)
                {
                    try
                    {
                        PollNativeState();
                    }
                    catch (SCardException)
                    {
                        _sleep(TimeSpan.FromSeconds(1));
                        continue;
                    }
                }
            }
            """;

        Assert.Empty(RuntimeResilienceScanner.Scan("SafeRetryLoop.cs", source));
    }

    [Fact]
    public void Scanner_FlagsSleepBeforeReadyToWritePoll()
    {
        const string source = """
            private async Task AwaitReadyToWriteAsync(CancellationToken cancellationToken)
            {
                while (stopwatch.ElapsedMilliseconds < timeLimitMs)
                {
                    await Task.Delay(50, cancellationToken).ConfigureAwait(false);

                    var report = await ReadFeatureReportAsync(cancellationToken).ConfigureAwait(false);
                    if ((report.Span[OtpConstants.FeatureReportDataSize] & OtpConstants.SlotWriteFlag) == 0)
                    {
                        return;
                    }
                }
            }
            """;

        var finding = Assert.Single(RuntimeResilienceScanner.Scan("OldOtpHidProtocol.cs", source));

        Assert.Equal("SleepBeforeReadyToWritePoll", finding.Category);
        Assert.Equal(5, finding.LineNumber);
    }

    [Fact]
    public void Scanner_CurrentCoreSource_HasNoFindings()
    {
        var repositoryRoot = FindRepositoryRoot();
        var sourceRoots = new[]
        {
            Path.Combine(repositoryRoot.FullName, "src", "Core", "src", "Protocols"),
            Path.Combine(repositoryRoot.FullName, "src", "Core", "src", "Transports")
        };

        foreach (var sourceRoot in sourceRoots)
        {
            Assert.True(Directory.Exists(sourceRoot), $"Source root does not exist: {sourceRoot}");
        }

        var sourceFiles = sourceRoots
            .SelectMany(root => Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            .ToArray();

        Assert.True(sourceFiles.Length > 0, "Current-source scan must cover at least one source file.");

        var findings = sourceFiles
            .SelectMany(file => RuntimeResilienceScanner.Scan(
                Path.GetRelativePath(repositoryRoot.FullName, file),
                File.ReadAllText(file)))
            .ToArray();

        Assert.True(findings.Length == 0, FormatFindings(findings));
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "toolchain.cs")))
        {
            current = current.Parent;
        }

        return current ?? throw new DirectoryNotFoundException("Could not locate repository root from test output path.");
    }

    private static string FormatFindings(IEnumerable<RuntimeResilienceScanner.Finding> findings) =>
        string.Join(Environment.NewLine, findings.Select(static finding =>
            $"{finding.Path}:{finding.LineNumber}: {finding.Category}: {finding.Message}"));

    private static class RuntimeResilienceScanner
    {
        private static readonly Regex MethodDeclarationPattern = new(
            @"\b(?<name>[A-Z_a-z][\w]*)\s*\([^;]*\)\s*$",
            RegexOptions.Compiled);

        public static IReadOnlyList<Finding> Scan(string path, string source)
        {
            var findings = new List<Finding>();
            var blocks = new Stack<Block>();
            var pendingLoop = false;
            var pendingCatch = false;
            var pendingMethod = (string?)null;
            var currentMethod = (string?)null;
            var methodDepth = -1;
            var readyToWriteReadSeen = false;
            var lines = source.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                var lineNumber = i + 1;
                var line = lines[i].Trim();
                var inLoop = blocks.Any(static block => block.IsLoop);
                var inCatch = blocks.Any(static block => block.IsCatch);

                if (inLoop && IsIgnoredNativeResult(line))
                {
                    findings.Add(new Finding(
                        path,
                        lineNumber,
                        "IgnoredNativeResultInLoop",
                        "Native status result is ignored inside a loop; handle, back off, exit, or throttle."));
                }

                if (currentMethod == "AwaitReadyToWriteAsync")
                {
                    if (!readyToWriteReadSeen && line.Contains("Task.Delay(", StringComparison.Ordinal))
                    {
                        findings.Add(new Finding(
                            path,
                            lineNumber,
                            "SleepBeforeReadyToWritePoll",
                            "Ready-to-write polling sleeps before checking whether the device is already ready."));
                    }

                    if (line.Contains("ReadFeatureReportAsync", StringComparison.Ordinal))
                    {
                        readyToWriteReadSeen = true;
                    }
                }

                if (inLoop && line == "continue;" && RecentNativeFailureWithoutBackoff(lines, i))
                {
                    findings.Add(new Finding(
                        path,
                        lineNumber,
                        "NativeFailureContinueWithoutBackoff",
                        "Native failure path continues the loop without a visible backoff, exit, or failure handler."));
                }

                if (inLoop && inCatch && line == "continue;" && !RecentBackoffOrExit(lines, i))
                {
                    findings.Add(new Finding(
                        path,
                        lineNumber,
                        "CatchRetryWithoutBackoff",
                        "Catch block retries a listener loop without a visible backoff or exit."));
                }

                pendingLoop |= line.StartsWith("while (", StringComparison.Ordinal);
                pendingCatch |= line.StartsWith("catch", StringComparison.Ordinal);
                pendingMethod ??= MatchMethodName(line);

                foreach (var character in line)
                {
                    if (character == '{')
                    {
                        blocks.Push(new Block(pendingLoop, pendingCatch));
                        pendingLoop = false;
                        pendingCatch = false;

                        if (pendingMethod is not null)
                        {
                            currentMethod = pendingMethod;
                            methodDepth = blocks.Count;
                            readyToWriteReadSeen = false;
                            pendingMethod = null;
                        }
                    }
                    else if (character == '}' && blocks.Count > 0)
                    {
                        if (blocks.Count == methodDepth)
                        {
                            currentMethod = null;
                            methodDepth = -1;
                        }

                        blocks.Pop();
                    }
                }
            }

            return findings;
        }

        private static bool IsIgnoredNativeResult(string line) =>
            line.StartsWith("_ = ", StringComparison.Ordinal)
            && (line.Contains("SCardGetStatusChange", StringComparison.Ordinal)
                || line.Contains("SCardListReaders", StringComparison.Ordinal)
                || line.Contains("SCardEstablishContext", StringComparison.Ordinal));

        private static string? MatchMethodName(string line)
        {
            var match = MethodDeclarationPattern.Match(line);
            if (!match.Success
                || line.Contains('=')
                || line.StartsWith("if ", StringComparison.Ordinal)
                || line.StartsWith("while ", StringComparison.Ordinal)
                || line.StartsWith("for ", StringComparison.Ordinal)
                || line.StartsWith("foreach ", StringComparison.Ordinal)
                || line.StartsWith("switch ", StringComparison.Ordinal)
                || line.StartsWith("catch ", StringComparison.Ordinal))
            {
                return null;
            }

            return match.Groups["name"].Value;
        }

        private static bool RecentNativeFailureWithoutBackoff(string[] lines, int index)
        {
            var window = RecentLines(lines, index);
            return window.Any(static line =>
                    line.Contains("NativeMethods.SCard", StringComparison.Ordinal)
                    || line.Contains("_sCardApi.SCard", StringComparison.Ordinal)
                    || line.Contains("result != ErrorCode.SCARD_S_SUCCESS", StringComparison.Ordinal))
                && !window.Any(IsBackoffExitOrHandler);
        }

        private static bool RecentBackoffOrExit(string[] lines, int index) =>
            RecentLines(lines, index).Any(IsBackoffExitOrHandler);

        private static IEnumerable<string> RecentLines(string[] lines, int index)
        {
            var start = Math.Max(0, index - 6);
            for (int i = start; i <= index; i++)
            {
                yield return lines[i].Trim();
            }
        }

        private static bool IsBackoffExitOrHandler(string line) =>
            line.Contains("_sleep(", StringComparison.Ordinal)
            || line.Contains("Thread.Sleep", StringComparison.Ordinal)
            || line.Contains("Task.Delay", StringComparison.Ordinal)
            || line.Contains("HandleSCardFailure", StringComparison.Ordinal)
            || line == "break;"
            || line.StartsWith("return", StringComparison.Ordinal);

        private readonly record struct Block(bool IsLoop, bool IsCatch);
        public readonly record struct Finding(string Path, int LineNumber, string Category, string Message);
    }
}