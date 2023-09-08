#include <openssl/evp.h>
#include "native_abi.h"
#include "Yubico.NativeShims.h"

typedef void* Native_EVP_CIPHER_CTX;

Native_EVP_CIPHER_CTX
NATIVEAPI
Native_EVP_CIPHER_CTX_new()
{
    return EVP_CIPHER_CTX_new();
}

void
NATIVEAPI
Native_EVP_CIPHER_CTX_free(
    Native_EVP_CIPHER_CTX c
    )
{
    EVP_CIPHER_CTX_free(c);
}

int32_t
NATIVEAPI
Native_EVP_Aes256Gcm_Init(
    int32_t isEncrypt,
    Native_EVP_CIPHER_CTX c,
    uint8_t* keyData,
    uint8_t* nonce
)
{
    if (isEncrypt != 0)
    {
        return EVP_EncryptInit_ex(c, EVP_aes_256_gcm(), NULL, keyData, nonce);
    }

    return EVP_DecryptInit_ex(c, EVP_aes_256_gcm(), NULL, keyData, nonce);
}

int32_t
NATIVEAPI
Native_EVP_Update(
    Native_EVP_CIPHER_CTX c,
    uint8_t* output,
    int32_t* outLen,
    uint8_t* input,
    int32_t inLen
    )
{
    if (EVP_CIPHER_CTX_encrypting(c) != 0)
    {
        return EVP_EncryptUpdate(c, output, outLen, input, inLen);
    }

    return EVP_DecryptUpdate(c, output, outLen, input, inLen);
}

int32_t
NATIVEAPI
Native_EVP_Final_ex(
    Native_EVP_CIPHER_CTX c,
    uint8_t* output,
    int32_t* outLen
    )
{
    if (EVP_CIPHER_CTX_encrypting(c) != 0)
    {
        return EVP_EncryptFinal_ex(c, output, outLen);
    }

    return EVP_DecryptFinal_ex(c, output, outLen);
}

int32_t
NATIVEAPI
Native_EVP_CIPHER_CTX_ctrl(
    Native_EVP_CIPHER_CTX c,
    int32_t csCmd,
    int32_t p1,
    uint8_t* p2
    )
{
    int cmd = EVP_CTRL_CCM_SET_TAG;

    /* The C# code can send a cmd, but it has no way to guarantee that the value
     * it sends is the value in the .h file this code uses. It is possible to
     * look at the .h file and get the numbers defined, but there is a nonzero
     * probability that the number is one .h file is different in another
     * (different version of OpenSSL, different platform). Not likely, but still
     * possible (note that we have seen this happen with SCard where values of
     * #defines were different between Windows and Linux).
     * There is no way to share information such as flag values between C and C#,
     * so this C code just has to know what 16 and 17 mean, and hope the C# code
     * never changes those meanings.
     */
    switch (csCmd)
    {
        default:
            return 0;

        case 16:
            cmd = EVP_CTRL_CCM_GET_TAG;

        case 17:
            break;
    }

    return EVP_CIPHER_CTX_ctrl(c, cmd, p1, p2);
}
