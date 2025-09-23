using System.Runtime.InteropServices;
using Yubico.YubiKit.Core;

namespace Yubico.YubiKit;

public class YubiKeyManager
{
    public async Task<IEnumerable<IYubiKey>> GetPcscDevices()
    {
        var pcscDevices = await ReadPcscDevices();

        return pcscDevices.Select(pcscDevice => new PcscYubiKey(pcscDevice)).Cast<IYubiKey>().ToList();
    }

    private async Task<IEnumerable<PcscDevice>> ReadPcscDevices()
    {
        throw new NotImplementedException();
    }
}

internal class PcscYubiKey : IYubiKey
{
    private PcscDevice pcscDevice;

    public PcscYubiKey(PcscDevice pcscDevice)
    {
        this.pcscDevice = pcscDevice;
    }
}

public interface IYubiKey
{
}

public static class PscsInterop
{
    
            [DllImport(Libraries.NativeShims, EntryPoint = "Native_SCardListReaders", ExactSpelling = true, CharSet = CharSet.Ansi)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        private static extern uint SCardListReaders(
            SCardContext context,
            byte[]? groups,
            byte[]? readerNames,
            ref int readerNamesLength
            );

        public static uint SCardListReaders(
            SCardContext context,
            string[]? groups,
            out string[] readerNames)
        {
            readerNames = Array.Empty<string>();

            byte[]? rawGroups = null;

            if (!(groups is null))
            {
                rawGroups = MultiString.GetBytes(groups, System.Text.Encoding.ASCII);
            }

            int readerNamesLength = 0;

            uint result = SCardListReaders(
                context,
                rawGroups,
                null,
                ref readerNamesLength);

            if (result == ErrorCode.SCARD_S_SUCCESS)
            {
                if (readerNamesLength == 0)
                {
                    throw new PlatformApiException(ExceptionMessages.SCardListReadersUnexpectedLength);
                }

                byte[] rawReaderNames = new byte[readerNamesLength];

                result = SCardListReaders(
                    context,
                    rawGroups,
                    rawReaderNames,
                    ref readerNamesLength);

                readerNames = MultiString.GetStrings(rawReaderNames, System.Text.Encoding.ASCII);
            }

            return result;
        }
}