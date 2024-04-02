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
using System.Collections.Generic;
using System.Security.Principal;
using Yubico.YubiKey.TestApp.Converters;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.TestApp.Plugins
{
    /// <summary>
    /// The base class for plugins. All plugins should inherit from this class.
    /// </summary>
    internal abstract class PluginBase
    {
        public PluginBase(IOutput output)
        {
            Output = output;
        }

        protected IOutput Output { get; }

        /// <summary>
        /// The name that is used to select the plugin.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// A short description of the plugin to be shown in the generated usage
        /// text.
        /// </summary>
        public abstract string Description { get; }

        /// <summary>
        /// The name of the command for the plugin to execute.
        /// If your plugin
        /// </summary>
        /// <remarks>
        /// requires this, the constructor should change the <c>Required</c>
        /// property of your <c>command</c> parameter to <c>true</c> in its
        /// constructor. If you don't use it at all, you can remove it from the
        /// <c>Parameters</c> collection property in your constructor.
        /// </remarks>
        protected string Command { get; private set; } = string.Empty;

        /// <summary>
        /// Collection of parameters for a plugin.
        /// </summary>
        /// <remarks>The plugin is free to either
        /// add to this collection in your constructor or replace it either by
        /// overriding the member or just reassigning it from your constructor.
        /// </remarks>
        public virtual Dictionary<string, Parameter> Parameters { get; }
            = new Dictionary<string, Parameter>
            {
                ["command"] = new Parameter
                {
                    Name = "Command",
                    Shortcut = "c",
                    Description = "[command] The command for the plugin to execute.",
                    Type = typeof(string),
                    Required = false
                }
            };

        /// <summary>
        /// Collection of <c>Func</c> objects for converting strings to various
        /// types.
        /// </summary>
        /// <remarks>
        /// This property is virtual, so a plugin could completely replace
        /// it. The easiest usage is to just change existing types that you want
        /// to change the conversion on, and add new types.
        /// </remarks>
        public virtual Dictionary<Type, Func<string, object>> Converters { get; }
            = new Dictionary<Type, Func<string, object>>
            {
                [typeof(string)] = (s) => s,
                [typeof(int)] = (s) => int.Parse(s),
                [typeof(bool)] = (s) => StaticConverters.ParseBool(s),
                [typeof(byte[])] = (s) => StaticConverters.ParseByteArray(s),
                [typeof(byte)] = (s) => StaticConverters.ParseSingleByte(s),
                [typeof(ModHexBytes)] = (s) => ModHexBytes.Encode(s),
                [typeof(Base32Bytes)] = (s) => Base32Bytes.Encode(s),
                [typeof(Base16Bytes)] = (s) => Base16Bytes.Encode(s),
                [typeof(Uri)] = (s) => new Uri(s),
             };

        /// <summary>
        /// The method that actually performs the main task the plugin is
        /// designed to execute.
        /// </summary>
        /// <remarks>
        /// It returns a <c>bool</c> in order to let the main program know
        /// whether the task was a success so that it can return the proper
        /// value from Main(). That functionality is so that batch files can
        /// determine success.
        /// </remarks>
        /// <returns>Boolean indication of success.</returns>
        public abstract bool Execute();

        /// <summary>
        /// Assigns the <c>object</c> in the parameter's <c>Value</c> property to
        /// a property in the plugin class.
        /// </summary>
        /// Note that if you override this and still use the <c>Command</c>
        /// parameter, you should either call the base implementation or handle
        /// it yourself.
        /// </summary>
        public virtual void HandleParameters()
        {
            // If it wasn't specified, we'll just use an empty string.
            Command = (string)(Parameters["command"].Value ?? string.Empty);
        }

        /// <summary>
        /// End of line string.
        /// </summary>
        /// <remarks>
        /// Use this instead of literals so that platform differences are handled correctly.
        /// </remarks>
        public static string Eol => Environment.NewLine;
        public static int ConsoleWidth => Console.IsOutputRedirected ? 80 : Console.WindowWidth;

        /// <summary>
        /// Tells if the current process is running with elevated permissions.
        /// </summary>
        protected static bool IsAdministrator
        {
            get
            {
                if (OperatingSystem.IsWindows())
                {
                    using var current = WindowsIdentity.GetCurrent();
                    return new WindowsPrincipal(current)
                        .IsInRole(WindowsBuiltInRole.Administrator);
                }

                return true;
            }
        }
    }
}
