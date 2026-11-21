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

namespace Yubico.YubiKit.Cli.Shared.Cli;

/// <summary>
/// Provides CancellationTokenSource setup for console commands.
/// Shared boilerplate for CLI tool entry points.
/// </summary>
public static class CommandHelper
{
    /// <summary>
    /// Sets up a <see cref="CancellationTokenSource"/> that cancels on Ctrl+C.
    /// </summary>
    /// <returns>A <see cref="CancellationTokenSource"/> that detaches from Console.CancelKeyPress on disposal.</returns>
    public static CancellationTokenSource CreateConsoleCts() => new ConsoleCancellationTokenSource();

    private sealed class ConsoleCancellationTokenSource : CancellationTokenSource
    {
        public ConsoleCancellationTokenSource() => Console.CancelKeyPress += OnCancelKeyPress;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                Console.CancelKeyPress -= OnCancelKeyPress;
            base.Dispose(disposing);
        }

        private void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs args)
        {
            try
            {
                Cancel();
                args.Cancel = true;
            }
            catch (ObjectDisposedException)
            {
                // A notification already captured by Console can race with detachment.
            }
        }
    }

}