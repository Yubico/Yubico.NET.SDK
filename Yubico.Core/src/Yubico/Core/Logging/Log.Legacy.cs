// Copyright 2025 Yubico AB
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
using Microsoft.Extensions.Logging.Abstractions;

namespace Yubico.Core.Logging
{
    public static partial class Log
    {
        private static ILoggerFactory? _factory;

        [Obsolete("Obsolete, use Log.Instance instead. Setting this will override the default dotnet console logger.")]
        public static ILoggerFactory LoggerFactory
        {
            get => _factory ??= new NullLoggerFactory();
            set
            {
                _factory = value;

                // Also swap out the new implementation instance
                Instance = value;
            }
        }

        [Obsolete("Obsolete, use equivalent ILogger method, or view the changelog for further instruction.")]
        public static Logger GetLogger() => new Logger(Yubico.Core.Logging.Log.LoggerFactory.CreateLogger("Yubico.Core logger"));
    }
}
