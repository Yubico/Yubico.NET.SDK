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

namespace Yubico.YubiKit.Fido2.Ctap;

/// <summary>
/// CTAP2 command codes per FIDO Alliance specification.
/// </summary>
/// <remarks>
/// <para>
/// See: https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-errata-20220621.html#commands
/// </para>
/// </remarks>
public static class CtapCommand
{
    /// <summary>
    /// authenticatorMakeCredential (0x01)
    /// </summary>
    public const byte MakeCredential = 0x01;
    
    /// <summary>
    /// authenticatorGetAssertion (0x02)
    /// </summary>
    public const byte GetAssertion = 0x02;
    
    /// <summary>
    /// authenticatorGetInfo (0x04)
    /// </summary>
    public const byte GetInfo = 0x04;
    
    /// <summary>
    /// authenticatorClientPin (0x06)
    /// </summary>
    public const byte ClientPin = 0x06;
    
    /// <summary>
    /// authenticatorReset (0x07)
    /// </summary>
    public const byte Reset = 0x07;
    
    /// <summary>
    /// authenticatorGetNextAssertion (0x08)
    /// </summary>
    public const byte GetNextAssertion = 0x08;
    
    /// <summary>
    /// authenticatorBioEnrollment (0x09) - CTAP 2.1
    /// </summary>
    public const byte BioEnrollment = 0x09;
    
    /// <summary>
    /// authenticatorCredentialManagement (0x0A) - CTAP 2.1
    /// </summary>
    public const byte CredentialManagement = 0x0A;
    
    /// <summary>
    /// authenticatorSelection (0x0B) - CTAP 2.1
    /// </summary>
    public const byte Selection = 0x0B;
    
    /// <summary>
    /// authenticatorLargeBlobs (0x0C) - CTAP 2.1
    /// </summary>
    public const byte LargeBlobs = 0x0C;
    
    /// <summary>
    /// authenticatorConfig (0x0D) - CTAP 2.1
    /// </summary>
    public const byte Config = 0x0D;
    
    /// <summary>
    /// prototypeBioEnrollment (0x40) - Prototype command
    /// </summary>
    public const byte PrototypeBioEnrollment = 0x40;
    
    /// <summary>
    /// prototypeCredentialManagement (0x41) - Prototype command
    /// </summary>
    public const byte PrototypeCredentialManagement = 0x41;
}
