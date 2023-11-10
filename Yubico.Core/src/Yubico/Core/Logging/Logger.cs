// Copyright 2021 Yubico AB
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

using System;
using Microsoft.Extensions.Logging;

namespace Yubico.Core.Logging
{
    /// <summary>
    /// A concrete logger implementation used by Yubico .NET-based libraries.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class builds on top of the standard <see cref="ILogger"/> interface used by the Microsoft.Extensions logging
    /// library. This is a meta-library for interoperating with different concrete logging implementations such as NLog,
    /// Serilog, or .NET's built in EventPipe system.
    /// </para>
    /// <para>
    /// Methods for logging potentially sensitive information are present. These methods are disabled for Release builds,
    /// resulting in a no-op for anything other than a Debug build of this library.
    /// </para>
    /// <para>
    /// Extension methods can be used to add further conveniences to the logging interface. For example, if you wanted to
    /// log a platform error code in a uniform way, you could introduce a `LogPlatformError` extension that takes care of
    /// formatting the error and calling one of the existing log methods.
    /// </para>
    /// </remarks>
    public sealed class Logger : ILogger
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Constructs a new instance of the <see cref="Logger"/> class.
        /// </summary>
        /// <param name="logger">
        /// An instance of the concrete logging mechanism. This should be constructed using the <see cref="Log"/> class,
        /// and not called directly.
        /// </param>
        internal Logger(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Writes an entry to the log.
        /// </summary>
        /// <param name="logLevel">
        /// The severity or log level for this log entry. See <see cref="Microsoft.Extensions.Logging.LogLevel"/> for
        /// more information.
        /// </param>
        /// <param name="eventId">
        /// An optional event identifier. Use this field if you want the event to be easily consumable by automated tooling.
        /// You can use this as a unique identifier for this specific type of event so that your tools can act on them.
        /// </param>
        /// <param name="state">
        /// The message or object that you wish to add to the log.
        /// </param>
        /// <param name="exception">
        /// An additional, optional, place to log exceptions that are associated with the log state.
        /// </param>
        /// <param name="formatter">
        /// A function that can take the data specified by the <paramref name="state"/> parameter and transform it into
        /// a string representation.
        /// </param>
        /// <typeparam name="TState">
        /// The type of the object being logged. In many cases this will be a string, but some logger implementations
        /// support rich object support (called "structured logging").
        /// </typeparam>
        /// <remarks>
        /// <para>
        /// This method will write an entry to the log that this instance was opened against. The Yubico SDK does not
        /// control the concrete implementation of the log. It defers to the logging provider(s) that the application
        /// has set up. See <see cref="Yubico.Core.Logging.Log.LoggerFactory"/> for more information on how to register
        /// a concrete logger with the SDK.
        /// </para>
        /// </remarks>
        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            _logger.Log(logLevel, eventId, state, exception, formatter);

        /// <summary>
        /// Checks whether the given `logLevel` has been enabled by the log provider.
        /// </summary>
        /// <param name="logLevel">
        /// The log level you wish to check.
        /// </param>
        /// <returns>
        /// `true` if enabled; `false` otherwise.
        /// </returns>
        /// <remarks>
        /// Sometimes you may wish to run additional code to gather extra diagnostics for your log. But, since this is
        /// extra work, you likely don't want to always run this code. You only want to run it when you know there is a
        /// log provider ready to consume it. You can use this method to first test to see if this log level is enabled
        /// before running the extra diagnostics code.
        /// </remarks>
        public bool IsEnabled(LogLevel logLevel) => _logger.IsEnabled(logLevel);

        /// <summary>
        /// Begins a logical operation scope to group log messages together.
        /// </summary>
        /// <param name="state">
        /// The identifier for the scope, usually a string.
        /// </param>
        /// <typeparam name="TState">
        /// The type of the state being logged as the beginning of a scope. Usually a string.
        /// </typeparam>
        /// <returns>
        /// A disposable object that ends the logical operation scope on dispose.
        /// </returns>
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => _logger.BeginScope(state);
    }
}
