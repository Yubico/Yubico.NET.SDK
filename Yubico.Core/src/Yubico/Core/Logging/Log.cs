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
    public static partial class Log
    {
        private static ILoggerFactory _factoryInstance = GetDefaultFactory();

        //Creates a logging factory based on a JsonConfiguration
        private static ILoggerFactory GetDefaultFactory()
        {
            const string AppsettingsJson = "appsettings.json";
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(AppsettingsJson, optional: true)
                .Build();

            IConfigurationSection loggingConfiguration = configuration.GetSection("Logging");
            // see if creation was OK
            return LoggerFactory.Create(
                builder =>
                {
                    try
                    {
                        IConfigurationSection configurationSection = configuration.GetRequiredSection("Logging");
                        _ = builder.AddConfiguration(configurationSection);
                    }
                    catch (InvalidOperationException) // File or section does not exist, so we set our own level
                    {
                        _ = builder.SetMinimumLevel(LogLevel.Error);
                    }

                    _ = builder.AddConsole();
                });
        }

        /// <summary>
        /// <example>
        /// From your project, you can set up logging dynamically like this, if you dont use this, the default loggingFactory will be used.
        /// <code language="csharp">
        /// Logging.ConfigureLoggerFactory(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Trace));
        /// </code>
        /// With the default logging factory, you can load config using json.
        /// </example>
        /// </summary>
        /// <param name="configure"></param>
        public static void ConfigureLoggerFactory(Action<ILoggingBuilder> configure) =>
            _factoryInstance = LoggerFactory.Create(configure);

        /// <inheritdoc cref="LoggerFactoryExtensions.CreateLogger{T}"/>
        public static ILogger GetLogger<T>() => _factoryInstance.CreateLogger<T>();

        /// <inheritdoc cref="LoggerFactoryExtensions.CreateLogger"/>
        public static ILogger GetLogger(string categoryName) =>
            _factoryInstance.CreateLogger(categoryName);
    }
}
