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

namespace Yubico.YubiKit.Core;

public class CtapException : Exception
{
    //An error response from a YubiKey
}

public class BadResponseException : Exception
{
    //The data contained in a YubiKey response was invalid
}

/* We also use:
InvalidOperationException (IllegalStateException)
TimeoutException The operation timed out waiting for something
*/