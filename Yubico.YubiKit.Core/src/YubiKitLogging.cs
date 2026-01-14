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

public static class YubiKitLogging
{
    private static ILoggerFactory _loggerFactory = NullLoggerFactory.Instance;
    private static readonly object _lock = new();

    public static ILoggerFactory LoggerFactory
    {
        get
        {
            lock (_lock)
                return _loggerFactory;
        }
        set
        {
            lock (_lock)
                _loggerFactory = value ?? NullLoggerFactory.Instance;
        }
    }

    internal static ILogger<T> CreateLogger<T>() => LoggerFactory.CreateLogger<T>();
    internal static ILogger CreateLogger(string categoryName) => LoggerFactory.CreateLogger(categoryName);

    /// <summary>
    /// Temporarily replaces the LoggerFactory. Dispose to restore. Useful for testing.
    /// </summary>
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

public sealed class YubiKitLoggingInitializer
{
    public YubiKitLoggingInitializer(ILoggerFactory? loggerFactory = null) =>
        YubiKitLogging.LoggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
}
