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

namespace Yubico.YubiKit.Cli.Shared.Cli;

/// <summary>
/// Builds and runs an interactive Spectre.Console selection menu with a main loop.
/// Provides a fluent API for registering menu items that map display labels to async actions.
/// </summary>
/// <remarks>
/// <para>
/// This builder extracts the common interactive menu pattern used across CLI tools:
/// display numbered choices, accept user input, map to an async action, handle errors,
/// and loop until the user selects "Exit" or cancels with Ctrl+C.
/// </para>
/// <para>
/// The exit label is always appended as the last choice and causes the loop to end.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// await InteractiveMenuBuilder.Create("What would you like to do?")
///     .AddItem("Device Info", ct => DeviceInfoMenu.RunAsync(ct))
///     .AddItem("Factory Reset", ct => ResetMenu.RunAsync(ct))
///     .RunAsync(cancellationToken);
/// </code>
/// </example>
public sealed class InteractiveMenuBuilder
{
    private readonly string _title;
    private readonly List<(string Label, Func<CancellationToken, Task> Action)> _items = [];
    private int _pageSize = 15;
    private string _exitLabel = "Exit";
    private string _exitMessage = "[grey]Goodbye![/]";
    private Action<Exception>? _errorHandler;

    private InteractiveMenuBuilder(string title)
    {
        _title = title;
    }

    /// <summary>
    /// Creates a new <see cref="InteractiveMenuBuilder"/> with the given menu title.
    /// </summary>
    /// <param name="title">The prompt title displayed above the selection list.</param>
    /// <returns>A new builder instance.</returns>
    public static InteractiveMenuBuilder Create(string title) => new(title);

    /// <summary>
    /// Adds a menu item that maps a display label to an async action.
    /// Items are displayed in the order they are added.
    /// </summary>
    /// <param name="label">The display label shown to the user.</param>
    /// <param name="action">The async action to execute when this item is selected.</param>
    /// <returns>This builder for fluent chaining.</returns>
    public InteractiveMenuBuilder AddItem(string label, Func<CancellationToken, Task> action)
    {
        _items.Add((label, action));
        return this;
    }

    /// <summary>
    /// Sets the page size for the selection prompt (default: 15).
    /// </summary>
    /// <param name="pageSize">Maximum number of items visible at once.</param>
    /// <returns>This builder for fluent chaining.</returns>
    public InteractiveMenuBuilder WithPageSize(int pageSize)
    {
        _pageSize = pageSize;
        return this;
    }

    /// <summary>
    /// Sets the label for the exit choice (default: "Exit").
    /// </summary>
    /// <param name="label">The exit menu item label.</param>
    /// <returns>This builder for fluent chaining.</returns>
    public InteractiveMenuBuilder WithExitLabel(string label)
    {
        _exitLabel = label;
        return this;
    }

    /// <summary>
    /// Sets the farewell message displayed when exiting (default: "[grey]Goodbye![/]").
    /// The message supports Spectre.Console markup.
    /// </summary>
    /// <param name="message">The markup message to display on exit.</param>
    /// <returns>This builder for fluent chaining.</returns>
    public InteractiveMenuBuilder WithExitMessage(string message)
    {
        _exitMessage = message;
        return this;
    }

    /// <summary>
    /// Sets a custom error handler for exceptions thrown by menu actions.
    /// By default, errors are displayed using <c>[red]Error: message[/]</c>.
    /// </summary>
    /// <param name="handler">A callback invoked when an action throws.</param>
    /// <returns>This builder for fluent chaining.</returns>
    public InteractiveMenuBuilder WithErrorHandler(Action<Exception> handler)
    {
        _errorHandler = handler;
        return this;
    }

    /// <summary>
    /// Runs the interactive menu loop until the user selects the exit item or cancels.
    /// </summary>
    /// <param name="cancellationToken">Token to observe for cancellation (e.g., Ctrl+C).</param>
    /// <returns>Exit code 0 on normal exit.</returns>
    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        var choices = _items.Select(i => i.Label).Append(_exitLabel).ToList();
        var actionMap = new Dictionary<string, Func<CancellationToken, Task>>(_items.Count);
        foreach (var (label, action) in _items)
        {
            actionMap[label] = action;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            string choice;
            try
            {
                choice = await new SelectionPrompt<string>()
                    .Title(_title)
                    .PageSize(_pageSize)
                    .AddChoices(choices)
                    .ShowAsync(AnsiConsole.Console, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (string.Equals(choice, _exitLabel, StringComparison.Ordinal))
            {
                AnsiConsole.MarkupLine(_exitMessage);
                break;
            }

            if (actionMap.TryGetValue(choice, out var selectedAction))
            {
                try
                {
                    await selectedAction(cancellationToken);
                }
                catch (Exception ex)
                {
                    if (_errorHandler is not null)
                    {
                        _errorHandler(ex);
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[red]Error: {Markup.Escape(ex.Message)}[/]");
                    }
                }
            }

            AnsiConsole.WriteLine();
        }

        return 0;
    }
}
