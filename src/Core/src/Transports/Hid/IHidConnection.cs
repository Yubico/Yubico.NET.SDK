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

public interface IHidConnection : IConnection
{
    int InputReportSize { get; }
    int OutputReportSize { get; }

    /// <summary>Sends a report through this synchronous connection.</summary>
    /// <remarks>
    ///     The caller owns <paramref name="report" />. Its contents are valid only until this method returns;
    ///     the caller may clear or reuse the array immediately afterward. An implementation that needs the
    ///     report after returning must copy it before returning.
    /// </remarks>
    void SetReport(byte[] report);
    byte[] GetReport();
}