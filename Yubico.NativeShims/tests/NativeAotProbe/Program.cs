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

using System.Runtime.InteropServices;

const uint ScardScopeSystem = 2;
const uint ScardSuccess = 0;

IntPtr bigNumber = NativeMethods.BnNew();
if (bigNumber == IntPtr.Zero)
{
    throw new InvalidOperationException("Native_BN_new returned a null pointer.");
}

NativeMethods.BnClearFree(bigNumber);
Console.WriteLine("OpenSSL shim call succeeded.");

uint establishResult = NativeMethods.SCardEstablishContext(ScardScopeSystem, out IntPtr context);
if (establishResult == ScardSuccess)
{
    uint releaseResult = NativeMethods.SCardReleaseContext(context);
    if (releaseResult != ScardSuccess)
    {
        throw new InvalidOperationException($"Native_SCardReleaseContext failed: 0x{releaseResult:X8}.");
    }

    Console.WriteLine("PC/SC shim calls succeeded.");
}
else
{
    // A runner need not have a smart-card service. Returning a PC/SC status proves
    // that the statically linked entry point was reached.
    Console.WriteLine($"PC/SC service unavailable: 0x{establishResult:X8} (accepted).");
}

internal static partial class NativeMethods
{
    private const string NativeShims = "Yubico.NativeShims";

    [DllImport(NativeShims, EntryPoint = "Native_BN_new", ExactSpelling = true)]
    internal static extern IntPtr BnNew();

    [DllImport(NativeShims, EntryPoint = "Native_BN_clear_free", ExactSpelling = true)]
    internal static extern void BnClearFree(IntPtr bigNumber);

    [DllImport(NativeShims, EntryPoint = "Native_SCardEstablishContext", ExactSpelling = true)]
    internal static extern uint SCardEstablishContext(uint scope, out IntPtr context);

    [DllImport(NativeShims, EntryPoint = "Native_SCardReleaseContext", ExactSpelling = true)]
    internal static extern uint SCardReleaseContext(IntPtr context);
}
