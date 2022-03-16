#ifndef _NATIVE_ABI_H_
#define _NATIVE_ABI_H_

#include <stddef.h>
#include <stdlib.h>
#include <stdint.h>
#include <string.h>

typedef char* u8str_t;

#ifdef WINDOWS

#define NATIVEAPI __stdcall

#elif MACOS

#define NATIVEAPI __attribute((cdecl))

#elif LINUX

#define NATIVEAPI __attribute((cdecl))

#else

#define NATIVEAPI

#endif

#endif