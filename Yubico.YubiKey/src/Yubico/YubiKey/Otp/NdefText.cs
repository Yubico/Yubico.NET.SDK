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

using System.Globalization;

namespace Yubico.YubiKey.Otp
{
    /// <summary>
    /// A class that represents the various fields of an NDEF text record.
    /// </summary>
    public class NdefText
    {
        /// <summary>
        /// Represents the underlying character encoding used by the record's text.
        /// </summary>
        public NdefTextEncoding Encoding { get; set; }

        /// <summary>
        /// Represents the language that the text is written.
        /// </summary>
        public CultureInfo Language { get; set; }

        /// <summary>
        /// The message text of the NDEF text record.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Constructs a new instance of the <see cref="NdefText"/> class.
        /// </summary>
        public NdefText()
        {
            Language = CultureInfo.InvariantCulture;
            Text = string.Empty;
        }

        /// <inheritdoc/>
        public override string ToString() => string.IsNullOrEmpty(Text) ? "(null or empty)" : Text;
    }
}
