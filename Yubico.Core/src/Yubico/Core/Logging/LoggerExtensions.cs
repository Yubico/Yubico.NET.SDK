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
    /// Logger extension methods for common scenarios.
    /// </summary>

    // These extension methods include both the standard extensions introduced by the Microsoft ILogger APIs, as well
    // as some new methods for logging potentially sensitive messages. We could have just introduced the sensitive log
    // methods and re-used the extensions from Microsoft.Extensions.Logging. However, combining them here allows users
    // of Yubico.Core.Logging to only have to use one namespace (Yubico.Core.Logging), instead of two (Microsoft.Extensions.Logging
    // as the second). I'm duplicating some code/API/effort in one file to avoid having to over-include in many hundreds
    // of files. Files that have to include both namespaces should then hopefully be exceptional cases.
    public static class LoggerExtensions
    {
        // Needed to satisfy compiling out the sensitive logs in Release builds.
        private static void NoOp(params object?[] _) { }

        //------------------------------------------DEBUG------------------------------------------//
        /// <summary>
        /// Formats and writes a debug log message.
        /// </summary>
        /// <param name="logger">The <see cref="Logger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">
        ///     Format string of the log message in message template format.
        ///     Example: <c>"User {User} logged in from {Address}"</c>
        /// </param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogDebug(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogDebug(this Logger logger, EventId eventId, Exception exception, string message, params object?[] args) =>
            ((ILogger)logger).LogDebug(eventId, exception, message, args);

        /// <summary>
        /// Formats and writes a debug log message that contains potentially sensitive information.
        /// </summary>
        /// <param name="logger">The <see cref="Logger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">
        ///     Format string of the log message in message template format.
        ///     Example: <c>"User {User} logged in from {Address}"</c>
        /// </param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogDebug(0, exception, "Error while processing request from {Address}", address)</example>
        public static void SensitiveLogDebug(this Logger logger, EventId eventId, Exception exception, string message, params object?[] args) =>
#if ENABLE_SENSITIVE_LOG
            ((ILogger)logger).LogDebug(eventId, exception, message, args);
#else
            NoOp(eventId, exception, message, args);
#endif

        /// <summary>
        /// Formats and writes a debug log message.
        /// </summary>
        /// <param name="logger">The <see cref="Logger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="message">
        ///     Format string of the log message in message template format.
        ///     Example: <c>"User {User} logged in from {Address}"</c>
        /// </param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogDebug(0, "Processing request from {Address}", address)</example>
        public static void LogDebug(this Logger logger, EventId eventId, string message, params object?[] args) =>
            ((ILogger)logger).LogDebug(eventId, message, args);

        /// <summary>
        /// Formats and writes a debug log message that contains potentially sensitive information.
        /// </summary>
        /// <param name="logger">The <see cref="Logger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="message">
        ///     Format string of the log message in message template format.
        ///     Example: <c>"User {User} logged in from {Address}"</c>
        /// </param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogDebug(0, "Processing request from {Address}", address)</example>
        public static void SensitiveLogDebug(this Logger logger, EventId eventId, string message, params object?[] args) =>
#if ENABLE_SENSITIVE_LOG
            ((ILogger)logger).LogDebug(eventId, message, args);
#else
            NoOp(eventId, message, args);
#endif

        /// <summary>
        /// Formats and writes a debug log message.
        /// </summary>
        /// <param name="logger">The <see cref="Logger"/> to write to.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">
        ///     Format string of the log message in message template format.
        ///     Example: <c>"User {User} logged in from {Address}"</c>
        /// </param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogDebug(exception, "Error while processing request from {Address}", address)</example>
        public static void LogDebug(this Logger logger, Exception exception, string message, params object?[] args) =>
            ((ILogger)logger).LogDebug(exception, message, args);

        /// <summary>
        /// Formats and writes a debug log message that contains potentially sensitive information.
        /// </summary>
        /// <param name="logger">The <see cref="Logger"/> to write to.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">
        ///     Format string of the log message in message template format.
        ///     Example: <c>"User {User} logged in from {Address}"</c>
        /// </param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogDebug(exception, "Error while processing request from {Address}", address)</example>
        public static void SensitiveLogDebug(this Logger logger, Exception exception, string message, params object?[] args) =>
#if ENABLE_SENSITIVE_LOG
            ((ILogger)logger).LogDebug(exception, message, args);
#else
            NoOp(exception, message, args);
#endif

        /// <summary>
        /// Formats and writes a debug log message.
        /// </summary>
        /// <param name="logger">The <see cref="Logger"/> to write to.</param>
        /// <param name="message">
        ///     Format string of the log message in message template format.
        ///     Example: <c>"User {User} logged in from {Address}"</c>
        /// </param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogDebug("Processing request from {Address}", address)</example>
        public static void LogDebug(this Logger logger, string message, params object?[] args) =>
            ((ILogger)logger).LogDebug(message, args);

        /// <summary>
        /// Formats and writes a debug log message that contains potentially sensitive information.
        /// </summary>
        /// <param name="logger">The <see cref="Logger"/> to write to.</param>
        /// <param name="message">
        ///     Format string of the log message in message template format.
        ///     Example: <c>"User {User} logged in from {Address}"</c>
        /// </param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogDebug("Processing request from {Address}", address)</example>
        public static void SensitiveLogDebug(this Logger logger, string message, params object?[] args) =>
#if ENABLE_SENSITIVE_LOG
            ((ILogger)logger).LogDebug(message, args);
#else
            NoOp(message, args);
#endif

        //------------------------------------------TRACE------------------------------------------//

        /// <summary>
        /// Formats and writes a trace log message.
        /// </summary>
        /// <param name="logger">The <see cref="Logger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">
        ///     Format string of the log message in message template format.
        ///     Example: <c>"User {User} logged in from {Address}"</c>
        /// </param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogTrace(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogTrace(this Logger logger, EventId eventId, Exception exception, string message, params object?[] args) =>
            ((ILogger)logger).LogTrace(eventId, exception, message, args);

        /// <summary>
        /// Formats and writes a trace log message that contains potentially sensitive information.
        /// </summary>
        /// <param name="logger">The <see cref="Logger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">
        ///     Format string of the log message in message template format.
        ///     Example: <c>"User {User} logged in from {Address}"</c>
        /// </param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogTrace(0, exception, "Error while processing request from {Address}", address)</example>
        public static void SensitiveLogTrace(this Logger logger, EventId eventId, Exception exception, string message, params object?[] args) =>
#if ENABLE_SENSITIVE_LOG
            ((ILogger)logger).LogTrace(eventId, exception, message, args);
#else
            NoOp(eventId, exception, message, args);
#endif

        /// <summary>
        /// Formats and writes a trace log message.
        /// </summary>
        /// <param name="logger">The <see cref="Logger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="message">
        ///     Format string of the log message in message template format.
        ///     Example: <c>"User {User} logged in from {Address}"</c>
        /// </param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogTrace(0, "Processing request from {Address}", address)</example>
        public static void LogTrace(this Logger logger, EventId eventId, string message, params object?[] args) =>
            ((ILogger)logger).LogTrace(eventId, message, args);

        /// <summary>
        /// Formats and writes a trace log message that contains potentially sensitive information.
        /// </summary>
        /// <param name="logger">The <see cref="Logger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="message">
        ///     Format string of the log message in message template format.
        ///     Example: <c>"User {User} logged in from {Address}"</c>
        /// </param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogTrace(0, "Processing request from {Address}", address)</example>
        public static void SensitiveLogTrace(this Logger logger, EventId eventId, string message, params object?[] args) =>
#if ENABLE_SENSITIVE_LOG
            ((ILogger)logger).LogTrace(eventId, message, args);
#else
            NoOp(eventId, message, args);
#endif

        /// <summary>
        /// Formats and writes a trace log message.
        /// </summary>
        /// <param name="logger">The <see cref="Logger"/> to write to.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">
        ///     Format string of the log message in message template format.
        ///     Example: <c>"User {User} logged in from {Address}"</c>
        /// </param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogTrace(exception, "Error while processing request from {Address}", address)</example>
        public static void LogTrace(this Logger logger, Exception exception, string message, params object?[] args) =>
            ((ILogger)logger).LogTrace(exception, message, args);

        /// <summary>
        /// Formats and writes a trace log message that contains potentially sensitive information.
        /// </summary>
        /// <param name="logger">The <see cref="Logger"/> to write to.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">
        ///     Format string of the log message in message template format.
        ///     Example: <c>"User {User} logged in from {Address}"</c>
        /// </param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogTrace(exception, "Error while processing request from {Address}", address)</example>
        public static void SensitiveLogTrace(this Logger logger, Exception exception, string message, params object?[] args) =>
#if ENABLE_SENSITIVE_LOG
            ((ILogger)logger).LogTrace(exception, message, args);
#else
            NoOp(exception, message, args);
#endif

        /// <summary>
        /// Formats and writes a trace log message.
        /// </summary>
        /// <param name="logger">The <see cref="Logger"/> to write to.</param>
        /// <param name="message">
        ///     Format string of the log message in message template format.
        ///     Example: <c>"User {User} logged in from {Address}"</c>
        /// </param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogTrace("Processing request from {Address}", address)</example>
        public static void LogTrace(this Logger logger, string message, params object?[] args) =>
            ((ILogger)logger).LogTrace(message, args);

        /// <summary>
        /// Formats and writes a trace log message that contains potentially sensitive information.
        /// </summary>
        /// <param name="logger">The <see cref="Logger"/> to write to.</param>
        /// <param name="message">
        ///     Format string of the log message in message template format.
        ///     Example: <c>"User {User} logged in from {Address}"</c>
        /// </param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogTrace("Processing request from {Address}", address)</example>
        public static void SensitiveLogTrace(this Logger logger, string message, params object?[] args) =>
#if ENABLE_SENSITIVE_LOG
            ((ILogger)logger).LogTrace(message, args);
#else
            NoOp(message, args);
#endif

        //------------------------------------------INFORMATION------------------------------------------//

        /// <summary>
        /// Formats and writes an informational log message.
        /// </summary>
        /// <param name="logger">The <see cref="Logger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">
        ///     Format string of the log message in message template format.
        ///     Example: <c>"User {User} logged in from {Address}"</c>
        /// </param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogInformation(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogInformation(this Logger logger, EventId eventId, Exception exception, string message, params object?[] args) =>
            ((ILogger)logger).LogInformation(eventId, exception, message, args);

        /// <summary>
        /// Formats and writes an informational log message that contains potentially sensitive information.
        /// </summary>
        /// <param name="logger">The <see cref="Logger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">
        ///     Format string of the log message in message template format.
        ///     Example: <c>"User {User} logged in from {Address}"</c>
        /// </param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogInformation(0, exception, "Error while processing request from {Address}", address)</example>
        public static void SensitiveLogInformation(this Logger logger, EventId eventId, Exception exception, string message, params object?[] args) =>
#if ENABLE_SENSITIVE_LOG
            ((ILogger)logger).LogInformation(eventId, exception, message, args);
#else
            NoOp(eventId, exception, message, args);
#endif

        /// <summary>
        /// Formats and writes an informational log message.
        /// </summary>
        /// <param name="logger">The <see cref="Logger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="message">
        ///     Format string of the log message in message template format.
        ///     Example: <c>"User {User} logged in from {Address}"</c>
        /// </param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogInformation(0, "Processing request from {Address}", address)</example>
        public static void LogInformation(this Logger logger, EventId eventId, string message, params object?[] args) =>
            ((ILogger)logger).LogInformation(eventId, message, args);

        /// <summary>
        /// Formats and writes an informational log message that contains potentially sensitive information.
        /// </summary>
        /// <param name="logger">The <see cref="Logger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="message">
        ///     Format string of the log message in message template format.
        ///     Example: <c>"User {User} logged in from {Address}"</c>
        /// </param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogInformation(0, "Processing request from {Address}", address)</example>
        public static void SensitiveLogInformation(this Logger logger, EventId eventId, string message, params object?[] args) =>
#if ENABLE_SENSITIVE_LOG
            ((ILogger)logger).LogInformation(eventId, message, args);
#else
            NoOp(eventId, message, args);
#endif

        /// <summary>
        /// Formats and writes an informational log message.
        /// </summary>
        /// <param name="logger">The <see cref="Logger"/> to write to.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">
        ///     Format string of the log message in message template format.
        ///     Example: <c>"User {User} logged in from {Address}"</c>
        /// </param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogInformation(exception, "Error while processing request from {Address}", address)</example>
        public static void LogInformation(this Logger logger, Exception exception, string message, params object?[] args) =>
            ((ILogger)logger).LogInformation(exception, message, args);

        /// <summary>
        /// Formats and writes an informational log message that contains potentially sensitive information.
        /// </summary>
        /// <param name="logger">The <see cref="Logger"/> to write to.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">
        ///     Format string of the log message in message template format.
        ///     Example: <c>"User {User} logged in from {Address}"</c>
        /// </param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogInformation(exception, "Error while processing request from {Address}", address)</example>
        public static void SensitiveLogInformation(this Logger logger, Exception exception, string message, params object?[] args) =>
#if ENABLE_SENSITIVE_LOG
            ((ILogger)logger).LogInformation(exception, message, args);
#else
            NoOp(exception, message, args);
#endif

        /// <summary>
        /// Formats and writes an informational log message.
        /// </summary>
        /// <param name="logger">The <see cref="Logger"/> to write to.</param>
        /// <param name="message">
        ///     Format string of the log message in message template format.
        ///     Example: <c>"User {User} logged in from {Address}"</c>
        /// </param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogInformation("Processing request from {Address}", address)</example>
        public static void LogInformation(this Logger logger, string message, params object?[] args) =>
            ((ILogger)logger).LogInformation(message, args);

        /// <summary>
        /// Formats and writes an informational log message that contains potentially sensitive information.
        /// </summary>
        /// <param name="logger">The <see cref="Logger"/> to write to.</param>
        /// <param name="message">
        ///     Format string of the log message in message template format.
        ///     Example: <c>"User {User} logged in from {Address}"</c>
        /// </param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogInformation("Processing request from {Address}", address)</example>
        public static void SensitiveLogInformation(this Logger logger, string message, params object?[] args) =>
#if ENABLE_SENSITIVE_LOG
            ((ILogger)logger).LogInformation(message, args);
#else
            NoOp(message, args);
#endif

        //------------------------------------------WARNING------------------------------------------//

        /// <summary>
        /// Formats and writes a warning log message.
        /// </summary>
        /// <param name="logger">The <see cref="Logger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">
        ///     Format string of the log message in message template format.
        ///     Example: <c>"User {User} logged in from {Address}"</c>
        /// </param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogWarning(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogWarning(this Logger logger, EventId eventId, Exception exception, string message, params object?[] args) =>
            ((ILogger)logger).LogWarning(eventId, exception, message, args);

        /// <summary>
        /// Formats and writes a warning log message that contains potentially sensitive information.
        /// </summary>
        /// <param name="logger">The <see cref="Logger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">
        ///     Format string of the log message in message template format.
        ///     Example: <c>"User {User} logged in from {Address}"</c>
        /// </param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogWarning(0, exception, "Error while processing request from {Address}", address)</example>
        public static void SensitiveLogWarning(this Logger logger, EventId eventId, Exception exception, string message, params object?[] args) =>
#if ENABLE_SENSITIVE_LOG
            ((ILogger)logger).LogWarning(eventId, exception, message, args);
#else
            NoOp(eventId, exception, message, args);
#endif

        /// <summary>
        /// Formats and writes a warning log message.
        /// </summary>
        /// <param name="logger">The <see cref="Logger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="message">
        ///     Format string of the log message in message template format.
        ///     Example: <c>"User {User} logged in from {Address}"</c>
        /// </param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogWarning(0, "Processing request from {Address}", address)</example>
        public static void LogWarning(this Logger logger, EventId eventId, string message, params object?[] args) =>
            ((ILogger)logger).LogWarning(eventId, message, args);

        /// <summary>
        /// Formats and writes a warning log message that contains potentially sensitive information.
        /// </summary>
        /// <param name="logger">The <see cref="Logger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="message">
        ///     Format string of the log message in message template format.
        ///     Example: <c>"User {User} logged in from {Address}"</c>
        /// </param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogWarning(0, "Processing request from {Address}", address)</example>
        public static void SensitiveLogWarning(this Logger logger, EventId eventId, string message, params object?[] args) =>
#if ENABLE_SENSITIVE_LOG
            ((ILogger)logger).LogWarning(eventId, message, args);
#else
            NoOp(eventId, message, args);
#endif

        /// <summary>
        /// Formats and writes a warning log message.
        /// </summary>
        /// <param name="logger">The <see cref="Logger"/> to write to.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">
        ///     Format string of the log message in message template format.
        ///     Example: <c>"User {User} logged in from {Address}"</c>
        /// </param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogWarning(exception, "Error while processing request from {Address}", address)</example>
        public static void LogWarning(this Logger logger, Exception exception, string message, params object?[] args) =>
            ((ILogger)logger).LogWarning(exception, message, args);

        /// <summary>
        /// Formats and writes a warning log message that contains potentially sensitive information.
        /// </summary>
        /// <param name="logger">The <see cref="Logger"/> to write to.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">
        ///     Format string of the log message in message template format.
        ///     Example: <c>"User {User} logged in from {Address}"</c>
        /// </param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogWarning(exception, "Error while processing request from {Address}", address)</example>
        public static void SensitiveLogWarning(this Logger logger, Exception exception, string message, params object?[] args) =>
#if ENABLE_SENSITIVE_LOG
            ((ILogger)logger).LogWarning(exception, message, args);
#else
            NoOp(exception, message, args);
#endif

        /// <summary>
        /// Formats and writes a warning log message.
        /// </summary>
        /// <param name="logger">The <see cref="Logger"/> to write to.</param>
        /// <param name="message">
        ///     Format string of the log message in message template format.
        ///     Example: <c>"User {User} logged in from {Address}"</c>
        /// </param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogWarning("Processing request from {Address}", address)</example>
        public static void LogWarning(this Logger logger, string message, params object?[] args) =>
            ((ILogger)logger).LogWarning(message, args);

        /// <summary>
        /// Formats and writes a warning log message that contains potentially sensitive information.
        /// </summary>
        /// <param name="logger">The <see cref="Logger"/> to write to.</param>
        /// <param name="message">
        ///     Format string of the log message in message template format.
        ///     Example: <c>"User {User} logged in from {Address}"</c>
        /// </param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogWarning("Processing request from {Address}", address)</example>
        public static void SensitiveLogWarning(this Logger logger, string message, params object?[] args) =>
#if ENABLE_SENSITIVE_LOG
            ((ILogger)logger).LogWarning(message, args);
#else
            NoOp(message, args);
#endif

        //------------------------------------------ERROR------------------------------------------//

        /// <summary>
        /// Formats and writes an error log message.
        /// </summary>
        /// <param name="logger">The <see cref="Logger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">
        ///     Format string of the log message in message template format.
        ///     Example: <c>"User {User} logged in from {Address}"</c>
        /// </param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogError(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogError(this Logger logger, EventId eventId, Exception exception, string message, params object?[] args) =>
            ((ILogger)logger).LogError(eventId, exception, message, args);

        /// <summary>
        /// Formats and writes an error log message that contains potentially sensitive information.
        /// </summary>
        /// <param name="logger">The <see cref="Logger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">
        ///     Format string of the log message in message template format.
        ///     Example: <c>"User {User} logged in from {Address}"</c>
        /// </param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogError(0, exception, "Error while processing request from {Address}", address)</example>
        public static void SensitiveLogError(this Logger logger, EventId eventId, Exception exception, string message, params object?[] args) =>
#if ENABLE_SENSITIVE_LOG
            ((ILogger)logger).LogError(eventId, exception, message, args);
#else
            NoOp(eventId, exception, message, args);
#endif

        /// <summary>
        /// Formats and writes an error log message.
        /// </summary>
        /// <param name="logger">The <see cref="Logger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="message">
        ///     Format string of the log message in message template format.
        ///     Example: <c>"User {User} logged in from {Address}"</c>
        /// </param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogError(0, "Processing request from {Address}", address)</example>
        public static void LogError(this Logger logger, EventId eventId, string message, params object?[] args) =>
            ((ILogger)logger).LogError(eventId, message, args);

        /// <summary>
        /// Formats and writes an error log message that contains potentially sensitive information.
        /// </summary>
        /// <param name="logger">The <see cref="Logger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="message">
        ///     Format string of the log message in message template format.
        ///     Example: <c>"User {User} logged in from {Address}"</c>
        /// </param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogError(0, "Processing request from {Address}", address)</example>
        public static void SensitiveLogError(this Logger logger, EventId eventId, string message, params object?[] args) =>
#if ENABLE_SENSITIVE_LOG
            ((ILogger)logger).LogError(eventId, message, args);
#else
            NoOp(eventId, message, args);
#endif

        /// <summary>
        /// Formats and writes an error log message.
        /// </summary>
        /// <param name="logger">The <see cref="Logger"/> to write to.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">
        ///     Format string of the log message in message template format.
        ///     Example: <c>"User {User} logged in from {Address}"</c>
        /// </param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogError(exception, "Error while processing request from {Address}", address)</example>
        public static void LogError(this Logger logger, Exception exception, string message, params object?[] args) =>
            ((ILogger)logger).LogError(exception, message, args);

        /// <summary>
        /// Formats and writes an error log message that contains potentially sensitive information.
        /// </summary>
        /// <param name="logger">The <see cref="Logger"/> to write to.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">
        ///     Format string of the log message in message template format.
        ///     Example: <c>"User {User} logged in from {Address}"</c>
        /// </param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogError(exception, "Error while processing request from {Address}", address)</example>
        public static void SensitiveLogError(this Logger logger, Exception exception, string message, params object?[] args) =>
#if ENABLE_SENSITIVE_LOG
            ((ILogger)logger).LogError(exception, message, args);
#else
            NoOp(exception, message, args);
#endif

        /// <summary>
        /// Formats and writes an error log message.
        /// </summary>
        /// <param name="logger">The <see cref="Logger"/> to write to.</param>
        /// <param name="message">
        ///     Format string of the log message in message template format.
        ///     Example: <c>"User {User} logged in from {Address}"</c>
        /// </param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogError("Processing request from {Address}", address)</example>
        public static void LogError(this Logger logger, string message, params object?[] args) =>
            ((ILogger)logger).LogError(message, args);

        /// <summary>
        /// Formats and writes an error log message that contains potentially sensitive information.
        /// </summary>
        /// <param name="logger">The <see cref="Logger"/> to write to.</param>
        /// <param name="message">
        ///     Format string of the log message in message template format.
        ///     Example: <c>"User {User} logged in from {Address}"</c>
        /// </param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogError("Processing request from {Address}", address)</example>
        public static void SensitiveLogError(this Logger logger, string message, params object?[] args) =>
#if ENABLE_SENSITIVE_LOG
            ((ILogger)logger).LogError(message, args);
#else
            NoOp(message, args);
#endif

        //------------------------------------------CRITICAL------------------------------------------//

        /// <summary>
        /// Formats and writes a critical log message.
        /// </summary>
        /// <param name="logger">The <see cref="Logger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">
        ///     Format string of the log message in message template format.
        ///     Example: <c>"User {User} logged in from {Address}"</c>
        /// </param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogCritical(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogCritical(this Logger logger, EventId eventId, Exception exception, string message, params object?[] args) =>
            ((ILogger)logger).LogCritical(eventId, exception, message, args);

        /// <summary>
        /// Formats and writes a critical log message that contains potentially sensitive information.
        /// </summary>
        /// <param name="logger">The <see cref="Logger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">
        ///     Format string of the log message in message template format.
        ///     Example: <c>"User {User} logged in from {Address}"</c>
        /// </param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogCritical(0, exception, "Error while processing request from {Address}", address)</example>
        public static void SensitiveLogCritical(this Logger logger, EventId eventId, Exception exception, string message, params object?[] args) =>
#if ENABLE_SENSITIVE_LOG
            ((ILogger)logger).LogCritical(eventId, exception, message, args);
#else
            NoOp(eventId, exception, message, args);
#endif

        /// <summary>
        /// Formats and writes a critical log message.
        /// </summary>
        /// <param name="logger">The <see cref="Logger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="message">
        ///     Format string of the log message in message template format.
        ///     Example: <c>"User {User} logged in from {Address}"</c>
        /// </param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogCritical(0, "Processing request from {Address}", address)</example>
        public static void LogCritical(this Logger logger, EventId eventId, string message, params object?[] args) =>
            ((ILogger)logger).LogCritical(eventId, message, args);

        /// <summary>
        /// Formats and writes a critical log message that contains potentially sensitive information.
        /// </summary>
        /// <param name="logger">The <see cref="Logger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="message">
        ///     Format string of the log message in message template format.
        ///     Example: <c>"User {User} logged in from {Address}"</c>
        /// </param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogCritical(0, "Processing request from {Address}", address)</example>
        public static void SensitiveLogCritical(this Logger logger, EventId eventId, string message, params object?[] args) =>
#if ENABLE_SENSITIVE_LOG
            ((ILogger)logger).LogCritical(eventId, message, args);
#else
            NoOp(eventId, message, args);
#endif

        /// <summary>
        /// Formats and writes a critical log message.
        /// </summary>
        /// <param name="logger">The <see cref="Logger"/> to write to.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">
        ///     Format string of the log message in message template format.
        ///     Example: <c>"User {User} logged in from {Address}"</c>
        /// </param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogCritical(exception, "Error while processing request from {Address}", address)</example>
        public static void LogCritical(this Logger logger, Exception exception, string message, params object?[] args) =>
            ((ILogger)logger).LogCritical(exception, message, args);

        /// <summary>
        /// Formats and writes a critical log message that contain potentially sensitive information.
        /// </summary>
        /// <param name="logger">The <see cref="Logger"/> to write to.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">
        ///     Format string of the log message in message template format.
        ///     Example: <c>"User {User} logged in from {Address}"</c>
        /// </param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogCritical(exception, "Error while processing request from {Address}", address)</example>
        public static void SensitiveLogCritical(this Logger logger, Exception exception, string message, params object?[] args) =>
#if ENABLE_SENSITIVE_LOG
            ((ILogger)logger).LogCritical(exception, message, args);
#else
            NoOp(exception, message, args);
#endif

        /// <summary>
        /// Formats and writes a critical log message.
        /// </summary>
        /// <param name="logger">The <see cref="Logger"/> to write to.</param>
        /// <param name="message">
        ///     Format string of the log message in message template format.
        ///     Example: <c>"User {User} logged in from {Address}"</c>
        /// </param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogCritical("Processing request from {Address}", address)</example>
        public static void LogCritical(this Logger logger, string message, params object?[] args) =>
            ((ILogger)logger).LogCritical(message, args);

        /// <summary>
        /// Formats and writes a critical log message that contains potentially sensitive information.
        /// </summary>
        /// <param name="logger">The <see cref="Logger"/> to write to.</param>
        /// <param name="message">
        ///     Format string of the log message in message template format.
        ///     Example: <c>"User {User} logged in from {Address}"</c>
        /// </param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogCritical("Processing request from {Address}", address)</example>
        public static void SensitiveLogCritical(this Logger logger, string message, params object?[] args) =>
#if ENABLE_SENSITIVE_LOG
            ((ILogger)logger).LogCritical(message, args);
#else
            NoOp(message, args);
#endif

        //--------------------------------------------LOG---------------------------------------------//

        /// <summary>
        /// Formats and writes a log message at the specified log level.
        /// </summary>
        /// <param name="logger">The <see cref="Logger"/> to write to.</param>
        /// <param name="logLevel">Entry will be written on this level.</param>
        /// <param name="message">Format string of the log message.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        public static void Log(this Logger logger, LogLevel logLevel, string message, params object?[] args) =>
            ((ILogger)logger).Log(logLevel, 0, null, message, args);

        /// <summary>
        /// Formats and writes a log message that may contain sensitive information at the specified log level.
        /// </summary>
        /// <param name="logger">The <see cref="Logger"/> to write to.</param>
        /// <param name="logLevel">Entry will be written on this level.</param>
        /// <param name="message">Format string of the log message.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        public static void SensitiveLog(this Logger logger, LogLevel logLevel, string message, params object?[] args) =>
#if ENABLE_SENSITIVE_LOG
            ((ILogger)logger).Log(logLevel, 0, null, message, args);
#else
            NoOp(logLevel, message, args);
#endif

        /// <summary>
        /// Formats and writes a log message at the specified log level.
        /// </summary>
        /// <param name="logger">The <see cref="Logger"/> to write to.</param>
        /// <param name="logLevel">Entry will be written on this level.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="message">Format string of the log message.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        public static void Log(this Logger logger, LogLevel logLevel, EventId eventId, string message, params object?[] args) =>
            ((ILogger)logger).Log(logLevel, eventId, null, message, args);

        /// <summary>
        /// Formats and writes a log message that may contain sensitive information at the specified log level.
        /// </summary>
        /// <param name="logger">The <see cref="Logger"/> to write to.</param>
        /// <param name="logLevel">Entry will be written on this level.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="message">Format string of the log message.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        public static void SensitiveLog(this Logger logger, LogLevel logLevel, EventId eventId, string message, params object?[] args) =>
#if ENABLE_SENSITIVE_LOG
            ((ILogger)logger).Log(logLevel, eventId, null, message, args);
#else
            NoOp(logLevel, eventId, message, args);
#endif

        /// <summary>
        /// Formats and writes a log message at the specified log level.
        /// </summary>
        /// <param name="logger">The <see cref="Logger"/> to write to.</param>
        /// <param name="logLevel">Entry will be written on this level.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        public static void Log(this Logger logger, LogLevel logLevel, Exception exception, string message, params object?[] args) =>
            ((ILogger)logger).Log(logLevel, 0, exception, message, args);

        /// <summary>
        /// Formats and writes a log message that may contain sensitive information at the specified log level.
        /// </summary>
        /// <param name="logger">The <see cref="Logger"/> to write to.</param>
        /// <param name="logLevel">Entry will be written on this level.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        public static void SensitiveLog(this Logger logger, LogLevel logLevel, Exception exception, string message, params object?[] args) =>
#if ENABLE_SENSITIVE_LOG
            ((ILogger)logger).Log(logLevel, 0, exception, message, args);
#else
            NoOp(logLevel, exception, message, args);
#endif

        /// <summary>
        /// Formats and writes a log message at the specified log level.
        /// </summary>
        /// <param name="logger">The <see cref="Logger"/> to write to.</param>
        /// <param name="logLevel">Entry will be written on this level.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        public static void Log(this Logger logger, LogLevel logLevel, EventId eventId, Exception exception, string message, params object?[] args) =>
            ((ILogger)logger).Log(logLevel, eventId, exception, message, args);

        /// <summary>
        /// Formats and writes a log message that may contain sensitive information at the specified log level.
        /// </summary>
        /// <param name="logger">The <see cref="Logger"/> to write to.</param>
        /// <param name="logLevel">Entry will be written on this level.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        public static void SensitiveLog(this Logger logger, LogLevel logLevel, EventId eventId, Exception exception, string message, params object?[] args) =>
#if ENABLE_SENSITIVE_LOG
            ((ILogger)logger).Log(logLevel, eventId, exception, message, args);
#else
            NoOp(logLevel, eventId, exception, message, args);
#endif

        //------------------------------------------Scope------------------------------------------//

        /// <summary>
        /// Formats the message and creates a scope.
        /// </summary>
        /// <param name="logger">The <see cref="Logger"/> to create the scope in.</param>
        /// <param name="messageFormat">
        ///     Format string of the log message in message template format.
        ///     Example: <c>"User {User} logged in from {Address}"</c>
        /// </param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <returns>A disposable scope object. Can be null.</returns>
        /// <example>
        /// using(logger.BeginScope("Processing request from {Address}", address))
        /// {
        /// }
        /// </example>
        public static IDisposable? BeginScope(
            this Logger logger,
            string messageFormat,
            params object?[] args) =>
            ((ILogger)logger).BeginScope(messageFormat, args);
    }
}
