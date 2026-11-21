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

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Yubico.YubiKit.Core;

/// <summary>Configures the factory used to create SDK loggers.</summary>
/// <remarks>
/// Configure logging before first SDK use. Replacing the factory affects newly created loggers;
/// SDK static logger fields keep previously created instances. The application owns supplied factories and must
/// keep them alive while their loggers are in use. This class neither disposes factories nor redacts
/// payloads or exception text; choose providers, levels, and retention appropriate to their contents.
/// </remarks>
public static class YubiKitLogging
{
    private static readonly Lock _lock = new();

    /// <summary>Gets or sets the process-wide factory for subsequently created loggers.</summary>
    public static ILoggerFactory LoggerFactory
    {
        get
        {
            lock (_lock)
                return field;
        }
        set
        {
            lock (_lock)
                field = value;
        }
    } = NullLoggerFactory.Instance;

    internal static ILogger<T> CreateLogger<T>() => LoggerFactory.CreateLogger<T>();
    internal static ILogger CreateLogger(string categoryName) => LoggerFactory.CreateLogger(categoryName);

    /// <summary>
    /// Configures YubiKit logging with the provided logger factory.
    /// Call this method once at application startup before using SDK services.
    /// </summary>
    /// <param name="loggerFactory">The logger factory to use, or null for no logging.</param>
    public static void Configure(ILoggerFactory? loggerFactory) =>
        LoggerFactory = loggerFactory ?? NullLoggerFactory.Instance;

    /// <summary>
    /// Temporarily replaces the process-wide factory and restores the captured factory on disposal.
    /// </summary>
    /// <remarks>
    /// Use only with coordinated, non-overlapping scopes. Restoration does not reconfigure existing
    /// loggers or dispose either factory; concurrent replacement or out-of-order disposal can restore
    /// a stale factory.
    /// </remarks>
    public static IDisposable UseTemporary(ILoggerFactory factory)
    {
        var original = LoggerFactory;
        LoggerFactory = factory;
        return new RestoreDisposable(() => LoggerFactory = original);
    }

    private sealed class RestoreDisposable(Action restore) : IDisposable
    {
        private Action? _restore = restore;

        public void Dispose()
        {
            _restore?.Invoke();
            _restore = null;
        }
    }
}