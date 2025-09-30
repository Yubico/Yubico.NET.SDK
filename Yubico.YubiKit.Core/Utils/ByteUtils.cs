namespace Yubico.YubiKit.Core.Utils;

public static class ByteUtils
{
    public static byte ValidateByte(int byteInt, string name)
    {
        if (byteInt is > 255 or < byte.MinValue)
            throw new ArgumentOutOfRangeException("Invalid value for " + name + ", must fit in a byte");

        return (byte)byteInt;
    }
}