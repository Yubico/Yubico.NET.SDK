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
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Fido2.Commands;

/// <summary>
///     A base class for CredentialManagementCommand classes to share code.
/// </summary>
public class CredentialMgmtSubCommand
{
    /// <summary>
    ///     The default constructor explicitly defined. We don't want it to be used.
    /// </summary>
    /// <exception cref="NotImplementedException">
    ///     If this constructor is called.
    /// </exception>
    protected CredentialMgmtSubCommand()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Constructs a new instance of <see cref="CredentialMgmtSubCommand" />.
    /// </summary>
    protected CredentialMgmtSubCommand(CredentialManagementCommand command)
    {
        Command = command;
    }

    /// <summary>
    ///     The object that will perform many of the operations. It contains
    ///     shared code.
    /// </summary>
    protected CredentialManagementCommand Command { get; }

    /// <inheritdoc />
    public YubiKeyApplication Application => Command.Application;

    /// <summary>
    ///     Indicates whether the Apdu should be built with the
    ///     CredentialMgmtPreview command or not.
    /// </summary>
    /// <remarks>
    ///     The authenticatorCredentialManagement command was introduced in
    ///     FIDO2.1. Hence, YubiKeys that do not support 2.1 will not have this
    ///     feature. However, there was a version "2_1_PRE" which contained the
    ///     "CredentialMgmtPreview" command. Each "credMgmt" command has a
    ///     corresponding operation in this preview command.
    ///     <para>
    ///         If the YubiKey does not support "credMgmt" but does support
    ///         "CredentialMgmtPreview", then set this boolean to <c>true</c>.
    ///         Otherwise, do nothing, it will be <c>false</c> by default.
    ///     </para>
    ///     <para>
    ///         When building the APDU, this class will use the appropriate command
    ///         byte, based on this property.
    ///     </para>
    ///     <para>
    ///         Note that a YubiKey that supports only "CredentialMgmtPreview" and
    ///         "FIDO_2_1_PRE" will not support AuthTokens with permissions. In this
    ///         case, the input <c>pinUvAuthToken</c> will need to be a PinToken.
    ///     </para>
    /// </remarks>
    public bool IsPreview { get; set; }

    /// <summary>
    ///     Creates a well-formed CommandApdu to send to the YubiKey.
    /// </summary>
    public CommandApdu CreateCommandApdu() => Command.CreateCommandApdu(IsPreview);
}
