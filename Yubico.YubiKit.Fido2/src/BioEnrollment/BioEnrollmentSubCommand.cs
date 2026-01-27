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

namespace Yubico.YubiKit.Fido2.BioEnrollment;

/// <summary>
/// CTAP2.1 authenticatorBioEnrollment sub-commands.
/// </summary>
/// <remarks>
/// <para>
/// See: CTAP 2.1 Section 6.7 - authenticatorBioEnrollment (0x09)
/// </para>
/// </remarks>
internal static class BioEnrollmentSubCommand
{
    /// <summary>
    /// Get fingerprint sensor info (0x01).
    /// </summary>
    public const byte GetFingerprintSensorInfo = 0x01;
    
    /// <summary>
    /// Begin fingerprint enrollment (0x02).
    /// </summary>
    public const byte EnrollBegin = 0x02;
    
    /// <summary>
    /// Continue fingerprint enrollment (0x03).
    /// </summary>
    public const byte EnrollCaptureNextSample = 0x03;
    
    /// <summary>
    /// Cancel fingerprint enrollment (0x04).
    /// </summary>
    public const byte EnrollCancel = 0x04;
    
    /// <summary>
    /// Begin enumeration of enrolled templates (0x05).
    /// </summary>
    public const byte EnumerateEnrollments = 0x05;
    
    /// <summary>
    /// Set friendly name for enrolled template (0x06).
    /// </summary>
    public const byte SetFriendlyName = 0x06;
    
    /// <summary>
    /// Remove enrolled template (0x07).
    /// </summary>
    public const byte RemoveEnrollment = 0x07;
    
    /// <summary>
    /// Get template info - returns sample count for existing template (0x08).
    /// </summary>
    /// <remarks>
    /// This command is vendor-specific (YubiKey specific).
    /// </remarks>
    public const byte GetTemplateInfo = 0x08;
}
