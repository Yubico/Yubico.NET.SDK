// Copyright 2026 Yubico AB
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

namespace Yubico.YubiKit.Core.Credentials;

/// <summary>Identifies how an operation associated with a user-presence notification ended.</summary>
public enum UserPresenceOutcome
{
    /// <summary>The operation completed successfully.</summary>
    Completed = 0,

    /// <summary>The operation was cancelled.</summary>
    Cancelled = 1,

    /// <summary>The operation ended because its user-presence wait timed out.</summary>
    TimedOut = 2,

    /// <summary>The operation failed for a reason other than cancellation or a user-presence timeout.</summary>
    Failed = 3
}