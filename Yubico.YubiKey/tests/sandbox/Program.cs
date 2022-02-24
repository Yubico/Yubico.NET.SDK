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
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using Yubico.YubiKey.TestApp.Plugins.Otp;
using Yubico.YubiKey.TestApp.Plugins;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.TestApp
{
    class Program : IOutput, IDisposable
    {
        // When you create a new plugin, just add it here. It's a lambda delegate,
        // so you're actually adding a method for creating an instance of your
        // plugin.
        private readonly Dictionary<string, Func<IOutput, PluginBase>> _plugIns =
            new Dictionary<string, Func<IOutput, PluginBase>>
            {
                ["calculate"] = (output) => new Calculate(output),
                ["chalresp"] = (output) => new ChallengeResponse(output),
                ["delete"] = (output) => new Delete(output),
                ["hotp"] = (output) => new Hotp(output),
                ["ndef"] = (output) => new Ndef(output),
                ["static"] = (output) => new Static(output),
                ["swap"] = (output) => new Swap(output),
                ["update"] = (output) => new UpdateSlot(output),
                ["yubiotp"] = (output) => new YubiOtp(output),
                ["enumeration"] = (output) => new EnumeratePlugin(output),
                ["fido"] = (output) => new FidoPlugin(output),
                ["u2f"] = (output) => new U2fPlugin(output),
                ["hidcodetablegenerator"] = (output) => new HidCodeTablePlugin(output),
                ["validatehotp"] = (output) => new ValidateHotp(output),
                ["jamie"] = (output) => new JamiePlugin(output),
                ["greg"] = (output) => new GregPlugin(output),
                ["eventmgmt"] = (output) => new EventManagerPlugin(output),
                ["hidevents"] = (output) => new HidDeviceListenerPlugin(output),
                ["smartcardevents"] = (output) => new SmartCardDeviceListenerPlugin(output),
                ["feature"] = (output) => new YubiKeyFeaturePlugin(output)
            };

        static int Main(string[] args)
        {
            using var app = new Program();
            return app.Execute(args);
        }

        private int Execute(string[] args)
        {
            try
            {
                ParseCommandline(args);
            }
            catch (Exception ex)
            {
                WriteLine($"Error parsing command line [{ ex.Message }].", OutputLevel.Error);
                Usage(true);
                return -1;
            }

            // If there's a config file, parse that.
            if (!string.IsNullOrWhiteSpace(_configFile))
            {
                try
                {
                    ParseConfigFile();
                    WriteLine($"Config file [{ _configFile }] parsed.");
                }
                catch (Exception ex)
                {
                    WriteLine($"Error: { ex.Message }.", OutputLevel.Error);
                    WriteLine($"File: { _configFile }", OutputLevel.Error);
                    return -1;
                }
            }

            // At this point, both the command line and the config file have
            // been processed, so we don't need to buffer output.
            ProcessBufferedOutput();

            // Did the user call for help?
            if (_rawParameters.ContainsKey("?")
                || _rawParameters.ContainsKey("usage"))
            {
                Usage();
                // Should we return error (-1) for usage?
                // I'm guessing yes for now. It's not exactly an error, but it's
                // not exactly a success case, either.
                return -1;
            }

            // We should have a plugin now.
            if (_plugin == null)
            {
                WriteLine("No plugin was selected on the command line or in a config file.", OutputLevel.Error);
                return -1;
            }
            WriteLine($"Plugin [{ _plugin.Name }] specified.");

            // Next, verify parameters.
            try
            {
                ProcessParameters();
            }
            catch (Exception ex)
            {
                WriteLine($"Error processing parameters [{ ex.Message }].", OutputLevel.Error);
                return -1;
            }

            int returnValue;
            // Nothing to it but to do it.
            try
            {
                WriteLine("Calling plugins Execute() method.");
                bool result = _plugin?.Execute() ?? false;
                WriteLine($"Plugin's Execute() method returned [{ result }]", OutputLevel.Verbose);
                returnValue = result ? 0 : -1;
            }
            catch (Exception ex)
            {
                WriteLine($"Exception in plugin [{ _plugin.Name }]: { ex.Message }.", OutputLevel.Error);
                returnValue = -1;
            }
            Write($"Returning [{ returnValue }] to command shell.", OutputLevel.Verbose);
            return returnValue;
        }

        // Usage is done in three main steps with some substeps.
        // 1. Build a list of text lines for the usage output.
        //     1a. Build a list of plugins. If one was selected, it's a list of one.
        //     1b. Add a line for each plug-in, then a line for each parameter of
        //         the plugin.
        // 2. Create a StringBuilder object and populate it with the lines.
        //     2a. Take the longest parameter name so that we can line up remarks
        //         on the same line.
        //     2b. Get the width of the console so that we can intelligenty wrap
        //         text to the width of the console.
        //     2c. For each line in our line collection, break it down into words
        //         and print lines, wrapping smartly, indenting the next line.
        // 3. Output result.
        //    3a. If there's an error condition, send it to stderr, Otherwise stdout.
        private void Usage(bool asError = false)
        {
            // Step 1.
            // Get global parameters.
            var paramUsage = new List<(string id, string desc)>
            {
                ("Global Parameters", string.Empty),
                ("-?/Usage", "Prints out this message."),
                ("-Config", "[file] Config file to get parameters from. Note "
                           + "that command line parameters override parameters "
                           + "in the config file."),
                ("-Plugin*", "[plugin name] The plugin to run with the given "
                           + "parameters."),
                ("-q/Quiet", "[true/false] Supress output except for errors (to "
                           + "stderr). If you call without a parameter, true is assumed."),
                ("-v/Verbose", "[true/false] Show more informational output. "
                           + "If you call without a parameter, true is assumed. "
                           + "Verbose and quiet are mutually exclusive."),
                ("-o/Output", "[file path] Write output to file. A parameter is required. "
                           + "If you do not supply a full path, the current working "
                           + "directory is the starting point."),
            };
            if (_plugin is null || ((_outputLevel ?? OutputLevel.Normal) == OutputLevel.Verbose))
            {
                paramUsage.Add(("Plug-ins", string.Empty));
            }

            // Creating a list of plugins. If one was chosen on the command line,
            // just add that one. Otherwise, add all available plugins.
            var plugIns = new List<PluginBase>(
                !(_plugin is null)
                ? new[] { _plugin }
                : _plugIns.Values.Select(p => p(this)));

            foreach (PluginBase plugin in plugIns)
            {
                if (_plugin is null && ((_outputLevel ?? OutputLevel.Normal) != OutputLevel.Verbose))
                {
                    // If there's no plug-in selected, show all of them, but only
                    // descriptions so that we don't have pages of parameters.
                    paramUsage.Add((plugin.Name, plugin.Description));
                }
                else
                {
                    // If there is a plug-in selected, show all of its parameters
                    // and their descriptions, but no description for the plug-in.
                    paramUsage.Add(($"{ plugin.Name } Parameters", string.Empty));
                    if (plugin.Parameters.Values.Any())
                    {
                        foreach (Parameter parameter in plugin.Parameters.Values)
                        {
                            // If there's both a long and short name, show them both,
                            // separated by a slash. Otherwise, show the one.
                            string id = "-" + string.Join(
                                '/',
                                new[] { parameter.Shortcut, parameter.Name }
                                    .Where(s => !string.IsNullOrWhiteSpace(s)))
                                // If the parameter is required, we want to indicate that.
                                + (parameter.Required ? "*" : string.Empty);
                            paramUsage.Add((id, parameter.Description));
                        }
                    }
                    else
                    {
                        paramUsage.Add(("none", "This plugin has no configurable parameters."));
                    }
                }
            }

            // Step 2.
            var sb = new StringBuilder(AppDomain.CurrentDomain.FriendlyName);
            _ = sb.AppendLine(" [-config configFile] [-plugin pluginIdentifier] [plugin options]");

            int maxFlagWidth = paramUsage
                .Where(l => !string.IsNullOrWhiteSpace(l.desc))
                .Select(l => l.id.Length).Max() + 4;

            int consoleWidth = Console.WindowWidth - 1;
            foreach ((string id, string desc) in paramUsage)
            {
                if (string.IsNullOrWhiteSpace(desc))
                {
                    _ = sb.AppendLine(PluginBase.Eol + id);
                }
                else
                {
                    int spaces = maxFlagWidth - id.Length - 3;
                    _ = sb.Append($"  { id }{ new string(' ', spaces)}");
                    WriteDescription(desc, maxFlagWidth);
                }
            }
            WriteDescription(PluginBase.Eol
                + "Parameters marked with asterisk are required. They can be "
                + "specified either on the command line or in a config file.");

            // Step 3.
            if (asError)
            {
                // I'm not going to use our output method because I don't want
                // to output the whole thing in bold or it will overshadow the
                // error message at the top.
                Console.Error.WriteLine(sb.ToString());
            }
            else
            {
                WriteLine(sb.ToString());
            }

            void WriteDescription(string description, int indent = 0)
            {
                string padding = new string(' ', indent);
                string[] words = description.Split(' ');
                for (int i = 0, position = indent; i < words.Length; ++i)
                {
                    string word = words[i];
                    if (position + word.Length > consoleWidth)
                    {
                        _ = sb!.Append(PluginBase.Eol + padding);
                        position = indent;
                    }
                    else
                    {
                        _ = sb!.Append(' ');
                    }
                    _ = sb!.Append(word);
                    position += word.Length + 1;
                }
                _ = sb.AppendLine();
            }
        }

        // Because we're merging two different ways of getting parameters, we
        // will collect parameters from here and just store them.
        private void ParseCommandline(string[] args)
        {
            WriteLine($"Parsing command line parameters [{ string.Join(", ", args) }].", OutputLevel.Verbose);
            for (int i = 0; i < args.Length; ++i)
            {
                string arg = args[i];
                // We should be at the boundry of a parameter. If we're not,
                // then there's a problem either with the input or our parsing
                // of it.
                if (arg[0] != '-' && arg[0] != '/')
                {
                    throw new ArgumentException($"[{ arg[0] }] was unexpected.");
                }

                // If the argument has a colon, then it can be assumed that the
                // data after the colon is the parameter value.
                string name, value;
                if (arg.Contains(':'))
                {
                    name = arg[1..arg.IndexOf(':')];
                    value = arg[(arg.IndexOf(':') + 1)..];
                }
                // If there is a next argument and it doesn't start with a
                // slash or dash, then it is the parameter value.
                else if (i < args.Length - 1
                        && args[i + 1][0] != '-'
                        && args[i + 1][0] != '/')
                {
                    name = arg[1..];
                    value = args[i + 1];
                    ++i;
                }
                // Otherwise, there's no parameter. Hopefully, it's a bool.
                else
                {
                    name = arg[1..];
                    // We're populating this with string.Empty so that
                    value = string.Empty;
                }

                // It will just be easier if this isn't case sensitive.
                name = name.ToLower();

                WriteLine($"Parsed parameter name[{ name }], value[{ value }]", OutputLevel.Verbose);

                // Since we're handling parameters from two different places,
                // we'll use a common method.
                HandleParameter(name, value);
            }
        }

        // This method handles program-level parameters and stores the rest for
        // the plug-in to handle.
        // The assumption is that this is used to parse the command line before
        // it's used to parse the config file.
        private void HandleParameter(string key, string value)
        {
            Write($"Processing [{ key }] with value [{ value }]...", OutputLevel.Verbose);
            bool isSet = true;

            switch (key)
            {
                case "plugin":
                    if (_plugin is null)
                    {
                        SetPlugin(value.ToLower());
                    }
                    else
                    {
                        Write($"Not setting [{ value }] as plugin. ", OutputLevel.Verbose);
                        WriteLine($"Plugin is already [{ _plugin.Name }].", OutputLevel.Verbose);
                        isSet = false;
                    }
                    break;
                case "config":
                    // We'll error check this later when we try to read it.
                    // Also, we'll make sure it hasn't been set so that someone
                    // can't try to set up an "Inception" situation. We don't
                    // want to be able to set config file from a config file.
                    if (string.IsNullOrWhiteSpace(_configFile))
                    {
                        if (string.IsNullOrWhiteSpace(key))
                        {
                            throw new ArgumentException("Can't set a null or empty string to config file.");
                        }
                        _configFile = value;
                    }
                    else
                    {
                        string message = $"Not setting [{ value }] as config file. " +
                            $"Config file is already [{ _configFile }].";
                        throw new ArgumentException(message);
                    }
                    break;
                case "q":
                case "quiet":
                case "v":
                case "verbose":
                    if (!_outputLevel.HasValue)
                    {
                        OutputLevel level = key[0] switch
                        {
                            'q' => OutputLevel.Quiet,
                            'v' => OutputLevel.Verbose,
                            _ => OutputLevel.None
                        };

                        _outputLevel = StaticConverters.ParseBool(value)
                            ? level
                            : OutputLevel.Normal;
                        // At this point, we know our verbosity, so we don't
                        // need to buffer output anymore.
                        ProcessBufferedOutput();
                    }
                    else
                    {
                        throw new ArgumentException(
                            $"Can't set [{ key }]. OutputLevel is already [{ _outputLevel.Value }]");
                    }
                    break;
                case "o":
                case "output":
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        throw new ArgumentException("You must supply a valid path for the output file.");
                    }
                    _outputFile = new StreamWriter(value, false, Encoding.UTF8, 256);
                    break;
                default:
                    // Since we process the command line first, and I believe
                    // that the command line should take precedence, we will
                    // assume that if we see a parameter twice, the first one
                    // wins. That also means that if the user specifies the
                    // same parameter twice, the first one also wins.
                    if (!_rawParameters.ContainsKey(key))
                    {
                        _rawParameters[key] = value;
                    }
                    else
                    {
                        Write($"Not setting [{ key }] as [{ value }]. ", OutputLevel.Verbose);
                        WriteLine(
                            $"[{ key }] was already set, probably in your config file.",
                            OutputLevel.Verbose);
                        isSet = false;
                    }
                    break;
            }
            WriteLine($"Property [{ key }] " + (isSet ? "set." : "not set."), OutputLevel.Verbose);
        }

        // My thinking is that if you're specifying something on the command
        // line, it should supersede anything specified in the config file.
        // What we'll do is go through the config file and items that are
        // already in the parameter collection will be skipped.
        private void ParseConfigFile()
        {
            if (!File.Exists(_configFile))
            {
                throw new FileNotFoundException($"Specified config file [{ _configFile }] doesn't exist.");
            }

            var configMap = new ExeConfigurationFileMap
            {
                ExeConfigFilename = _configFile
            };
            Configuration config = ConfigurationManager.OpenMappedExeConfiguration(
                configMap,
                ConfigurationUserLevel.None);
            foreach (KeyValueConfigurationElement? setting in config.AppSettings.Settings)
            {
                string key = setting?.Key.ToLower() ?? string.Empty;
                string value = setting?.Value ?? string.Empty;

                // Since we're handling parameters from two different places,
                // we'll use a common method.
                HandleParameter(key, value);
            }
        }

        private void SetPlugin(string plugin)
        {
            if (!_plugIns.TryGetValue(plugin, out Func<IOutput, PluginBase>? getter))
            {
                throw new InvalidOperationException($"Plugin [{ plugin }] not found.");
            }
            _plugin = getter(this);
        }

        private void ProcessParameters()
        {
            // If we're here, then we've got a plugin. Let's short-circuit the
            // null checking stuff.
            PluginBase plugin = _plugin!;

            // Because there are two ways to call a parameter, with its name or
            // shortcut, we'll build a single use lookup.
            var parameters = plugin.Parameters.Values
                .Where(p => !string.IsNullOrEmpty(p.Name))
                .Select(p => new KeyValuePair<string, Parameter>(p.Name.ToLower(), p))
                .Concat(plugin.Parameters.Values
                .Where(p => !string.IsNullOrEmpty(p.Shortcut))
                .Select(p => new KeyValuePair<string, Parameter>(p.Shortcut.ToLower(), p)))
                .ToDictionary(pair => pair.Key, pair => pair.Value);

            // First, apply the parameters to the plugin.
            foreach (KeyValuePair<string, string> kvp in _rawParameters)
            {
                if (!parameters.TryGetValue(kvp.Key, out Parameter? parameter))
                {
                    throw new ArgumentException($"Unexpected parameter [{ kvp.Key }] for plugin [{ plugin.Name }]");
                }
                if (!plugin.Converters.TryGetValue(parameter.Type, out Func<string, object>? converter))
                {
                    string message = string.Format(
                        "Missing converter for type [{0}], parameter [-{1} ({2})]",
                        parameter.Type,
                        parameter.Shortcut,
                        parameter.Name);
                    throw new InvalidOperationException(message);
                }
                try
                {
                    parameter.Value = converter(kvp.Value);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Error: { ex.Message }, Parameter: { parameter.Name }, Value: { kvp.Value }, Type: { parameter.Type }",
                        ex);
                }
                WriteLine($"Parameter [{ parameter.Name }] parsed as type [{ parameter.Type }]", OutputLevel.Verbose);
            }

            // Then, find out if we're missing any required parameters.
            foreach (Parameter parameter in plugin.Parameters.Values.Where(p => p.Required))
            {
                if (parameter.Value is null)
                {
                    throw new ArgumentException($"Required parameter [-{ parameter.Shortcut } ({ parameter.Name })] was not specified");
                }
            }

            // Now, give the plugin a chance to populate local fields and
            // properties so you don't have to mess with the parameter objects
            // later.
            Write("Calling plugin's HandleParameters method.", OutputLevel.Verbose);
            plugin.HandleParameters();
        }

        #region IOutput Implementation
        public OutputLevel OutputLevel => _outputLevel ?? OutputLevel.Normal;

        public void Write(string output = "", OutputLevel level = OutputLevel.Normal)
        {
            // If it's an error condition, we'll go ahead and stop buffering.
            if (level == OutputLevel.Error)
            {
                ProcessBufferedOutput();
            }
            if (_bufferedOutput is null)
            {
                if (level <= _outputLevel)
                {
                    if (level == OutputLevel.Error)
                    {
                        Console.BackgroundColor = ConsoleColor.White;
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.Error.Write(output);
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.Write(output);
                        _outputFile?.Write(output);
                    }
                }
            }
            else
            {
                _bufferedOutput.Add((level, output));
            }
        }

        public void WriteLine(string output, OutputLevel level = OutputLevel.Normal) =>
            Write(output + PluginBase.Eol, level);

        // Important note: This method avoids writing secrets to a file, but that
        // wouldn't stop someone from piping the output into a file. The bottom line
        // is that the program has to be able to output secrets to the user, so some
        // responsibility must be borne by the user not to compromise security.
        public void WriteSensitive(Span<char> output, OutputLevel level = OutputLevel.Normal)
        {
            if (level <= _outputLevel)
            {
                if (level == OutputLevel.Error)
                {
                    Console.BackgroundColor = ConsoleColor.White;
                    Console.ForegroundColor = ConsoleColor.Black;
                    foreach (char c in output)
                    {
                        Console.Error.Write(c);
                    }
                    Console.ResetColor();
                }
                else
                {
                    foreach (char c in output)
                    {
                        Console.Write(c);
                    }
                    _outputFile?.Write("********");
                }
            }
        }

        private void ProcessBufferedOutput()
        {
            if (!(_bufferedOutput is null))
            {
                // If the verbosity hasn't been set yet, we'll just make it normal.
                _outputLevel ??= OutputLevel.Normal;

                // We'll grab the reference and set the field to null so that
                // we can use the normal method of output.
                List<(OutputLevel level, string message)> bufferedOutput = _bufferedOutput;
                _bufferedOutput = null;
                bufferedOutput.ForEach((item) => Write(item.message, item.level));
            }
        }
        #endregion

        #region IDisposable Implementation
        public void Dispose()
        {
            _outputFile?.Dispose();
            _outputFile = null;
        }
        #endregion

        #region Private Fields
        private string _configFile = string.Empty;
        private StreamWriter? _outputFile;
        private readonly Dictionary<string, string> _rawParameters =
            new Dictionary<string, string>();
        private PluginBase? _plugin;
        private OutputLevel? _outputLevel;
        private List<(OutputLevel, string)>? _bufferedOutput = new List<(OutputLevel, string)>();
        #endregion
    }
}
