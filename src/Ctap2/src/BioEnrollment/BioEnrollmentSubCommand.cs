// Copyright 2025 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
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

/// <summary>Standard CTAP 2.1 Section 6.7 fingerprint subcommands.</summary>
internal static class BioEnrollmentSubCommand
{
    public const byte EnrollBegin = 0x01;
    public const byte EnrollCaptureNextSample = 0x02;
    public const byte EnrollCancel = 0x03;
    public const byte EnumerateEnrollments = 0x04;
    public const byte SetFriendlyName = 0x05;
    public const byte RemoveEnrollment = 0x06;
    public const byte GetFingerprintSensorInfo = 0x07;
}