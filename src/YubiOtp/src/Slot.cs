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

namespace Yubico.YubiKit.YubiOtp;

/// <summary>
/// Represents a YubiKey OTP slot (1 or 2).
/// </summary>
public enum Slot
{
    One = 1,
    Two = 2
}

/// <summary>
/// Extension methods for mapping <see cref="Slot"/> to <see cref="ConfigSlot"/> values.
/// </summary>
public static class SlotExtensions
{
    /// <summary>
    /// Maps a <see cref="Slot"/> to the corresponding <see cref="ConfigSlot"/> for a given operation type.
    /// </summary>
    public static ConfigSlot Map(this Slot slot, SlotOperation operation) =>
        (slot, operation) switch
        {
            (Slot.One, SlotOperation.Configure) => ConfigSlot.Config1,
            (Slot.Two, SlotOperation.Configure) => ConfigSlot.Config2,
            (Slot.One, SlotOperation.Update) => ConfigSlot.Update1,
            (Slot.Two, SlotOperation.Update) => ConfigSlot.Update2,
            (Slot.One, SlotOperation.Ndef) => ConfigSlot.Ndef1,
            (Slot.Two, SlotOperation.Ndef) => ConfigSlot.Ndef2,
            (Slot.One, SlotOperation.ChallengeHmac) => ConfigSlot.ChalHmac1,
            (Slot.Two, SlotOperation.ChallengeHmac) => ConfigSlot.ChalHmac2,
            _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, "Invalid slot or operation.")
        };
}

/// <summary>
/// The type of slot operation to perform.
/// </summary>
public enum SlotOperation
{
    Configure,
    Update,
    Ndef,
    ChallengeHmac
}
