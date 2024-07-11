// Copyright 2023 Yubico AB
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

namespace Yubico.YubiKey.Fido2.Commands
{
    /// <summary>
    /// Holds the template information for one enrolled fingerprint.
    /// </summary>
    /// <remarks>
    /// A Bio series YubiKey will store a numerical representation of each
    /// enrolled fingerprint. This numerical representation is known as a
    /// "template". Each template in a YubiKey is identified by a templateId. It
    /// is possible to set a friendly name as well.
    /// <para>
    /// When operating on or specifying a fingerprint, it is generally necessary
    /// to supply the templateId. However, because templateIds are binary byte
    /// arrays, it is not practical to offer a user a choice based on templateId.
    /// Hence, the user has the option of assigning a friendly name to each
    /// fingerprint template. In that way, a user can make a choice based on the
    /// name, and the code can use its associated templateId.
    /// </para>
    /// <para>
    /// When enumerating fingerprint templates, the YubiKey will return the
    /// templateId and friendly name for each enrolled fingerprint. The SDK
    /// presents this enumeration as a List of <c>TemplateInfo</c>.
    /// </para>
    /// </remarks>
    public class TemplateInfo
    {
        /// <summary>
        /// The templateId of the enrolled fingerprint.
        /// </summary>
        public ReadOnlyMemory<byte> TemplateId { get; private set; }

        /// <summary>
        /// The friendlyName of the enrolled fingerprint. If there is no name set
        /// for this print, this property will be the empty string ("").
        /// </summary>
        public string FriendlyName { get; private set; }

        // The default constructor explicitly defined. We don't want it to be
        // used.
        private TemplateInfo()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Build a new instance of <see cref="TemplateInfo"/> from the given
        /// <c>templateId</c> and <c>friendlyName</c>. This will copy a reference
        /// to the <c>templateId</c>.
        /// </summary>
        /// <param name="templateId">
        /// The ID used by the YubiKey to determine which template to use.
        /// </param>
        /// <param name="friendlyName">
        /// A name to make it easier to select an appropriate template.
        /// </param>
        public TemplateInfo(ReadOnlyMemory<byte> templateId, string friendlyName)
        {
            TemplateId = templateId;
            FriendlyName = friendlyName;
        }
    }
}
