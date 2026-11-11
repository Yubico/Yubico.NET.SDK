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

using Yubico.YubiKit.Core.Abstractions;

namespace Yubico.YubiKit.Core.Transports.Hid;

/// <summary>Provides synchronous access to reports on an opened HID interface.</summary>
/// <remarks>
///     This low-level contract retains array-based report signatures for compatibility with custom providers.
///     Report-size metadata, accepted lengths and blocking behavior depend on the platform and report kind;
///     this interface does not impose typed FIDO or OTP protocol framing or sequencing.
/// </remarks>
public interface IHidConnection : IConnection
{
    /// <summary>Gets the input report size reported by this connection, in bytes.</summary>
    /// <remarks>
    ///     This is size metadata, not a promise that every <see cref="GetReport" /> result has this length.
    ///     The report kind and implementation determine whether it describes a capacity or a fixed size.
    /// </remarks>
    int InputReportSize { get; }

    /// <summary>Gets the output report size reported by this connection, in bytes.</summary>
    /// <remarks>
    ///     Accepted <see cref="SetReport" /> lengths depend on the report kind and implementation. Some
    ///     implementations require this exact size; others preserve supported caller-supplied lengths.
    /// </remarks>
    int OutputReportSize { get; }

    /// <summary>Sends a report through this synchronous connection.</summary>
    /// <remarks>
    ///     The caller owns <paramref name="report" />. Its contents are valid only until this method returns;
    ///     the caller may clear or reuse the array immediately afterward. An implementation that needs the
    ///     report after returning must copy it before returning.
    ///     The caller retains responsibility for clearing sensitive source contents after the call; do not
    ///     rely on connection disposal to clear caller-owned input.
    /// </remarks>
    /// <param name="report">The caller-owned report array, kept stable for the duration of this call.</param>
    void SetReport(byte[] report);

    /// <summary>Synchronously receives a report from this connection.</summary>
    /// <remarks>
    ///     The caller is responsible for handling the returned contents and clearing sensitive data after use;
    ///     do not rely on connection disposal to clear a returned report. Built-in implementations transfer
    ///     their result arrays to the caller. Custom providers can have different sharing or reuse rules;
    ///     consult their contract before retaining or modifying returned storage. No fresh-array, exact-length
    ///     or uniform timeout guarantee is imposed on every provider by this interface.
    /// </remarks>
    /// <returns>The received report array; its length and contents follow the report kind and implementation.</returns>
    byte[] GetReport();
}