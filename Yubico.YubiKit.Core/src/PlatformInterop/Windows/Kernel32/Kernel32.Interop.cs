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

using Microsoft.Win32.SafeHandles;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Yubico.YubiKit.Core.PlatformInterop.Windows.Kernel32;

internal static partial class NativeMethods
{
    #region Enumerations and flags

    [Flags]
    [SuppressMessage("Design", "CA1069:Enums values should not be duplicated",
        Justification = "Keeping interop as close to original C headers as possible")]
    internal enum DESIRED_ACCESS
    {
        NONE = 0x0,

        FILE_READ_DATA = 0x0000_0001, // file & pipe
        FILE_LIST_DIRECTORY = 0x0000_0001, // directory

        FILE_WRITE_DATA = 0x0000_0002, // file & pipe
        FILE_ADD_FILE = 0x0000_0002, // directory

        FILE_APPEND_DATA = 0x0000_0004, // file
        FILE_ADD_SUBDIRECTORY = 0x0000_0004, // directory
        FILE_CREATE_PIPE_INSTANCE = 0x0000_0004, // named pipe

        FILE_READ_EA = 0x0000_0008, // file & directory

        FILE_WRITE_EA = 0x0000_0010, // file & directory

        FILE_EXECUTE = 0x0000_0020, // file
        FILE_TRAVERSE = 0x0000_0020, // directory

        FILE_DELETE_CHILD = 0x0000_0040, // directory

        FILE_READ_ATTRIBUTES = 0x0000_0080, // all

        FILE_WRITE_ATTRIBUTES = 0x0000_0100, // all

        FILE_ALL_ACCESS = STANDARD_RIGHTS_REQUIRED | SYNCHRONIZE | 0x1FF,

        DELETE = 0x0001_0000,
        READ_CONTROL = 0x0002_0000,
        WRITE_DAC = 0x0004_0000,
        WRITE_OWNER = 0x0008_0000,
        SYNCHRONIZE = 0x0010_0000,

        STANDARD_RIGHTS_REQUIRED = 0x000F_0000,

        STANDARD_RIGHTS_READ = READ_CONTROL,
        STANDARD_RIGHTS_WRITE = READ_CONTROL,
        STANDARD_RIGHTS_EXECUTE = READ_CONTROL,

        STANDARD_RIGHTS_ALL = 0x001F_0000,

        SPECIFIC_RIGHTS_ALL = 0x0000_FFFF,

        FILE_GENERIC_READ = STANDARD_RIGHTS_READ | FILE_READ_DATA | FILE_READ_ATTRIBUTES | FILE_READ_EA |
                            SYNCHRONIZE,

        FILE_GENERIC_WRITE = STANDARD_RIGHTS_WRITE | FILE_WRITE_DATA | FILE_WRITE_ATTRIBUTES | FILE_WRITE_EA |
                             FILE_APPEND_DATA | SYNCHRONIZE,
        FILE_GENERIC_EXECUTE = STANDARD_RIGHTS_EXECUTE | FILE_READ_ATTRIBUTES | FILE_EXECUTE | SYNCHRONIZE,

        GENERIC_READ = unchecked((int)0x8000_0000),
        GENERIC_WRITE = 0x4000_0000,
        GENERIC_EXECUTE = 0x2000_0000,
        GENERIC_ALL = 0x1000_0000
    }

    [Flags]
    internal enum FILE_SHARE
    {
        NONE = 0x00,
        READ = 0x01,
        WRITE = 0x02,
        DELETE = 0x04,

        READWRITE = READ | WRITE,
        ALL = READWRITE | DELETE
    }

    internal enum CREATION_DISPOSITION
    {
        CREATE_NEW = 1,
        CREATE_ALWAYS = 2,
        OPEN_EXISTING = 3,
        OPEN_ALWAYS = 4,
        TRUNACTE_EXISTING = 5
    }

    [Flags]
    [SuppressMessage("Design", "CA1069:Enums values should not be duplicated",
        Justification = "Keeping interop as close to original C headers as possible")]
    internal enum FILE_FLAG
    {
        // Attributes
        READONLY = 0x0000_0001,
        HIDDEN = 0x0000_0002,
        SYSTEM = 0x0000_0004,
        DIRECTORY = 0x0000_0010,
        ARCHIVE = 0x0000_0020,
        DEVICE = 0x0000_0040,
        NORMAL = 0x0000_0080,
        TEMPORARY = 0x0000_0100,
        SPARSE_FILE = 0x0000_0200,
        REPARSE_POINT = 0x0000_0400,
        COMPRESSED = 0x0000_0800,
        OFFLINE = 0x0000_1000,
        NOT_CONTENT_INDEXED = 0x0000_2000,
        ENCRYPTED = 0x0000_4000,
        INTEGRITY_STREAM = 0x0000_8000,
        VIRTUAL = 0x0001_0000,
        NO_SCRUB_DATA = 0x0002_0000,
        EA = 0x0004_0000,
        PINNED = 0x0008_0000,
        UNPINNED = 0x0010_0000,
        RECALL_ON_OPEN = 0x0004_0000,
        RECALL_ON_DATA_ACCESS = 0x0040_0000,
        WRITE_THROUGH = unchecked((int)0x8000_0000),

        // Flags
        OVERLAPPED = 0x4000_0000,
        NO_BUFFERING = 0x2000_0000,
        RANDOM_ACCESS = 0x1000_0000,
        SEQUENTIAL_SCAN = 0x0800_0000,
        DELETE_ON_CLOSE = 0x0400_0000,
        BACKUP_SEMANTICS = 0x0200_0000,
        POSIX_SEMANTICS = 0x0100_0000,
        SESSION_AWARE = 0x0080_0000,
        OPEN_REPARSE_POINT = 0x0020_0000,
        OPEN_NO_RECALL = 0x0010_0000,
        FIRST_PIPE_INSTANCE = 0x0008_0000,
        FLAG_OPEN_REQUIRING_OPLOCK = 0x0004_0000

        // Security QOS
    }

    #endregion

    #region P/Invoke DLL Imports

    [LibraryImport(Libraries.Kernel32, EntryPoint = "CreateFileW", SetLastError = true,
        StringMarshalling = StringMarshalling.Utf8)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static partial SafeFileHandle CreateFile(
        string lpFileName,
        DESIRED_ACCESS dwDesiredAccess,
        FILE_SHARE dwShareMode,
        IntPtr lpSecurityAttributes,
        CREATION_DISPOSITION dwCreationDisposition,
        FILE_FLAG dwFlagsAndAttributes,
        IntPtr hTemplateFile
    );

    //SYSLIB1092: The usage of 'LibraryImportAttribute' does not follow recommendations. It is recommended to use explicit '[In]' and '[Out]' attributes on array parameters.
    [LibraryImport(Libraries.Kernel32, EntryPoint = "WriteFile", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool WriteFile(
        SafeFileHandle handle,
        [In] byte[] lpBuffer,
        int numBytesToWrite,
        out int numBytesWritten,
        IntPtr mustBeZero
    );

    [LibraryImport(Libraries.Kernel32, EntryPoint = "ReadFile", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool ReadFile(
        SafeFileHandle handle,
        [Out] byte[] lpBuffer,
        int numBytesToRead,
        out int numBytesRead,
        IntPtr mustBeZero
    );

    #endregion
}