// ﻿// Copyright 2021 Yubico AB
// //
// // Licensed under the Apache License, Version 2.0 (the "License").
// // You may not use this file except in compliance with the License.
// // You may obtain a copy of the License at
// //
// //     http://www.apache.org/licenses/LICENSE-2.0
// //
// // Unless required by applicable law or agreed to in writing, software
// // distributed under the License is distributed on an "AS IS" BASIS,
// // WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// // See the License for the specific language governing permissions and
// // limitations under the License.
//
// using Microsoft.Extensions.Logging;
// using Microsoft.Extensions.Logging.Abstractions;
//
// namespace Yubico.Core.Logging
// {
//     /// <summary>
//     /// A static class for managing Yubico SDK logging for this process.
//     /// </summary>
//     /// <remarks>
//     /// <para>
//     /// This class is used for managing the active logger used globally by .NET-based Yubico SDKs in the current process.
//     /// Changing the settings in this class will not have any effect on applications or services that are not running
//     /// within the current application's process. It will affect all libraries contained within - for example, changing
//     /// the logger factory here will impact both the Yubico.YubiKey and Yubico.Core libraries.
//     /// </para>
//     /// <para>
//     /// The <see cref="LoggerFactory"/> property is used to set and control the concrete log to be used by the SDK. By
//     /// default, we send logs to the "null" logger - effectively disabling logging. If you set this property with your
//     /// own logger factory, the SDK will use this log from the point of the set until someone calls this set method again.
//     /// </para>
//     /// <para>
//     /// <see cref="GetLogger"/> should be used to return an instance of the <see cref="Logger"/> class. This is the object
//     /// used to actually write the log messages. It is generally OK to cache an instance of a logger within another
//     /// class instance. Holding a Logger instance open longer than that is not recommended, as changes to the LoggerFactory
//     /// will not be reflected until you call the `GetLogger` method again.
//     /// </para>
//     /// </remarks>
//     public static class Log
//     {
//         private static ILoggerFactory? _factory;
//
//         /// <summary>
//         /// The logger factory implementation that should be used by the SDK. Use this to set the active logger.
//         /// </summary>
//         /// <remarks>
//         /// <para>
//         /// The LoggerFactory controls how the concrete log(s) that the SDK will use get created. This is something that
//         /// should be controlled by the application using the SDK, and not the SDK itself. The application can decide
//         /// whether they would like to send events to the Windows Event Log, or to a cross platform logger such as NLog,
//         /// Serilog, or others. An application can decide to send log messages to multiple sinks as well (see examples).
//         /// </para>
//         /// <para>
//         /// The <see cref="ILoggerFactory"/> interface is the same one that is used by `Microsoft.Extensions.Logging.` You
//         /// can read more about how to integrate with this interface in the
//         /// [Logging in .NET](https://docs.microsoft.com/en-us/dotnet/core/extensions/logging) webpage provided by Microsoft.
//         /// </para>
//         /// </remarks>
//         /// <example>
//         /// <para>
//         /// Send SDK log messages to the console:
//         /// </para>
//         /// <code language="csharp">
//         /// using Microsoft.Extensions.Logging;
//         /// using Yubico.Core.Logging;
//         ///
//         /// static class Program
//         /// {
//         ///     static void EnableLogging()
//         ///     {
//         ///         Log.LoggerFactory = LoggerFactory.Create(
//         ///             builder => builder.AddSimpleConsole(
//         ///                options =>
//         ///                {
//         ///                    options.IncludeScopes = true;
//         ///                    options.SingleLine = true;
//         ///                    options.TimestampFormat = "hh:mm:ss";
//         ///                })
//         ///                .AddFilter(level => level >= LogLevel.Information));
//         ///     }
//         /// }
//         /// </code>
//         /// </example>
//         /// <example>
//         /// <para>
//         /// Send SDK log messages to Serilog.
//         /// </para>
//         /// <para>
//         /// First, begin by adding a package reference to `Serilog.Extensions.Logging` and `Serilog.Sinks.Console` (or
//         /// to the appropriate sink you plan to use).
//         /// </para>
//         /// <para>
//         /// Now, you can add the following code to your application:
//         /// </para>
//         /// <code language="csharp">
//         /// using Microsoft.Extensions.Logging;
//         /// using Serilog;
//         /// using Yubico.Core.Logging;
//         ///
//         /// static class Program
//         /// {
//         ///     static void EnableLogging()
//         ///     {
//         ///         // Serilog does setup through its own LoggerConfiguration builder. The factory will
//         ///         // pick up the log from Serilog.Log.Logger.
//         ///         Serilog.Log.Logger = new LoggerConfiguration()
//         ///             .Enrich().FromLogContext()
//         ///             .WriteTo.Console()
//         ///             .CreateLogger();
//         ///
//         ///         // Fully qualified name to avoid conflicts with Serilog types
//         ///         Yubico.Core.Logging.Log.LoggerFactory = LoggerFactory.Create(
//         ///             builder => builder
//         ///                .AddSerilog(dispose: true)
//         ///                .AddFilter(level => level >= LogLevel.Information));
//         ///     }
//         /// }
//         /// </code>
//         /// </example>
//         public static ILoggerFactory LoggerFactory
//         {
//             get => _factory ??= new NullLoggerFactory();
//             set => _factory = value;
//         }
//
//         /// <summary>
//         /// Gets an instance of the active logger.
//         /// </summary>
//         /// <returns>
//         /// An instance of the active concrete logger.
//         /// </returns>
//         /// <example>
//         /// <para>
//         /// Write some information to the log.
//         /// </para>
//         /// <code language="csharp">
//         /// using Yubico.Core.Logging;
//         ///
//         /// public class Example
//         /// {
//         ///     private Logger _log = Log.GetLogger();
//         ///
//         ///     public void SampleMethod()
//         ///     {
//         ///         _log.LogDebug("The SampleMethod method has been called!");
//         ///     }
//         ///
//         ///     public void StaticMethod()
//         ///     {
//         ///         Logger log = Log.GetLogger(); // Can't use the instance logger because we're static.
//         ///         log.LogDebug("Called from a static method!");
//         ///     }
//         /// }
//         /// </code>
//         /// </example>
//         public static Logger GetLogger() => new Logger(LoggerFactory.CreateLogger("Yubico.Core logger"));
//     }
// }
