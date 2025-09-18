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

namespace Yubico.YubiKey.TestUtilities;

public class PemHelper
{
    /// <summary>
    ///     Returns the Base64-encoded data without PEM headers and footers.
    /// </summary>
    /// <returns>Base64 string of the cryptographic data.</returns>
    public static string AsBase64String(
        string pemString)
    {
        return StripPemHeaderFooter(pemString);
    }

    public static byte[] GetBytesFromPem(
        string pemData)
    {
        var withoutNewlines = pemData.Replace("\n", "").Trim();
        var base64 = StripPemHeaderFooter(withoutNewlines);
        return Convert.FromBase64String(base64);
    }

    private static string StripPemHeaderFooter(
        string pemData)
    {
        var base64 = pemData
            .Replace("-----BEGIN PUBLIC KEY-----", "")
            .Replace("-----END PUBLIC KEY-----", "")
            .Replace("-----BEGIN PRIVATE KEY-----", "")
            .Replace("-----END PRIVATE KEY-----", "")
            .Replace("-----BEGIN EC PRIVATE KEY-----", "")
            .Replace("-----END EC PRIVATE KEY-----", "")
            .Replace("-----BEGIN CERTIFICATE-----", "")
            .Replace("-----END CERTIFICATE-----", "")
            .Replace("-----BEGIN CERTIFICATE REQUEST-----", "")
            .Replace("-----END CERTIFICATE REQUEST-----", "")
            .Replace("\n", "")
            .Trim();
        return base64;
    }
}
