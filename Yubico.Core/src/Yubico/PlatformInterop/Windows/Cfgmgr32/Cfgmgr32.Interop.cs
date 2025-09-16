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

namespace Yubico.PlatformInterop
{
    internal static partial class NativeMethods
    {
        #region Enumerations and flags

        /// <summary>
        /// Configuration Manager CONFIGRET return status codes
        /// </summary>
        internal enum CmErrorCode : int
        {
            CR_SUCCESS = 0x00000000,
            CR_DEFAULT = 0x00000001,
            CR_OUT_OF_MEMORY = 0x00000002,
            CR_INVALID_POINTER = 0x00000003,
            CR_INVALID_FLAG = 0x00000004,
            CR_INVALID_DEVNODE = 0x00000005,
            CR_INVALID_DEVINST = CR_INVALID_DEVNODE,
            CR_INVALID_RES_DES = 0x00000006,
            CR_INVALID_LOG_CONF = 0x00000007,
            CR_INVALID_ARBITRATOR = 0x00000008,
            CR_INVALID_NODELIST = 0x00000009,
            CR_DEVNODE_HAS_REQS = 0x0000000A,
            CR_DEVINST_HAS_REQS = CR_DEVNODE_HAS_REQS,
            CR_INVALID_RESOURCEID = 0x0000000B,
            CR_DLVXD_NOT_FOUND = 0x0000000C, // WIN 95 ONLY
            CR_NO_SUCH_DEVNODE = 0x0000000D,
            CR_NO_SUCH_DEVINST = CR_NO_SUCH_DEVNODE,
            CR_NO_MORE_LOG_CONF = 0x0000000E,
            CR_NO_MORE_RES_DES = 0x0000000F,
            CR_ALREADY_SUCH_DEVNODE = 0x00000010,
            CR_ALREADY_SUCH_DEVINST = CR_ALREADY_SUCH_DEVNODE,
            CR_INVALID_RANGE_LIST = 0x00000011,
            CR_INVALID_RANGE = 0x00000012,
            CR_FAILURE = 0x00000013,
            CR_NO_SUCH_LOGICAL_DEV = 0x00000014,
            CR_CREATE_BLOCKED = 0x00000015,
            CR_NOT_SYSTEM_VM = 0x00000016, // WIN 95 ONLY
            CR_REMOVE_VETOED = 0x00000017,
            CR_APM_VETOED = 0x00000018,
            CR_INVALID_LOAD_TYPE = 0x00000019,
            CR_BUFFER_SMALL = 0x0000001A,
            CR_NO_ARBITRATOR = 0x0000001B,
            CR_NO_REGISTRY_HANDLE = 0x0000001C,
            CR_REGISTRY_ERROR = 0x0000001D,
            CR_INVALID_DEVICE_ID = 0x0000001E,
            CR_INVALID_DATA = 0x0000001F,
            CR_INVALID_API = 0x00000020,
            CR_DEVLOADER_NOT_READY = 0x00000021,
            CR_NEED_RESTART = 0x00000022,
            CR_NO_MORE_HW_PROFILES = 0x00000023,
            CR_DEVICE_NOT_THERE = 0x00000024,
            CR_NO_SUCH_VALUE = 0x00000025,
            CR_WRONG_TYPE = 0x00000026,
            CR_INVALID_PRIORITY = 0x00000027,
            CR_NOT_DISABLEABLE = 0x00000028,
            CR_FREE_RESOURCES = 0x00000029,
            CR_QUERY_VETOED = 0x0000002A,
            CR_CANT_SHARE_IRQ = 0x0000002B,
            CR_NO_DEPENDENT = 0x0000002C,
            CR_SAME_RESOURCES = 0x0000002D,
            CR_NO_SUCH_REGISTRY_KEY = 0x0000002E,
            CR_INVALID_MACHINENAME = 0x0000002F,   // NT ONLY
            CR_REMOTE_COMM_FAILURE = 0x00000030,   // NT ONLY
            CR_MACHINE_UNAVAILABLE = 0x00000031,   // NT ONLY
            CR_NO_CM_SERVICES = 0x00000032,   // NT ONLY
            CR_ACCESS_DENIED = 0x00000033,   // NT ONLY
            CR_CALL_NOT_IMPLEMENTED = 0x00000034,
            CR_INVALID_PROPERTY = 0x00000035,
            CR_DEVICE_INTERFACE_ACTIVE = 0x00000036,
            CR_NO_SUCH_DEVICE_INTERFACE = 0x00000037,
            CR_INVALID_REFERENCE_STRING = 0x00000038,
            CR_INVALID_CONFLICT_LIST = 0x00000039,
            CR_INVALID_INDEX = 0x0000003A,
            CR_INVALID_STRUCTURE_SIZE = 0x0000003B
        }

        //
        // Flags for CM_Locate_DevNode
        //
        [Flags]
        internal enum CM_LOCATE_DEVNODE : int
        {
            NORMAL = 0x0000_0000,
            PHANTOM = 0x0000_0001,
            CANCELREMOVE = 0x0000_0002,
            NOVALIDATION = 0x0000_0004,
        }

        [Flags]
        internal enum CM_LOCATE_DEVINST : int
        {
            NORMAL = CM_LOCATE_DEVNODE.NORMAL,
            PHANTOM = CM_LOCATE_DEVNODE.PHANTOM,
            CANCELREMOVE = CM_LOCATE_DEVNODE.CANCELREMOVE,
            NOVALIDATION = CM_LOCATE_DEVNODE.NOVALIDATION,
        }

        //
        // Device notification flags for registration filters
        //

        [Flags]
        internal enum CM_NOTIFY_FILTER_FLAG : int
        {
            ALL_INTERFACE_CLASSES = 0x0000_0001,
            ALL_DEVICE_INSTANCES = 0x0000_0002,
        }

        //
        // Device notification filter types
        //

        internal enum CM_NOTIFY_FILTER_TYPE
        {
            DEVINTERFACE = 0,
            DEVICEHANDLE = 1,
            DEVICEINSTANCE = 2,
        }

        internal enum CM_NOTIFY_ACTION : int
        {
            // Filter type: CM_NOTIFY_FILTER_TYPE.DEVICEINTERFACE
            DEVICEINTERFACEARRIVAL = 0,
            DEVICEINTERFACEREMOVAL = 1,

            // Filter type: CM_NOTIFY_FILTER_TYPE.DEVICEHANDLE
            DEVICEQUERYREMOVE = 2,
            DEVICEQUERYREMOVEFAILED = 3,
            DEVICEQUERYMOVEPENDING = 4,
            DEVICEREMOVECOMPLETE = 5,
            DEVICECUSTOMEVENT = 6,

            // Filter type: CM_NOTIFY_FILTER_TYPE.DEVICEINSTANCE
            DEVICEINSTANCEENUMERATED = 7,
            DEVICEINSTANCESTARTED = 8,
            DEVICEINSTANCEREMOVED = 9,
        }

        // Flags for CM_Get_Device_Interface_List, CM_Get_Device_Interface_List_Size
        internal enum CM_GET_DEVICE_LIST : int
        {
            PRESENT = 0x0000_0000, // Only currently 'live' device interfaces
            ALL_DEVICES = 0x0000_0001, // All registered device interfaces, live or not
        }

        #endregion

        #region Structures

        // These are the values that define the CM_NOTIFY_FILTER in the Windows
        // cfgmgr32 library.
        internal const int CmNotifyFilterSize = 416;
        internal const int OffsetCbSize = 0;
        internal const int OffsetFlags = 4;
        internal const int OffsetFilterType = 8;
        internal const int OffsetReserved = 12;
        internal const int OffsetGuidData1 = 16;
        internal const int OffsetGuidData2 = 20;
        internal const int OffsetGuidData3 = 22;
        internal const int OffsetGuidData4 = 24;
        internal const int LengthGuidData4 = 8;

        [StructLayout(LayoutKind.Sequential)] // May need to be Explicit
        internal struct CM_NOTIFY_FILTER
        {
            internal int cbSize;
            internal CM_NOTIFY_FILTER_FLAG Flags;
            internal CM_NOTIFY_FILTER_TYPE FilterType;
            private readonly int MustBeZero;
            internal Guid ClassGuid;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CM_NOTIFY_EVENT_DATA
        {
            internal CM_NOTIFY_FILTER_TYPE FilterType;
            internal int Reserved;
            internal Guid ClassGuid;
            // String SymbolicLink
        }

        #endregion

        #region Delegates

        internal delegate int CM_NOTIFY_CALLBACK(IntPtr hNotify, IntPtr Context, CM_NOTIFY_ACTION Action, IntPtr EventData, int EventDataSize);

        #endregion

        #region P/Invoke DLL Imports

        // Note that the DefaultDllImportSearchPaths attribute is a security best
        // practice on the Windows platform (and required by our analyzer
        // settings). It does not currently have any effect on platforms other
        // than Windows, but is included because of the analyzer and in the hope
        // that it will be supported by these platforms in the future.
        [DllImport(Libraries.CfgMgr, CharSet = CharSet.Unicode, EntryPoint = "CM_Get_Child", ExactSpelling = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static extern CmErrorCode CM_Get_Child(
            out int childInstance,
            int devInstance,
            int mustBeZero
            );

        internal static CmErrorCode CM_Get_Child(
            out int childInstance,
            int devInstance
            ) =>
            CM_Get_Child(out childInstance, devInstance, 0);

        [DllImport(Libraries.CfgMgr, CharSet = CharSet.Unicode, EntryPoint = "CM_Get_Device_IDW", ExactSpelling = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static extern CmErrorCode CM_Get_Device_Id(
            int dnDevInst,
            char[] buffer,
            int bufferLen,
            int mustBeZero
            );

        internal static CmErrorCode CM_Get_Device_Id(
            int dnDevInst,
            char[] buffer,
            int bufferLen
            ) => CM_Get_Device_Id(dnDevInst, buffer, bufferLen, 0);

        [DllImport(Libraries.CfgMgr, CharSet = CharSet.Unicode, EntryPoint = "CM_Get_Device_ID_Size", ExactSpelling = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static extern CmErrorCode CM_Get_Device_Id_Size(
            out IntPtr pulLen,
            int dnDevInst,
            int mustBeZero
            );

        internal static CmErrorCode CM_Get_Device_Id_Size(
            out IntPtr pulLen,
            int dnDevInst
            ) => CM_Get_Device_Id_Size(out pulLen, dnDevInst, 0);

        [DllImport(Libraries.CfgMgr, CharSet = CharSet.Unicode, EntryPoint = "CM_Get_Device_Interface_ListW", ExactSpelling = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        internal static extern CmErrorCode CM_Get_Device_Interface_List(
            [MarshalAs(UnmanagedType.LPStruct)]
            Guid interfaceClassGuid,
            string? deviceId,
            byte[] byteBuffer,
            int bufferLengthCch,
            CM_GET_DEVICE_LIST flags
            );

        [DllImport(Libraries.CfgMgr, CharSet = CharSet.Unicode, EntryPoint = "CM_Get_Device_Interface_List_SizeW", ExactSpelling = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        internal static extern CmErrorCode CM_Get_Device_Interface_List_Size(
            out int bufferLengthCch,
            [MarshalAs(UnmanagedType.LPStruct)]
            Guid interfaceClassGuid,
            string? deviceId,
            CM_GET_DEVICE_LIST flags
            );

        [DllImport(Libraries.CfgMgr, CharSet = CharSet.Unicode, EntryPoint = "CM_Get_Device_Interface_PropertyW", ExactSpelling = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static extern CmErrorCode CM_Get_Device_Interface_Property(
            string deviceInterface,
            in DEVPROPKEY propertyKey,
            out DEVPROP_TYPE propertyType,
            byte[]? propertyBuffer,
            ref IntPtr propertyBufferSize,
            int mustBeZero
            );

        internal static CmErrorCode CM_Get_Device_Interface_Property(
            string deviceInterface,
            in DEVPROPKEY propertyKey,
            out DEVPROP_TYPE propertyType,
            byte[]? propertyBuffer,
            ref IntPtr propertyBufferSize
            ) => CM_Get_Device_Interface_Property(deviceInterface, propertyKey, out propertyType, propertyBuffer, ref propertyBufferSize, 0);

        [DllImport(Libraries.CfgMgr, CharSet = CharSet.Unicode, EntryPoint = "CM_Get_DevNode_PropertyW", ExactSpelling = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static extern CmErrorCode CM_Get_DevNode_Property(
            int devInstance,
            in DEVPROPKEY propertyKey,
            out DEVPROP_TYPE propertyType,
            byte[]? propertyBuffer,
            ref IntPtr propertyBufferSize,
            int mustBeZero
            );

        internal static CmErrorCode CM_Get_DevNode_Property(
            int devInstance,
            in DEVPROPKEY propertyKey,
            out DEVPROP_TYPE propertyType,
            byte[]? propertyBuffer,
            ref IntPtr propertyBufferSize
            ) => CM_Get_DevNode_Property(devInstance, in propertyKey, out propertyType, propertyBuffer, ref propertyBufferSize, 0);

        [DllImport(Libraries.CfgMgr, CharSet = CharSet.Unicode, EntryPoint = "CM_Get_Parent", ExactSpelling = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static extern CmErrorCode CM_Get_Parent(
            out int pdnDevInst,
            int dnDevInst,
            int mustBeZero
            );

        internal static CmErrorCode CM_Get_Parent(
            out int pdnDevInst,
            int dnDevInst
            ) => CM_Get_Parent(out pdnDevInst, dnDevInst, 0);

        [DllImport(Libraries.CfgMgr, CharSet = CharSet.Unicode, EntryPoint = "CM_Locate_DevNodeW", ExactSpelling = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        internal static extern CmErrorCode CM_Locate_DevNode(
            out int devInstance,
            string deviceId,
            CM_LOCATE_DEVNODE flags
            );

        [DllImport(Libraries.CfgMgr, CharSet = CharSet.Unicode, EntryPoint = "CM_Register_Notification", ExactSpelling = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        internal static extern CmErrorCode CM_Register_Notification(
            IntPtr pFilter,
            IntPtr pContext,
            CM_NOTIFY_CALLBACK pCM_NOTIFY_CALLBACK,
            out IntPtr pNotifyContext
            );

        [DllImport(Libraries.CfgMgr, CharSet = CharSet.Unicode, EntryPoint = "CM_Get_Sibling", ExactSpelling = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        internal static extern CmErrorCode CM_Get_Sibling(
            out int siblingInstance,
            int devInstance,
            int mustBeZero
            );

        internal static CmErrorCode CM_Get_Sibling(
            out int siblingInstance,
            int devInstance
            ) => CM_Get_Sibling(out siblingInstance, devInstance, 0);

        [DllImport(Libraries.CfgMgr, CharSet = CharSet.Unicode, EntryPoint = "CM_Unregister_Notification", ExactSpelling = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        internal static extern CmErrorCode CM_Unregister_Notification(
            IntPtr NotifyContext
            );

        #endregion
    }
}
