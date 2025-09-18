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

namespace Yubico.YubiKey;

/// <summary>
///     Represents an arbitrary pipeline that accepts command APDUs and returns response APDUs.
/// </summary>
/// <remarks>
///     Can often be composed, using the constructor of an implementing type.
/// </remarks>
public interface IApduTransform
{
    /// <summary>
    ///     Passes the supplied command into the pipeline, and returns the final response.
    /// </summary>
    ResponseApdu Invoke(CommandApdu command, Type commandType, Type responseType);

    /// <summary>
    ///     Sets up the pipeline; should be called only once, before any `Invoke` calls.
    /// </summary>
    void Setup();

    /// <summary>
    ///     Cleans up the pipeline; should be called only once, after all `Invoke` calls.
    /// </summary>
    void Cleanup();
}
