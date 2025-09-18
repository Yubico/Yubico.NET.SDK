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
using System.Runtime.InteropServices;
using Yubico.Core.Cryptography;

namespace Yubico.PlatformInterop;

/// <summary>
///     The SafeHandle that holds the Native CMAC CTX.
/// </summary>
internal class SafeEvpCmacCtx : SafeHandle
{
    /// <summary>
    ///     Create a new <c>SafeEvpCmacCtx</c>. This constructor will initialize
    ///     the <c>BlockCipherAlgorithm</c> to <c>Aes128</c>.
    /// </summary>
    public SafeEvpCmacCtx() : base(IntPtr.Zero, true)
    {
        BlockCipherAlgorithm = CmacBlockCipherAlgorithm.Aes128;
    }

    /// <summary>
    ///     Create a new <c>SafeEvpCmacCtx</c>. This constructor will initialize
    ///     the <c>BlockCipherAlgorithm</c> to <c>Aes128</c>.
    /// </summary>
    public SafeEvpCmacCtx(IntPtr invalidHandleValue, bool ownsHandle) : base(invalidHandleValue, ownsHandle)
    {
        BlockCipherAlgorithm = CmacBlockCipherAlgorithm.Aes128;
    }

    /// <summary>
    ///     This specifies which algorithm the CMAC will use as the underlying
    ///     block cipher algorithm. The constructors will initialize this to
    ///     <c>Aes128</c>. If you want to use a different algorithm, set this
    ///     property.
    /// </summary>
    public CmacBlockCipherAlgorithm BlockCipherAlgorithm { get; set; }

    /// <inheritdoc />
    public override bool IsInvalid => handle == IntPtr.Zero;

    /// <inheritdoc />
    protected override bool ReleaseHandle()
    {
        if (!IsInvalid)
        {
            NativeMethods.EvpMacCtxFree(handle);
        }

        return true;
    }
}
