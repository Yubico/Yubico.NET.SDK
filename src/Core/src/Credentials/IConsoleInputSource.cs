// Copyright 2025 Yubico AB
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

namespace Yubico.YubiKit.Core.Credentials;

/// <summary>
/// Abstraction for console input to enable unit testing.
/// </summary>
internal interface IConsoleInputSource
{
    /// <summary>
    /// Gets whether console input is available (not redirected from a file/pipe).
    /// </summary>
    bool IsInteractive { get; }

    /// <summary>
    /// Gets a value indicating whether a key press is available to be read.
    /// </summary>
    bool KeyAvailable { get; }

    /// <summary>
    /// Reads the next key from the console without displaying it.
    /// </summary>
    ConsoleKeyInfo ReadKey(bool intercept);

    /// <summary>
    /// Reads a line of text from the console (fallback for non-interactive mode).
    /// </summary>
    string? ReadLine();

    /// <summary>
    /// Writes text to the console output.
    /// </summary>
    void Write(string text);

    /// <summary>
    /// Writes a line of text to the console output.
    /// </summary>
    void WriteLine(string text);
}

/// <summary>
/// Real console input implementation that delegates to <see cref="Console"/>.
/// </summary>
internal sealed class RealConsoleInput : IConsoleInputSource
{
    public bool IsInteractive
    {
        get
        {
            try
            {
                return !Console.IsInputRedirected && !Console.IsOutputRedirected;
            }
            catch
            {
                return false;
            }
        }
    }

    public bool KeyAvailable => Console.KeyAvailable;

    public ConsoleKeyInfo ReadKey(bool intercept) => Console.ReadKey(intercept);

    public string? ReadLine() => Console.ReadLine();

    public void Write(string text) => Console.Write(text);

    public void WriteLine(string text) => Console.WriteLine(text);
}

/// <summary>
/// Mock console input for unit testing.
/// </summary>
internal sealed class MockConsoleInput : IConsoleInputSource
{
    private readonly Queue<ConsoleKeyInfo> _keys = new();
    private readonly Queue<string?> _lines = new();
    private readonly List<string> _output = [];

    public bool IsInteractive { get; set; } = true;

    public bool KeyAvailable => _keys.Count > 0;

    public IReadOnlyList<string> Output => _output;

    public void EnqueueKey(ConsoleKey key, char keyChar = '\0', bool shift = false, bool alt = false, bool control = false)
    {
        _keys.Enqueue(new ConsoleKeyInfo(
            keyChar == '\0' ? (char)key : keyChar,
            key,
            shift,
            alt,
            control));
    }

    public void EnqueueKeys(string text)
    {
        foreach (char c in text)
        {
            _keys.Enqueue(new ConsoleKeyInfo(c, ConsoleKey.None, false, false, false));
        }
    }

    public void EnqueueLine(string? line) => _lines.Enqueue(line);

    public ConsoleKeyInfo ReadKey(bool intercept)
    {
        if (_keys.Count == 0)
        {
            throw new InvalidOperationException("No more keys in queue");
        }

        return _keys.Dequeue();
    }

    public string? ReadLine()
    {
        if (_lines.Count == 0)
        {
            throw new InvalidOperationException("No more lines in queue");
        }

        return _lines.Dequeue();
    }

    public void Write(string text) => _output.Add(text);

    public void WriteLine(string text) => _output.Add(text + Environment.NewLine);
}
