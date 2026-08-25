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

using System;
using System.Runtime.InteropServices;

internal static class Program
{
    private const uint ScopeUser = 0;

    [DllImport(
        "Yubico.NativeShims",
        EntryPoint = "Native_SCardEstablishContext",
        ExactSpelling = true)]
    private static extern uint EstablishContext(uint scope, out nint context);

    [DllImport(
        "Yubico.NativeShims",
        EntryPoint = "Native_SCardReleaseContext",
        ExactSpelling = true)]
    private static extern uint ReleaseContext(nint context);

    private static int Main()
    {
        uint result = EstablishContext(ScopeUser, out nint context);
        Console.WriteLine($"SCardEstablishContext returned 0x{result:X8}.");
        if (result != 0)
        {
            return 1;
        }

        result = ReleaseContext(context);
        Console.WriteLine($"SCardReleaseContext returned 0x{result:X8}.");
        return result == 0 ? 0 : 1;
    }
}
