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
using Yubico.YubiKey.InterIndustry.Commands;

namespace Yubico.YubiKey.Oath;

/// <summary>
///     Represents the information returned when selecting the OATH application.
///     Includes information such the salt and challenge which are needed for setting/validating a password.
/// </summary>
public class OathApplicationData : ISelectApplicationData
{
    // We explicitly do not want a default constructor for this command.
    private OathApplicationData()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Constructs an instance of the <see cref="OathApplicationData" /> class.
    /// </summary>
    public OathApplicationData(
        ReadOnlyMemory<byte> rawData,
        FirmwareVersion version,
        ReadOnlyMemory<byte> salt,
        ReadOnlyMemory<byte> challenge,
        HashAlgorithm algorithm = HashAlgorithm.Sha1)
    {
        if (version is null)
        {
            throw new ArgumentNullException(nameof(version));
        }

        if (challenge.Length != 0 && challenge.Length != 8)
        {
            throw new ArgumentException(ExceptionMessages.InvalidChallengeLength);
        }

        RawData = rawData;
        Version = version;
        Salt = salt;
        Challenge = challenge;
        Algorithm = algorithm;
    }

    /// <summary>
    ///     The version of the firmware currently running on the YubiKey.
    /// </summary>
    public FirmwareVersion Version { get; }

    /// <summary>
    ///     The device identifier.
    /// </summary>
    public ReadOnlyMemory<byte> Salt { get; }

    /// <summary>
    ///     The 8 byte challenge if authentication is configured.
    /// </summary>
    public ReadOnlyMemory<byte> Challenge { get; }

    /// <summary>
    ///     What hash algorithm to use.
    /// </summary>
    public HashAlgorithm Algorithm { get; }

    #region ISelectApplicationData Members

    /// <inheritdoc />
    public ReadOnlyMemory<byte> RawData { get; }

    #endregion
}
