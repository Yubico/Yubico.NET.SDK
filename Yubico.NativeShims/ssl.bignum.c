#include <openssl/bn.h>
#include "native_abi.h"
#include "Yubico.NativeShims.h"

typedef void* Native_BIGNUM;

int32_t
NATIVEAPI
Native_BN_bn2bin(
    const Native_BIGNUM a,
    uint8_t* to
)
{
    return BN_bn2bin(a, to);
}

Native_BIGNUM
NATIVEAPI
Native_BN_bin2bn(
    const uint8_t* s,
    int32_t len,
    Native_BIGNUM ret
)
{
    return BN_bin2bn(s, len, ret);
}

int32_t
NATIVEAPI
Native_BN_num_bytes(
    const Native_BIGNUM a
)
{
    return BN_num_bytes(a);
}

void
NATIVEAPI
Native_BN_clear_free(
    const Native_BIGNUM a
)
{
    return BN_clear_free(a);
}
