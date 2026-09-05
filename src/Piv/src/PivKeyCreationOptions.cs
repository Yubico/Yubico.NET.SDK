// Copyright 2026 Yubico AB
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

namespace Yubico.YubiKit.Piv;

/// <summary>Configures the use policies assigned to a generated or imported PIV key.</summary>
public sealed class PivKeyCreationOptions
{
    /// <summary>Gets the PIN verification policy assigned to the key.</summary>
    public PivPinPolicy PinPolicy { get; init; } = PivPinPolicy.Default;

    /// <summary>Gets the physical-touch policy assigned to the key.</summary>
    public PivTouchPolicy TouchPolicy { get; init; } = PivTouchPolicy.Default;
}
