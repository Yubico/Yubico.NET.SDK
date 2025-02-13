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
using System.Linq;

namespace Yubico.YubiKey.TestApp
{
    /// <summary>
    /// Container for plug in properties supplied either on the command line or
    /// in a config file.
    /// </summary>
    public class Parameter
    {
        /// <summary>
        /// The name of the parameter, used for both command-line and config file.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The short name of the parameter, used only for command-line.
        /// </summary>
        public string Shortcut { get; set; } = string.Empty;

        /// <summary>
        /// Short description used for the auto generated usage output.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// If this is true, then the plugin will not run without this parameter
        /// set.
        /// </summary>
        public bool Required { get; set; }

        /// <summary>
        /// The CLR type of the parameter. Used for converting the string
        /// representation.
        /// </summary>
        public Type Type { get; set; } = typeof(string);

        /// <summary>
        /// The value of the object after it is parsed from the string
        /// representation.
        /// </summary>
        public object? Value { get; set; }

        /// <inheritdoc/>
        public override string ToString() =>
            string.Join(", ", new[]
            {
                (!string.IsNullOrWhiteSpace(Name) ? $"Name[{ Name }]" : string.Empty) +
                (!string.IsNullOrWhiteSpace(Shortcut) ? $"({Shortcut})" : string.Empty),
                $"Type[{ Type }]",
                Value != null ? $"Value[{ Value }]" : string.Empty
            }.Where(s => !string.IsNullOrWhiteSpace(s)));
    }
}
