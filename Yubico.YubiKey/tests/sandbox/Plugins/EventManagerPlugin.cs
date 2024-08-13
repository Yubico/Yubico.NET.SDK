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
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Log = Yubico.Core.Logging.Log;
using Logger = Serilog.Core.Logger;

namespace Yubico.YubiKey.TestApp.Plugins
{
    class ThreadIdEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                "ThreadId", Environment.CurrentManagedThreadId));
        }
    }

    internal class EventManagerPlugin : PluginBase
    {
        public override string Name => "EventManager";
        public override string Description => "A place for YubiKeyEventManager test code";

        public EventManagerPlugin(IOutput output) : base(output) { }

        public override bool Execute()
        {
            Logger log = new LoggerConfiguration()
                .Enrich.With(new ThreadIdEnricher())
                .WriteTo.Console(
                    outputTemplate: "[{Level}] ({ThreadId})  {Message}{NewLine}{Exception}")
                .CreateLogger();

            Log.ConfigureLoggerFactory(builder =>
                builder
                    .AddSerilog(log)
                    .AddFilter(level => level >= LogLevel.Information));
            YubiKeyDeviceListener.Instance.Arrived += (s, e) =>
            {
                Console.WriteLine("YubiKey arrived:");
                Console.WriteLine(e.Device);
            };

            YubiKeyDeviceListener.Instance.Removed += (s, e) =>
            {
                Console.WriteLine("YubiKey removed:");
                Console.WriteLine(e.Device);
            };

            _ = Console.ReadLine();

            return true;
        }
    }
}
