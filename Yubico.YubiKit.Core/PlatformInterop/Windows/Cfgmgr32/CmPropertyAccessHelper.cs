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

using System.Buffers.Binary;
using System.Diagnostics;
using System.Text;
using Yubico.YubiKit.Core.Core.Buffers;

namespace Yubico.YubiKit.Core.PlatformInterop.Windows.Cfgmgr32;

internal class CmPropertyAccessHelper
{
    internal static object? TryGetProperty<T>(GetObjectProperty<T> getObjectProperty, T objectId,
        NativeMethods.DEVPROPKEY propertyKey)
    {
        NativeMethods.CmErrorCode errorCode;

        IntPtr propertyBufferSize = IntPtr.Zero;
        errorCode = getObjectProperty(
            objectId,
            propertyKey,
            out NativeMethods.DEVPROP_TYPE propertyType,
            null,
            ref propertyBufferSize
        );

        if (errorCode == NativeMethods.CmErrorCode.CR_NO_SUCH_VALUE) return default;

        if (errorCode != NativeMethods.CmErrorCode.CR_BUFFER_SMALL)
            throw new PlatformApiException(
                "CONFIG_RET",
                (int)errorCode,
                $"Failed to get the size needed for the property {propertyKey} for ConfigMgr object {objectId}."
            );

        byte[] propertyBuffer = new byte[propertyBufferSize.ToInt64()];
        errorCode = getObjectProperty(
            objectId,
            propertyKey,
            out _,
            propertyBuffer,
            ref propertyBufferSize
        );

        if (errorCode != NativeMethods.CmErrorCode.CR_SUCCESS)
            throw new PlatformApiException(
                "CONFIG_RET",
                (int)errorCode,
                $"Failed to get the property {propertyKey} for ConfigMgr object {objectId}."
            );

        switch (propertyType)
        {
            case NativeMethods.DEVPROP_TYPE.BOOLEAN:
                return propertyBuffer[0] != 0;

            case NativeMethods.DEVPROP_TYPE.GUID:
                Debug.Assert(propertyBufferSize.ToInt32() == 16);
                return new Guid(propertyBuffer);

            case NativeMethods.DEVPROP_TYPE.STRING:
                return Encoding.Unicode.GetString(propertyBuffer);

            case NativeMethods.DEVPROP_TYPE.STRING_LIST:
                return MultiString.GetStrings(propertyBuffer, Encoding.Unicode);

            case NativeMethods.DEVPROP_TYPE.UINT32:
                Debug.Assert(propertyBufferSize.ToInt32() == 4);
                return BinaryPrimitives.ReadUInt32LittleEndian(propertyBuffer);

            case NativeMethods.DEVPROP_TYPE.UINT16:
                Debug.Assert(propertyBufferSize.ToInt32() == 2);
                return BinaryPrimitives.ReadUInt16LittleEndian(propertyBuffer);

            default:
                throw new NotSupportedException($"GetProperty does not support properties of type {propertyType}");
        }
    }

    #region Nested type: GetObjectProperty

    internal delegate NativeMethods.CmErrorCode GetObjectProperty<T>(
        T ObjectId,
        in NativeMethods.DEVPROPKEY propertyKey,
        out NativeMethods.DEVPROP_TYPE propertyType,
        byte[]? propertyBuffer,
        ref IntPtr propertyBufferSize
    );

    #endregion
}