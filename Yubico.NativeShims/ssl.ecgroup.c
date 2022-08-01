#include <openssl/ec.h>
#include "native_abi.h"
#include "Yubico.NativeShims.h"

typedef void* Native_EC_GROUP;

Native_EC_GROUP
NATIVEAPI
Native_EC_GROUP_new_by_curve_name(
    int32_t nid
    )
{
    return EC_GROUP_new_by_curve_name(nid);
}

void
NATIVEAPI
Native_EC_GROUP_free(
    Native_EC_GROUP group
    )
{
    return EC_GROUP_free(group);
}
