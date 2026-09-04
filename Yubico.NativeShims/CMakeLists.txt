cmake_minimum_required(VERSION 3.15)

#
# Project version
#
if(NOT DEFINED PROJECT_VERSION)
    set(PROJECT_VERSION "1.0.0")
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
        # 64 KB max page size: aligns LOAD segments so the shim is loadable on systems
        # with larger memory pages. Required for Android 15+ 16 KB-page devices (Google
        # Play, Nov 2025) AND for aarch64 Linux distros that use 64 KB kernel pages
        # (e.g. RHEL/Fedora). 64 KB is a superset alignment: it satisfies 4 KB, 16 KB and
        # 64 KB page sizes. x86_64 defaults to 4 KB; aarch64 already defaults to 64 KB, so
        # this is a no-op there and never lowers it. https://developer.android.com/guide/practices/page-sizes
        add_link_options("-Wl,-z,max-page-size=65536")
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
    else()
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
# Build targets
# =============================================================================

add_library(Yubico.NativeShims SHARED)

option(YUBICO_BUILD_STATIC "Build the merged NativeShims and OpenSSL static archive" OFF)
if(YUBICO_BUILD_STATIC)
    add_library(Yubico.NativeShims.Static STATIC)
    set_target_properties(
        Yubico.NativeShims.Static
        PROPERTIES
            OUTPUT_NAME "Yubico.NativeShims"
            ARCHIVE_OUTPUT_DIRECTORY "${CMAKE_CURRENT_BINARY_DIR}/static"
            ARCHIVE_OUTPUT_DIRECTORY_RELEASE "${CMAKE_CURRENT_BINARY_DIR}/static"
            ARCHIVE_OUTPUT_DIRECTORY_DEBUG "${CMAKE_CURRENT_BINARY_DIR}/static"
            ARCHIVE_OUTPUT_DIRECTORY_RELWITHDEBINFO "${CMAKE_CURRENT_BINARY_DIR}/static"
            ARCHIVE_OUTPUT_DIRECTORY_MINSIZEREL "${CMAKE_CURRENT_BINARY_DIR}/static"
        )

    # Keep compiler-specific LTO objects out of the package archive so Native AOT
    # consumers can link it with the platform toolchain selected by the .NET SDK.
    set_property(TARGET Yubico.NativeShims.Static PROPERTY INTERPROCEDURAL_OPTIMIZATION FALSE)
endif()

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

if(YUBICO_BUILD_STATIC)
    target_include_directories(
        Yubico.NativeShims.Static
        PUBLIC
            "${PROJECT_BINARY_DIR}"
            )
endif()

# =============================================================================
# Sources
# =============================================================================

set(NATIVE_SHIMS_SOURCES
    pcsc.c              # Smart card (PC/SC) shim
    ssl.bignum.c        # OpenSSL BIGNUM operations
    ssl.ecgroup.c       # OpenSSL EC group operations
    ssl.ecpoint.c       # OpenSSL EC point operations
    ssl.gcmevp.c        # OpenSSL AES-GCM via EVP interface
    ssl.cmac.c          # OpenSSL CMAC operations
    )

target_sources(Yubico.NativeShims PRIVATE ${NATIVE_SHIMS_SOURCES})
if(YUBICO_BUILD_STATIC)
    target_sources(Yubico.NativeShims.Static PRIVATE ${NATIVE_SHIMS_SOURCES})
endif()

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

if(YUBICO_BUILD_STATIC)
    # PC/SC remains a final-consumer dependency. OpenSSL supplies include paths
    # here and is merged into the package archive by the post-build step below.
    target_link_libraries(Yubico.NativeShims.Static PRIVATE OpenSSL::Crypto)

    if(APPLE)
        find_program(NATIVE_SHIMS_LIBTOOL libtool REQUIRED)
        set(NATIVE_SHIMS_MERGE_PLATFORM "APPLE")
    elseif(WIN32)
        set(NATIVE_SHIMS_MERGE_PLATFORM "WINDOWS")
    else()
        set(NATIVE_SHIMS_MERGE_PLATFORM "LINUX")
    endif()

    add_custom_command(
        TARGET Yubico.NativeShims.Static
        POST_BUILD
        COMMAND
            ${CMAKE_COMMAND}
            "-DMERGE_PLATFORM=${NATIVE_SHIMS_MERGE_PLATFORM}"
            "-DINPUT_ARCHIVE=$<TARGET_FILE:Yubico.NativeShims.Static>"
            "-DDEPENDENCY_ARCHIVE=$<TARGET_FILE:OpenSSL::Crypto>"
            "-DOUTPUT_ARCHIVE=$<TARGET_FILE:Yubico.NativeShims.Static>"
            "-DAR=${CMAKE_AR}"
            "-DLIBTOOL=${NATIVE_SHIMS_LIBTOOL}"
            -P "${CMAKE_CURRENT_SOURCE_DIR}/cmake/merge_static_libraries.cmake"
        COMMENT "Merging NativeShims and OpenSSL into $<TARGET_FILE_NAME:Yubico.NativeShims.Static>"
        VERBATIM
        )
endif()
