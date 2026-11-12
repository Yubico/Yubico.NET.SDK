using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Core.Native;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.UnitTests.Protocols.SmartCard.Apdu.Fakes;
using System.Runtime.InteropServices;
namespace Audit20261111.DocExamples.Test206;
public sealed partial class DocumentationBody206
{
// ✅ RIGHT — source-generated, compile-time-visible marshalling
[LibraryImport(Libraries.NativeShims, EntryPoint = "Native_Foo",
    StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
[DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
internal static partial int Foo(string name, [MarshalAs(UnmanagedType.U1)] bool flag);
}
