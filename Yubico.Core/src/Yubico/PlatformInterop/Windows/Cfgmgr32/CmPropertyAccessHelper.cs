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
using System.Buffers.Binary;
using System.Diagnostics;
using System.Text;
using Yubico.Core.Buffers;
using static Yubico.PlatformInterop.NativeMethods;

namespace Yubico.PlatformInterop
{

    internal class CmPropertyAccessHelper
    {
        internal delegate CmErrorCode GetObjectProperty<T>(
            T ObjectId,
            in DEVPROPKEY propertyKey,
            out DEVPROP_TYPE propertyType,
            byte[]? propertyBuffer,
            ref IntPtr propertyBufferSize
            );

        internal static object? TryGetProperty<T>(GetObjectProperty<T> getObjectProperty, T objectId, DEVPROPKEY propertyKey)
        {
            IntPtr propertyBufferSize = IntPtr.Zero;
            CmErrorCode errorCode = getObjectProperty(
                objectId,
                propertyKey,
                out DEVPROP_TYPE propertyType,
                null,
                ref propertyBufferSize
                );

            if (errorCode == CmErrorCode.CR_NO_SUCH_VALUE)
            {
                return null;
            }
            
            if (errorCode != CmErrorCode.CR_BUFFER_SMALL)
            {
                throw new PlatformApiException(
                    "CONFIG_RET",
                    (int)errorCode,
                    $"Failed to get the size needed for the property {propertyKey} for ConfigMgr object {objectId}."
                    );
            }

            byte[] propertyBuffer = new byte[propertyBufferSize.ToInt64()];
            errorCode = getObjectProperty(
                objectId,
                propertyKey,
                out _,
                propertyBuffer,
                ref propertyBufferSize
                );

            if (errorCode != CmErrorCode.CR_SUCCESS)
            {
                throw new PlatformApiException(
                    "CONFIG_RET",
                    (int)errorCode,
                    $"Failed to get the property {propertyKey} for ConfigMgr object {objectId}."
                    );
            }

            switch (propertyType)
            {
                case DEVPROP_TYPE.BOOLEAN:
                    return propertyBuffer[0] != 0;

                case DEVPROP_TYPE.GUID:
                    Debug.Assert(propertyBufferSize.ToInt32() == 16);
                    return new Guid(propertyBuffer);

                case DEVPROP_TYPE.STRING:
                    return Encoding.Unicode.GetString(propertyBuffer);

                case DEVPROP_TYPE.STRING_LIST:
                    return MultiString.GetStrings(propertyBuffer, Encoding.Unicode);

                case DEVPROP_TYPE.UINT32:
                    Debug.Assert(propertyBufferSize.ToInt32() == 4);
                    return BinaryPrimitives.ReadUInt32LittleEndian(propertyBuffer);

                case DEVPROP_TYPE.UINT16:
                    Debug.Assert(propertyBufferSize.ToInt32() == 2);
                    return BinaryPrimitives.ReadUInt16LittleEndian(propertyBuffer);

                default:
                    throw new NotSupportedException($"GetProperty does not support properties of type {propertyType}");
            }
        }
    }
}
