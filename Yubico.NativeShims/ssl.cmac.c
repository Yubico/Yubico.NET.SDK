#include "native_abi.h"
#include "Yubico.NativeShims.h"
#include "openssl/evp.h"
#ifdef PLATFORM_LINUX
#include "openssl/cmac.h"
#endif

#define CMAC_MAX_BLOCK_SIZE 16
typedef void* Native_EVP_MAC_CTX;

Native_EVP_MAC_CTX
NATIVEAPI
Native_CMAC_EVP_MAC_CTX_new()
{
#ifndef PLATFORM_LINUX
    EVP_MAC *mac = NULL;
    EVP_MAC_CTX *macCtx = NULL;

    mac = EVP_MAC_fetch(NULL, "CMAC", "provider=default");
    if (mac != NULL)
    {
        macCtx = EVP_MAC_CTX_new(mac);
        EVP_MAC_free(mac);
    }

    return macCtx;
#else
    return CMAC_CTX_new();
#endif
}

void
NATIVEAPI
Native_EVP_MAC_CTX_free(
    Native_EVP_MAC_CTX c
    )
{
#ifndef PLATFORM_LINUX
    EVP_MAC_CTX_free(c);
#else
    CMAC_CTX_free(c);
#endif
}

int32_t
NATIVEAPI
Native_CMAC_EVP_MAC_init(
    Native_EVP_MAC_CTX c,
    int32_t algorithm,
    uint8_t* keyData,
    int32_t keyLen
    )
{
#ifndef PLATFORM_LINUX
    char *cipherString;
    int32_t cipherStringLen;
    int32_t blockSize;

    switch (algorithm)
    {
        default:
            cipherString = "aes-128-cbc";
            cipherStringLen = 11;
            blockSize = 16;
            break;

        case 2:
            cipherString = "aes-192-cbc";
            cipherStringLen = 11;
            blockSize = 16;
            break;

        case 3:
            cipherString = "aes-256-cbc";
            cipherStringLen = 11;
            blockSize = 16;
            break;
    }
//    { "cipher", OSSL_PARAM_UTF8_STRING, cipherString, cipherStringLen, (size_t)-1 },
//    { "iv", OSSL_PARAM_OCTET_STRING, iv, blockSize, (size_t)-1 },
//    { NULL, 0, NULL, 0, 0 }

    unsigned char iv[CMAC_MAX_BLOCK_SIZE] = { 0 };
    OSSL_PARAM params[3] = {
        OSSL_PARAM_construct_utf8_string("cipher", cipherString, cipherStringLen),
        OSSL_PARAM_construct_octet_string("iv", iv, blockSize),
        OSSL_PARAM_END
    };

    return EVP_MAC_init(c, keyData, keyLen, params);
#else
    EVP_CIPHER *evpCipher;

    switch (algorithm)
    {
        default:
            evpCipher = EVP_aes_128_cbc();
            break;

        case 2:
            evpCipher = EVP_aes_192_cbc();
            break;

        case 3:
            evpCipher = EVP_aes_256_cbc();
            break;
    }

    return CMAC_Init(c, keyData, keyLen, evpCipher, NULL);
#endif
}

int32_t
NATIVEAPI
Native_CMAC_EVP_MAC_update(
    Native_EVP_MAC_CTX c,
    uint8_t* input,
    int32_t inLen
    )
{
#ifndef PLATFORM_LINUX
    return EVP_MAC_update(c, input, inLen);
#else
    return CMAC_Update(c, input, inLen);
#endif
}

int32_t
NATIVEAPI
Native_CMAC_EVP_MAC_final(
    Native_EVP_MAC_CTX c,
    uint8_t* output,
    int32_t outputSize,
    int32_t* outLen
    )
{
    int status;
    size_t outputLen = (size_t)outputSize;
#ifndef PLATFORM_LINUX
    status = EVP_MAC_final(c, output, &outputLen, outputSize);
#else
    status = CMAC_Final(c, output, &outputLen);
#endif
    *outLen = (int32_t)outputLen;
    return status;
}
