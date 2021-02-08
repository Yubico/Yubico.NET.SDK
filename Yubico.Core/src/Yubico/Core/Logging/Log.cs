// Copyright (c) Yubico AB

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;

namespace Yubico.Core.Logging
{
    public static class Log
    {
        private static ILogger Instance { get; set; } =
            new NullLoggerFactory().CreateLogger("YubiKey SDK null logger");

        public static void SetLoggerInstance(ILogger instance) => Instance = instance;

        /// <summary>
        /// Formats the message and creates a scope.
        /// </summary>
        /// <param name="messageFormat">
        /// Format string of the log message in message template format. Example:
        /// "User {User} logged in from {Address}"
        /// </param>
        /// <param name="args">
        /// An object array that contains zero or more objects to format.
        /// </param>
        /// <returns>
        /// A disposable scope object. Can be null.
        /// </returns>
        public static IDisposable BeginLogScope(string messageFormat, params object[] args) =>
            Instance.BeginScope(messageFormat, args);

        /// <summary>
        /// Formats and writes a critical log message.
        /// </summary>
        /// <param name="eventId">
        /// The event id associated with the log.
        /// </param>
        /// <param name="message">
        /// Format string of the log message in message template format. Example:
        /// "User {User} logged in from {Address}"
        /// </param>
        /// <param name="args">
        /// An object array that contains zero or more objects to format.
        /// </param>
        public static void LogCritical(EventId eventId, string message, params object[] args) =>
            Instance.LogCritical(eventId, message, args);

        /// <summary>
        /// Formats and writes a critical log message.
        /// </summary>
        /// <param name="eventId">
        /// The event id associated with the log.
        /// </param>
        /// <param name="exception">
        /// The exception to log.
        /// </param>
        /// <param name="message">
        /// Format string of the log message in message template format. Example:
        /// "User {User} logged in from {Address}"
        /// </param>
        /// <param name="args">
        /// An object array that contains zero or more objects to format.
        /// </param>
        public static void LogCritical(EventId eventId, Exception exception, string message, params object[] args) =>
            Instance.LogCritical(eventId, exception, message, args);

        /// <summary>
        /// Formats and writes a debug log message.
        /// </summary>
        /// <param name="eventId">
        /// The event id associated with the log.
        /// </param>
        /// <param name="message">
        /// Format string of the log message in message template format. Example:
        /// "User {User} logged in from {Address}"
        /// </param>
        /// <param name="args">
        /// An object array that contains zero or more objects to format.
        /// </param>
        public static void LogDebug(EventId eventId, string message, params object[] args) =>
            Instance.LogDebug(eventId, message, args);

        /// <summary>
        /// Formats and writes an error log message.
        /// </summary>
        /// <param name="eventId">
        /// The event id associated with the log.
        /// </param>
        /// <param name="message">
        /// Format string of the log message in message template format. Example:
        /// "User {User} logged in from {Address}"
        /// </param>
        /// <param name="args">
        /// An object array that contains zero or more objects to format.
        /// </param>
        public static void LogError(EventId eventId, string message, params object[] args) =>
            Instance.LogError(eventId, message, args);

        /// <summary>
        /// Formats and writes an error log message.
        /// </summary>
        /// <param name="eventId">
        /// The event id associated with the log.
        /// </param>
        /// <param name="exception">
        /// The exception to log.
        /// </param>
        /// <param name="message">
        /// Format string of the log message in message template format. Example:
        /// "User {User} logged in from {Address}"
        /// </param>
        /// <param name="args">
        /// An object array that contains zero or more objects to format.
        /// </param>
        public static void LogError(EventId eventId, Exception exception, string message, params object[] args) =>
            Instance.LogDebug(eventId, exception, message, args);

        /// <summary>
        /// Formats and writes an informational log message.
        /// </summary>
        /// <param name="eventId">
        /// The event id associated with the log.
        /// </param>
        /// <param name="message">
        /// Format string of the log message in message template format. Example:
        /// "User {User} logged in from {Address}"
        /// </param>
        /// <param name="args">
        /// An object array that contains zero or more objects to format.
        /// </param>
        public static void LogInformation(EventId eventId, string message, params object[] args) =>
            Instance.LogInformation(eventId, message, args);


        /// <summary>
        /// Formats and writes a trace log message.
        /// </summary>
        /// <param name="eventId">
        /// The event id associated with the log.
        /// </param>
        /// <param name="message">
        /// Format string of the log message in message template format. Example:
        /// "User {User} logged in from {Address}"
        /// </param>
        /// <param name="args">
        /// An object array that contains zero or more objects to format.
        /// </param>
        public static void LogTrace(EventId eventId, string message, params object[] args) =>
            Instance.LogTrace(eventId, message, args);

        /// <summary>
        /// Formats and writes a warning log message.
        /// </summary>
        /// <param name="eventId">
        /// The event id associated with the log.
        /// </param>
        /// <param name="message">
        /// Format string of the log message in message template format. Example:
        /// "User {User} logged in from {Address}"
        /// </param>
        /// <param name="args">
        /// An object array that contains zero or more objects to format.
        /// </param>
        public static void LogWarning(EventId eventId, string message, params object[] args) =>
            Instance.LogWarning(eventId, message, args);

        /// <summary>
        /// Formats and writes a warning log message.
        /// </summary>
        /// <param name="eventId">
        /// The event id associated with the log.
        /// </param>
        /// <param name="exception">
        /// The exception to log.
        /// </param>
        /// <param name="message">
        /// Format string of the log message in message template format. Example:
        /// "User {User} logged in from {Address}"
        /// </param>
        /// <param name="args">
        /// An object array that contains zero or more objects to format.
        /// </param>
        public static void LogWarning(EventId eventId, Exception exception, string message, params object[] args) =>
            Instance.LogWarning(eventId, exception, message, args);

        /// <summary>
        /// Formats and writes a critical log message that may contain secret or sensitive materal.
        /// Only available on debug builds.
        /// </summary>
        /// <param name="eventId">
        /// The event id associated with the log.
        /// </param>
        /// <param name="message">
        /// Format string of the log message in message template format. Example:
        /// "User {User} logged in from {Address}"
        /// </param>
        /// <param name="args">
        /// An object array that contains zero or more objects to format.
        /// </param>
        public static void LogCriticalSensitive(EventId eventId, string message, params object[] args) =>
#if DEBUG
            Instance.LogCritical(eventId, message, args);
#else
            Instance.LogCritical(eventId, "REDACTED", Array.Empty<object>());
#endif

        /// <summary>
        /// Formats and writes a critical log message that may contain secret or sensitive materal.
        /// Only available on debug builds.
        /// </summary>
        /// <param name="eventId">
        /// The event id associated with the log.
        /// </param>
        /// <param name="exception">
        /// The exception to log.
        /// </param>
        /// <param name="message">
        /// Format string of the log message in message template format. Example:
        /// "User {User} logged in from {Address}"
        /// </param>
        /// <param name="args">
        /// An object array that contains zero or more objects to format.
        /// </param>
        public static void LogCriticalSensitive(EventId eventId, Exception exception, string message, params object[] args) =>
#if DEBUG
            Instance.LogCritical(eventId, exception, message, args);
#else
            Instance.LogCritical(eventId, exception, "REDACTED", Array.Empty<object>());
#endif

        /// <summary>
        /// Formats and writes a debug log message that may contain secret or sensitive materal.
        /// Only available on debug builds.
        /// </summary>
        /// <param name="eventId">
        /// The event id associated with the log.
        /// </param>
        /// <param name="message">
        /// Format string of the log message in message template format. Example:
        /// "User {User} logged in from {Address}"
        /// </param>
        /// <param name="args">
        /// An object array that contains zero or more objects to format.
        /// </param>
        public static void LogDebugSensitive(EventId eventId, string message, params object[] args) =>
#if DEBUG
            Instance.LogDebug(eventId, message, args);
#else
            Instance.LogDebug(eventId, "REDACTED", Array.Empty<object>());
#endif

        /// <summary>
        /// Formats and writes an error log message that may contain secret or sensitive materal.
        /// Only available on debug builds.
        /// </summary>
        /// <param name="eventId">
        /// The event id associated with the log.
        /// </param>
        /// <param name="message">
        /// Format string of the log message in message template format. Example:
        /// "User {User} logged in from {Address}"
        /// </param>
        /// <param name="args">
        /// An object array that contains zero or more objects to format.
        /// </param>
        public static void LogErrorSensitive(EventId eventId, string message, params object[] args) =>
#if DEBUG
            Instance.LogError(eventId, message, args);
#else
            Instance.LogError(eventId, "REDACTED", Array.Empty<object>());
#endif

        /// <summary>
        /// Formats and writes an error log message that may contain secret or sensitive materal.
        /// Only available on debug builds.
        /// </summary>
        /// <param name="eventId">
        /// The event id associated with the log.
        /// </param>
        /// <param name="exception">
        /// The exception to log.
        /// </param>
        /// <param name="message">
        /// Format string of the log message in message template format. Example:
        /// "User {User} logged in from {Address}"
        /// </param>
        /// <param name="args">
        /// An object array that contains zero or more objects to format.
        /// </param>
        public static void LogErrorSensitive(EventId eventId, Exception exception, string message, params object[] args) =>
#if DEBUG
            Instance.LogDebug(eventId, exception, message, args);
#else
            Instance.LogDebug(eventId, exception, "REDACTED", Array.Empty<object>());
#endif

        /// <summary>
        /// Formats and writes an informational log message that may contain secret or sensitive materal.
        /// Only available on debug builds.
        /// </summary>
        /// <param name="eventId">
        /// The event id associated with the log.
        /// </param>
        /// <param name="message">
        /// Format string of the log message in message template format. Example:
        /// "User {User} logged in from {Address}"
        /// </param>
        /// <param name="args">
        /// An object array that contains zero or more objects to format.
        /// </param>
        public static void LogInformationSensitive(EventId eventId, string message, params object[] args) =>
#if DEBUG
            Instance.LogInformation(eventId, message, args);
#else
            Instance.LogInformation(eventId, "REDACTED", Array.Empty<object>());
#endif

        /// <summary>
        /// Formats and writes a trace log message that may contain secret or sensitive materal.
        /// Only available on debug builds.
        /// </summary>
        /// <param name="eventId">
        /// The event id associated with the log.
        /// </param>
        /// <param name="message">
        /// Format string of the log message in message template format. Example:
        /// "User {User} logged in from {Address}"
        /// </param>
        /// <param name="args">
        /// An object array that contains zero or more objects to format.
        /// </param>
        public static void LogTraceSensitive(EventId eventId, string message, params object[] args) =>
#if DEBUG
            Instance.LogTrace(eventId, message, args);
#else
            Instance.LogTrace(eventId, "REDACTED", Array.Empty<object>());
#endif

        /// <summary>
        /// Formats and writes a warning log message that may contain secret or sensitive materal.
        /// Only available on debug builds.
        /// </summary>
        /// <param name="eventId">
        /// The event id associated with the log.
        /// </param>
        /// <param name="message">
        /// Format string of the log message in message template format. Example:
        /// "User {User} logged in from {Address}"
        /// </param>
        /// <param name="args">
        /// An object array that contains zero or more objects to format.
        /// </param>
        public static void LogWarningSensitive(EventId eventId, string message, params object[] args) =>
#if DEBUG
            Instance.LogWarning(eventId, message, args);
#else
            Instance.LogWarning(eventId, "REDACTED", Array.Empty<object>());
#endif

        /// <summary>
        /// Formats and writes a warning log message that may contain secret or sensitive materal.
        /// Only available on debug builds.
        /// </summary>
        /// <param name="eventId">
        /// The event id associated with the log.
        /// </param>
        /// <param name="exception">
        /// The exception to log.
        /// </param>
        /// <param name="message">
        /// Format string of the log message in message template format. Example:
        /// "User {User} logged in from {Address}"
        /// </param>
        /// <param name="args">
        /// An object array that contains zero or more objects to format.
        /// </param>
        public static void LogWarningSensitive(EventId eventId, Exception exception, string message, params object[] args) =>
#if DEBUG
            Instance.LogWarning(eventId, exception, message, args);
#else
            Instance.LogWarning(eventId, exception, "REDACTED", Array.Empty<object>());
#endif
    }
}
