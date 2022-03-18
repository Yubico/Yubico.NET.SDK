#pragma once

#include <stddef.h>
#include <stdlib.h>
#include <stdint.h>
#include <string.h>

#include "Yubico.NativeShims.h"

typedef char* u8str_t;

#ifdef PLATFORM_WINDOWS

#define NATIVEAPI __stdcall

#elif PLATFORM_MACOS

#define NATIVEAPI __attribute((cdecl))

#elif PLATFORM_LINUX

#define NATIVEAPI __attribute((cdecl))

#else

#define NATIVEAPI

#endif

