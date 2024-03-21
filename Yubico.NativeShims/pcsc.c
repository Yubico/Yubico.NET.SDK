#include "native_abi.h"
#include "Yubico.NativeShims.h"

#ifdef BACKEND_PCSC
#ifdef HAVE_PCSC_WINSCARD_H
# include <PCSC/wintypes.h>
# include <PCSC/winscard.h>
#else
# include <winscard.h>
#endif
#endif

#include <stdio.h>

#pragma pack(1)

typedef struct
{
    const char* szReader;
    void* pvUserData;
    uint32_t dwCurrentState;
    uint32_t dwEventState;
    uint32_t cbAtr;
    uint8_t rgbAtr[36];
} NATIVE_SCARD_READERSTATE;

#pragma pack()

int32_t
NATIVEAPI
Native_SCardEstablishContext(
    uint32_t dwScope,
    LPSCARDCONTEXT phContext
)
{
    return SCardEstablishContext(
        dwScope,
        NULL,
        NULL,
        phContext
    );
}

int32_t
NATIVEAPI
Native_SCardReleaseContext(
    SCARDCONTEXT hContext
)
{
    return SCardReleaseContext(hContext);
}

int32_t
NATIVEAPI
Native_SCardConnect(
    SCARDCONTEXT hContext,
    u8str_t szReader,
    uint32_t dwShareMode,
    uint32_t dwPreferredProtocols,
    LPSCARDHANDLE phCard,
    uint32_t* pdwActiveProtocol
)
{
    DWORD activeProtocol;
    int32_t status = SCardConnect(
        hContext,
        szReader,
        dwShareMode,
        dwPreferredProtocols,
        phCard,
        &activeProtocol
    );
    *pdwActiveProtocol = (uint32_t)activeProtocol;
    return status;
}

int32_t
NATIVEAPI
Native_SCardReconnect(
    SCARDHANDLE hCard,
    uint32_t dwShareMode,
    uint32_t dwPreferredProtocols,
    uint32_t dwInitialization,
    uint32_t* pdwActiveProtocol
)
{
    DWORD activeProtocol;
    int32_t status = SCardReconnect(
        hCard,
        dwShareMode,
        dwPreferredProtocols,
        dwInitialization,
        &activeProtocol
    );
    *pdwActiveProtocol = (uint32_t)activeProtocol;
    return status;
}

int32_t
NATIVEAPI
Native_SCardDisconnect(
    SCARDHANDLE hCard,
    uint32_t dwDisposition
)
{
    return SCardDisconnect(
        hCard,
        dwDisposition
    );
}

int32_t
NATIVEAPI
Native_SCardBeginTransaction(
    SCARDHANDLE hCard
)
{
    return SCardBeginTransaction(hCard);
}

int32_t
NATIVEAPI
Native_SCardEndTransaction(
    SCARDHANDLE hCard,
    uint32_t dwDisposition
)
{
    return SCardEndTransaction(
        hCard,
        dwDisposition
        );
}

int32_t
NATIVEAPI
Native_SCardGetStatusChange(
    SCARDCONTEXT hContext,
    uint32_t dwTimeout,
    NATIVE_SCARD_READERSTATE* rgReaderStates,
    uint32_t cReaders
)
{
    SCARD_READERSTATE* readerStates = (SCARD_READERSTATE*)malloc(cReaders * sizeof(SCARD_READERSTATE));

    if (readerStates == NULL)
    {
        return (int32_t)SCARD_E_NO_MEMORY;
    }

    memset(readerStates, 0, cReaders * sizeof(SCARD_READERSTATE));

    for (uint32_t i = 0; i < cReaders; i++)
    {
        readerStates[i].szReader = rgReaderStates[i].szReader;
        readerStates[i].dwCurrentState = rgReaderStates[i].dwCurrentState;
        readerStates[i].dwEventState = rgReaderStates[i].dwEventState;
        readerStates[i].cbAtr = rgReaderStates[i].cbAtr;
        memcpy(readerStates[i].rgbAtr, rgReaderStates[i].rgbAtr, sizeof(readerStates[i].rgbAtr));
    }

    int32_t result = (int32_t)SCardGetStatusChange(
        hContext,
        dwTimeout,
        readerStates,
        cReaders
    );

    for (uint32_t i = 0; i < cReaders; i++)
    {
        rgReaderStates[i].dwCurrentState = readerStates[i].dwCurrentState;
        rgReaderStates[i].dwEventState = readerStates[i].dwEventState;
        rgReaderStates[i].cbAtr = readerStates[i].cbAtr;
        memcpy(rgReaderStates[i].rgbAtr, readerStates[i].rgbAtr, sizeof(readerStates[i].rgbAtr));
    }

    free(readerStates);

    return result;
}

int32_t
NATIVEAPI
Native_SCardTransmit(
    SCARDHANDLE hCard,
    const SCARD_IO_REQUEST* pioSendPci,
    const void* pbSendBuffer,
    uint32_t cbSendLength,
    SCARD_IO_REQUEST* pioRecvPci,
    void* pbRecvBuffer,
    uint32_t* pcbRecvLength
)
{
    DWORD recvLength = *pcbRecvLength;
    int32_t status = SCardTransmit(
        hCard,
        pioSendPci,
        pbSendBuffer,
        cbSendLength,
        pioRecvPci,
        pbRecvBuffer,
        &recvLength
    );
    *pcbRecvLength = (uint32_t)recvLength;
    return status;
}

int32_t
NATIVEAPI
Native_SCardListReaders(
    SCARDCONTEXT hContext,
    const u8str_t mszGroups,
    u8str_t mszReaders,
    uint32_t* pcchReaders
)
{
    DWORD cchReaders = *pcchReaders;
    int32_t status = SCardListReaders(
        hContext,
        mszGroups,
        mszReaders,
        &cchReaders
    );
    *pcchReaders = (uint32_t)cchReaders;
    return status;
}

int32_t
NATIVEAPI
Native_SCardCancel(
    SCARDCONTEXT hContext
)
{
    return SCardCancel(hContext);
}
