#include <openssl/ec.h>
#include "native_abi.h"
#include "Yubico.NativeShims.h"

typedef void* Native_EC_KEY;
typedef void* Native_BIGNUM;

Native_EC_KEY
NATIVEAPI
Native_EC_KEY_new_by_curve_name(
    int32_t nid
    )
{
    return EC_KEY_new_by_curve_name(nid);
}

void
NATIVEAPI
Native_EC_KEY_free(
    Native_EC_KEY key
    )
{
    return EC_KEY_free(key);
}

Native_BIGNUM
NATIVEAPI
Native_EC_KEY_get0_private_key(
    const Native_EC_KEY key
    )
{
    return EC_KEY_get0_private_key(key);
}

int32_t
NATIVEAPI
Native_EC_KEY_set_private_key(
    Native_EC_KEY key,
    const Native_BIGNUM prv
    )
{
    return EC_KEY_set_private_key(key, prv);
}
