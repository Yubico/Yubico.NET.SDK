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

using Spectre.Console;
using Yubico.YubiKit.Cli.Shared.Device;
using Yubico.YubiKit.Cli.Shared.Output;
using Yubico.YubiKit.Core.Interfaces;

namespace Yubico.YubiKit.Cli.Shared.Cli;

/// <summary>
/// Provides shared session lifecycle helpers for CLI tools:
/// device selection, session creation with error handling, and credential input.
/// </summary>
/// <remarks>
/// <para>
/// Each CLI tool creates its own application-specific session (OATH, PIV, FIDO2, etc.),
/// but the surrounding lifecycle is identical: select device, create session, handle errors,
/// dispose properly. This helper extracts that pattern into a reusable flow.
/// </para>
/// <para>
/// The <see cref="WithSessionAsync{TSession}"/> method encapsulates the full lifecycle:
/// select device, create session via a factory delegate, execute an action, and dispose.
/// </para>
/// </remarks>
public static class SessionHelper
{
    /// <summary>
    /// Selects a device, creates an application session, and executes an action within it.
    /// The session is disposed automatically when the action completes or throws.
    /// </summary>
    /// <typeparam name="TSession">The session type (must be <see cref="IAsyncDisposable"/>).</typeparam>
    /// <param name="deviceSelector">
    /// A function that selects a device. Returns null if no device is available.
    /// </param>
    /// <param name="sessionFactory">
    /// A factory that creates a session from the selected device.
    /// </param>
    /// <param name="action">
    /// The action to execute with the device selection and session.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the action completed; false if device selection failed.</returns>
    public static async Task<bool> WithSessionAsync<TSession>(
        Func<CancellationToken, Task<DeviceSelection?>> deviceSelector,
        Func<IYubiKey, CancellationToken, Task<TSession>> sessionFactory,
        Func<DeviceSelection, TSession, CancellationToken, Task> action,
        CancellationToken cancellationToken = default)
        where TSession : IAsyncDisposable
    {
        var selection = await deviceSelector(cancellationToken);
        if (selection is null)
        {
            OutputHelpers.WriteError("No YubiKey found.");
            return false;
        }

        OutputHelpers.WriteActiveDevice(selection.DisplayName);

        await using var session = await sessionFactory(selection.Device, cancellationToken);
        await action(selection, session, cancellationToken);
        return true;
    }

    /// <summary>
    /// Selects a device and executes an action with it, without creating a typed session.
    /// Useful for operations that only need a device reference (e.g., info queries).
    /// </summary>
    /// <param name="deviceSelector">
    /// A function that selects a device. Returns null if no device is available.
    /// </param>
    /// <param name="action">
    /// The action to execute with the selected device.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the action completed; false if device selection failed.</returns>
    public static async Task<bool> WithDeviceAsync(
        Func<CancellationToken, Task<DeviceSelection?>> deviceSelector,
        Func<DeviceSelection, CancellationToken, Task> action,
        CancellationToken cancellationToken = default)
    {
        var selection = await deviceSelector(cancellationToken);
        if (selection is null)
        {
            OutputHelpers.WriteError("No YubiKey found.");
            return false;
        }

        OutputHelpers.WriteActiveDevice(selection.DisplayName);

        await action(selection, cancellationToken);
        return true;
    }

    /// <summary>
    /// Reads a password from the console with masked input (no echo).
    /// Supports both interactive terminals and redirected input.
    /// </summary>
    /// <param name="prompt">The prompt text to display (default: "Password").</param>
    /// <returns>The entered password string.</returns>
    public static string ReadPasswordMasked(string prompt = "Password")
    {
        if (Console.IsInputRedirected)
        {
            return Console.ReadLine() ?? string.Empty;
        }

        Console.Error.Write($"{prompt}: ");
        var password = new List<char>();
        while (true)
        {
            var keyInfo = Console.ReadKey(intercept: true);
            if (keyInfo.Key is ConsoleKey.Enter)
            {
                break;
            }

            if (keyInfo.Key is ConsoleKey.Backspace && password.Count > 0)
            {
                password.RemoveAt(password.Count - 1);
            }
            else if (!char.IsControl(keyInfo.KeyChar))
            {
                password.Add(keyInfo.KeyChar);
            }
        }

        Console.Error.WriteLine();
        return new string(password.ToArray());
    }

    /// <summary>
    /// Prompts for a credential (PIN, password) using either the provided value
    /// or an interactive Spectre.Console secret prompt if no value was given.
    /// </summary>
    /// <param name="existingValue">A value from CLI flags, or null to prompt interactively.</param>
    /// <param name="label">The prompt label (e.g., "PIN", "Password").</param>
    /// <returns>The credential value.</returns>
    public static string PromptOrUse(string? existingValue, string label = "PIN")
    {
        if (existingValue is not null)
        {
            return existingValue;
        }

        return AnsiConsole.Prompt(
            new TextPrompt<string>($"{label}:")
                .Secret());
    }

    /// <summary>
    /// Executes an async operation with a Spectre.Console status spinner.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="statusMessage">The spinner status message (e.g., "Querying device...").</param>
    /// <param name="operation">The async operation to execute.</param>
    /// <returns>The result of the operation.</returns>
    public static async Task<T> WithStatusAsync<T>(
        string statusMessage,
        Func<Task<T>> operation) =>
        await AnsiConsole.Status()
            .StartAsync(statusMessage, async _ => await operation());
}
