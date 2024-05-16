using System;
using System.Collections.Generic;

namespace Yubico.YubiKey
{
    public interface IGetPagedDeviceInfoCommand<out T> : IYubiKeyCommand<T>
        where T : IYubiKeyResponseWithData<Dictionary<int, ReadOnlyMemory<byte>>>
    {
        public byte Page { get; set; }

    }
} 
    
