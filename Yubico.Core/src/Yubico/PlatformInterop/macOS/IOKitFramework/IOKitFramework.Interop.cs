// Copyright 2021 Yubico AB
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
    internal enum kern_return_t
    {
        KERN_SUCCESS = 0,
        KERN_INVALID_ADDRESS = 1,
        KERN_PROTECTION_FAILURE = 2,
        KERN_NO_SPACE = 3,
        KERN_INVALID_ARGUMENT = 4,
        KERN_FAILURE = 5,
        KERN_RESOURCE_SHORTAGE = 6,
        KERN_NOT_RECEIVER = 7,
        KERN_NO_ACCESS = 8,
        KERN_MEMORY_FAILURE = 9,
        KERN_MEMORY_ERROR = 10,
        KERN_ALREADY_IN_SET = 11,
        KERN_NOT_IN_SET = 12,
        KERN_NAME_EXISTS = 13,
        KERN_ABORTED = 14,
        KERN_INVALID_NAME = 15,
        KERN_INVALID_TASK = 16,
        KERN_INVALID_RIGHT = 17,
        KERN_INVALID_VALUE = 18,
        KERN_UREFS_OVERFLOW = 19,
        KERN_INVALID_CAPABILITY = 20,
        KERN_RIGHT_EXISTS = 21,
        KERN_INVALID_HOST = 22,
        KERN_MEMORY_PRESENT = 23,
        KERN_MEMORY_DATA_MOVED = 24,
        KERN_MEMORY_RESTART_COPY = 25,
        KERN_INVALID_PROCESSOR_SET = 26,
        KERN_POLICY_LIMIT = 27,
        KERN_INVALID_POLICY = 28,
        KERN_INVALID_OBJECT = 29,
        KERN_ALREADY_WAITING = 30,
        KERN_DEFAULT_SET = 31,
        KERN_EXCEPTION_PROTECTED = 32,
        KERN_INVALID_LEDGER = 33,
        KERN_INVALID_MEMORY_CONTROL = 34,
        KERN_INVALID_SECURITY = 35,
        KERN_NOT_DEPRESSED = 36,
        KERN_TERMINATED = 37,
        KERN_LOCK_SET_DESTROYED = 38,
        KERN_LOCK_UNSTABLE = 39,
        KERN_LOCK_OWNED = 40,
        KERN_LOCK_OWNED_SELF = 41,
        KERN_SEMAPHORE_DESTROYED = 42,
        KERN_RPC_SERVER_TERMINATED = 43,
        KERN_RPC_TERMINATE_ORPHAN = 44,
        KERN_RPC_CONTINUE_ORPHAN = 45,
        KERN_NOT_SUPPORTED = 46,
        KERN_NODE_DOWN = 47,
        KERN_NOT_WAITING = 48,
        KERN_OPERATION_TIMED_OUT = 49,
        KERN_CODESIGN_ERROR = 50,
        KERN_POLICY_STATIC = 51,
        KERN_INSUFFICIENT_BUFFER_SIZE = 52,

        // err_system 0xE000_0000
        IOReturnError = unchecked((int)0xE000_02BC),
        IOReturnNoMemory,
        IOReturnNoResources,
        IOReturnIPCError,
        IOReturnNoDevice,
        IOReturnNotPrivileged,
        IOReturnBadArgument,
        IOReturnLockedRead,
        IOReturnLockedWrite,
        IOReturnExclusiveAccess,
        IOReturnBadMessageID,
        IOReturnUnsupported,
        IOReturnVMError,
        IOReturnInternalError,
        IOReturnIOError,
        IOReturnCannotLock,
        IOReturnNotOpen,
        IOReturnNotReadable,
        IOReturnNotWritable,
        IOReturnNotAligned,
        IOReturnBadMedia,
        IOReturnStillOpen,
        IOReturnRLDError,
        IOReturnDMAError,
        IOReturnBusy,
        IOReturnTimeout,
        IOReturnOffline,
        IOReturnNotReady,
        IOReturnNotAttached,
        IOReturnNoChannels,
        IOReturnNoSpace,
        IOReturnPortExists,
        IOReturnCannotWire,
        IOReturnNoInterrupt,
        IOReturnNoFrames,
        IOReturnMessageTooLarge,
        IOReturnNotPermitted,
        IOReturnNoPower,
        IOReturnNoMedia,
        IOReturnUnformattedMedia,
        IOReturnUnsupportedMode,
        IOReturnUnderrun,
        IOReturnOverrun,
        IOReturnDeviceError,
        IOReturnNoCompletion,
        IOReturnAborted,
        IOReturnNoBandwidth,
        IOReturnNotResponding,
        IOReturnIsoTooOld,
        IOReturnIsoTooNew,
        IOReturnNotFound,
        IOReturnInvalid,
    }

    /*
     * kern_return_t == int
     * mach_port_t == uint / int
     * CFDictionaryRef = IntPtr / NSDictionary.Handle
     * io_name_t = char[128]
     * io_object_t = int
     * io_connect_t, io_enumerator_t, io_iterator_t, io_registry_entry_t, io_service_t = int
     */
    internal static partial class NativeMethods
    {
        internal const int kIOMasterPortDefault = 0;

        /*! @function IOObjectRelease
            @abstract Releases an object handle previously returned by IOKitLib.
            @discussion All objects returned by IOKitLib should be released with this function when access to them is no longer needed. Using the object after it has been released may or may not return an error, depending on how many references the task has to the same object in the kernel.
            @param object The IOKit object to release.
            @result A kern_return_t error code. */
        [DllImport(Libraries.IOKitFramework)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static extern kern_return_t IOObjectRelease(int self);

        /*! @function IORegistryEntryGetRegistryEntryID
            @abstract Returns an ID for the registry entry that is global to all tasks.
            @discussion The entry ID returned by IORegistryEntryGetRegistryEntryID can be used to identify a registry entry across all tasks. A registry entry may be looked up by its entryID by creating a matching dictionary with IORegistryEntryIDMatching() to be used with the IOKit matching functions. The ID is valid only until the machine reboots.
            @param entry The registry entry handle whose ID to look up.
            @param entryID The resulting ID.
            @result A kern_return_t error code. */
        [DllImport(Libraries.IOKitFramework)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static extern kern_return_t IORegistryEntryGetRegistryEntryID(int self, out long entryID);

        /*! @function IORegistryEntryIDMatching
            @abstract Create a matching dictionary that specifies an IOService match based on a registry entry ID.
            @discussion This function creates a matching dictionary that will match a registered, active IOService found with the given registry entry ID. The entry ID for a registry entry is returned by IORegistryEntryGetRegistryEntryID().
            @param entryID The registry entry ID to be found.
            @result The matching dictionary created, is returned on success, or zero on failure. The dictionary is commonly passed to IOServiceGetMatchingServices or IOServiceAddNotification which will consume a reference, otherwise it should be released with CFRelease by the caller. */
        [DllImport(Libraries.IOKitFramework)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static extern IntPtr IORegistryEntryIDMatching(long entryID); /* Returns retained */

        /*!
            @function IOServiceGetMatchingService
            @abstract Look up a registered IOService object that matches a matching dictionary.
            @discussion This is the preferred method of finding IOService objects currently registered by IOKit (that is, objects that have had their registerService() methods invoked). To find IOService objects that aren't yet registered, use an iterator as created by IORegistryEntryCreateIterator(). IOServiceAddMatchingNotification can also supply this information and install a notification of new IOServices. The matching information used in the matching dictionary may vary depending on the class of service being looked up.
            @param masterPort The master port obtained from IOMasterPort(). Pass kIOMasterPortDefault to look up the default master port.
            @param matching A CF dictionary containing matching information, of which one reference is always consumed by this function (Note prior to the Tiger release there was a small chance that the dictionary might not be released if there was an error attempting to serialize the dictionary). IOKitLib can construct matching dictionaries for common criteria with helper functions such as IOServiceMatching, IOServiceNameMatching, IOBSDNameMatching.
            @result The first service matched is returned on success. The service must be released by the caller.
          */
        [DllImport(Libraries.IOKitFramework)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static extern int IOServiceGetMatchingService(int masterPort, IntPtr matching /* Releases arg */);
    }
}
