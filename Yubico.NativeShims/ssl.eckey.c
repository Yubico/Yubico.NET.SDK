#include <openssl/ec.h>
#include "native_abi.h"
#include "Yubico.NativeShims.h"

typedef void* Native_EC_KEY;
typedef void* Native_BIGNUM;
typedef const void* Native_CBIGNUM;

#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wdeprecated-declarations"

// Function is deprecated in OpenSSL 3.x
Native_EC_KEY
NATIVEAPI
Native_EC_KEY_new_by_curve_name(
    int32_t nid
    )
{
    return EC_KEY_new_by_curve_name(nid);
}

// Function is deprecated in OpenSSL 3.x
void
NATIVEAPI
Native_EC_KEY_free(
    Native_EC_KEY key
    )
{
    return EC_KEY_free(key);
}

// Function is deprecated in OpenSSL 3.x
Native_CBIGNUM
NATIVEAPI
Native_EC_KEY_get0_private_key(
    const Native_EC_KEY key
    )
{
    return EC_KEY_get0_private_key(key);
}

// Function is deprecated in OpenSSL 3.x
int32_t
NATIVEAPI
Native_EC_KEY_set_private_key(
    Native_EC_KEY key,
    const Native_BIGNUM prv
    )
{
    return EC_KEY_set_private_key(key, prv);
}

#pragma clang diagnostic pop