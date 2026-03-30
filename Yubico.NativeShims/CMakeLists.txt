cmake_minimum_required(VERSION 3.15)

#
# Project version
#
if(NOT DEFINED PROJECT_VERSION)
    set(PROJECT_VERSION "1.14.0")
endif()
set(VCPKG_MANIFEST_VERSION ${PROJECT_VERSION})

project(Yubico.NativeShims VERSION ${PROJECT_VERSION})
include(CheckCCompilerFlag)

# =============================================================================
# Platform configuration
#
# Each platform sets:
#   PLATFORM_*   — boolean flag for conditional logic later
#   BACKEND      — which smart card API to use (winscard/pcsc/macscard)
#   exports file — controls which symbols are visible in the shared library
#   hardening    — platform-appropriate security compiler/linker flags
# =============================================================================

if (APPLE OR UNIX)

    # --- macOS / Linux: platform identity and symbol exports ---

    if (APPLE)
        set(PLATFORM_MACOS true)
        set(BACKEND "macscard")
        add_link_options("-Wl,-exported_symbols_list,${CMAKE_CURRENT_SOURCE_DIR}/exports.llvm")
    elseif(UNIX)
        find_package(PkgConfig REQUIRED)
        set(PLATFORM_LINUX true)
        set(BACKEND "pcsc")
        # RELRO + NOW = full read-only relocations (exploit mitigation)
        add_link_options("-Wl,-z,relro,-z,now,--version-script=${CMAKE_CURRENT_SOURCE_DIR}/exports.gnu")
    endif()

    # --- macOS / Linux: security hardening (GCC/Clang) ---

    if (CMAKE_C_COMPILER_ID STREQUAL "Clang" OR
        CMAKE_C_COMPILER_ID STREQUAL "AppleClang" OR
        CMAKE_C_COMPILER_ID STREQUAL "GNU")

        # Warnings — treat all as errors
        add_compile_options(-Wall -Wextra -Werror)
        add_compile_options(-Wformat -Wformat-nonliteral -Wformat-security)
        add_compile_options(-Wshadow -Wcast-qual -Wbad-function-cast)
        add_compile_options(-pedantic -pedantic-errors)

        # Position-independent code (required for shared libraries)
        add_compile_options(-fpic)
        add_link_options(-fpic)

        # Optimization and runtime hardening
        add_compile_options(-O2)
        add_compile_definitions(-D_FORTIFY_SOURCE=2)  # Buffer overflow detection in libc calls

        # Stack protection — prefer -all variant, fall back to basic
        check_c_compiler_flag("-fstack-protector-all" HAVE_STACK_PROTECTOR_ALL)
        if (HAVE_STACK_PROTECTOR_ALL)
            message(STATUS "-fstack-protector-all support detected")
            add_compile_options(-fstack-protector-all)
            add_link_options(-fstack-protector-all)
        else ()
            check_c_compiler_flag("-fstack-protector" HAVE_STACK_PROTECTOR)
            if(HAVE_STACK_PROTECTOR)
                message(STATUS "-fstack-protector support detected")
                add_compile_options(-fstack-protector)
                add_link_options(-fstack-protector)
            else ()
                message(WARNING "No stack protection supported.")
            endif ()
        endif()
    elseif()
        message(WARNING "No compatible compiler found for setting additional security compiler flags.")
    endif()

elseif(WIN32)

    # --- Windows: platform identity ---

    set(PLATFORM_WINDOWS true)
    set(BACKEND "winscard")

    # Security hardening
    add_link_options("/guard:cf")                # Control Flow Guard
    add_compile_options("/GS" "/Gs")             # Buffer security check + stack probe

    # Symbol exports
    add_link_options("/def:${CMAKE_CURRENT_SOURCE_DIR}/exports.msvc")

    # Static CRT to avoid VC++ Redistributable dependency (see #391)
    set(CMAKE_MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>")

endif()

# =============================================================================
# Dependencies
# =============================================================================

include(${CMAKE_SOURCE_DIR}/cmake/pcscd.cmake)
find_pcscd()
find_package(OpenSSL REQUIRED)

# =============================================================================
# Build target
# =============================================================================

add_library(Yubico.NativeShims SHARED)

# Link-time optimization (cross-file inlining, dead code elimination)
include(CheckIPOSupported)
check_ipo_supported(RESULT result OUTPUT output)
if(result)
    message(INFO "IPO is enabled.")
    set_property(TARGET Yubico.NativeShims PROPERTY INTERPROCEDURAL_OPTIMIZATION TRUE)
else()
    message(WARNING "IPO is not supported: ${output}")
endif()

# Generate header from template (substitutes @VERSION@ etc.)
configure_file(
    ${CMAKE_CURRENT_SOURCE_DIR}/Yubico.NativeShims.h.in
    ${CMAKE_CURRENT_SOURCE_DIR}/Yubico.NativeShims.h
    @ONLY
    )

target_include_directories(
    Yubico.NativeShims
    PUBLIC
        "${PROJECT_BINARY_DIR}"
        )

# =============================================================================
# Sources
# =============================================================================

target_sources(
    Yubico.NativeShims
    PRIVATE
        pcsc.c              # Smart card (PC/SC) shim
        ssl.bignum.c        # OpenSSL BIGNUM operations
        ssl.ecgroup.c       # OpenSSL EC group operations
        ssl.ecpoint.c       # OpenSSL EC point operations
        ssl.gcmevp.c        # OpenSSL AES-GCM via EVP interface
        ssl.cmac.c          # OpenSSL CMAC operations
        )

if(WIN32)
    target_sources(
        Yubico.NativeShims
        PRIVATE
            Yubico.NativeShims.rc   # Windows version info resource
            )
endif()

# =============================================================================
# Link libraries
# =============================================================================

target_link_libraries(
    Yubico.NativeShims
        ${PCSC_LIBRARIES}       # Linux: libpcsclite
        ${PCSC_WIN_LIBS}        # Windows: winscard.lib
        ${PCSC_MACOSX_LIBS}     # macOS: PCSC.framework
        ${PCSC_CUSTOM_LIBS}     # Cross-compilation overrides
        OpenSSL::Crypto         # libcrypto (static via vcpkg on Windows)
        )
