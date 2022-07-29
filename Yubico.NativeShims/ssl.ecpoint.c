#include <openssl/ec.h>
#include "native_abi.h"
#include "Yubico.NativeShims.h"

typedef void* Native_EC_POINT;
typedef void* Native_EC_GROUP;
typedef void* Native_BIGNUM;
typedef void* Native_BN_CTX;

#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wdeprecated-declarations"

Native_EC_POINT
NATIVEAPI
Native_EC_POINT_new(
    const Native_EC_GROUP group
    )
{
    return EC_POINT_new(group);
}

void
NATIVEAPI
Native_EC_POINT_free(
    Native_EC_POINT point
    )
{
    return EC_POINT_free(point);
}


// Function is deprecated in OpenSSL 3.x
int32_t
NATIVEAPI
Native_EC_POINT_set_affine_coordinates_GFp(
    const Native_EC_GROUP group,
    Native_EC_POINT p,
    const Native_BIGNUM x,
    const Native_BIGNUM y,
    Native_BN_CTX ctx
    )
{
    return EC_POINT_set_affine_coordinates_GFp(group, p, x, y, ctx);
}

// Function is deprecated in OpenSSL 3.x
int32_t
NATIVEAPI
Native_EC_POINT_get_affine_coordinates_GFp(
    const Native_EC_GROUP group,
    const Native_EC_POINT p,
    Native_BIGNUM x,
    Native_BIGNUM y,
    Native_BN_CTX ctx
    )
{
    return EC_POINT_get_affine_coordinates_GFp(group, p, x, y, ctx);
}

int32_t
NATIVEAPI
Native_EC_POINT_mul(
    const Native_EC_GROUP group,
    Native_EC_POINT r,
    const Native_BIGNUM n,
    const Native_EC_POINT q,
    const Native_BIGNUM m,
    const Native_BN_CTX ctx
    )
{
    return EC_POINT_mul(group, r, n, q, m, ctx);
}

#pragma clang diagnostic pop