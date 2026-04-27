#include <openssl/ec.h>
#include "native_abi.h"
#include "Yubico.NativeShims.h"

typedef void* Native_EC_POINT;
typedef void* Native_EC_GROUP;
typedef void* Native_BIGNUM;
typedef void* Native_BN_CTX;

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
    EC_POINT_free(point);
}

int32_t
NATIVEAPI
Native_EC_POINT_set_affine_coordinates(
    const Native_EC_GROUP group,
    Native_EC_POINT p,
    const Native_BIGNUM x,
    const Native_BIGNUM y,
    Native_BN_CTX ctx
    )
{
    return EC_POINT_set_affine_coordinates(group, p, x, y, ctx);
}

int32_t
NATIVEAPI
Native_EC_POINT_get_affine_coordinates(
    const Native_EC_GROUP group,
    const Native_EC_POINT p,
    Native_BIGNUM x,
    Native_BIGNUM y,
    Native_BN_CTX ctx
    )
{
    return EC_POINT_get_affine_coordinates(group, p, x, y, ctx);
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

// Validates that an EC_POINT lies on the curve defined by the EC_GROUP.
// Returns 1 if the point is on the curve, 0 if not, -1 on error.
// Required for ARKG-P256 input validation: untrusted public keys (pkBl, pkKem)
// received from authenticator responses MUST be validated before use to prevent
// invalid-curve attacks.
int32_t
NATIVEAPI
Native_EC_POINT_is_on_curve(
    const Native_EC_GROUP group,
    const Native_EC_POINT point,
    Native_BN_CTX ctx
    )
{
    return EC_POINT_is_on_curve(group, point, ctx);
}
