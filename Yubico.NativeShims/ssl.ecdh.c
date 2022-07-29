#include <openssl/ecdh.h>
#include "native_abi.h"
#include "Yubico.NativeShims.h"

typedef void* Native_EC_POINT;
typedef void* Native_EC_KEY;

#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wdeprecated-declarations"

int32_t
NATIVEAPI
Native_ECDH_compute_key(
    void* out,
    uint64_t outlen,
    const Native_EC_POINT public_key,
    Native_EC_KEY ecdh,
    void* kdf)
{
    return ECDH_compute_key(out, outlen, public_key, ecdh, kdf);
}

#pragma clang diagnostic pop