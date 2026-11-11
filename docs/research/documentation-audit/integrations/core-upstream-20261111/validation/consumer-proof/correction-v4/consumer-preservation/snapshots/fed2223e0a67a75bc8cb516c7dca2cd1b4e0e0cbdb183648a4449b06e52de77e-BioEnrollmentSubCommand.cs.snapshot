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

namespace Yubico.YubiKit.Ctap2.BioEnrollment;

/// <summary>
/// Local biometric subcommand constants used by the current helper.
/// </summary>
/// <remarks>
/// <para>
/// CTAP 2.1 (2022-06-21 errata), Section 6.7, defines begin/next/cancel/enumerate/rename/remove
/// as 1/2/3/4/5/6 and sensor info as 7. The constants below do not match those standard values.
/// Their presence is not evidence of an interoperable standard or vendor command profile.
/// </para>
/// </remarks>
internal static class BioEnrollmentSubCommand
{
    /// <summary>
    /// Local sensor-info value 0x01; the standard assigns 0x07 (not used by the current sensor request).
    /// </summary>
    public const byte GetFingerprintSensorInfo = 0x01;

    /// <summary>
    /// Local begin value 0x02; the standard assigns 0x01.
    /// </summary>
    public const byte EnrollBegin = 0x02;

    /// <summary>
    /// Local continuation value 0x03; the standard assigns 0x02.
    /// </summary>
    public const byte EnrollCaptureNextSample = 0x03;

    /// <summary>
    /// Local cancellation value 0x04; the standard assigns 0x03.
    /// </summary>
    public const byte EnrollCancel = 0x04;

    /// <summary>
    /// Local enumeration value 0x05; the standard assigns 0x04.
    /// </summary>
    public const byte EnumerateEnrollments = 0x05;

    /// <summary>
    /// Local rename value 0x06; the standard assigns 0x05.
    /// </summary>
    public const byte SetFriendlyName = 0x06;

    /// <summary>
    /// Local removal value 0x07; the standard assigns 0x06.
    /// </summary>
    public const byte RemoveEnrollment = 0x07;

    /// <summary>
    /// Unused local template-info value 0x08.
    /// </summary>
    /// <remarks>
    /// No corresponding subcommand is defined in the cited CTAP revision, and no
    /// original vendor definition establishes this value's meaning or support.
    /// </remarks>
    public const byte GetTemplateInfo = 0x08;
}