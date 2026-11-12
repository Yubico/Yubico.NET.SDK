using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Core.Utilities;
using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.SecurityDomain;

namespace Audit20261111.DocExamples.Full;
internal static class Body292
{
const byte ClaGlobalPlatform = 0x80;

// Common instructions
const byte InsGetData = 0xCA;
const byte InsPutKey = 0xD8;
const byte InsInitializeUpdate = 0x50;
const byte InsExternalAuthenticate = 0x82;
const byte InsInternalAuthenticate = 0x88;
const byte InsDelete = 0xE4;
const byte InsGenerateKey = 0xF1;
const byte InsPerformSecurityOperation = 0x2A;

// TLV tags for data encoding
const int TagKeyInformationTemplate = 0xE0;
const byte TagControlReference = 0xA6;
const byte TagKidKvn = 0x83;
}
