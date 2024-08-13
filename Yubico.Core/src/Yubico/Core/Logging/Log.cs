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
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Yubico.Core.Logging
{
    /// <summary>
    /// The <see cref="Log"/> class provides centralized logging support for your application or library.
    /// It allows you to configure logging either through a JSON configuration file (<a href="https://learn.microsoft.com/en-us/dotnet/core/extensions/logging?tabs=command-line#configure-logging-without-code">appsettings.json</a>) 
    /// or by dynamically setting up a logger using the <see cref="ConfigureLoggerFactory(Action{ILoggingBuilder})"/> method.
    ///
    /// <para><b>How to enable Logging:</b></para>
    /// There are two primary ways to enable logging:
    /// <list type="bullet">
    /// <item><description>1. Add an <c><a href="https://learn.microsoft.com/en-us/dotnet/core/extensions/logging?tabs=command-line#configure-logging-without-code">appsettings.json</a></c> to your project.</description></item>
    /// <item><description>2. Use the <see cref="ConfigureLoggerFactory(Action{ILoggingBuilder})"/> method.</description></item>
    /// </list>
    ///
    /// <para><b>Option 1: Using appsettings.json</b></para>
    /// Place an <c><a href="https://learn.microsoft.com/en-us/dotnet/core/extensions/logging?tabs=command-line#configure-logging-without-code">appsettings.json</a></c> file in your project directory with the following structure:
    /// <code language="json">
    /// {
    ///   "Logging": {
    ///     "LogLevel": {
    ///       "Yubico.Core": "Warning",
    ///       "Yubico.Yubikey": "Information"
    ///     }
    ///   }
    /// }
    /// </code>
    ///
    /// <para><b>Option 2: Using ConfigureLoggerFactory</b></para>
    /// Configure the logger dynamically in your code:
    /// <example>
    /// <code language="csharp">
    ///
    /// // Optionally, clear previous loggers
    /// Log.ConfigureLoggerFactory(builder => builder.ClearProviders());
    /// 
    /// // Add a console logger (added by default)
    /// Log.ConfigureLoggerFactory(builder => builder.AddConsole());
    ///
    /// // Add a Serilog logger
    /// Log.ConfigureLoggerFactory(builder => builder.AddSerilog());
    ///
    /// // Add both Console and Serilog loggers
    /// Log.ConfigureLoggerFactory(builder => builder.AddConsole().AddSerilog());
    /// </code>
    /// </example>
    ///
    /// <para><b>Using the Logger</b></para>
    /// After configuring the logger, you can create log instances and log messages as follows:
    /// <example>
    /// <code language="csharp">
    /// namespace Yubico;
    /// public class ExampleClass
    /// {
    ///     public ExampleClass()
    ///     {
    ///         // Logger with the class name as the category
    ///         ILogger typeNamedLogger = Log.GetLogger&lt;ExampleClass&gt;();
    ///         typeNamedLogger.LogInformation("Hello World");
    ///         // Output: Yubico.ExampleClass: Hello World
    ///
    ///         // Logger with a custom category name
    ///         ILogger categoryLogger = Log.GetLogger("SmartCard");
    ///         categoryLogger.LogInformation("Hello World");
    ///         // Output: SmartCard: Hello World
    ///     }
    /// }
    /// </code>
    /// </example>
    ///
    /// <para><b>Note:</b></para>
    /// You can also directly set a custom logger factory using the <see cref="Log.Instance"/> property, 
    /// though it is not the recommended approach. Using <see cref="ConfigureLoggerFactory"/>
    /// or the <a href="https://learn.microsoft.com/en-us/dotnet/core/extensions/logging?tabs=command-line#configure-logging-without-code">appsettings.json</a> approach is preferred.
    /// </summary>
    public static partial class Log
    {
        private static ILoggerFactory? _instance;
        private static readonly object Lock = new object();

        /// <summary>
        /// Gets or sets the global <see cref="Microsoft.Extensions.Logging.ILoggerFactory"/> instance used for logging throughout the application.
        /// By default, it's instantiated by using the <c>Logging-section</c> in your
        /// <a href="https://learn.microsoft.com/en-us/dotnet/core/extensions/logging?tabs=command-line#configure-logging-without-code">appsettings.json</a>file. 
        /// Refer to the <see cref="Yubico.Core.Logging.Log"/> class for additional information. 
        /// </summary>
        // This property uses double-checked locking to ensure thread-safe lazy initialization.
        // The getter initializes the factory if it hasn't been set, while the setter allows
        // for custom factory configuration.
        public static ILoggerFactory Instance
        {
            get
            {
                // First check: Quick return if instance is already initialized
                if (_instance != null)
                {
                    return _instance;
                }

                // Second check: Thread-safe initialization if instance is null
                lock (Lock)
                {
                    // Use null-coalescing assignment to initialize if still null
                    // This prevents multiple initializations in case of concurrent access
                    return _instance ??= GetDefaultLoggerFactory();
                }
            }
            set
            {
                // Ensure thread-safe assignment of new logger factory
                lock (Lock)
                {
                    // Prevent setting a null value to maintain a valid logger factory
                    _instance = value ?? throw new ArgumentNullException(nameof(value));
                }
            }
        }

        /// <inheritdoc cref="LoggerFactoryExtensions.CreateLogger{T}"/>
        public static ILogger GetLogger<T>() => Instance.CreateLogger<T>();

        /// <inheritdoc cref="LoggerFactoryExtensions.CreateLogger"/>
        public static ILogger GetLogger(string categoryName) => Instance.CreateLogger(categoryName);
        
        /// <summary>
        /// <example>
        /// From your project, you can set up logging dynamically like this, if you don't use this,
        /// the default dotnet <see cref="Microsoft.Extensions.Logging.LoggerFactory"/>
        /// will be created and output to the console.
        /// <code language="csharp">
        /// Logging.ConfigureLoggerFactory(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Trace));
        /// </code>
        /// With the default logging factory, you can load config using json.
        /// </example>
        /// </summary>
        /// <param name="configure"></param>
        public static void ConfigureLoggerFactory(Action<ILoggingBuilder> configure) =>
            Instance = Microsoft.Extensions.Logging.LoggerFactory.Create(configure);

        //Creates a logging factory based on a JsonConfiguration in appsettings.json
        private static ILoggerFactory GetDefaultLoggerFactory()
        {
            ILoggerFactory? configuredLoggingFactory = null;
            try
            {
                IConfigurationRoot configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: true)
                    .AddJsonFile("appsettings.Development.json", optional: true)
                    .Build();

                configuredLoggingFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(
                    builder =>
                    {
                        IConfigurationSection loggingSection = configuration.GetSection("Logging");
                        _ = builder.AddConfiguration(loggingSection);
                        _ = builder.AddConsole();
                    });
            }
            #pragma warning disable CA1031
            catch (Exception e)
            #pragma warning restore CA1031
            {
                Console.Error.WriteLine(e);
            }

            return configuredLoggingFactory ?? Microsoft.Extensions.Logging.LoggerFactory.Create(
                builder => builder
                    .AddConsole()
                    .SetMinimumLevel(LogLevel.Error));
        }
    }
}
