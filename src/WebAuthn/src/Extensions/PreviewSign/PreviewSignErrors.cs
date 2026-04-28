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

using Yubico.YubiKit.Fido2.Ctap;

namespace Yubico.YubiKit.WebAuthn.Extensions.PreviewSign;

/// <summary>
/// Maps CTAP error codes to WebAuthn client errors for previewSign extension.
/// </summary>
internal static class PreviewSignErrors
{
    /// <summary>
    /// Maps a CTAP exception to a typed WebAuthn client error.
    /// </summary>
    /// <param name="ex">The CTAP exception.</param>
    /// <returns>
    /// A <see cref="WebAuthnClientError"/> with an appropriate error code and message.
    /// </returns>
    public static WebAuthnClientError MapCtapError(CtapException ex) =>
        ex.Status switch
        {
            CtapStatus.UnsupportedAlgorithm => new WebAuthnClientError(
                WebAuthnClientErrorCode.NotSupported,
                "previewSign: requested algorithm not supported by authenticator",
                ex),

            CtapStatus.InvalidOption => new WebAuthnClientError(
                WebAuthnClientErrorCode.InvalidState,
                "previewSign: invalid option (e.g. malformed flags)",
                ex),

            CtapStatus.UpRequired => new WebAuthnClientError(
                WebAuthnClientErrorCode.NotAllowed,
                "previewSign: user presence required but not provided",
                ex),

            // Note: CtapStatus constant is "PuvathRequired" (not "PuatRequired")
            CtapStatus.PuvathRequired => new WebAuthnClientError(
                WebAuthnClientErrorCode.NotAllowed,
                "previewSign: PIN/UV auth token required",
                ex),

            CtapStatus.InvalidCredential => new WebAuthnClientError(
                WebAuthnClientErrorCode.InvalidRequest,
                "previewSign: signByCredential references unknown credential",
                ex),

            CtapStatus.MissingParameter => new WebAuthnClientError(
                WebAuthnClientErrorCode.InvalidRequest,
                "previewSign: missing required parameter",
                ex),

            _ => new WebAuthnClientError(
                WebAuthnClientErrorCode.Unknown,
                $"previewSign CTAP error: {ex.Status}",
                ex)
        };
}
